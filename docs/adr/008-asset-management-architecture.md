# ADR-008: Asset Management Architecture

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2025-12-21 · **Last amended:** 2026-07-26
**Relates to:** [ADR-007](007-capability-based-plugin-architecture.md) (plugin capabilities) · [#428](https://github.com/orion-ecs/keen-eye/issues/428) · [#429](https://github.com/orion-ecs/keen-eye/issues/429)

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
│  │  (facade)      │  │ (batch stream) │  │ (dev mode)     │             │
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
public struct AssetRef<T> : IComponent, IEquatable<AssetRef<T>>
    where T : class, IDisposable
{
    /// <summary>Path to the asset for serialization.</summary>
    public string Path;

    /// <summary>Runtime handle (set by AssetResolutionSystem).</summary>
    internal int HandleId;

    public readonly bool IsResolved => HandleId > 0;
    public readonly bool HasPath => !string.IsNullOrEmpty(Path);

    public static AssetRef<T> FromPath(string path) => new() { Path = path };

    /// <summary>Clears the resolved handle, forcing re-resolution.</summary>
    public void Invalidate() => HandleId = 0;
}
```

`AssetRef<T>` is a plain struct implementing `IComponent` — the `[Component]` source generator does not apply to generic types, so no fluent builder methods are generated for it.

**Usage in ECS:**
```csharp
// In entity definition
world.Spawn()
    .With(new AssetRef<TextureAsset> { Path = "textures/player.png" })
    .With(new SpriteRenderer { /* ... */ })
    .Build();

// AssetResolutionSystem automatically resolves paths to handles
```

`AssetResolutionSystem` resolves the four built-in instantiations — `AssetRef<TextureAsset>`, `AssetRef<AudioClipAsset>`, `AssetRef<MeshAsset>`, and `AssetRef<RawAsset>` — rather than resolving generically over all `AssetRef<T>` types. Custom asset types are loaded through `AssetManager` directly.

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
| `TextureLoader` | `TextureAsset` | .png, .jpg/.jpeg, .bmp, .tga, .gif, .psd, .hdr | StbImageSharp, IGraphicsContext |
| `DdsTextureLoader` | `TextureAsset` | .dds | Pfim, IGraphicsContext |
| `SpriteAtlasLoader` | `SpriteAtlasAsset` | .atlas, .json | IGraphicsContext |
| `AnimationLoader` | `AnimationAsset` | .keanim | IGraphicsContext |
| `FontLoader` | `FontAsset` | .ttf, .otf | IFontManagerProvider |
| `AudioClipLoader` | `AudioClipAsset` | .wav, .ogg, .mp3, .flac | NVorbis, NLayer, built-in FLAC decoder, IAudioContext |
| `MeshLoader` | `MeshAsset` | .gltf, .glb | SharpGLTF |
| `ModelLoader` | `ModelAsset` | .gltf, .glb | SharpGLTF |
| `SkeletalAnimationLoader` | `SkeletalAnimationAsset` | .gltf, .glb | SharpGLTF |
| `RawLoader` | `RawAsset` | .bin, .dat, .raw, .bytes | None |

The originally planned `JsonLoader<T>` (System.Text.Json) was not implemented; JSON-backed formats are handled by dedicated loaders (sprite atlases, animations) instead.

#### 5. Async Loading and Batch Streaming

```csharp
public enum LoadPriority
{
    Immediate = 0,  // Block until loaded (avoid in production)
    High = 1,       // Next in queue
    Normal = 2,     // Standard priority
    Low = 3,        // Background loading
    Streaming = 4   // Lowest priority, for level streaming
}
```

Async loading is exposed directly on the `AssetManager` facade:

```csharp
public async Task<AssetHandle<T>> LoadAsync<T>(
    string path,
    LoadPriority priority = LoadPriority.Normal,
    CancellationToken cancellationToken = default) where T : class, IDisposable;
```

The `LoadPriority` enum is part of the API, but requests are **not** scheduled through a priority queue — the originally sketched `PriorityQueue<LoadRequest, LoadPriority>` scheduler was not implemented. Instead, `StreamingManager` is a batch streaming utility for level loads: queue paths with `Queue<T>(path)` / `QueueMany<T>(paths)`, run them with a bounded-concurrency worker (`Start(maxConcurrent)`, capped via `SemaphoreSlim`), track `Progress` / `QueuedCount` / `IsStreaming`, subscribe to `OnAssetStreamed` / `OnStreamingComplete` / `OnStreamingError`, and await `WaitForCompletionAsync()`. All streaming loads run at `LoadPriority.Streaming`.

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

The debounce delay is a constructor parameter (default 100 ms); there is no separate `ReloadConfig` type.

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
│   └── AssetState.cs                  # State enum
│
├── Loading/
│   ├── IAssetLoader.cs                # Loader interface
│   ├── AssetLoadContext.cs            # Context passed to loaders
│   ├── AssetLoadException.cs          # Load failure exception
│   ├── LoadPriority.cs                # Priority enum
│   └── LoaderRegistry.cs              # Extension → Loader mapping
│
├── Caching/
│   ├── AssetCache.cs                  # Central cache
│   ├── AssetEntry.cs                  # Cache entry
│   ├── CachePolicy.cs                 # Policy enum (LRU, Manual, Aggressive)
│   └── CacheStats.cs                  # Statistics record
│
├── Streaming/
│   └── StreamingManager.cs            # Batch level streaming
│
├── Loaders/
│   ├── TextureLoader.cs               # PNG, JPG, BMP, TGA, ... via StbImageSharp
│   ├── DdsTextureLoader.cs            # DDS via Pfim
│   ├── SpriteAtlasLoader.cs           # Sprite atlas JSON
│   ├── AnimationLoader.cs             # Sprite animation (.keanim)
│   ├── SkeletalAnimationLoader.cs     # Skeletal animation via SharpGLTF
│   ├── FontLoader.cs                  # TTF/OTF via IFontManagerProvider
│   ├── AudioClipLoader.cs             # WAV, OGG, MP3, FLAC (NVorbis, NLayer)
│   ├── FlacDecoder.cs                 # Built-in FLAC decoding
│   ├── MeshLoader.cs                  # GLTF via SharpGLTF
│   ├── ModelLoader.cs                 # Full model import via SharpGLTF
│   └── RawLoader.cs                   # Raw binary
│
├── Assets/                            # Wrapper asset types
│   ├── TextureAsset.cs, TextureData.cs
│   ├── AudioClipAsset.cs
│   ├── MeshAsset.cs, ModelAsset.cs, MaterialData.cs
│   ├── FontAsset.cs
│   ├── SpriteAtlasAsset.cs
│   ├── AnimationAsset.cs, SkeletonAsset.cs, SkeletalAnimationAsset.cs
│   └── RawAsset.cs
│
├── Data/                              # Atlas & animation JSON models
│   ├── AtlasJsonModels.cs, AtlasJsonContext.cs
│   ├── AnimationJsonModels.cs
│   └── SpriteRegion.cs
│
├── Manifest/                          # Asset manifest support
│   ├── AssetManifest.cs
│   ├── AssetManifestBuilder.cs
│   ├── AssetInfo.cs
│   └── ManifestStatistics.cs
│
├── HotReload/
│   └── ReloadManager.cs               # FileSystemWatcher wrapper
│
└── Systems/
    └── AssetResolutionSystem.cs       # Resolves AssetRef<T> → handles
```

Originally planned but not built: `Core/AssetMetadata.cs`, `Streaming/StreamingConfig.cs`, `HotReload/ReloadConfig.cs`, `Loaders/JsonLoader.cs`, and `Systems/AssetUploadSystem.cs`.

### Dependencies

**Project references (all unconditional):**
- `KeenEyes.Abstractions` (IWorldPlugin, IComponent, etc.)
- `KeenEyes.Core` (World, System, Query)
- `KeenEyes.Common` (shared utilities)
- `KeenEyes.Animation` (animation asset types)
- `KeenEyes.Graphics.Abstractions` (IGraphicsContext for texture/atlas/font loaders)
- `KeenEyes.Audio.Abstractions` (IAudioContext for AudioClipLoader)

**Packages:**
- `StbImageSharp` (pure C# image loading)
- `SharpGLTF.Core` (pure C# glTF loading)
- `NVorbis` (pure C# Ogg Vorbis decoding)
- `NLayer` (pure C# MP3 decoding)
- `Pfim` (DDS texture loading)

The Graphics and Audio abstractions were originally sketched as optional dependencies; as built they are hard compile-time references. What remains conditional is loader *registration* at runtime — `AssetsPlugin` only registers the graphics- and audio-backed loaders when the corresponding subsystem extensions are present in the world.

### Plugin Integration

```csharp
public sealed class AssetsPlugin(AssetsConfig? config = null) : IWorldPlugin
{
    private readonly AssetsConfig resolvedConfig = config ?? AssetsConfig.Default;
    private AssetManager? assetManager;
    private ReloadManager? reloadManager;

    public string Name => "Assets";

    public void Install(IPluginContext context)
    {
        // Create asset manager
        assetManager = new AssetManager(resolvedConfig);

        // Graphics-dependent loaders
        if (context.TryGetExtension<IGraphicsContext>(out var graphics) && graphics != null)
        {
            assetManager.RegisterLoader(new TextureLoader(graphics));
            assetManager.RegisterLoader(new DdsTextureLoader(graphics));
            assetManager.RegisterLoader(new SpriteAtlasLoader());
            assetManager.RegisterLoader(new AnimationLoader());

            // FontLoader requires an IFontManager (from IFontManagerProvider)
            if (graphics is IFontManagerProvider fontProvider &&
                fontProvider.GetFontManager() is { } fontManager)
            {
                assetManager.RegisterLoader(new FontLoader(fontManager));
            }
        }

        // Audio-dependent loader
        if (context.TryGetExtension<IAudioContext>(out var audio) && audio != null)
        {
            assetManager.RegisterLoader(new AudioClipLoader(audio));
        }

        // Always register these (no external dependencies)
        assetManager.RegisterLoader(new MeshLoader());
        assetManager.RegisterLoader(new RawLoader());

        // Register as extension
        context.SetExtension(assetManager);

        // Register the asset resolution system
        context.AddSystem<AssetResolutionSystem>(SystemPhase.EarlyUpdate, order: -100);

        // Hot reload requires the asset root to exist
        if (resolvedConfig.EnableHotReload && Directory.Exists(resolvedConfig.RootPath))
        {
            reloadManager = new ReloadManager(resolvedConfig.RootPath, assetManager);
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

Differences from the original sketch: no explicit `RegisterComponent<AssetRef<...>>` calls are needed, there is no `AssetUploadSystem` (GPU upload happens inside the graphics-backed loaders), and the single `AssetResolutionSystem` runs in `SystemPhase.EarlyUpdate` (order -100) rather than `PreUpdate`.

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
- **Async loading** - Task-based `LoadAsync` and `StreamingManager` batch streaming keep loads off the frame (loads are concurrency-capped, not priority-scheduled)
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

## Implementation Status

The system shipped 2025-12-21 in commit `f3ab185d` — the same commit that added this ADR. Status of the original plan:

### Phase 1: Core Infrastructure ✅
1. ✅ Project structure
2. ✅ `AssetHandle<T>`, `AssetRef<T>`, `AssetState`
3. ✅ `AssetCache` with reference counting
4. ✅ `AssetManager` facade
5. ✅ `LoaderRegistry`
6. ✅ `IAssetLoader<T>` interface

### Phase 2: Built-in Loaders ✅ (except JsonLoader)
1. ✅ `RawLoader` (simplest, no dependencies)
2. Not implemented: `JsonLoader<T>` (System.Text.Json)
3. ✅ `TextureLoader` (StbImageSharp + IGraphicsContext) — plus `DdsTextureLoader`, `SpriteAtlasLoader`, `AnimationLoader`, and `FontLoader` beyond the plan
4. ✅ `AudioClipLoader` (NVorbis + IAudioContext) — plus MP3 via NLayer and built-in FLAC decoding
5. ✅ `MeshLoader` (SharpGLTF) — plus `ModelLoader` and `SkeletalAnimationLoader`

### Phase 3: Async & Streaming — partial
1. ✅ `StreamingManager` (as a batch level-streaming helper)
2. Not implemented: priority-queue load scheduling (`LoadPriority` exists as an API parameter only)
3. Not implemented: `AssetUploadSystem` for GPU uploads (uploads happen inside graphics-backed loaders)
4. ✅ Progress callbacks (`StreamingManager.Progress` / `OnAssetStreamed`)

### Phase 4: ECS Integration ✅
1. ✅ `AssetResolutionSystem` (resolves the four built-in `AssetRef<T>` instantiations)
2. Component-type registration — not needed as built (no `RegisterComponent` calls)
3. ✅ `AssetsPlugin`

### Phase 5: Hot Reload ✅
1. ✅ `ReloadManager`
2. ✅ File watching (FileSystemWatcher, debounced)
3. ✅ Reload callbacks (`OnAssetReloaded`)

### Phase 6: Polish — partial
1. ✅ Cache policies (LRU, Manual, Aggressive)
2. ✅ Cache statistics (`CacheStats`, `GetCacheStats`)
3. Not implemented: sample application (no sample currently uses `AssetsPlugin`/`AssetManager`)
4. ✅ Documentation ([docs/assets.md](../assets.md))

## References

- [#428](https://github.com/orion-ecs/keen-eye/issues/428): Epic: Asset Management
- [#429](https://github.com/orion-ecs/keen-eye/issues/429): Create KeenEyes.Assets project
- [asset-loading research](../research/asset-loading.md): Library evaluation research
- [ADR-007: Capability-based Plugin Architecture](./007-capability-based-plugin-architecture.md)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted (implemented in the same commit that added the ADR; header date corrected 2024 → 2025-12-21). Implementation marked Partial — not built: `JsonLoader<T>`, `AssetUploadSystem`, priority-queue load scheduling, and a sample application. Body amended to the as-built code: expanded loader table (DDS/atlas/animation/font/model/skeletal loaders, MP3/FLAC audio), `AssetRef<T>` as a plain `IComponent` struct resolved for four hard-coded instantiations, async loading on `AssetManager.LoadAsync` with `StreamingManager` as a batch streaming helper, plugin wiring (conditional loader registration, `EarlyUpdate` order -100, hot reload gated on the asset root existing), hard Graphics/Audio project references plus NLayer/Pfim packages, and the as-built project tree (Manifest/, Data/, no config classes).
- **v1 — 2025-12-21 (#428/#429, f3ab185d):** Accepted — create KeenEyes.Assets as a unified asset layer (caching, refcounting, async loading, pluggable loaders, hot reload) coordinating with Graphics/Audio; implementation landed in the same commit as the ADR.
