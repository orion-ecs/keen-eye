namespace KeenEyes.Assets;

/// <summary>
/// System that resolves <see cref="AssetRef{T}"/> components to loaded asset handles.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the PreUpdate phase and automatically loads assets referenced
/// by <see cref="AssetRef{T}"/> components. Once an asset is loaded, the component's
/// internal handle ID is set, and <see cref="AssetRef{T}.IsResolved"/> returns true.
/// </para>
/// <para>
/// Assets are loaded asynchronously with <see cref="LoadPriority.Normal"/> priority.
/// The system uses async loading to avoid frame hitches from loading large assets.
/// </para>
/// </remarks>
public sealed class AssetResolutionSystem : ISystem
{
    private IWorld? world;
    private AssetManager? assetManager;

    // Track pending loads to avoid duplicate requests
    private readonly HashSet<string> pendingLoads = [];

    /// <inheritdoc />
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public void Initialize(IWorld world)
    {
        this.world = world;

        if (!world.TryGetExtension<AssetManager>(out assetManager))
        {
            // No asset manager - system will do nothing
            Enabled = false;
        }
    }

    /// <inheritdoc />
    public void Update(float deltaTime)
    {
        if (assetManager == null || world == null)
        {
            return;
        }

        // Resolve texture assets
        ResolveAssets<TextureAsset>();

        // Resolve audio assets
        ResolveAssets<AudioClipAsset>();

        // Resolve mesh assets
        ResolveAssets<MeshAsset>();

        // Resolve raw assets
        ResolveAssets<RawAsset>();
    }

    private void ResolveAssets<T>() where T : class, IDisposable
    {
        foreach (var entity in world!.Query<AssetRef<T>>())
        {
            ref var assetRef = ref world.Get<AssetRef<T>>(entity);

            // Skip if already resolved or no path set
            if (assetRef.IsResolved || !assetRef.HasPath)
            {
                continue;
            }

            // Check if already loaded
            if (assetManager!.IsLoaded(assetRef.Path))
            {
                // Asset is loaded, resolve the reference
                var handle = assetManager.Load<T>(assetRef.Path);
                assetRef.HandleId = handle.Id;
                continue;
            }

            // Check if load is pending
            if (pendingLoads.Contains(assetRef.Path))
            {
                continue;
            }

            // Start async load
            pendingLoads.Add(assetRef.Path);
            _ = LoadAssetAsync<T>(assetRef.Path);
        }
    }

    private async Task LoadAssetAsync<T>(string path) where T : class, IDisposable
    {
        try
        {
            await assetManager!.LoadAsync<T>(path, LoadPriority.Normal);
        }
        catch (Exception ex)
        {
            // Log but don't throw - continue with other entities
            Console.Error.WriteLine($"[AssetResolution] Failed to load {path}: {ex.Message}");
        }
        finally
        {
            pendingLoads.Remove(path);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }
}
