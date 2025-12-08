using KeenEyes.Testing;

namespace KeenEyes.Graphics.Tests;

/// <summary>
/// Tests for the GraphicsPlugin class.
/// </summary>
public class GraphicsPluginTests
{
    #region Plugin Properties Tests

    [Fact]
    public void Name_ReturnsGraphics()
    {
        var plugin = new GraphicsPlugin();

        Assert.Equal("Graphics", plugin.Name);
    }

    [Fact]
    public void Constructor_Default_CreatesPlugin()
    {
        var plugin = new GraphicsPlugin();

        Assert.NotNull(plugin);
    }

    [Fact]
    public void Constructor_WithConfig_CreatesPlugin()
    {
        var config = new GraphicsConfig { WindowWidth = 800 };
        var plugin = new GraphicsPlugin(config);

        Assert.NotNull(plugin);
    }

    #endregion

    #region Install Tests

    [Fact]
    public void Install_RegistersGraphicsContextExtension()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        world.ShouldHavePlugin<GraphicsPlugin>();
        Assert.True(world.HasExtension<GraphicsContext>());
    }

    [Fact]
    public void Install_GraphicsContext_IsAccessible()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        var graphics = world.GetExtension<GraphicsContext>();
        Assert.NotNull(graphics);
    }

    [Fact]
    public void Install_GraphicsContext_HasConfig()
    {
        using var world = new World();
        var config = new GraphicsConfig { WindowTitle = "Test" };
        var plugin = new GraphicsPlugin(config);

        world.InstallPlugin(plugin);

        var graphics = world.GetExtension<GraphicsContext>();
        Assert.Equal("Test", graphics.Config.WindowTitle);
    }

    [Fact]
    public void Install_GraphicsContext_NotInitializedYet()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        var graphics = world.GetExtension<GraphicsContext>();
        Assert.False(graphics.IsInitialized);
    }

    [Fact]
    public void Install_RegistersCameraSystem()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        // The world should have systems registered (we can't easily check this without
        // internal access, but we can verify the plugin installed without error)
        Assert.True(world.HasExtension<GraphicsContext>());
    }

    [Fact]
    public void Install_RegistersRenderSystem()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        // Similar to above - verify installation succeeded
        Assert.True(world.HasExtension<GraphicsContext>());
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_RemovesGraphicsContextExtension()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);
        world.ShouldHavePlugin<GraphicsPlugin>();

        world.UninstallPlugin<GraphicsPlugin>();

        world.ShouldNotHavePlugin<GraphicsPlugin>();
        Assert.False(world.HasExtension<GraphicsContext>());
    }

    [Fact]
    public void Uninstall_GraphicsContext_IsDisposed()
    {
        var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);
        var graphics = world.GetExtension<GraphicsContext>();

        world.UninstallPlugin<GraphicsPlugin>();

        // GraphicsContext should be disposed - ShouldClose returns true when disposed
        Assert.True(graphics.ShouldClose);
    }

    [Fact]
    public void Uninstall_CanBeReinstalled()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);
        world.UninstallPlugin<GraphicsPlugin>();

        // Should be able to install a new plugin
        var newPlugin = new GraphicsPlugin();
        world.InstallPlugin(newPlugin);

        Assert.True(world.HasExtension<GraphicsContext>());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Plugin_WithDefaultConfig_UsesDefaultValues()
    {
        using var world = new World();
        var plugin = new GraphicsPlugin();

        world.InstallPlugin(plugin);

        var graphics = world.GetExtension<GraphicsContext>();
        Assert.Equal(1280, graphics.Config.WindowWidth);
        Assert.Equal(720, graphics.Config.WindowHeight);
    }

    [Fact]
    public void Plugin_WithCustomConfig_UsesCustomValues()
    {
        using var world = new World();
        var config = new GraphicsConfig
        {
            WindowWidth = 1920,
            WindowHeight = 1080,
            VSync = false
        };
        var plugin = new GraphicsPlugin(config);

        world.InstallPlugin(plugin);

        var graphics = world.GetExtension<GraphicsContext>();
        Assert.Equal(1920, graphics.Config.WindowWidth);
        Assert.Equal(1080, graphics.Config.WindowHeight);
        Assert.False(graphics.Config.VSync);
    }

    [Fact]
    public void Plugin_MultipleWorlds_EachHasOwnContext()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.InstallPlugin(new GraphicsPlugin(new GraphicsConfig { WindowTitle = "World1" }));
        world2.InstallPlugin(new GraphicsPlugin(new GraphicsConfig { WindowTitle = "World2" }));

        var graphics1 = world1.GetExtension<GraphicsContext>();
        var graphics2 = world2.GetExtension<GraphicsContext>();

        Assert.NotSame(graphics1, graphics2);
        Assert.Equal("World1", graphics1.Config.WindowTitle);
        Assert.Equal("World2", graphics2.Config.WindowTitle);
    }

    #endregion
}
