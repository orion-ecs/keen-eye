namespace KeenEyes.Replay;

/// <summary>
/// Represents a single frame of recorded replay data.
/// </summary>
/// <remarks>
/// <para>
/// A replay frame captures all events that occurred during a single game
/// update cycle. Frames are numbered sequentially and have associated
/// timing information for accurate playback.
/// </para>
/// <para>
/// During playback, frames can be stepped through individually or played
/// back at variable speed. The <see cref="DeltaTime"/> property preserves
/// the original timing for accurate reproduction.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Iterate through frames during playback
/// foreach (var frame in replayData.Frames)
/// {
///     Console.WriteLine($"Frame {frame.FrameNumber}: {frame.Events.Count} events, dt={frame.DeltaTime.TotalMilliseconds}ms");
/// }
/// </code>
/// </example>
public sealed record ReplayFrame
{
    /// <summary>
    /// Gets or sets the sequential frame number.
    /// </summary>
    /// <remarks>
    /// Frame numbers start at 0 and increment by 1 for each frame.
    /// This provides a stable reference for seeking and bookmarking.
    /// </remarks>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets or sets the delta time for this frame.
    /// </summary>
    /// <remarks>
    /// This is the time elapsed since the previous frame, as passed to
    /// the world's Update method. Preserving the original delta time
    /// enables accurate reproduction of time-dependent behavior.
    /// </remarks>
    public required TimeSpan DeltaTime { get; init; }

    /// <summary>
    /// Gets or sets the elapsed time since the start of recording.
    /// </summary>
    /// <remarks>
    /// This is the cumulative time from the beginning of the recording
    /// to the start of this frame. It provides an absolute time reference
    /// for seeking to specific points in the replay.
    /// </remarks>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets or sets the events that occurred during this frame.
    /// </summary>
    /// <remarks>
    /// Events are stored in chronological order based on their
    /// <see cref="ReplayEvent.Timestamp"/> values.
    /// </remarks>
    public required IReadOnlyList<ReplayEvent> Events { get; init; }

    /// <summary>
    /// Gets or sets the index of the snapshot that precedes this frame, if any.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If this frame is immediately after a snapshot, this property contains
    /// the index into <see cref="ReplayData.Snapshots"/>. This enables efficient
    /// seeking by restoring the nearest snapshot and replaying from there.
    /// </para>
    /// <para>
    /// A value of null indicates no snapshot immediately precedes this frame.
    /// </para>
    /// </remarks>
    public int? PrecedingSnapshotIndex { get; init; }

    /// <summary>
    /// Gets or sets the world state checksum at the end of this frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The checksum captures the complete world state (entities, components,
    /// singletons) at the end of this frame. It is used during playback to
    /// detect desynchronization.
    /// </para>
    /// <para>
    /// A value of null indicates the checksum was not recorded. This can occur
    /// when recording without checksum generation enabled or when loading older
    /// replay files that predate checksum support.
    /// </para>
    /// </remarks>
    /// <seealso cref="WorldChecksum"/>
    /// <seealso cref="ReplayDesyncException"/>
    public uint? Checksum { get; init; }
}
