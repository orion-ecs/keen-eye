using KeenEyes.Graphics.Silk.Resources;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="TextureFilter"/> and <see cref="TextureWrap"/> enums.
/// </summary>
public sealed class TextureEnumsTests
{
    #region TextureFilter Tests

    [Fact]
    public void TextureFilter_HasNearestValue()
    {
        var filter = TextureFilter.Nearest;
        Assert.True(Enum.IsDefined(typeof(TextureFilter), filter));
    }

    [Fact]
    public void TextureFilter_HasLinearValue()
    {
        var filter = TextureFilter.Linear;
        Assert.True(Enum.IsDefined(typeof(TextureFilter), filter));
    }

    [Fact]
    public void TextureFilter_HasTrilinearValue()
    {
        var filter = TextureFilter.Trilinear;
        Assert.True(Enum.IsDefined(typeof(TextureFilter), filter));
    }

    [Fact]
    public void TextureFilter_HasExactlyThreeValues()
    {
        var values = Enum.GetValues<TextureFilter>();
        Assert.Equal(3, values.Length);
    }

    [Fact]
    public void TextureFilter_Nearest_HasCorrectUnderlyingValue()
    {
        Assert.Equal(0, (int)TextureFilter.Nearest);
    }

    [Fact]
    public void TextureFilter_Linear_HasCorrectUnderlyingValue()
    {
        Assert.Equal(1, (int)TextureFilter.Linear);
    }

    [Fact]
    public void TextureFilter_Trilinear_HasCorrectUnderlyingValue()
    {
        Assert.Equal(2, (int)TextureFilter.Trilinear);
    }

    [Fact]
    public void TextureFilter_AllValues_CanBeEnumerated()
    {
        var values = Enum.GetValues<TextureFilter>();
        Assert.Contains(TextureFilter.Nearest, values);
        Assert.Contains(TextureFilter.Linear, values);
        Assert.Contains(TextureFilter.Trilinear, values);
    }

    [Fact]
    public void TextureFilter_CanBeUsedInSwitch()
    {
        var filter = TextureFilter.Linear;
        var result = filter switch
        {
            TextureFilter.Nearest => "nearest",
            TextureFilter.Linear => "linear",
            TextureFilter.Trilinear => "trilinear",
            _ => "unknown"
        };

        Assert.Equal("linear", result);
    }

    #endregion

    #region TextureWrap Tests

    [Fact]
    public void TextureWrap_HasRepeatValue()
    {
        var wrap = TextureWrap.Repeat;
        Assert.True(Enum.IsDefined(typeof(TextureWrap), wrap));
    }

    [Fact]
    public void TextureWrap_HasMirroredRepeatValue()
    {
        var wrap = TextureWrap.MirroredRepeat;
        Assert.True(Enum.IsDefined(typeof(TextureWrap), wrap));
    }

    [Fact]
    public void TextureWrap_HasClampToEdgeValue()
    {
        var wrap = TextureWrap.ClampToEdge;
        Assert.True(Enum.IsDefined(typeof(TextureWrap), wrap));
    }

    [Fact]
    public void TextureWrap_HasClampToBorderValue()
    {
        var wrap = TextureWrap.ClampToBorder;
        Assert.True(Enum.IsDefined(typeof(TextureWrap), wrap));
    }

    [Fact]
    public void TextureWrap_HasExactlyFourValues()
    {
        var values = Enum.GetValues<TextureWrap>();
        Assert.Equal(4, values.Length);
    }

    [Fact]
    public void TextureWrap_Repeat_HasCorrectUnderlyingValue()
    {
        Assert.Equal(0, (int)TextureWrap.Repeat);
    }

    [Fact]
    public void TextureWrap_MirroredRepeat_HasCorrectUnderlyingValue()
    {
        Assert.Equal(1, (int)TextureWrap.MirroredRepeat);
    }

    [Fact]
    public void TextureWrap_ClampToEdge_HasCorrectUnderlyingValue()
    {
        Assert.Equal(2, (int)TextureWrap.ClampToEdge);
    }

    [Fact]
    public void TextureWrap_ClampToBorder_HasCorrectUnderlyingValue()
    {
        Assert.Equal(3, (int)TextureWrap.ClampToBorder);
    }

    [Fact]
    public void TextureWrap_AllValues_CanBeEnumerated()
    {
        var values = Enum.GetValues<TextureWrap>();
        Assert.Contains(TextureWrap.Repeat, values);
        Assert.Contains(TextureWrap.MirroredRepeat, values);
        Assert.Contains(TextureWrap.ClampToEdge, values);
        Assert.Contains(TextureWrap.ClampToBorder, values);
    }

    [Fact]
    public void TextureWrap_CanBeUsedInSwitch()
    {
        var wrap = TextureWrap.ClampToEdge;
        var result = wrap switch
        {
            TextureWrap.Repeat => "repeat",
            TextureWrap.MirroredRepeat => "mirrored",
            TextureWrap.ClampToEdge => "clamp_edge",
            TextureWrap.ClampToBorder => "clamp_border",
            _ => "unknown"
        };

        Assert.Equal("clamp_edge", result);
    }

    #endregion

    #region Enum Validation

    [Fact]
    public void TextureFilter_InvalidValue_IsNotDefined()
    {
        var invalidFilter = (TextureFilter)999;
        Assert.False(Enum.IsDefined(typeof(TextureFilter), invalidFilter));
    }

    [Fact]
    public void TextureWrap_InvalidValue_IsNotDefined()
    {
        var invalidWrap = (TextureWrap)999;
        Assert.False(Enum.IsDefined(typeof(TextureWrap), invalidWrap));
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void TextureFilter_ToString_ReturnsName()
    {
        Assert.Equal("Nearest", TextureFilter.Nearest.ToString());
        Assert.Equal("Linear", TextureFilter.Linear.ToString());
        Assert.Equal("Trilinear", TextureFilter.Trilinear.ToString());
    }

    [Fact]
    public void TextureWrap_ToString_ReturnsName()
    {
        Assert.Equal("Repeat", TextureWrap.Repeat.ToString());
        Assert.Equal("MirroredRepeat", TextureWrap.MirroredRepeat.ToString());
        Assert.Equal("ClampToEdge", TextureWrap.ClampToEdge.ToString());
        Assert.Equal("ClampToBorder", TextureWrap.ClampToBorder.ToString());
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void TextureFilter_CanBeParsedFromString()
    {
        var filter = Enum.Parse<TextureFilter>("Linear");
        Assert.Equal(TextureFilter.Linear, filter);
    }

    [Fact]
    public void TextureWrap_CanBeParsedFromString()
    {
        var wrap = Enum.Parse<TextureWrap>("ClampToEdge");
        Assert.Equal(TextureWrap.ClampToEdge, wrap);
    }

    #endregion
}
