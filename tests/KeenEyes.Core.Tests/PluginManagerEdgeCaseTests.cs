namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for PluginManager to improve coverage.
/// Focuses on edge cases and less common code paths.
/// </summary>
public class PluginManagerEdgeCaseTests
{
    #region Test Plugins

    private sealed class TestPlugin : IWorldPlugin
    {
        public string Name => "TestPlugin";
        public bool InstallCalled { get; private set; }
        public bool UninstallCalled { get; private set; }

        public void Install(IPluginContext context)
        {
            InstallCalled = true;
        }

        public void Uninstall(IPluginContext context)
        {
            UninstallCalled = true;
        }
    }

    private sealed class AnotherTestPlugin : IWorldPlugin
    {
        public string Name => "AnotherTestPlugin";

        public void Install(IPluginContext context) { }
        public void Uninstall(IPluginContext context) { }
    }

    private sealed class ThirdPlugin : IWorldPlugin
    {
        public string Name => "ThirdPlugin";

        public void Install(IPluginContext context) { }
        public void Uninstall(IPluginContext context) { }
    }

    #endregion

    #region GetPlugin<T> Tests

    [Fact]
    public void GetPlugin_Generic_WithMatchingPlugin_ReturnsPlugin()
    {
        using var world = new World();
        var plugin = new TestPlugin();

        world.InstallPlugin(plugin);

        var result = world.GetPlugin<TestPlugin>();

        Assert.NotNull(result);
        Assert.Same(plugin, result);
    }

    [Fact]
    public void GetPlugin_Generic_WithNonMatchingPlugin_ReturnsNull()
    {
        using var world = new World();
        var plugin = new TestPlugin();

        world.InstallPlugin(plugin);

        var result = world.GetPlugin<AnotherTestPlugin>();

        Assert.Null(result);
    }

    [Fact]
    public void GetPlugin_Generic_WithMultiplePlugins_ReturnsFirstMatch()
    {
        using var world = new World();
        var plugin1 = new TestPlugin();
        var plugin2 = new AnotherTestPlugin();

        world.InstallPlugin(plugin1);
        world.InstallPlugin(plugin2);

        var result = world.GetPlugin<TestPlugin>();

        Assert.NotNull(result);
        Assert.Same(plugin1, result);
    }

    #endregion

    #region UninstallPlugin Error Paths

    [Fact]
    public void UninstallPlugin_WithNonExistentPlugin_ReturnsFalse()
    {
        using var world = new World();

        var result = world.UninstallPlugin("NonExistentPlugin");

        Assert.False(result);
    }

    [Fact]
    public void UninstallPlugin_CalledTwice_SecondReturnsFalse()
    {
        using var world = new World();
        var plugin = new TestPlugin();

        world.InstallPlugin(plugin);

        var firstResult = world.UninstallPlugin("TestPlugin");
        Assert.True(firstResult);

        var secondResult = world.UninstallPlugin("TestPlugin");
        Assert.False(secondResult);
    }

    #endregion

    #region Multiple Plugin Type Tests

    [Fact]
    public void GetPlugin_WithDifferentPluginTypes_ReturnsCorrectType()
    {
        using var world = new World();
        var testPlugin = new TestPlugin();
        var anotherPlugin = new AnotherTestPlugin();
        var thirdPlugin = new ThirdPlugin();

        world.InstallPlugin(testPlugin);
        world.InstallPlugin(anotherPlugin);
        world.InstallPlugin(thirdPlugin);

        // Should return the correct type for each query
        Assert.Same(testPlugin, world.GetPlugin<TestPlugin>());
        Assert.Same(anotherPlugin, world.GetPlugin<AnotherTestPlugin>());
        Assert.Same(thirdPlugin, world.GetPlugin<ThirdPlugin>());
    }

    #endregion
}
