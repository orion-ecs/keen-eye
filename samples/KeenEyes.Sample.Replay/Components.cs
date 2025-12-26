namespace KeenEyes.Sample.Replay;

/// <summary>
/// Position component for entity location.
/// </summary>
[Component(Serializable = true)]
public partial struct Position
{
    /// <summary>X coordinate.</summary>
    public float X;
    /// <summary>Y coordinate.</summary>
    public float Y;
}

/// <summary>
/// Velocity component for entity movement.
/// </summary>
[Component(Serializable = true)]
public partial struct Velocity
{
    /// <summary>X velocity.</summary>
    public float X;
    /// <summary>Y velocity.</summary>
    public float Y;
}

/// <summary>
/// Health component for entities that can take damage.
/// </summary>
[Component(Serializable = true)]
public partial struct Health
{
    /// <summary>Current health points.</summary>
    public int Current;
    /// <summary>Maximum health points.</summary>
    public int Max;
}

/// <summary>
/// Tag for player entities.
/// </summary>
[TagComponent]
public partial struct Player;

/// <summary>
/// Tag for enemy entities.
/// </summary>
[TagComponent]
public partial struct Enemy;

/// <summary>
/// Tag for projectile entities.
/// </summary>
[TagComponent]
public partial struct Projectile;
