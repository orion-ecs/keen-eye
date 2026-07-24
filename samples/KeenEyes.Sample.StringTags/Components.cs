namespace KeenEyes.Sample.StringTags;

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
/// Loot drop table.
/// </summary>
[Component]
public partial struct LootTable
{
    /// <summary>Loot table asset identifier.</summary>
    public string TableId;
    /// <summary>Probability of dropping loot (0-1).</summary>
    public float DropChance;
}

/// <summary>
/// Type-safe tag for enemy entities (for comparison with string tags).
/// </summary>
[TagComponent]
public partial struct Enemy;
