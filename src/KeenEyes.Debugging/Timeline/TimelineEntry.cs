namespace KeenEyes.Debugging.Timeline;

/// <summary>
/// Represents a single entry in the system execution timeline.
/// </summary>
/// <remarks>
/// Each entry captures the execution of a single system within a frame,
/// including timing information and the phase in which it executed.
/// </remarks>
public readonly record struct TimelineEntry
{
    /// <summary>
    /// Gets the frame number when this system executed.
    /// </summary>
    public required long FrameNumber { get; init; }

    /// <summary>
    /// Gets the name of the system that was executed.
    /// </summary>
    public required string SystemName { get; init; }

    /// <summary>
    /// Gets the phase in which the system executed.
    /// </summary>
    public required SystemPhase Phase { get; init; }

    /// <summary>
    /// Gets the timestamp when execution started (in ticks since the timeline started recording).
    /// </summary>
    public required long StartTicks { get; init; }

    /// <summary>
    /// Gets the duration of the system execution.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the delta time passed to the system's Update method.
    /// </summary>
    public required float DeltaTime { get; init; }
}
