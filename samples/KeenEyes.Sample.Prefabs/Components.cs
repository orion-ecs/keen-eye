namespace KeenEyes.Sample.Prefabs;

/// <summary>
/// Position component for entity location.
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
/// Velocity component for entity movement.
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
/// Health component for entities that can take damage.
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
/// Damage component for entities that deal damage.
/// </summary>
[Component]
public partial struct Damage
{
    /// <summary>Damage amount per hit.</summary>
    public int Amount;
    /// <summary>Attack range.</summary>
    public float Range;
}

/// <summary>
/// AI behavior component.
/// </summary>
[Component]
public partial struct AIBehavior
{
    /// <summary>How far the AI can detect targets.</summary>
    public float DetectionRange;
    /// <summary>Cooldown between attacks in seconds.</summary>
    public float AttackCooldown;
}

/// <summary>
/// Sprite rendering component.
/// </summary>
[Component]
public partial struct Sprite
{
    /// <summary>Texture asset identifier.</summary>
    public string TextureId;
    /// <summary>Render layer (higher = on top).</summary>
    public int Layer;
}

/// <summary>
/// Tag for enemy entities.
/// </summary>
[TagComponent]
public partial struct Enemy;

/// <summary>
/// Tag for flying enemies.
/// </summary>
[TagComponent]
public partial struct Flying;

/// <summary>
/// Tag for boss enemies.
/// </summary>
[TagComponent]
public partial struct Boss;

/// <summary>
/// Tag for player entities.
/// </summary>
[TagComponent]
public partial struct Player;

/// <summary>
/// Tag for collectible items.
/// </summary>
[TagComponent]
public partial struct Collectible;
