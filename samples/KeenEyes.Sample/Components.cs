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
/// <remarks>
/// This component follows ECS principles by containing only data.
/// Computed properties like Percentage are implemented as extension properties
/// (see HealthExtensions.cs) to keep the component as pure data while maintaining
/// convenient access patterns.
/// </remarks>
[Component]
public partial struct Health
{
    /// <summary>Current health points.</summary>
    public float Current;
    /// <summary>Maximum health points.</summary>
    public float Max;
    /// <summary>Whether this entity is immune to damage.</summary>
    public bool Invulnerable;
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

// =============================================================================
// COMPONENT MIXINS
// =============================================================================
// Mixins allow compile-time field composition by copying fields from one
// struct into another. This is useful for sharing common field patterns
// across multiple component types without duplication.
// =============================================================================

/// <summary>
/// Base 2D position mixin - can be reused in multiple components.
/// </summary>
public partial struct Position2DMixin
{
    /// <summary>X coordinate.</summary>
    public float X;
    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>
/// Base 2D velocity mixin - can be reused in multiple components.
/// </summary>
public partial struct Velocity2DMixin
{
    /// <summary>X velocity component.</summary>
    public float VelX;
    /// <summary>Y velocity component.</summary>
    public float VelY;
}

/// <summary>
/// Transform component using mixins for position and velocity.
/// The source generator will copy X, Y, VelX, VelY fields into this component.
/// </summary>
[Component]
[Mixin(typeof(Position2DMixin))]
[Mixin(typeof(Velocity2DMixin))]
public partial struct Transform2D
{
    // Mixin fields (X, Y, VelX, VelY) are generated automatically
    // We only define additional fields unique to Transform2D

    /// <summary>Rotation angle in radians.</summary>
    public float Rotation;

    /// <summary>Scale factor.</summary>
    public float Scale;
}

/// <summary>
/// Example of transitive mixins - CharacterStats mixins BaseStats,
/// which will bring all fields from BaseStats into CharacterStats.
/// </summary>
public partial struct BaseStats
{
    /// <summary>Strength attribute.</summary>
    public int Strength;

    /// <summary>Dexterity attribute.</summary>
    public int Dexterity;
}

/// <summary>
/// Intermediate mixin that includes BaseStats.
/// </summary>
[Mixin(typeof(BaseStats))]
public partial struct CharacterStats
{
    // Fields from BaseStats (Strength, Dexterity) are included transitively

    /// <summary>Intelligence attribute.</summary>
    public int Intelligence;

    /// <summary>Wisdom attribute.</summary>
    public int Wisdom;
}

/// <summary>
/// Advanced character component using transitive mixin.
/// Will have: Strength, Dexterity, Intelligence, Wisdom, Level, Experience
/// </summary>
[Component]
[Mixin(typeof(CharacterStats))]
public partial struct AdvancedCharacter
{
    /// <summary>Character level.</summary>
    public int Level;

    /// <summary>Experience points.</summary>
    public int Experience;
}
