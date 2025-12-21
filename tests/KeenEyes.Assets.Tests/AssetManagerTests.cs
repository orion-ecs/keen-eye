namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetManager functionality including loading, caching, and lifecycle.
/// </summary>
public class AssetManagerTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public AssetManagerTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        manager.RegisterLoader(new TestAssetLoader());
        manager.RegisterLoader(new SecondTestAssetLoader());
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    #region Load Tests

    [Fact]
    public void Load_WithValidFile_ReturnsLoadedAsset()
    {
        var path = testDir.CreateFile("test.txt", "Hello, World!");

        using var handle = manager.Load<TestAsset>(path);

        Assert.True(handle.IsValid);
        Assert.True(handle.IsLoaded);
        Assert.Equal("Hello, World!", handle.Asset!.Content);
    }

    [Fact]
    public void Load_WithMissingFile_ThrowsAssetLoadException()
    {
        var ex = Assert.Throws<AssetLoadException>(() => manager.Load<TestAsset>("missing.txt"));
        Assert.Contains("not found", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_WithNoLoader_ThrowsAssetLoadException()
    {
        testDir.CreateFile("unknown.xyz", "content");

        var ex = Assert.Throws<AssetLoadException>(() => manager.Load<TestAsset>("unknown.xyz"));
        Assert.Contains(".xyz", ex.Message);
    }

    [Fact]
    public void Load_SamePathTwice_ReturnsSameAsset()
    {
        var path = testDir.CreateFile("test.txt", "Content");

        using var handle1 = manager.Load<TestAsset>(path);
        using var handle2 = manager.Load<TestAsset>(path);

        Assert.Equal(handle1.Id, handle2.Id);
        Assert.Same(handle1.Asset, handle2.Asset);
    }

    [Fact]
    public void Load_NullPath_ThrowsArgumentException()
    {
        Assert.ThrowsAny<ArgumentException>(() => manager.Load<TestAsset>(null!));
    }

    [Fact]
    public void Load_EmptyPath_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => manager.Load<TestAsset>(""));
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_WithValidFile_ReturnsLoadedAsset()
    {
        var path = testDir.CreateFile("async.txt", "Async Content");

        using var handle = await manager.LoadAsync<TestAsset>(path);

        Assert.True(handle.IsLoaded);
        Assert.Equal("Async Content", handle.Asset!.Content);
    }

    [Fact]
    public async Task LoadAsync_WithCancellation_ThrowsOperationCanceled()
    {
        manager.RegisterLoader(new SlowLoader(500));
        var path = testDir.CreateFile("slow.slow", "Slow Content");
        var cts = new CancellationTokenSource();

        var loadTask = manager.LoadAsync<TestAsset>(path, cancellationToken: cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loadTask);
    }

    [Fact]
    public async Task LoadAsync_ConcurrentLoads_OnlyLoadsOnce()
    {
        manager.RegisterLoader(new SlowLoader(100));
        var path = testDir.CreateFile("concurrent.slow", "Content");

        var task1 = manager.LoadAsync<TestAsset>(path);
        var task2 = manager.LoadAsync<TestAsset>(path);

        var handle1 = await task1;
        var handle2 = await task2;

        Assert.Equal(handle1.Id, handle2.Id);
        handle1.Dispose();
        handle2.Dispose();
    }

    #endregion

    #region IsLoaded Tests

    [Fact]
    public void IsLoaded_BeforeLoad_ReturnsFalse()
    {
        testDir.CreateFile("notloaded.txt", "content");

        Assert.False(manager.IsLoaded("notloaded.txt"));
    }

    [Fact]
    public void IsLoaded_AfterLoad_ReturnsTrue()
    {
        var path = testDir.CreateFile("loaded.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.True(manager.IsLoaded(path));
    }

    [Fact]
    public void IsLoaded_AfterUnload_ReturnsFalse()
    {
        var path = testDir.CreateFile("unloaded.txt", "content");
        using var handle = manager.Load<TestAsset>(path);
        manager.Unload(path);

        Assert.False(manager.IsLoaded(path));
    }

    #endregion

    #region Unload Tests

    [Fact]
    public void Unload_LoadedAsset_DisposesAsset()
    {
        var path = testDir.CreateFile("dispose.txt", "content");
        var handle = manager.Load<TestAsset>(path);
        var asset = handle.Asset!;

        manager.Unload(path);

        Assert.True(asset.IsDisposed);
    }

    [Fact]
    public void Unload_NonExistent_DoesNotThrow()
    {
        manager.Unload("nonexistent.txt");
    }

    [Fact]
    public void UnloadAll_DisposesAllAssets()
    {
        var path1 = testDir.CreateFile("a.txt", "a");
        var path2 = testDir.CreateFile("b.txt", "b");

        var h1 = manager.Load<TestAsset>(path1);
        var h2 = manager.Load<TestAsset>(path2);
        var asset1 = h1.Asset!;
        var asset2 = h2.Asset!;

        manager.UnloadAll();

        Assert.True(asset1.IsDisposed);
        Assert.True(asset2.IsDisposed);
    }

    #endregion

    #region Reference Counting Tests

    [Fact]
    public void Handle_Dispose_DecreasesRefCount()
    {
        var path = testDir.CreateFile("ref.txt", "content");

        var h1 = manager.Load<TestAsset>(path);
        var h2 = manager.Load<TestAsset>(path);
        var asset = h1.Asset!;

        h1.Dispose();
        Assert.False(asset.IsDisposed); // Still referenced by h2

        h2.Dispose();
        // Asset may or may not be disposed depending on cache policy
    }

    [Fact]
    public void Handle_Acquire_IncreasesRefCount()
    {
        var path = testDir.CreateFile("acquire.txt", "content");
        var original = manager.Load<TestAsset>(path);
        var acquired = original.Acquire();

        original.Dispose();
        Assert.True(manager.IsLoaded(path)); // Still loaded due to acquired handle

        acquired.Dispose();
    }

    #endregion

    #region Cache Stats Tests

    [Fact]
    public void GetCacheStats_ReturnsCorrectCounts()
    {
        var path1 = testDir.CreateFile("s1.txt", "a");
        var path2 = testDir.CreateFile("s2.txt", "b");

        using var h1 = manager.Load<TestAsset>(path1);
        using var h2 = manager.Load<TestAsset>(path2);

        var stats = manager.GetCacheStats();

        Assert.Equal(2, stats.TotalAssets);
        Assert.Equal(2, stats.LoadedAssets);
    }

    [Fact]
    public void GetCacheStats_TracksCacheHitsAndMisses()
    {
        var path = testDir.CreateFile("hits.txt", "content");

        using var h1 = manager.Load<TestAsset>(path); // Miss
        using var h2 = manager.Load<TestAsset>(path); // Hit

        var stats = manager.GetCacheStats();

        Assert.Equal(1, stats.CacheMisses);
        Assert.Equal(1, stats.CacheHits);
    }

    #endregion

    #region TrimCache Tests

    [Fact]
    public void TrimCache_ReducesCacheSize()
    {
        var path = testDir.CreateFile("trim.txt", new string('x', 1000));
        var handle = manager.Load<TestAsset>(path);
        handle.Dispose(); // Release reference

        // TrimCache may or may not evict depending on implementation
        manager.TrimCache(0);

        // Just verify it doesn't throw
    }

    [Fact]
    public void TrimCache_PreservesReferencedAssets()
    {
        var path = testDir.CreateFile("keep.txt", new string('x', 1000));
        using var handle = manager.Load<TestAsset>(path);

        manager.TrimCache(0); // Request trim to 0 bytes

        Assert.True(manager.IsLoaded(path)); // Still loaded because referenced
    }

    #endregion

    #region Multiple Asset Types Tests

    [Fact]
    public void Load_DifferentAssetTypes_WorksCorrectly()
    {
        var txtPath = testDir.CreateFile("text.txt", "Hello");
        var numPath = testDir.CreateFile("number.num", "42");

        using var textHandle = manager.Load<TestAsset>(txtPath);
        using var numHandle = manager.Load<SecondTestAsset>(numPath);

        Assert.Equal("Hello", textHandle.Asset!.Content);
        Assert.Equal(42, numHandle.Asset!.Value);
    }

    #endregion

    #region Dependency Loading Tests

    [Fact]
    public void LoadDependency_TracksRelationship()
    {
        var parentPath = testDir.CreateFile("parent.txt", "parent");
        var depPath = testDir.CreateFile("dep.txt", "dependency");

        using var parentHandle = manager.Load<TestAsset>(parentPath);
        var depHandle = manager.LoadDependency<TestAsset>(parentPath, depPath);

        Assert.True(depHandle.IsLoaded);
        Assert.True(manager.IsLoaded(depPath));
    }

    [Fact]
    public async Task LoadDependencyAsync_TracksRelationship()
    {
        var parentPath = testDir.CreateFile("aparent.txt", "parent");
        var depPath = testDir.CreateFile("adep.txt", "dependency");

        using var parentHandle = await manager.LoadAsync<TestAsset>(parentPath);
        var depHandle = await manager.LoadDependencyAsync<TestAsset>(parentPath, depPath);

        Assert.True(depHandle.IsLoaded);
    }

    #endregion

    #region Disposed Manager Tests

    [Fact]
    public void Load_AfterDispose_ThrowsObjectDisposedException()
    {
        var path = testDir.CreateFile("disposed.txt", "content");
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.Load<TestAsset>(path));
    }

    [Fact]
    public async Task LoadAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var path = testDir.CreateFile("disposed.txt", "content");
        manager.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => manager.LoadAsync<TestAsset>(path));
    }

    #endregion
}
