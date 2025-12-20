using KeenEyes.Testing.Platform;

namespace KeenEyes.Testing.Tests.Platform;

public class MockLoopProviderTests
{
    #region Initialize Tests

    [Fact]
    public void Initialize_SetsIsInitializedToTrue()
    {
        using var loop = new MockLoopProvider();

        loop.Initialize();

        loop.IsInitialized.ShouldBeTrue();
    }

    [Fact]
    public void Initialize_WhenAlreadyInitialized_Throws()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        Should.Throw<InvalidOperationException>(() => loop.Initialize());
    }

    #endregion

    #region Run Tests

    [Fact]
    public void Run_SetsIsRunningToTrue()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        loop.Run();

        loop.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public void Run_WithoutInitialize_Throws()
    {
        using var loop = new MockLoopProvider();

        Should.Throw<InvalidOperationException>(() => loop.Run());
    }

    [Fact]
    public void Run_WhenAlreadyRunning_Throws()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        Should.Throw<InvalidOperationException>(() => loop.Run());
    }

    [Fact]
    public void Run_IsNonBlocking()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();

        // This should return immediately, not block
        loop.Run();

        loop.IsRunning.ShouldBeTrue();
    }

    #endregion

    #region TriggerReady Tests

    [Fact]
    public void TriggerReady_FiresOnReadyEvent()
    {
        using var loop = new MockLoopProvider();
        var eventFired = false;
        loop.OnReady += () => eventFired = true;

        loop.TriggerReady();

        eventFired.ShouldBeTrue();
    }

    #endregion

    #region TriggerUpdate Tests

    [Fact]
    public void TriggerUpdate_IncrementsUpdateCount()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerUpdate(0.016f);

        loop.UpdateCount.ShouldBe(1);
    }

    [Fact]
    public void TriggerUpdate_AccumulatesTotalTime()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerUpdate(0.1f);
        loop.TriggerUpdate(0.2f);
        loop.TriggerUpdate(0.15f);

        loop.TotalTime.ShouldBe(0.45f, tolerance: 0.0001f);
    }

    [Fact]
    public void TriggerUpdate_FiresOnUpdateEvent()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0;
        loop.OnUpdate += dt => receivedDelta = dt;

        loop.TriggerUpdate(0.033f);

        receivedDelta.ShouldBe(0.033f, tolerance: 0.0001f);
    }

    #endregion

    #region TriggerRender Tests

    [Fact]
    public void TriggerRender_IncrementsRenderCount()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerRender(0.016f);

        loop.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void TriggerRender_FiresOnRenderEvent()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0;
        loop.OnRender += dt => receivedDelta = dt;

        loop.TriggerRender(0.016f);

        receivedDelta.ShouldBe(0.016f, tolerance: 0.0001f);
    }

    #endregion

    #region TriggerResize Tests

    [Fact]
    public void TriggerResize_UpdatesDimensions()
    {
        using var loop = new MockLoopProvider();

        loop.TriggerResize(1920, 1080);

        loop.Width.ShouldBe(1920);
        loop.Height.ShouldBe(1080);
    }

    [Fact]
    public void TriggerResize_FiresOnResizeEvent()
    {
        using var loop = new MockLoopProvider();
        int receivedWidth = 0;
        int receivedHeight = 0;
        loop.OnResize += (w, h) =>
        {
            receivedWidth = w;
            receivedHeight = h;
        };

        loop.TriggerResize(1280, 720);

        receivedWidth.ShouldBe(1280);
        receivedHeight.ShouldBe(720);
    }

    #endregion

    #region TriggerClosing Tests

    [Fact]
    public void TriggerClosing_SetsIsRunningToFalse()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();

        loop.TriggerClosing();

        loop.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void TriggerClosing_FiresOnClosingEvent()
    {
        using var loop = new MockLoopProvider();
        var eventFired = false;
        loop.OnClosing += () => eventFired = true;

        loop.TriggerClosing();

        eventFired.ShouldBeTrue();
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_TriggersUpdateAndRender()
    {
        using var loop = new MockLoopProvider();

        loop.Step();

        loop.UpdateCount.ShouldBe(1);
        loop.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void Step_UsesDefaultDeltaTime()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0;
        loop.OnUpdate += dt => receivedDelta = dt;

        loop.Step();

        receivedDelta.ShouldBe(1f / 60f, tolerance: 0.0001f);
    }

    [Fact]
    public void Step_UsesProvidedDeltaTime()
    {
        using var loop = new MockLoopProvider();
        float receivedDelta = 0;
        loop.OnUpdate += dt => receivedDelta = dt;

        loop.Step(0.05f);

        receivedDelta.ShouldBe(0.05f, tolerance: 0.0001f);
    }

    #endregion

    #region StepFrames Tests

    [Fact]
    public void StepFrames_SimulatesMultipleFrames()
    {
        using var loop = new MockLoopProvider();

        loop.StepFrames(10);

        loop.UpdateCount.ShouldBe(10);
        loop.RenderCount.ShouldBe(10);
    }

    [Fact]
    public void StepFrames_AccumulatesTotalTime()
    {
        using var loop = new MockLoopProvider();
        float deltaTime = 1f / 60f;

        loop.StepFrames(60, deltaTime);

        loop.TotalTime.ShouldBe(1f, tolerance: 0.001f);
    }

    [Fact]
    public void StepFrames_ZeroCount_DoesNothing()
    {
        using var loop = new MockLoopProvider();

        loop.StepFrames(0);

        loop.UpdateCount.ShouldBe(0);
        loop.RenderCount.ShouldBe(0);
    }

    #endregion

    #region ResetCounters Tests

    [Fact]
    public void ResetCounters_ClearsAllCounters()
    {
        using var loop = new MockLoopProvider();
        loop.StepFrames(5);

        loop.ResetCounters();

        loop.UpdateCount.ShouldBe(0);
        loop.RenderCount.ShouldBe(0);
        loop.TotalTime.ShouldBe(0f);
    }

    [Fact]
    public void ResetCounters_DoesNotAffectState()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();
        loop.StepFrames(5);

        loop.ResetCounters();

        loop.IsInitialized.ShouldBeTrue();
        loop.IsRunning.ShouldBeTrue();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();
        loop.TriggerResize(1920, 1080);
        loop.StepFrames(10);

        loop.Reset();

        loop.IsInitialized.ShouldBeFalse();
        loop.IsRunning.ShouldBeFalse();
        loop.UpdateCount.ShouldBe(0);
        loop.RenderCount.ShouldBe(0);
        loop.TotalTime.ShouldBe(0f);
        loop.Width.ShouldBe(800);
        loop.Height.ShouldBe(600);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ResetsState()
    {
        var loop = new MockLoopProvider();
        loop.Initialize();
        loop.Run();
        loop.StepFrames(5);

        loop.Dispose();

        loop.IsInitialized.ShouldBeFalse();
        loop.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var loop = new MockLoopProvider();

        Should.NotThrow(() =>
        {
            loop.Dispose();
            loop.Dispose();
        });
    }

    #endregion

    #region Default Values Tests

    [Fact]
    public void NewInstance_HasDefaultDimensions()
    {
        using var loop = new MockLoopProvider();

        loop.Width.ShouldBe(800);
        loop.Height.ShouldBe(600);
    }

    [Fact]
    public void NewInstance_IsNotInitialized()
    {
        using var loop = new MockLoopProvider();

        loop.IsInitialized.ShouldBeFalse();
    }

    [Fact]
    public void NewInstance_IsNotRunning()
    {
        using var loop = new MockLoopProvider();

        loop.IsRunning.ShouldBeFalse();
    }

    [Fact]
    public void NewInstance_HasZeroCounts()
    {
        using var loop = new MockLoopProvider();

        loop.UpdateCount.ShouldBe(0);
        loop.RenderCount.ShouldBe(0);
        loop.TotalTime.ShouldBe(0f);
    }

    #endregion
}
