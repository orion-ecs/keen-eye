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

    private sealed class ThrowingUninstallPlugin : IWorldPlugin
    {
        public string Name => "ThrowingUninstall";

        public TestPluginSystem? System { get; private set; }

        public void Install(IPluginContext context)
        {
            System = context.AddSystem<TestPluginSystem>();
        }

        public void Uninstall(IPluginContext context)
        {
            throw new InvalidOperationException("Uninstall failed deliberately.");
        }
    }

    private sealed class ThrowingInstallPlugin : IWorldPlugin
    {
        public string Name => "ThrowingInstall";

        public TestPluginSystem? System { get; private set; }

        public void Install(IPluginContext context)
        {
            System = context.AddSystem<TestPluginSystem>();
            throw new InvalidOperationException("Install failed deliberately.");
        }

        public void Uninstall(IPluginContext context) { }
    }

    #endregion

    #region Lifecycle Failure Tests

    [Fact]
    public void UninstallPlugin_WhenUninstallThrows_StillRemovesRegisteredSystems()
    {
        // Regression test for #1150: a throwing Uninstall must not leave the plugin's systems
        // orphaned. The registry entry is removed before Uninstall runs, so without guaranteed
        // cleanup the system would remain registered with no way to remove it.
        using var world = new World();
        var plugin = new ThrowingUninstallPlugin();
        world.InstallPlugin(plugin);

        Assert.NotNull(world.GetSystem<TestPluginSystem>());

        Assert.Throws<InvalidOperationException>(() => world.UninstallPlugin<ThrowingUninstallPlugin>());

        Assert.Null(world.GetSystem<TestPluginSystem>());
        Assert.True(plugin.System!.Disposed);
    }

    [Fact]
    public void InstallPlugin_WhenInstallThrows_RollsBackRegisteredSystems()
    {
        // Regression test for #1150: if Install registers systems and then throws, those
        // systems must be rolled back rather than left orphaned in the world.
        using var world = new World();
        var plugin = new ThrowingInstallPlugin();

        Assert.Throws<InvalidOperationException>(() => world.InstallPlugin(plugin));

        Assert.Null(world.GetSystem<TestPluginSystem>());
        Assert.False(world.HasPlugin<ThrowingInstallPlugin>());
        Assert.True(plugin.System!.Disposed);
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
