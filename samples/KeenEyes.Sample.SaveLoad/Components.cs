namespace KeenEyes.Sample.SaveLoad;

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
    /// <summary>X velocity component.</summary>
    public float X;
    /// <summary>Y velocity component.</summary>
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
/// Experience and level tracking.
/// </summary>
[Component(Serializable = true)]
public partial struct Experience
{
    /// <summary>Current character level.</summary>
    public int Level;
    /// <summary>Experience points earned at current level.</summary>
    public int CurrentXP;
    /// <summary>Experience needed to reach next level.</summary>
    public int XPToNextLevel;
}

/// <summary>
/// Inventory slot count.
/// </summary>
[Component(Serializable = true)]
public partial struct Inventory
{
    /// <summary>Total inventory slots.</summary>
    public int Slots;
    /// <summary>Currently used slots.</summary>
    public int UsedSlots;
}

/// <summary>
/// Gold currency.
/// </summary>
[Component(Serializable = true)]
public partial struct Gold
{
    /// <summary>Amount of gold.</summary>
    public int Amount;
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
/// Tag for NPC entities.
/// </summary>
[TagComponent]
public partial struct NPC;
