using KeenEyes.Capabilities;
using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockSystemHookCapabilityTests
{
    #region AddSystemHook

    [Fact]
    public void AddSystemHook_WithBeforeHook_RegistersHook()
    {
        var capability = new MockSystemHookCapability();

        var subscription = capability.AddSystemHook(beforeHook: (_, _) => { });

        Assert.Equal(1, capability.HookCount);
        Assert.True(capability.WasHookAdded);
        Assert.True(capability.HasBeforeHook);
    }

    [Fact]
    public void AddSystemHook_WithAfterHook_RegistersHook()
    {
        var capability = new MockSystemHookCapability();

        var subscription = capability.AddSystemHook(afterHook: (_, _) => { });

        Assert.Equal(1, capability.HookCount);
        Assert.True(capability.HasAfterHook);
    }

    [Fact]
    public void AddSystemHook_WithBothHooks_RegistersHook()
    {
        var capability = new MockSystemHookCapability();

        var subscription = capability.AddSystemHook(
            beforeHook: (_, _) => { },
            afterHook: (_, _) => { });

        Assert.Equal(1, capability.HookCount);
        Assert.True(capability.HasBeforeHook);
        Assert.True(capability.HasAfterHook);
    }

    [Fact]
    public void AddSystemHook_WithPhase_RegistersHookWithPhase()
    {
        var capability = new MockSystemHookCapability();

        var subscription = capability.AddSystemHook(
            beforeHook: (_, _) => { },
            phase: SystemPhase.Update);

        Assert.True(capability.HasHookForPhase(SystemPhase.Update));
        Assert.False(capability.HasHookForPhase(SystemPhase.FixedUpdate));
    }

    [Fact]
    public void AddSystemHook_WithNoHooks_ThrowsArgumentException()
    {
        var capability = new MockSystemHookCapability();

        Assert.Throws<ArgumentException>(() => capability.AddSystemHook());
    }

    [Fact]
    public void AddSystemHook_ReturnsSubscription_ThatRemovesHook()
    {
        var capability = new MockSystemHookCapability();
        var subscription = capability.AddSystemHook(beforeHook: (_, _) => { });

        subscription.Dispose();

        Assert.Equal(0, capability.HookCount);
        Assert.False(capability.WasHookAdded);
    }

    #endregion

    #region SimulateSystemExecution

    [Fact]
    public void SimulateSystemExecution_InvokesBeforeHook()
    {
        var capability = new MockSystemHookCapability();
        var invoked = false;
        capability.AddSystemHook(beforeHook: (_, _) => invoked = true);
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f);

        Assert.True(invoked);
    }

    [Fact]
    public void SimulateSystemExecution_InvokesAfterHook()
    {
        var capability = new MockSystemHookCapability();
        var invoked = false;
        capability.AddSystemHook(afterHook: (_, _) => invoked = true);
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f);

        Assert.True(invoked);
    }

    [Fact]
    public void SimulateSystemExecution_PassesCorrectParameters()
    {
        var capability = new MockSystemHookCapability();
        ISystem? receivedSystem = null;
        float receivedDelta = 0;
        capability.AddSystemHook(beforeHook: (s, dt) =>
        {
            receivedSystem = s;
            receivedDelta = dt;
        });
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f);

        Assert.Same(mockSystem, receivedSystem);
        Assert.Equal(0.016f, receivedDelta);
    }

    [Fact]
    public void SimulateSystemExecution_WhenHookExecutionDisabled_DoesNotInvokeHooks()
    {
        var capability = new MockSystemHookCapability();
        var invoked = false;
        capability.AddSystemHook(beforeHook: (_, _) => invoked = true);
        capability.EnableHookExecution = false;
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f);

        Assert.False(invoked);
    }

    [Fact]
    public void SimulateSystemExecution_WithPhaseFilter_OnlyInvokesMatchingHooks()
    {
        var capability = new MockSystemHookCapability();
        var updateInvoked = false;
        var fixedUpdateInvoked = false;
        capability.AddSystemHook(beforeHook: (_, _) => updateInvoked = true, phase: SystemPhase.Update);
        capability.AddSystemHook(beforeHook: (_, _) => fixedUpdateInvoked = true, phase: SystemPhase.FixedUpdate);
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f, SystemPhase.Update);

        Assert.True(updateInvoked);
        Assert.False(fixedUpdateInvoked);
    }

    [Fact]
    public void SimulateSystemExecution_WithNoPhase_InvokesAllHooks()
    {
        var capability = new MockSystemHookCapability();
        var updateInvoked = false;
        var noPhaseInvoked = false;
        capability.AddSystemHook(beforeHook: (_, _) => updateInvoked = true, phase: SystemPhase.Update);
        capability.AddSystemHook(beforeHook: (_, _) => noPhaseInvoked = true);
        var mockSystem = new MockSystem();

        capability.SimulateSystemExecution(mockSystem, 0.016f);

        Assert.True(updateInvoked);
        Assert.True(noPhaseInvoked);
    }

    #endregion

    #region GetHook

    [Fact]
    public void GetHook_ReturnsHookAtIndex()
    {
        var capability = new MockSystemHookCapability();
        SystemHook beforeHook = (_, _) => { };
        capability.AddSystemHook(beforeHook: beforeHook, phase: SystemPhase.Update);

        var hook = capability.GetHook(0);

        Assert.Equal(beforeHook, hook.BeforeHook);
        Assert.Equal(SystemPhase.Update, hook.Phase);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllHooks()
    {
        var capability = new MockSystemHookCapability();
        capability.AddSystemHook(beforeHook: (_, _) => { });
        capability.AddSystemHook(afterHook: (_, _) => { });

        capability.Clear();

        Assert.Equal(0, capability.HookCount);
        Assert.False(capability.WasHookAdded);
    }

    [Fact]
    public void Clear_DisposesSubscriptions()
    {
        var capability = new MockSystemHookCapability();
        var sub1 = capability.AddSystemHook(beforeHook: (_, _) => { });
        var sub2 = capability.AddSystemHook(afterHook: (_, _) => { });

        capability.Clear();

        // After clear, hooks should be removed
        Assert.Empty(capability.RegisteredHooks);
    }

    #endregion

    #region RegisteredHooks

    [Fact]
    public void RegisteredHooks_ReturnsAllHooks()
    {
        var capability = new MockSystemHookCapability();
        capability.AddSystemHook(beforeHook: (_, _) => { }, phase: SystemPhase.Update);
        capability.AddSystemHook(afterHook: (_, _) => { }, phase: SystemPhase.Render);

        var hooks = capability.RegisteredHooks;

        Assert.Equal(2, hooks.Count);
        Assert.Equal(SystemPhase.Update, hooks[0].Phase);
        Assert.Equal(SystemPhase.Render, hooks[1].Phase);
    }

    #endregion

    private sealed class MockSystem : ISystem
    {
        public bool Enabled { get; set; } = true;
        public void Initialize(IWorld world) { }
        public void Update(float deltaTime) { }
        public void Dispose() { }
    }
}

public class RegisteredHookInfoTests
{
    [Fact]
    public void Constructor_StoresAllProperties()
    {
        SystemHook before = (_, _) => { };
        SystemHook after = (_, _) => { };
        SystemPhase phase = SystemPhase.Update;

        var info = new RegisteredHookInfo(before, after, phase);

        Assert.Equal(before, info.BeforeHook);
        Assert.Equal(after, info.AfterHook);
        Assert.Equal(phase, info.Phase);
    }

    [Fact]
    public void Constructor_WithNullHooks_StoresNulls()
    {
        var info = new RegisteredHookInfo(null, null, null);

        Assert.Null(info.BeforeHook);
        Assert.Null(info.AfterHook);
        Assert.Null(info.Phase);
    }
}
