# ADR-008: Asset Management Architecture

**Date:** 2024-12-21
**Status:** Proposed
**References:** Issue #428 (Epic), Issue #429 (Implementation)

## Context

KeenEyes currently lacks a unified asset management system. Each subsystem loads resources independently:
- Graphics: `graphics.CreateTexture(...)` with raw pixel data
- Audio: `audio.LoadClip(path)` returning handles
- No shared caching, reference counting, or async loading

### Problems with Current Approach

1. **Duplicate loading** - Same texture loaded twice wastes memory
2. **No reference counting** - When is it safe to unload?
3. **No async loading** - Frame hitches during loads
4. **No hot-reload** - Must restart to see asset changes
5. **Inconsistent APIs** - Each subsystem has different patterns

### Requirements (from Issue #429)

- Load assets by path, return opaque handles
- Reference counting with automatic cleanup
- Async loading with priority queues
- Built-in loaders: textures, audio, models, fonts
- Custom loader registration
- Hot-reload in development mode
- No duplicate loads (caching)

## Decision

Create `KeenEyes.Assets` as a higher-level abstraction that coordinates with existing subsystems (Graphics, Audio) while adding unified caching, reference counting, and async loading capabilities.

### Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                           KeenEyes.Assets                                │
├──────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐             │
│  │  AssetManager  │  │ StreamingMgr   │  │ ReloadManager  │             │
│  │  (facade)      │  │ (async queue)  │  │ (dev mode)     │             │
│  └───────┬────────┘  └───────┬────────┘  └───────┬────────┘             │
│          │                   │                   │                       │
│          ▼                   ▼                   ▼                       │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                        AssetCache                                │    │
│  │  ┌─────────────────────────────────────────────────────────┐    │    │
│  │  │ Path → AssetEntry { Asset, RefCount, State, Metadata }  │    │    │
│  │  └─────────────────────────────────────────────────────────┘    │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                 │                                        │
│                                 ▼                                        │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     IAssetLoader<T> Registry                     │    │
│  │  ┌─────────┐ ┌─────────────┐ ┌──────────┐ ┌────────────────┐    │    │
│  │  │ Texture │ │ AudioClip   │ │ Mesh     │ │ Custom...      │    │    │
│  │  │ Loader  │ │ Loader      │ │ Loader   │ │ Loaders        │    │    │
│  │  └────┬────┘ └──────┬──────┘ └────┬─────┘ └───────┬────────┘    │    │
│  └───────┼─────────────┼─────────────┼───────────────┼─────────────┘    │
│          │             │             │               │                   │
└──────────┼─────────────┼─────────────┼───────────────┼───────────────────┘
           │             │             │               │
           ▼             ▼             ▼               ▼
    ┌────────────┐ ┌───────────┐ ┌──────────────┐
    │ IGraphics  │ │ IAudio    │ │ SharpGLTF    │
    │ Context    │ │ Context   │ │ (pure C#)    │
    └────────────┘ └───────────┘ └──────────────┘
```

### Key Design Decisions

#### 1. Wrapper Asset Types (Not Raw Handles)

The asset system defines wrapper types that contain the underlying handles plus metadata:

```csharp
// Asset types wrap handles with metadata
public sealed class TextureAsset : IDisposable
{
    public TextureHandle Handle { get; }
    public int Width { get; }
    public int Height { get; }
    public TextureFormat Format { get; }
    internal IGraphicsContext Graphics { get; }

    public void Dispose() => Graphics.DeleteTexture(Handle);
}

public sealed class AudioClipAsset : IDisposable
{
    public AudioClipHandle Handle { get; }
    public TimeSpan Duration { get; }
    public int Channels { get; }
    public int SampleRate { get; }
    internal IAudioContext Audio { get; }

    public void Dispose() => Audio.UnloadClip(Handle);
}
```

**Rationale:** This decouples asset management from specific subsystem implementations and allows storing metadata alongside handles.

#### 2. Generic AssetHandle<T> with Reference Counting

```csharp
public readonly struct AssetHandle<T> : IDisposable, IEquatable<AssetHandle<T>>
    where T : class, IDisposable
{
    internal readonly int Id;
    internal readonly AssetManager Manager;

    public bool IsValid => Id > 0 && Manager != null;
    public AssetState State => IsValid ? Manager.GetState(Id) : AssetState.Invalid;
    public bool IsLoaded => State == AssetState.Loaded;

    public T? Asset => IsValid ? Manager.TryGetAsset<T>(Id) : null;

    // Dispose releases reference count
    public void Dispose() => Manager?.Release(Id);
}
```

**Key Points:**
- Handle is a **value type** (struct) for efficiency
- Contains internal ID + manager reference
- Calling `Dispose()` decrements reference count
- Asset stays loaded until refcount reaches 0 (based on cache policy)

#### 3. Component-Friendly AssetRef<T>

```csharp
[Component]
public partial struct AssetRef<T> where T : class, IDisposable
{
    /// <summary>Path to the asset for serialization.</summary>
    public string Path;

    /// <summary>Runtime handle (set by AssetResolutionSystem).</summary>
    internal int HandleId;

    public readonly bool IsResolved => HandleId > 0;
}
```

**Usage in ECS:**
```csharp
// In entity definition
world.Spawn()
    .With(new AssetRef<TextureAsset> { Path = "textures/player.png" })
    .With(new SpriteRenderer { /* ... */ })
    .Build();

// AssetResolutionSystem automatically resolves paths to handles
```

#### 4. Pluggable Loaders via IAssetLoader<T>

```csharp
public interface IAssetLoader<T> where T : class, IDisposable
{
    /// <summary>File extensions this loader handles (e.g., ".png", ".jpg").</summary>
    IReadOnlyList<string> Extensions { get; }

    /// <summary>Synchronous load from stream.</summary>
    T Load(Stream stream, AssetLoadContext context);

    /// <summary>Asynchronous load from stream.</summary>
    Task<T> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default);
}

public readonly record struct AssetLoadContext(
    string Path,
    AssetManager Manager,
    IServiceProvider? Services = null
);
```

**Built-in Loaders:**
| Loader | Asset Type | Extensions | Dependency |
|--------|------------|------------|------------|
| `TextureLoader` | `TextureAsset` | .png, .jpg, .bmp, .tga | StbImageSharp, IGraphicsContext |
| `AudioClipLoader` | `AudioClipAsset` | .wav, .ogg | NVorbis, IAudioContext |
| `MeshLoader` | `MeshAsset` | .gltf, .glb | SharpGLTF |
| `JsonLoader<T>` | `T` | .json | System.Text.Json |
| `RawLoader` | `RawAsset` | * | None |

#### 5. Async Loading with Priority Queue

```csharp
public enum LoadPriority
{
    Immediate = 0,  // Block until loaded (avoid in production)
    High = 1,       // Next in queue
    Normal = 2,     // Standard priority
    Low = 3,        // Background loading
    Streaming = 4   // Lowest priority, for level streaming
}

public sealed class StreamingManager
{
    private readonly PriorityQueue<LoadRequest, LoadPriority> queue;
    private readonly SemaphoreSlim concurrencyLimit;
    private readonly int maxConcurrentLoads;

    public async Task<AssetHandle<T>> LoadAsync<T>(
        string path,
        LoadPriority priority = LoadPriority.Normal,
        CancellationToken ct = default) where T : class, IDisposable
    {
        // Queue the request
        var request = new LoadRequest(path, typeof(T), priority);
        queue.Enqueue(request, priority);

        // Wait for slot
        await concurrencyLimit.WaitAsync(ct);
        try
        {
            var loader = GetLoader<T>();
            using var stream = fileSystem.Open(path);
            var asset = await loader.LoadAsync(stream, new AssetLoadContext(path, manager), ct);
            return cache.AddAndGetHandle<T>(path, asset);
        }
        finally
        {
            concurrencyLimit.Release();
        }
    }
}
```

#### 6. Reference-Counted Cache with Policies

```csharp
public enum CachePolicy
{
    /// <summary>Evict least-recently-used when cache is full.</summary>
    LRU,

    /// <summary>Only unload when explicitly requested.</summary>
    Manual,

    /// <summary>Unload immediately when refcount reaches 0.</summary>
    Aggressive
}

internal sealed class AssetCache
{
    private readonly Dictionary<string, AssetEntry> entries = new();
    private readonly CachePolicy policy;
    private readonly long maxBytes;
    private long currentBytes;

    internal AssetEntry GetOrCreate(string path) { /* ... */ }
    internal void AddRef(int id) { /* ... */ }
    internal void Release(int id) { /* ... */ }
    internal void Evict(int id) { /* ... */ }
    internal void TrimToSize(long targetBytes) { /* ... */ }
}

internal sealed class AssetEntry
{
    public int Id { get; }
    public string Path { get; }
    public object? Asset { get; set; }
    public Type AssetType { get; }
    public AssetState State { get; set; }
    public int RefCount { get; private set; }
    public DateTime LastAccess { get; private set; }
    public long SizeBytes { get; set; }

    public void AddRef() { RefCount++; LastAccess = DateTime.UtcNow; }
    public bool Release() { RefCount--; return RefCount <= 0; }
}
```

#### 7. Hot Reload (Development Mode)

```csharp
public sealed class ReloadManager : IDisposable
{
    private readonly FileSystemWatcher watcher;
    private readonly AssetManager manager;
    private readonly ConcurrentDictionary<string, DateTime> pendingReloads;
    private readonly TimeSpan debounceDelay = TimeSpan.FromMilliseconds(100);

    public event Action<string>? OnAssetReloaded;

    public ReloadManager(string rootPath, AssetManager manager)
    {
        watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid changes
        pendingReloads[e.FullPath] = DateTime.UtcNow;
        await Task.Delay(debounceDelay);

        if (pendingReloads.TryRemove(e.FullPath, out _) &&
            manager.IsLoaded(e.FullPath))
        {
            await manager.ReloadAsync(e.FullPath);
            OnAssetReloaded?.Invoke(e.FullPath);
        }
    }
}
```

### Project Structure

```
src/KeenEyes.Assets/
├── KeenEyes.Assets.csproj
├── AssetsPlugin.cs                    # IWorldPlugin implementation
├── AssetsConfig.cs                    # Configuration record
│
├── Core/
│   ├── AssetManager.cs                # Central facade
│   ├── AssetHandle.cs                 # AssetHandle<T> struct
│   ├── AssetRef.cs                    # AssetRef<T> component
│   ├── AssetState.cs                  # State enum
│   └── AssetMetadata.cs               # Metadata record
│
├── Loading/
│   ├── IAssetLoader.cs                # Loader interface
│   ├── AssetLoadContext.cs            # Context passed to loaders
│   ├── LoadPriority.cs                # Priority enum
│   └── LoaderRegistry.cs              # Extension → Loader mapping
│
├── Caching/
│   ├── AssetCache.cs                  # Central cache
│   ├── AssetEntry.cs                  # Cache entry
│   ├── CachePolicy.cs                 # Policy enum
│   └── CacheStats.cs                  # Statistics record
│
├── Streaming/
│   ├── StreamingManager.cs            # Async load queue
│   └── StreamingConfig.cs             # Configuration
│
├── Loaders/
│   ├── TextureLoader.cs               # PNG, JPG via StbImageSharp
│   ├── AudioClipLoader.cs             # WAV, OGG via NVorbis
│   ├── MeshLoader.cs                  # GLTF via SharpGLTF
│   ├── JsonLoader.cs                  # JSON via System.Text.Json
│   └── RawLoader.cs                   # Raw binary
│
├── Assets/
│   ├── TextureAsset.cs                # Wrapper for TextureHandle
│   ├── AudioClipAsset.cs              # Wrapper for AudioClipHandle
│   ├── MeshAsset.cs                   # Contains vertex/index data
│   └── RawAsset.cs                    # Raw byte array
│
├── HotReload/
│   ├── ReloadManager.cs               # FileSystemWatcher wrapper
│   └── ReloadConfig.cs                # Configuration
│
└── Systems/
    ├── AssetResolutionSystem.cs       # Resolves AssetRef<T> → handles
    └── AssetUploadSystem.cs           # Processes pending GPU uploads
```

### Dependencies

**Required:**
- `KeenEyes.Abstractions` (IWorldPlugin, IComponent, etc.)
- `KeenEyes.Core` (World, System, Query)
- `StbImageSharp` (pure C# image loading)
- `SharpGLTF.Core` (pure C# glTF loading)
- `NVorbis` (pure C# Ogg Vorbis decoding)

**Optional (for built-in loaders):**
- `KeenEyes.Graphics.Abstractions` (IGraphicsContext for TextureLoader)
- `KeenEyes.Audio.Abstractions` (IAudioContext for AudioClipLoader)

### Plugin Integration

```csharp
public sealed class AssetsPlugin : IWorldPlugin
{
    private readonly AssetsConfig config;
    private AssetManager? assetManager;
    private ReloadManager? reloadManager;

    public string Name => "Assets";

    public AssetsPlugin(AssetsConfig? config = null)
    {
        this.config = config ?? new AssetsConfig();
    }

    public void Install(IPluginContext context)
    {
        // Create asset manager
        assetManager = new AssetManager(config);

        // Register built-in loaders if dependencies available
        if (context.TryGetExtension<IGraphicsContext>(out var graphics))
        {
            assetManager.RegisterLoader(new TextureLoader(graphics));
        }

        if (context.TryGetExtension<IAudioContext>(out var audio))
        {
            assetManager.RegisterLoader(new AudioClipLoader(audio));
        }

        // Always register these (no external dependencies)
        assetManager.RegisterLoader(new MeshLoader());
        assetManager.RegisterLoader(new JsonLoader<object>());
        assetManager.RegisterLoader(new RawLoader());

        // Register as extension
        context.SetExtension(assetManager);

        // Register component types
        context.RegisterComponent<AssetRef<TextureAsset>>();
        context.RegisterComponent<AssetRef<AudioClipAsset>>();
        context.RegisterComponent<AssetRef<MeshAsset>>();

        // Register systems
        context.AddSystem<AssetResolutionSystem>(SystemPhase.PreUpdate, order: -100);
        context.AddSystem<AssetUploadSystem>(SystemPhase.PreUpdate, order: -99);

        // Hot reload (dev mode only)
        if (config.EnableHotReload)
        {
            reloadManager = new ReloadManager(config.RootPath, assetManager);
        }
    }

    public void Uninstall(IPluginContext context)
    {
        reloadManager?.Dispose();
        context.RemoveExtension<AssetManager>();
        assetManager?.Dispose();
    }
}
```

### Usage Examples

**Basic Loading:**
```csharp
var assets = world.GetExtension<AssetManager>();

// Synchronous load (blocks)
using var texture = assets.Load<TextureAsset>("textures/player.png");
graphics.DrawSprite(texture.Asset!.Handle, position);

// Async load (non-blocking)
var textureHandle = await assets.LoadAsync<TextureAsset>("textures/enemy.png");
// Use later when loaded
```

**ECS Integration:**
```csharp
// Create entity with asset reference (path-based, for serialization)
world.Spawn()
    .With(new AssetRef<TextureAsset> { Path = "textures/player.png" })
    .With(new Transform2D { Position = new Vector2(100, 100) })
    .WithTag<PlayerTag>()
    .Build();

// AssetResolutionSystem automatically loads and resolves
// Render system checks if resolved before drawing
```

**Custom Loader:**
```csharp
public class TiledMapLoader : IAssetLoader<TiledMapAsset>
{
    public IReadOnlyList<string> Extensions => [".tmx", ".tmj"];

    public TiledMapAsset Load(Stream stream, AssetLoadContext context)
    {
        // Parse Tiled map format
        var json = JsonDocument.Parse(stream);
        return new TiledMapAsset(json);
    }

    public async Task<TiledMapAsset> LoadAsync(
        Stream stream, AssetLoadContext context, CancellationToken ct)
    {
        var json = await JsonDocument.ParseAsync(stream, default, ct);
        return new TiledMapAsset(json);
    }
}

// Register custom loader
assets.RegisterLoader(new TiledMapLoader());
```

## Alternatives Considered

### 1. Integrate into Existing Subsystems

Instead of a unified `KeenEyes.Assets`, each subsystem (Graphics, Audio) could manage its own caching.

**Rejected because:**
- Duplicated caching logic
- No cross-subsystem asset dependencies (e.g., model loading textures)
- No unified async loading

### 2. Static Asset Registry

A global static registry for assets.

**Rejected because:**
- Violates "no static state" principle
- Can't have multiple isolated asset contexts
- Testing becomes difficult

### 3. Use Existing Library (e.g., Veldrid's asset loading)

Adopt an existing asset management library.

**Rejected because:**
- Most are tightly coupled to specific renderers
- Don't integrate with ECS patterns
- Would add large dependencies

## Consequences

### Positive

- **Unified API** - One way to load all asset types
- **Automatic caching** - No duplicate loads
- **Memory management** - Reference counting prevents leaks
- **Async loading** - No frame hitches
- **Dev experience** - Hot reload speeds iteration
- **Extensibility** - Custom loaders for game-specific formats
- **ECS integration** - AssetRef<T> components work with queries

### Negative

- **Additional dependency** - Games need to install AssetsPlugin
- **Indirection** - Extra layer between game and subsystems
- **Memory overhead** - Cache metadata per asset
- **Complexity** - Reference counting requires discipline (dispose handles)

### Neutral

- Built-in loaders require corresponding subsystem plugins to be installed first
- Hot reload only works with file-based assets (not embedded resources)

## Implementation Plan

### Phase 1: Core Infrastructure
1. Create project structure
2. Implement `AssetHandle<T>`, `AssetRef<T>`, `AssetState`
3. Implement `AssetCache` with reference counting
4. Implement `AssetManager` facade
5. Implement `LoaderRegistry`
6. Implement `IAssetLoader<T>` interface

### Phase 2: Built-in Loaders
1. `RawLoader` (simplest, no dependencies)
2. `JsonLoader<T>` (System.Text.Json)
3. `TextureLoader` (StbImageSharp + IGraphicsContext)
4. `AudioClipLoader` (NVorbis + IAudioContext)
5. `MeshLoader` (SharpGLTF)

### Phase 3: Async & Streaming
1. Implement `StreamingManager`
2. Implement priority queue
3. Implement `AssetUploadSystem` for GPU uploads
4. Add progress callbacks

### Phase 4: ECS Integration
1. Implement `AssetResolutionSystem`
2. Register component types
3. Create `AssetsPlugin`

### Phase 5: Hot Reload
1. Implement `ReloadManager`
2. Add file watching
3. Implement reload callbacks

### Phase 6: Polish
1. Cache policies (LRU eviction)
2. Cache statistics
3. Sample application
4. Documentation

## References

- Issue #428: Epic: Asset Management
- Issue #429: Create KeenEyes.Assets project
- docs/research/asset-loading.md: Library evaluation research
- ADR-007: Capability-based Plugin Architecture
