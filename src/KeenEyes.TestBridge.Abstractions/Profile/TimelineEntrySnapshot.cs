namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of a single entry in the system execution timeline.
/// </summary>
/// <remarks>
/// Each entry captures the execution of a single system within a frame,
/// including timing information and the phase in which it executed.
/// </remarks>
public sealed record TimelineEntrySnapshot
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
    /// Gets the name of the phase in which the system executed.
    /// </summary>
    public required string Phase { get; init; }

    /// <summary>
    /// Gets the start offset in milliseconds since timeline recording started.
    /// </summary>
    public required double StartOffsetMs { get; init; }

    /// <summary>
    /// Gets the duration of the system execution in milliseconds.
    /// </summary>
    public required double DurationMs { get; init; }

    /// <summary>
    /// Gets the delta time passed to the system's Update method.
    /// </summary>
    public required float DeltaTime { get; init; }
}
