# Asset Management Architecture

This document outlines the architecture for a robust asset management system in KeenEyes, handling loading, caching, and lifecycle management of game assets.

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Design Goals](#design-goals)
3. [Architecture Overview](#architecture-overview)
4. [Asset Types](#asset-types)
5. [Loading Pipeline](#loading-pipeline)
6. [Reference Counting](#reference-counting)
7. [Async Loading](#async-loading)
8. [Implementation Plan](#implementation-plan)

---

## Executive Summary

KeenEyes Assets provides a unified system for managing all game resources:
- **Textures, audio, fonts, shaders** - Binary assets
- **Prefabs, scenes, data files** - Serialized data
- **Reference counting** - Automatic unloading when unused
- **Async loading** - Non-blocking asset loads

**Key Decision:** Assets are referenced by path/ID, loaded on-demand, and automatically unloaded when no longer referenced.

---

## Design Goals

1. **Unified API** - Single interface for all asset types
2. **Async by Default** - Non-blocking loads for smooth gameplay
3. **Reference Counted** - Automatic memory management
4. **Hot Reload** - Development-time asset reloading
5. **AOT Compatible** - No reflection-based loading
6. **Extensible** - Custom asset types via loaders

---

## Architecture Overview

### Project Structure

```
KeenEyes.Assets/
├── KeenEyes.Assets.csproj
├── AssetPlugin.cs                 # IWorldPlugin entry point
│
├── Core/
│   ├── IAssetManager.cs          # Main API interface
│   ├── AssetHandle.cs            # Typed reference to loaded asset
│   ├── AssetId.cs                # Unique asset identifier
│   ├── AssetState.cs             # Loading state enum
│   └── AssetMetadata.cs          # Asset info (size, type, deps)
│
├── Loading/
│   ├── IAssetLoader.cs           # Asset type loader interface
│   ├── AssetLoaderRegistry.cs    # Loader registration
│   ├── LoadRequest.cs            # Async load request
│   └── LoadPriority.cs           # Loading priority levels
│
├── Loaders/
│   ├── TextureLoader.cs          # Image loading
│   ├── AudioLoader.cs            # Audio file loading
│   ├── ShaderLoader.cs           # Shader loading
│   ├── FontLoader.cs             # Font loading
│   ├── JsonLoader.cs             # JSON data loading
│   └── PrefabLoader.cs           # Entity prefab loading
│
├── Caching/
│   ├── AssetCache.cs             # In-memory cache
│   ├── RefCounter.cs             # Reference counting
│   └── CachePolicy.cs            # Eviction policies
│
├── Bundles/
│   ├── AssetBundle.cs            # Grouped assets
│   ├── BundleManifest.cs         # Bundle contents
│   └── BundleLoader.cs           # Bundle loading
│
└── Utilities/
    ├── AssetPath.cs              # Path normalization
    ├── AssetWatcher.cs           # Hot reload watcher
    └── AssetDatabase.cs          # Asset registry
```

---

## Asset Types

### IAsset Interface

```csharp
public interface IAsset : IDisposable
{
    AssetId Id { get; }
    string Path { get; }
    AssetState State { get; }
    long SizeBytes { get; }
    Type AssetType { get; }
}

public enum AssetState
{
    Unloaded,
    Loading,
    Loaded,
    Failed
}

public readonly record struct AssetId(ulong Value)
{
    public static AssetId FromPath(string path)
        => new(XxHash64.HashToUInt64(Encoding.UTF8.GetBytes(path)));
}
```

### Common Asset Types

```csharp
// Texture asset
public sealed class TextureAsset : IAsset
{
    public AssetId Id { get; }
    public string Path { get; }
    public AssetState State { get; internal set; }
    public long SizeBytes => Width * Height * BytesPerPixel;
    public Type AssetType => typeof(TextureAsset);

    public ITexture Texture { get; internal set; }
    public int Width { get; internal set; }
    public int Height { get; internal set; }
    public TextureFormat Format { get; internal set; }
}

// Audio asset
public sealed class AudioAsset : IAsset
{
    public IAudioClip Clip { get; internal set; }
    public float Duration { get; internal set; }
    public int SampleRate { get; internal set; }
    // ... IAsset implementation
}

// Shader asset
public sealed class ShaderAsset : IAsset
{
    public IShader Shader { get; internal set; }
    public string[] Uniforms { get; internal set; }
    // ... IAsset implementation
}

// Data asset (JSON/binary)
public sealed class DataAsset<T> : IAsset where T : class
{
    public T Data { get; internal set; }
    // ... IAsset implementation
}

// Prefab asset
public sealed class PrefabAsset : IAsset
{
    public EntityTemplate Template { get; internal set; }
    public AssetId[] Dependencies { get; internal set; }
    // ... IAsset implementation
}
```

---

## Loading Pipeline

### IAssetLoader Interface

```csharp
public interface IAssetLoader
{
    Type AssetType { get; }
    string[] SupportedExtensions { get; }

    bool CanLoad(string path);
    IAsset Load(string path, IAssetLoadContext context);
    Task<IAsset> LoadAsync(string path, IAssetLoadContext context, CancellationToken ct);
    void Unload(IAsset asset);
}

public interface IAssetLoadContext
{
    IGraphicsBackend? Graphics { get; }
    IAudioContext? Audio { get; }
    IAssetManager Assets { get; }  // For loading dependencies
    Stream OpenRead(string path);
}
```

### TextureLoader Example

```csharp
public class TextureLoader : IAssetLoader
{
    public Type AssetType => typeof(TextureAsset);
    public string[] SupportedExtensions => [".png", ".jpg", ".jpeg", ".bmp", ".tga"];

    public bool CanLoad(string path)
        => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());

    public IAsset Load(string path, IAssetLoadContext context)
    {
        using var stream = context.OpenRead(path);
        var imageData = ImageDecoder.Decode(stream);

        var texture = context.Graphics!.LoadTexture(
            imageData.Data,
            imageData.Format
        );

        return new TextureAsset
        {
            Id = AssetId.FromPath(path),
            Path = path,
            State = AssetState.Loaded,
            Texture = texture,
            Width = imageData.Width,
            Height = imageData.Height,
            Format = imageData.Format
        };
    }

    public async Task<IAsset> LoadAsync(string path, IAssetLoadContext context, CancellationToken ct)
    {
        // Decode on thread pool
        using var stream = context.OpenRead(path);
        var imageData = await Task.Run(() => ImageDecoder.Decode(stream), ct);

        // GPU upload must be on main thread
        var texture = context.Graphics!.LoadTexture(imageData.Data, imageData.Format);

        return new TextureAsset { /* ... */ };
    }

    public void Unload(IAsset asset)
    {
        if (asset is TextureAsset tex)
        {
            tex.Texture?.Dispose();
        }
    }
}
```

### Loader Registry

```csharp
public class AssetLoaderRegistry
{
    private readonly Dictionary<Type, IAssetLoader> loadersByType = new();
    private readonly Dictionary<string, IAssetLoader> loadersByExtension = new();

    public void Register(IAssetLoader loader)
    {
        loadersByType[loader.AssetType] = loader;

        foreach (var ext in loader.SupportedExtensions)
        {
            loadersByExtension[ext.ToLowerInvariant()] = loader;
        }
    }

    public IAssetLoader? GetLoader(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return loadersByExtension.GetValueOrDefault(ext);
    }

    public IAssetLoader? GetLoader<T>() where T : IAsset
        => loadersByType.GetValueOrDefault(typeof(T));
}
```

---

## Reference Counting

### AssetHandle<T>

```csharp
public readonly struct AssetHandle<T> : IDisposable where T : class, IAsset
{
    private readonly AssetManager manager;
    private readonly AssetId id;

    internal AssetHandle(AssetManager manager, AssetId id)
    {
        this.manager = manager;
        this.id = id;
        manager.AddRef(id);
    }

    public T? Asset => manager.GetLoaded<T>(id);
    public bool IsLoaded => Asset?.State == AssetState.Loaded;
    public bool IsLoading => manager.IsLoading(id);

    public void Dispose()
    {
        manager.Release(id);
    }

    // Implicit conversion for convenience
    public static implicit operator T?(AssetHandle<T> handle) => handle.Asset;
}
```

### Reference Counter

```csharp
public class RefCounter
{
    private readonly Dictionary<AssetId, int> refCounts = new();
    private readonly Dictionary<AssetId, IAsset> loadedAssets = new();

    public void AddRef(AssetId id)
    {
        refCounts.TryGetValue(id, out int count);
        refCounts[id] = count + 1;
    }

    public bool Release(AssetId id, out IAsset? asset)
    {
        asset = null;

        if (!refCounts.TryGetValue(id, out int count))
            return false;

        count--;

        if (count <= 0)
        {
            refCounts.Remove(id);
            if (loadedAssets.Remove(id, out asset))
            {
                return true; // Should unload
            }
        }
        else
        {
            refCounts[id] = count;
        }

        return false;
    }

    public int GetRefCount(AssetId id)
        => refCounts.GetValueOrDefault(id, 0);
}
```

### Usage Pattern

```csharp
// Load and hold reference
using var textureHandle = await assets.LoadAsync<TextureAsset>("sprites/player.png");

// Use the asset
renderer.Draw(textureHandle.Asset.Texture, position);

// Reference released when handle disposed
// Asset unloaded if no other references exist
```

---

## IAssetManager API

### Interface Definition

```csharp
public interface IAssetManager
{
    // Synchronous loading
    AssetHandle<T> Load<T>(string path) where T : class, IAsset;

    // Asynchronous loading
    Task<AssetHandle<T>> LoadAsync<T>(string path, LoadPriority priority = LoadPriority.Normal)
        where T : class, IAsset;

    // Batch loading
    Task LoadAllAsync(IEnumerable<string> paths, IProgress<float>? progress = null);

    // Query
    bool IsLoaded(string path);
    bool IsLoading(string path);
    T? GetLoaded<T>(string path) where T : class, IAsset;

    // Management
    void Unload(string path);
    void UnloadUnused();
    void UnloadAll();

    // Statistics
    AssetStatistics GetStatistics();
}

public enum LoadPriority
{
    Low,        // Background loading
    Normal,     // Standard priority
    High,       // Load soon
    Immediate   // Load now, block if needed
}

public readonly record struct AssetStatistics(
    int LoadedCount,
    int LoadingCount,
    long TotalMemoryBytes,
    int CacheHits,
    int CacheMisses
);
```

### Implementation

```csharp
public class AssetManager : IAssetManager, IDisposable
{
    private readonly AssetLoaderRegistry loaders;
    private readonly RefCounter refCounter;
    private readonly AssetCache cache;
    private readonly ConcurrentDictionary<AssetId, Task<IAsset>> pendingLoads = new();

    public AssetHandle<T> Load<T>(string path) where T : class, IAsset
    {
        var id = AssetId.FromPath(path);

        // Check cache
        if (cache.TryGet<T>(id, out var cached))
        {
            return new AssetHandle<T>(this, id);
        }

        // Synchronous load
        var loader = loaders.GetLoader(path)
            ?? throw new InvalidOperationException($"No loader for {path}");

        var asset = loader.Load(path, CreateContext());
        cache.Add(id, asset);

        return new AssetHandle<T>(this, id);
    }

    public async Task<AssetHandle<T>> LoadAsync<T>(string path, LoadPriority priority)
        where T : class, IAsset
    {
        var id = AssetId.FromPath(path);

        // Check cache
        if (cache.TryGet<T>(id, out _))
        {
            return new AssetHandle<T>(this, id);
        }

        // Check pending
        if (pendingLoads.TryGetValue(id, out var pending))
        {
            await pending;
            return new AssetHandle<T>(this, id);
        }

        // Start async load
        var loadTask = LoadAssetAsync(path, id, priority);
        pendingLoads[id] = loadTask;

        try
        {
            await loadTask;
            return new AssetHandle<T>(this, id);
        }
        finally
        {
            pendingLoads.TryRemove(id, out _);
        }
    }

    private async Task<IAsset> LoadAssetAsync(string path, AssetId id, LoadPriority priority)
    {
        var loader = loaders.GetLoader(path)
            ?? throw new InvalidOperationException($"No loader for {path}");

        var asset = await loader.LoadAsync(path, CreateContext(), CancellationToken.None);
        cache.Add(id, asset);

        return asset;
    }
}
```

---

## Async Loading

### Load Queue

```csharp
public class AssetLoadQueue
{
    private readonly PriorityQueue<LoadRequest, int> queue = new();
    private readonly SemaphoreSlim concurrencyLimit;
    private readonly CancellationTokenSource cts = new();

    public AssetLoadQueue(int maxConcurrent = 4)
    {
        concurrencyLimit = new SemaphoreSlim(maxConcurrent);
        StartWorkers();
    }

    public Task<IAsset> Enqueue(string path, LoadPriority priority)
    {
        var request = new LoadRequest(path, priority);
        queue.Enqueue(request, -(int)priority); // Higher priority = lower value
        return request.Completion.Task;
    }

    private async void StartWorkers()
    {
        while (!cts.IsCancellationRequested)
        {
            await concurrencyLimit.WaitAsync(cts.Token);

            if (queue.TryDequeue(out var request, out _))
            {
                _ = ProcessRequest(request);
            }
            else
            {
                concurrencyLimit.Release();
                await Task.Delay(10, cts.Token);
            }
        }
    }

    private async Task ProcessRequest(LoadRequest request)
    {
        try
        {
            var asset = await LoadAssetAsync(request.Path);
            request.Completion.SetResult(asset);
        }
        catch (Exception ex)
        {
            request.Completion.SetException(ex);
        }
        finally
        {
            concurrencyLimit.Release();
        }
    }
}
```

### Progress Reporting

```csharp
public async Task LoadAllAsync(IEnumerable<string> paths, IProgress<float>? progress)
{
    var pathList = paths.ToList();
    int completed = 0;

    var tasks = pathList.Select(async path =>
    {
        await LoadAsync<IAsset>(path);
        var current = Interlocked.Increment(ref completed);
        progress?.Report((float)current / pathList.Count);
    });

    await Task.WhenAll(tasks);
}

// Usage
await assets.LoadAllAsync(levelAssets, new Progress<float>(p =>
{
    loadingBar.Value = p;
    loadingText.Text = $"Loading... {p:P0}";
}));
```

---

## Hot Reload (Development)

```csharp
public class AssetWatcher : IDisposable
{
    private readonly FileSystemWatcher watcher;
    private readonly AssetManager manager;
    private readonly ConcurrentDictionary<string, DateTime> pendingReloads = new();

    public AssetWatcher(string rootPath, AssetManager manager)
    {
        this.manager = manager;

        watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };

        watcher.Changed += OnFileChanged;
        watcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce rapid changes
        pendingReloads[e.FullPath] = DateTime.UtcNow;

        Task.Delay(100).ContinueWith(_ =>
        {
            if (pendingReloads.TryRemove(e.FullPath, out var time)
                && DateTime.UtcNow - time >= TimeSpan.FromMilliseconds(100))
            {
                ReloadAsset(e.FullPath);
            }
        });
    }

    private void ReloadAsset(string fullPath)
    {
        var relativePath = Path.GetRelativePath(watcher.Path, fullPath);

        if (manager.IsLoaded(relativePath))
        {
            Log.Info($"Hot reloading: {relativePath}");
            manager.Reload(relativePath);
        }
    }
}
```

---

## Implementation Plan

### Phase 1: Core Infrastructure

1. Create `KeenEyes.Assets` project
2. Implement AssetId and AssetHandle
3. Implement RefCounter
4. Basic AssetManager with sync loading

**Milestone:** Load textures with reference counting

### Phase 2: Loaders

1. TextureLoader
2. AudioLoader
3. ShaderLoader
4. JsonLoader

**Milestone:** Load common asset types

### Phase 3: Async Loading

1. Implement load queue
2. Add priority system
3. Progress reporting
4. Batch loading

**Milestone:** Non-blocking asset loads

### Phase 4: Advanced Features

1. Asset bundles
2. Dependency tracking
3. Hot reload (dev only)
4. Cache policies

**Milestone:** Production-ready asset system

### Phase 5: Integration

1. Prefab loader integration
2. Scene asset loading
3. Streaming support
4. Memory budgets

---

## Open Questions

1. **Virtual File System** - Support for archives (ZIP, custom)?
2. **Compression** - Automatic decompression on load?
3. **Streaming** - Large asset streaming (terrain, open world)?
4. **Addressables** - Unity-style addressable assets?
5. **Build Pipeline** - Asset processing/optimization on build?
6. **Localization** - Localized asset variants?

---

## Related Issues

- Milestone #19: Asset Management
- Issue #428: Create KeenEyes.Assets with core loading infrastructure
- Issue #429: Implement asset loaders and reference counting
