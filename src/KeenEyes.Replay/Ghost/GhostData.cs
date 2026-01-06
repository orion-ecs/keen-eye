namespace KeenEyes.Replay.Ghost;

/// <summary>
/// Contains all data for a ghost recording.
/// </summary>
/// <remarks>
/// <para>
/// GhostData is a lightweight container for transform data extracted from a
/// full replay recording. It contains only the position and rotation information
/// needed to display a ghost entity, making it orders of magnitude smaller than
/// a full replay (KB vs MB).
/// </para>
/// <para>
/// Ghost data can be:
/// <list type="bullet">
/// <item><description>Extracted from a <see cref="ReplayData"/> using <see cref="GhostExtractor"/>.</description></item>
/// <item><description>Saved to a .keghost file using <see cref="GhostFileFormat"/>.</description></item>
/// <item><description>Loaded from a .keghost file for playback.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Extract a ghost from a replay
/// var extractor = new GhostExtractor();
/// var ghostData = extractor.ExtractGhost(replayData, "Player");
///
/// // Save the ghost to a file
/// GhostFileFormat.WriteToFile("personal_best.keghost", ghostData);
///
/// // Later, load and play the ghost
/// var (_, loadedGhost) = GhostFileFormat.ReadFromFile("personal_best.keghost");
/// var player = new GhostPlayer();
/// player.Load(loadedGhost);
/// player.Play();
/// </code>
/// </example>
public sealed record GhostData
{
    /// <summary>
    /// The current version of the ghost data format.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Gets or sets the format version of this ghost data.
    /// </summary>
    /// <remarks>
    /// Used for backwards compatibility when the format changes.
    /// </remarks>
    public int Version { get; init; } = CurrentVersion;

    /// <summary>
    /// Gets or sets the optional name of this ghost recording.
    /// </summary>
    /// <remarks>
    /// Applications can use this to identify ghosts, for example
    /// "Personal Best" or "World Record".
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the name of the entity this ghost was extracted from.
    /// </summary>
    /// <remarks>
    /// This is the entity name that was used when extracting the ghost
    /// from a replay. It can be used to identify which entity the ghost
    /// represents (e.g., "Player", "Car1").
    /// </remarks>
    public string? EntityName { get; init; }

    /// <summary>
    /// Gets or sets the timestamp when the original recording started.
    /// </summary>
    /// <remarks>
    /// This preserves the original recording time from the source replay,
    /// allowing ghosts to be identified by when they were recorded.
    /// </remarks>
    public required DateTimeOffset RecordingStarted { get; init; }

    /// <summary>
    /// Gets or sets the total duration of the ghost recording.
    /// </summary>
    /// <remarks>
    /// This is the time from the first frame to the last frame.
    /// </remarks>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets or sets the total number of frames in the ghost recording.
    /// </summary>
    public required int FrameCount { get; init; }

    /// <summary>
    /// Gets or sets all ghost frames.
    /// </summary>
    /// <remarks>
    /// Frames are stored in chronological order. Each frame contains
    /// the position, rotation, and timing information for the ghost
    /// at that point in time.
    /// </remarks>
    public required IReadOnlyList<GhostFrame> Frames { get; init; }

    /// <summary>
    /// Gets or sets optional metadata associated with this ghost.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applications can store custom metadata such as:
    /// <list type="bullet">
    /// <item><description>Track name</description></item>
    /// <item><description>Completion time</description></item>
    /// <item><description>Player name</description></item>
    /// <item><description>Difficulty level</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Metadata values must be JSON-serializable types.
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets the total distance traveled by the ghost.
    /// </summary>
    /// <remarks>
    /// This is the distance value from the last frame, representing
    /// the total path length. Returns 0 if no frames exist or if
    /// distance tracking was not enabled during extraction.
    /// </remarks>
    public float TotalDistance =>
        Frames.Count > 0 ? Frames[^1].Distance : 0f;

    /// <summary>
    /// Gets the average frame rate of the ghost recording.
    /// </summary>
    /// <remarks>
    /// Returns the average frames per second based on the total duration
    /// and frame count. Returns 0 if duration is zero.
    /// </remarks>
    public double AverageFrameRate =>
        Duration.TotalSeconds > 0 ? FrameCount / Duration.TotalSeconds : 0;
}
