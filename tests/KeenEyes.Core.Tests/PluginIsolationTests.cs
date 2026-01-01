namespace KeenEyes.Tests;

/// <summary>
/// Tests for world isolation with plugins.
/// </summary>
public class PluginIsolationTests
{
    [Fact]
    public void Plugins_AreIsolatedBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.InstallPlugin<TestSimplePlugin>();

        Assert.True(world1.HasPlugin<TestSimplePlugin>());
        Assert.False(world2.HasPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void Extensions_AreIsolatedBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world2.SetExtension(new TestPhysicsWorld { Gravity = -10.0f });

        var physics1 = world1.GetExtension<TestPhysicsWorld>();
        var physics2 = world2.GetExtension<TestPhysicsWorld>();

        Assert.Equal(-9.81f, physics1.Gravity);
        Assert.Equal(-10.0f, physics2.Gravity);
    }

    [Fact]
    public void PluginUninstall_DoesNotAffectOtherWorld()
    {
        using var world1 = new World();
        using var world2 = new World();

        world1.InstallPlugin<TestSimplePlugin>();
        world2.InstallPlugin<TestSimplePlugin>();

        world1.UninstallPlugin<TestSimplePlugin>();

        Assert.False(world1.HasPlugin<TestSimplePlugin>());
        Assert.True(world2.HasPlugin<TestSimplePlugin>());
    }
}

/// <summary>
/// Tests for PluginContext with dependency constraints.
/// </summary>
public class PluginContextDependencyTests
{
    [Fact]
    public void AddSystem_Generic_WithDependencies_RegistersSystem()
    {
        using var world = new World();
        var plugin = new TestDependencyPlugin();

        world.InstallPlugin(plugin);
        world.Update(0.016f);

        Assert.NotNull(plugin.System);
        Assert.Equal(1, plugin.System.UpdateCount);
    }

    [Fact]
    public void AddSystem_Instance_WithDependencies_RegistersSystem()
    {
        using var world = new World();
        var plugin = new TestInstanceDependencyPlugin();

        world.InstallPlugin(plugin);
        world.Update(0.016f);

        Assert.NotNull(plugin.System);
        Assert.Equal(1, plugin.System.UpdateCount);
    }
}
