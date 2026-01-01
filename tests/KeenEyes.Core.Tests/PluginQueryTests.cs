namespace KeenEyes.Tests;

/// <summary>
/// Tests for plugin query methods.
/// </summary>
public class PluginQueryTests
{
    [Fact]
    public void GetPlugin_Generic_ReturnsPlugin()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();

        Assert.NotNull(plugin);
        Assert.Equal("TestSimple", plugin.Name);
    }

    [Fact]
    public void GetPlugin_Generic_ReturnsNull_WhenNotFound()
    {
        using var world = new World();

        var plugin = world.GetPlugin<TestSimplePlugin>();

        Assert.Null(plugin);
    }

    [Fact]
    public void GetPlugin_ByName_ReturnsPlugin()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin("TestSimple");

        Assert.NotNull(plugin);
        Assert.Equal("TestSimple", plugin.Name);
    }

    [Fact]
    public void GetPlugin_ByName_ReturnsNull_WhenNotFound()
    {
        using var world = new World();

        var plugin = world.GetPlugin("NonExistent");

        Assert.Null(plugin);
    }

    [Fact]
    public void HasPlugin_Generic_ReturnsTrue_WhenInstalled()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void HasPlugin_Generic_ReturnsFalse_WhenNotInstalled()
    {
        using var world = new World();

        Assert.False(world.HasPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void HasPlugin_ByName_ReturnsTrue_WhenInstalled()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        Assert.True(world.HasPlugin("TestSimple"));
    }

    [Fact]
    public void HasPlugin_ByName_ReturnsFalse_WhenNotInstalled()
    {
        using var world = new World();

        Assert.False(world.HasPlugin("NonExistent"));
    }

    [Fact]
    public void GetPlugins_ReturnsAllPlugins()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();
        world.InstallPlugin<TestExtensionPlugin>();

        var plugins = world.GetPlugins().ToList();

        Assert.Equal(2, plugins.Count);
        Assert.Contains(plugins, p => p.Name == "TestSimple");
        Assert.Contains(plugins, p => p.Name == "TestExtension");
    }

    [Fact]
    public void GetPlugins_ReturnsEmpty_WhenNoPlugins()
    {
        using var world = new World();

        var plugins = world.GetPlugins().ToList();

        Assert.Empty(plugins);
    }
}
