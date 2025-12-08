namespace KeenEyes.Testing;

/// <summary>
/// Provides deterministic time control for testing ECS systems.
/// </summary>
/// <remarks>
/// <para>
/// TestClock allows precise control over time progression in tests,
/// enabling frame-by-frame stepping and deterministic behavior.
/// This is essential for testing time-dependent systems without
/// relying on real-time passage.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var clock = new TestClock(fps: 60);
///
/// // Advance one frame (1/60th of a second)
/// clock.Step();
///
/// // Advance by specific time
/// clock.StepByTime(100); // 100ms
///
/// // Use in system update
/// mySystem.Update(clock.DeltaSeconds);
/// </code>
/// </example>
public sealed class TestClock
{
    private float targetFps;
    private float frameDuration;

    /// <summary>
    /// Gets the current simulation time in milliseconds.
    /// </summary>
    public float CurrentTime { get; private set; }

    /// <summary>
    /// Gets the time elapsed since the last step in milliseconds.
    /// </summary>
    public float DeltaTime { get; private set; }

    /// <summary>
    /// Gets the time elapsed since the last step in seconds.
    /// </summary>
    public float DeltaSeconds => DeltaTime / 1000f;

    /// <summary>
    /// Gets the current target frames per second.
    /// </summary>
    public float Fps => targetFps;

    /// <summary>
    /// Gets or sets whether the clock is paused.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Gets the total number of frames that have been stepped.
    /// </summary>
    public int FrameCount { get; private set; }

    /// <summary>
    /// Creates a new test clock with the specified target FPS.
    /// </summary>
    /// <param name="fps">Target frames per second. Defaults to 60.</param>
    public TestClock(float fps = 60f)
    {
        SetFps(fps);
    }

    /// <summary>
    /// Advances the clock by the specified number of frames.
    /// </summary>
    /// <param name="frames">Number of frames to advance. Defaults to 1.</param>
    /// <returns>The total delta time for all stepped frames in seconds.</returns>
    public float Step(int frames = 1)
    {
        if (IsPaused || frames <= 0)
        {
            DeltaTime = 0;
            return 0;
        }

        DeltaTime = frameDuration * frames;
        CurrentTime += DeltaTime;
        FrameCount += frames;

        return DeltaSeconds;
    }

    /// <summary>
    /// Advances the clock by the specified time in milliseconds.
    /// </summary>
    /// <param name="deltaMs">Time to advance in milliseconds.</param>
    /// <returns>The delta time in seconds.</returns>
    public float StepByTime(float deltaMs)
    {
        if (IsPaused || deltaMs <= 0)
        {
            DeltaTime = 0;
            return 0;
        }

        DeltaTime = deltaMs;
        CurrentTime += deltaMs;
        FrameCount++;

        return DeltaSeconds;
    }

    /// <summary>
    /// Sets the absolute simulation time.
    /// </summary>
    /// <param name="timeMs">The time to set in milliseconds.</param>
    public void SetTime(float timeMs)
    {
        var previousTime = CurrentTime;
        CurrentTime = Math.Max(0, timeMs);
        DeltaTime = CurrentTime - previousTime;
    }

    /// <summary>
    /// Sets the target frames per second.
    /// </summary>
    /// <param name="fps">Target FPS. Must be greater than 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when fps is less than or equal to 0.</exception>
    public void SetFps(float fps)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(fps, 0);
        targetFps = fps;
        frameDuration = 1000f / fps;
    }

    /// <summary>
    /// Pauses the clock. Subsequent calls to Step will not advance time.
    /// </summary>
    public void Pause()
    {
        IsPaused = true;
    }

    /// <summary>
    /// Resumes the clock after being paused.
    /// </summary>
    public void Resume()
    {
        IsPaused = false;
    }

    /// <summary>
    /// Resets the clock to initial state.
    /// </summary>
    public void Reset()
    {
        CurrentTime = 0;
        DeltaTime = 0;
        FrameCount = 0;
        IsPaused = false;
    }
}
