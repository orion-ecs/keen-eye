namespace KeenEyes.Sample.Relationships;

// =============================================================================
// COMPONENT DEFINITIONS
// =============================================================================
// This sample demonstrates the v0.3.0 features: entity hierarchies, events,
// and change tracking.
// =============================================================================

/// <summary>
/// Position in 2D space.
/// </summary>
[Component]
public partial struct Position
{
    /// <summary>X coordinate.</summary>
    public float X;
    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>
/// Local offset from parent position (for hierarchies).
/// </summary>
[Component]
public partial struct LocalOffset
{
    /// <summary>X offset from parent.</summary>
    public float X;
    /// <summary>Y offset from parent.</summary>
    public float Y;
}

/// <summary>
/// Computed world position (calculated from parent + local offset).
/// </summary>
[Component]
public partial struct WorldPosition
{
    /// <summary>World X coordinate.</summary>
    public float X;
    /// <summary>World Y coordinate.</summary>
    public float Y;
}

/// <summary>
/// Entity health with current and max values.
/// </summary>
[Component]
public partial struct Health
{
    /// <summary>Current health points.</summary>
    public int Current;
    /// <summary>Maximum health points.</summary>
    public int Max;
}

/// <summary>
/// Damage amount for an entity that deals damage.
/// </summary>
[Component]
public partial struct Damage
{
    /// <summary>Damage dealt per hit.</summary>
    public int Amount;
}

/// <summary>
/// Name component for display purposes.
/// </summary>
[Component]
public partial struct DisplayName
{
    /// <summary>The display name.</summary>
    public required string Name;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================

/// <summary>Marks an entity as the scene root.</summary>
[TagComponent]
public partial struct SceneRoot;

/// <summary>Marks an entity as a UI element.</summary>
[TagComponent]
public partial struct UIElement;

/// <summary>Marks an entity as dirty for network sync.</summary>
[TagComponent]
public partial struct NetworkDirty;

// =============================================================================
// CUSTOM EVENTS
// =============================================================================

/// <summary>
/// Event fired when an entity takes damage.
/// </summary>
/// <param name="Target">The entity that took damage.</param>
/// <param name="Amount">The amount of damage dealt.</param>
/// <param name="Source">The entity that dealt the damage, if any.</param>
public readonly record struct DamageEvent(Entity Target, int Amount, Entity? Source);

/// <summary>
/// Event fired when an entity is healed.
/// </summary>
/// <param name="Target">The entity that was healed.</param>
/// <param name="Amount">The amount of healing applied.</param>
public readonly record struct HealEvent(Entity Target, int Amount);

/// <summary>
/// Event fired when an entity dies.
/// </summary>
/// <param name="Entity">The entity that died.</param>
/// <param name="Cause">The cause of death, if known.</param>
public readonly record struct DeathEvent(Entity Entity, string? Cause);
