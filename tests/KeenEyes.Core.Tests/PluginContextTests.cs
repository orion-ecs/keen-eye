namespace KeenEyes.Tests;

/// <summary>
/// Comprehensive tests for PluginContext class to increase coverage.
/// </summary>
public class PluginContextTests
{
    #region Test Systems and Extensions

    private sealed class TestSystem : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    private sealed class TestSystem2 : SystemBase
    {
        public int UpdateCount { get; private set; }

        public override void Update(float deltaTime)
        {
            UpdateCount++;
        }
    }

    private sealed class TestExtension
    {
        public int Value { get; set; }
    }

    private sealed class TestExtension2
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion

    #region Property Tests

    [Fact]
    public void World_ReturnsCorrectWorld()
    {
        using var world = new World();
        var plugin = new TestContextPlugin();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.CapturedContext);
        Assert.Same(world, plugin.CapturedContext.World);
    }

    [Fact]
    public void Plugin_ReturnsCorrectPlugin()
    {
        using var world = new World();
        var plugin = new TestContextPlugin();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.CapturedContext);
        Assert.Same(plugin, plugin.CapturedContext.Plugin);
    }

    [Fact]
    public void IWorldInterface_ReturnsWorld()
    {
        using var world = new World();
        var plugin = new TestContextPlugin();

        world.InstallPlugin(plugin);

        var iworld = ((IPluginContext)plugin.CapturedContext!).World;
        Assert.Same(world, iworld);
    }

    #endregion

    #region AddSystem Generic Tests

    [Fact]
    public void AddSystem_Generic_DefaultParameters_RegistersSystem()
    {
        using var world = new World();
        var plugin = new TestGenericSystemPlugin<TestSystem>();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.System);
        world.Update(0.016f);
        Assert.Equal(1, plugin.System.UpdateCount);
    }

    [Fact]
    public void AddSystem_Generic_WithPhase_RegistersInCorrectPhase()
    {
        using var world = new World();
        var plugin = new TestPhaseSystemPlugin();

        world.InstallPlugin(plugin);

        // Update phase should not run during FixedUpdate
        world.FixedUpdate(0.016f);
        Assert.Equal(0, plugin.System!.UpdateCount);

        // Should run during Update
        world.Update(0.016f);
        Assert.Equal(1, plugin.System.UpdateCount);
    }

    [Fact]
    public void AddSystem_Generic_WithOrder_RespectsOrder()
    {
        using var world = new World();
        var executionOrder = new List<int>();
        var plugin = new TestOrderSystemPlugin(executionOrder);

        world.InstallPlugin(plugin);
        world.Update(0.016f);

        Assert.Equal([1, 2], executionOrder);
    }

    [Fact]
    public void AddSystem_Generic_WithDependencies_RegistersCorrectly()
    {
        using var world = new World();
        var plugin = new TestDependencySystemPlugin();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.System);
        world.Update(0.016f);
        Assert.Equal(1, plugin.System.UpdateCount);
    }

    [Fact]
    public void AddSystem_Generic_ReturnsSystemInstance()
    {
        using var world = new World();
        var plugin = new TestGenericSystemPlugin<TestSystem>();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.System);
        Assert.IsType<TestSystem>(plugin.System);
    }

    #endregion

    #region AddSystem Instance Tests

    [Fact]
    public void AddSystem_Instance_DefaultParameters_RegistersSystem()
    {
        using var world = new World();
        var system = new TestSystem();
        var plugin = new TestInstanceSystemPlugin(system);

        world.InstallPlugin(plugin);

        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void AddSystem_Instance_WithPhaseAndOrder_RegistersCorrectly()
    {
        using var world = new World();
        var system = new TestSystem();
        var plugin = new TestInstancePhaseOrderPlugin(system);

        world.InstallPlugin(plugin);

        world.FixedUpdate(0.016f);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void AddSystem_Instance_WithDependencies_RegistersCorrectly()
    {
        using var world = new World();
        var system = new TestSystem();
        var plugin = new TestInstanceDependencyPlugin(system);

        world.InstallPlugin(plugin);

        Assert.Same(system, plugin.ReturnedSystem);
        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void AddSystem_Instance_ReturnsSameInstance()
    {
        using var world = new World();
        var system = new TestSystem();
        var plugin = new TestInstanceSystemPlugin(system);

        world.InstallPlugin(plugin);

        Assert.Same(system, plugin.ReturnedSystem);
    }

    #endregion

    #region AddSystemGroup Tests

    [Fact]
    public void AddSystemGroup_DefaultParameters_RegistersGroup()
    {
        using var world = new World();
        var system = new TestSystem();
        var group = new SystemGroup("TestGroup").Add(system);
        var plugin = new TestSystemGroupPlugin(group);

        world.InstallPlugin(plugin);

        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void AddSystemGroup_WithPhaseAndOrder_RegistersCorrectly()
    {
        using var world = new World();
        var system = new TestSystem();
        var group = new SystemGroup("TestGroup").Add(system);
        var plugin = new TestSystemGroupPhasePlugin(group);

        world.InstallPlugin(plugin);

        world.FixedUpdate(0.016f);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void AddSystemGroup_ReturnsSameGroup()
    {
        using var world = new World();
        var group = new SystemGroup("TestGroup");
        var plugin = new TestSystemGroupPlugin(group);

        world.InstallPlugin(plugin);

        Assert.Same(group, plugin.ReturnedGroup);
    }

    #endregion

    #region Extension Tests

    [Fact]
    public void GetExtension_RetrievesExtension()
    {
        using var world = new World();
        var ext = new TestExtension { Value = 42 };
        world.SetExtension(ext);

        var plugin = new TestGetExtensionPlugin();
        world.InstallPlugin(plugin);

        Assert.Same(ext, plugin.RetrievedExtension);
    }

    [Fact]
    public void TryGetExtension_ExistingExtension_ReturnsTrue()
    {
        using var world = new World();
        var ext = new TestExtension { Value = 42 };
        world.SetExtension(ext);

        var plugin = new TestTryGetExtensionPlugin();
        world.InstallPlugin(plugin);

        Assert.True(plugin.TryResult);
        Assert.Same(ext, plugin.RetrievedExtension);
    }

    [Fact]
    public void TryGetExtension_NonExistingExtension_ReturnsFalse()
    {
        using var world = new World();

        var plugin = new TestTryGetExtensionPlugin();
        world.InstallPlugin(plugin);

        Assert.False(plugin.TryResult);
        Assert.Null(plugin.RetrievedExtension);
    }

    [Fact]
    public void SetExtension_StoresExtension()
    {
        using var world = new World();
        var plugin = new TestSetExtensionPlugin();

        world.InstallPlugin(plugin);

        var retrieved = world.GetExtension<TestExtension>();
        Assert.Equal(123, retrieved.Value);
    }

    [Fact]
    public void RemoveExtension_RemovesExtension()
    {
        using var world = new World();
        world.SetExtension(new TestExtension { Value = 42 });

        var plugin = new TestRemoveExtensionPlugin();
        world.InstallPlugin(plugin);

        Assert.True(plugin.RemoveResult);
        Assert.False(world.HasExtension<TestExtension>());
    }

    [Fact]
    public void RemoveExtension_NonExisting_ReturnsFalse()
    {
        using var world = new World();

        var plugin = new TestRemoveExtensionPlugin();
        world.InstallPlugin(plugin);

        Assert.False(plugin.RemoveResult);
    }

    #endregion

    #region RegisterComponent Tests

    [Fact]
    public void RegisterComponent_RegistersComponentType()
    {
        using var world = new World();
        var plugin = new TestRegisterComponentPlugin();

        world.InstallPlugin(plugin);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        Assert.True(world.Has<TestComponent>(entity));
    }

    [Fact]
    public void RegisterComponent_WithTag_RegistersAsTag()
    {
        using var world = new World();
        var plugin = new TestRegisterTagPlugin();

        world.InstallPlugin(plugin);

        var entity = world.Spawn()
            .With(new TestTagComponent())
            .Build();

        Assert.True(world.Has<TestTagComponent>(entity));
    }

    #endregion

    #region Capability Tests

    [Fact]
    public void GetCapability_WorldImplementsInterface_ReturnsWorld()
    {
        using var world = new World();
        var plugin = new TestGetCapabilityPlugin();

        world.InstallPlugin(plugin);

        Assert.NotNull(plugin.WorldCapability);
        Assert.Same(world, plugin.WorldCapability);
    }

    [Fact]
    public void GetCapability_NotAvailable_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var plugin = new TestGetNonExistentCapabilityPlugin();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            world.InstallPlugin(plugin));

        Assert.Contains("Capability", ex.Message);
    }

    [Fact]
    public void TryGetCapability_WorldImplementsInterface_ReturnsTrue()
    {
        using var world = new World();
        var plugin = new TestTryGetCapabilityPlugin();

        world.InstallPlugin(plugin);

        Assert.True(plugin.TryResult);
        Assert.NotNull(plugin.WorldCapability);
    }

    [Fact]
    public void TryGetCapability_NotAvailable_ReturnsFalse()
    {
        using var world = new World();
        var plugin = new TestTryGetNonExistentCapabilityPlugin();

        world.InstallPlugin(plugin);

        Assert.False(plugin.TryResult);
        Assert.Null(plugin.NonExistentCapability);
    }

    [Fact]
    public void HasCapability_WorldImplementsInterface_ReturnsTrue()
    {
        using var world = new World();
        var plugin = new TestHasCapabilityPlugin();

        world.InstallPlugin(plugin);

        Assert.True(plugin.HasResult);
    }

    [Fact]
    public void HasCapability_NotAvailable_ReturnsFalse()
    {
        using var world = new World();
        var plugin = new TestHasNonExistentCapabilityPlugin();

        world.InstallPlugin(plugin);

        Assert.False(plugin.HasResult);
    }

    #endregion

    #region RegisteredSystems Tests

    [Fact]
    public void RegisteredSystems_TracksAllAddedSystems()
    {
        using var world = new World();
        var plugin = new TestMultipleSystemsPlugin();

        world.InstallPlugin(plugin);

        var context = plugin.CapturedContext!;
        var registeredSystems = ((PluginContext)context).RegisteredSystems;

        Assert.Equal(3, registeredSystems.Count);
    }

    [Fact]
    public void RegisteredSystems_IncludesInstanceSystems()
    {
        using var world = new World();
        var system1 = new TestSystem();
        var system2 = new TestSystem2();
        var plugin = new TestMixedSystemsPlugin(system1, system2);

        world.InstallPlugin(plugin);

        var context = plugin.CapturedContext!;
        var registeredSystems = ((PluginContext)context).RegisteredSystems;

        Assert.Equal(2, registeredSystems.Count);
        Assert.Contains(system1, registeredSystems);
        Assert.Contains(system2, registeredSystems);
    }

    #endregion

    #region Test Plugins and Components

    private struct TestComponent : IComponent
    {
        public int Value;
    }

    private struct TestTagComponent : IComponent
    {
    }

    private sealed class TestContextPlugin : IWorldPlugin
    {
        public string Name => "TestContext";
        public IPluginContext? CapturedContext { get; private set; }

        public void Install(IPluginContext context)
        {
            CapturedContext = context;
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestGenericSystemPlugin<T> : IWorldPlugin where T : ISystem, new()
    {
        public string Name => "TestGenericSystem";
        public T? System { get; private set; }

        public void Install(IPluginContext context)
        {
            System = context.AddSystem<T>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestPhaseSystemPlugin : IWorldPlugin
    {
        public string Name => "TestPhaseSystem";
        public TestSystem? System { get; private set; }

        public void Install(IPluginContext context)
        {
            System = context.AddSystem<TestSystem>(SystemPhase.Update);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestOrderSystemPlugin(List<int> executionOrder) : IWorldPlugin
    {
        public string Name => "TestOrderSystem";

        public void Install(IPluginContext context)
        {
            context.AddSystem(new TestOrderTrackingSystem(1, executionOrder), SystemPhase.Update, order: 0);
            context.AddSystem(new TestOrderTrackingSystem(2, executionOrder), SystemPhase.Update, order: 10);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestOrderTrackingSystem(int id, List<int> executionOrder) : SystemBase
    {
        public override void Update(float deltaTime)
        {
            executionOrder.Add(id);
        }
    }

    private sealed class TestDependencySystemPlugin : IWorldPlugin
    {
        public string Name => "TestDependencySystem";
        public TestSystem? System { get; private set; }

        public void Install(IPluginContext context)
        {
            System = context.AddSystem<TestSystem>(
                SystemPhase.Update,
                order: 0,
                runsBefore: [typeof(TestSystem2)],
                runsAfter: []);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestInstanceSystemPlugin(TestSystem system) : IWorldPlugin
    {
        public string Name => "TestInstanceSystem";
        public ISystem? ReturnedSystem { get; private set; }

        public void Install(IPluginContext context)
        {
            ReturnedSystem = context.AddSystem(system);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestInstancePhaseOrderPlugin(TestSystem system) : IWorldPlugin
    {
        public string Name => "TestInstancePhaseOrder";

        public void Install(IPluginContext context)
        {
            context.AddSystem(system, SystemPhase.FixedUpdate, order: 5);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestInstanceDependencyPlugin(TestSystem system) : IWorldPlugin
    {
        public string Name => "TestInstanceDependency";
        public ISystem? ReturnedSystem { get; private set; }

        public void Install(IPluginContext context)
        {
            ReturnedSystem = context.AddSystem(
                system,
                SystemPhase.Update,
                order: 0,
                runsBefore: [typeof(TestSystem2)],
                runsAfter: []);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestSystemGroupPlugin(SystemGroup group) : IWorldPlugin
    {
        public string Name => "TestSystemGroup";
        public SystemGroup? ReturnedGroup { get; private set; }

        public void Install(IPluginContext context)
        {
            ReturnedGroup = context.AddSystemGroup(group);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestSystemGroupPhasePlugin(SystemGroup group) : IWorldPlugin
    {
        public string Name => "TestSystemGroupPhase";

        public void Install(IPluginContext context)
        {
            context.AddSystemGroup(group, SystemPhase.FixedUpdate, order: 5);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestGetExtensionPlugin : IWorldPlugin
    {
        public string Name => "TestGetExtension";
        public TestExtension? RetrievedExtension { get; private set; }

        public void Install(IPluginContext context)
        {
            RetrievedExtension = context.GetExtension<TestExtension>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestTryGetExtensionPlugin : IWorldPlugin
    {
        public string Name => "TestTryGetExtension";
        public bool TryResult { get; private set; }
        public TestExtension? RetrievedExtension { get; private set; }

        public void Install(IPluginContext context)
        {
            TryResult = context.TryGetExtension<TestExtension>(out var ext);
            RetrievedExtension = ext;
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestSetExtensionPlugin : IWorldPlugin
    {
        public string Name => "TestSetExtension";

        public void Install(IPluginContext context)
        {
            context.SetExtension(new TestExtension { Value = 123 });
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestRemoveExtensionPlugin : IWorldPlugin
    {
        public string Name => "TestRemoveExtension";
        public bool RemoveResult { get; private set; }

        public void Install(IPluginContext context)
        {
            RemoveResult = context.RemoveExtension<TestExtension>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestRegisterComponentPlugin : IWorldPlugin
    {
        public string Name => "TestRegisterComponent";

        public void Install(IPluginContext context)
        {
            context.RegisterComponent<TestComponent>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestRegisterTagPlugin : IWorldPlugin
    {
        public string Name => "TestRegisterTag";

        public void Install(IPluginContext context)
        {
            context.RegisterComponent<TestTagComponent>(isTag: true);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestGetCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestGetCapability";
        public IWorld? WorldCapability { get; private set; }

        public void Install(IPluginContext context)
        {
            WorldCapability = context.GetCapability<IWorld>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private interface INonExistentCapability
    {
    }

    private sealed class TestGetNonExistentCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestGetNonExistentCapability";

        public void Install(IPluginContext context)
        {
            // Should throw
            context.GetCapability<INonExistentCapability>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestTryGetCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestTryGetCapability";
        public bool TryResult { get; private set; }
        public IWorld? WorldCapability { get; private set; }

        public void Install(IPluginContext context)
        {
            TryResult = context.TryGetCapability<IWorld>(out var capability);
            WorldCapability = capability;
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestTryGetNonExistentCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestTryGetNonExistentCapability";
        public bool TryResult { get; private set; }
        public INonExistentCapability? NonExistentCapability { get; private set; }

        public void Install(IPluginContext context)
        {
            TryResult = context.TryGetCapability<INonExistentCapability>(out var capability);
            NonExistentCapability = capability;
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestHasCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestHasCapability";
        public bool HasResult { get; private set; }

        public void Install(IPluginContext context)
        {
            HasResult = context.HasCapability<IWorld>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestHasNonExistentCapabilityPlugin : IWorldPlugin
    {
        public string Name => "TestHasNonExistentCapability";
        public bool HasResult { get; private set; }

        public void Install(IPluginContext context)
        {
            HasResult = context.HasCapability<INonExistentCapability>();
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestMultipleSystemsPlugin : IWorldPlugin
    {
        public string Name => "TestMultipleSystems";
        public IPluginContext? CapturedContext { get; private set; }

        public void Install(IPluginContext context)
        {
            CapturedContext = context;
            context.AddSystem<TestSystem>();
            context.AddSystem<TestSystem2>();
            context.AddSystem(new TestSystem());
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    private sealed class TestMixedSystemsPlugin(TestSystem system1, TestSystem2 system2) : IWorldPlugin
    {
        public string Name => "TestMixedSystems";
        public IPluginContext? CapturedContext { get; private set; }

        public void Install(IPluginContext context)
        {
            CapturedContext = context;
            context.AddSystem(system1);
            context.AddSystem(system2);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    #endregion
}
