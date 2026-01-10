using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the font loader.
/// </summary>
public class FontLoaderTests : IDisposable
{
    private readonly MockFontManager fontManager;
    private readonly TestAssetDirectory testDir;
    private readonly AssetManager manager;

    public FontLoaderTests()
    {
        fontManager = new MockFontManager();
        testDir = new TestAssetDirectory();
        manager = new AssetManager(new AssetsConfig { RootPath = testDir.RootPath });
        manager.RegisterLoader(new FontLoader(fontManager));
    }

    public void Dispose()
    {
        manager.Dispose();
        testDir.Dispose();
    }

    [Fact]
    public void FontLoader_Extensions_ContainsTtf()
    {
        var loader = new FontLoader(fontManager);

        Assert.Contains(".ttf", loader.Extensions);
    }

    [Fact]
    public void FontLoader_Extensions_ContainsOtf()
    {
        var loader = new FontLoader(fontManager);

        Assert.Contains(".otf", loader.Extensions);
    }

    [Fact]
    public void FontLoader_Extensions_HasTwoFormats()
    {
        var loader = new FontLoader(fontManager);

        Assert.Equal(2, loader.Extensions.Count);
    }

    [Fact]
    public void FontLoader_Constructor_ThrowsOnNullFontManager()
    {
        Assert.Throws<ArgumentNullException>(() => new FontLoader(null!));
    }

    [Fact]
    public void FontLoader_Load_CreatesFontAsset()
    {
        // Create a minimal font data stream (just needs to be non-empty for mock)
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = manager.Load<FontAsset>(path);

        Assert.NotNull(handle.Asset);
        Assert.True(handle.Asset!.Handle.IsValid);
        Assert.Equal("TestFont", handle.Asset.FamilyName);
        Assert.Equal(16f, handle.Asset.DefaultSize);
    }

    [Fact]
    public void FontLoader_Load_StoresFontDataInAsset()
    {
        var fontData = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = manager.Load<FontAsset>(path);

        // SizeBytes should include font data size plus overhead
        Assert.True(handle.Asset!.SizeBytes >= fontData.Length);
    }

    [Fact]
    public void FontAsset_GetSized_CreatesNewFontHandle()
    {
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = manager.Load<FontAsset>(path);
        var largeFont = handle.Asset!.GetSized(32f);

        Assert.True(largeFont.IsValid);
        Assert.NotEqual(handle.Asset.Handle.Id, largeFont.Id);
        Assert.Equal(32f, fontManager.GetFontSize(largeFont));
    }

    [Fact]
    public void FontAsset_LineHeight_ReturnsFromFontManager()
    {
        fontManager.DefaultLineHeight = 20f;
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = manager.Load<FontAsset>(path);

        Assert.Equal(20f, handle.Asset!.LineHeight);
    }

    [Fact]
    public void FontAsset_Dispose_ReleasesFontHandle()
    {
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        var handle = manager.Load<FontAsset>(path);
        var fontHandle = handle.Asset!.Handle;
        var asset = handle.Asset;

        Assert.True(fontManager.IsValid(fontHandle));

        // Dispose handle (decrements refcount)
        handle.Dispose();

        // Force unload from manager to actually dispose asset
        manager.Unload(path);

        // Now the asset should be disposed
        Assert.False(fontManager.IsValid(fontHandle));
    }

    [Fact]
    public void FontLoader_Load_ExtractsFamilyNameFromPath()
    {
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };

        // Test with different path formats
        var testCases = new[]
        {
            ("fonts/Roboto-Regular.ttf", "Roboto-Regular"),
            ("OpenSans.otf", "OpenSans"),
            ("assets/fonts/subdir/Arial-Bold.ttf", "Arial-Bold")
        };

        foreach (var (fontPath, expectedName) in testCases)
        {
            var path = testDir.CreateFile(fontPath, fontData);

            using var handle = manager.Load<FontAsset>(path);

            Assert.Equal(expectedName, handle.Asset!.FamilyName);
        }
    }

    [Fact]
    public async Task FontLoader_LoadAsync_CreatesFontAsset()
    {
        var fontData = new byte[] { 0x00, 0x01, 0x00, 0x00 };
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = await manager.LoadAsync<FontAsset>(path);

        Assert.NotNull(handle.Asset);
        Assert.True(handle.Asset!.Handle.IsValid);
        Assert.Equal("TestFont", handle.Asset.FamilyName);
    }

    [Fact]
    public void FontLoader_EstimateSize_ReturnsFontAssetSize()
    {
        var fontData = new byte[1024];
        var path = testDir.CreateFile("fonts/TestFont.ttf", fontData);

        using var handle = manager.Load<FontAsset>(path);

        var loader = new FontLoader(fontManager);
        var estimatedSize = loader.EstimateSize(handle.Asset!);

        Assert.Equal(handle.Asset!.SizeBytes, estimatedSize);
        Assert.True(estimatedSize >= 1024); // At least the font data size
    }
}
