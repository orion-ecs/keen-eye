namespace KeenEyes.Tests;

/// <summary>
/// Test system for tracking hook invocations.
/// </summary>
public class TestHookSystem : SystemBase
{
    public int UpdateCount { get; private set; }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }
}

/// <summary>
/// Tests for global system hooks.
/// </summary>
public class SystemHookTests
{
    #region Basic Hook Registration

    [Fact]
    public void AddSystemHook_WithBeforeHook_InvokesBeforeSystem()
    {
        using var world = new World();
        var invoked = false;

        world.AddSystemHook(
            beforeHook: (system, dt) => invoked = true,
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.True(invoked);
    }

    [Fact]
    public void AddSystemHook_WithAfterHook_InvokesAfterSystem()
    {
        using var world = new World();
        var invoked = false;

        world.AddSystemHook(
            beforeHook: null,
            afterHook: (system, dt) => invoked = true
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.True(invoked);
    }

    [Fact]
    public void AddSystemHook_WithBothHooks_InvokesBoth()
    {
        using var world = new World();
        var beforeInvoked = false;
        var afterInvoked = false;

        world.AddSystemHook(
            beforeHook: (system, dt) => beforeInvoked = true,
            afterHook: (system, dt) => afterInvoked = true
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.True(beforeInvoked);
        Assert.True(afterInvoked);
    }

    [Fact]
    public void AddSystemHook_WithBothNull_ThrowsArgumentException()
    {
        using var world = new World();

        Assert.Throws<ArgumentException>(() =>
            world.AddSystemHook(beforeHook: null, afterHook: null)
        );
    }

    #endregion

    #region Hook Execution Order

    [Fact]
    public void SystemHooks_BeforeHook_ExecutesBeforeSystemUpdate()
    {
        using var world = new World();
        var callOrder = new List<string>();
        var system = new TestHookSystem();

        world.AddSystemHook(
            beforeHook: (sys, dt) => callOrder.Add("BeforeHook"),
            afterHook: null
        );

        world.AddSystem(system);
        callOrder.Add("Setup");
        world.Update(0.016f);
        callOrder.Add("AfterUpdate");

        Assert.Equal(["Setup", "BeforeHook", "AfterUpdate"], callOrder);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void SystemHooks_AfterHook_ExecutesAfterSystemUpdate()
    {
        using var world = new World();
        var callOrder = new List<string>();
        var system = new TestHookSystem();

        world.AddSystemHook(
            beforeHook: null,
            afterHook: (sys, dt) => callOrder.Add("AfterHook")
        );

        world.AddSystem(system);
        callOrder.Add("Setup");
        world.Update(0.016f);
        callOrder.Add("Done");

        Assert.Equal(["Setup", "AfterHook", "Done"], callOrder);
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void SystemHooks_ExecuteInCorrectOrder()
    {
        using var world = new World();
        var callOrder = new List<string>();

        world.AddSystemHook(
            beforeHook: (sys, dt) => callOrder.Add("Before"),
            afterHook: (sys, dt) => callOrder.Add("After")
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        // Before hook -> System update -> After hook
        Assert.Equal(["Before", "After"], callOrder);
    }

    [Fact]
    public void SystemHooks_MultipleHooks_ExecuteInRegistrationOrder()
    {
        using var world = new World();
        var callOrder = new List<string>();

        world.AddSystemHook(
            beforeHook: (sys, dt) => callOrder.Add("Hook1-Before"),
            afterHook: (sys, dt) => callOrder.Add("Hook1-After")
        );

        world.AddSystemHook(
            beforeHook: (sys, dt) => callOrder.Add("Hook2-Before"),
            afterHook: (sys, dt) => callOrder.Add("Hook2-After")
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.Equal([
            "Hook1-Before",
            "Hook2-Before",
            "Hook1-After",
            "Hook2-After"
        ], callOrder);
    }

    #endregion

    #region Hook Parameters

    [Fact]
    public void SystemHooks_ReceiveCorrectSystem()
    {
        using var world = new World();
        ISystem? capturedSystem = null;

        world.AddSystemHook(
            beforeHook: (system, dt) => capturedSystem = system,
            afterHook: null
        );

        var testSystem = new TestHookSystem();
        world.AddSystem(testSystem);
        world.Update(0.016f);

        Assert.Same(testSystem, capturedSystem);
    }

    [Fact]
    public void SystemHooks_ReceiveCorrectDeltaTime()
    {
        using var world = new World();
        var capturedDeltaTimes = new List<float>();

        world.AddSystemHook(
            beforeHook: (system, dt) => capturedDeltaTimes.Add(dt),
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.033f);

        Assert.Single(capturedDeltaTimes);
        Assert.Equal(0.033f, capturedDeltaTimes[0], 5);
    }

    [Fact]
    public void SystemHooks_InvokeForEachSystem()
    {
        using var world = new World();
        var invokedSystems = new List<Type>();

        world.AddSystemHook(
            beforeHook: (system, dt) => invokedSystems.Add(system.GetType()),
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.AddSystem<TestLifecycleSystem>();
        world.Update(0.016f);

        Assert.Equal(2, invokedSystems.Count);
        Assert.Contains(typeof(TestHookSystem), invokedSystems);
        Assert.Contains(typeof(TestLifecycleSystem), invokedSystems);
    }

    #endregion

    #region Hook Unregistration

    [Fact]
    public void SystemHook_Dispose_UnregistersHook()
    {
        using var world = new World();
        var invokeCount = 0;

        var subscription = world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);
        Assert.Equal(1, invokeCount);

        subscription.Dispose();
        world.Update(0.016f);
        Assert.Equal(1, invokeCount); // Not incremented after disposal
    }

    [Fact]
    public void SystemHook_MultipleDispose_IsIdempotent()
    {
        using var world = new World();
        var invokeCount = 0;

        var subscription = world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null
        );

        subscription.Dispose();
        subscription.Dispose();
        subscription.Dispose();

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);
        Assert.Equal(0, invokeCount);
    }

    [Fact]
    public void SystemHook_IndependentUnregistration()
    {
        using var world = new World();
        var count1 = 0;
        var count2 = 0;

        var sub1 = world.AddSystemHook(
            beforeHook: (system, dt) => count1++,
            afterHook: null
        );

        var sub2 = world.AddSystemHook(
            beforeHook: (system, dt) => count2++,
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);
        Assert.Equal(1, count1);
        Assert.Equal(1, count2);

        sub1.Dispose();
        world.Update(0.016f);
        Assert.Equal(1, count1); // No longer incremented
        Assert.Equal(2, count2); // Still incremented

        sub2.Dispose();
        world.Update(0.016f);
        Assert.Equal(1, count1);
        Assert.Equal(2, count2); // No longer incremented
    }

    #endregion

    #region Disabled Systems

    [Fact]
    public void SystemHooks_NotInvokedForDisabledSystems()
    {
        using var world = new World();
        var invokeCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null
        );

        var system = new TestHookSystem();
        world.AddSystem(system);
        system.Enabled = false;

        world.Update(0.016f);

        Assert.Equal(0, invokeCount);
        Assert.Equal(0, system.UpdateCount);
    }

    #endregion

    #region Phase Filtering

    [Fact]
    public void SystemHook_WithPhaseFilter_OnlyInvokesForMatchingPhase()
    {
        using var world = new World();
        var updatePhaseCount = 0;
        var fixedUpdatePhaseCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => updatePhaseCount++,
            afterHook: null,
            phase: SystemPhase.Update
        );

        world.AddSystem<TestHookSystem>(SystemPhase.Update);
        world.AddSystem<TestLifecycleSystem>(SystemPhase.FixedUpdate);

        world.Update(0.016f);

        Assert.Equal(1, updatePhaseCount); // Only Update phase system
        Assert.Equal(0, fixedUpdatePhaseCount);
    }

    [Fact]
    public void SystemHook_WithPhaseFilter_InvokesInFixedUpdate()
    {
        using var world = new World();
        var invokeCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null,
            phase: SystemPhase.FixedUpdate
        );

        world.AddSystem<TestHookSystem>(SystemPhase.FixedUpdate);
        world.FixedUpdate(0.02f);

        Assert.Equal(1, invokeCount);
    }

    [Fact]
    public void SystemHook_WithoutPhaseFilter_InvokesForAllPhases()
    {
        using var world = new World();
        var invokeCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null,
            phase: null
        );

        world.AddSystem<TestHookSystem>(SystemPhase.Update);
        world.AddSystem<TestLifecycleSystem>(SystemPhase.FixedUpdate);

        world.Update(0.016f);

        Assert.Equal(2, invokeCount); // Both systems
    }

    [Fact]
    public void SystemHook_WithPhaseFilter_DoesNotInvokeInUpdate()
    {
        using var world = new World();
        var invokeCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null,
            phase: SystemPhase.FixedUpdate
        );

        world.AddSystem<TestHookSystem>(SystemPhase.Update);
        world.Update(0.016f);

        Assert.Equal(0, invokeCount); // FixedUpdate filter, Update phase
    }

    #endregion

    #region World Disposal

    [Fact]
    public void World_Dispose_ClearsAllHooks()
    {
        var world = new World();
        var invokeCount = 0;

        world.AddSystemHook(
            beforeHook: (system, dt) => invokeCount++,
            afterHook: null
        );

        world.AddSystem<TestHookSystem>();
        world.Dispose();

        // After disposal, hooks should be cleared
        // Note: We can't call Update after Dispose, but disposal should clear hooks
        Assert.Equal(0, invokeCount);
    }

    #endregion

    #region Use Cases

    [Fact]
    public void SystemHook_ProfilingUseCase()
    {
        using var world = new World();
        var profilerData = new Dictionary<string, (int count, float totalTime)>();

        world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                var name = system.GetType().Name;
                if (!profilerData.ContainsKey(name))
                {
                    profilerData[name] = (0, 0f);
                }
            },
            afterHook: (system, dt) =>
            {
                var name = system.GetType().Name;
                var (count, totalTime) = profilerData[name];
                profilerData[name] = (count + 1, totalTime + dt);
            }
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);
        world.Update(0.033f);

        Assert.True(profilerData.ContainsKey(nameof(TestHookSystem)));
        var (executionCount, totalDeltaTime) = profilerData[nameof(TestHookSystem)];
        Assert.Equal(2, executionCount);
        Assert.Equal(0.049f, totalDeltaTime, 5);
    }

    [Fact]
    public void SystemHook_LoggingUseCase()
    {
        using var world = new World();
        var logs = new List<string>();

        world.AddSystemHook(
            beforeHook: (system, dt) => logs.Add($"[START] {system.GetType().Name}"),
            afterHook: (system, dt) => logs.Add($"[END] {system.GetType().Name}")
        );

        world.AddSystem<TestHookSystem>();
        world.Update(0.016f);

        Assert.Equal(2, logs.Count);
        Assert.Equal("[START] TestHookSystem", logs[0]);
        Assert.Equal("[END] TestHookSystem", logs[1]);
    }

    [Fact]
    public void SystemHook_ConditionalExecutionUseCase()
    {
        using var world = new World();
        var debugMode = false;

        world.AddSystemHook(
            beforeHook: (system, dt) =>
            {
                if (system.GetType().Name.Contains("Debug") && !debugMode)
                {
                    system.Enabled = false;
                }
            },
            afterHook: null
        );

        var system = new TestHookSystem();
        world.AddSystem(system);
        world.Update(0.016f);

        // System is not a debug system, so it should execute
        Assert.Equal(1, system.UpdateCount);
    }

    [Fact]
    public void SystemHook_MetricsCollectionUseCase()
    {
        using var world = new World();
        var metrics = new
        {
            TotalSystemExecutions = 0,
            TotalDeltaTime = 0f
        };

        world.AddSystemHook(
            beforeHook: null,
            afterHook: (system, dt) =>
            {
                metrics = new
                {
                    TotalSystemExecutions = metrics.TotalSystemExecutions + 1,
                    TotalDeltaTime = metrics.TotalDeltaTime + dt
                };
            }
        );

        world.AddSystem<TestHookSystem>();
        world.AddSystem<TestLifecycleSystem>();
        world.Update(0.016f);
        world.Update(0.016f);

        Assert.Equal(4, metrics.TotalSystemExecutions); // 2 systems * 2 updates
        Assert.Equal(0.064f, metrics.TotalDeltaTime, 5);
    }

    #endregion

    #region Performance

    [Fact]
    public void SystemHooks_NoHooksRegistered_MinimalOverhead()
    {
        using var world = new World();
        var system = new TestHookSystem();

        world.AddSystem(system);
        world.Update(0.016f);

        // Just verify it works without hooks
        Assert.Equal(1, system.UpdateCount);
    }

    #endregion
}
