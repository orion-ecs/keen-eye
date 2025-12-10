namespace KeenEyes.Tests;

/// <summary>
/// Test system that tracks initialization and updates.
/// </summary>
public class TestCountingSystem : SystemBase
{
    public int InitializeCount { get; private set; }
    public int UpdateCount { get; private set; }
    public float TotalDeltaTime { get; private set; }
    public bool WasDisposed { get; private set; }

    protected override void OnInitialize()
    {
        InitializeCount++;
    }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
        TotalDeltaTime += deltaTime;
    }

    public override void Dispose()
    {
        WasDisposed = true;
        base.Dispose();
    }
}

/// <summary>
/// Test system that accesses the World during update.
/// </summary>
public class TestWorldAccessSystem : SystemBase
{
    public int EntityCount { get; private set; }

    public override void Update(float deltaTime)
    {
        EntityCount = World.GetAllEntities().Count();
    }
}

/// <summary>
/// Test system that modifies entity components.
/// </summary>
public class TestMovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<TestPosition, TestVelocity>())
        {
            ref var position = ref World.Get<TestPosition>(entity);
            ref readonly var velocity = ref World.Get<TestVelocity>(entity);

            position.X += velocity.X * deltaTime;
            position.Y += velocity.Y * deltaTime;
        }
    }
}

/// <summary>
/// Test system without parameterless constructor for error testing.
/// </summary>
/// <param name="name">The system name.</param>
public class TestNonDefaultConstructorSystem(string name) : ISystem
{
    private readonly string name = name;

    public string Name => name;
    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world) { }
    public void Update(float deltaTime) { }
    public void Dispose() { }
}

/// <summary>
/// Tests for SystemBase abstract class.
/// </summary>
public class SystemBaseTests
{
    [Fact]
    public void SystemBase_Initialize_SetsWorld()
    {
        using var world = new World();
        var system = new TestWorldAccessSystem();

        system.Initialize(world);
        world.Spawn().With(new TestPosition { X = 0f, Y = 0f }).Build();
        system.Update(0f);

        Assert.Equal(1, system.EntityCount);
    }

    [Fact]
    public void SystemBase_Initialize_CallsOnInitialize()
    {
        using var world = new World();
        var system = new TestCountingSystem();

        system.Initialize(world);

        Assert.Equal(1, system.InitializeCount);
    }

    [Fact]
    public void SystemBase_World_ThrowsBeforeInitialize()
    {
        var system = new TestWorldAccessSystem();

        Assert.Throws<InvalidOperationException>(() => system.Update(0f));
    }

    [Fact]
    public void SystemBase_Update_ReceivesDeltaTime()
    {
        using var world = new World();
        var system = new TestCountingSystem();
        system.Initialize(world);

        system.Update(0.016f);
        system.Update(0.016f);
        system.Update(0.016f);

        Assert.Equal(3, system.UpdateCount);
        Assert.Equal(0.048f, system.TotalDeltaTime, 5);
    }

    [Fact]
    public void SystemBase_Dispose_CanBeOverridden()
    {
        using var world = new World();
        var system = new TestCountingSystem();
        system.Initialize(world);

        system.Dispose();

        Assert.True(system.WasDisposed);
    }

    [Fact]
    public void SystemBase_Update_CanQueryAndModifyEntities()
    {
        using var world = new World();
        var system = new TestMovementSystem();
        system.Initialize(world);

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 10f, Y = 5f })
            .Build();

        system.Update(1.0f);

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(5f, position.Y);
    }
}

/// <summary>
/// Tests for SystemGroup class.
/// </summary>
public class SystemGroupTests
{
    [Fact]
    public void SystemGroup_Constructor_SetsName()
    {
        var group = new SystemGroup("UpdateGroup");

        Assert.Equal("UpdateGroup", group.Name);
    }

    [Fact]
    public void SystemGroup_AddGeneric_AddsSystem()
    {
        using var world = new World();
        var group = new SystemGroup("TestGroup");

        group.Add<TestCountingSystem>();
        group.Initialize(world);
        group.Update(0.016f);

        // If we got here without exception, the system was added and updated
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_AddInstance_AddsSystem()
    {
        using var world = new World();
        var group = new SystemGroup("TestGroup");
        var system = new TestCountingSystem();

        group.Add(system);
        group.Initialize(world);
        group.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_Add_ReturnsSelf_ForChaining()
    {
        var group = new SystemGroup("TestGroup");

        var result = group.Add<TestCountingSystem>();

        Assert.Same(group, result);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_AddInstance_ReturnsSelf_ForChaining()
    {
        var group = new SystemGroup("TestGroup");
        var system = new TestCountingSystem();

        var result = group.Add(system);

        Assert.Same(group, result);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_Initialize_InitializesAllSystems()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup")
            .Add(system1)
            .Add(system2);

        group.Initialize(world);

        Assert.Equal(1, system1.InitializeCount);
        Assert.Equal(1, system2.InitializeCount);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_Update_UpdatesAllSystems()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup")
            .Add(system1)
            .Add(system2);

        group.Initialize(world);
        group.Update(0.016f);

        Assert.Equal(1, system1.UpdateCount);
        Assert.Equal(1, system2.UpdateCount);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_Update_PassesDeltaTimeToAllSystems()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup")
            .Add(system1)
            .Add(system2);

        group.Initialize(world);
        group.Update(0.5f);

        Assert.Equal(0.5f, system1.TotalDeltaTime);
        Assert.Equal(0.5f, system2.TotalDeltaTime);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_Dispose_DisposesAllSystems()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup")
            .Add(system1)
            .Add(system2);

        group.Initialize(world);
        group.Dispose();

        Assert.True(system1.WasDisposed);
        Assert.True(system2.WasDisposed);
    }

    [Fact]
    public void SystemGroup_Dispose_ClearsSystems()
    {
        using var world = new World();
        var system = new TestCountingSystem();

        var group = new SystemGroup("TestGroup").Add(system);
        group.Initialize(world);
        group.Dispose();

        // After dispose, update should not affect the system anymore
        // because the systems list is cleared
        group.Update(0.016f);

        Assert.Equal(0, system.UpdateCount);
    }

    [Fact]
    public void SystemGroup_AddAfterInitialize_InitializesNewSystem()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup").Add(system1);
        group.Initialize(world);

        // Add second system after initialization
        group.Add(system2);

        Assert.Equal(1, system1.InitializeCount);
        Assert.Equal(1, system2.InitializeCount); // Should be auto-initialized
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_AddGenericAfterInitialize_InitializesNewSystem()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();

        var group = new SystemGroup("TestGroup").Add(system1);
        group.Initialize(world);

        // Add system using generic method after initialization
        group.Add<TestWorldAccessSystem>();

        // Verify the group can update without errors (system is initialized)
        group.Update(0.016f);
        group.Dispose();
    }

    [Fact]
    public void SystemGroup_CanBeNested()
    {
        using var world = new World();
        var system = new TestCountingSystem();

        var innerGroup = new SystemGroup("InnerGroup").Add(system);
        var outerGroup = new SystemGroup("OuterGroup").Add(innerGroup);

        outerGroup.Initialize(world);
        outerGroup.Update(0.016f);

        Assert.Equal(1, system.InitializeCount);
        Assert.Equal(1, system.UpdateCount);
        outerGroup.Dispose();
    }

    [Fact]
    public void SystemGroup_EmptyGroup_UpdateDoesNotThrow()
    {
        using var world = new World();
        var group = new SystemGroup("EmptyGroup");

        group.Initialize(world);
        group.Update(0.016f); // Should not throw

        group.Dispose();
    }

    [Fact]
    public void SystemGroup_CanAddNonDefaultConstructorSystem()
    {
        using var world = new World();
        var system = new TestNonDefaultConstructorSystem("MySystem");

        var group = new SystemGroup("TestGroup").Add(system);
        group.Initialize(world);
        group.Update(0.016f);

        Assert.Equal("MySystem", system.Name);
        group.Dispose();
    }
}

/// <summary>
/// Tests for World.AddSystem functionality.
/// </summary>
public class WorldSystemTests
{
    [Fact]
    public void World_AddSystemGeneric_AddsAndInitializesSystem()
    {
        using var world = new World();

        world.AddSystem<TestCountingSystem>();
        world.Update(0.016f);

        // If we got here without exception, the system was added and updated
    }

    [Fact]
    public void World_AddSystemInstance_AddsAndInitializesSystem()
    {
        using var world = new World();
        var system = new TestCountingSystem();

        world.AddSystem(system);
        world.Update(0.016f);

        Assert.Equal(1, system.InitializeCount);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void World_AddSystem_ReturnsSelf_ForChaining()
    {
        using var world = new World();

        var result = world.AddSystem<TestCountingSystem>();

        Assert.Same(world, result);
    }

    [Fact]
    public void World_Update_UpdatesAllSystems()
    {
        using var world = new World();
        var system1 = new TestCountingSystem();
        var system2 = new TestCountingSystem();

        world.AddSystem(system1).AddSystem(system2);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(2, system1.UpdateCount);
        Assert.Equal(2, system2.UpdateCount);
    }

    [Fact]
    public void World_Dispose_DisposesAllSystems()
    {
        var system = new TestCountingSystem();
        var world = new World();

        world.AddSystem(system);
        world.Dispose();

        Assert.True(system.WasDisposed);
    }

    [Fact]
    public void World_AddSystemGroup_WorksCorrectly()
    {
        using var world = new World();
        var system = new TestCountingSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Equal(1, system.UpdateCount);
    }
}

/// <summary>
/// Test system that tracks lifecycle hook invocations.
/// </summary>
public class TestLifecycleSystem : SystemBase
{
    public int BeforeUpdateCount { get; private set; }
    public int AfterUpdateCount { get; private set; }
    public int EnabledCount { get; private set; }
    public int DisabledCount { get; private set; }
    public int UpdateCount { get; private set; }
    public float LastDeltaTime { get; private set; }
    public List<string> CallOrder { get; } = [];

    protected override void OnBeforeUpdate(float deltaTime)
    {
        BeforeUpdateCount++;
        LastDeltaTime = deltaTime;
        CallOrder.Add("BeforeUpdate");
    }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
        CallOrder.Add("Update");
    }

    protected override void OnAfterUpdate(float deltaTime)
    {
        AfterUpdateCount++;
        CallOrder.Add("AfterUpdate");
    }

    protected override void OnEnabled()
    {
        EnabledCount++;
        CallOrder.Add("Enabled");
    }

    protected override void OnDisabled()
    {
        DisabledCount++;
        CallOrder.Add("Disabled");
    }
}

/// <summary>
/// Tests for system lifecycle hooks.
/// </summary>
public class SystemLifecycleHookTests
{
    [Fact]
    public void OnBeforeUpdate_CalledBeforeUpdate()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        world.Update(0.016f);

        Assert.Equal(1, system.BeforeUpdateCount);
        Assert.Equal(1, system.UpdateCount);
        Assert.Equal(["BeforeUpdate", "Update", "AfterUpdate"], system.CallOrder);
    }

    [Fact]
    public void OnAfterUpdate_CalledAfterUpdate()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        world.Update(0.016f);

        Assert.Equal(1, system.AfterUpdateCount);
        Assert.Equal(["BeforeUpdate", "Update", "AfterUpdate"], system.CallOrder);
    }

    [Fact]
    public void OnBeforeUpdate_ReceivesDeltaTime()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        world.Update(0.033f);

        Assert.Equal(0.033f, system.LastDeltaTime, 5);
    }

    [Fact]
    public void LifecycleHooks_CalledInCorrectOrder()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(
            ["BeforeUpdate", "Update", "AfterUpdate", "BeforeUpdate", "Update", "AfterUpdate"],
            system.CallOrder);
    }

    [Fact]
    public void OnEnabled_CalledWhenEnabled()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;
        system.Enabled = true;

        Assert.Equal(1, system.EnabledCount);
    }

    [Fact]
    public void OnDisabled_CalledWhenDisabled()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;

        Assert.Equal(1, system.DisabledCount);
    }

    [Fact]
    public void OnEnabled_NotCalledWhenAlreadyEnabled()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = true; // Already enabled, should not trigger callback
        system.Enabled = true;

        Assert.Equal(0, system.EnabledCount);
    }

    [Fact]
    public void OnDisabled_NotCalledWhenAlreadyDisabled()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;
        system.Enabled = false; // Already disabled, should not trigger callback

        Assert.Equal(1, system.DisabledCount);
    }

    [Fact]
    public void LifecycleHooks_InSystemGroup_CalledCorrectly()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Equal(1, system.BeforeUpdateCount);
        Assert.Equal(1, system.UpdateCount);
        Assert.Equal(1, system.AfterUpdateCount);
        Assert.Equal(["BeforeUpdate", "Update", "AfterUpdate"], system.CallOrder);
    }
}

/// <summary>
/// Tests for system runtime control (enable/disable).
/// </summary>
public class SystemRuntimeControlTests
{
    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        var system = new TestLifecycleSystem();

        Assert.True(system.Enabled);
    }

    [Fact]
    public void DisabledSystem_NotUpdated()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;
        world.Update(0.016f);

        Assert.Equal(0, system.UpdateCount);
        Assert.Equal(0, system.BeforeUpdateCount);
        Assert.Equal(0, system.AfterUpdateCount);
    }

    [Fact]
    public void GetSystem_ReturnsSystem()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        var retrieved = world.GetSystem<TestLifecycleSystem>();

        Assert.Same(system, retrieved);
    }

    [Fact]
    public void GetSystem_ReturnsNull_WhenNotFound()
    {
        using var world = new World();

        var retrieved = world.GetSystem<TestLifecycleSystem>();

        Assert.Null(retrieved);
    }

    [Fact]
    public void GetSystem_FindsSystemInGroup()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        world.AddSystem(group);
        var retrieved = world.GetSystem<TestLifecycleSystem>();

        Assert.Same(system, retrieved);
    }

    [Fact]
    public void GetSystem_FindsSystemInNestedGroups()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();
        var innerGroup = new SystemGroup("Inner").Add(system);
        var outerGroup = new SystemGroup("Outer").Add(innerGroup);

        world.AddSystem(outerGroup);
        var retrieved = world.GetSystem<TestLifecycleSystem>();

        Assert.Same(system, retrieved);
    }

    [Fact]
    public void EnableSystem_EnablesDisabledSystem()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;

        var result = world.EnableSystem<TestLifecycleSystem>();

        Assert.True(result);
        Assert.True(system.Enabled);
    }

    [Fact]
    public void EnableSystem_ReturnsFalse_WhenNotFound()
    {
        using var world = new World();

        var result = world.EnableSystem<TestLifecycleSystem>();

        Assert.False(result);
    }

    [Fact]
    public void DisableSystem_DisablesEnabledSystem()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);

        var result = world.DisableSystem<TestLifecycleSystem>();

        Assert.True(result);
        Assert.False(system.Enabled);
    }

    [Fact]
    public void DisableSystem_ReturnsFalse_WhenNotFound()
    {
        using var world = new World();

        var result = world.DisableSystem<TestLifecycleSystem>();

        Assert.False(result);
    }

    [Fact]
    public void EnableSystem_TriggersOnEnabledCallback()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.Enabled = false;
        system.CallOrder.Clear();

        world.EnableSystem<TestLifecycleSystem>();

        Assert.Contains("Enabled", system.CallOrder);
    }

    [Fact]
    public void DisableSystem_TriggersOnDisabledCallback()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);
        system.CallOrder.Clear();

        world.DisableSystem<TestLifecycleSystem>();

        Assert.Contains("Disabled", system.CallOrder);
    }

    [Fact]
    public void DisabledSystem_InGroup_NotUpdated()
    {
        using var world = new World();
        var system1 = new TestLifecycleSystem();
        var system2 = new TestLifecycleSystem();
        var group = new SystemGroup("TestGroup").Add(system1).Add(system2);

        world.AddSystem(group);
        system1.Enabled = false;
        world.Update(0.016f);

        Assert.Equal(0, system1.UpdateCount);
        Assert.Equal(1, system2.UpdateCount);
    }

    [Fact]
    public void SystemGroup_Enabled_ControlsEntireGroup()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        world.AddSystem(group);
        group.Enabled = false;
        world.Update(0.016f);

        Assert.Equal(0, system.UpdateCount);
    }

    [Fact]
    public void SystemGroup_GetSystem_FindsSystem()
    {
        var system = new TestLifecycleSystem();
        var group = new SystemGroup("TestGroup").Add(system);

        var retrieved = group.GetSystem<TestLifecycleSystem>();

        Assert.Same(system, retrieved);
    }

    [Fact]
    public void SystemGroup_GetSystem_ReturnsNull_WhenNotFound()
    {
        var group = new SystemGroup("TestGroup");

        var retrieved = group.GetSystem<TestLifecycleSystem>();

        Assert.Null(retrieved);
    }

    [Fact]
    public void EnableDisable_Toggle_WorksCorrectly()
    {
        using var world = new World();
        var system = new TestLifecycleSystem();

        world.AddSystem(system);

        // Initial state: enabled
        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount);

        // Disable
        world.DisableSystem<TestLifecycleSystem>();
        world.Update(0.016f);
        Assert.Equal(1, system.UpdateCount); // No change

        // Re-enable
        world.EnableSystem<TestLifecycleSystem>();
        world.Update(0.016f);
        Assert.Equal(2, system.UpdateCount); // Incremented
    }

    [Fact]
    public void BaseOnEnabled_CalledWhenNoOverride()
    {
        // TestCountingSystem doesn't override OnEnabled, so base implementation is called
        using var world = new World();
        var system = new TestCountingSystem();

        world.AddSystem(system);
        system.Enabled = false;
        system.Enabled = true; // Triggers base OnEnabled()

        // Should not throw - base implementation is empty but should be covered
        Assert.True(system.Enabled);
    }

    [Fact]
    public void BaseOnDisabled_CalledWhenNoOverride()
    {
        // TestCountingSystem doesn't override OnDisabled, so base implementation is called
        using var world = new World();
        var system = new TestCountingSystem();

        world.AddSystem(system);
        system.Enabled = false; // Triggers base OnDisabled()

        // Should not throw - base implementation is empty but should be covered
        Assert.False(system.Enabled);
    }
}
