namespace KeenEyes.Tests;

/// <summary>
/// Tests for plugin system registration via PluginContext.
/// </summary>
public class PluginSystemRegistrationTests
{
    [Fact]
    public void AddSystem_RegistersWithWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestSystemPlugin>();

        world.Update(0.016f);

        var plugin = world.GetPlugin<TestSystemPlugin>();
        Assert.Equal(1, plugin!.System1!.UpdateCount);
        Assert.Equal(1, plugin.System2!.UpdateCount);
    }

    [Fact]
    public void AddSystem_InitializesSystem()
    {
        using var world = new World();
        world.InstallPlugin<TestSystemPlugin>();

        var plugin = world.GetPlugin<TestSystemPlugin>();
        Assert.Equal(1, plugin!.System1!.InitCount);
    }

    [Fact]
    public void AddSystem_RespectsPhaseAndOrder()
    {
        using var world = new World();
        var updateOrder = new List<string>();

        var system1 = new TestOrderTrackingSystem("First", updateOrder);
        var system2 = new TestOrderTrackingSystem("Second", updateOrder);

        var plugin = new TestOrderedPlugin(system1, system2);
        world.InstallPlugin(plugin);

        world.Update(0.016f);

        Assert.Equal(["First", "Second"], updateOrder);
    }

    [Fact]
    public void AddSystemGroup_RegistersGroup()
    {
        using var world = new World();
        var system = new TestPluginSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        var plugin = new TestGroupPlugin(group);
        world.InstallPlugin(plugin);

        world.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }
}

/// <summary>
/// Integration tests for plugin system.
/// </summary>
public class PluginIntegrationTests
{
    [Fact]
    public void FullWorkflow_InstallUpdateUninstall()
    {
        using var world = new World();

        // Install plugin
        world.InstallPlugin<TestSystemPlugin>();

        // Verify systems are running
        world.Update(0.016f);
        var plugin = world.GetPlugin<TestSystemPlugin>();
        Assert.Equal(1, plugin!.System1!.UpdateCount);

        // Update a few more times
        world.Update(0.016f);
        world.Update(0.016f);
        Assert.Equal(3, plugin.System1.UpdateCount);

        // Uninstall
        world.UninstallPlugin<TestSystemPlugin>();

        // Systems should no longer update
        world.Update(0.016f);
        Assert.Equal(3, plugin.System1.UpdateCount); // Count unchanged
    }

    [Fact]
    public void Plugin_AccessesWorld_DuringInstall()
    {
        using var world = new World();
        world.InstallPlugin<TestWorldAccessPlugin>();

        Assert.True(world.HasSingleton<TestGameConfig>());
    }

    [Fact]
    public void Plugin_CanReinstall_AfterUninstall()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();
        world.UninstallPlugin<TestSimplePlugin>();
        world.InstallPlugin<TestSimplePlugin>();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
    }
}
