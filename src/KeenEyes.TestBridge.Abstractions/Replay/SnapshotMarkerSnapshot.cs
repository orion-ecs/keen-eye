namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Information about a snapshot marker in a replay.
/// </summary>
public sealed record SnapshotMarkerSnapshot
{
    /// <summary>
    /// Gets the frame number where the snapshot was taken.
    /// </summary>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets the elapsed time from the start in seconds.
    /// </summary>
    public required float ElapsedTimeSeconds { get; init; }

    /// <summary>
    /// Gets the checksum of the snapshot, if recorded.
    /// </summary>
    public uint? Checksum { get; init; }

    /// <summary>
    /// Gets the index of this snapshot in the replay.
    /// </summary>
    public required int Index { get; init; }
}
