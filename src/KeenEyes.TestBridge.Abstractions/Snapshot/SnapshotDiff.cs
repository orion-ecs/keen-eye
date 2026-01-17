namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Represents the differences between two snapshots.
/// </summary>
/// <remarks>
/// The diff provides a detailed breakdown of changes at the entity, component,
/// and field levels, enabling precise identification of state changes.
/// </remarks>
public sealed record SnapshotDiff
{
    /// <summary>
    /// Gets the name of the first (baseline) snapshot.
    /// </summary>
    public required string Snapshot1 { get; init; }

    /// <summary>
    /// Gets the name of the second snapshot being compared.
    /// </summary>
    public required string Snapshot2 { get; init; }

    /// <summary>
    /// Gets entities that were added (present in snapshot2 but not in snapshot1).
    /// </summary>
    public required IReadOnlyList<EntityDiff> AddedEntities { get; init; }

    /// <summary>
    /// Gets entities that were removed (present in snapshot1 but not in snapshot2).
    /// </summary>
    public required IReadOnlyList<EntityDiff> RemovedEntities { get; init; }

    /// <summary>
    /// Gets entities that exist in both snapshots but have changes.
    /// </summary>
    public required IReadOnlyList<EntityDiff> ModifiedEntities { get; init; }

    /// <summary>
    /// Gets the total number of changes across all categories.
    /// </summary>
    public required int TotalChanges { get; init; }

    /// <summary>
    /// Gets whether the snapshots are identical.
    /// </summary>
    public bool AreEqual => TotalChanges == 0;
}
