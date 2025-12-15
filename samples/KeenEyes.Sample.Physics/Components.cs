namespace KeenEyes.Sample.Physics;

// =============================================================================
// TAG COMPONENTS - Used to identify entities in the physics demos
// =============================================================================

/// <summary>
/// Tag marking an entity as a ground plane.
/// </summary>
[TagComponent]
public partial struct GroundTag;

/// <summary>
/// Tag marking an entity as a falling object (boxes, spheres, etc.).
/// </summary>
[TagComponent]
public partial struct FallingObjectTag;

/// <summary>
/// Tag marking an entity as part of a stack for the stacking demo.
/// </summary>
[TagComponent]
public partial struct StackedObjectTag;

/// <summary>
/// Tag marking an entity as a raycast target for visualization.
/// </summary>
[TagComponent]
public partial struct RaycastTargetTag;

/// <summary>
/// Tag marking an entity as having been hit by a raycast.
/// </summary>
[TagComponent]
public partial struct RaycastHitTag;

// =============================================================================
// DATA COMPONENTS - Store additional data for visualization and demos
// =============================================================================

/// <summary>
/// Component storing the name/label of an entity for display purposes.
/// </summary>
[Component]
public partial struct EntityLabel
{
    /// <summary>
    /// The display name of the entity.
    /// </summary>
    public string Name;
}

/// <summary>
/// Component storing collision count for an entity.
/// </summary>
[Component]
public partial struct CollisionCounter
{
    /// <summary>
    /// The number of collisions recorded.
    /// </summary>
    public int Count;
}

/// <summary>
/// Component marking when an entity was spawned (for timed demos).
/// </summary>
[Component]
public partial struct SpawnTime
{
    /// <summary>
    /// The time at which the entity was spawned.
    /// </summary>
    public float Time;
}
