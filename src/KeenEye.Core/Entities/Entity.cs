namespace KeenEye;

/// <summary>
/// Represents an entity in the ECS world.
/// An entity is essentially just an ID - components give it data and behavior.
/// </summary>
/// <param name="Id">The unique identifier for this entity.</param>
/// <param name="Version">Generation counter to detect stale references.</param>
public readonly record struct Entity(int Id, int Version)
{
    /// <summary>A null/invalid entity reference.</summary>
    public static readonly Entity Null = new(-1, 0);

    /// <summary>Whether this entity reference is valid (non-null).</summary>
    public bool IsValid => Id >= 0;

    /// <inheritdoc />
    public override string ToString() => $"Entity({Id}v{Version})";
}
