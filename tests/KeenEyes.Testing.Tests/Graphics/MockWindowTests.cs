using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockWindowTests
{
    #region Constructor and Properties

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        using var window = new MockWindow();

        Assert.Equal(800, window.Width);
        Assert.Equal(600, window.Height);
        Assert.Equal("MockWindow", window.Title);
        Assert.False(window.IsClosing);
        Assert.True(window.IsFocused);
    }

    [Fact]
    public void Constructor_AcceptsCustomDimensions()
    {
        using var window = new MockWindow(1920, 1080, "Custom Window");

        Assert.Equal(1920, window.Width);
        Assert.Equal(1080, window.Height);
        Assert.Equal("Custom Window", window.Title);
    }

    [Fact]
    public void AspectRatio_ReturnsCorrectValue()
    {
        using var window = new MockWindow(1600, 900);

        var ratio = window.AspectRatio;

        Assert.Equal(1600f / 900f, ratio);
    }

    [Fact]
    public void AspectRatio_WhenHeightIsZero_ReturnsOne()
    {
        using var window = new MockWindow(800, 0);

        var ratio = window.AspectRatio;

        Assert.Equal(1f, ratio);
    }

    [Fact]
    public void Title_CanBeSet()
    {
        using var window = new MockWindow();

        window.Title = "New Title";

        Assert.Equal("New Title", window.Title);
    }

    [Fact]
    public void IsFocused_CanBeSet()
    {
        using var window = new MockWindow();

        window.IsFocused = false;

        Assert.False(window.IsFocused);
    }

    #endregion

    #region Run and Close

    [Fact]
    public void Run_SetsIsRunningTrue()
    {
        using var window = new MockWindow();

        window.Run();

        Assert.True(window.IsRunning);
    }

    [Fact]
    public void Run_WhenAlreadyRunning_ThrowsException()
    {
        using var window = new MockWindow();
        window.Run();

        Assert.Throws<InvalidOperationException>(() => window.Run());
    }

    [Fact]
    public void Close_SetsIsClosingTrue()
    {
        using var window = new MockWindow();
        window.Run();

        window.Close();

        Assert.True(window.IsClosing);
    }

    [Fact]
    public void Close_SetsIsRunningFalse()
    {
        using var window = new MockWindow();
        window.Run();

        window.Close();

        Assert.False(window.IsRunning);
    }

    [Fact]
    public void Close_TriggersOnClosingEvent()
    {
        using var window = new MockWindow();
        var closingFired = false;
        window.OnClosing += () => closingFired = true;
        window.Run();

        window.Close();

        Assert.True(closingFired);
    }

    [Fact]
    public void Close_WhenAlreadyClosing_DoesNothing()
    {
        using var window = new MockWindow();
        var closingCount = 0;
        window.OnClosing += () => closingCount++;
        window.Run();

        window.Close();
        window.Close(); // Second call should do nothing

        Assert.Equal(1, closingCount);
    }

    #endregion

    #region CreateDevice

    [Fact]
    public void CreateDevice_ReturnsMockGraphicsDevice()
    {
        using var window = new MockWindow();

        var device = window.CreateDevice();

        Assert.NotNull(device);
        Assert.IsType<MockGraphicsDevice>(device);
    }

    [Fact]
    public void CreateDevice_ReturnsSameInstance()
    {
        using var window = new MockWindow();

        var device1 = window.CreateDevice();
        var device2 = window.CreateDevice();

        Assert.Same(device1, device2);
    }

    [Fact]
    public void MockDevice_ReturnsCreatedDevice()
    {
        using var window = new MockWindow();
        var device = window.CreateDevice();

        Assert.Same(device, window.MockDevice);
    }

    #endregion

    #region DoEvents and SwapBuffers

    [Fact]
    public void DoEvents_IncrementsCounter()
    {
        using var window = new MockWindow();

        window.DoEvents();
        window.DoEvents();
        window.DoEvents();

        Assert.Equal(3, window.DoEventsCount);
    }

    [Fact]
    public void SwapBuffers_IncrementsCounter()
    {
        using var window = new MockWindow();

        window.SwapBuffers();
        window.SwapBuffers();

        Assert.Equal(2, window.SwapBufferCount);
    }

    #endregion

    #region Trigger Methods

    [Fact]
    public void TriggerLoad_FiresOnLoadEvent()
    {
        using var window = new MockWindow();
        var loadFired = false;
        window.OnLoad += () => loadFired = true;

        window.TriggerLoad();

        Assert.True(loadFired);
        Assert.Equal(1, window.LoadCount);
    }

    [Fact]
    public void TriggerUpdate_FiresOnUpdateEvent()
    {
        using var window = new MockWindow();
        double receivedDelta = 0;
        window.OnUpdate += dt => receivedDelta = dt;

        window.TriggerUpdate(0.016);

        Assert.Equal(0.016, receivedDelta);
        Assert.Equal(1, window.UpdateCount);
    }

    [Fact]
    public void TriggerUpdate_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.TriggerUpdate(0.1);
        window.TriggerUpdate(0.2);
        window.TriggerUpdate(0.3);

        Assert.Equal(0.6, window.TotalUpdateTime, precision: 5);
    }

    [Fact]
    public void TriggerRender_FiresOnRenderEvent()
    {
        using var window = new MockWindow();
        double receivedDelta = 0;
        window.OnRender += dt => receivedDelta = dt;

        window.TriggerRender(0.016);

        Assert.Equal(0.016, receivedDelta);
        Assert.Equal(1, window.RenderCount);
    }

    [Fact]
    public void TriggerRender_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.TriggerRender(0.05);
        window.TriggerRender(0.05);

        Assert.Equal(0.1, window.TotalRenderTime, precision: 5);
    }

    [Fact]
    public void TriggerResize_UpdatesDimensions()
    {
        using var window = new MockWindow();

        window.TriggerResize(1920, 1080);

        Assert.Equal(1920, window.Width);
        Assert.Equal(1080, window.Height);
    }

    [Fact]
    public void TriggerResize_FiresOnResizeEvent()
    {
        using var window = new MockWindow();
        int receivedWidth = 0, receivedHeight = 0;
        window.OnResize += (w, h) =>
        {
            receivedWidth = w;
            receivedHeight = h;
        };

        window.TriggerResize(1024, 768);

        Assert.Equal(1024, receivedWidth);
        Assert.Equal(768, receivedHeight);
    }

    [Fact]
    public void TriggerClosing_FiresOnClosingEvent()
    {
        using var window = new MockWindow();
        var closingFired = false;
        window.OnClosing += () => closingFired = true;

        window.TriggerClosing();

        Assert.True(closingFired);
    }

    #endregion

    #region Step Methods

    [Fact]
    public void Step_TriggersUpdateAndRender()
    {
        using var window = new MockWindow();

        window.Step(0.016);

        Assert.Equal(1, window.UpdateCount);
        Assert.Equal(1, window.RenderCount);
    }

    [Fact]
    public void Step_UsesDefaultDeltaTime()
    {
        using var window = new MockWindow();
        double totalTime = 0;
        window.OnUpdate += dt => totalTime += dt;

        window.Step(); // Default is 1/60

        Assert.Equal(1.0 / 60.0, totalTime, precision: 5);
    }

    [Fact]
    public void StepFrames_SimulatesMultipleFrames()
    {
        using var window = new MockWindow();

        window.StepFrames(10);

        Assert.Equal(10, window.UpdateCount);
        Assert.Equal(10, window.RenderCount);
    }

    [Fact]
    public void StepFrames_AccumulatesTotalTime()
    {
        using var window = new MockWindow();

        window.StepFrames(5, 0.016);

        Assert.Equal(0.08, window.TotalUpdateTime, precision: 5);
        Assert.Equal(0.08, window.TotalRenderTime, precision: 5);
    }

    #endregion

    #region Reset Methods

    [Fact]
    public void ResetCounters_ClearsAllCounters()
    {
        using var window = new MockWindow();
        window.TriggerLoad();
        window.TriggerUpdate(0.1);
        window.TriggerRender(0.1);
        window.DoEvents();
        window.SwapBuffers();

        window.ResetCounters();

        Assert.Equal(0, window.LoadCount);
        Assert.Equal(0, window.UpdateCount);
        Assert.Equal(0, window.RenderCount);
        Assert.Equal(0, window.DoEventsCount);
        Assert.Equal(0, window.SwapBufferCount);
        Assert.Equal(0, window.TotalUpdateTime);
        Assert.Equal(0, window.TotalRenderTime);
    }

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var window = new MockWindow();
        window.Run();
        window.Close();
        window.TriggerUpdate(0.1);

        window.Reset();

        Assert.False(window.IsClosing);
        Assert.False(window.IsRunning);
        Assert.True(window.IsFocused);
        Assert.Equal(0, window.UpdateCount);
    }

    [Fact]
    public void Reset_ResetsDevice()
    {
        using var window = new MockWindow();
        var device = window.CreateDevice() as MockGraphicsDevice;
        device!.Enable(KeenEyes.Graphics.Abstractions.RenderCapability.DepthTest);

        window.Reset();

        Assert.Empty(device.RenderState.EnabledCapabilities);
    }

    [Fact]
    public void SetSize_UpdatesDimensionsWithoutEvent()
    {
        using var window = new MockWindow();
        var resizeFired = false;
        window.OnResize += (_, _) => resizeFired = true;

        window.SetSize(1280, 720);

        Assert.Equal(1280, window.Width);
        Assert.Equal(720, window.Height);
        Assert.False(resizeFired);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_DisposesDevice()
    {
        var window = new MockWindow();
        _ = window.CreateDevice(); // Ensure device is created

        window.Dispose();

        Assert.Null(window.MockDevice);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var window = new MockWindow();
        window.CreateDevice();

        window.Dispose();
        window.Dispose(); // Should not throw
    }

    #endregion
}
