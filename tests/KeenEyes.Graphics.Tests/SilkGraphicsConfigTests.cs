using System.Numerics;

using KeenEyes.Graphics.Silk;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="SilkGraphicsConfig"/>.
/// </summary>
public sealed class SilkGraphicsConfigTests
{
    #region Constructor and Default Values

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var config = new SilkGraphicsConfig();

        Assert.Equal(new Vector4(0.1f, 0.1f, 0.1f, 1f), config.ClearColor);
        Assert.True(config.EnableDepthTest);
        Assert.True(config.EnableCulling);
    }

    #endregion

    #region Property Setters

    [Fact]
    public void ClearColor_CanBeSet()
    {
        var config = new SilkGraphicsConfig
        {
            ClearColor = new Vector4(1f, 0f, 0f, 1f)
        };

        Assert.Equal(new Vector4(1f, 0f, 0f, 1f), config.ClearColor);
    }

    [Fact]
    public void EnableDepthTest_CanBeSet()
    {
        var config = new SilkGraphicsConfig
        {
            EnableDepthTest = false
        };

        Assert.False(config.EnableDepthTest);
    }

    [Fact]
    public void EnableCulling_CanBeSet()
    {
        var config = new SilkGraphicsConfig
        {
            EnableCulling = false
        };

        Assert.False(config.EnableCulling);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var clearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f);
        var config = new SilkGraphicsConfig
        {
            ClearColor = clearColor,
            EnableDepthTest = false,
            EnableCulling = false
        };

        Assert.Equal(clearColor, config.ClearColor);
        Assert.False(config.EnableDepthTest);
        Assert.False(config.EnableCulling);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ClearColor_AcceptsZeroAlpha()
    {
        var config = new SilkGraphicsConfig
        {
            ClearColor = new Vector4(0.5f, 0.5f, 0.5f, 0f)
        };

        Assert.Equal(0f, config.ClearColor.W);
    }

    [Fact]
    public void ClearColor_AcceptsValuesOutsideNormalRange()
    {
        var config = new SilkGraphicsConfig
        {
            ClearColor = new Vector4(2f, -1f, 1.5f, 3f)
        };

        Assert.Equal(new Vector4(2f, -1f, 1.5f, 3f), config.ClearColor);
    }

    #endregion
}
