namespace KeenEyes.Assets;

/// <summary>
/// Central manager for loading, caching, and lifecycle management of game assets.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AssetManager"/> provides a unified API for loading various asset types
/// (textures, audio, meshes, etc.) with automatic caching, reference counting, and
/// async loading support.
/// </para>
/// <para>
/// Assets are identified by file paths and loaded using registered <see cref="IAssetLoader{T}"/>
/// implementations. The manager maintains a cache of loaded assets and tracks references
/// to determine when assets can be unloaded.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var manager = new AssetManager(new AssetsConfig { RootPath = "Assets" });
///
/// // Register loaders
/// manager.RegisterLoader(new TextureLoader(graphics));
/// manager.RegisterLoader(new AudioClipLoader(audio));
///
/// // Load assets
/// using var texture = manager.Load&lt;TextureAsset&gt;("textures/player.png");
/// using var clip = await manager.LoadAsync&lt;AudioClipAsset&gt;("audio/music.ogg");
///
/// // Use assets
/// if (texture.IsLoaded)
/// {
///     graphics.DrawSprite(texture.Asset!.Handle, position);
/// }
/// </code>
/// </example>
public sealed class AssetManager : IDisposable
{
    private readonly AssetsConfig config;
    private readonly AssetCache cache;
    private readonly LoaderRegistry loaders;
    private readonly SemaphoreSlim loadSemaphore;

    private bool disposed;

    /// <summary>
    /// Gets the root path for asset files.
    /// </summary>
    public string RootPath => config.RootPath;

    /// <summary>
    /// Gets the cache policy.
    /// </summary>
    public CachePolicy CachePolicy => config.CachePolicy;

    /// <summary>
    /// Gets the optional error callback for load failures.
    /// </summary>
    internal Action<string, Exception>? OnLoadError => config.OnLoadError;

    /// <summary>
    /// Raised when an asset is reloaded (hot reload).
    /// </summary>
    public event Action<string>? OnAssetReloaded;

    /// <summary>
    /// Creates a new asset manager with the specified configuration.
    /// </summary>
    /// <param name="config">Configuration options.</param>
    public AssetManager(AssetsConfig? config = null)
    {
        this.config = config ?? AssetsConfig.Default;
        cache = new AssetCache(this.config.CachePolicy, this.config.MaxCacheBytes);
        loaders = new LoaderRegistry();
        loadSemaphore = new SemaphoreSlim(this.config.MaxConcurrentLoads);
    }

    /// <summary>
    /// Registers a loader for a specific asset type.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="loader">The loader to register.</param>
    /// <exception cref="ArgumentNullException">Loader is null.</exception>
    public void RegisterLoader<T>(IAssetLoader<T> loader) where T : class, IDisposable
    {
        ArgumentNullException.ThrowIfNull(loader);
        loaders.Register(loader);
    }

    /// <summary>
    /// Synchronously loads an asset from a file.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="path">Path to the asset file (relative to root).</param>
    /// <returns>A handle to the loaded asset.</returns>
    /// <exception cref="AssetLoadException">Thrown if loading fails.</exception>
    public AssetHandle<T> Load<T>(string path) where T : class, IDisposable
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path);

        var entry = cache.GetOrCreate(path, typeof(T), out var wasCreated);

        if (!wasCreated)
        {
            // Wait for existing load to complete
            WaitForLoad(entry);
            return new AssetHandle<T>(entry.Id, this);
        }

        // Load the asset
        try
        {
            LoadAssetSync<T>(entry, path);
        }
        catch (Exception ex)
        {
            entry.State = AssetState.Failed;
            entry.Error = ex;
            throw;
        }

        return new AssetHandle<T>(entry.Id, this);
    }

    /// <summary>
    /// Asynchronously loads an asset from a file.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="path">Path to the asset file (relative to root).</param>
    /// <param name="priority">Load priority.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A handle to the loaded asset.</returns>
    /// <exception cref="AssetLoadException">Thrown if loading fails.</exception>
    public async Task<AssetHandle<T>> LoadAsync<T>(
        string path,
        LoadPriority priority = LoadPriority.Normal,
        CancellationToken cancellationToken = default) where T : class, IDisposable
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(path);

        var entry = cache.GetOrCreate(path, typeof(T), out var wasCreated);

        if (!wasCreated)
        {
            // Wait for existing load to complete
            await WaitForLoadAsync(entry, cancellationToken);
            return new AssetHandle<T>(entry.Id, this);
        }

        // Load the asset
        try
        {
            await LoadAssetAsync<T>(entry, path, cancellationToken);
        }
        catch (Exception ex)
        {
            entry.State = AssetState.Failed;
            entry.Error = ex;
            throw;
        }

        return new AssetHandle<T>(entry.Id, this);
    }

    /// <summary>
    /// Checks if an asset is loaded (without loading it).
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if the asset is loaded and ready.</returns>
    public bool IsLoaded(string path)
    {
        var entry = cache.GetByPath(path);
        return entry?.State == AssetState.Loaded;
    }

    /// <summary>
    /// Gets the state of an asset by ID.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <returns>The current state.</returns>
    internal AssetState GetState(int id)
        => cache.GetById(id)?.State ?? AssetState.Invalid;

    /// <summary>
    /// Gets the path of an asset by ID.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <returns>The asset path, or null if not found.</returns>
    internal string? GetPath(int id)
        => cache.GetById(id)?.Path;

    /// <summary>
    /// Tries to get the loaded asset instance.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="id">Asset ID.</param>
    /// <returns>The asset, or null if not loaded.</returns>
    internal T? TryGetAsset<T>(int id) where T : class, IDisposable
    {
        var entry = cache.GetById(id);
        return entry?.Asset as T;
    }

    /// <summary>
    /// Adds a reference to an asset.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    internal void AddRef(int id)
    {
        cache.AddRef(id);
    }

    /// <summary>
    /// Releases a reference to an asset.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    internal void Release(int id)
    {
        cache.Release(id);
    }

    /// <summary>
    /// Manually unloads an asset regardless of reference count.
    /// </summary>
    /// <param name="path">Path of the asset to unload.</param>
    public void Unload(string path)
    {
        var entry = cache.GetByPath(path);
        if (entry != null)
        {
            cache.Evict(entry.Id);
        }
    }

    /// <summary>
    /// Unloads all assets from the cache.
    /// </summary>
    public void UnloadAll()
    {
        cache.Clear();
    }

    /// <summary>
    /// Trims the cache to the specified size.
    /// </summary>
    /// <param name="targetBytes">Target size in bytes.</param>
    public void TrimCache(long targetBytes)
    {
        cache.TrimToSize(targetBytes);
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Current cache statistics.</returns>
    public CacheStats GetCacheStats()
        => cache.GetStats();

    /// <summary>
    /// Reloads an asset from disk.
    /// </summary>
    /// <param name="path">Path of the asset to reload.</param>
    /// <returns>Task that completes when reload is finished.</returns>
    public async Task ReloadAsync(string path)
    {
        var entry = cache.GetByPath(path);
        if (entry == null || entry.State != AssetState.Loaded)
        {
            return;
        }

        // Get the loader for this asset type
        var extension = Path.GetExtension(path);
        var assetType = entry.AssetType;

        // Reload using reflection-free approach with known types
        if (assetType == typeof(TextureAsset))
        {
            await ReloadAssetAsync<TextureAsset>(entry, path);
        }
        else if (assetType == typeof(AudioClipAsset))
        {
            await ReloadAssetAsync<AudioClipAsset>(entry, path);
        }
        else if (assetType == typeof(MeshAsset))
        {
            await ReloadAssetAsync<MeshAsset>(entry, path);
        }
        else if (assetType == typeof(RawAsset))
        {
            await ReloadAssetAsync<RawAsset>(entry, path);
        }

        OnAssetReloaded?.Invoke(path);
    }

    /// <summary>
    /// Releases all resources used by the asset manager.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        cache.Clear();
        loadSemaphore.Dispose();
    }

    private void LoadAssetSync<T>(AssetEntry entry, string path) where T : class, IDisposable
    {
        entry.State = AssetState.Loading;

        var extension = Path.GetExtension(path);
        var loader = loaders.GetLoader<T>(extension)
            ?? throw AssetLoadException.UnsupportedFormat(path, typeof(T), extension);

        var fullPath = Path.Combine(config.RootPath, path);
        if (!File.Exists(fullPath))
        {
            throw AssetLoadException.FileNotFound(path, typeof(T));
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            var context = new AssetLoadContext(path, this, config.Services);
            var asset = loader.Load(stream, context);

            entry.Asset = asset;
            entry.State = AssetState.Loaded;
            entry.SizeBytes = loader.EstimateSize(asset);
            cache.UpdateSize(entry.Id, entry.SizeBytes);
        }
        catch (Exception ex)
        {
            throw AssetLoadException.ParseError(path, typeof(T), ex);
        }
    }

    private async Task LoadAssetAsync<T>(AssetEntry entry, string path, CancellationToken ct)
        where T : class, IDisposable
    {
        entry.State = AssetState.Loading;

        var extension = Path.GetExtension(path);
        var loader = loaders.GetLoader<T>(extension)
            ?? throw AssetLoadException.UnsupportedFormat(path, typeof(T), extension);

        var fullPath = Path.Combine(config.RootPath, path);
        if (!File.Exists(fullPath))
        {
            throw AssetLoadException.FileNotFound(path, typeof(T));
        }

        await loadSemaphore.WaitAsync(ct);
        try
        {
            await using var stream = File.OpenRead(fullPath);
            var context = new AssetLoadContext(path, this, config.Services);
            var asset = await loader.LoadAsync(stream, context, ct);

            entry.Asset = asset;
            entry.State = AssetState.Loaded;
            entry.SizeBytes = loader.EstimateSize(asset);
            cache.UpdateSize(entry.Id, entry.SizeBytes);
        }
        catch (OperationCanceledException)
        {
            entry.State = AssetState.Failed;
            throw;
        }
        catch (Exception ex)
        {
            throw AssetLoadException.ParseError(path, typeof(T), ex);
        }
        finally
        {
            loadSemaphore.Release();
        }
    }

    private async Task ReloadAssetAsync<T>(AssetEntry entry, string path) where T : class, IDisposable
    {
        var extension = Path.GetExtension(path);
        var loader = loaders.GetLoader<T>(extension);
        if (loader == null)
        {
            return;
        }

        var fullPath = Path.Combine(config.RootPath, path);
        if (!File.Exists(fullPath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(fullPath);
            var context = new AssetLoadContext(path, this, config.Services);
            var newAsset = await loader.LoadAsync(stream, context);

            // Dispose old asset and replace
            var oldAsset = entry.Asset;
            entry.Asset = newAsset;
            entry.SizeBytes = loader.EstimateSize(newAsset);
            cache.UpdateSize(entry.Id, entry.SizeBytes);

            if (oldAsset is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch
        {
            // Silently fail on reload - keep the old asset
        }
    }

    private static void WaitForLoad(AssetEntry entry)
    {
        // Spin wait for load to complete
        var spinWait = new SpinWait();
        while (entry.State is AssetState.Pending or AssetState.Loading)
        {
            spinWait.SpinOnce();
        }

        if (entry.State == AssetState.Failed && entry.Error != null)
        {
            throw entry.Error;
        }
    }

    private static async Task WaitForLoadAsync(AssetEntry entry, CancellationToken ct)
    {
        while (entry.State is AssetState.Pending or AssetState.Loading)
        {
            await Task.Delay(1, ct);
        }

        if (entry.State == AssetState.Failed && entry.Error != null)
        {
            throw entry.Error;
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
