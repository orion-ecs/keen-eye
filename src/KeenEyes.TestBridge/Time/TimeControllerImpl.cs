using KeenEyes.TestBridge.Time;

namespace KeenEyes.TestBridge.TimeImpl;

/// <summary>
/// In-process implementation of <see cref="ITimeController"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation manages time control state via a singleton stored in the World.
/// Games must check this singleton in their update loops to honor pause/time scale settings.
/// </para>
/// </remarks>
/// <param name="world">The world to control time for.</param>
internal sealed class TimeControllerImpl(World world) : ITimeController
{
    private long currentFrame;

    /// <inheritdoc />
    public Task<TimeStateSnapshot> GetTimeStateAsync()
    {
        var state = GetOrCreateTimeState();
        return Task.FromResult(CreateSnapshot(state));
    }

    /// <inheritdoc />
    public Task<TimeStateSnapshot> PauseAsync()
    {
        var state = GetOrCreateTimeState();
        if (!state.IsPaused)
        {
            state.IsPaused = true;
            state.LastModifiedFrame = currentFrame;
            world.SetSingleton(state);
        }
        return Task.FromResult(CreateSnapshot(state));
    }

    /// <inheritdoc />
    public Task<TimeStateSnapshot> ResumeAsync()
    {
        var state = GetOrCreateTimeState();
        if (state.IsPaused)
        {
            state.IsPaused = false;
            state.PendingStepFrames = 0; // Clear any pending steps
            state.LastModifiedFrame = currentFrame;
            world.SetSingleton(state);
        }
        return Task.FromResult(CreateSnapshot(state));
    }

    /// <inheritdoc />
    public Task<TimeStateSnapshot> SetTimeScaleAsync(float scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(scale);

        var state = GetOrCreateTimeState();
        state.TimeScale = scale;
        state.LastModifiedFrame = currentFrame;
        world.SetSingleton(state);

        return Task.FromResult(CreateSnapshot(state));
    }

    /// <inheritdoc />
    public Task<TimeStateSnapshot> StepFrameAsync(int frames = 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(frames, 1);

        var state = GetOrCreateTimeState();

        // If not paused, pause first
        if (!state.IsPaused)
        {
            state.IsPaused = true;
        }

        // Set or add to pending step frames
        state.PendingStepFrames = frames;
        state.LastModifiedFrame = currentFrame;
        world.SetSingleton(state);

        return Task.FromResult(CreateSnapshot(state));
    }

    /// <inheritdoc />
    public Task<TimeStateSnapshot> TogglePauseAsync()
    {
        var state = GetOrCreateTimeState();
        if (state.IsPaused)
        {
            return ResumeAsync();
        }
        else
        {
            return PauseAsync();
        }
    }

    /// <summary>
    /// Advances the frame counter. Called by the TestBridge plugin after each frame.
    /// </summary>
    internal void AdvanceFrame()
    {
        currentFrame++;

        // If paused with pending step frames, decrement after the step
        if (world.TryGetSingleton<TimeState>(out var state) && state.IsPaused && state.PendingStepFrames > 0)
        {
            state.PendingStepFrames--;
            state.LastModifiedFrame = currentFrame;
            world.SetSingleton(state);
        }
    }

    /// <summary>
    /// Records paused time. Called by the TestBridge plugin when paused.
    /// </summary>
    /// <param name="deltaTime">The delta time that would have passed.</param>
    internal void RecordPausedTime(double deltaTime)
    {
        if (world.TryGetSingleton<TimeState>(out var state) && state.IsPaused)
        {
            state.TotalPausedTime += deltaTime;
            world.SetSingleton(state);
        }
    }

    private TimeState GetOrCreateTimeState()
    {
        if (world.TryGetSingleton<TimeState>(out var state))
        {
            return state;
        }

        // Create default state
        var defaultState = TimeState.CreateDefault();
        world.SetSingleton(defaultState);
        return defaultState;
    }

    private TimeStateSnapshot CreateSnapshot(TimeState state)
    {
        return new TimeStateSnapshot
        {
            IsPaused = state.IsPaused,
            TimeScale = state.TimeScale,
            TotalPausedTime = state.TotalPausedTime,
            LastModifiedFrame = state.LastModifiedFrame,
            PendingStepFrames = state.PendingStepFrames
        };
    }
}
