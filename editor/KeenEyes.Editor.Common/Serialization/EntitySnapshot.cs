namespace KeenEyes.Editor.Common.Serialization;

/// <summary>
/// Represents a snapshot of an entity's complete state for editor operations.
/// </summary>
/// <remarks>
/// <para>
/// This record captures all aspects of an entity including its components,
/// name, and hierarchy relationships. It is designed for in-memory editor
/// operations like clipboard (cut/copy/paste) and undo/redo.
/// </para>
/// <para>
/// Unlike <see cref="KeenEyes.Serialization.SerializedEntity"/> which is optimized
/// for JSON/binary file serialization, this type stores component data as boxed
/// objects for faster in-process operations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Capture an entity's state
/// var snapshot = EntitySerializer.CaptureEntity(world, entity);
///
/// // Later, restore the entity
/// var newEntity = EntitySerializer.RestoreEntity(world, snapshot);
/// </code>
/// </example>
public sealed record EntitySnapshot
{
    /// <summary>
    /// Gets or sets the original entity ID at the time of capture.
    /// </summary>
    /// <remarks>
    /// This ID is for reference purposes only. When the snapshot is restored,
    /// the new entity will have a different ID assigned by the world.
    /// </remarks>
    public int OriginalId { get; init; }

    /// <summary>
    /// Gets or sets the optional name of the entity.
    /// </summary>
    /// <remarks>
    /// Entity names must be unique within a world. When restoring, if the name
    /// is already taken, a suffix may be added to ensure uniqueness.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the collection of component snapshots for this entity.
    /// </summary>
    /// <remarks>
    /// This includes both regular components with data and tag components.
    /// The order of components is not significant.
    /// </remarks>
    public required IReadOnlyList<ComponentSnapshot> Components { get; init; }

    /// <summary>
    /// Gets or sets the snapshots of child entities.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When an entity with children is captured, the entire subtree is included.
    /// This enables paste/undo operations to restore the complete hierarchy.
    /// </para>
    /// <para>
    /// Empty if the entity has no children.
    /// </para>
    /// </remarks>
    public IReadOnlyList<EntitySnapshot> Children { get; init; } = [];

    /// <summary>
    /// Gets or sets the timestamp when this snapshot was created.
    /// </summary>
    /// <remarks>
    /// Used for display purposes in undo history and clipboard management.
    /// </remarks>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
