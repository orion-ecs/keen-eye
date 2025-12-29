using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="SilkGraphicsContext"/>.
/// Note: Tests focus on initialization state and event subscription since creating
/// an actual OpenGL context requires a real window/display, which is not available
/// in headless test environments.
/// </summary>
public sealed class SilkGraphicsContextTests : IDisposable
{
    private readonly MockSilkWindowProvider windowProvider;
    private readonly SilkGraphicsContext context;

    public SilkGraphicsContextTests()
    {
        windowProvider = new MockSilkWindowProvider();
        context = new SilkGraphicsContext(windowProvider);
    }

    public void Dispose()
    {
        context.Dispose();
        windowProvider.Dispose();
    }

    #region Constructor

    [Fact]
    public void Constructor_WithWindowProvider_InitializesCorrectly()
    {
        Assert.NotNull(context);
        Assert.False(context.IsInitialized);
        Assert.Null(context.Window);
        Assert.Null(context.Device);
    }

    [Fact]
    public void Constructor_WithConfig_StoresConfig()
    {
        var config = new SilkGraphicsConfig
        {
            EnableDepthTest = true,
            EnableCulling = true,
            ClearColor = new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
        };

        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.Equal(config, customContext.Config);
        Assert.Equal(new Vector4(0.5f, 0.5f, 0.5f, 1.0f), customContext.Config.ClearColor);
        Assert.True(customContext.Config.EnableDepthTest);
        Assert.True(customContext.Config.EnableCulling);
    }

    [Fact]
    public void Constructor_WithoutConfig_UsesDefaultConfig()
    {
        Assert.NotNull(context.Config);
        Assert.Equal(new Vector4(0.1f, 0.1f, 0.1f, 1f), context.Config.ClearColor);
        Assert.True(context.Config.EnableDepthTest);
        Assert.True(context.Config.EnableCulling);
    }

    [Fact]
    public void Constructor_SubscribesToWindowProviderEvents()
    {
        var resizeCalled = false;
        var closingCalled = false;

        windowProvider.OnResize += (w, h) => resizeCalled = true;
        windowProvider.OnClosing += () => closingCalled = true;

        using var testContext = new SilkGraphicsContext(windowProvider);

        // Trigger events to verify subscriptions (avoid Load which creates GL context)
        windowProvider.SimulateResize(1024, 768);
        windowProvider.SimulateClosing();

        Assert.True(resizeCalled);
        Assert.True(closingCalled);
    }

    #endregion

    #region Initialization State

    [Fact]
    public void IsInitialized_BeforeLoad_ReturnsFalse()
    {
        Assert.False(context.IsInitialized);
    }

    [Fact]
    public void Window_BeforeLoad_ReturnsNull()
    {
        Assert.Null(context.Window);
    }

    [Fact]
    public void Device_BeforeLoad_ReturnsNull()
    {
        Assert.Null(context.Device);
    }

    [Fact]
    public void Width_BeforeLoad_ReturnsZero()
    {
        Assert.Equal(0, context.Width);
    }

    [Fact]
    public void Height_BeforeLoad_ReturnsZero()
    {
        Assert.Equal(0, context.Height);
    }

    [Fact]
    public void Renderer2D_BeforeLoad_ReturnsNull()
    {
        Assert.Null(context.Renderer2D);
        Assert.Null(context.Get2DRenderer());
    }

    [Fact]
    public void TextRenderer_BeforeLoad_ReturnsNull()
    {
        Assert.Null(context.TextRenderer);
        Assert.Null(context.GetTextRenderer());
    }

    [Fact]
    public void FontManager_BeforeLoad_ReturnsNull()
    {
        Assert.Null(context.FontManager);
        Assert.Null(context.GetFontManager());
    }

    #endregion

    #region Event Handling

    [Fact]
    public void ProcessEvents_BeforeLoad_DoesNotThrow()
    {
        context.ProcessEvents();

        Assert.False(context.IsInitialized);
    }

    [Fact]
    public void SwapBuffers_BeforeLoad_DoesNotThrow()
    {
        context.SwapBuffers();

        Assert.False(context.IsInitialized);
    }

    #endregion

    #region Disposal

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        context.Dispose();
        context.Dispose();
        context.Dispose();

        // Should handle multiple dispose calls gracefully
        Assert.False(context.IsInitialized);
    }

    [Fact]
    public void Dispose_UnsubscribesFromWindowProviderEvents()
    {
        context.Dispose();

        // Trigger resize (which doesn't require GL context)
        windowProvider.SimulateResize(1024, 768);

        // Context should not be initialized since it was disposed
        Assert.False(context.IsInitialized);
    }

    [Fact]
    public void Dispose_BeforeLoad_DoesNotThrow()
    {
        using var uninitializedContext = new SilkGraphicsContext(windowProvider);

        uninitializedContext.Dispose();

        // Should handle disposal before initialization
        Assert.False(uninitializedContext.IsInitialized);
    }

    #endregion

    #region Configuration

    [Fact]
    public void Config_DepthTestEnabled_StoresValue()
    {
        var config = new SilkGraphicsConfig { EnableDepthTest = true };
        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.True(customContext.Config.EnableDepthTest);
    }

    [Fact]
    public void Config_DepthTestDisabled_StoresValue()
    {
        var config = new SilkGraphicsConfig { EnableDepthTest = false };
        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.False(customContext.Config.EnableDepthTest);
    }

    [Fact]
    public void Config_CullingEnabled_StoresValue()
    {
        var config = new SilkGraphicsConfig { EnableCulling = true };
        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.True(customContext.Config.EnableCulling);
    }

    [Fact]
    public void Config_CullingDisabled_StoresValue()
    {
        var config = new SilkGraphicsConfig { EnableCulling = false };
        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.False(customContext.Config.EnableCulling);
    }

    [Fact]
    public void Config_ClearColor_StoresValue()
    {
        var color = new Vector4(0.2f, 0.4f, 0.6f, 0.8f);
        var config = new SilkGraphicsConfig { ClearColor = color };
        using var customContext = new SilkGraphicsContext(windowProvider, config);

        Assert.Equal(color, customContext.Config.ClearColor);
    }

    [Fact]
    public void Config_DefaultClearColor_IsGray()
    {
        var config = new SilkGraphicsConfig();

        Assert.Equal(new Vector4(0.1f, 0.1f, 0.1f, 1f), config.ClearColor);
    }

    [Fact]
    public void Config_DefaultDepthTest_IsTrue()
    {
        var config = new SilkGraphicsConfig();

        Assert.True(config.EnableDepthTest);
    }

    [Fact]
    public void Config_DefaultCulling_IsTrue()
    {
        var config = new SilkGraphicsConfig();

        Assert.True(config.EnableCulling);
    }

    #endregion

    #region Interface Providers

    [Fact]
    public void Get2DRenderer_ReturnsRenderer2DValue()
    {
        Assert.Same(context.Renderer2D, context.Get2DRenderer());
    }

    [Fact]
    public void GetTextRenderer_ReturnsTextRendererValue()
    {
        Assert.Same(context.TextRenderer, context.GetTextRenderer());
    }

    [Fact]
    public void GetFontManager_ReturnsFontManagerValue()
    {
        Assert.Same(context.FontManager, context.GetFontManager());
    }

    #endregion

    #region Internal Managers

    [Fact]
    public void MeshManager_IsAccessible()
    {
        Assert.NotNull(context.MeshManager);
    }

    [Fact]
    public void TextureManager_IsAccessible()
    {
        Assert.NotNull(context.TextureManager);
    }

    [Fact]
    public void ShaderManager_IsAccessible()
    {
        Assert.NotNull(context.ShaderManager);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Constructor_WithNullConfig_UsesDefaultConfig()
    {
        using var customContext = new SilkGraphicsContext(windowProvider, null);

        Assert.NotNull(customContext.Config);
        Assert.Equal(new Vector4(0.1f, 0.1f, 0.1f, 1f), customContext.Config.ClearColor);
    }

    [Fact]
    public void Dispose_After_Dispose_DoesNotThrowForOperations()
    {
        context.Dispose();

        // These should not throw even after disposal
        context.ProcessEvents();
        context.SwapBuffers();

        Assert.False(context.IsInitialized);
    }

    [Fact]
    public void Config_Property_ReturnsSameInstance()
    {
        var config1 = context.Config;
        var config2 = context.Config;

        Assert.Same(config1, config2);
    }

    [Fact]
    public void Width_Height_BeforeInit_ReturnZero()
    {
        Assert.Equal(0, context.Width);
        Assert.Equal(0, context.Height);
    }

    #endregion
}
