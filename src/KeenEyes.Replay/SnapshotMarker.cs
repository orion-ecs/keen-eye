using KeenEyes.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Marks the position of a world snapshot within a replay recording.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots are periodic captures of the complete world state during recording.
/// They enable efficient seeking by providing restoration points throughout
/// the replay timeline.
/// </para>
/// <para>
/// The snapshot marker stores metadata about when the snapshot was taken
/// (frame number and elapsed time) along with the actual snapshot data.
/// During playback, the system can restore any snapshot and replay forward
/// from that point.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Find the nearest snapshot before a target frame
/// var targetFrame = 500;
/// var snapshot = replayData.Snapshots
///     .Where(s => s.FrameNumber &lt;= targetFrame)
///     .OrderByDescending(s => s.FrameNumber)
///     .FirstOrDefault();
///
/// if (snapshot != null)
/// {
///     SnapshotManager.RestoreSnapshot(world, snapshot.Snapshot, serializer);
///     // Replay from snapshot.FrameNumber to targetFrame
/// }
/// </code>
/// </example>
public sealed record SnapshotMarker
{
    /// <summary>
    /// Gets or sets the frame number when this snapshot was captured.
    /// </summary>
    /// <remarks>
    /// The snapshot captures the world state at the beginning of this frame,
    /// before any systems have executed for the frame.
    /// </remarks>
    public required int FrameNumber { get; init; }

    /// <summary>
    /// Gets or sets the elapsed time when this snapshot was captured.
    /// </summary>
    /// <remarks>
    /// This corresponds to the <see cref="ReplayFrame.ElapsedTime"/> of the
    /// frame when the snapshot was taken.
    /// </remarks>
    public required TimeSpan ElapsedTime { get; init; }

    /// <summary>
    /// Gets or sets the complete world state captured at this point.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The snapshot contains all entities, components, hierarchy relationships,
    /// and singletons. It can be restored using
    /// <see cref="SnapshotManager.RestoreSnapshot"/>.
    /// </para>
    /// <para>
    /// Snapshots are created using the configured <see cref="IComponentSerializer"/>
    /// to ensure AOT compatibility.
    /// </para>
    /// </remarks>
    public required WorldSnapshot Snapshot { get; init; }

    /// <summary>
    /// Gets or sets the world state checksum when this snapshot was captured.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The checksum captures the complete world state at the moment of snapshot
    /// capture. It can be used to verify snapshot integrity and detect desyncs
    /// during replay seeking operations.
    /// </para>
    /// <para>
    /// A value of null indicates the checksum was not recorded. This can occur
    /// when recording without checksum generation enabled or when loading older
    /// replay files that predate checksum support.
    /// </para>
    /// </remarks>
    /// <seealso cref="WorldChecksum"/>
    public uint? Checksum { get; init; }
}
