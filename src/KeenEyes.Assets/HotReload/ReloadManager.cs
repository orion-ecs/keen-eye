using System.Collections.Concurrent;

namespace KeenEyes.Assets;

/// <summary>
/// Watches for file changes and triggers asset reloading in development mode.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReloadManager"/> uses <see cref="FileSystemWatcher"/> to monitor
/// the asset directory for changes. When a file is modified, it debounces rapid
/// changes and triggers a reload through the <see cref="AssetManager"/>.
/// </para>
/// <para>
/// This is intended for development use only. In production, hot reload should
/// be disabled for performance.
/// </para>
/// </remarks>
public sealed class ReloadManager : IDisposable
{
    private readonly FileSystemWatcher watcher;
    private readonly AssetManager assetManager;
    private readonly ConcurrentDictionary<string, DateTime> pendingReloads = new();
    private readonly TimeSpan debounceDelay;
    private readonly CancellationTokenSource cts = new();

    private bool disposed;

    /// <summary>
    /// Raised when an asset is reloaded.
    /// </summary>
    public event Action<string>? OnAssetReloaded;

    /// <summary>
    /// Creates a new reload manager.
    /// </summary>
    /// <param name="rootPath">Root directory to watch.</param>
    /// <param name="assetManager">Asset manager to trigger reloads on.</param>
    /// <param name="debounceDelay">Delay before triggering reload after change.</param>
    public ReloadManager(
        string rootPath,
        AssetManager assetManager,
        TimeSpan? debounceDelay = null)
    {
        ArgumentNullException.ThrowIfNull(assetManager);

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Asset root directory not found: {rootPath}");
        }

        this.assetManager = assetManager;
        this.debounceDelay = debounceDelay ?? TimeSpan.FromMilliseconds(100);

        watcher = new FileSystemWatcher(rootPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnFileChanged;
        watcher.Created += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
    }

    /// <summary>
    /// Starts watching for file changes.
    /// </summary>
    public void Start()
    {
        watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Stops watching for file changes.
    /// </summary>
    public void Stop()
    {
        watcher.EnableRaisingEvents = false;
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
        cts.Cancel();
        watcher.EnableRaisingEvents = false;
        watcher.Dispose();
        cts.Dispose();
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        await TriggerReloadAsync(e.FullPath);
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (disposed)
        {
            return;
        }

        await TriggerReloadAsync(e.FullPath);
    }

    private async Task TriggerReloadAsync(string fullPath)
    {
        // Convert to relative path
        var relativePath = Path.GetRelativePath(watcher.Path, fullPath);

        // Skip hidden files and directories
        if (relativePath.StartsWith('.') || relativePath.Contains("/.") || relativePath.Contains("\\."))
        {
            return;
        }

        // Debounce: track the change time
        pendingReloads[relativePath] = DateTime.UtcNow;

        try
        {
            await Task.Delay(debounceDelay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // Check if this is still the most recent change
        if (!pendingReloads.TryRemove(relativePath, out _))
        {
            return; // Another change superseded this one
        }

        // Check if the asset is loaded
        if (!assetManager.IsLoaded(relativePath))
        {
            return;
        }

        // Trigger reload
        try
        {
            await assetManager.ReloadAsync(relativePath);
            OnAssetReloaded?.Invoke(relativePath);
        }
        catch (Exception ex)
        {
            // Log but don't throw - hot reload failures shouldn't crash the app
            Console.Error.WriteLine($"[HotReload] Failed to reload {relativePath}: {ex.Message}");
        }
    }
}
