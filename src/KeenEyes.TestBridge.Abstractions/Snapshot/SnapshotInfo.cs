namespace KeenEyes.TestBridge.Snapshot;

/// <summary>
/// Metadata about a stored snapshot.
/// </summary>
/// <remarks>
/// Provides summary information about a snapshot without including the full
/// entity/component data. Use <see cref="ISnapshotController.ExportJsonAsync"/>
/// to retrieve the complete snapshot data.
/// </remarks>
public sealed record SnapshotInfo
{
    /// <summary>
    /// Gets the unique name of this snapshot.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when this snapshot was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Gets the number of entities in this snapshot.
    /// </summary>
    public required int EntityCount { get; init; }

    /// <summary>
    /// Gets the total number of components across all entities.
    /// </summary>
    public required int ComponentCount { get; init; }

    /// <summary>
    /// Gets the approximate size of the snapshot in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }

    /// <summary>
    /// Gets whether this is the quicksave slot.
    /// </summary>
    public bool IsQuickSave { get; init; }
}
