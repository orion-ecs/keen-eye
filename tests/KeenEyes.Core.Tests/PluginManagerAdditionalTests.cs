namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for plugin management to increase code coverage.
/// </summary>
public class PluginManagerAdditionalTests
{
    #region Test Plugins and Systems

    private sealed class TestDisposableSystem : SystemBase
    {
        public bool Disposed { get; private set; }
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class TestCountingPlugin : IWorldPlugin
    {
        public string Name => "TestCounting";
        public int InstallCount { get; private set; }
        public int UninstallCount { get; private set; }

        public void Install(IPluginContext context)
        {
            InstallCount++;
        }

        public void Uninstall(IPluginContext context)
        {
            UninstallCount++;
        }
    }

    private sealed class TestExceptionPlugin : IWorldPlugin
    {
        public string Name => "TestException";

        public void Install(IPluginContext context)
        {
            throw new InvalidOperationException("Install failed");
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestMultiSystemPlugin : IWorldPlugin
    {
        public string Name => "TestMultiSystem";
        public List<TestDisposableSystem> Systems { get; } = [];

        public void Install(IPluginContext context)
        {
            for (int i = 0; i < 5; i++)
            {
                var system = new TestDisposableSystem();
                Systems.Add(system);
                context.AddSystem(system);
            }
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    #endregion

    #region Multiple Plugin Installation Tests

    [Fact]
    public void InstallPlugin_MultipleDifferentPlugins_AllInstalled()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();
        world.InstallPlugin<TestExtensionPlugin>();
        world.InstallPlugin<TestSystemPlugin>();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
        Assert.True(world.HasPlugin<TestExtensionPlugin>());
        Assert.True(world.HasPlugin<TestSystemPlugin>());

        var plugins = world.GetPlugins().ToList();
        Assert.Equal(3, plugins.Count);
    }

    [Fact]
    public void InstallPlugin_SameNameDifferentInstances_ThrowsInvalidOperationException()
    {
        using var world = new World();

        var plugin1 = new TestConfigurablePlugin("SameName");
        var plugin2 = new TestConfigurablePlugin("SameName");

        world.InstallPlugin(plugin1);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.InstallPlugin(plugin2));

        Assert.Contains("SameName", ex.Message);
        Assert.Contains("already installed", ex.Message);
    }

    #endregion

    #region Uninstall All Tests

    [Fact]
    public void Dispose_WithMultiplePlugins_UninstallsAll()
    {
        var plugin1 = new TestCountingPlugin();
        var plugin2 = new TestSimplePlugin();
        var plugin3 = new TestExtensionPlugin();

        var world = new World();
        world.InstallPlugin(plugin1);
        world.InstallPlugin(plugin2);
        world.InstallPlugin(plugin3);

        world.Dispose();

        Assert.Equal(1, plugin1.UninstallCount);
        Assert.True(plugin2.UninstallCalled);
    }

    [Fact]
    public void Dispose_WithPluginSystems_DisposesAllSystems()
    {
        var world = new World();
        var plugin = new TestMultiSystemPlugin();

        world.InstallPlugin(plugin);
        world.Dispose();

        foreach (var system in plugin.Systems)
        {
            Assert.True(system.Disposed);
        }
    }

    [Fact]
    public void Dispose_MultipleTimes_DoesNotThrow()
    {
        var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        world.Dispose();
        world.Dispose(); // Second dispose should not throw
    }

    #endregion

    #region Plugin Query Edge Cases

    [Fact]
    public void GetPlugin_Generic_AfterUninstall_ReturnsNull()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();
        world.UninstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();

        Assert.Null(plugin);
    }

    [Fact]
    public void GetPlugin_ByName_WithEmptyString_ReturnsNull()
    {
        using var world = new World();

        var plugin = world.GetPlugin(string.Empty);

        Assert.Null(plugin);
    }

    [Fact]
    public void GetPlugins_AfterUninstallingSome_ReturnsOnlyRemaining()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();
        world.InstallPlugin<TestExtensionPlugin>();
        world.InstallPlugin<TestSystemPlugin>();

        world.UninstallPlugin<TestExtensionPlugin>();

        var plugins = world.GetPlugins().ToList();

        Assert.Equal(2, plugins.Count);
        Assert.Contains(plugins, p => p.Name == "TestSimple");
        Assert.Contains(plugins, p => p.Name == "TestSystem");
        Assert.DoesNotContain(plugins, p => p.Name == "TestExtension");
    }

    #endregion

    #region System Cleanup Tests

    [Fact]
    public void UninstallPlugin_RemovesAllPluginSystems()
    {
        using var world = new World();
        var plugin = new TestMultiSystemPlugin();

        world.InstallPlugin(plugin);

        // Verify systems are running
        world.Update(0.016f);
        foreach (var system in plugin.Systems)
        {
            Assert.Equal(1, system.UpdateCount);
        }

        world.UninstallPlugin<TestMultiSystemPlugin>();

        // Systems should no longer update
        world.Update(0.016f);
        foreach (var system in plugin.Systems)
        {
            Assert.Equal(1, system.UpdateCount); // Count unchanged
            Assert.True(system.Disposed);
        }
    }

    [Fact]
    public void UninstallPlugin_WithNoSystems_DoesNotThrow()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        world.UninstallPlugin<TestSimplePlugin>();

        // Should complete without error
        Assert.False(world.HasPlugin<TestSimplePlugin>());
    }

    #endregion

    #region Plugin Installation Exception Tests

    [Fact]
    public void InstallPlugin_ThrowsException_LeavesWorldInValidState()
    {
        using var world = new World();

        Assert.Throws<InvalidOperationException>(() =>
            world.InstallPlugin<TestExceptionPlugin>());

        // Plugin should not be registered
        Assert.False(world.HasPlugin<TestExceptionPlugin>());

        // World should still be usable
        world.InstallPlugin<TestSimplePlugin>();
        Assert.True(world.HasPlugin<TestSimplePlugin>());
    }

    #endregion

    #region Plugin Reinstallation Tests

    [Fact]
    public void InstallPlugin_AfterUninstall_WorksCorrectly()
    {
        using var world = new World();
        var plugin1 = new TestCountingPlugin();

        world.InstallPlugin(plugin1);
        Assert.Equal(1, plugin1.InstallCount);

        world.UninstallPlugin<TestCountingPlugin>();
        Assert.Equal(1, plugin1.UninstallCount);

        // Reinstall new instance
        var plugin2 = new TestCountingPlugin();
        world.InstallPlugin(plugin2);
        Assert.Equal(1, plugin2.InstallCount);
        Assert.True(world.HasPlugin<TestCountingPlugin>());
    }

    #endregion

    #region Plugin Context Access Tests

    [Fact]
    public void Plugin_CanAccessWorldDuringUninstall()
    {
        using var world = new World();
        var plugin = new TestWorldModifyingPlugin();

        world.SetSingleton(new TestGameConfig { Difficulty = 5 });
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestWorldModifyingPlugin>();

        // Plugin should have been able to access world during uninstall
        Assert.True(plugin.UninstalledSuccessfully);
    }

    #endregion

    #region HasPlugin Edge Cases

    [Fact]
    public void HasPlugin_Generic_WithNeverInstalledType_ReturnsFalse()
    {
        using var world = new World();

        Assert.False(world.HasPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void HasPlugin_ByName_CaseSensitive()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>(); // Name is "TestSimple"

        Assert.True(world.HasPlugin("TestSimple"));
        Assert.False(world.HasPlugin("testsimple"));
        Assert.False(world.HasPlugin("TESTSIMPLE"));
    }

    #endregion

    #region Multiple Worlds Isolation Tests

    [Fact]
    public void Plugins_InDifferentWorlds_AreIndependent()
    {
        using var world1 = new World();
        using var world2 = new World();

        var plugin1 = new TestCountingPlugin();
        var plugin2 = new TestCountingPlugin();

        world1.InstallPlugin(plugin1);
        world2.InstallPlugin(plugin2);

        world1.UninstallPlugin<TestCountingPlugin>();

        // Only plugin1 should be uninstalled
        Assert.Equal(1, plugin1.UninstallCount);
        Assert.Equal(0, plugin2.UninstallCount);

        Assert.False(world1.HasPlugin<TestCountingPlugin>());
        Assert.True(world2.HasPlugin<TestCountingPlugin>());
    }

    #endregion

    #region Test Helper Classes

    private sealed class TestWorldModifyingPlugin : IWorldPlugin
    {
        public string Name => "TestWorldModifying";
        public bool UninstalledSuccessfully { get; private set; }

        public void Install(IPluginContext context)
        {
        }

        public void Uninstall(IPluginContext context)
        {
            // Access world during uninstall
#pragma warning disable KEEN050 // IWorld to World cast - test code
            var world = (World)context.World;
#pragma warning restore KEEN050
            if (world.HasSingleton<TestGameConfig>())
            {
                UninstalledSuccessfully = true;
            }
        }
    }

    #endregion
}
