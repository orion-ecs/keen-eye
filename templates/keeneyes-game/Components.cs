namespace KeenEyesGame;

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
/// Marks an entity as player-controlled.
/// </summary>
[TagComponent]
public partial struct Player;

/// <summary>
/// Marks an entity as an enemy.
/// </summary>
[TagComponent]
public partial struct Enemy;
