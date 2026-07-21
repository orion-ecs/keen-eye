# Asset Management

The `KeenEyes.Assets` library provides loading, caching, and lifecycle management for game resources such as textures, audio clips, meshes, and fonts. It integrates with a `World` as a plugin, so asset resolution can be driven directly from ECS components.

## Overview

`KeenEyes.Assets` is built around a few core pieces:

- **`AssetManager`** - the central API for loading, caching, and unloading assets. Assets are identified by file path and loaded through registered `IAssetLoader<T>` implementations.
- **`AssetHandle<T>`** - a type-safe, reference-counted handle to a loaded asset, returned from `AssetManager.Load<T>`/`LoadAsync<T>`.
- **`AssetRef<T>`** - a serializable struct for use inside components, holding just a path. The `AssetResolutionSystem` resolves it to a loaded asset automatically.
- **`AssetsPlugin`** - the `IWorldPlugin` that wires an `AssetManager` and the resolution system into a `World`, auto-registering built-in loaders based on what other plugins (graphics, audio) are installed.

Assets are cached by path, reference-counted, and unloaded according to a configurable `CachePolicy`. Loading can happen synchronously (`Load<T>`) or asynchronously (`LoadAsync<T>`) without blocking the calling thread.

## Quick Start

### Installation

Install `AssetsPlugin` on a `World`. If graphics or audio plugins are installed first, `AssetsPlugin` automatically registers the loaders that depend on them:

```csharp
using KeenEyes.Assets;

using var world = new World();

// Install graphics and audio first (optional - enables texture/audio loaders)
world.InstallPlugin(new SilkGraphicsPlugin(graphicsConfig));
world.InstallPlugin(new SilkAudioPlugin());

// Install assets plugin
world.InstallPlugin(new AssetsPlugin(new AssetsConfig
{
    RootPath = "Assets",
    EnableHotReload = true // Development mode
}));

// Now load assets
var assets = world.GetExtension<AssetManager>();
using var texture = assets.Load<TextureAsset>("textures/player.png");
```

During `Install`, `AssetsPlugin`:

- Creates an `AssetManager` from the supplied `AssetsConfig` (or `AssetsConfig.Default` if none is passed) and registers it as a world extension (`context.SetExtension(assetManager)`), retrievable via `world.GetExtension<AssetManager>()`.
- Registers built-in loaders based on available dependencies:
  - `TextureLoader`, `DdsTextureLoader`, `SpriteAtlasLoader`, `AnimationLoader` - if an `IGraphicsContext` extension is present.
  - `FontLoader` - if the graphics context also implements `IFontManagerProvider`.
  - `AudioClipLoader` - if an `IAudioContext` extension is present.
  - `MeshLoader` and `RawLoader` - always, since they have no external dependencies.
- Registers `AssetResolutionSystem` in the `SystemPhase.EarlyUpdate` phase with `order: -100`.
- If `AssetsConfig.EnableHotReload` is `true` and `AssetsConfig.RootPath` exists, creates a `ReloadManager` that watches the root path for file changes.

On `Uninstall`, the plugin disposes the `ReloadManager` (if any), removes the `AssetManager` extension, and disposes the `AssetManager` (which unloads all cached assets).

### Configuration

`AssetsConfig` is a record with the following settings:

| Property | Default | Description |
|---|---|---|
| `RootPath` | `"Assets"` | Root directory that all asset paths are relative to. |
| `MaxCacheBytes` | 512 MB | Cache size at which eviction may occur, subject to `CachePolicy`. |
| `CachePolicy` | `CachePolicy.LRU` | Eviction strategy - see below. |
| `MaxConcurrentLoads` | `4` | Maximum number of concurrent async load operations. |
| `EnableHotReload` | `false` | Watches `RootPath` for changes and reloads modified assets. Development only. |
| `DefaultPriority` | `LoadPriority.Normal` | Default priority for async loads. |
| `Services` | `null` | Optional `IServiceProvider` passed to loaders via `AssetLoadContext`. |
| `OnLoadError` | `null` | Callback invoked with `(path, exception)` when an async load triggered by `AssetResolutionSystem` fails. |

`AssetsConfig.Default` returns production-friendly defaults; `AssetsConfig.Development` enables hot reload and switches to `CachePolicy.Aggressive`.

```csharp
var config = new AssetsConfig
{
    RootPath = "Assets",
    MaxCacheBytes = 1024 * 1024 * 1024, // 1 GB
    CachePolicy = CachePolicy.LRU,
    EnableHotReload = true
};

world.InstallPlugin(new AssetsPlugin(config));
```

## Loading Assets Directly

`AssetManager` exposes both synchronous and asynchronous loading. Both return an `AssetHandle<T>`, which is `IDisposable` and reference-counted:

```csharp
var manager = world.GetExtension<AssetManager>();

// Synchronous load - blocks until the file is read and parsed
using var texture = manager.Load<TextureAsset>("textures/player.png");

// Asynchronous load - does not block the calling thread
using var clip = await manager.LoadAsync<AudioClipAsset>(
    "audio/music.ogg",
    priority: LoadPriority.Normal);

if (texture.IsLoaded)
{
    var asset = texture.Asset!; // TextureAsset
}
```

Key `AssetHandle<T>` members:

- `IsValid` - refers to a tracked asset (may still be loading).
- `State` - the current `AssetState` (`Invalid`, `Pending`, `Loading`, `Loaded`, `Failed`, `Unloaded`).
- `IsLoaded`, `IsLoading`, `IsFailed` - convenience checks over `State`.
- `Asset` - the loaded `T`, or `null` if not yet loaded.
- `Acquire()` - takes an additional reference, returning a new handle that must also be disposed.
- `Dispose()` - releases this handle's reference; the asset becomes eligible for eviction once all references are released.

Repeated calls to `Load<T>`/`LoadAsync<T>` for the same path return handles to the same cached entry rather than reloading from disk.

Requesting the same asset with two different type parameters (e.g. `Load<TextureAsset>` and `Load<RawAsset>` for the same path) is not supported - each cache entry is keyed by path and a single asset type.

### Built-in Asset Types

The loaders registered by `AssetsPlugin` produce these asset types, all `sealed class`, all `IDisposable`:

- `TextureAsset` - wraps a `TextureHandle` plus `Width`, `Height`, `Format` (`TextureFormat`), and estimated `SizeBytes`.
- `AudioClipAsset` - wraps an `AudioClipHandle` plus `Duration`, `Channels`, `SampleRate`, `BitsPerSample`.
- `MeshAsset` - holds `Vertices` (`MeshVertex[]`), `Indices` (`uint[]`), `Submeshes`, and bounds (`BoundsMin`/`BoundsMax`). Use `MeshAsset.Create(name, vertices, indices)` to build one with computed bounds.
- `RawAsset` - unprocessed file bytes, exposed via `Data`, `AsSpan()`, `AsMemory()`, and `CreateStream()`.

Other asset types exist for specialized loaders (`FontAsset`, `SpriteAtlasAsset`, `AnimationAsset`, `SkeletonAsset`, `SkeletalAnimationAsset`, `ModelAsset`) - see the `KeenEyes.Assets.Assets` namespace for details.

### Custom Loaders

Add support for a new asset type by implementing `IAssetLoader<T>` and registering it with `AssetManager.RegisterLoader<T>`:

```csharp
public sealed class MyFormatLoader : IAssetLoader<MyAsset>
{
    public IReadOnlyList<string> Extensions => [".myf"];

    public MyAsset Load(Stream stream, AssetLoadContext context)
    {
        // Parse the stream and return the asset
        return new MyAsset(stream);
    }

    public async Task<MyAsset> LoadAsync(
        Stream stream, AssetLoadContext context, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Load(stream, context), cancellationToken);
    }
}

manager.RegisterLoader(new MyFormatLoader());
```

`AssetLoadContext` (a `readonly record struct`) carries `Path`, the owning `Manager`, and the optional `Services` provider configured on `AssetsConfig`. Loaders that need to load a dependent asset (e.g. a model referencing textures) can call `AssetManager.LoadDependency<T>(parentPath, dependencyPath)` or its async counterpart; the dependency's reference count is then tied to the parent and released automatically when the parent is released.

## Resolving Assets from Components with `AssetRef<T>`

`AssetRef<T>` is an `IComponent` that stores just a path, suitable for serializing with entity data. Add it as a component field and let `AssetResolutionSystem` resolve it:

```csharp
[Component]
public partial struct SpriteRenderer
{
    public AssetRef<TextureAsset> Texture;
    public Vector4 Color;
}

world.Spawn()
    .With(new SpriteRenderer
    {
        Texture = AssetRef<TextureAsset>.FromPath("textures/player.png"),
        Color = Vector4.One
    })
    .Build();
```

`AssetResolutionSystem` runs every `EarlyUpdate` (registered at `order: -100` by `AssetsPlugin`) and, for every entity with an unresolved `AssetRef<T>`, starts an async load at `LoadPriority.Normal`. It currently resolves `AssetRef<TextureAsset>`, `AssetRef<AudioClipAsset>`, `AssetRef<MeshAsset>`, and `AssetRef<RawAsset>` components each tick. Check `AssetRef<T>.IsResolved` (true once `HandleId` is set) before relying on the referenced asset being available. If a load fails, the `AssetsConfig.OnLoadError` callback (if configured) is invoked with the path and exception. Call `Invalidate()` on the ref after changing its `Path` to force re-resolution.

## Caching and Eviction

`AssetManager.CachePolicy` controls eviction behavior, all subject to reference counting - assets with active `AssetHandle<T>` references are never evicted:

- `CachePolicy.LRU` (default) - evicts zero-reference assets by least-recently-used order once the cache exceeds `MaxCacheBytes`.
- `CachePolicy.Manual` - never auto-evicts; call `Unload(path)` or `UnloadAll()` explicitly.
- `CachePolicy.Aggressive` - unloads an asset as soon as its reference count reaches zero; no caching benefit, useful for development/memory-constrained scenarios.

```csharp
var stats = manager.GetCacheStats(); // CacheStats
Console.WriteLine($"{stats.LoadedAssets}/{stats.TotalAssets} loaded, hit ratio {stats.HitRatio:P0}");

manager.TrimCache(targetBytes: 256 * 1024 * 1024);
```

`CacheStats` reports `TotalAssets`, `LoadedAssets`, `PendingAssets`, `FailedAssets`, `TotalSizeBytes`, `MaxSizeBytes`, `CacheHits`, `CacheMisses`, plus computed `HitRatio` and `UtilizationRatio`.

## Streaming and Hot Reload

`StreamingManager` preloads a batch of assets in the background at `LoadPriority.Streaming`, useful for level transitions:

```csharp
var streaming = new StreamingManager(manager);

streaming.Queue<TextureAsset>("levels/forest/ground.png");
streaming.Queue<MeshAsset>("levels/forest/trees.glb");
streaming.Start();

await streaming.WaitForCompletionAsync();
```

`Progress` reports completion as a `0`-`1` float, and `OnAssetStreamed`/`OnStreamingComplete`/`OnStreamingError` events report per-asset and overall status.

When `AssetsConfig.EnableHotReload` is `true`, `AssetsPlugin` creates a `ReloadManager` that watches `RootPath` (via `FileSystemWatcher`) and calls `AssetManager.ReloadAsync(path)` for loaded assets when their backing file changes, after a short debounce. This works for any type with a registered loader, without additional configuration. Hot reload is intended for development only.

## Asset Manifests

`AssetManifest` describes build-time asset metadata (path, type, size, hash, dependencies) without needing filesystem scanning at runtime - useful for preloading and validating packaged builds:

```csharp
var manifest = AssetManifest.Load("Assets/manifest.json");

if (manifest.Exists("textures/player.png"))
{
    var info = manifest.GetInfo("textures/player.png"); // AssetInfo?
}
```

Build one programmatically with `AssetManifest.CreateBuilder()` (returns an `AssetManifestBuilder`), chaining `AddAsset(path, type, size, hash, dependencies)` calls and finishing with `Build()`.

## Performance

- Prefer `LoadAsync<T>` over `Load<T>` on hot paths (e.g. gameplay code running each frame) - `Load<T>` blocks the calling thread until the file is read and parsed.
- Dispose every `AssetHandle<T>` you acquire. Cache policies other than `Manual` can only evict assets with zero active references, so undisposed handles keep assets resident indefinitely.
- `AssetsConfig.MaxConcurrentLoads` bounds concurrent disk/parse work; raise it cautiously since higher values use more threads and I/O bandwidth simultaneously.
- Use `CachePolicy.Aggressive` only for development or memory-constrained targets - it reloads from disk on every access with no caching benefit.

## Next Steps

- [Plugins Guide](plugins.md) - how `IWorldPlugin` installation and extensions work in general
- [Systems Guide](systems.md) - system phases and ordering, as used by `AssetResolutionSystem`
- [Components Guide](components.md) - defining components like `AssetRef<T>` fields with `[Component]`
- [Asset Management Design](research/asset-management.md) - original design document (aspirational; verify details against source before relying on them)
- [Asset Loading Design](research/asset-loading.md) - original loading pipeline design document
