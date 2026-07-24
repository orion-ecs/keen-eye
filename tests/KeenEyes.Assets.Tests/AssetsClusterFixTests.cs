using System.Reflection;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Regression tests for the assets bug cluster: dependency reference counting on cache hits
/// (#1190), a cancelled streaming load leaving an entry stuck in Loading (#1191), cancellation
/// poisoning cache entries (#1192), and the inverted hot-reload debounce (#1197).
/// </summary>
public class AssetsClusterFixTests
{
    #region #1190 - Dependency refcount on cache hit

    /// <summary>
    /// An asset that owns a dependency loaded through <see cref="AssetManager.LoadDependency{T}"/>.
    /// </summary>
    private sealed class ParentAsset(TestAsset dependency) : IDisposable
    {
        public TestAsset Dependency { get; } = dependency;

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>
    /// Loader that pulls in a child asset as a registered dependency of the parent.
    /// </summary>
    private sealed class ParentLoader(string dependencyPath) : IAssetLoader<ParentAsset>
    {
        public IReadOnlyList<string> Extensions => [".parent"];

        public ParentAsset Load(Stream stream, AssetLoadContext context)
        {
            // The returned handle is owned by the parent's dependency tracking, not the caller.
            var dependency = context.Manager.LoadDependency<TestAsset>(context.Path, dependencyPath);
            return new ParentAsset(dependency.Asset!);
        }

        public Task<ParentAsset> LoadAsync(Stream stream, AssetLoadContext context, CancellationToken ct = default)
            => Task.FromResult(Load(stream, context));

        public long EstimateSize(ParentAsset asset) => 1;
    }

    [Fact]
    public void Load_CacheHitOnParent_KeepsDependencyAliveAfterReleasingOneReference()
    {
        using var dir = new TestAssetDirectory();
        dir.CreateFile("dependency.txt", "dep-content");
        dir.CreateFile("model.parent", "parent");

        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = dir.RootPath,
            CachePolicy = CachePolicy.Aggressive
        });
        manager.RegisterLoader(new TestAssetLoader());
        manager.RegisterLoader(new ParentLoader("dependency.txt"));

        // First load creates the parent and its dependency (dependency refcount = 1).
        var first = manager.Load<ParentAsset>("model.parent");
        var dependency = first.Asset!.Dependency;
        Assert.False(dependency.IsDisposed);

        // Second load is a cache hit. Acquisition must reference the dependency too, mirroring
        // the recursive release, otherwise releasing one parent handle drives the dependency's
        // refcount to zero and disposes it while a parent still holds it.
        var second = manager.Load<ParentAsset>("model.parent");

        first.Dispose();

        Assert.False(dependency.IsDisposed);
        Assert.Equal("dep-content", dependency.Content);
        Assert.True(second.IsLoaded);
        Assert.Same(dependency, second.Asset!.Dependency);

        second.Dispose();
    }

    #endregion

    #region #1191 - Cancelled LoadByTypeAsync must not stick in Loading

    [Fact]
    public async Task LoadByTypeAsync_WhenCancelled_DoesNotLeaveEntryStuckLoadingAndAllowsRetry()
    {
        using var dir = new TestAssetDirectory();
        var path = dir.CreateFile("streamed.bin", "payload");

        using var manager = new AssetManager(new AssetsConfig { RootPath = dir.RootPath });
        manager.RegisterLoader(new RawLoader());

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        // A pre-cancelled token throws out of the semaphore wait, before the load body runs.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => manager.LoadByTypeAsync(path, typeof(RawAsset), LoadPriority.Normal, cancelled.Token));

        // Before the fix the entry stayed in Loading, so this retry would spin in
        // WaitForLoadAsync forever. It must now complete promptly.
        var retry = manager.LoadByTypeAsync(path, typeof(RawAsset), LoadPriority.Normal, CancellationToken.None);
        var finished = await Task.WhenAny(retry, Task.Delay(2000));

        Assert.Same(retry, finished);
        await retry;
        Assert.True(manager.IsLoaded(path));
    }

    #endregion

    #region #1192 - Cancellation must not poison a cache entry

    [Fact]
    public async Task LoadAsync_AfterCancellation_RetriesSuccessfullyWithFreshToken()
    {
        using var dir = new TestAssetDirectory();
        var path = dir.CreateFile("retry.bin", "payload");

        using var manager = new AssetManager(new AssetsConfig { RootPath = dir.RootPath });
        manager.RegisterLoader(new RawLoader());

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => manager.LoadAsync<RawAsset>(path, LoadPriority.Normal, cancelled.Token));

        // Before the fix the cancelled entry was cached as a permanent failure holding the stale
        // OperationCanceledException, so this retry rethrew it. It must now load cleanly.
        using var handle = await manager.LoadAsync<RawAsset>(path);

        Assert.True(handle.IsLoaded);
        Assert.NotNull(handle.Asset);
    }

    #endregion

    #region #1197 - Trailing debounce reloads only the most-recent change

    [Fact]
    public async Task ReloadManager_OverlappingChanges_ReloadsExactlyOnceForTheMostRecentChange()
    {
        // NOTE: This exercises the debounce decision directly by invoking TriggerReloadAsync
        // (the FileSystemWatcher callback body) rather than mutating files, because real watcher
        // event timing is inherently non-deterministic. Debounce is deliberately large relative
        // to the inter-change spacing so overlapping changes coalesce; the burst still spans more
        // than one debounce window, which is what exposed the inverted (leading) behaviour.
        using var dir = new TestAssetDirectory();
        var relativePath = dir.CreateFile("hot.bin", "payload");
        var fullPath = Path.Combine(dir.RootPath, relativePath);

        using var manager = new AssetManager(new AssetsConfig { RootPath = dir.RootPath });
        manager.RegisterLoader(new RawLoader());

        // Keep the asset loaded so ReloadAsync actually runs and fires the reload event.
        using var handle = manager.Load<RawAsset>(relativePath);
        Assert.True(handle.IsLoaded);

        using var reload = new ReloadManager(dir.RootPath, manager, TimeSpan.FromMilliseconds(200));
        reload.Stop(); // Silence the real watcher; we drive the debounce logic directly.

        var reloadCount = 0;
        reload.OnAssetReloaded += _ => Interlocked.Increment(ref reloadCount);

        var trigger = typeof(ReloadManager).GetMethod(
            "TriggerReloadAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Fire a burst of overlapping changes (~20ms apart, ~400ms total span > 200ms debounce).
        var pending = new List<Task>();
        for (var i = 0; i < 20; i++)
        {
            pending.Add((Task)trigger.Invoke(reload, [fullPath])!);
            await Task.Delay(20);
        }

        await Task.WhenAll(pending);
        await Task.Delay(100); // Let any trailing reload event settle.

        // Leading (buggy) debounce fired for the first change while later changes were still
        // arriving, producing multiple reloads. True trailing debounce coalesces to exactly one.
        Assert.Equal(1, Volatile.Read(ref reloadCount));
    }

    #endregion
}
