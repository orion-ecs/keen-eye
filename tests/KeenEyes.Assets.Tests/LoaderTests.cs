namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for built-in asset loaders.
/// </summary>
public class LoaderTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public LoaderTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    #region RawLoader Tests

    [Fact]
    public void RawLoader_Extensions_ContainsBin()
    {
        var loader = new RawLoader();

        Assert.Contains(".bin", loader.Extensions);
    }

    [Fact]
    public void RawLoader_Extensions_ContainsDat()
    {
        var loader = new RawLoader();

        Assert.Contains(".dat", loader.Extensions);
    }

    [Fact]
    public void RawLoader_Extensions_ContainsBytes()
    {
        var loader = new RawLoader();

        Assert.Contains(".bytes", loader.Extensions);
    }

    [Fact]
    public void RawLoader_Load_ReturnsCorrectData()
    {
        manager.RegisterLoader(new RawLoader());
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0xFF };
        var path = testDir.CreateFile("data.bin", data);

        using var handle = manager.Load<RawAsset>(path);

        Assert.Equal(data, handle.Asset!.Data);
    }

    [Fact]
    public async Task RawLoader_LoadAsync_ReturnsCorrectData()
    {
        manager.RegisterLoader(new RawLoader());
        var data = new byte[] { 0xAA, 0xBB, 0xCC };
        var path = testDir.CreateFile("async.bin", data);

        using var handle = await manager.LoadAsync<RawAsset>(path);

        Assert.Equal(data, handle.Asset!.Data);
    }

    [Fact]
    public void RawLoader_EstimateSize_ReturnsDataLength()
    {
        var loader = new RawLoader();
        using var asset = new RawAsset([1, 2, 3, 4, 5]);

        var size = loader.EstimateSize(asset);

        Assert.Equal(5, size);
    }

    #endregion

    #region IAssetLoader Interface Tests

    [Fact]
    public void TestAssetLoader_Extensions_ReturnsExpectedValues()
    {
        var loader = new TestAssetLoader();

        Assert.Contains(".txt", loader.Extensions);
        Assert.Contains(".test", loader.Extensions);
    }

    [Fact]
    public void TestAssetLoader_Load_ReturnsCorrectContent()
    {
        manager.RegisterLoader(new TestAssetLoader());
        var path = testDir.CreateFile("content.txt", "Hello, Test!");

        using var handle = manager.Load<TestAsset>(path);

        Assert.Equal("Hello, Test!", handle.Asset!.Content);
    }

    [Fact]
    public async Task TestAssetLoader_LoadAsync_ReturnsCorrectContent()
    {
        manager.RegisterLoader(new TestAssetLoader());
        var path = testDir.CreateFile("async.txt", "Async Content");

        using var handle = await manager.LoadAsync<TestAsset>(path);

        Assert.Equal("Async Content", handle.Asset!.Content);
    }

    [Fact]
    public void TestAssetLoader_EstimateSize_ReturnsApproximateSize()
    {
        var loader = new TestAssetLoader();
        using var asset = new TestAsset("Hello");

        var size = loader.EstimateSize(asset);

        Assert.Equal(10, size); // 5 chars * 2 bytes per char
    }

    #endregion

    #region Loader Registration Tests

    [Fact]
    public void RegisterLoader_AllowsLoadingWithRegisteredExtension()
    {
        manager.RegisterLoader(new TestAssetLoader());
        var path = testDir.CreateFile("file.txt", "content");

        using var handle = manager.Load<TestAsset>(path);

        Assert.True(handle.IsLoaded);
    }

    [Fact]
    public void RegisterLoader_NullLoader_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            manager.RegisterLoader<TestAsset>(null!));
    }

    [Fact]
    public void RegisterLoader_WithMultipleLoaderTypes_LoadsBothCorrectly()
    {
        manager.RegisterLoader(new TestAssetLoader());
        manager.RegisterLoader(new SecondTestAssetLoader());

        var txtPath = testDir.CreateFile("a.txt", "text");
        var numPath = testDir.CreateFile("b.num", "42");

        using var txtHandle = manager.Load<TestAsset>(txtPath);
        using var numHandle = manager.Load<SecondTestAsset>(numPath);

        Assert.Equal("text", txtHandle.Asset!.Content);
        Assert.Equal(42, numHandle.Asset!.Value);
    }

    [Fact]
    public void RegisterLoader_WithDuplicateLoader_ReplacesExistingLoader()
    {
        manager.RegisterLoader(new TestAssetLoader());
        manager.RegisterLoader(new TestAssetLoader()); // Register again

        var path = testDir.CreateFile("override.txt", "content");
        using var handle = manager.Load<TestAsset>(path);

        Assert.True(handle.IsLoaded);
    }

    #endregion

    #region Loader Error Handling Tests

    [Fact]
    public void Load_WithFailingLoader_ThrowsAssetLoadException()
    {
        manager.RegisterLoader(new FailingLoader());
        testDir.CreateFile("fail.fail", "content");

        var ex = Assert.Throws<AssetLoadException>(() =>
            manager.Load<TestAsset>("fail.fail"));

        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public async Task LoadAsync_WithFailingLoader_ThrowsAssetLoadException()
    {
        manager.RegisterLoader(new FailingLoader());
        testDir.CreateFile("fail.fail", "content");

        var ex = await Assert.ThrowsAsync<AssetLoadException>(
            () => manager.LoadAsync<TestAsset>("fail.fail"));

        Assert.NotNull(ex.InnerException);
    }

    #endregion
}
