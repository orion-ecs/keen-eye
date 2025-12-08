namespace KeenEyes.Serialization;

/// <summary>
/// Represents a complete snapshot of a world's state.
/// </summary>
/// <remarks>
/// <para>
/// A snapshot captures all entities, their components, hierarchy relationships,
/// and singleton data at a specific point in time. This can be serialized to
/// JSON or binary format for persistence.
/// </para>
/// <para>
/// Snapshots are immutable once created. To modify world state, restore the
/// snapshot to a world, make changes, and create a new snapshot.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a snapshot
/// var snapshot = SnapshotManager.CreateSnapshot(world);
///
/// // Serialize to JSON
/// var json = SnapshotManager.ToJson(snapshot);
///
/// // Later, deserialize and restore
/// var loadedSnapshot = SnapshotManager.FromJson(json);
/// SnapshotManager.RestoreSnapshot(world, loadedSnapshot);
/// </code>
/// </example>
public sealed record WorldSnapshot
{
    /// <summary>
    /// Gets or sets the format version of this snapshot.
    /// </summary>
    /// <remarks>
    /// Used for backwards compatibility when the snapshot format changes.
    /// Current version is 1.
    /// </remarks>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets or sets the timestamp when this snapshot was created.
    /// </summary>
    /// <remarks>
    /// Stored as UTC time for consistency across time zones.
    /// </remarks>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets or sets the collection of all serialized entities in the snapshot.
    /// </summary>
    /// <remarks>
    /// Entities are stored in creation order to facilitate predictable restoration.
    /// Parent entities should appear before their children to ensure hierarchy
    /// can be reconstructed in a single pass.
    /// </remarks>
    public required IReadOnlyList<SerializedEntity> Entities { get; init; }

    /// <summary>
    /// Gets or sets the collection of all serialized singletons in the snapshot.
    /// </summary>
    /// <remarks>
    /// Singletons are world-level data not tied to any entity.
    /// </remarks>
    public required IReadOnlyList<SerializedSingleton> Singletons { get; init; }

    /// <summary>
    /// Gets or sets optional metadata associated with this snapshot.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This dictionary can be used to store application-specific metadata
    /// such as save slot name, player name, game progress, etc.
    /// </para>
    /// <para>
    /// Metadata values must be JSON-serializable types (strings, numbers, booleans,
    /// or nested dictionaries/arrays of these types).
    /// </para>
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
