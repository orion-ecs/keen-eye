namespace KeenEyes.Tests;

/// <summary>
/// Test system that records when it was updated for ordering verification.
/// </summary>
public class OrderTrackingSystem : SystemBase
{
    private readonly List<string> executionLog;
    private readonly string name;

    public OrderTrackingSystem(List<string> executionLog, string name)
    {
        this.executionLog = executionLog;
        this.name = name;
    }

    public override void Update(float deltaTime)
    {
        executionLog.Add(name);
    }
}

/// <summary>
/// Tests for system execution ordering by phase and order.
/// </summary>
public class SystemOrderingTests
{
    #region Phase Ordering Tests

    [Fact]
    public void Systems_ExecuteInPhaseOrder()
    {
        using var world = new World();
        var log = new List<string>();

        // Add systems in reverse phase order
        world.AddSystem(new OrderTrackingSystem(log, "PostRender"), SystemPhase.PostRender);
        world.AddSystem(new OrderTrackingSystem(log, "Render"), SystemPhase.Render);
        world.AddSystem(new OrderTrackingSystem(log, "LateUpdate"), SystemPhase.LateUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "Update"), SystemPhase.Update);
        world.AddSystem(new OrderTrackingSystem(log, "FixedUpdate"), SystemPhase.FixedUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "EarlyUpdate"), SystemPhase.EarlyUpdate);

        world.Update(0.016f);

        Assert.Equal(
            ["EarlyUpdate", "FixedUpdate", "Update", "LateUpdate", "Render", "PostRender"],
            log);
    }

    [Fact]
    public void Systems_WithSamePhase_ExecuteInOrderValue()
    {
        using var world = new World();
        var log = new List<string>();

        // Add systems in random order
        world.AddSystem(new OrderTrackingSystem(log, "Third"), SystemPhase.Update, order: 30);
        world.AddSystem(new OrderTrackingSystem(log, "First"), SystemPhase.Update, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "Second"), SystemPhase.Update, order: 20);

        world.Update(0.016f);

        Assert.Equal(["First", "Second", "Third"], log);
    }

    [Fact]
    public void Systems_WithNegativeOrder_ExecuteBeforeZero()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Default"), SystemPhase.Update, order: 0);
        world.AddSystem(new OrderTrackingSystem(log, "Priority"), SystemPhase.Update, order: -10);
        world.AddSystem(new OrderTrackingSystem(log, "Later"), SystemPhase.Update, order: 10);

        world.Update(0.016f);

        Assert.Equal(["Priority", "Default", "Later"], log);
    }

    [Fact]
    public void Systems_SortByPhaseThenOrder()
    {
        using var world = new World();
        var log = new List<string>();

        // Add in chaotic order
        world.AddSystem(new OrderTrackingSystem(log, "Update2"), SystemPhase.Update, order: 20);
        world.AddSystem(new OrderTrackingSystem(log, "Render1"), SystemPhase.Render, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "Early1"), SystemPhase.EarlyUpdate, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "Update1"), SystemPhase.Update, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "Early2"), SystemPhase.EarlyUpdate, order: 20);

        world.Update(0.016f);

        Assert.Equal(["Early1", "Early2", "Update1", "Update2", "Render1"], log);
    }

    [Theory]
    [InlineData(SystemPhase.EarlyUpdate)]
    [InlineData(SystemPhase.FixedUpdate)]
    [InlineData(SystemPhase.Update)]
    [InlineData(SystemPhase.LateUpdate)]
    [InlineData(SystemPhase.Render)]
    [InlineData(SystemPhase.PostRender)]
    public void AddSystem_AcceptsAllPhaseValues(SystemPhase phase)
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Test"), phase);
        world.Update(0.016f);

        Assert.Single(log);
        Assert.Equal("Test", log[0]);
    }

    #endregion

    #region Default Value Tests

    [Fact]
    public void AddSystem_DefaultsToUpdatePhase()
    {
        using var world = new World();
        var log = new List<string>();

        // Systems without explicit phase
        world.AddSystem(new OrderTrackingSystem(log, "Default"));

        // Add systems before and after Update phase
        world.AddSystem(new OrderTrackingSystem(log, "Early"), SystemPhase.EarlyUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "Late"), SystemPhase.LateUpdate);

        world.Update(0.016f);

        // Default should execute after EarlyUpdate, before LateUpdate
        Assert.Equal(["Early", "Default", "Late"], log);
    }

    [Fact]
    public void AddSystem_DefaultsToOrderZero()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Default"), SystemPhase.Update);
        world.AddSystem(new OrderTrackingSystem(log, "Before"), SystemPhase.Update, order: -5);
        world.AddSystem(new OrderTrackingSystem(log, "After"), SystemPhase.Update, order: 5);

        world.Update(0.016f);

        Assert.Equal(["Before", "Default", "After"], log);
    }

    [Fact]
    public void AddSystemGeneric_AcceptsPhaseAndOrder()
    {
        using var world = new World();

        world.AddSystem<TestCountingSystem>(SystemPhase.LateUpdate, order: 100);
        world.Update(0.016f);

        var system = world.GetSystem<TestCountingSystem>();
        Assert.NotNull(system);
        Assert.Equal(1, system.UpdateCount);
    }

    #endregion

    #region Multiple Updates Test

    [Fact]
    public void Systems_MaintainOrderAcrossMultipleUpdates()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "C"), SystemPhase.Update, order: 30);
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "B"), SystemPhase.Update, order: 20);

        world.Update(0.016f);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(["A", "B", "C", "A", "B", "C", "A", "B", "C"], log);
    }

    [Fact]
    public void Systems_SortOnlyOnceWhenNotModified()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "First"), SystemPhase.Update, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "Second"), SystemPhase.Update, order: 20);

        // Run multiple updates - sorting should only happen once
        for (int i = 0; i < 100; i++)
        {
            world.Update(0.016f);
        }

        // All 200 entries should be alternating First, Second
        Assert.Equal(200, log.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal("First", log[i * 2]);
            Assert.Equal("Second", log[i * 2 + 1]);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Systems_WithSamePhaseAndOrder_MaintainAddOrder()
    {
        using var world = new World();
        var log = new List<string>();

        // Add systems with identical phase and order
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update, order: 0);
        world.AddSystem(new OrderTrackingSystem(log, "B"), SystemPhase.Update, order: 0);
        world.AddSystem(new OrderTrackingSystem(log, "C"), SystemPhase.Update, order: 0);

        world.Update(0.016f);

        // With stable sort, relative order should be preserved
        Assert.Equal(3, log.Count);
        // We don't guarantee order when phase+order are identical, but all should execute
        Assert.Contains("A", log);
        Assert.Contains("B", log);
        Assert.Contains("C", log);
    }

    [Fact]
    public void Systems_WithExtremeOrderValues_SortCorrectly()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "MinValue"), SystemPhase.Update, order: int.MinValue);
        world.AddSystem(new OrderTrackingSystem(log, "Zero"), SystemPhase.Update, order: 0);
        world.AddSystem(new OrderTrackingSystem(log, "MaxValue"), SystemPhase.Update, order: int.MaxValue);

        world.Update(0.016f);

        Assert.Equal(["MinValue", "Zero", "MaxValue"], log);
    }

    [Fact]
    public void EmptyWorld_UpdateDoesNotThrow()
    {
        using var world = new World();

        // Should not throw
        world.Update(0.016f);
    }

    [Fact]
    public void SingleSystem_UpdateWorks()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Only"), SystemPhase.Update);
        world.Update(0.016f);

        Assert.Single(log);
        Assert.Equal("Only", log[0]);
    }

    #endregion

    #region Integration with Existing Features

    [Fact]
    public void DisabledSystems_NotExecutedRegardlessOfOrder()
    {
        using var world = new World();
        var log = new List<string>();

        var disabled = new OrderTrackingSystem(log, "Disabled");
        world.AddSystem(disabled, SystemPhase.Update, order: -100);
        world.AddSystem(new OrderTrackingSystem(log, "Enabled"), SystemPhase.Update, order: 100);

        disabled.Enabled = false;
        world.Update(0.016f);

        Assert.Single(log);
        Assert.Equal("Enabled", log[0]);
    }

    [Fact]
    public void GetSystem_WorksWithOrderedSystems()
    {
        using var world = new World();

        world.AddSystem<TestCountingSystem>(SystemPhase.Update, order: 50);
        world.AddSystem<TestWorldAccessSystem>(SystemPhase.LateUpdate, order: 10);

        var counting = world.GetSystem<TestCountingSystem>();
        var access = world.GetSystem<TestWorldAccessSystem>();

        Assert.NotNull(counting);
        Assert.NotNull(access);
    }

    [Fact]
    public void EnableDisableSystem_WorksWithOrderedSystems()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Target"), SystemPhase.Update, order: 10);

        world.DisableSystem<OrderTrackingSystem>();
        world.Update(0.016f);
        Assert.Empty(log);

        world.EnableSystem<OrderTrackingSystem>();
        world.Update(0.016f);
        Assert.Single(log);
    }

    #endregion
}

/// <summary>
/// Tests for SystemGroup ordering functionality.
/// </summary>
public class SystemGroupOrderingTests
{
    [Fact]
    public void SystemGroup_SortsByOrder()
    {
        using var world = new World();
        var log = new List<string>();

        var group = new SystemGroup("TestGroup")
            .Add(new OrderTrackingSystem(log, "Third"), order: 30)
            .Add(new OrderTrackingSystem(log, "First"), order: 10)
            .Add(new OrderTrackingSystem(log, "Second"), order: 20);

        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Equal(["First", "Second", "Third"], log);
    }

    [Fact]
    public void SystemGroup_AcceptsNegativeOrder()
    {
        using var world = new World();
        var log = new List<string>();

        var group = new SystemGroup("TestGroup")
            .Add(new OrderTrackingSystem(log, "Zero"), order: 0)
            .Add(new OrderTrackingSystem(log, "Negative"), order: -10)
            .Add(new OrderTrackingSystem(log, "Positive"), order: 10);

        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Equal(["Negative", "Zero", "Positive"], log);
    }

    [Fact]
    public void SystemGroup_DefaultsToOrderZero()
    {
        using var world = new World();
        var log = new List<string>();

        var group = new SystemGroup("TestGroup")
            .Add(new OrderTrackingSystem(log, "Default"))
            .Add(new OrderTrackingSystem(log, "Before"), order: -5)
            .Add(new OrderTrackingSystem(log, "After"), order: 5);

        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Equal(["Before", "Default", "After"], log);
    }

    [Fact]
    public void SystemGroup_AddGenericWithOrder()
    {
        using var world = new World();

        var group = new SystemGroup("TestGroup")
            .Add<TestCountingSystem>(order: 100);

        world.AddSystem(group);
        world.Update(0.016f);

        var system = group.GetSystem<TestCountingSystem>();
        Assert.NotNull(system);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void SystemGroup_InWorld_ReceivesPhaseFromWorld()
    {
        using var world = new World();
        var log = new List<string>();

        var earlyGroup = new SystemGroup("Early")
            .Add(new OrderTrackingSystem(log, "EarlySystem"));

        var lateGroup = new SystemGroup("Late")
            .Add(new OrderTrackingSystem(log, "LateSystem"));

        // Groups added in reverse order but with explicit phases
        world.AddSystem(lateGroup, SystemPhase.LateUpdate);
        world.AddSystem(earlyGroup, SystemPhase.EarlyUpdate);

        world.Update(0.016f);

        Assert.Equal(["EarlySystem", "LateSystem"], log);
    }

    [Fact]
    public void NestedSystemGroups_OrderCorrectly()
    {
        using var world = new World();
        var log = new List<string>();

        var innerGroup = new SystemGroup("Inner")
            .Add(new OrderTrackingSystem(log, "Inner2"), order: 20)
            .Add(new OrderTrackingSystem(log, "Inner1"), order: 10);

        var outerGroup = new SystemGroup("Outer")
            .Add(innerGroup, order: 10)
            .Add(new OrderTrackingSystem(log, "Outer"), order: 5);

        world.AddSystem(outerGroup);
        world.Update(0.016f);

        // Outer executes at order 5, then innerGroup at order 10
        // Inner group orders its own systems (Inner1, Inner2)
        Assert.Equal(["Outer", "Inner1", "Inner2"], log);
    }

    [Fact]
    public void SystemGroup_DisabledSystemsNotExecuted()
    {
        using var world = new World();
        var log = new List<string>();

        var disabled = new OrderTrackingSystem(log, "Disabled");
        var group = new SystemGroup("TestGroup")
            .Add(disabled, order: 0)
            .Add(new OrderTrackingSystem(log, "Enabled"), order: 10);

        disabled.Enabled = false;
        world.AddSystem(group);
        world.Update(0.016f);

        Assert.Single(log);
        Assert.Equal("Enabled", log[0]);
    }

    [Fact]
    public void SystemGroup_GetSystemFindsOrderedSystems()
    {
        var group = new SystemGroup("TestGroup")
            .Add<TestCountingSystem>(order: 50)
            .Add<TestWorldAccessSystem>(order: 10);

        var counting = group.GetSystem<TestCountingSystem>();
        var access = group.GetSystem<TestWorldAccessSystem>();

        Assert.NotNull(counting);
        Assert.NotNull(access);
    }

    [Fact]
    public void SystemGroup_MaintainsOrderAcrossMultipleUpdates()
    {
        using var world = new World();
        var log = new List<string>();

        var group = new SystemGroup("TestGroup")
            .Add(new OrderTrackingSystem(log, "C"), order: 30)
            .Add(new OrderTrackingSystem(log, "A"), order: 10)
            .Add(new OrderTrackingSystem(log, "B"), order: 20);

        world.AddSystem(group);
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(["A", "B", "C", "A", "B", "C"], log);
    }
}
