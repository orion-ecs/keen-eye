namespace KeenEyes.Replay;

/// <summary>
/// Contains all data for a complete replay recording.
/// </summary>
/// <remarks>
/// <para>
/// ReplayData is the root container for a replay recording, containing all
/// frames, snapshots, and metadata. It can be serialized to disk for
/// persistence and loaded later for playback.
/// </para>
/// <para>
/// The recording includes:
/// <list type="bullet">
/// <item><description>Sequential frames with all events</description></item>
/// <item><description>Periodic snapshots for efficient seeking</description></item>
/// <item><description>Metadata about the recording (name, timestamp, etc.)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create replay data from a recording session
/// var replayData = recorder.StopRecording();
///
/// // Access recording metadata
/// Console.WriteLine($"Recording: {replayData.Name}");
/// Console.WriteLine($"Duration: {replayData.Duration}");
/// Console.WriteLine($"Frames: {replayData.Frames.Count}");
/// Console.WriteLine($"Snapshots: {replayData.Snapshots.Count}");
/// </code>
/// </example>
public sealed record ReplayData
{
    /// <summary>
    /// The current version of the replay data format.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Gets or sets the format version of this replay data.
    /// </summary>
    /// <remarks>
    /// Used for backwards compatibility when the format changes.
    /// </remarks>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>
    /// Gets or sets the optional name of this recording.
    /// </summary>
    /// <remarks>
    /// Applications can use this to identify recordings, for example
    /// "Crash Replay" or "Level 1 Playthrough".
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the optional description of this recording.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when recording started.
    /// </summary>
    /// <remarks>
    /// Stored as UTC time for consistency across time zones.
    /// </remarks>
    public required DateTimeOffset RecordingStarted { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when recording ended.
    /// </summary>
    public DateTimeOffset? RecordingEnded { get; init; }

    /// <summary>
    /// Gets or sets the total duration of the recording.
    /// </summary>
    /// <remarks>
    /// This is the sum of all frame delta times, representing the
    /// in-game time covered by the recording.
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the total number of frames in the recording.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets or sets all recorded frames.
    /// </summary>
    /// <remarks>
    /// Frames are stored in chronological order by frame number.
    /// </remarks>
    public required IReadOnlyList<ReplayFrame> Frames { get; init; }

    /// <summary>
    /// Gets or sets all snapshots captured during recording.
    /// </summary>
    /// <remarks>
    /// Snapshots are stored in chronological order by frame number.
    /// The frequency of snapshots is controlled by <see cref="ReplayOptions.SnapshotInterval"/>.
    /// </remarks>
    public required IReadOnlyList<SnapshotMarker> Snapshots { get; init; }

    /// <summary>
    /// Gets or sets optional metadata associated with this recording.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applications can store custom metadata such as:
    /// <list type="bullet">
    /// <item><description>Game version</description></item>
    /// <item><description>Level name</description></item>
    /// <item><description>Player information</description></item>
    /// <item><description>Random seed for deterministic replay</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Metadata values must be JSON-serializable types.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets the average frame rate of the recording.
    /// </summary>
    /// <remarks>
    /// Returns the average frames per second based on the total duration
    /// and frame count. Returns 0 if duration is zero.
    /// </remarks>
    public double AverageFrameRate =>
        Duration.TotalSeconds > 0 ? FrameCount / Duration.TotalSeconds : 0;
}
