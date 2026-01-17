namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Snapshot of a single replay frame.
/// </summary>
public sealed record ReplayFrameSnapshot
{
    /// <summary>
    /// Gets the frame number (0-based).
    /// </summary>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets the delta time for this frame in seconds.
    /// </summary>
    public required float DeltaTimeSeconds { get; init; }

    /// <summary>
    /// Gets the elapsed time from the start of the replay in seconds.
    /// </summary>
    public required float ElapsedTimeSeconds { get; init; }

    /// <summary>
    /// Gets the number of input events in this frame.
    /// </summary>
    public required int InputEventCount { get; init; }

    /// <summary>
    /// Gets the number of replay events in this frame.
    /// </summary>
    public required int EventCount { get; init; }

    /// <summary>
    /// Gets whether this frame has a preceding snapshot.
    /// </summary>
    public required bool HasSnapshot { get; init; }

    /// <summary>
    /// Gets the checksum for this frame, if recorded.
    /// </summary>
    public uint? Checksum { get; init; }
}
