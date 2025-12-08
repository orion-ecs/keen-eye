namespace KeenEyes.Serialization;

/// <summary>
/// Represents a serialized entity with its ID, name, components, and hierarchy information.
/// </summary>
/// <remarks>
/// <para>
/// This record captures the complete state of an entity for serialization purposes.
/// It includes all components attached to the entity, optional naming information,
/// and parent-child relationships for hierarchy reconstruction.
/// </para>
/// </remarks>
public sealed record SerializedEntity
{
    /// <summary>
    /// Gets or sets the entity's unique identifier within the snapshot.
    /// </summary>
    /// <remarks>
    /// This ID is used to maintain entity references during serialization
    /// and to reconstruct hierarchy relationships during deserialization.
    /// Note that the actual entity IDs after restoration may differ.
    /// </remarks>
    public required int Id { get; init; }

    /// <summary>
    /// Gets or sets the optional name of the entity.
    /// </summary>
    /// <remarks>
    /// Entity names must be unique within a world. If <see langword="null"/>,
    /// the entity is unnamed.
    /// </remarks>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the collection of serialized components attached to this entity.
    /// </summary>
    /// <remarks>
    /// This includes both regular components with data and tag components.
    /// </remarks>
    public required IReadOnlyList<SerializedComponent> Components { get; init; }

    /// <summary>
    /// Gets or sets the ID of this entity's parent, or <see langword="null"/> if it has no parent.
    /// </summary>
    /// <remarks>
    /// This references the <see cref="Id"/> of another <see cref="SerializedEntity"/>
    /// in the same snapshot. During restoration, parent-child relationships are
    /// reconstructed using these references.
    /// </remarks>
    public int? ParentId { get; init; }
}
