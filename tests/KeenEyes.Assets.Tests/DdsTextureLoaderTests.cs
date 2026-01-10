using KeenEyes.Testing.Graphics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the DDS texture loader.
/// </summary>
public class DdsTextureLoaderTests
{
    [Fact]
    public void DdsTextureLoader_Extensions_ContainsDds()
    {
        var graphics = new MockGraphicsContext();
        var loader = new DdsTextureLoader(graphics);

        Assert.Contains(".dds", loader.Extensions);
    }

    [Fact]
    public void DdsTextureLoader_Extensions_HasOneFormat()
    {
        var graphics = new MockGraphicsContext();
        var loader = new DdsTextureLoader(graphics);

        Assert.Single(loader.Extensions);
    }

    [Fact]
    public void DdsTextureLoader_Constructor_ThrowsOnNullGraphicsContext()
    {
        Assert.Throws<ArgumentNullException>(() => new DdsTextureLoader(null!));
    }

    [Fact]
    public void DdsTextureLoader_EstimateSize_ReturnsAssetSizeBytes()
    {
        var graphics = new MockGraphicsContext();
        var loader = new DdsTextureLoader(graphics);

        // Create a mock texture asset with known size
        var textureHandle = graphics.CreateTexture(64, 64, new byte[64 * 64 * 4]);
        var asset = new TextureAsset(textureHandle, 64, 64, TextureFormat.Rgba8, graphics);

        var estimatedSize = loader.EstimateSize(asset);

        Assert.Equal(asset.SizeBytes, estimatedSize);
    }
}
