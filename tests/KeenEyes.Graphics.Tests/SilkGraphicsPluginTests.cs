using KeenEyes;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Graphics.Silk;
using KeenEyes.Graphics.Tests.Mocks;
using KeenEyes.Platform.Silk;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for <see cref="SilkGraphicsPlugin"/>.
/// </summary>
public sealed class SilkGraphicsPluginTests
{
    #region Constructor

    [Fact]
    public void Constructor_WithConfig_SetsName()
    {
        var config = new SilkGraphicsConfig();
        var plugin = new SilkGraphicsPlugin(config);

        Assert.Equal("SilkGraphics", plugin.Name);
    }

    [Fact]
    public void Constructor_WithoutConfig_UsesDefaultConfig()
    {
        var plugin = new SilkGraphicsPlugin();

        Assert.Equal("SilkGraphics", plugin.Name);
    }

    #endregion

    #region Install

    [Fact]
    public void Install_WithoutWindowProvider_ThrowsInvalidOperationException()
    {
        var plugin = new SilkGraphicsPlugin();
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() => world.InstallPlugin(plugin));
        Assert.Contains("SilkWindowPlugin", exception.Message);
        Assert.Contains("installed first", exception.Message);
    }

    [Fact]
    public void Install_WithWindowProvider_RegistersExtensions()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin = new SilkGraphicsPlugin();
        world.InstallPlugin(plugin);

        // Verify all expected extensions are registered
        Assert.True(world.TryGetExtension<IGraphicsContext>(out _));
        Assert.True(world.TryGetExtension<I2DRendererProvider>(out _));
        Assert.True(world.TryGetExtension<ITextRendererProvider>(out _));
        Assert.True(world.TryGetExtension<IFontManagerProvider>(out _));
        Assert.True(world.TryGetExtension<SilkGraphicsContext>(out _));
    }

    [Fact]
    public void Install_WithWindowProvider_RegistersSameContextForAllInterfaces()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin = new SilkGraphicsPlugin();
        world.InstallPlugin(plugin);

        world.TryGetExtension<IGraphicsContext>(out var graphicsContext);
        world.TryGetExtension<I2DRendererProvider>(out var rendererProvider);
        world.TryGetExtension<ITextRendererProvider>(out var textProvider);
        world.TryGetExtension<IFontManagerProvider>(out var fontProvider);
        world.TryGetExtension<SilkGraphicsContext>(out var concreteContext);

        // All should reference the same instance
        Assert.Same(concreteContext, graphicsContext);
        Assert.Same(concreteContext, rendererProvider);
        Assert.Same(concreteContext, textProvider);
        Assert.Same(concreteContext, fontProvider);
    }

    #endregion

    #region Uninstall

    [Fact]
    public void Uninstall_RemovesAllExtensions()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin = new SilkGraphicsPlugin();
        world.InstallPlugin(plugin);
        world.UninstallPlugin<SilkGraphicsPlugin>();

        // Verify all extensions are removed
        Assert.False(world.TryGetExtension<IGraphicsContext>(out _));
        Assert.False(world.TryGetExtension<I2DRendererProvider>(out _));
        Assert.False(world.TryGetExtension<ITextRendererProvider>(out _));
        Assert.False(world.TryGetExtension<IFontManagerProvider>(out _));
        Assert.False(world.TryGetExtension<SilkGraphicsContext>(out _));
    }

    [Fact]
    public void Uninstall_DoesNotRemoveWindowProviderExtensions()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin = new SilkGraphicsPlugin();
        world.InstallPlugin(plugin);
        world.UninstallPlugin<SilkGraphicsPlugin>();

        // Window provider should still be present (owned by SilkWindowPlugin)
        Assert.True(world.TryGetExtension<ISilkWindowProvider>(out _));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Install_ThenUninstall_ThenReinstall_Succeeds()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin = new SilkGraphicsPlugin();

        // Install
        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<IGraphicsContext>(out _));

        // Uninstall
        world.UninstallPlugin<SilkGraphicsPlugin>();
        Assert.False(world.TryGetExtension<IGraphicsContext>(out _));

        // Reinstall
        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<IGraphicsContext>(out _));
    }

    [Fact]
    public void MultiplePluginInstances_CannotBeInstalledSimultaneously()
    {
        var windowProvider = new MockSilkWindowProvider();
        using var world = new World();
        world.SetExtension<ISilkWindowProvider>(windowProvider);

        var plugin1 = new SilkGraphicsPlugin();
        var plugin2 = new SilkGraphicsPlugin();

        world.InstallPlugin(plugin1);

        // Second install with same name should throw
        var exception = Assert.Throws<InvalidOperationException>(() => world.InstallPlugin(plugin2));
        Assert.Contains("already installed", exception.Message);
    }

    #endregion
}
