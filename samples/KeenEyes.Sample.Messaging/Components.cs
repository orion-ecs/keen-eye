namespace KeenEyes.Sample.Messaging;

// =============================================================================
// COMPONENT TYPES
// =============================================================================

/// <summary>
/// 2D position component.
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
/// Health component with current and max values.
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
/// Attack capability component.
/// </summary>
[Component]
public partial struct Attack
{
    /// <summary>Damage dealt per attack.</summary>
    public int Damage;
    /// <summary>Range of the attack.</summary>
    public float Range;
}

/// <summary>
/// Target reference component.
/// </summary>
[Component]
public partial struct Target
{
    /// <summary>The targeted entity.</summary>
    public Entity Entity;
}

/// <summary>
/// Score tracking component.
/// </summary>
[Component]
public partial struct Score
{
    /// <summary>Current score value.</summary>
    public int Value;
}

// =============================================================================
// TAG COMPONENTS
// =============================================================================

/// <summary>Marks an entity as the player.</summary>
[TagComponent]
public partial struct Player;

/// <summary>Marks an entity as an enemy.</summary>
[TagComponent]
public partial struct Enemy;

/// <summary>Marks an entity as dead.</summary>
[TagComponent]
public partial struct Dead;
