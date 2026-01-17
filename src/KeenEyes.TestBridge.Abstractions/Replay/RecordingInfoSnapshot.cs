namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Information about the current recording session.
/// </summary>
public sealed record RecordingInfoSnapshot
{
    /// <summary>
    /// Gets whether recording is currently active.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets the name of the recording.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the current frame count.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets the elapsed recording duration in seconds.
    /// </summary>
    public required float DurationSeconds { get; init; }

    /// <summary>
    /// Gets the number of snapshots captured.
    /// </summary>
    public required int SnapshotCount { get; init; }

    /// <summary>
    /// Gets the timestamp when recording started (UTC).
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }
}
