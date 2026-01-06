namespace KeenEyes.Replay;

/// <summary>
/// Configuration options for the replay recording plugin.
/// </summary>
/// <remarks>
/// <para>
/// These options control how replays are recorded, including snapshot frequency,
/// event filtering, and which phases of system execution to track.
/// </para>
/// <para>
/// Options are immutable after creation. Use object initializer syntax or the
/// <c>with</c> expression to create modified copies.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create options with custom snapshot interval
/// var options = new ReplayOptions
/// {
///     SnapshotInterval = TimeSpan.FromSeconds(5),
///     RecordSystemEvents = true,
///     RecordComponentEvents = false // Reduce recording size
/// };
///
/// world.InstallPlugin(new ReplayPlugin(options));
/// </code>
/// </example>
public sealed record ReplayOptions
{
    /// <summary>
    /// Gets or sets the interval between automatic snapshots.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Snapshots enable efficient seeking during playback. More frequent
    /// snapshots increase file size but allow faster seeking. Less frequent
    /// snapshots reduce file size but require more replay time when seeking.
    /// </para>
    /// <para>
    /// Default is 1 second. Set to <see cref="TimeSpan.Zero"/> to disable
    /// automatic snapshots (not recommended for long recordings).
    /// </para>
    /// </remarks>
    public TimeSpan SnapshotInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets whether to record system execution events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, <see cref="ReplayEventType.SystemStart"/> and
    /// <see cref="ReplayEventType.SystemEnd"/> events are recorded for
    /// each system execution. This is useful for debugging system timing
    /// and execution order.
    /// </para>
    /// <para>
    /// Default is true. Disable to reduce recording size when system-level
    /// detail is not needed.
    /// </para>
    /// </remarks>
    public bool RecordSystemEvents { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to record entity lifecycle events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, <see cref="ReplayEventType.EntityCreated"/> and
    /// <see cref="ReplayEventType.EntityDestroyed"/> events are recorded.
    /// </para>
    /// <para>
    /// Default is true.
    /// </para>
    /// </remarks>
    public bool RecordEntityEvents { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to record component events.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, <see cref="ReplayEventType.ComponentAdded"/>,
    /// <see cref="ReplayEventType.ComponentRemoved"/>, and
    /// <see cref="ReplayEventType.ComponentChanged"/> events are recorded.
    /// </para>
    /// <para>
    /// Default is true. This can significantly increase recording size
    /// in component-heavy applications. Consider disabling for production
    /// crash replays where only input events are needed.
    /// </para>
    /// </remarks>
    public bool RecordComponentEvents { get; init; } = true;

    /// <summary>
    /// Gets or sets the phase filter for system event recording.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If specified, system events will only be recorded for systems in
    /// the specified phase. If null, system events for all phases are recorded.
    /// </para>
    /// <para>
    /// Default is null (record all phases).
    /// </para>
    /// </remarks>
    public SystemPhase? SystemEventPhase { get; init; }

    /// <summary>
    /// Gets or sets the maximum number of frames to record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, recording automatically stops after this many frames.
    /// Useful for implementing fixed-length recording buffers.
    /// </para>
    /// <para>
    /// Default is null (no limit).
    /// </para>
    /// </remarks>
    public int? MaxFrames { get; init; }

    /// <summary>
    /// Gets or sets the maximum duration to record.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, recording automatically stops after this duration.
    /// The actual duration may slightly exceed this value as it's checked
    /// at frame boundaries.
    /// </para>
    /// <para>
    /// Default is null (no limit).
    /// </para>
    /// </remarks>
    public TimeSpan? MaxDuration { get; init; }

    /// <summary>
    /// Gets or sets whether to use a ring buffer for frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled with <see cref="MaxFrames"/> set, older frames are
    /// discarded as new frames are recorded, creating a rolling window
    /// of the most recent gameplay. This is ideal for crash replay scenarios
    /// where only the last N seconds before a crash are needed.
    /// </para>
    /// <para>
    /// Default is false.
    /// </para>
    /// </remarks>
    public bool UseRingBuffer { get; init; }

    /// <summary>
    /// Gets or sets the name to assign to new recordings.
    /// </summary>
    /// <remarks>
    /// Default is null. Can be overridden per-recording using
    /// <see cref="ReplayRecorder.StartRecording(string?, IReadOnlyDictionary{string, object}?)"/>.
    /// </remarks>
    public string? DefaultRecordingName { get; init; }

    /// <summary>
    /// Gets or sets whether to record frame checksums for determinism validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, a checksum of the world state is calculated at the end of
    /// each frame and stored in the replay data. During playback, these checksums
    /// can be used to detect desynchronization between the recorded and replayed states.
    /// </para>
    /// <para>
    /// Checksum calculation has a performance cost (approximately 1ms per frame for
    /// typical world sizes). Enable this when determinism validation is required,
    /// such as during development testing or for competitive replay verification.
    /// </para>
    /// <para>
    /// Default is false.
    /// </para>
    /// </remarks>
    /// <seealso cref="WorldChecksum"/>
    /// <seealso cref="ReplayPlayer.AutoValidate"/>
    /// <seealso cref="ReplayPlayer.ValidateCurrentFrame"/>
    public bool RecordChecksums { get; init; }
}
