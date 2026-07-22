using KeenEyes.Serialization;

namespace KeenEyes.Replay;

/// <summary>
/// Marks the position of a world snapshot within a replay recording.
/// </summary>
/// <remarks>
/// <para>
/// Snapshots are periodic captures of world state during recording. They enable
/// efficient seeking by providing restoration points throughout the replay timeline.
/// </para>
/// <para>
/// A marker is one of two kinds, distinguished by <see cref="IsKeyframe"/>:
/// <list type="bullet">
/// <item><description>
/// A <b>keyframe</b> carries a complete <see cref="Snapshot"/> of the world state and
/// no delta. Keyframes are self-contained restore points.
/// </description></item>
/// <item><description>
/// A <b>delta marker</b> carries only the changes since the previous marker's state
/// (<see cref="Delta"/>) and references that marker via <see cref="BaselineFrameNumber"/>.
/// Reconstructing a delta marker requires restoring the nearest preceding keyframe and
/// applying the intervening deltas in order.
/// </description></item>
/// </list>
/// Storing deltas between keyframes (controlled by
/// <see cref="ReplayOptions.KeyframeInterval"/>) substantially reduces recording size
/// when most entities are unchanged between snapshots.
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
    /// Gets the complete world state captured at this point, or <c>null</c> when this
    /// marker is a delta (see <see cref="Delta"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// When present, the snapshot contains all entities, components, hierarchy
    /// relationships, and singletons, and can be restored using
    /// <see cref="SnapshotManager.RestoreSnapshot"/>. This is populated only for
    /// keyframe markers; delta markers leave it <c>null</c> and store their changes in
    /// <see cref="Delta"/> instead.
    /// </para>
    /// <para>
    /// Snapshots are created using the configured <see cref="IComponentSerializer"/>
    /// to ensure AOT compatibility.
    /// </para>
    /// </remarks>
    public WorldSnapshot? Snapshot { get; init; }

    /// <summary>
    /// Gets the incremental changes relative to the baseline marker identified by
    /// <see cref="BaselineFrameNumber"/>, or <c>null</c> when this marker is a keyframe
    /// (see <see cref="Snapshot"/>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A delta stores only entities and components that changed since the previous
    /// marker's state. To reconstruct the world at this marker, restore the nearest
    /// preceding keyframe's <see cref="Snapshot"/> and apply each subsequent marker's
    /// delta in order using <see cref="DeltaRestorer.ApplyDelta"/>.
    /// </para>
    /// </remarks>
    public DeltaSnapshot? Delta { get; init; }

    /// <summary>
    /// Gets the frame number of the marker this marker's <see cref="Delta"/> is relative
    /// to, or <c>null</c> for keyframe markers.
    /// </summary>
    /// <remarks>
    /// The baseline is always the immediately preceding marker in the recording, so the
    /// delta chain from a keyframe up to any delta marker can be replayed in order.
    /// </remarks>
    public int? BaselineFrameNumber { get; init; }

    /// <summary>
    /// Gets a value indicating whether this marker is a full-state keyframe.
    /// </summary>
    /// <remarks>
    /// Returns <c>true</c> when <see cref="Snapshot"/> is present (a self-contained
    /// restore point) and <c>false</c> when the marker carries only a <see cref="Delta"/>.
    /// </remarks>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsKeyframe => Snapshot is not null;

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
