using System.Numerics;
using KeenEyes.Graphics;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the GraphicsConfig class.
/// </summary>
public class GraphicsConfigTests
{
    private const float Epsilon = 1e-5f;

    #region Default Values Tests

    [Fact]
    public void Default_WindowWidth_Is1280()
    {
        var config = new GraphicsConfig();

        Assert.Equal(1280, config.WindowWidth);
    }

    [Fact]
    public void Default_WindowHeight_Is720()
    {
        var config = new GraphicsConfig();

        Assert.Equal(720, config.WindowHeight);
    }

    [Fact]
    public void Default_WindowTitle_IsKeenEyesApplication()
    {
        var config = new GraphicsConfig();

        Assert.Equal("KeenEyes Application", config.WindowTitle);
    }

    [Fact]
    public void Default_VSync_IsTrue()
    {
        var config = new GraphicsConfig();

        Assert.True(config.VSync);
    }

    [Fact]
    public void Default_Resizable_IsTrue()
    {
        var config = new GraphicsConfig();

        Assert.True(config.Resizable);
    }

    [Fact]
    public void Default_Fullscreen_IsFalse()
    {
        var config = new GraphicsConfig();

        Assert.False(config.Fullscreen);
    }

    [Fact]
    public void Default_TargetFps_IsZero()
    {
        var config = new GraphicsConfig();

        Assert.Equal(0d, config.TargetFps);
    }

    [Fact]
    public void Default_ClearColor_IsDarkGray()
    {
        var config = new GraphicsConfig();

        Assert.Equal(0.1f, config.ClearColor.X, Epsilon);
        Assert.Equal(0.1f, config.ClearColor.Y, Epsilon);
        Assert.Equal(0.1f, config.ClearColor.Z, Epsilon);
        Assert.Equal(1f, config.ClearColor.W, Epsilon);
    }

    #endregion

    #region Init Properties Tests

    [Fact]
    public void WindowWidth_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { WindowWidth = 1920 };

        Assert.Equal(1920, config.WindowWidth);
    }

    [Fact]
    public void WindowHeight_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { WindowHeight = 1080 };

        Assert.Equal(1080, config.WindowHeight);
    }

    [Fact]
    public void WindowTitle_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { WindowTitle = "My Game" };

        Assert.Equal("My Game", config.WindowTitle);
    }

    [Fact]
    public void VSync_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { VSync = false };

        Assert.False(config.VSync);
    }

    [Fact]
    public void Resizable_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { Resizable = false };

        Assert.False(config.Resizable);
    }

    [Fact]
    public void Fullscreen_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { Fullscreen = true };

        Assert.True(config.Fullscreen);
    }

    [Fact]
    public void TargetFps_CanBeSetViaInit()
    {
        var config = new GraphicsConfig { TargetFps = 60 };

        Assert.Equal(60d, config.TargetFps);
    }

    [Fact]
    public void ClearColor_CanBeSetViaInit()
    {
        var clearColor = new Vector4(0.2f, 0.3f, 0.4f, 1f);
        var config = new GraphicsConfig { ClearColor = clearColor };

        Assert.Equal(clearColor, config.ClearColor);
    }

    #endregion

    #region Combined Configuration Tests

    [Fact]
    public void Config_MultiplePropertiesCanBeSet()
    {
        var config = new GraphicsConfig
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            WindowTitle = "Test Game",
            VSync = false,
            Fullscreen = true,
            TargetFps = 144
        };

        Assert.Equal(1920, config.WindowWidth);
        Assert.Equal(1080, config.WindowHeight);
        Assert.Equal("Test Game", config.WindowTitle);
        Assert.False(config.VSync);
        Assert.True(config.Fullscreen);
        Assert.Equal(144d, config.TargetFps);
    }

    [Fact]
    public void Config_UnsetPropertiesRetainDefaults()
    {
        var config = new GraphicsConfig { WindowWidth = 800 };

        // Only WindowWidth changed, others should be default
        Assert.Equal(800, config.WindowWidth);
        Assert.Equal(720, config.WindowHeight);
        Assert.Equal("KeenEyes Application", config.WindowTitle);
        Assert.True(config.VSync);
    }

    #endregion
}
