namespace KeenEyes.Sample;

// =============================================================================
// COMPONENT DEFINITIONS
// =============================================================================
// Users define components as partial structs with the [Component] attribute.
// The source generator will produce:
//   1. ComponentInfo metadata (ID, size, etc.)
//   2. Fluent builder extension methods (WithPosition, WithVelocity, etc.)
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
/// Velocity for movement.
/// </summary>
[Component]
public partial struct Velocity
{
    /// <summary>X velocity component.</summary>
    public float X;
    /// <summary>Y velocity component.</summary>
    public float Y;
}

/// <summary>
/// Entity health with current and max values.
/// </summary>
[Component]
public partial struct Health
{
    /// <summary>Current health points.</summary>
    public float Current;
    /// <summary>Maximum health points.</summary>
    public float Max;
    /// <summary>Whether this entity is immune to damage.</summary>
    public bool Invulnerable;

    /// <summary>Gets the health percentage (0-1).</summary>
    public readonly float Percentage => Max > 0 ? Current / Max : 0;
}

/// <summary>
/// Visual representation with a sprite.
/// </summary>
[Component]
public partial struct Sprite
{
    /// <summary>Required - must be provided when creating.</summary>
    public required string TextureId;

    /// <summary>Render layer (defaults to 0).</summary>
    public int Layer;

    /// <summary>Opacity from 0-1 (defaults to 1).</summary>
    [DefaultValue(1f)]
    public float Opacity;
}

/// <summary>
/// Configuration with various default values.
/// </summary>
[Component]
public partial struct Config
{
    /// <summary>Maximum number of entities allowed.</summary>
    [DefaultValue(100)]
    public int MaxEntities;

    /// <summary>Simulation tick rate in Hz.</summary>
    [DefaultValue(60f)]
    public float TickRate;

    /// <summary>Whether debug mode is enabled.</summary>
    [DefaultValue(true)]
    public bool EnableDebug;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================
// Tag components are zero-size markers used for filtering queries.
// They generate parameterless builder methods.
// =============================================================================

/// <summary>Marks an entity as player-controlled.</summary>
[TagComponent]
public partial struct Player;

/// <summary>Marks an entity as an enemy.</summary>
[TagComponent]
public partial struct Enemy;

/// <summary>Marks an entity as disabled/inactive.</summary>
[TagComponent]
public partial struct Disabled;

/// <summary>Marks an entity as needing destruction.</summary>
[TagComponent]
public partial struct PendingDestroy;
