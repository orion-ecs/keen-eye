using System.Text.Json;
using KeenEyes;

namespace KeenEyes.Sample.SchemaMigration;

// =============================================================================
// COMPONENT SCHEMA MIGRATION DEMO
// =============================================================================
//
// This file demonstrates the evolution of component schemas across versions:
//
// PlayerStats:
//   v1: Health, MaxHealth
//   v2: Health, MaxHealth, Shield (new field)
//   v3: Health, MaxHealth, Shield, Armor, MagicResist (new fields)
//
// Equipment:
//   v1: WeaponType (string), Damage (int)
//   v2: WeaponType (string), Damage (int), Durability (new), IsMagical (new)
//   v3: WeaponType (string), BaseDamage (renamed), Durability, IsMagical,
//       BonusDamage (new), EnchantmentLevel (new)
//
// Currency:
//   v1: Gold
//   v2: Gold, Silver (using [DefaultValue] for auto-migration)
//   v3: Gold, Silver, Gems (using [DefaultValue] for auto-migration)
//
// =============================================================================

/// <summary>
/// Player statistics component demonstrating v1 → v2 → v3 evolution.
/// </summary>
/// <remarks>
/// Version history:
/// <list type="bullet">
/// <item>v1: Health and MaxHealth only</item>
/// <item>v2: Added Shield field</item>
/// <item>v3: Added Armor and MagicResist fields</item>
/// </list>
/// </remarks>
[Component(Serializable = true, Version = 3)]
public partial struct PlayerStats
{
    /// <summary>Current health points.</summary>
    public int Health;

    /// <summary>Maximum health points.</summary>
    public int MaxHealth;

    /// <summary>Current shield points (absorbs damage before health). Added in v2.</summary>
    public int Shield;

    /// <summary>Physical damage reduction. Added in v3.</summary>
    public int Armor;

    /// <summary>Magic damage reduction. Added in v3.</summary>
    public int MagicResist;

    /// <summary>
    /// Migrates from v1 (Health, MaxHealth) to v2 (adds Shield).
    /// </summary>
    [MigrateFrom(1)]
    internal static PlayerStats MigrateFromV1(JsonElement oldData)
    {
        return new PlayerStats
        {
            Health = oldData.GetProperty("health").GetInt32(),
            MaxHealth = oldData.GetProperty("maxHealth").GetInt32(),
            Shield = 0,        // New field - starts at 0
            Armor = 0,         // Also new (from v3)
            MagicResist = 0    // Also new (from v3)
        };
    }

    /// <summary>
    /// Migrates from v2 (Health, MaxHealth, Shield) to v3 (adds Armor, MagicResist).
    /// </summary>
    [MigrateFrom(2)]
    internal static PlayerStats MigrateFromV2(JsonElement oldData)
    {
        return new PlayerStats
        {
            Health = oldData.GetProperty("health").GetInt32(),
            MaxHealth = oldData.GetProperty("maxHealth").GetInt32(),
            Shield = oldData.GetProperty("shield").GetInt32(),
            Armor = 0,         // New field - starts at 0
            MagicResist = 0    // New field - starts at 0
        };
    }
}

/// <summary>
/// Equipment component demonstrating field additions and computed values.
/// </summary>
/// <remarks>
/// Version history:
/// <list type="bullet">
/// <item>v1: WeaponType, Damage</item>
/// <item>v2: Added Durability, IsMagical</item>
/// <item>v3: Added BonusDamage, EnchantmentLevel (computed from IsMagical)</item>
/// </list>
/// </remarks>
[Component(Serializable = true, Version = 3)]
public partial struct Equipment
{
    /// <summary>Type of weapon (Sword, Bow, Staff, etc.).</summary>
    public string WeaponType;

    /// <summary>Base damage value.</summary>
    public int Damage;

    /// <summary>Current durability percentage. Added in v2.</summary>
    public int Durability;

    /// <summary>Whether the weapon has magical properties. Added in v2.</summary>
    public bool IsMagical;

    /// <summary>Bonus damage from enchantments. Added in v3.</summary>
    public int BonusDamage;

    /// <summary>Enchantment level (0-5). Added in v3.</summary>
    public int EnchantmentLevel;

    /// <summary>
    /// Migrates from v1 (WeaponType, Damage) to current version.
    /// </summary>
    [MigrateFrom(1)]
    internal static Equipment MigrateFromV1(JsonElement oldData)
    {
        return new Equipment
        {
            WeaponType = oldData.GetProperty("weaponType").GetString() ?? "Unknown",
            Damage = oldData.GetProperty("damage").GetInt32(),
            Durability = 100,  // New weapons start at full durability
            IsMagical = false,
            BonusDamage = 0,
            EnchantmentLevel = 0
        };
    }

    /// <summary>
    /// Migrates from v2 to v3 (adds BonusDamage, EnchantmentLevel).
    /// </summary>
    /// <remarks>
    /// If the weapon is magical, we grant a starting enchantment level.
    /// </remarks>
    [MigrateFrom(2)]
    internal static Equipment MigrateFromV2(JsonElement oldData)
    {
        var isMagical = oldData.GetProperty("isMagical").GetBoolean();

        return new Equipment
        {
            WeaponType = oldData.GetProperty("weaponType").GetString() ?? "Unknown",
            Damage = oldData.GetProperty("damage").GetInt32(),
            Durability = oldData.GetProperty("durability").GetInt32(),
            IsMagical = isMagical,
            BonusDamage = 0,
            // Magical weapons from v2 get a starting enchantment level
            EnchantmentLevel = isMagical ? 1 : 0
        };
    }
}

/// <summary>
/// Currency component demonstrating [DefaultValue] auto-migration.
/// </summary>
/// <remarks>
/// This component uses [DefaultValue] attributes instead of explicit [MigrateFrom]
/// methods. The source generator automatically creates migrations for simple field additions.
///
/// Version history:
/// <list type="bullet">
/// <item>v1: Gold only</item>
/// <item>v2: Added Silver with [DefaultValue(0)]</item>
/// <item>v3: Added Gems with [DefaultValue(0)]</item>
/// </list>
/// </remarks>
[Component(Serializable = true, Version = 3)]
public partial struct Currency
{
    /// <summary>Gold coins.</summary>
    public int Gold;

    /// <summary>Silver coins. Added in v2.</summary>
    [DefaultValue(0)]
    public int Silver;

    /// <summary>Precious gems. Added in v3.</summary>
    [DefaultValue(0)]
    public int Gems;
}

/// <summary>
/// Position component (no versioning needed - unchanged since v1).
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
/// Tag component marking player entities.
/// </summary>
[TagComponent]
public partial struct Player;
