namespace KeenEyes.Testing.Platform;

/// <summary>
/// A mock implementation of <see cref="ILoopProvider"/> for testing game loops
/// without blocking on a real window or main loop.
/// </summary>
/// <remarks>
/// <para>
/// MockLoopProvider enables step-through testing of game systems that depend on the
/// main loop lifecycle. Instead of blocking on <see cref="Run"/>, the mock provides
/// manual trigger methods to fire lifecycle events at controlled times.
/// </para>
/// <para>
/// Use <see cref="Step"/> or <see cref="StepFrames"/> for convenient frame simulation,
/// or use individual trigger methods for fine-grained control.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var loop = new MockLoopProvider();
/// loop.Initialize();
///
/// // Simulate 10 frames at 60 FPS
/// loop.StepFrames(10, 1/60f);
///
/// loop.UpdateCount.Should().Be(10);
/// loop.RenderCount.Should().Be(10);
/// </code>
/// </example>
public sealed class MockLoopProvider : ILoopProvider, IDisposable
{
    private bool isInitialized;
    private bool isRunning;
    private int updateCount;
    private int renderCount;
    private float totalTime;
    private bool disposed;

    /// <inheritdoc />
    public event Action? OnReady;

    /// <inheritdoc />
    public event Action<float>? OnUpdate;

    /// <inheritdoc />
    public event Action<float>? OnRender;

    /// <inheritdoc />
    public event Action<int, int>? OnResize;

    /// <inheritdoc />
    public event Action? OnClosing;

    /// <inheritdoc />
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// Gets whether the loop is currently running.
    /// </summary>
    /// <remarks>
    /// In the mock, <see cref="Run"/> sets this to true but returns immediately
    /// (non-blocking). Use <see cref="TriggerClosing"/> to set this back to false.
    /// </remarks>
    public bool IsRunning => isRunning;

    /// <summary>
    /// Gets the number of times <see cref="OnUpdate"/> has been triggered.
    /// </summary>
    public int UpdateCount => updateCount;

    /// <summary>
    /// Gets the number of times <see cref="OnRender"/> has been triggered.
    /// </summary>
    public int RenderCount => renderCount;

    /// <summary>
    /// Gets the total accumulated time from all update calls.
    /// </summary>
    public float TotalTime => totalTime;

    /// <summary>
    /// Gets or sets the current window/viewport width.
    /// </summary>
    /// <remarks>
    /// Set this before calling <see cref="TriggerResize"/> or use the overload
    /// that takes width and height parameters.
    /// </remarks>
    public int Width { get; set; } = 800;

    /// <summary>
    /// Gets or sets the current window/viewport height.
    /// </summary>
    public int Height { get; set; } = 600;

    /// <inheritdoc />
    /// <remarks>
    /// In the mock, this sets <see cref="IsInitialized"/> to true.
    /// Call <see cref="TriggerReady"/> separately to fire the <see cref="OnReady"/> event.
    /// </remarks>
    public void Initialize()
    {
        if (isInitialized)
        {
            throw new InvalidOperationException("Already initialized.");
        }

        isInitialized = true;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// In the mock, this is non-blocking. It sets <see cref="IsRunning"/> to true
    /// and returns immediately. Use <see cref="Step"/> or trigger methods to
    /// simulate frame updates.
    /// </para>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if not initialized.</exception>
    public void Run()
    {
        if (!isInitialized)
        {
            throw new InvalidOperationException("Must call Initialize() before Run().");
        }

        if (isRunning)
        {
            throw new InvalidOperationException("Already running.");
        }

        isRunning = true;
        // Non-blocking: returns immediately for testing
    }

    /// <summary>
    /// Triggers the <see cref="OnReady"/> event.
    /// </summary>
    /// <remarks>
    /// Call this after <see cref="Initialize"/> to simulate the window being ready.
    /// </remarks>
    public void TriggerReady()
    {
        OnReady?.Invoke();
    }

    /// <summary>
    /// Triggers the <see cref="OnUpdate"/> event with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last update.</param>
    public void TriggerUpdate(float deltaTime)
    {
        updateCount++;
        totalTime += deltaTime;
        OnUpdate?.Invoke(deltaTime);
    }

    /// <summary>
    /// Triggers the <see cref="OnRender"/> event with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds since the last render.</param>
    public void TriggerRender(float deltaTime)
    {
        renderCount++;
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
    /// Triggers the <see cref="OnClosing"/> event and sets <see cref="IsRunning"/> to false.
    /// </summary>
    public void TriggerClosing()
    {
        isRunning = false;
        OnClosing?.Invoke();
    }

    /// <summary>
    /// Simulates a single frame by triggering update and render with the specified delta time.
    /// </summary>
    /// <param name="deltaTime">The time in seconds for this frame. Defaults to 1/60 (60 FPS).</param>
    public void Step(float deltaTime = 1f / 60f)
    {
        TriggerUpdate(deltaTime);
        TriggerRender(deltaTime);
    }

    /// <summary>
    /// Simulates multiple frames by calling <see cref="Step"/> the specified number of times.
    /// </summary>
    /// <param name="count">The number of frames to simulate.</param>
    /// <param name="deltaTime">The time in seconds for each frame. Defaults to 1/60 (60 FPS).</param>
    public void StepFrames(int count, float deltaTime = 1f / 60f)
    {
        for (int i = 0; i < count; i++)
        {
            Step(deltaTime);
        }
    }

    /// <summary>
    /// Resets all counters and state tracking.
    /// </summary>
    /// <remarks>
    /// Does not affect event subscriptions or <see cref="IsInitialized"/>/<see cref="IsRunning"/> state.
    /// </remarks>
    public void ResetCounters()
    {
        updateCount = 0;
        renderCount = 0;
        totalTime = 0f;
    }

    /// <summary>
    /// Fully resets the mock to its initial state.
    /// </summary>
    /// <remarks>
    /// Clears all counters, sets <see cref="IsInitialized"/> and <see cref="IsRunning"/>
    /// to false, and resets dimensions to defaults. Does not clear event subscriptions.
    /// </remarks>
    public void Reset()
    {
        ResetCounters();
        isInitialized = false;
        isRunning = false;
        Width = 800;
        Height = 600;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Reset();
        }
    }
}
