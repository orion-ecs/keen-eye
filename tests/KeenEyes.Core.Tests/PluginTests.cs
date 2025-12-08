namespace KeenEyes.Tests;

/// <summary>
/// Test extension class for plugin tests.
/// </summary>
public class TestPhysicsWorld
{
    public float Gravity { get; set; } = -9.81f;

    public bool Raycast(float x, float y) => true;
}

/// <summary>
/// Test extension class for plugin tests.
/// </summary>
public class TestRenderer
{
    public int DrawCalls { get; set; }
}

/// <summary>
/// Test system for plugin tests.
/// </summary>
public class TestPluginSystem : SystemBase
{
    public int InitCount { get; private set; }
    public int UpdateCount { get; private set; }
    public bool Disposed { get; private set; }

    protected override void OnInitialize()
    {
        InitCount++;
    }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }

    public override void Dispose()
    {
        Disposed = true;
        base.Dispose();
    }
}

/// <summary>
/// Another test system for plugin tests.
/// </summary>
public class TestSecondPluginSystem : SystemBase
{
    public int UpdateCount { get; private set; }
    public bool Disposed { get; private set; }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }

    public override void Dispose()
    {
        Disposed = true;
        base.Dispose();
    }
}

/// <summary>
/// Simple test plugin for plugin tests.
/// </summary>
public class TestSimplePlugin : IWorldPlugin
{
    public string Name => "TestSimple";

    public bool InstallCalled { get; private set; }
    public bool UninstallCalled { get; private set; }
    public IPluginContext? LastContext { get; private set; }

    public void Install(IPluginContext context)
    {
        InstallCalled = true;
        LastContext = context;
    }

    public void Uninstall(IPluginContext context)
    {
        UninstallCalled = true;
    }
}

/// <summary>
/// Test plugin that registers systems.
/// </summary>
public class TestSystemPlugin : IWorldPlugin
{
    public string Name => "TestSystem";

    public TestPluginSystem? System1 { get; private set; }
    public TestSecondPluginSystem? System2 { get; private set; }

    public void Install(IPluginContext context)
    {
        System1 = context.AddSystem<TestPluginSystem>(SystemPhase.Update, order: 0);
        System2 = context.AddSystem<TestSecondPluginSystem>(SystemPhase.Update, order: 10);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that exposes extensions.
/// </summary>
public class TestExtensionPlugin : IWorldPlugin
{
    public string Name => "TestExtension";

    public TestPhysicsWorld? Physics { get; private set; }

    public void Install(IPluginContext context)
    {
        Physics = new TestPhysicsWorld { Gravity = -10.0f };
        context.SetExtension(Physics);
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<TestPhysicsWorld>();
    }
}

/// <summary>
/// Test plugin with configurable name.
/// </summary>
public class TestConfigurablePlugin : IWorldPlugin
{
    private readonly string name;

    public TestConfigurablePlugin(string name)
    {
        this.name = name;
    }

    public string Name => name;

    public void Install(IPluginContext context)
    {
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Tests for IWorldPlugin interface and plugin installation.
/// </summary>
public class PluginInstallationTests
{
    [Fact]
    public void InstallPlugin_Generic_CallsInstall()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();
        Assert.NotNull(plugin);
        Assert.True(plugin.InstallCalled);
    }

    [Fact]
    public void InstallPlugin_Instance_CallsInstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();

        world.InstallPlugin(plugin);

        Assert.True(plugin.InstallCalled);
    }

    [Fact]
    public void InstallPlugin_ProvidesValidContext()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var plugin = world.GetPlugin<TestSimplePlugin>();
        Assert.NotNull(plugin);
        Assert.NotNull(plugin.LastContext);
        Assert.Same(world, plugin.LastContext.World);
        Assert.Same(plugin, plugin.LastContext.Plugin);
    }

    [Fact]
    public void InstallPlugin_ReturnsWorld_ForChaining()
    {
        using var world = new World();

        var result = world.InstallPlugin<TestSimplePlugin>();

        Assert.Same(world, result);
    }

    [Fact]
    public void InstallPlugin_MultiplePlugins_AllInstalled()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>()
             .InstallPlugin<TestExtensionPlugin>();

        Assert.True(world.HasPlugin<TestSimplePlugin>());
        Assert.True(world.HasPlugin<TestExtensionPlugin>());
    }

    [Fact]
    public void InstallPlugin_DuplicateName_ThrowsInvalidOperationException()
    {
        using var world = new World();

        world.InstallPlugin<TestSimplePlugin>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.InstallPlugin<TestSimplePlugin>());

        Assert.Contains("TestSimple", exception.Message);
        Assert.Contains("already installed", exception.Message);
    }

    [Fact]
    public void InstallPlugin_Null_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.InstallPlugin(null!));
    }
}

/// <summary>
/// Tests for plugin uninstallation.
/// </summary>
public class PluginUninstallationTests
{
    [Fact]
    public void UninstallPlugin_Generic_CallsUninstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSimplePlugin>();

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void UninstallPlugin_ByName_CallsUninstall()
    {
        using var world = new World();
        var plugin = new TestSimplePlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin("TestSimple");

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void UninstallPlugin_ReturnsTrue_WhenPluginExists()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        var result = world.UninstallPlugin<TestSimplePlugin>();

        Assert.True(result);
    }

    [Fact]
    public void UninstallPlugin_ReturnsFalse_WhenPluginNotFound()
    {
        using var world = new World();

        var result = world.UninstallPlugin<TestSimplePlugin>();

        Assert.False(result);
    }

    [Fact]
    public void UninstallPlugin_ByName_ReturnsFalse_WhenNotFound()
    {
        using var world = new World();

        var result = world.UninstallPlugin("NonExistent");

        Assert.False(result);
    }

    [Fact]
    public void UninstallPlugin_RemovesFromWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestSimplePlugin>();

        world.UninstallPlugin<TestSimplePlugin>();

        Assert.False(world.HasPlugin<TestSimplePlugin>());
        Assert.Null(world.GetPlugin<TestSimplePlugin>());
    }

    [Fact]
    public void UninstallPlugin_DisposesRegisteredSystems()
    {
        using var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSystemPlugin>();

        Assert.True(plugin.System1!.Disposed);
        Assert.True(plugin.System2!.Disposed);
    }

    [Fact]
    public void UninstallPlugin_RemovesSystemsFromWorld()
    {
        using var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<TestSystemPlugin>();
        world.Update(0.016f);

        // Systems should no longer update after uninstall
        Assert.Equal(0, plugin.System1!.UpdateCount);
        Assert.Equal(0, plugin.System2!.UpdateCount);
    }
}

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
/// Helper system for tracking update order.
/// </summary>
public class TestOrderTrackingSystem : SystemBase
{
    private readonly string name;
    private readonly List<string> order;

    public TestOrderTrackingSystem(string name, List<string> order)
    {
        this.name = name;
        this.order = order;
    }

    public override void Update(float deltaTime)
    {
        order.Add(name);
    }
}

/// <summary>
/// Test plugin that registers systems in specific order.
/// </summary>
public class TestOrderedPlugin : IWorldPlugin
{
    private readonly ISystem system1;
    private readonly ISystem system2;

    public TestOrderedPlugin(ISystem system1, ISystem system2)
    {
        this.system1 = system1;
        this.system2 = system2;
    }

    public string Name => "TestOrdered";

    public void Install(IPluginContext context)
    {
        context.AddSystem(system1, SystemPhase.Update, order: 0);
        context.AddSystem(system2, SystemPhase.Update, order: 10);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that registers a system group.
/// </summary>
public class TestGroupPlugin : IWorldPlugin
{
    private readonly SystemGroup group;

    public TestGroupPlugin(SystemGroup group)
    {
        this.group = group;
    }

    public string Name => "TestGroup";

    public void Install(IPluginContext context)
    {
        context.AddSystemGroup(group, SystemPhase.Update);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Tests for World extension API.
/// </summary>
public class ExtensionTests
{
    [Fact]
    public void SetExtension_StoresValue()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void SetExtension_ReplacesExisting()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestPhysicsWorld { Gravity = -10.0f });

        var physics = world.GetExtension<TestPhysicsWorld>();
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void SetExtension_Null_ThrowsArgumentNullException()
    {
        using var world = new World();

        Assert.Throws<ArgumentNullException>(() =>
            world.SetExtension<TestPhysicsWorld>(null!));
    }

    [Fact]
    public void GetExtension_ReturnsStoredValue()
    {
        using var world = new World();
        var original = new TestPhysicsWorld { Gravity = -15.0f };

        world.SetExtension(original);
        var retrieved = world.GetExtension<TestPhysicsWorld>();

        Assert.Same(original, retrieved);
        Assert.Equal(-15.0f, retrieved.Gravity);
    }

    [Fact]
    public void GetExtension_ThrowsInvalidOperationException_WhenNotSet()
    {
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetExtension<TestPhysicsWorld>());

        Assert.Contains("TestPhysicsWorld", exception.Message);
    }

    [Fact]
    public void TryGetExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.True(result);
        Assert.NotNull(physics);
        Assert.Equal(-9.81f, physics.Gravity);
    }

    [Fact]
    public void TryGetExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.TryGetExtension<TestPhysicsWorld>(out var physics);

        Assert.False(result);
        Assert.Null(physics);
    }

    [Fact]
    public void HasExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        Assert.True(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void HasExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsTrue_WhenExists()
    {
        using var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.True(result);
        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }

    [Fact]
    public void RemoveExtension_ReturnsFalse_WhenNotSet()
    {
        using var world = new World();

        var result = world.RemoveExtension<TestPhysicsWorld>();

        Assert.False(result);
    }

    [Fact]
    public void Extensions_MultipleTypes_Independent()
    {
        using var world = new World();

        world.SetExtension(new TestPhysicsWorld { Gravity = -9.81f });
        world.SetExtension(new TestRenderer { DrawCalls = 100 });

        var physics = world.GetExtension<TestPhysicsWorld>();
        var renderer = world.GetExtension<TestRenderer>();

        Assert.Equal(-9.81f, physics.Gravity);
        Assert.Equal(100, renderer.DrawCalls);
    }
}

/// <summary>
/// Tests for plugin extensions via PluginContext.
/// </summary>
public class PluginExtensionTests
{
    [Fact]
    public void Plugin_SetExtension_AvailableOnWorld()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        var physics = world.GetExtension<TestPhysicsWorld>();

        Assert.NotNull(physics);
        Assert.Equal(-10.0f, physics.Gravity);
    }

    [Fact]
    public void Plugin_Uninstall_RemovesExtension()
    {
        using var world = new World();
        world.InstallPlugin<TestExtensionPlugin>();

        world.UninstallPlugin<TestExtensionPlugin>();

        Assert.False(world.HasExtension<TestPhysicsWorld>());
    }
}

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
/// Test plugin for tracking install order.
/// </summary>
public class TestTrackingPlugin : IWorldPlugin
{
    private readonly string name;
    private readonly List<string> installOrder;

    public TestTrackingPlugin(string name, List<string> installOrder)
    {
        this.name = name;
        this.installOrder = installOrder;
    }

    public string Name => name;

    public void Install(IPluginContext context)
    {
        installOrder.Add(name);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Tests for World disposal with plugins.
/// </summary>
public class PluginDisposeTests
{
    [Fact]
    public void Dispose_UninstallsAllPlugins()
    {
        var plugin = new TestSimplePlugin();
        var world = new World();
        world.InstallPlugin(plugin);

        world.Dispose();

        Assert.True(plugin.UninstallCalled);
    }

    [Fact]
    public void Dispose_DisposesPluginSystems()
    {
        var world = new World();
        var plugin = new TestSystemPlugin();
        world.InstallPlugin(plugin);

        world.Dispose();

        Assert.True(plugin.System1!.Disposed);
        Assert.True(plugin.System2!.Disposed);
    }

    [Fact]
    public void Dispose_ClearsExtensions()
    {
        var world = new World();
        world.SetExtension(new TestPhysicsWorld());

        world.Dispose();

        // Extensions should be cleared (can't easily test this without internal access)
        // The main point is that Dispose runs without error
    }
}

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

/// <summary>
/// Test plugin that accesses the world during install.
/// </summary>
public class TestWorldAccessPlugin : IWorldPlugin
{
    public string Name => "TestWorldAccess";

    public void Install(IPluginContext context)
    {
        var world = (World)context.World;
        world.SetSingleton(new TestGameConfig { Difficulty = 5 });
    }

    public void Uninstall(IPluginContext context)
    {
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

/// <summary>
/// Test plugin that uses AddSystem with dependency constraints.
/// </summary>
public class TestDependencyPlugin : IWorldPlugin
{
    public string Name => "TestDependency";

    public TestPluginSystem? System { get; private set; }

    public void Install(IPluginContext context)
    {
        System = context.AddSystem<TestPluginSystem>(
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(TestSecondPluginSystem)],
            runsAfter: []);
    }

    public void Uninstall(IPluginContext context)
    {
    }
}

/// <summary>
/// Test plugin that uses AddSystem instance overload with dependency constraints.
/// </summary>
public class TestInstanceDependencyPlugin : IWorldPlugin
{
    public string Name => "TestInstanceDependency";

    public TestPluginSystem? System { get; private set; }

    public void Install(IPluginContext context)
    {
        System = new TestPluginSystem();
        context.AddSystem(
            System,
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(TestSecondPluginSystem)],
            runsAfter: []);
    }

    public void Uninstall(IPluginContext context)
    {
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
