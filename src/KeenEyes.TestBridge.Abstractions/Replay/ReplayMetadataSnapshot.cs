namespace KeenEyes.TestBridge.Replay;

/// <summary>
/// Metadata from a replay file.
/// </summary>
public sealed record ReplayMetadataSnapshot
{
    /// <summary>
    /// Gets the name of the recording.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the description of the recording.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the timestamp when recording started (UTC).
    /// </summary>
    public required DateTimeOffset RecordingStarted { get; init; }

    /// <summary>
    /// Gets the timestamp when recording ended (UTC).
    /// </summary>
    public DateTimeOffset? RecordingEnded { get; init; }

    /// <summary>
    /// Gets the total duration in seconds.
    /// </summary>
    public required float DurationSeconds { get; init; }

    /// <summary>
    /// Gets the total number of frames.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets the number of snapshots in the recording.
    /// </summary>
    public required int SnapshotCount { get; init; }

    /// <summary>
    /// Gets the average frame rate.
    /// </summary>
    public required float AverageFrameRate { get; init; }

    /// <summary>
    /// Gets the replay data version.
    /// </summary>
    public required int DataVersion { get; init; }

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long? FileSizeBytes { get; init; }

    /// <summary>
    /// Gets whether the file has a checksum.
    /// </summary>
    public bool? HasChecksum { get; init; }
}
