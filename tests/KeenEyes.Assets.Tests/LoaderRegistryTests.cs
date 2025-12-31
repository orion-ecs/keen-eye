namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the <see cref="LoaderRegistry"/> class.
/// </summary>
public class LoaderRegistryTests
{
    [Fact]
    public void Register_AddsLoaderForExtensions()
    {
        var registry = new LoaderRegistry();
        var loader = new TestAssetLoader();

        registry.Register(loader);

        Assert.True(registry.HasLoader(".txt"));
        Assert.True(registry.HasLoader(".test"));
    }

    [Fact]
    public void Register_NormalizesExtensions()
    {
        var registry = new LoaderRegistry();
        var loader = new TestAssetLoader();

        registry.Register(loader);

        // Should work with or without leading dot
        Assert.True(registry.HasLoader("txt"));
        Assert.True(registry.HasLoader(".txt"));
        Assert.True(registry.HasLoader("TXT")); // Case insensitive
    }

    [Fact]
    public void GetLoader_ByExtension_ReturnsRegisteredLoader()
    {
        var registry = new LoaderRegistry();
        var loader = new TestAssetLoader();
        registry.Register(loader);

        var result = registry.GetLoader<TestAsset>(".txt");

        Assert.Same(loader, result);
    }

    [Fact]
    public void GetLoader_ByType_ReturnsRegisteredLoader()
    {
        var registry = new LoaderRegistry();
        var loader = new TestAssetLoader();
        registry.Register(loader);

        var result = registry.GetLoader<TestAsset>();

        Assert.Same(loader, result);
    }

    [Fact]
    public void GetLoader_UnregisteredExtension_ReturnsNull()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        var result = registry.GetLoader<TestAsset>(".unknown");

        Assert.Null(result);
    }

    [Fact]
    public void GetLoader_UnregisteredType_ReturnsNull()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        var result = registry.GetLoader<SecondTestAsset>();

        Assert.Null(result);
    }

    [Fact]
    public void GetLoader_WrongTypeForExtension_ReturnsNull()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        // .txt is registered for TestAsset, not SecondTestAsset
        var result = registry.GetLoader<SecondTestAsset>(".txt");

        Assert.Null(result);
    }

    [Fact]
    public void HasLoader_RegisteredExtension_ReturnsTrue()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        Assert.True(registry.HasLoader(".txt"));
    }

    [Fact]
    public void HasLoader_UnregisteredExtension_ReturnsFalse()
    {
        var registry = new LoaderRegistry();

        Assert.False(registry.HasLoader(".unknown"));
    }

    [Fact]
    public void HasLoader_EmptyExtension_ReturnsFalse()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        Assert.False(registry.HasLoader(""));
    }

    [Fact]
    public void GetSupportedExtensions_ReturnsAllRegisteredExtensions()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());
        registry.Register(new SecondTestAssetLoader());

        var extensions = registry.GetSupportedExtensions().ToList();

        Assert.Contains(".txt", extensions);
        Assert.Contains(".test", extensions);
        Assert.Contains(".num", extensions);
    }

    [Fact]
    public void GetLoadDelegate_RegisteredType_ReturnsDelegate()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        var del = registry.GetLoadDelegate(typeof(TestAsset));

        Assert.NotNull(del);
    }

    [Fact]
    public void GetLoadDelegate_UnregisteredType_ReturnsNull()
    {
        var registry = new LoaderRegistry();

        var del = registry.GetLoadDelegate(typeof(TestAsset));

        Assert.Null(del);
    }

    [Fact]
    public async Task GetLoadDelegate_Invoked_LoadsAsset()
    {
        var registry = new LoaderRegistry();
        registry.Register(new TestAssetLoader());

        var del = registry.GetLoadDelegate(typeof(TestAsset));
        Assert.NotNull(del);

        using var stream = new MemoryStream("Test Content"u8.ToArray());
        var context = new AssetLoadContext("test.txt", null!, null);

        var (asset, size) = await del(stream, context, CancellationToken.None);

        Assert.IsType<TestAsset>(asset);
        Assert.Equal("Test Content", ((TestAsset)asset).Content);
        Assert.True(size > 0);

        ((IDisposable)asset).Dispose();
    }

    [Fact]
    public void Register_MultipleLoaders_AllAccessible()
    {
        var registry = new LoaderRegistry();
        var loader1 = new TestAssetLoader();
        var loader2 = new SecondTestAssetLoader();

        registry.Register(loader1);
        registry.Register(loader2);

        Assert.Same(loader1, registry.GetLoader<TestAsset>());
        Assert.Same(loader2, registry.GetLoader<SecondTestAsset>());
    }
}
