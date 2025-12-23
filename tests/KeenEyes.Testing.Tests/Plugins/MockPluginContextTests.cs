using KeenEyes.Testing.Plugins;

namespace KeenEyes.Testing.Tests.Plugins;

public class MockPluginContextTests
{
    private sealed class TestExtension
    {
        public string Value { get; set; } = "Test";
    }

    private sealed class AnotherExtension
    {
        public int Number { get; set; } = 42;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidPlugin_CreatesContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Same(plugin, context.Plugin);
        Assert.Empty(context.RegisteredSystems);
        Assert.Empty(context.RegisteredExtensions);
        Assert.Empty(context.RegisteredComponents);
    }

    [Fact]
    public void Constructor_WithNullPlugin_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MockPluginContext(null!));
    }

    [Fact]
    public void Constructor_WithWorld_StoresWorld()
    {
        using var world = new World();
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin, world);

        Assert.Same(world, context.World);
    }

    [Fact]
    public void Constructor_WithoutWorld_WorldPropertyThrows()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<InvalidOperationException>(() => context.World);
    }

    #endregion

    #region AddSystem Tests

    [Fact]
    public void AddSystem_Generic_RegistersSystem()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var system = context.AddSystem<TestCountingSystem>();

        Assert.NotNull(system);
        Assert.Single(context.RegisteredSystems);
        Assert.True(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void AddSystem_Generic_WithPhaseAndOrder_RegistersWithCorrectInfo()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var system = context.AddSystem<TestCountingSystem>(SystemPhase.FixedUpdate, 100);

        var registration = context.GetSystemRegistration<TestCountingSystem>();
        Assert.NotNull(registration);
        Assert.Equal(SystemPhase.FixedUpdate, registration.Value.Phase);
        Assert.Equal(100, registration.Value.Order);
        Assert.Same(system, registration.Value.Instance);
    }

    [Fact]
    public void AddSystem_Generic_WithDependencies_RegistersSystem()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var system = context.AddSystem<TestCountingSystem>(
            SystemPhase.Update,
            0,
            Array.Empty<Type>(),
            Array.Empty<Type>());

        Assert.NotNull(system);
        Assert.True(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void AddSystem_Instance_RegistersSystem()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var system = new TestCountingSystem();

        var result = context.AddSystem(system);

        Assert.Same(system, result);
        Assert.Single(context.RegisteredSystems);
        Assert.True(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void AddSystem_Instance_WithNullSystem_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<ArgumentNullException>(() => context.AddSystem(null!));
    }

    [Fact]
    public void AddSystem_Instance_WithPhaseAndOrder_RegistersWithCorrectInfo()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var system = new TestCountingSystem();

        context.AddSystem(system, SystemPhase.LateUpdate, 200);

        var registration = context.GetSystemRegistration<TestCountingSystem>();
        Assert.NotNull(registration);
        Assert.Equal(SystemPhase.LateUpdate, registration.Value.Phase);
        Assert.Equal(200, registration.Value.Order);
    }

    [Fact]
    public void AddSystem_Instance_WithDependencies_RegistersSystem()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var system = new TestCountingSystem();

        context.AddSystem(system, SystemPhase.Update, 0, Array.Empty<Type>(), Array.Empty<Type>());

        Assert.True(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void AddSystemGroup_RegistersGroup()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var group = new SystemGroup("TestGroup");

        var result = context.AddSystemGroup(group);

        Assert.Same(group, result);
        Assert.Single(context.RegisteredSystems);
    }

    [Fact]
    public void AddSystemGroup_WithNullGroup_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<ArgumentNullException>(() => context.AddSystemGroup(null!));
    }

    #endregion

    #region Extension Tests

    [Fact]
    public void SetExtension_RegistersExtension()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var extension = new TestExtension { Value = "Hello" };

        context.SetExtension(extension);

        Assert.True(context.WasExtensionSet<TestExtension>());
        Assert.Same(extension, context.GetSetExtension<TestExtension>());
        Assert.Single(context.RegisteredExtensions);
    }

    [Fact]
    public void SetExtension_WithNullExtension_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<ArgumentNullException>(() => context.SetExtension<TestExtension>(null!));
    }

    [Fact]
    public void SetExtension_MultipleTimes_RecordsAll()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var ext1 = new TestExtension { Value = "First" };
        var ext2 = new TestExtension { Value = "Second" };

        context.SetExtension(ext1);
        context.SetExtension(ext2);

        Assert.Equal(2, context.RegisteredExtensions.Count);
    }

    [Fact]
    public void GetExtension_WhenSet_ReturnsExtension()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var extension = new TestExtension();

        context.SetExtension(extension);
        var result = context.GetExtension<TestExtension>();

        Assert.Same(extension, result);
    }

    [Fact]
    public void GetExtension_WhenNotSet_ThrowsInvalidOperationException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<InvalidOperationException>(() => context.GetExtension<TestExtension>());
    }

    [Fact]
    public void TryGetExtension_WhenSet_ReturnsTrueAndExtension()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var extension = new TestExtension();

        context.SetExtension(extension);
        var success = context.TryGetExtension<TestExtension>(out var result);

        Assert.True(success);
        Assert.Same(extension, result);
    }

    [Fact]
    public void TryGetExtension_WhenNotSet_ReturnsFalseAndNull()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var success = context.TryGetExtension<TestExtension>(out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void RemoveExtension_WhenSet_ReturnsTrueAndRemovesExtension()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var extension = new TestExtension();

        context.SetExtension(extension);
        var removed = context.RemoveExtension<TestExtension>();

        Assert.True(removed);
        Assert.False(context.TryGetExtension<TestExtension>(out _));
    }

    [Fact]
    public void RemoveExtension_WhenNotSet_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var removed = context.RemoveExtension<TestExtension>();

        Assert.False(removed);
    }

    #endregion

    #region Component Tests

    [Fact]
    public void RegisterComponent_RegistersComponent()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.RegisterComponent<TestPosition>();

        Assert.True(context.WasComponentRegistered<TestPosition>());
        Assert.Single(context.RegisteredComponents);
    }

    [Fact]
    public void RegisterComponent_AsTag_RegistersWithTagFlag()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.RegisterComponent<ActiveTag>(isTag: true);

        Assert.True(context.WasComponentRegistered<ActiveTag>());
        var registered = context.RegisteredComponents.First(c => c.ComponentType == typeof(ActiveTag));
        Assert.True(registered.IsTag);
    }

    [Fact]
    public void RegisterComponent_MultipleTimes_RecordsAll()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.RegisterComponent<TestPosition>();
        context.RegisterComponent<TestVelocity>();
        context.RegisterComponent<TestHealth>();

        Assert.Equal(3, context.RegisteredComponents.Count);
    }

    #endregion

    #region Capability Tests

    [Fact]
    public void SetCapability_StoresCapability()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        context.SetCapability(capability);

        Assert.True(context.WasCapabilitySet<TestExtension>());
        Assert.Same(capability, context.GetSetCapability<TestExtension>());
    }

    [Fact]
    public void SetCapability_WithNullCapability_ThrowsArgumentNullException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.Throws<ArgumentNullException>(() => context.SetCapability<TestExtension>(null!));
    }

    [Fact]
    public void SetCapability_ReturnsContext()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        var result = context.SetCapability(capability);

        Assert.Same(context, result);
    }

    [Fact]
    public void GetCapability_WhenSet_ReturnsCapability()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        context.SetCapability(capability);
        var result = context.GetCapability<TestExtension>();

        Assert.Same(capability, result);
    }

    [Fact]
    public void GetCapability_WhenNotSet_ThrowsInvalidOperationException()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var ex = Assert.Throws<InvalidOperationException>(() => context.GetCapability<TestExtension>());
        Assert.Contains("TestExtension", ex.Message);
        Assert.Contains("SetCapability", ex.Message);
    }

    [Fact]
    public void TryGetCapability_WhenSet_ReturnsTrueAndCapability()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        context.SetCapability(capability);
        var success = context.TryGetCapability<TestExtension>(out var result);

        Assert.True(success);
        Assert.Same(capability, result);
    }

    [Fact]
    public void TryGetCapability_WhenNotSet_ReturnsFalseAndNull()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var success = context.TryGetCapability<TestExtension>(out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryGetCapability_WhenWorldImplementsCapability_ReturnsTrueAndWorld()
    {
        using var world = new World();
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin, world);

        var success = context.TryGetCapability<IWorld>(out var result);

        Assert.True(success);
        Assert.Same(world, result);
    }

    [Fact]
    public void HasCapability_WhenSet_ReturnsTrue()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        context.SetCapability(capability);

        Assert.True(context.HasCapability<TestExtension>());
    }

    [Fact]
    public void HasCapability_WhenNotSet_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.False(context.HasCapability<TestExtension>());
    }

    [Fact]
    public void HasCapability_WhenWorldImplementsCapability_ReturnsTrue()
    {
        using var world = new World();
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin, world);

        Assert.True(context.HasCapability<IWorld>());
    }

    #endregion

    #region Query Methods Tests

    [Fact]
    public void WasSystemRegistered_WhenRegistered_ReturnsTrue()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.AddSystem<TestCountingSystem>();

        Assert.True(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void WasSystemRegistered_WhenNotRegistered_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        Assert.False(context.WasSystemRegistered<TestCountingSystem>());
    }

    [Fact]
    public void WasSystemRegisteredAtPhase_WhenRegisteredAtPhase_ReturnsTrue()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.AddSystem<TestCountingSystem>(SystemPhase.FixedUpdate);

        Assert.True(context.WasSystemRegisteredAtPhase<TestCountingSystem>(SystemPhase.FixedUpdate));
    }

    [Fact]
    public void WasSystemRegisteredAtPhase_WhenRegisteredAtDifferentPhase_ReturnsFalse()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.AddSystem<TestCountingSystem>(SystemPhase.FixedUpdate);

        Assert.False(context.WasSystemRegisteredAtPhase<TestCountingSystem>(SystemPhase.Update));
    }

    [Fact]
    public void GetSystemRegistration_WhenRegistered_ReturnsInfo()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.AddSystem<TestCountingSystem>(SystemPhase.LateUpdate, 50);

        var registration = context.GetSystemRegistration<TestCountingSystem>();

        Assert.NotNull(registration);
        Assert.Equal(typeof(TestCountingSystem), registration.Value.SystemType);
        Assert.Equal(SystemPhase.LateUpdate, registration.Value.Phase);
        Assert.Equal(50, registration.Value.Order);
    }

    [Fact]
    public void GetSystemRegistration_WhenNotRegistered_ReturnsDefault()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var registration = context.GetSystemRegistration<TestCountingSystem>();

        // When not registered, LINQ FirstOrDefault returns default struct, not null
        Assert.False(registration.HasValue);
    }

    [Fact]
    public void GetSetExtension_WhenSet_ReturnsExtension()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var extension = new TestExtension();

        context.SetExtension(extension);
        var result = context.GetSetExtension<TestExtension>();

        Assert.Same(extension, result);
    }

    [Fact]
    public void GetSetExtension_WhenNotSet_ReturnsNull()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var result = context.GetSetExtension<TestExtension>();

        Assert.Null(result);
    }

    [Fact]
    public void GetSetCapability_WhenSet_ReturnsCapability()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);
        var capability = new TestExtension();

        context.SetCapability(capability);
        var result = context.GetSetCapability<TestExtension>();

        Assert.Same(capability, result);
    }

    [Fact]
    public void GetSetCapability_WhenNotSet_ReturnsNull()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        var result = context.GetSetCapability<TestExtension>();

        Assert.Null(result);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllCollections()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.AddSystem<TestCountingSystem>();
        context.SetExtension(new TestExtension());
        context.RegisterComponent<TestPosition>();
        context.SetCapability(new AnotherExtension());

        context.Reset();

        Assert.Empty(context.RegisteredSystems);
        Assert.Empty(context.RegisteredExtensions);
        Assert.Empty(context.RegisteredComponents);
        Assert.False(context.WasCapabilitySet<AnotherExtension>());
    }

    [Fact]
    public void Reset_DoesNotChangePlugin()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        context.Reset();

        Assert.Same(plugin, context.Plugin);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_RegisterMultipleItems_AllTracked()
    {
        var plugin = new MockPlugin("TestPlugin");
        var context = new MockPluginContext(plugin);

        // Register systems
        context.AddSystem<TestCountingSystem>(SystemPhase.Update, 10);

        // Set extensions
        var ext1 = new TestExtension { Value = "Ext1" };
        var ext2 = new AnotherExtension { Number = 100 };
        context.SetExtension(ext1);
        context.SetExtension(ext2);

        // Register components
        context.RegisterComponent<TestPosition>();
        context.RegisterComponent<TestVelocity>();
        context.RegisterComponent<ActiveTag>(isTag: true);

        // Set capabilities
        var cap = new TestExtension { Value = "Cap" };
        context.SetCapability(cap);

        // Verify
        Assert.Single(context.RegisteredSystems);
        Assert.Equal(2, context.RegisteredExtensions.Count);
        Assert.Equal(3, context.RegisteredComponents.Count);
        Assert.True(context.WasCapabilitySet<TestExtension>());
    }

    #endregion
}

public class RegisteredSystemInfoTests
{
    [Fact]
    public void Constructor_WithAllParameters_CreatesRecord()
    {
        var system = new TestCountingSystem();
        var info = new RegisteredSystemInfo(
            typeof(TestCountingSystem),
            SystemPhase.FixedUpdate,
            100,
            system);

        Assert.Equal(typeof(TestCountingSystem), info.SystemType);
        Assert.Equal(SystemPhase.FixedUpdate, info.Phase);
        Assert.Equal(100, info.Order);
        Assert.Same(system, info.Instance);
    }

    [Fact]
    public void Constructor_WithoutInstance_CreatesRecordWithNullInstance()
    {
        var info = new RegisteredSystemInfo(
            typeof(TestCountingSystem),
            SystemPhase.Update,
            0);

        Assert.Equal(typeof(TestCountingSystem), info.SystemType);
        Assert.Null(info.Instance);
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        var info1 = new RegisteredSystemInfo(typeof(TestCountingSystem), SystemPhase.Update, 10);
        var info2 = new RegisteredSystemInfo(typeof(TestCountingSystem), SystemPhase.Update, 10);

        Assert.Equal(info1, info2);
    }
}

public class RegisteredExtensionInfoTests
{
    private sealed class TestExtension { }

    [Fact]
    public void Constructor_CreatesRecord()
    {
        var extension = new TestExtension();
        var info = new RegisteredExtensionInfo(typeof(TestExtension), extension);

        Assert.Equal(typeof(TestExtension), info.ExtensionType);
        Assert.Same(extension, info.Instance);
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        var extension = new TestExtension();
        var info1 = new RegisteredExtensionInfo(typeof(TestExtension), extension);
        var info2 = new RegisteredExtensionInfo(typeof(TestExtension), extension);

        Assert.Equal(info1, info2);
    }
}

public class RegisteredComponentInfoTests
{
    [Fact]
    public void Constructor_CreatesRecord()
    {
        var info = new RegisteredComponentInfo(typeof(TestPosition), IsTag: false);

        Assert.Equal(typeof(TestPosition), info.ComponentType);
        Assert.False(info.IsTag);
    }

    [Fact]
    public void Constructor_AsTag_SetsIsTagTrue()
    {
        var info = new RegisteredComponentInfo(typeof(ActiveTag), IsTag: true);

        Assert.Equal(typeof(ActiveTag), info.ComponentType);
        Assert.True(info.IsTag);
    }

    [Fact]
    public void Equality_WithSameValues_AreEqual()
    {
        var info1 = new RegisteredComponentInfo(typeof(TestPosition), IsTag: false);
        var info2 = new RegisteredComponentInfo(typeof(TestPosition), IsTag: false);

        Assert.Equal(info1, info2);
    }
}
