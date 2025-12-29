using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing.Tests.Platform;

public class MockLoopProviderTests
{
    #region Construction and Initialization

    [Fact]
    public void Constructor_CreatesInstanceWithDefaultState()
    {
        using var loop = new MockLoopProvider();

        Assert.False(loop.IsInitialized);
        Assert.False(loop.IsRunning);
        Assert.Equal(0, loop.UpdateCount);
        Assert.Equal(0, loop.RenderCount);
        Assert.Equal(0f, loop.TotalTime);
        Assert.Equal(800, loop.Width);
        Assert.Equal(600, loop.Height);
    }

    [Fact]
    public void Initialize_SetsIsInitializedToTrue()
    {
        using var loop = new MockLoopProvider();

        loop.Initialize();

        Assert.True(loop.IsInitialized);
    }

    [Fact]
    public void Initialize_WhenAlreadyInitialized_ThrowsInvalidOperationException()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        var ex = Assert.Throws<InvalidOperationException>(() => loop.Initialize());
        Assert.Equal("Already initialized.", ex.Message);
    }

    #endregion

    #region Run

    [Fact]
    public void Run_SetsIsRunningToTrue()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        loop.Run();

        Assert.True(loop.IsRunning);
    }

    [Fact]
    public void Run_WhenNotInitialized_ThrowsInvalidOperationException()
    {
        using var loop = new MockLoopProvider();

        var ex = Assert.Throws<InvalidOperationException>(() => loop.Run());
        Assert.Equal("Must call Initialize() before Run().", ex.Message);
    }

    [Fact]
    public void Run_WhenAlreadyRunning_ThrowsInvalidOperationException()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        var ex = Assert.Throws<InvalidOperationException>(() => loop.Run());
        Assert.Equal("Already running.", ex.Message);
    }

    [Fact]
    public void Run_IsNonBlocking()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        // Run should return immediately (not block)
        loop.Run();

        Assert.True(loop.IsRunning);
    }

    #endregion

    #region Event Triggering

    [Fact]
    public void TriggerReady_InvokesOnReadyEvent()
    {
        using var loop = new MockLoopProvider();
        var eventFired = false;
        loop.OnReady += () => eventFired = true;

        loop.TriggerReady();

        Assert.True(eventFired);
    }

    [Fact]
    public void TriggerUpdate_IncrementsUpdateCountAndTotalTime()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerUpdate(0.016f);

        Assert.Equal(1, loop.UpdateCount);
        Assert.Equal(0.016f, loop.TotalTime, 5);
    }

    [Fact]
    public void TriggerUpdate_InvokesOnUpdateEvent()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0f;
        loop.OnUpdate += dt => receivedDelta = dt;

        loop.TriggerUpdate(0.016f);

        Assert.Equal(0.016f, receivedDelta, 5);
    }

    [Fact]
    public void TriggerUpdate_AccumulatesTotalTime()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerUpdate(0.016f);
        loop.TriggerUpdate(0.016f);
        loop.TriggerUpdate(0.016f);

        Assert.Equal(3, loop.UpdateCount);
        Assert.Equal(0.048f, loop.TotalTime, 5);
    }

    [Fact]
    public void TriggerRender_IncrementsRenderCount()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerRender(0.016f);

        Assert.Equal(1, loop.RenderCount);
    }

    [Fact]
    public void TriggerRender_InvokesOnRenderEvent()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0f;
        loop.OnRender += dt => receivedDelta = dt;

        loop.TriggerRender(0.016f);

        Assert.Equal(0.016f, receivedDelta, 5);
    }

    [Fact]
    public void TriggerResize_UpdatesWidthAndHeight()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerResize(1920, 1080);

        Assert.Equal(1920, loop.Width);
        Assert.Equal(1080, loop.Height);
    }

    [Fact]
    public void TriggerResize_InvokesOnResizeEvent()
    {
        using var loop = new MockLoopProvider();
        int receivedWidth = 0;
        int receivedHeight = 0;
        loop.OnResize += (w, h) => { receivedWidth = w; receivedHeight = h; };

        loop.TriggerResize(1920, 1080);

        Assert.Equal(1920, receivedWidth);
        Assert.Equal(1080, receivedHeight);
    }

    [Fact]
    public void TriggerClosing_SetsIsRunningToFalse()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        loop.TriggerClosing();

        Assert.False(loop.IsRunning);
    }

    [Fact]
    public void TriggerClosing_InvokesOnClosingEvent()
    {
        using var loop = new MockLoopProvider();
        var eventFired = false;
        loop.OnClosing += () => eventFired = true;

        loop.TriggerClosing();

        Assert.True(eventFired);
    }

    #endregion

    #region Step Methods

    [Fact]
    public void Step_TriggersUpdateAndRender()
    {
        using var loop = new MockLoopProvider();

        loop.Step(0.016f);

        Assert.Equal(1, loop.UpdateCount);
        Assert.Equal(1, loop.RenderCount);
        Assert.Equal(0.016f, loop.TotalTime, 5);
    }

    [Fact]
    public void Step_WithDefaultDelta_Uses60FPS()
    {
        using var loop = new MockLoopProvider();

        loop.Step();

        Assert.Equal(1, loop.UpdateCount);
        Assert.Equal(1, loop.RenderCount);
        Assert.Equal(1f / 60f, loop.TotalTime, 5);
    }

    [Fact]
    public void StepFrames_CallsStepMultipleTimes()
    {
        using var loop = new MockLoopProvider();

        loop.StepFrames(5, 0.016f);

        Assert.Equal(5, loop.UpdateCount);
        Assert.Equal(5, loop.RenderCount);
        Assert.Equal(0.08f, loop.TotalTime, 5);
    }

    [Fact]
    public void StepFrames_WithDefaultDelta_Uses60FPS()
    {
        using var loop = new MockLoopProvider();

        loop.StepFrames(10);

        Assert.Equal(10, loop.UpdateCount);
        Assert.Equal(10, loop.RenderCount);
        Assert.Equal(10f / 60f, loop.TotalTime, 5);
    }

    #endregion

    #region Reset Methods

    [Fact]
    public void ResetCounters_ClearsCountersAndTotalTime()
    {
        using var loop = new MockLoopProvider();
        loop.TriggerUpdate(0.016f);
        loop.TriggerRender(0.016f);

        loop.ResetCounters();

        Assert.Equal(0, loop.UpdateCount);
        Assert.Equal(0, loop.RenderCount);
        Assert.Equal(0f, loop.TotalTime);
    }

    [Fact]
    public void ResetCounters_DoesNotAffectInitializedOrRunningState()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        loop.ResetCounters();

        Assert.True(loop.IsInitialized);
        Assert.True(loop.IsRunning);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();
        loop.TriggerUpdate(0.016f);
        loop.TriggerResize(1920, 1080);

        loop.Reset();

        Assert.False(loop.IsInitialized);
        Assert.False(loop.IsRunning);
        Assert.Equal(0, loop.UpdateCount);
        Assert.Equal(0, loop.RenderCount);
        Assert.Equal(0f, loop.TotalTime);
        Assert.Equal(800, loop.Width);
        Assert.Equal(600, loop.Height);
    }

    [Fact]
    public void Reset_DoesNotClearEventSubscriptions()
    {
        using var loop = new MockLoopProvider();
        var eventFired = false;
        loop.OnReady += () => eventFired = true;

        loop.Reset();
        loop.TriggerReady();

        Assert.True(eventFired);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_CallsReset()
    {
        var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();
        loop.TriggerUpdate(0.016f);

        loop.Dispose();

        Assert.False(loop.IsInitialized);
        Assert.False(loop.IsRunning);
        Assert.Equal(0, loop.UpdateCount);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var loop = new MockLoopProvider();

        loop.Dispose();
        loop.Dispose();

        // Should not throw
    }

    #endregion

    #region Width and Height

    [Fact]
    public void Width_CanBeSetDirectly()
    {
        using var loop = new MockLoopProvider();

        loop.Width = 1024;

        Assert.Equal(1024, loop.Width);
    }

    [Fact]
    public void Height_CanBeSetDirectly()
    {
        using var loop = new MockLoopProvider();

        loop.Height = 768;

        Assert.Equal(768, loop.Height);
    }

    #endregion
}
