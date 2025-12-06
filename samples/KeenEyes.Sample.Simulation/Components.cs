namespace KeenEyes.Sample.Simulation;

// =============================================================================
// COMPONENT DEFINITIONS - Self-Running Simulation
// =============================================================================
// This simulation demonstrates a complete ECS game loop with:
// - Movement physics with wall bouncing
// - Collision detection between entities
// - Combat with health and damage
// - Entity spawning and despawning
// - Real-time ASCII visualization
// =============================================================================

/// <summary>
/// Position in 2D simulation space.
/// </summary>
[Component]
public partial struct Position
{
    /// <summary>X coordinate (0 to WorldWidth).</summary>
    public float X;
    /// <summary>Y coordinate (0 to WorldHeight).</summary>
    public float Y;
}

/// <summary>
/// Velocity for movement.
/// </summary>
[Component]
public partial struct Velocity
{
    /// <summary>X velocity component (units per second).</summary>
    public float X;
    /// <summary>Y velocity component (units per second).</summary>
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

    /// <summary>Gets whether the entity is alive.</summary>
    public readonly bool IsAlive => Current > 0;

    /// <summary>Gets health as a fraction (0-1).</summary>
    public readonly float Fraction => Max > 0 ? (float)Current / Max : 0;
}

/// <summary>
/// Damage dealt on collision.
/// </summary>
[Component]
public partial struct Damage
{
    /// <summary>Damage dealt per collision.</summary>
    public int Amount;
}

/// <summary>
/// Collision radius for hit detection.
/// </summary>
[Component]
public partial struct Collider
{
    /// <summary>Collision radius.</summary>
    [DefaultValue(0.5f)]
    public float Radius;
}

/// <summary>
/// Visual representation for ASCII rendering.
/// </summary>
[Component]
public partial struct Renderable
{
    /// <summary>Character to display.</summary>
    public char Symbol;

    /// <summary>Console color for rendering.</summary>
    [DefaultValue(ConsoleColor.White)]
    public ConsoleColor Color;
}

/// <summary>
/// Remaining lifetime before entity is destroyed.
/// </summary>
[Component]
public partial struct Lifetime
{
    /// <summary>Seconds remaining before despawn.</summary>
    public float Remaining;
}

/// <summary>
/// Cooldown timer for attacks or spawning.
/// </summary>
[Component]
public partial struct Cooldown
{
    /// <summary>Seconds remaining in cooldown.</summary>
    public float Remaining;
}

/// <summary>
/// Statistics tracking for display.
/// </summary>
[Component]
public partial struct Stats
{
    /// <summary>Total kills by this entity.</summary>
    public int Kills;
    /// <summary>Total damage dealt.</summary>
    public int DamageDealt;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================

/// <summary>Marks an entity as the player (controlled by input).</summary>
[TagComponent]
public partial struct Player;

/// <summary>Marks an entity as an enemy.</summary>
[TagComponent]
public partial struct Enemy;

/// <summary>Marks an entity as a projectile.</summary>
[TagComponent]
public partial struct Projectile;

/// <summary>Marks an entity as pending destruction.</summary>
[TagComponent]
public partial struct Dead;

/// <summary>Marks an entity as invulnerable.</summary>
[TagComponent]
public partial struct Invulnerable;
