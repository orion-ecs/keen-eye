using System.Text.Json.Serialization;

namespace KeenEyes.Serialization;

/// <summary>
/// Represents incremental changes to a world state relative to a baseline snapshot.
/// </summary>
/// <remarks>
/// <para>
/// Delta snapshots capture only what has changed since a baseline snapshot was taken,
/// resulting in significantly smaller save files for games with many entities but
/// few changes per save cycle.
/// </para>
/// <para>
/// To restore a world, you must first load the baseline snapshot, then apply
/// the delta snapshots in sequence.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create delta from current world state
/// var delta = DeltaDiffer.CreateDelta(world, baselineSnapshot, serializer);
///
/// // Later, restore by applying delta to baseline
/// SnapshotManager.RestoreSnapshot(world, baselineSnapshot, serializer);
/// DeltaRestorer.ApplyDelta(world, delta, serializer);
/// </code>
/// </example>
public sealed record DeltaSnapshot
{
    /// <summary>
    /// Gets the version of the delta snapshot format.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the timestamp when this delta was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the sequence number of this delta relative to the baseline.
    /// </summary>
    /// <remarks>
    /// Sequence numbers start at 1 for the first delta after a baseline.
    /// </remarks>
    public int SequenceNumber { get; init; } = 1;

    /// <summary>
    /// Gets the slot name of the baseline this delta is relative to.
    /// </summary>
    public required string BaselineSlotName { get; init; }

    /// <summary>
    /// Gets the entities that were created since the baseline.
    /// </summary>
    public IReadOnlyList<SerializedEntity> CreatedEntities { get; init; } = [];

    /// <summary>
    /// Gets the IDs of entities that were destroyed since the baseline.
    /// </summary>
    public IReadOnlyList<int> DestroyedEntityIds { get; init; } = [];

    /// <summary>
    /// Gets the modifications to existing entities.
    /// </summary>
    public IReadOnlyList<EntityDelta> ModifiedEntities { get; init; } = [];

    /// <summary>
    /// Gets the singletons that were added or modified since the baseline.
    /// </summary>
    public IReadOnlyList<SerializedSingleton> ModifiedSingletons { get; init; } = [];

    /// <summary>
    /// Gets the type names of singletons that were removed since the baseline.
    /// </summary>
    public IReadOnlyList<string> RemovedSingletonTypes { get; init; } = [];

    /// <summary>
    /// Gets optional metadata attached to this delta.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Gets whether this delta is empty (no changes).
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty =>
        CreatedEntities.Count == 0 &&
        DestroyedEntityIds.Count == 0 &&
        ModifiedEntities.Count == 0 &&
        ModifiedSingletons.Count == 0 &&
        RemovedSingletonTypes.Count == 0;

    /// <summary>
    /// Gets the total number of changes in this delta.
    /// </summary>
    [JsonIgnore]
    public int ChangeCount =>
        CreatedEntities.Count +
        DestroyedEntityIds.Count +
        ModifiedEntities.Count +
        ModifiedSingletons.Count +
        RemovedSingletonTypes.Count;
}

/// <summary>
/// Represents changes to a single entity.
/// </summary>
public sealed record EntityDelta
{
    /// <summary>
    /// Gets the ID of the entity that was modified.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the entity's name changed.
    /// </summary>
    public string? NewName { get; init; }

    /// <summary>
    /// Gets whether the entity's parent changed.
    /// </summary>
    public int? NewParentId { get; init; }

    /// <summary>
    /// Gets whether the parent was explicitly set to null (orphaned).
    /// </summary>
    public bool ParentRemoved { get; init; }

    /// <summary>
    /// Gets components that were added to the entity.
    /// </summary>
    public IReadOnlyList<SerializedComponent> AddedComponents { get; init; } = [];

    /// <summary>
    /// Gets type names of components that were removed from the entity.
    /// </summary>
    public IReadOnlyList<string> RemovedComponentTypes { get; init; } = [];

    /// <summary>
    /// Gets components that were modified on the entity.
    /// </summary>
    public IReadOnlyList<SerializedComponent> ModifiedComponents { get; init; } = [];

    /// <summary>
    /// Gets whether this delta is empty (no changes).
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty =>
        NewName is null &&
        NewParentId is null &&
        !ParentRemoved &&
        AddedComponents.Count == 0 &&
        RemovedComponentTypes.Count == 0 &&
        ModifiedComponents.Count == 0;
}
