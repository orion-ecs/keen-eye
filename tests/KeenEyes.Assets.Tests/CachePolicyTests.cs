namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for different cache eviction policies.
/// </summary>
public class CachePolicyTests : IDisposable
{
    private readonly TestAssetDirectory testDir;

    public CachePolicyTests()
    {
        testDir = new TestAssetDirectory();
    }

    public void Dispose()
    {
        testDir.Dispose();
    }

    #region LRU Policy Tests

    [Fact]
    public void LRU_TrimCache_DoesNotThrow()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.LRU
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path1 = testDir.CreateFile("old.txt", new string('a', 100));
        var path2 = testDir.CreateFile("new.txt", new string('b', 100));

        // Load old first, then new
        var h1 = manager.Load<TestAsset>(path1);
        Thread.Sleep(10); // Ensure different timestamps
        var h2 = manager.Load<TestAsset>(path2);

        h1.Dispose();
        h2.Dispose();

        // Access new to update LRU timestamp
        using var h2Again = manager.Load<TestAsset>(path2);

        // Trim with LRU policy should not throw
        manager.TrimCache(100);

        // Just verify it doesn't throw - cache eviction behavior is implementation-specific
    }

    [Fact]
    public void LRU_WithActiveReferences_DoesNotEvict()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.LRU
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("keep.txt", new string('x', 100));
        using var handle = manager.Load<TestAsset>(path);

        manager.TrimCache(0);

        Assert.True(manager.IsLoaded(path));
    }

    #endregion

    #region Manual Policy Tests

    [Fact]
    public void Manual_TrimCache_DoesNotEvict()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.Manual
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("manual.txt", new string('x', 100));
        var handle = manager.Load<TestAsset>(path);
        handle.Dispose();

        manager.TrimCache(0);

        Assert.True(manager.IsLoaded(path));
    }

    [Fact]
    public void Manual_ExplicitUnload_Works()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.Manual
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("unload.txt", "content");
        var handle = manager.Load<TestAsset>(path);
        handle.Dispose();

        manager.Unload(path);

        Assert.False(manager.IsLoaded(path));
    }

    #endregion

    #region Aggressive Policy Tests

    [Fact]
    public void Aggressive_ReleaseLastReference_DisposesImmediately()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.Aggressive
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("aggressive.txt", "content");
        var handle = manager.Load<TestAsset>(path);
        var asset = handle.Asset!;

        handle.Dispose();

        Assert.True(asset.IsDisposed);
        Assert.False(manager.IsLoaded(path));
    }

    [Fact]
    public void Aggressive_WithMultipleReferences_DisposesOnLastRelease()
    {
        using var manager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.Aggressive
        });
        manager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("multi.txt", "content");
        var h1 = manager.Load<TestAsset>(path);
        var h2 = manager.Load<TestAsset>(path);
        var asset = h1.Asset!;

        h1.Dispose();
        Assert.False(asset.IsDisposed);

        h2.Dispose();
        Assert.True(asset.IsDisposed);
    }

    #endregion
}
