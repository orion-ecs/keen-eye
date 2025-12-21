using System.Collections.Concurrent;

namespace KeenEyes.Assets;

/// <summary>
/// System that resolves <see cref="AssetRef{T}"/> components to loaded asset handles.
/// </summary>
/// <remarks>
/// <para>
/// This system runs in the EarlyUpdate phase and automatically loads assets referenced
/// by <see cref="AssetRef{T}"/> components. Once an asset is loaded, the component's
/// internal handle ID is set, and <see cref="AssetRef{T}.IsResolved"/> returns true.
/// </para>
/// <para>
/// Assets are loaded asynchronously with <see cref="LoadPriority.Normal"/> priority.
/// The system uses async loading to avoid frame hitches from loading large assets.
/// Pending load tasks are tracked and observed to prevent unobserved exceptions.
/// </para>
/// </remarks>
public sealed class AssetResolutionSystem : ISystem
{
    private IWorld? world;
    private AssetManager? assetManager;

    // Track pending loads with their tasks to observe exceptions
    private readonly ConcurrentDictionary<string, Task> pendingLoads = new();

    // Completed tasks to process on next update
    private readonly ConcurrentQueue<string> completedPaths = new();

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

        // Process completed tasks first
        ProcessCompletedTasks();

        // Resolve texture assets
        ResolveAssets<TextureAsset>();

        // Resolve audio assets
        ResolveAssets<AudioClipAsset>();

        // Resolve mesh assets
        ResolveAssets<MeshAsset>();

        // Resolve raw assets
        ResolveAssets<RawAsset>();
    }

    private void ProcessCompletedTasks()
    {
        // Clean up completed tasks from the dictionary
        while (completedPaths.TryDequeue(out var path))
        {
            pendingLoads.TryRemove(path, out _);
        }
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
            if (pendingLoads.ContainsKey(assetRef.Path))
            {
                continue;
            }

            // Start async load and track the task
            var loadTask = LoadAssetAsync<T>(assetRef.Path);
            pendingLoads.TryAdd(assetRef.Path, loadTask);
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
            // Invoke error callback if configured
            assetManager!.OnLoadError?.Invoke(path, ex);
        }
        finally
        {
            // Queue for removal on next update
            completedPaths.Enqueue(path);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // Wait for all pending loads to complete to ensure no unobserved exceptions
        var tasks = pendingLoads.Values.ToArray();
        if (tasks.Length > 0)
        {
            Task.WhenAll(tasks).ConfigureAwait(false).GetAwaiter().GetResult();
        }
        pendingLoads.Clear();
    }
}
