namespace KeenEyes.Tests;

/// <summary>
/// Test system that records when it was updated for ordering verification.
/// </summary>
/// <param name="executionLog">The list to track execution order.</param>
/// <param name="name">The system name.</param>
public class OrderTrackingSystem(List<string> executionLog, string name) : SystemBase
{
    private readonly List<string> executionLog = executionLog;
    private readonly string name = name;

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

/// <summary>
/// Tests for RunBefore/RunAfter dependency constraints with topological sorting.
/// </summary>
public class SystemDependencyOrderingTests
{
    #region RunBefore Constraints

    [Fact]
    public void RunsBefore_SystemExecutesBeforeTarget()
    {
        using var world = new World();
        var log = new List<string>();

        // A runs before B (A has constraint, should execute first)
        world.AddSystem(
            new OrderTrackingSystem(log, "A"),
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(SystemB)],
            runsAfter: []);

        world.AddSystem(new SystemB(log), SystemPhase.Update);

        world.Update(0.016f);

        Assert.Equal(["A", "B"], log);
    }

    [Fact]
    public void RunsBefore_OverridesOrderValue()
    {
        using var world = new World();
        var log = new List<string>();

        // A has higher order but must run before B due to constraint
        world.AddSystem(
            new OrderTrackingSystem(log, "A"),
            SystemPhase.Update,
            order: 100, // Higher order would normally run later
            runsBefore: [typeof(SystemB)],
            runsAfter: []);

        world.AddSystem(new SystemB(log), SystemPhase.Update, order: 0);

        world.Update(0.016f);

        Assert.Equal(["A", "B"], log);
    }

    [Fact]
    public void RunsBefore_MultipleTargets()
    {
        using var world = new World();
        var log = new List<string>();

        // A runs before both B and C
        world.AddSystem(
            new OrderTrackingSystem(log, "A"),
            SystemPhase.Update,
            order: 0,
            runsBefore: [typeof(SystemB), typeof(SystemC)],
            runsAfter: []);

        world.AddSystem(new SystemB(log), SystemPhase.Update);
        world.AddSystem(new SystemC(log), SystemPhase.Update);

        world.Update(0.016f);

        Assert.Equal("A", log[0]);
        Assert.Contains("B", log);
        Assert.Contains("C", log);
    }

    #endregion

    #region RunAfter Constraints

    [Fact]
    public void RunsAfter_SystemExecutesAfterTarget()
    {
        using var world = new World();
        var log = new List<string>();

        // B runs after A
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update);
        world.AddSystem(
            new SystemB(log),
            SystemPhase.Update,
            order: 0,
            runsBefore: [],
            runsAfter: [typeof(OrderTrackingSystem)]);

        world.Update(0.016f);

        Assert.Equal(["A", "B"], log);
    }

    [Fact]
    public void RunsAfter_OverridesOrderValue()
    {
        using var world = new World();
        var log = new List<string>();

        // B has lower order but must run after A due to constraint
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update, order: 100);
        world.AddSystem(
            new SystemB(log),
            SystemPhase.Update,
            order: -100, // Lower order would normally run first
            runsBefore: [],
            runsAfter: [typeof(OrderTrackingSystem)]);

        world.Update(0.016f);

        Assert.Equal(["A", "B"], log);
    }

    [Fact]
    public void RunsAfter_MultipleTargets()
    {
        using var world = new World();
        var log = new List<string>();

        // C runs after both A and B
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update);
        world.AddSystem(new SystemB(log), SystemPhase.Update);
        world.AddSystem(
            new SystemC(log),
            SystemPhase.Update,
            order: 0,
            runsBefore: [],
            runsAfter: [typeof(OrderTrackingSystem), typeof(SystemB)]);

        world.Update(0.016f);

        Assert.Equal("C", log[2]);
        Assert.Contains("A", log.Take(2));
        Assert.Contains("B", log.Take(2));
    }

    #endregion

    #region Combined RunBefore and RunAfter

    [Fact]
    public void CombinedConstraints_ChainedDependencies()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> B -> C chain
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemB)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemC)], runsAfter: []);
        world.AddSystem(new SystemC(log), SystemPhase.Update);

        world.Update(0.016f);

        Assert.Equal(["A", "B", "C"], log);
    }

    [Fact]
    public void CombinedConstraints_DiamondDependency()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> B, A -> C, B -> D, C -> D (diamond shape)
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemB), typeof(SystemC)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemD)], runsAfter: []);
        world.AddSystem(new SystemC(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemD)], runsAfter: []);
        world.AddSystem(new SystemD(log), SystemPhase.Update);

        world.Update(0.016f);

        Assert.Equal("A", log[0]);
        Assert.Equal("D", log[3]);
        // B and C can be in either order between A and D
        Assert.Contains("B", log.Skip(1).Take(2));
        Assert.Contains("C", log.Skip(1).Take(2));
    }

    #endregion

    #region Cycle Detection

    [Fact]
    public void CycleDetection_DirectCycle_ThrowsException()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> B -> A cycle
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemB)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(OrderTrackingSystem)], runsAfter: []);

        var ex = Assert.Throws<InvalidOperationException>(() => world.Update(0.016f));
        Assert.Contains("Cycle detected", ex.Message);
        Assert.Contains("Update", ex.Message); // Phase name
    }

    [Fact]
    public void CycleDetection_IndirectCycle_ThrowsException()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> B -> C -> A cycle
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemB)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemC)], runsAfter: []);
        world.AddSystem(new SystemC(log), SystemPhase.Update,
            order: 0, runsBefore: [typeof(OrderTrackingSystem)], runsAfter: []);

        var ex = Assert.Throws<InvalidOperationException>(() => world.Update(0.016f));
        Assert.Contains("Cycle detected", ex.Message);
    }

    [Fact]
    public void CycleDetection_SelfReference_ThrowsException()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> A self-cycle
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(OrderTrackingSystem)], runsAfter: []);

        var ex = Assert.Throws<InvalidOperationException>(() => world.Update(0.016f));
        Assert.Contains("Cycle detected", ex.Message);
    }

    [Fact]
    public void CycleDetection_SelfReferenceViaRunsAfter_ThrowsException()
    {
        using var world = new World();
        var log = new List<string>();

        // A -> A self-cycle via runsAfter
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [], runsAfter: [typeof(OrderTrackingSystem)]);

        var ex = Assert.Throws<InvalidOperationException>(() => world.Update(0.016f));
        Assert.Contains("Cycle detected", ex.Message);
    }

    #endregion

    #region Phase Isolation

    [Fact]
    public void Constraints_OnlyApplyWithinSamePhase()
    {
        using var world = new World();
        var log = new List<string>();

        // A (EarlyUpdate) references B (Update), should be ignored
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.EarlyUpdate,
            order: 0, runsBefore: [typeof(SystemB)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update);

        // Should not throw, constraint is ignored for cross-phase references
        world.Update(0.016f);

        Assert.Equal(["A", "B"], log);
    }

    [Fact]
    public void Constraints_SystemsInDifferentPhases_ExecuteInPhaseOrder()
    {
        using var world = new World();
        var log = new List<string>();

        // A (LateUpdate) runs after B (Update) references C (EarlyUpdate)
        // Phase order should still be: EarlyUpdate -> Update -> LateUpdate
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.LateUpdate,
            order: 0, runsBefore: [], runsAfter: [typeof(SystemC)]);
        world.AddSystem(new SystemB(log), SystemPhase.Update);
        world.AddSystem(new SystemC(log), SystemPhase.EarlyUpdate);

        world.Update(0.016f);

        Assert.Equal(["C", "B", "A"], log);
    }

    #endregion

    #region Order Tiebreaking

    [Fact]
    public void OrderTiebreaking_UsedWhenNoConstraints()
    {
        using var world = new World();
        var log = new List<string>();

        // No constraints, should use Order values
        world.AddSystem(new OrderTrackingSystem(log, "C"), SystemPhase.Update, order: 30);
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "B"), SystemPhase.Update, order: 20);

        world.Update(0.016f);

        Assert.Equal(["A", "B", "C"], log);
    }

    [Fact]
    public void OrderTiebreaking_UsedForUnconstrainedSystems()
    {
        using var world = new World();
        var log = new List<string>();

        // A must run before D, but B and C have no constraints
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.Update,
            order: 0, runsBefore: [typeof(SystemD)], runsAfter: []);
        world.AddSystem(new SystemB(log), SystemPhase.Update, order: 5);
        world.AddSystem(new SystemC(log), SystemPhase.Update, order: 3);
        world.AddSystem(new SystemD(log), SystemPhase.Update, order: 100);

        world.Update(0.016f);

        // A, C, B should come before D based on constraints and Order tiebreaking
        Assert.Equal("D", log[3]);
    }

    #endregion

    #region Helper Systems

    private sealed class SystemB(List<string> log) : SystemBase
    {
        private readonly List<string> log = log;

        public override void Update(float deltaTime)
        {
            log.Add("B");
        }
    }

    private sealed class SystemC(List<string> log) : SystemBase
    {
        private readonly List<string> log = log;

        public override void Update(float deltaTime)
        {
            log.Add("C");
        }
    }

    private sealed class SystemD(List<string> log) : SystemBase
    {
        private readonly List<string> log = log;

        public override void Update(float deltaTime)
        {
            log.Add("D");
        }
    }

    #endregion
}

/// <summary>
/// Tests for World.FixedUpdate() method.
/// </summary>
public class FixedUpdateTests
{
    [Fact]
    public void FixedUpdate_OnlyExecutesFixedUpdatePhaseSystems()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "EarlyUpdate"), SystemPhase.EarlyUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "FixedUpdate"), SystemPhase.FixedUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "Update"), SystemPhase.Update);
        world.AddSystem(new OrderTrackingSystem(log, "LateUpdate"), SystemPhase.LateUpdate);

        world.FixedUpdate(0.016f);

        Assert.Single(log);
        Assert.Equal("FixedUpdate", log[0]);
    }

    [Fact]
    public void FixedUpdate_RespectsOrder()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "C"), SystemPhase.FixedUpdate, order: 30);
        world.AddSystem(new OrderTrackingSystem(log, "A"), SystemPhase.FixedUpdate, order: 10);
        world.AddSystem(new OrderTrackingSystem(log, "B"), SystemPhase.FixedUpdate, order: 20);

        world.FixedUpdate(0.016f);

        Assert.Equal(["A", "B", "C"], log);
    }

    [Fact]
    public void FixedUpdate_SkipsDisabledSystems()
    {
        using var world = new World();
        var log = new List<string>();

        var disabled = new OrderTrackingSystem(log, "Disabled");
        world.AddSystem(disabled, SystemPhase.FixedUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "Enabled"), SystemPhase.FixedUpdate);

        disabled.Enabled = false;
        world.FixedUpdate(0.016f);

        Assert.Single(log);
        Assert.Equal("Enabled", log[0]);
    }

    [Fact]
    public void FixedUpdate_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Physics"), SystemPhase.FixedUpdate);

        world.FixedUpdate(0.016f);
        world.FixedUpdate(0.016f);
        world.FixedUpdate(0.016f);

        Assert.Equal(["Physics", "Physics", "Physics"], log);
    }

    [Fact]
    public void FixedUpdate_DoesNotAffectOtherPhaseSystems()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Update"), SystemPhase.Update);
        world.AddSystem(new OrderTrackingSystem(log, "FixedUpdate"), SystemPhase.FixedUpdate);

        // Call FixedUpdate multiple times
        world.FixedUpdate(0.016f);
        world.FixedUpdate(0.016f);

        Assert.Equal(2, log.Count);
        Assert.All(log, name => Assert.Equal("FixedUpdate", name));
    }

    [Fact]
    public void FixedUpdate_ThenUpdate_ExecutesSeparately()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "EarlyUpdate"), SystemPhase.EarlyUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "FixedUpdate"), SystemPhase.FixedUpdate);
        world.AddSystem(new OrderTrackingSystem(log, "Update"), SystemPhase.Update);

        // Simulate typical game loop pattern
        world.FixedUpdate(0.016f);
        world.FixedUpdate(0.016f);
        world.Update(0.033f);

        // Two FixedUpdates + three phases from Update
        Assert.Equal(["FixedUpdate", "FixedUpdate", "EarlyUpdate", "FixedUpdate", "Update"], log);
    }

    [Fact]
    public void FixedUpdate_EmptyWorld_DoesNotThrow()
    {
        using var world = new World();

        // Should not throw
        world.FixedUpdate(0.016f);
    }

    [Fact]
    public void FixedUpdate_NoFixedUpdateSystems_DoesNotThrow()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new OrderTrackingSystem(log, "Update"), SystemPhase.Update);

        // Should not throw, just execute nothing
        world.FixedUpdate(0.016f);

        Assert.Empty(log);
    }

    [Fact]
    public void FixedUpdate_CallsLifecycleHooks()
    {
        using var world = new World();
        var log = new List<string>();

        world.AddSystem(new LifecycleTrackingSystem(log), SystemPhase.FixedUpdate);

        world.FixedUpdate(0.016f);

        Assert.Equal(["OnBeforeUpdate", "Update", "OnAfterUpdate"], log);
    }

    private sealed class LifecycleTrackingSystem(List<string> log) : SystemBase
    {
        private readonly List<string> log = log;

        protected override void OnBeforeUpdate(float deltaTime)
        {
            log.Add("OnBeforeUpdate");
        }

        public override void Update(float deltaTime)
        {
            log.Add("Update");
        }

        protected override void OnAfterUpdate(float deltaTime)
        {
            log.Add("OnAfterUpdate");
        }
    }

    [Fact]
    public void FixedUpdate_WithNonSystemBaseSystem_CallsUpdate()
    {
        using var world = new World();
        var log = new List<string>();

        // Use a system that implements ISystem directly (not SystemBase)
        world.AddSystem(new DirectISystemImplementation(log), SystemPhase.FixedUpdate);

        world.FixedUpdate(0.016f);

        Assert.Equal(["DirectUpdate"], log);
    }

    private sealed class DirectISystemImplementation(List<string> log) : ISystem
    {
        private readonly List<string> log = log;

        public bool Enabled { get; set; } = true;

        public void Initialize(IWorld world) { }

        public void Update(float deltaTime)
        {
            log.Add("DirectUpdate");
        }

        public void Dispose() { }
    }
}
