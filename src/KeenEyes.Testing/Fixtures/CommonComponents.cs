namespace KeenEyes.Testing.Fixtures;

/// <summary>
/// Common component types for use in tests.
/// </summary>
/// <remarks>
/// <para>
/// These components provide standard archetypes that are commonly needed
/// when testing ECS functionality. They can be used directly or as templates
/// for custom test components.
/// </para>
/// </remarks>

/// <summary>
/// A 2D position component.
/// </summary>
[Component]
public partial struct TestPosition
{
    /// <summary>The X coordinate.</summary>
    public float X;

    /// <summary>The Y coordinate.</summary>
    public float Y;

    /// <summary>
    /// Creates a new position with the specified coordinates.
    /// </summary>
    public static TestPosition Create(float x, float y) => new() { X = x, Y = y };

    /// <summary>
    /// Creates a position at the origin (0, 0).
    /// </summary>
    public static TestPosition Zero => new() { X = 0, Y = 0 };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Position({X}, {Y})";
}

/// <summary>
/// A 3D position component.
/// </summary>
[Component]
public partial struct TestPosition3D
{
    /// <summary>The X coordinate.</summary>
    public float X;

    /// <summary>The Y coordinate.</summary>
    public float Y;

    /// <summary>The Z coordinate.</summary>
    public float Z;

    /// <summary>
    /// Creates a new position with the specified coordinates.
    /// </summary>
    public static TestPosition3D Create(float x, float y, float z) => new() { X = x, Y = y, Z = z };

    /// <summary>
    /// Creates a position at the origin (0, 0, 0).
    /// </summary>
    public static TestPosition3D Zero => new() { X = 0, Y = 0, Z = 0 };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Position3D({X}, {Y}, {Z})";
}

/// <summary>
/// A 2D velocity component.
/// </summary>
[Component]
public partial struct TestVelocity
{
    /// <summary>The X velocity.</summary>
    public float VX;

    /// <summary>The Y velocity.</summary>
    public float VY;

    /// <summary>
    /// Creates a new velocity with the specified values.
    /// </summary>
    public static TestVelocity Create(float vx, float vy) => new() { VX = vx, VY = vy };

    /// <summary>
    /// Creates a zero velocity.
    /// </summary>
    public static TestVelocity Zero => new() { VX = 0, VY = 0 };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Velocity({VX}, {VY})";
}

/// <summary>
/// A health component with current and maximum values.
/// </summary>
[Component]
public partial struct TestHealth
{
    /// <summary>The current health value.</summary>
    public int Current;

    /// <summary>The maximum health value.</summary>
    public int Max;

    /// <summary>
    /// Creates a new health component with the specified values.
    /// </summary>
    public static TestHealth Create(int current, int max) => new() { Current = current, Max = max };

    /// <summary>
    /// Creates a full health component with the specified maximum.
    /// </summary>
    public static TestHealth Full(int max) => new() { Current = max, Max = max };

    /// <summary>
    /// Gets the health percentage (0.0 to 1.0).
    /// </summary>
    public readonly float Percentage => Max > 0 ? (float)Current / Max : 0f;

    /// <summary>
    /// Gets whether the entity is alive (health > 0).
    /// </summary>
    public readonly bool IsAlive => Current > 0;

    /// <inheritdoc/>
    public override readonly string ToString() => $"Health({Current}/{Max})";
}

/// <summary>
/// A damage component for combat systems.
/// </summary>
[Component]
public partial struct TestDamage
{
    /// <summary>The damage amount.</summary>
    public int Amount;

    /// <summary>
    /// Creates a new damage component with the specified amount.
    /// </summary>
    public static TestDamage Create(int amount) => new() { Amount = amount };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Damage({Amount})";
}

/// <summary>
/// A speed component for movement systems.
/// </summary>
[Component]
public partial struct TestSpeed
{
    /// <summary>The movement speed.</summary>
    public float Value;

    /// <summary>
    /// Creates a new speed component with the specified value.
    /// </summary>
    public static TestSpeed Create(float value) => new() { Value = value };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Speed({Value})";
}

/// <summary>
/// A rotation component in degrees.
/// </summary>
[Component]
public partial struct TestRotation
{
    /// <summary>The rotation angle in degrees.</summary>
    public float Angle;

    /// <summary>
    /// Creates a new rotation component with the specified angle.
    /// </summary>
    public static TestRotation Create(float angle) => new() { Angle = angle };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Rotation({Angle}°)";
}

/// <summary>
/// A scale component for entity sizing.
/// </summary>
[Component]
public partial struct TestScale
{
    /// <summary>The scale value.</summary>
    public float Value;

    /// <summary>
    /// Creates a new scale component with the specified value.
    /// </summary>
    public static TestScale Create(float value) => new() { Value = value };

    /// <summary>
    /// Creates a unit scale (1.0).
    /// </summary>
    public static TestScale One => new() { Value = 1.0f };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Scale({Value})";
}

/// <summary>
/// A lifetime component for temporary entities.
/// </summary>
[Component]
public partial struct TestLifetime
{
    /// <summary>The remaining lifetime in seconds.</summary>
    public float Remaining;

    /// <summary>
    /// Creates a new lifetime component with the specified duration.
    /// </summary>
    public static TestLifetime Create(float seconds) => new() { Remaining = seconds };

    /// <summary>
    /// Gets whether the lifetime has expired.
    /// </summary>
    public readonly bool HasExpired => Remaining <= 0;

    /// <inheritdoc/>
    public override readonly string ToString() => $"Lifetime({Remaining}s)";
}

/// <summary>
/// A team component for grouping entities.
/// </summary>
[Component]
public partial struct TestTeam
{
    /// <summary>The team identifier.</summary>
    public int Id;

    /// <summary>
    /// Creates a new team component with the specified ID.
    /// </summary>
    public static TestTeam Create(int id) => new() { Id = id };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Team({Id})";
}

/// <summary>
/// A counter component for tracking values.
/// </summary>
[Component]
public partial struct TestCounter
{
    /// <summary>The current count.</summary>
    public int Value;

    /// <summary>
    /// Creates a new counter with the specified initial value.
    /// </summary>
    public static TestCounter Create(int value) => new() { Value = value };

    /// <inheritdoc/>
    public override readonly string ToString() => $"Counter({Value})";
}

/// <summary>
/// Tag component marking an entity as a player.
/// </summary>
[TagComponent]
public partial struct PlayerTag;

/// <summary>
/// Tag component marking an entity as an enemy.
/// </summary>
[TagComponent]
public partial struct EnemyTag;

/// <summary>
/// Tag component marking an entity as a projectile.
/// </summary>
[TagComponent]
public partial struct ProjectileTag;

/// <summary>
/// Tag component marking an entity as a pickup/collectible.
/// </summary>
[TagComponent]
public partial struct PickupTag;

/// <summary>
/// Tag component marking an entity as dead.
/// </summary>
[TagComponent]
public partial struct DeadTag;

/// <summary>
/// Tag component marking an entity as active.
/// </summary>
[TagComponent]
public partial struct ActiveTag;

/// <summary>
/// Tag component marking an entity as disabled/inactive.
/// </summary>
[TagComponent]
public partial struct DisabledTag;

/// <summary>
/// Tag component marking an entity as invulnerable.
/// </summary>
[TagComponent]
public partial struct InvulnerableTag;
