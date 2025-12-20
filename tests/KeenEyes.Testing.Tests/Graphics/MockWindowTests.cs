using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockWindowTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_SetsDefaultDimensions()
    {
        using var window = new MockWindow();

        window.Width.ShouldBe(800);
        window.Height.ShouldBe(600);
        window.Title.ShouldBe("MockWindow");
    }

    [Fact]
    public void Constructor_WithDimensions_SetsDimensions()
    {
        using var window = new MockWindow(1920, 1080, "Test Window");

        window.Width.ShouldBe(1920);
        window.Height.ShouldBe(1080);
        window.Title.ShouldBe("Test Window");
    }

    #endregion

    #region Run Tests

    [Fact]
    public void Run_SetsIsRunning()
    {
        using var window = new MockWindow();

        window.Run();

        window.IsRunning.ShouldBeTrue();
    }

    [Fact]
    public void Run_WhenAlreadyRunning_Throws()
    {
        using var window = new MockWindow();
        window.Run();

        Should.Throw<InvalidOperationException>(() => window.Run());
    }

    #endregion

    #region TriggerLoad Tests

    [Fact]
    public void TriggerLoad_FiresOnLoadEvent()
    {
        using var window = new MockWindow();
        var eventFired = false;
        window.OnLoad += () => eventFired = true;

        window.TriggerLoad();

        eventFired.ShouldBeTrue();
    }

    [Fact]
    public void TriggerLoad_IncrementsLoadCount()
    {
        using var window = new MockWindow();

        window.TriggerLoad();
        window.TriggerLoad();

        window.LoadCount.ShouldBe(2);
    }

    #endregion

    #region TriggerUpdate Tests

    [Fact]
    public void TriggerUpdate_FiresOnUpdateEvent()
    {
        using var window = new MockWindow();
        double receivedDelta = 0;
        window.OnUpdate += dt => receivedDelta = dt;

        window.TriggerUpdate(0.016);

        receivedDelta.ShouldBe(0.016, tolerance: 0.0001);
    }

    [Fact]
    public void TriggerUpdate_IncrementsUpdateCount()
    {
        using var window = new MockWindow();

        window.TriggerUpdate(0.016);

        window.UpdateCount.ShouldBe(1);
    }

    [Fact]
    public void TriggerUpdate_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.TriggerUpdate(0.1);
        window.TriggerUpdate(0.2);

        window.TotalUpdateTime.ShouldBe(0.3, tolerance: 0.0001);
    }

    #endregion

    #region TriggerRender Tests

    [Fact]
    public void TriggerRender_FiresOnRenderEvent()
    {
        using var window = new MockWindow();
        double receivedDelta = 0;
        window.OnRender += dt => receivedDelta = dt;

        window.TriggerRender(0.016);

        receivedDelta.ShouldBe(0.016, tolerance: 0.0001);
    }

    [Fact]
    public void TriggerRender_IncrementsRenderCount()
    {
        using var window = new MockWindow();

        window.TriggerRender(0.016);

        window.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void TriggerRender_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.TriggerRender(0.1);
        window.TriggerRender(0.2);

        window.TotalRenderTime.ShouldBe(0.3, tolerance: 0.0001);
    }

    #endregion

    #region TriggerResize Tests

    [Fact]
    public void TriggerResize_UpdatesDimensions()
    {
        using var window = new MockWindow();

        window.TriggerResize(1920, 1080);

        window.Width.ShouldBe(1920);
        window.Height.ShouldBe(1080);
    }

    [Fact]
    public void TriggerResize_FiresOnResizeEvent()
    {
        using var window = new MockWindow();
        int receivedWidth = 0;
        int receivedHeight = 0;
        window.OnResize += (w, h) =>
        {
            receivedWidth = w;
            receivedHeight = h;
        };

        window.TriggerResize(1280, 720);

        receivedWidth.ShouldBe(1280);
        receivedHeight.ShouldBe(720);
    }

    #endregion

    #region Close Tests

    [Fact]
    public void Close_SetsIsClosing()
    {
        using var window = new MockWindow();

        window.Close();

        window.IsClosing.ShouldBeTrue();
    }

    [Fact]
    public void Close_FiresOnClosingEvent()
    {
        using var window = new MockWindow();
        var eventFired = false;
        window.OnClosing += () => eventFired = true;

        window.Close();

        eventFired.ShouldBeTrue();
    }

    [Fact]
    public void Close_SetsIsRunningToFalse()
    {
        using var window = new MockWindow();
        window.Run();

        window.Close();

        window.IsRunning.ShouldBeFalse();
    }

    #endregion

    #region Step Tests

    [Fact]
    public void Step_TriggersUpdateAndRender()
    {
        using var window = new MockWindow();

        window.Step();

        window.UpdateCount.ShouldBe(1);
        window.RenderCount.ShouldBe(1);
    }

    [Fact]
    public void Step_UsesDefaultDeltaTime()
    {
        using var window = new MockWindow();

        window.Step();

        window.TotalUpdateTime.ShouldBe(1.0 / 60.0, tolerance: 0.0001);
    }

    [Fact]
    public void Step_UsesProvidedDeltaTime()
    {
        using var window = new MockWindow();

        window.Step(0.05);

        window.TotalUpdateTime.ShouldBe(0.05, tolerance: 0.0001);
    }

    #endregion

    #region StepFrames Tests

    [Fact]
    public void StepFrames_SimulatesMultipleFrames()
    {
        using var window = new MockWindow();

        window.StepFrames(10);

        window.UpdateCount.ShouldBe(10);
        window.RenderCount.ShouldBe(10);
    }

    [Fact]
    public void StepFrames_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.StepFrames(60, 1.0 / 60.0);

        window.TotalUpdateTime.ShouldBe(1.0, tolerance: 0.001);
    }

    #endregion

    #region CreateDevice Tests

    [Fact]
    public void CreateDevice_ReturnsMockGraphicsDevice()
    {
        using var window = new MockWindow();

        var device = window.CreateDevice();

        device.ShouldBeOfType<MockGraphicsDevice>();
    }

    [Fact]
    public void CreateDevice_ReturnsSameInstanceOnMultipleCalls()
    {
        using var window = new MockWindow();

        var device1 = window.CreateDevice();
        var device2 = window.CreateDevice();

        device1.ShouldBeSameAs(device2);
    }

    [Fact]
    public void MockDevice_ReturnsCreatedDevice()
    {
        using var window = new MockWindow();
        window.CreateDevice();

        window.MockDevice.ShouldNotBeNull();
    }

    #endregion

    #region DoEvents and SwapBuffers Tests

    [Fact]
    public void DoEvents_IncrementsCount()
    {
        using var window = new MockWindow();

        window.DoEvents();
        window.DoEvents();

        window.DoEventsCount.ShouldBe(2);
    }

    [Fact]
    public void SwapBuffers_IncrementsCount()
    {
        using var window = new MockWindow();

        window.SwapBuffers();
        window.SwapBuffers();

        window.SwapBufferCount.ShouldBe(2);
    }

    #endregion

    #region AspectRatio Tests

    [Fact]
    public void AspectRatio_ReturnsCorrectValue()
    {
        using var window = new MockWindow(1920, 1080);

        window.AspectRatio.ShouldBe(1920f / 1080f, tolerance: 0.0001f);
    }

    [Fact]
    public void AspectRatio_ZeroHeight_ReturnsOne()
    {
        using var window = new MockWindow(800, 600);
        window.SetSize(800, 0);

        window.AspectRatio.ShouldBe(1f);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void ResetCounters_ClearsAllCounters()
    {
        using var window = new MockWindow();
        window.StepFrames(5);
        window.DoEvents();
        window.SwapBuffers();
        window.TriggerLoad();

        window.ResetCounters();

        window.UpdateCount.ShouldBe(0);
        window.RenderCount.ShouldBe(0);
        window.DoEventsCount.ShouldBe(0);
        window.SwapBufferCount.ShouldBe(0);
        window.LoadCount.ShouldBe(0);
        window.TotalUpdateTime.ShouldBe(0);
        window.TotalRenderTime.ShouldBe(0);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var window = new MockWindow();
        window.Run();
        window.StepFrames(5);

        window.Reset();

        window.IsRunning.ShouldBeFalse();
        window.IsClosing.ShouldBeFalse();
        window.UpdateCount.ShouldBe(0);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var window = new MockWindow();

        Should.NotThrow(() =>
        {
            window.Dispose();
            window.Dispose();
        });
    }

    [Fact]
    public void Dispose_DisposesDevice()
    {
        var window = new MockWindow();
        _ = window.CreateDevice();

        window.Dispose();

        window.MockDevice.ShouldBeNull();
    }

    #endregion
}
