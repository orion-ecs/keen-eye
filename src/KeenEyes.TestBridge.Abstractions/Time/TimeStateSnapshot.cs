namespace KeenEyes.TestBridge.Time;

/// <summary>
/// IPC-transportable snapshot of the current time control state.
/// </summary>
/// <remarks>
/// <para>
/// This record represents a point-in-time snapshot of the game's time
/// control state. It is used for communication between the TestBridge
/// IPC server and MCP tools.
/// </para>
/// <para>
/// Changes to time state are made through <see cref="ITimeController"/>
/// methods, which return updated snapshots reflecting the new state.
/// </para>
/// </remarks>
public sealed record TimeStateSnapshot
{
    /// <summary>
    /// Gets whether the game is currently paused.
    /// </summary>
    public required bool IsPaused { get; init; }

    /// <summary>
    /// Gets the current time scale multiplier.
    /// </summary>
    /// <remarks>
    /// A value of 1.0 is normal speed. Less than 1.0 is slow motion,
    /// greater than 1.0 is fast forward.
    /// </remarks>
    public required float TimeScale { get; init; }

    /// <summary>
    /// Gets the total time spent in paused state, in seconds.
    /// </summary>
    public required double TotalPausedTime { get; init; }

    /// <summary>
    /// Gets the frame number at which time control was last modified.
    /// </summary>
    public required long LastModifiedFrame { get; init; }

    /// <summary>
    /// Gets the number of pending step frames remaining.
    /// </summary>
    /// <remarks>
    /// When greater than zero, the game should execute this many frames
    /// even while paused (for frame-by-frame debugging).
    /// </remarks>
    public required int PendingStepFrames { get; init; }
}
