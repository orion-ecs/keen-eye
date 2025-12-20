using KeenEyes.Graphics.Abstractions;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the BlendModeExtensions class.
/// </summary>
public class BlendModeExtensionsTests
{
    [Fact]
    public void ToBlendFactors_Transparent_ReturnsSrcAlphaAndOneMinusSrcAlpha()
    {
        var (src, dst) = BlendMode.Transparent.ToBlendFactors();

        Assert.Equal(BlendFactor.SrcAlpha, src);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, dst);
    }

    [Fact]
    public void ToBlendFactors_Additive_ReturnsSrcAlphaAndOne()
    {
        var (src, dst) = BlendMode.Additive.ToBlendFactors();

        Assert.Equal(BlendFactor.SrcAlpha, src);
        Assert.Equal(BlendFactor.One, dst);
    }

    [Fact]
    public void ToBlendFactors_Multiply_ReturnsDstColorAndZero()
    {
        var (src, dst) = BlendMode.Multiply.ToBlendFactors();

        Assert.Equal(BlendFactor.DstColor, src);
        Assert.Equal(BlendFactor.Zero, dst);
    }

    [Fact]
    public void ToBlendFactors_Premultiplied_ReturnsOneAndOneMinusSrcAlpha()
    {
        var (src, dst) = BlendMode.Premultiplied.ToBlendFactors();

        Assert.Equal(BlendFactor.One, src);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, dst);
    }

    [Fact]
    public void ToBlendFactors_UnknownValue_ReturnsDefaultTransparent()
    {
        // Cast an invalid value to BlendMode
        var invalidMode = (BlendMode)999;

        var (src, dst) = invalidMode.ToBlendFactors();

        // Should default to Transparent
        Assert.Equal(BlendFactor.SrcAlpha, src);
        Assert.Equal(BlendFactor.OneMinusSrcAlpha, dst);
    }

    [Theory]
    [InlineData(BlendMode.Transparent)]
    [InlineData(BlendMode.Additive)]
    [InlineData(BlendMode.Multiply)]
    [InlineData(BlendMode.Premultiplied)]
    public void ToBlendFactors_AllModes_ReturnValidFactors(BlendMode mode)
    {
        var (src, dst) = mode.ToBlendFactors();

        // Both factors should be valid enum values
        Assert.True(Enum.IsDefined(typeof(BlendFactor), src));
        Assert.True(Enum.IsDefined(typeof(BlendFactor), dst));
    }
}
