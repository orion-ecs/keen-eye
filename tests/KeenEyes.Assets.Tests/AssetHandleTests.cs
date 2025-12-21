namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for AssetHandle struct behavior.
/// </summary>
public class AssetHandleTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public AssetHandleTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        manager.RegisterLoader(new TestAssetLoader());
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    #region IsValid Tests

    [Fact]
    public void IsValid_WithLoadedHandle_ReturnsTrue()
    {
        var path = testDir.CreateFile("valid.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.True(handle.IsValid);
    }

    [Fact]
    public void IsValid_WithDefaultHandle_ReturnsFalse()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.False(handle.IsValid);
    }

    #endregion

    #region State Tests

    [Fact]
    public void State_WhenLoaded_ReturnsLoaded()
    {
        var path = testDir.CreateFile("state.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.Equal(AssetState.Loaded, handle.State);
    }

    [Fact]
    public void State_WhenInvalid_ReturnsInvalid()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.Equal(AssetState.Invalid, handle.State);
    }

    #endregion

    #region IsLoaded Tests

    [Fact]
    public void IsLoaded_WhenLoaded_ReturnsTrue()
    {
        var path = testDir.CreateFile("loaded.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.True(handle.IsLoaded);
    }

    [Fact]
    public void IsLoaded_WhenInvalid_ReturnsFalse()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.False(handle.IsLoaded);
    }

    #endregion

    #region Asset Tests

    [Fact]
    public void Asset_WhenLoaded_ReturnsAsset()
    {
        var path = testDir.CreateFile("asset.txt", "test content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.NotNull(handle.Asset);
        Assert.Equal("test content", handle.Asset!.Content);
    }

    [Fact]
    public void Asset_WhenInvalid_ReturnsNull()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.Null(handle.Asset);
    }

    #endregion

    #region Path Tests

    [Fact]
    public void Path_WhenLoaded_ReturnsPath()
    {
        var path = testDir.CreateFile("path.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.Equal(path, handle.Path);
    }

    [Fact]
    public void Path_WhenInvalid_ReturnsNull()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.Null(handle.Path);
    }

    #endregion

    #region Acquire Tests

    [Fact]
    public void Acquire_ReturnsNewHandle()
    {
        var path = testDir.CreateFile("acquire.txt", "content");
        using var original = manager.Load<TestAsset>(path);
        using var acquired = original.Acquire();

        Assert.Equal(original.Id, acquired.Id);
        Assert.Same(original.Asset, acquired.Asset);
    }

    [Fact]
    public void Acquire_IncreasesRefCount()
    {
        var path = testDir.CreateFile("refcount.txt", "content");
        var original = manager.Load<TestAsset>(path);
        var acquired = original.Acquire();

        original.Dispose();

        // Asset should still be loaded because acquired holds a reference
        Assert.True(manager.IsLoaded(path));

        acquired.Dispose();
    }

    [Fact]
    public void Acquire_WhenInvalid_ThrowsInvalidOperationException()
    {
        var handle = default(AssetHandle<TestAsset>);

        Assert.Throws<InvalidOperationException>(() => handle.Acquire());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_MultipleCallsAreIdempotent()
    {
        var path = testDir.CreateFile("dispose.txt", "content");
        var handle = manager.Load<TestAsset>(path);

        handle.Dispose();
        handle.Dispose(); // Should not throw
        handle.Dispose(); // Should not throw
    }

    [Fact]
    public void Dispose_ReleasesReference()
    {
        var aggressiveManager = new AssetManager(new AssetsConfig
        {
            RootPath = testDir.RootPath,
            CachePolicy = CachePolicy.Aggressive
        });
        aggressiveManager.RegisterLoader(new TestAssetLoader());

        var path = testDir.CreateFile("aggressive.txt", "content");
        var handle = aggressiveManager.Load<TestAsset>(path);
        var asset = handle.Asset!;

        handle.Dispose();

        Assert.True(asset.IsDisposed);
        aggressiveManager.Dispose();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_SameHandle_ReturnsTrue()
    {
        var path = testDir.CreateFile("equal.txt", "content");
        using var handle1 = manager.Load<TestAsset>(path);
        using var handle2 = manager.Load<TestAsset>(path);

        Assert.True(handle1.Equals(handle2));
    }

    [Fact]
    public void Equals_DifferentHandle_ReturnsFalse()
    {
        var path1 = testDir.CreateFile("diff1.txt", "a");
        var path2 = testDir.CreateFile("diff2.txt", "b");
        using var handle1 = manager.Load<TestAsset>(path1);
        using var handle2 = manager.Load<TestAsset>(path2);

        Assert.False(handle1.Equals(handle2));
    }

    [Fact]
    public void GetHashCode_SameHandle_ReturnsSameHash()
    {
        var path = testDir.CreateFile("hash.txt", "content");
        using var handle1 = manager.Load<TestAsset>(path);
        using var handle2 = manager.Load<TestAsset>(path);

        Assert.Equal(handle1.GetHashCode(), handle2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_SameHandle_ReturnsTrue()
    {
        var path = testDir.CreateFile("op.txt", "content");
        using var handle1 = manager.Load<TestAsset>(path);
        using var handle2 = manager.Load<TestAsset>(path);

        Assert.True(handle1 == handle2);
    }

    [Fact]
    public void OperatorNotEquals_DifferentHandle_ReturnsTrue()
    {
        var path1 = testDir.CreateFile("ne1.txt", "a");
        var path2 = testDir.CreateFile("ne2.txt", "b");
        using var handle1 = manager.Load<TestAsset>(path1);
        using var handle2 = manager.Load<TestAsset>(path2);

        Assert.True(handle1 != handle2);
    }

    #endregion
}
