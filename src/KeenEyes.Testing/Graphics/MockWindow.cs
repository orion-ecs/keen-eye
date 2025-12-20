using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Testing.Graphics;

/// <summary>
/// A mock implementation of <see cref="IWindow"/> for testing window lifecycle
/// and rendering code without a real window.
/// </summary>
/// <remarks>
/// <para>
/// MockWindow provides a headless window for testing that tracks all lifecycle
/// events and operations. Use the trigger methods to simulate window events
/// in tests.
/// </para>
/// <para>
/// The <see cref="Run"/> method is non-blocking in the mock, allowing tests
/// to proceed immediately. Use <see cref="TriggerLoad"/>, <see cref="TriggerUpdate"/>,
/// and <see cref="TriggerRender"/> to simulate the window lifecycle.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var window = new MockWindow(800, 600, "Test");
/// window.OnLoad += () => InitializeScene();
/// window.OnUpdate += dt => UpdateGame(dt);
/// window.OnRender += dt => RenderFrame(dt);
///
/// window.Run();
/// window.TriggerLoad();
///
/// // Simulate 10 frames
/// for (int i = 0; i &lt; 10; i++)
/// {
///     window.TriggerUpdate(1/60.0);
///     window.TriggerRender(1/60.0);
/// }
///
/// window.UpdateCount.Should().Be(10);
/// </code>
/// </example>
/// <param name="width">The window width in pixels.</param>
/// <param name="height">The window height in pixels.</param>
/// <param name="title">The window title.</param>
public sealed class MockWindow(int width = 800, int height = 600, string title = "MockWindow") : IWindow
{
    private bool disposed;
    private bool isRunning;
    private MockGraphicsDevice? device;

    #region IWindow Properties

    /// <inheritdoc />
    public int Width { get; private set; } = width;

    /// <inheritdoc />
    public int Height { get; private set; } = height;

    /// <inheritdoc />
    public string Title { get; set; } = title;

    /// <inheritdoc />
    public bool IsClosing { get; private set; }

    /// <inheritdoc />
    public bool IsFocused { get; set; } = true;

    /// <inheritdoc />
    public float AspectRatio => Height > 0 ? (float)Width / Height : 1f;

    #endregion

    #region IWindow Events

    /// <inheritdoc />
    public event Action? OnLoad;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    /// <inheritdoc />
    public event Action<double>? OnUpdate;

    /// <inheritdoc />
    public event Action<double>? OnRender;

    #endregion

    #region Test Tracking

    /// <summary>
    /// Gets the mock graphics device created by this window, if any.
    /// </summary>
    public MockGraphicsDevice? MockDevice => device;

    /// <summary>
    /// Gets the number of times <see cref="SwapBuffers"/> has been called.
    /// </summary>
    public int SwapBufferCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="DoEvents"/> has been called.
    /// </summary>
    public int DoEventsCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="OnUpdate"/> has been triggered.
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="OnRender"/> has been triggered.
    /// </summary>
    public int RenderCount { get; private set; }

    /// <summary>
    /// Gets the number of times <see cref="OnLoad"/> has been triggered.
    /// </summary>
    public int LoadCount { get; private set; }

    /// <summary>
    /// Gets the total accumulated update time.
    /// </summary>
    public double TotalUpdateTime { get; private set; }

    /// <summary>
    /// Gets the total accumulated render time.
    /// </summary>
    public double TotalRenderTime { get; private set; }

    /// <summary>
    /// Gets whether the window has been started with <see cref="Run"/>.
    /// </summary>
    public bool IsRunning => isRunning;

    #endregion

    #region IWindow Methods

    /// <inheritdoc />
    public IGraphicsDevice CreateDevice()
    {
        device ??= new MockGraphicsDevice();
        return device;
    }

    /// <inheritdoc />
    /// <remarks>
    /// In the mock, this is non-blocking. It sets <see cref="IsRunning"/> to true
    /// and returns immediately.
    /// </remarks>
    public void Run()
    {
        if (isRunning)
        {
            throw new InvalidOperationException("Window is already running.");
        }

        isRunning = true;
    }

    /// <inheritdoc />
    public void DoEvents()
    {
        DoEventsCount++;
    }

    /// <inheritdoc />
    public void SwapBuffers()
    {
        SwapBufferCount++;
    }

    /// <inheritdoc />
    public void Close()
    {
        if (!IsClosing)
        {
            IsClosing = true;
            TriggerClosing();
            isRunning = false;
        }
    }

    #endregion

    #region Trigger Methods

    /// <summary>
    /// Triggers the <see cref="OnLoad"/> event.
    /// </summary>
    public void TriggerLoad()
    {
        LoadCount++;
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Triggers the <see cref="OnUpdate"/> event with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last update.</param>
    public void TriggerUpdate(double deltaTime)
    {
        UpdateCount++;
        TotalUpdateTime += deltaTime;
        OnUpdate?.Invoke(deltaTime);
    }

    /// <summary>
    /// Triggers the <see cref="OnRender"/> event with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last render.</param>
    public void TriggerRender(double deltaTime)
    {
        RenderCount++;
        TotalRenderTime += deltaTime;
        OnRender?.Invoke(deltaTime);
    }

    /// <summary>
    /// Triggers the <see cref="OnResize"/> event with the specified dimensions.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public void TriggerResize(int width, int height)
    {
        Width = width;
        Height = height;
        OnResize?.Invoke(width, height);
    }

    /// <summary>
    /// Triggers the <see cref="OnClosing"/> event.
    /// </summary>
    public void TriggerClosing()
    {
        OnClosing?.Invoke();
    }

    /// <summary>
    /// Simulates a single frame by triggering update and render with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds for this frame. Defaults to 1/60 (60 FPS).</param>
    public void Step(double deltaTime = 1.0 / 60.0)
    {
        TriggerUpdate(deltaTime);
        TriggerRender(deltaTime);
    }

    /// <summary>
    /// Simulates multiple frames.
    /// </summary>
    /// <param name="count">The number of frames to simulate.</param>
    /// <param name="deltaTime">The time in seconds for each frame. Defaults to 1/60 (60 FPS).</param>
    public void StepFrames(int count, double deltaTime = 1.0 / 60.0)
    {
        for (int i = 0; i < count; i++)
        {
            Step(deltaTime);
        }
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Resets all tracking counters and state.
    /// </summary>
    public void ResetCounters()
    {
        SwapBufferCount = 0;
        DoEventsCount = 0;
        UpdateCount = 0;
        RenderCount = 0;
        LoadCount = 0;
        TotalUpdateTime = 0;
        TotalRenderTime = 0;
    }

    /// <summary>
    /// Fully resets the window to its initial state.
    /// </summary>
    public void Reset()
    {
        ResetCounters();
        IsClosing = false;
        isRunning = false;
        IsFocused = true;
        device?.Reset();
    }

    /// <summary>
    /// Sets the window size without triggering a resize event.
    /// </summary>
    /// <param name="width">The new width.</param>
    /// <param name="height">The new height.</param>
    public void SetSize(int width, int height)
    {
        Width = width;
        Height = height;
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            device?.Dispose();
            device = null;
        }
    }
}
