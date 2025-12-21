using System.Collections.Concurrent;

namespace KeenEyes.Assets;

/// <summary>
/// Manages background preloading of assets for level streaming.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="StreamingManager"/> provides a way to preload assets in the background
/// before they're needed, enabling smooth level transitions and reducing load times
/// when entering new areas.
/// </para>
/// <para>
/// Assets are queued for streaming and loaded with <see cref="LoadPriority.Streaming"/>
/// priority, ensuring they don't interfere with higher-priority loads.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var streaming = new StreamingManager(assetManager);
///
/// // Queue assets for the next level
/// streaming.Queue&lt;TextureAsset&gt;("levels/forest/ground.png");
/// streaming.Queue&lt;MeshAsset&gt;("levels/forest/trees.glb");
/// streaming.Queue&lt;AudioClipAsset&gt;("levels/forest/ambient.ogg");
///
/// // Start streaming
/// streaming.Start();
///
/// // Check progress
/// Console.WriteLine($"Streaming: {streaming.Progress:P0} complete");
///
/// // Wait for completion before level transition
/// await streaming.WaitForCompletionAsync();
/// </code>
/// </example>
public sealed class StreamingManager : IDisposable
{
    private readonly AssetManager assetManager;
    private readonly ConcurrentQueue<StreamingRequest> pendingRequests = new();
    private readonly ConcurrentDictionary<string, StreamingRequest> activeRequests = new();

    private CancellationTokenSource? streamingCts;
    private Task? streamingTask;
    private int totalQueued;
    private int completed;
    private bool disposed;

    /// <summary>
    /// Creates a new streaming manager for the specified asset manager.
    /// </summary>
    /// <param name="assetManager">The asset manager to use for loading.</param>
    public StreamingManager(AssetManager assetManager)
    {
        ArgumentNullException.ThrowIfNull(assetManager);
        this.assetManager = assetManager;
    }

    /// <summary>
    /// Gets the number of assets currently queued for streaming.
    /// </summary>
    public int QueuedCount => pendingRequests.Count + activeRequests.Count;

    /// <summary>
    /// Gets the streaming progress as a value between 0 and 1.
    /// </summary>
    public float Progress => totalQueued == 0 ? 1f : (float)completed / totalQueued;

    /// <summary>
    /// Gets whether streaming is currently in progress.
    /// </summary>
    public bool IsStreaming => streamingTask != null && !streamingTask.IsCompleted;

    /// <summary>
    /// Raised when an asset finishes streaming.
    /// </summary>
    public event Action<string>? OnAssetStreamed;

    /// <summary>
    /// Raised when all queued assets have finished streaming.
    /// </summary>
    public event Action? OnStreamingComplete;

    /// <summary>
    /// Raised when an asset fails to stream.
    /// </summary>
    public event Action<string, Exception>? OnStreamingError;

    /// <summary>
    /// Queues an asset for background streaming.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="path">Path to the asset.</param>
    /// <remarks>
    /// If the asset is already loaded, this method does nothing.
    /// If the asset is already queued, it won't be queued again.
    /// </remarks>
    public void Queue<T>(string path) where T : class, IDisposable
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        // Skip if already loaded
        if (assetManager.IsLoaded(path))
        {
            return;
        }

        // Skip if already queued
        if (activeRequests.ContainsKey(path))
        {
            return;
        }

        var request = new StreamingRequest(path, typeof(T));
        pendingRequests.Enqueue(request);
        Interlocked.Increment(ref totalQueued);
    }

    /// <summary>
    /// Queues multiple assets for background streaming.
    /// </summary>
    /// <typeparam name="T">The asset type.</typeparam>
    /// <param name="paths">Paths to the assets.</param>
    public void QueueMany<T>(IEnumerable<string> paths) where T : class, IDisposable
    {
        foreach (var path in paths)
        {
            Queue<T>(path);
        }
    }

    /// <summary>
    /// Starts background streaming of queued assets.
    /// </summary>
    /// <param name="maxConcurrent">Maximum concurrent load operations.</param>
    /// <remarks>
    /// If streaming is already in progress, newly queued assets will be
    /// picked up automatically.
    /// </remarks>
    public void Start(int maxConcurrent = 2)
    {
        if (IsStreaming)
        {
            return;
        }

        streamingCts = new CancellationTokenSource();
        streamingTask = StreamAsync(maxConcurrent, streamingCts.Token);
    }

    /// <summary>
    /// Stops background streaming.
    /// </summary>
    /// <remarks>
    /// Assets currently being loaded will complete, but no new assets
    /// will be started. The queue is not cleared.
    /// </remarks>
    public void Stop()
    {
        streamingCts?.Cancel();
    }

    /// <summary>
    /// Clears all queued assets without loading them.
    /// </summary>
    public void Clear()
    {
        while (pendingRequests.TryDequeue(out _))
        {
            // Drain the queue
        }

        Interlocked.Exchange(ref totalQueued, 0);
        Interlocked.Exchange(ref completed, 0);
    }

    /// <summary>
    /// Waits for all queued assets to finish streaming.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task that completes when streaming is done.</returns>
    public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        if (streamingTask == null)
        {
            return;
        }

        try
        {
            await streamingTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Expected if cancelled
        }
    }

    /// <summary>
    /// Releases all resources.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        streamingCts?.Cancel();
        streamingCts?.Dispose();
        Clear();
    }

    private async Task StreamAsync(int maxConcurrent, CancellationToken ct)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrent);
        var loadTasks = new List<Task>();

        while (!ct.IsCancellationRequested)
        {
            // Wait for a slot
            await semaphore.WaitAsync(ct);

            if (!pendingRequests.TryDequeue(out var request))
            {
                semaphore.Release();

                // No more pending requests - check if we're done
                if (loadTasks.Count == 0)
                {
                    break;
                }

                // Wait for any active task to complete
                await Task.WhenAny(loadTasks);
                loadTasks.RemoveAll(t => t.IsCompleted);
                continue;
            }

            // Track this request
            activeRequests.TryAdd(request.Path, request);

            // Start loading
            var loadTask = LoadAssetAsync(request, semaphore, ct);
            loadTasks.Add(loadTask);
        }

        // Wait for remaining tasks
        if (loadTasks.Count > 0)
        {
            await Task.WhenAll(loadTasks);
        }

        OnStreamingComplete?.Invoke();
    }

    private async Task LoadAssetAsync(StreamingRequest request, SemaphoreSlim semaphore, CancellationToken ct)
    {
        try
        {
            // Use extensible type-erased loading (works with any registered loader)
            await assetManager.LoadByTypeAsync(request.Path, request.AssetType, LoadPriority.Streaming, ct);

            Interlocked.Increment(ref completed);
            OnAssetStreamed?.Invoke(request.Path);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnStreamingError?.Invoke(request.Path, ex);
        }
        finally
        {
            activeRequests.TryRemove(request.Path, out _);
            semaphore.Release();
        }
    }

    private readonly record struct StreamingRequest(string Path, Type AssetType);
}
