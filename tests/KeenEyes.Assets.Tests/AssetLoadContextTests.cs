namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the <see cref="AssetLoadContext"/> record struct.
/// </summary>
public class AssetLoadContextTests : IDisposable
{
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public AssetLoadContextTests()
    {
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    [Fact]
    public void Constructor_SetsPath()
    {
        var context = new AssetLoadContext("test/path.txt", manager);

        Assert.Equal("test/path.txt", context.Path);
    }

    [Fact]
    public void Constructor_SetsManager()
    {
        var context = new AssetLoadContext("test.txt", manager);

        Assert.Same(manager, context.Manager);
    }

    [Fact]
    public void Constructor_ServicesDefaultsToNull()
    {
        var context = new AssetLoadContext("test.txt", manager);

        Assert.Null(context.Services);
    }

    [Fact]
    public void Constructor_WithServices_SetsServices()
    {
        var services = new TestServiceProvider();
        var context = new AssetLoadContext("test.txt", manager, services);

        Assert.Same(services, context.Services);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var context1 = new AssetLoadContext("test.txt", manager);
        var context2 = new AssetLoadContext("test.txt", manager);

        Assert.Equal(context1, context2);
        Assert.True(context1 == context2);
    }

    [Fact]
    public void Equality_DifferentPath_NotEqual()
    {
        var context1 = new AssetLoadContext("test1.txt", manager);
        var context2 = new AssetLoadContext("test2.txt", manager);

        Assert.NotEqual(context1, context2);
        Assert.True(context1 != context2);
    }

    [Fact]
    public void GetHashCode_SameForEqualContexts()
    {
        var context1 = new AssetLoadContext("test.txt", manager);
        var context2 = new AssetLoadContext("test.txt", manager);

        Assert.Equal(context1.GetHashCode(), context2.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsPath()
    {
        var context = new AssetLoadContext("assets/texture.png", manager);

        var str = context.ToString();

        Assert.Contains("Path", str);
        Assert.Contains("assets/texture.png", str);
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
