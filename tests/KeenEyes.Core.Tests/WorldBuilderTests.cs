namespace KeenEyes.Tests;

/// <summary>
/// Tests for WorldBuilder.
/// </summary>
public class WorldBuilderTests
{
    [Fact]
    public void Build_CreatesWorld()
    {
        var builder = new WorldBuilder();

        using var world = builder.Build();

        Assert.NotNull(world);
    }

    [Fact]
    public void WithPlugin_Generic_InstallsPlugin()
    {
        using var world = new WorldBuilder()
            .WithPlugin<TestSimplePlugin>()
            .Build();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void WithPlugin_Instance_InstallsPlugin()
    {
        var plugin = new TestSimplePlugin();

        using var world = new WorldBuilder()
            .WithPlugin(plugin)
            .Build();

        Assert.True(plugin.InstallCalled);
    }

    [Fact]
    public void WithPlugin_MultiplePlugins_AllInstalled()
    {
        using var world = new WorldBuilder()
            .WithPlugin<TestSimplePlugin>()
            .WithPlugin<TestExtensionPlugin>()
            .Build();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
        Assert.True(world.HasPlugin<TestExtensionPlugin>());
    }

    [Fact]
    public void WithPlugin_InstallsInOrder()
    {
        var installOrder = new List<string>();
        var plugin1 = new TestTrackingPlugin("First", installOrder);
        var plugin2 = new TestTrackingPlugin("Second", installOrder);

        using var world = new WorldBuilder()
            .WithPlugin(plugin1)
            .WithPlugin(plugin2)
            .Build();

        Assert.Equal(["First", "Second"], installOrder);
    }

    [Fact]
    public void WithSystem_Generic_RegistersSystem()
    {
        using var world = new WorldBuilder()
            .WithSystem<TestPluginSystem>()
            .Build();

        world.Update(0.016f);

        var system = world.GetSystem<TestPluginSystem>();
        Assert.NotNull(system);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void WithSystem_Instance_RegistersSystem()
    {
        var system = new TestPluginSystem();

        using var world = new WorldBuilder()
            .WithSystem(system, SystemPhase.Update)
            .Build();

        world.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void WithSystem_RespectsPhaseAndOrder()
    {
        var updateOrder = new List<string>();
        var system1 = new TestOrderTrackingSystem("Second", updateOrder);
        var system2 = new TestOrderTrackingSystem("First", updateOrder);

        using var world = new WorldBuilder()
            .WithSystem(system1, SystemPhase.Update, order: 10)
            .WithSystem(system2, SystemPhase.Update, order: 0)
            .Build();

        world.Update(0.016f);

        Assert.Equal(["First", "Second"], updateOrder);
    }

    [Fact]
    public void WithSystemGroup_RegistersGroup()
    {
        var system = new TestPluginSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        using var world = new WorldBuilder()
            .WithSystemGroup(group)
            .Build();

        world.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void Build_PluginsThenSystems()
    {
        // Systems added via builder should be registered after plugins
        using var world = new WorldBuilder()
            .WithPlugin<TestSystemPlugin>()
            .WithSystem<TestCountingSystem>()
            .Build();

        world.Update(0.016f);

        // Both plugin systems and builder system should run
        var pluginSystem = world.GetPlugin<TestSystemPlugin>();
        var builderSystem = world.GetSystem<TestCountingSystem>();

        Assert.Equal(1, pluginSystem!.System1!.UpdateCount);
        Assert.Equal(1, builderSystem!.UpdateCount);
    }

    [Fact]
    public void Build_CanBeReusedForMultipleWorlds()
    {
        var builder = new WorldBuilder()
            .WithPlugin<TestSimplePlugin>();

        using var world1 = builder.Build();
        using var world2 = builder.Build();

        Assert.True(world1.HasPlugin<TestSimplePlugin>());
        Assert.True(world2.HasPlugin<TestSimplePlugin>());

        // Should be different plugin instances
        var plugin1 = world1.GetPlugin<TestSimplePlugin>();
        var plugin2 = world2.GetPlugin<TestSimplePlugin>();
        Assert.NotSame(plugin1, plugin2);
    }

    [Fact]
    public void WithPlugin_Null_ThrowsArgumentNullException()
    {
        var builder = new WorldBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            builder.WithPlugin(null!));
    }

    [Fact]
    public void WithSystem_Null_ThrowsArgumentNullException()
    {
        var builder = new WorldBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            builder.WithSystem(null!));
    }

    [Fact]
    public void WithSystemGroup_Null_ThrowsArgumentNullException()
    {
        var builder = new WorldBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            builder.WithSystemGroup(null!));
    }
}

/// <summary>
/// Tests for WorldBuilder with dependency constraints.
/// </summary>
public class WorldBuilderDependencyTests
{
    [Fact]
    public void WithSystem_Generic_WithDependencies_RegistersSystem()
    {
        using var world = new WorldBuilder()
            .WithSystem<TestPluginSystem>(
                SystemPhase.Update,
                order: 0,
                runsBefore: [typeof(TestSecondPluginSystem)],
                runsAfter: [])
            .Build();

        world.Update(0.016f);

        var system = world.GetSystem<TestPluginSystem>();
        Assert.NotNull(system);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void WithSystem_Instance_WithDependencies_RegistersSystem()
    {
        var system = new TestPluginSystem();

        using var world = new WorldBuilder()
            .WithSystem(
                system,
                SystemPhase.Update,
                order: 0,
                runsBefore: [typeof(TestSecondPluginSystem)],
                runsAfter: [])
            .Build();

        world.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void WithSystemGroup_WithPhaseAndOrder_RegistersGroup()
    {
        var system = new TestPluginSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        using var world = new WorldBuilder()
            .WithSystemGroup(group, SystemPhase.FixedUpdate, order: 5)
            .Build();

        world.FixedUpdate(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }
}
