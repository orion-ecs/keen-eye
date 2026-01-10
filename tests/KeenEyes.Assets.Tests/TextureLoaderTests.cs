using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Assets.Tests;

/// <summary>
/// Tests for the texture loader.
/// </summary>
public class TextureLoaderTests
{
    [Fact]
    public void TextureLoader_Extensions_ContainsPng()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".png", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsJpg()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".jpg", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsJpeg()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".jpeg", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsBmp()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".bmp", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsTga()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".tga", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsGif()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".gif", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_ContainsWebP()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Contains(".webp", loader.Extensions);
    }

    [Fact]
    public void TextureLoader_Extensions_HasNineFormats()
    {
        var graphics = new MockGraphicsContext();
        var loader = new TextureLoader(graphics);

        Assert.Equal(9, loader.Extensions.Count);
    }

    [Fact]
    public void TextureLoader_Constructor_ThrowsOnNullGraphicsContext()
    {
        Assert.Throws<ArgumentNullException>(() => new TextureLoader(null!));
    }
}
