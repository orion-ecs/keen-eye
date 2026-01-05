# Schema Migration Sample

This sample demonstrates component schema evolution in KeenEyes ECS, showing how to:

- Define versioned components with `[Component(Version = N)]`
- Create migration methods with `[MigrateFrom(version)]`
- Use `[DefaultValue]` for auto-generated migrations
- Load save files from older versions automatically

## Components Demonstrated

### PlayerStats (v1 → v2 → v3)

| Version | Fields |
|---------|--------|
| v1 | Health, MaxHealth |
| v2 | + Shield |
| v3 | + Armor, MagicResist |

Uses explicit `[MigrateFrom]` migrations for each version step.

### Equipment (v1 → v2 → v3)

| Version | Fields | Notes |
|---------|--------|-------|
| v1 | WeaponType, Damage | |
| v2 | + Durability, IsMagical | |
| v3 | + BonusDamage, EnchantmentLevel | Computed value from IsMagical |

Demonstrates field additions and computed values during migration.

### Currency (v1 → v2 → v3)

| Version | Fields |
|---------|--------|
| v1 | Gold |
| v2 | + Silver |
| v3 | + Gems |

Uses `[DefaultValue(0)]` for automatic migrations (no explicit methods needed).

## Running the Sample

```bash
dotnet run --project samples/KeenEyes.Sample.SchemaMigration
```

## Test Data

The `TestData/` folder contains pre-made save files at each version:

- `save_v1.json` - Original save with v1 components
- `save_v2.json` - Save after first schema update
- `save_v3.json` - Current version save

These files are useful for:
- Testing migration paths
- Regression testing after schema changes
- Integration testing with CI/CD

## Key Concepts

### Explicit Migrations

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct PlayerStats
{
    public int Health;
    public int MaxHealth;
    public int Shield;  // New in v2

    [MigrateFrom(1)]
    internal static PlayerStats MigrateFromV1(JsonElement oldData)
    {
        return new PlayerStats
        {
            Health = oldData.GetProperty("health").GetInt32(),
            MaxHealth = oldData.GetProperty("maxHealth").GetInt32(),
            Shield = 0  // Default for new field
        };
    }
}
```

### Auto-Migrations with DefaultValue

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Currency
{
    public int Gold;

    [DefaultValue(0)]
    public int Silver;  // Auto-migrated with Silver = 0
}
```

### Computed Values

```csharp
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
        // Computed: magical weapons get enchantment level 1
        EnchantmentLevel = isMagical ? 1 : 0
    };
}
```

## Related Documentation

- [Migration Guide](../../docs/migrations.md) - Comprehensive documentation
- [ADR-015](../../docs/adr/015-component-schema-migrations.md) - Design decision record
