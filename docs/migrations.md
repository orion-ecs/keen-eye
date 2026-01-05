# Component Schema Migrations

This guide covers everything you need to know about managing component schema evolution in KeenEyes ECS, from basic versioning to advanced migration patterns.

## Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Version Management](#version-management)
- [Migration Patterns](#migration-patterns)
- [Default Value Injection](#default-value-injection)
- [Testing Migrations](#testing-migrations)
- [Batch Upgrader Tool](#batch-upgrader-tool)
- [CI/CD Integration](#cicd-integration)
- [Troubleshooting](#troubleshooting)
- [API Reference](#api-reference)

## Overview

Games evolve over time - components gain new fields, remove deprecated ones, or restructure data. Without schema evolution support, updating your game breaks existing save files and frustrates players.

KeenEyes provides a **versioned component migration system** that:

- **Preserves save compatibility** - Old saves load after component changes
- **Provides explicit control** - Define exactly how data transforms
- **Works with Native AOT** - No reflection, fully source-generated
- **Chains automatically** - Multi-version migrations handled seamlessly
- **Catches errors early** - Compile-time validation for migration issues

### How It Works

```
Save File (v1)          Component (v3)
┌─────────────┐         ┌─────────────┐
│ Health      │         │ Health      │
│   Current   │  ──→    │   Current   │
│   Max       │  v1→v2  │   Max       │
└─────────────┘  ──→    │   Shield    │  ← Added in v2
                 v2→v3  │   Armor     │  ← Added in v3
                        └─────────────┘
```

When loading a v1 save into a v3 game:
1. Deserialize the old JSON data
2. Run v1→v2 migration (adds `Shield = 0`)
3. Run v2→v3 migration (adds `Armor = 0`)
4. Deserialize the migrated data into the current `Health` struct

## Quick Start

### Step 1: Add Version to Your Component

```csharp
// Version 1: Original component
[Component(Serializable = true, Version = 1)]
public partial struct Health : IComponent
{
    public int Current;
    public int Max;
}
```

### Step 2: When You Add a Field, Bump the Version

```csharp
// Version 2: Added Shield field
[Component(Serializable = true, Version = 2)]
public partial struct Health : IComponent
{
    public int Current;
    public int Max;
    public int Shield;  // New field

    [MigrateFrom(1)]
    internal static Health MigrateFromV1(JsonElement oldData)
    {
        return new Health
        {
            Current = oldData.GetProperty("Current").GetInt32(),
            Max = oldData.GetProperty("Max").GetInt32(),
            Shield = 0  // Default for new field
        };
    }
}
```

### Step 3: Load Old Saves Automatically

```csharp
// Old v1 saves work automatically - migration runs during load
var snapshot = SnapshotManager.FromJson(File.ReadAllText("save.json"));
SnapshotManager.RestoreSnapshot(world, snapshot, ComponentSerializer.Instance);
// Player's Health now has Shield = 0
```

## Version Management

### When to Bump Version Numbers

| Change Type | Bump Version? | Migration Needed? |
|-------------|---------------|-------------------|
| Add new field | **Yes** | Yes (explicit or `[DefaultValue]`) |
| Remove field | **Yes** | Yes (explicit) |
| Rename field | **Yes** | Yes (explicit) |
| Change field type | **Yes** | Yes (explicit) |
| Restructure data | **Yes** | Yes (explicit) |
| Add non-serialized field | No | No |
| Change default value logic | No | No |
| Refactor internal code | No | No |

### Version Numbering Rules

1. **Start at 1** - All components default to version 1
2. **Increment by 1** - Always bump to the next integer (1 → 2 → 3)
3. **Never skip versions** - Creates gaps in migration chain (KEEN114 warning)
4. **Never decrease** - Breaks existing saves

```csharp
// ❌ BAD: Skipped versions
[Component(Serializable = true, Version = 5)]  // Where's v2, v3, v4?

// ✅ GOOD: Sequential versions
[Component(Serializable = true, Version = 2)]
```

### Non-Serializable Components

Components without `Serializable = true` don't need versioning:

```csharp
// No serialization = no migration needed
[Component]
public partial struct RuntimeState : IComponent
{
    public float ElapsedTime;
    public bool IsProcessing;
}
```

## Migration Patterns

### Pattern 1: Adding a Field

The most common migration - add a new field with a sensible default.

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Position : IComponent
{
    public float X;
    public float Y;
    public float Z;  // Added in v2

    [MigrateFrom(1)]
    private static Position MigrateFromV1(JsonElement oldData)
    {
        return new Position
        {
            X = oldData.GetProperty("X").GetSingle(),
            Y = oldData.GetProperty("Y").GetSingle(),
            Z = 0f  // New entities start at ground level
        };
    }
}
```

### Pattern 2: Removing a Field

When removing a field, you simply don't include it in the migration:

```csharp
// v1 had: Foo, Bar, Baz
// v2 removes: Baz

[Component(Serializable = true, Version = 2)]
public partial struct MyComponent : IComponent
{
    public int Foo;
    public int Bar;
    // Baz removed

    [MigrateFrom(1)]
    private static MyComponent MigrateFromV1(JsonElement oldData)
    {
        return new MyComponent
        {
            Foo = oldData.GetProperty("Foo").GetInt32(),
            Bar = oldData.GetProperty("Bar").GetInt32()
            // Baz is simply not copied - data is discarded
        };
    }
}
```

### Pattern 3: Renaming a Field

Map the old field name to the new field name:

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Character : IComponent
{
    public int Vitality;  // Was "Health" in v1

    [MigrateFrom(1)]
    private static Character MigrateFromV1(JsonElement oldData)
    {
        return new Character
        {
            Vitality = oldData.GetProperty("Health").GetInt32()  // Old name
        };
    }
}
```

### Pattern 4: Changing Field Types

Convert between types during migration:

```csharp
// v1: Used int for coordinates
// v2: Changed to float for precision

[Component(Serializable = true, Version = 2)]
public partial struct Position : IComponent
{
    public float X;  // Was int
    public float Y;  // Was int

    [MigrateFrom(1)]
    private static Position MigrateFromV1(JsonElement oldData)
    {
        return new Position
        {
            X = oldData.GetProperty("X").GetInt32(),  // int → float
            Y = oldData.GetProperty("Y").GetInt32()
        };
    }
}
```

### Pattern 5: Restructuring Data

Transform data from a flat structure to nested, or vice versa:

```csharp
// v1: Flat structure with X, Y
// v2: Nested structure with Vitality containing Current/Max

[Component(Serializable = true, Version = 2)]
public partial struct Character : IComponent
{
    public VitalityData Vitality;

    [MigrateFrom(1)]
    private static Character MigrateFromV1(JsonElement oldData)
    {
        return new Character
        {
            Vitality = new VitalityData
            {
                Current = oldData.GetProperty("Health").GetInt32(),
                Max = oldData.GetProperty("MaxHealth").GetInt32()
            }
        };
    }
}

public struct VitalityData
{
    public int Current;
    public int Max;
}
```

### Pattern 6: Computing Derived Values

Calculate new field values from old data:

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Stats : IComponent
{
    public int Strength;
    public int Agility;
    public int Intelligence;
    public int TotalPower;  // New: computed field

    [MigrateFrom(1)]
    private static Stats MigrateFromV1(JsonElement oldData)
    {
        var str = oldData.GetProperty("Strength").GetInt32();
        var agi = oldData.GetProperty("Agility").GetInt32();
        var intel = oldData.GetProperty("Intelligence").GetInt32();

        return new Stats
        {
            Strength = str,
            Agility = agi,
            Intelligence = intel,
            TotalPower = str + agi + intel  // Computed during migration
        };
    }
}
```

### Pattern 7: Multi-Version Migration Chain

When a component evolves through multiple versions:

```csharp
[Component(Serializable = true, Version = 4)]
public partial struct Health : IComponent
{
    public int Current;
    public int Max;
    public int Shield;      // Added in v2
    public int Armor;       // Added in v3
    public float Regen;     // Added in v4

    [MigrateFrom(1)]
    private static Health MigrateFromV1(JsonElement oldData)
    {
        return new Health
        {
            Current = oldData.GetProperty("Current").GetInt32(),
            Max = oldData.GetProperty("Max").GetInt32(),
            Shield = 0,
            Armor = 0,
            Regen = 0f
        };
    }

    [MigrateFrom(2)]
    private static Health MigrateFromV2(JsonElement oldData)
    {
        return new Health
        {
            Current = oldData.GetProperty("Current").GetInt32(),
            Max = oldData.GetProperty("Max").GetInt32(),
            Shield = oldData.GetProperty("Shield").GetInt32(),
            Armor = 0,
            Regen = 0f
        };
    }

    [MigrateFrom(3)]
    private static Health MigrateFromV3(JsonElement oldData)
    {
        return new Health
        {
            Current = oldData.GetProperty("Current").GetInt32(),
            Max = oldData.GetProperty("Max").GetInt32(),
            Shield = oldData.GetProperty("Shield").GetInt32(),
            Armor = oldData.GetProperty("Armor").GetInt32(),
            Regen = 0f
        };
    }
}
```

**Automatic Chaining:** Loading a v1 save into this v4 component runs:
`v1 → v2 → v3 → v4` automatically.

### Pattern 8: Conditional Migration

Apply different logic based on old data values:

```csharp
[MigrateFrom(1)]
private static Equipment MigrateFromV1(JsonElement oldData)
{
    var weaponType = oldData.GetProperty("WeaponType").GetString();

    // Different defaults based on weapon type
    var baseDamage = weaponType switch
    {
        "Sword" => 10,
        "Bow" => 8,
        "Staff" => 5,
        _ => 1
    };

    return new Equipment
    {
        WeaponType = weaponType ?? "Unknown",
        BaseDamage = baseDamage,
        EnchantmentLevel = 0  // New field
    };
}
```

## Default Value Injection

For simple additions (new fields with constant defaults), use `[DefaultValue]` to auto-generate migrations:

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Health : IComponent
{
    public int Current;
    public int Max;

    [DefaultValue(0)]
    public int Shield;  // Auto-migrated with Shield = 0
}
```

The source generator creates the migration automatically - no explicit `[MigrateFrom]` needed.

### When to Use `[DefaultValue]` vs Explicit Migration

| Scenario | Use `[DefaultValue]` | Use `[MigrateFrom]` |
|----------|---------------------|---------------------|
| Add field with constant default | ✅ | Overkill |
| Add field computed from old data | ❌ | ✅ |
| Rename a field | ❌ | ✅ |
| Change field type | ❌ | ✅ |
| Restructure data | ❌ | ✅ |
| Remove a field | ❌ | ✅ |

### Combining `[DefaultValue]` and `[MigrateFrom]`

You can use both - `[DefaultValue]` handles simple additions while explicit migrations handle complex transformations:

```csharp
[Component(Serializable = true, Version = 3)]
public partial struct Character : IComponent
{
    public string Name;
    public int Level;

    [DefaultValue(100)]
    public int MaxHealth;  // v2: auto-migrated

    public int CurrentHealth;

    // v2→v3: Complex migration (CurrentHealth was "HP" in v2)
    [MigrateFrom(2)]
    private static Character MigrateFromV2(JsonElement oldData)
    {
        return new Character
        {
            Name = oldData.GetProperty("Name").GetString() ?? "",
            Level = oldData.GetProperty("Level").GetInt32(),
            MaxHealth = oldData.GetProperty("MaxHealth").GetInt32(),
            CurrentHealth = oldData.GetProperty("HP").GetInt32()  // Renamed
        };
    }
}
```

## Testing Migrations

### Unit Testing Migration Functions

Test each migration function in isolation:

```csharp
public class HealthMigrationTests
{
    [Fact]
    public void MigrateFromV1_AddsDefaultShield()
    {
        // Arrange: Create v1 JSON data
        var v1Data = JsonSerializer.SerializeToElement(new
        {
            Current = 50,
            Max = 100
        });

        // Act: Run migration
        var result = InvokeMigration<Health>(1, v1Data);

        // Assert: Shield has default value
        Assert.Equal(50, result.Current);
        Assert.Equal(100, result.Max);
        Assert.Equal(0, result.Shield);
    }

    [Fact]
    public void MigrateFromV2_PreservesAllFields()
    {
        var v2Data = JsonSerializer.SerializeToElement(new
        {
            Current = 75,
            Max = 100,
            Shield = 25
        });

        var result = InvokeMigration<Health>(2, v2Data);

        Assert.Equal(75, result.Current);
        Assert.Equal(100, result.Max);
        Assert.Equal(25, result.Shield);
        Assert.Equal(0, result.Armor);  // New default
    }

    private static T InvokeMigration<T>(int fromVersion, JsonElement data)
        where T : struct, IComponent
    {
        var migrator = ComponentSerializer.Instance as IComponentMigrator;
        var currentVersion = ComponentMigrationMetadata.GetVersion<T>();
        var migrated = migrator!.Migrate(typeof(T), data, fromVersion, currentVersion);
        return JsonSerializer.Deserialize<T>(migrated!.Value.GetRawText());
    }
}
```

### Integration Testing with Snapshots

Test full save/load cycles with old data:

```csharp
[Fact]
public void LoadV1Save_MigratesToCurrentVersion()
{
    // Load pre-made v1 save file
    var v1Json = File.ReadAllText("TestData/save_v1.json");
    var snapshot = SnapshotManager.FromJson(v1Json);

    using var world = new World();
    SnapshotManager.RestoreSnapshot(world, snapshot!, ComponentSerializer.Instance);

    // Verify migrated data
    var player = world.GetEntityByName("Player");
    var health = world.Get<Health>(player);

    Assert.Equal(100, health.Current);
    Assert.Equal(100, health.Max);
    Assert.Equal(0, health.Shield);  // Default from migration
}
```

### Creating Test Save Files

Create saves at each version for regression testing:

```bash
# Create saves at each version
samples/SchemaMigration/
  TestData/
    save_v1.json     # Health with Current, Max only
    save_v2.json     # Health with Current, Max, Shield
    save_v3.json     # Current version
```

See the `samples/SchemaMigration` project for pre-made test files.

## Batch Upgrader Tool

Upgrade save files in bulk using the CLI tool:

### Preview Mode (Dry Run)

See what would be migrated without making changes:

```bash
dotnet keeneyes migrate --path ./saves/ --dry-run
```

Output:
```
Analyzing save files...

save1.json:
  Health v1 → v3 (12 entities)
  Inventory v2 → v3 (5 entities)

save2.json:
  Health v1 → v3 (8 entities)

Summary:
  2 files need migration
  25 component instances to upgrade
  No errors detected
```

### Apply Migrations

Upgrade files with automatic backup:

```bash
dotnet keeneyes migrate --path ./saves/ --backup
```

This creates `.backup` files before modifying originals.

### Options

| Flag | Description |
|------|-------------|
| `--path <dir>` | Directory containing save files |
| `--pattern <glob>` | File pattern (default: `*.json`) |
| `--dry-run` | Preview only, don't modify files |
| `--backup` | Create `.backup` files before upgrading |
| `--recursive` | Search subdirectories |
| `--verbose` | Show detailed migration steps |

## CI/CD Integration

### Pre-Commit Validation

Add migration gap detection to your build:

```yaml
# .github/workflows/build.yml
- name: Check Migration Gaps
  run: |
    dotnet build 2>&1 | grep -E "KEEN114|KEEN115" && exit 1 || exit 0
```

### Migration Test Matrix

Test migrations from all historical versions:

```yaml
strategy:
  matrix:
    save_version: [1, 2, 3]

steps:
  - name: Test Migration from v${{ matrix.save_version }}
    run: |
      dotnet test --filter "Category=Migration&Version=${{ matrix.save_version }}"
```

### Version Tracking

Track component versions in your build output:

```csharp
// Build-time: Generate version manifest
// Generated by source generator
public static class ComponentVersionManifest
{
    public static IReadOnlyDictionary<string, int> Versions { get; } = new Dictionary<string, int>
    {
        ["Health"] = 3,
        ["Inventory"] = 2,
        ["Position"] = 1,
        // ...
    };
}
```

### Upgrade Path Documentation

Document supported upgrade paths:

```markdown
## Supported Save Versions

| Component | Min Version | Current | Notes |
|-----------|-------------|---------|-------|
| Health | 1 | 3 | Full migration chain |
| Inventory | 2 | 3 | v1 no longer supported |
| Position | 1 | 1 | No migrations |
```

## Troubleshooting

### Common Errors

#### KEEN110: Migration method must be static

```csharp
// ❌ BAD: Instance method
[MigrateFrom(1)]
private Health MigrateFromV1(JsonElement data) { ... }

// ✅ GOOD: Static method
[MigrateFrom(1)]
private static Health MigrateFromV1(JsonElement data) { ... }
```

#### KEEN111: Migration method must return component type

```csharp
// ❌ BAD: Wrong return type
[MigrateFrom(1)]
private static object MigrateFromV1(JsonElement data) { ... }

// ✅ GOOD: Returns containing type
[MigrateFrom(1)]
private static Health MigrateFromV1(JsonElement data) { ... }
```

#### KEEN112: Migration method must take JsonElement parameter

```csharp
// ❌ BAD: Wrong parameter type
[MigrateFrom(1)]
private static Health MigrateFromV1(string data) { ... }

// ✅ GOOD: Takes JsonElement
[MigrateFrom(1)]
private static Health MigrateFromV1(JsonElement data) { ... }
```

#### KEEN113: Migration version must be less than component version

```csharp
// ❌ BAD: MigrateFrom(2) but component is Version = 2
[Component(Serializable = true, Version = 2)]
public partial struct Health
{
    [MigrateFrom(2)]  // Error: 2 >= 2
    private static Health MigrateFromV2(JsonElement data) { ... }
}

// ✅ GOOD: MigrateFrom version < component version
[Component(Serializable = true, Version = 3)]
public partial struct Health
{
    [MigrateFrom(2)]  // OK: 2 < 3
    private static Health MigrateFromV2(JsonElement data) { ... }
}
```

#### KEEN114: Missing migration for version (Warning)

```csharp
// ⚠️ WARNING: Gap in migration chain
[Component(Serializable = true, Version = 3)]
public partial struct Health
{
    [MigrateFrom(1)]  // v1 → v2 defined
    // Missing [MigrateFrom(2)]  // v2 → v3 not defined!
}

// ✅ GOOD: Complete chain
[Component(Serializable = true, Version = 3)]
public partial struct Health
{
    [MigrateFrom(1)]
    private static Health MigrateFromV1(JsonElement data) { ... }

    [MigrateFrom(2)]
    private static Health MigrateFromV2(JsonElement data) { ... }
}
```

#### KEEN115: Duplicate migration for version

```csharp
// ❌ BAD: Two migrations for same version
[MigrateFrom(1)]
private static Health MigrateFromV1A(JsonElement data) { ... }

[MigrateFrom(1)]  // Error: duplicate
private static Health MigrateFromV1B(JsonElement data) { ... }
```

#### KEEN116: Migration method must be in a component type

```csharp
// ❌ BAD: Migration in non-component type
public static class Migrations
{
    [MigrateFrom(1)]  // Error: not in [Component] type
    public static Health MigrateHealth(JsonElement data) { ... }
}

// ✅ GOOD: Migration inside component
[Component(Serializable = true, Version = 2)]
public partial struct Health
{
    [MigrateFrom(1)]
    private static Health MigrateFromV1(JsonElement data) { ... }
}
```

#### KEEN117: Component must be serializable to use migrations

```csharp
// ❌ BAD: Not serializable
[Component(Version = 2)]  // Missing Serializable = true
public partial struct Health
{
    [MigrateFrom(1)]  // Error: no serialization = no migration
    private static Health MigrateFromV1(JsonElement data) { ... }
}

// ✅ GOOD: Serializable component
[Component(Serializable = true, Version = 2)]
public partial struct Health
{
    [MigrateFrom(1)]
    private static Health MigrateFromV1(JsonElement data) { ... }
}
```

#### KEEN118: DefaultValue type mismatch

```csharp
// ❌ BAD: Type mismatch
[DefaultValue("hello")]  // String, but field is int
public int Count;

// ✅ GOOD: Matching types
[DefaultValue(0)]
public int Count;

[DefaultValue("")]
public string Name;
```

### Runtime Exceptions

#### ComponentVersionException: Future Version

```
Cannot load Health v5 (current version is v3).
The save file was created with a newer version of the application.
Update the application to load this save file.
```

**Cause:** Save file has newer component version than current code.

**Solution:** You cannot downgrade. Either:
- Update to the newer version of your game
- Use the save file with the version that created it

#### ComponentVersionException: Missing Migration Path

```
Cannot migrate Health from v1 to v3. No migration path is available.
```

**Cause:** Gap in migration chain (missing v2 migration).

**Solution:** Add the missing migration or use `[DefaultValue]` for simple additions.

### Debugging Migrations

Use `IMigrationDiagnostics` for detailed debugging:

```csharp
var diagnostics = ComponentSerializer.Instance as IMigrationDiagnostics;

// Get migration graph for a component
var graph = diagnostics!.GetMigrationGraph(typeof(Health));
Console.WriteLine(graph?.ToDiagnosticString());

// Output:
// Migration Graph for Health
// Current Version: 3
// Edges: 2
//
// Migrations:
//   v1 → v2
//   v2 → v3

// Find all migration gaps
var gaps = diagnostics.FindAllMigrationGaps();
foreach (var (component, versions) in gaps)
{
    Console.WriteLine($"{component} missing: {string.Join(", ", versions)}");
}

// Generate full diagnostic report
Console.WriteLine(diagnostics.GenerateDiagnosticReport());
```

## API Reference

### Attributes

#### `[Component]`

Marks a struct as an ECS component.

```csharp
[Component(Serializable = true, Version = 2)]
public partial struct Health : IComponent { }
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Serializable` | `bool` | `false` | Enable serialization support |
| `Version` | `int` | `1` | Current schema version |

#### `[MigrateFrom]`

Marks a static method as a migration handler.

```csharp
[MigrateFrom(1)]
internal static Health MigrateFromV1(JsonElement data) { ... }
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `fromVersion` | `int` | Source version (must be ≥ 1 and < component version) |

**Method Requirements:**
- Must be `static`
- Must be `internal` or `public` (the generated serializer needs to access the method)
- Must return the containing component type
- Must take a single `System.Text.Json.JsonElement` parameter

#### `[DefaultValue]`

Specifies a default value for new fields during auto-migration.

```csharp
[DefaultValue(100)]
public int MaxHealth;
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `value` | `object?` | Default value (must be compatible with field type) |

### Interfaces

#### `IComponentMigrator`

Core interface for component migration.

```csharp
public interface IComponentMigrator
{
    bool CanMigrate(Type type, int fromVersion, int toVersion);
    JsonElement? Migrate(Type type, JsonElement data, int fromVersion, int toVersion);
    IEnumerable<int> GetMigrationVersions(Type type);
}
```

#### `IMigrationDiagnostics`

Extended interface with diagnostic capabilities.

```csharp
public interface IMigrationDiagnostics : IComponentMigrator
{
    MigrationResult MigrateWithDiagnostics(Type type, JsonElement data, int from, int to);
    MigrationGraph? GetMigrationGraph(Type type);
    IEnumerable<string> GetComponentsWithMigrations();
    IReadOnlyDictionary<string, IReadOnlyList<int>> FindAllMigrationGaps();
    string GenerateDiagnosticReport();
}
```

### Classes

#### `MigrationGraph`

Represents the migration paths for a component.

```csharp
var graph = new MigrationGraph("Health", currentVersion: 3);
graph.AddEdge(1, 2);
graph.AddEdge(2, 3);

graph.HasPath(1, 3);  // true
graph.GetMigrationChain(1, 3);  // [(1,2), (2,3)]
graph.FindGaps();  // []
graph.ToDiagnosticString();  // Human-readable output
```

#### `MigrationResult`

Encapsulates migration execution results.

```csharp
var result = migrator.MigrateWithDiagnostics(typeof(Health), data, 1, 3);

if (result.Success)
{
    Console.WriteLine($"Migrated in {result.TotalElapsed.TotalMilliseconds}ms");
    foreach (var step in result.StepTimings)
    {
        Console.WriteLine($"  {step.Step}: {step.Elapsed.TotalMilliseconds}ms");
    }
}
else
{
    Console.WriteLine($"Failed at v{result.FailedAtVersion}: {result.ErrorMessage}");
}
```

#### `ComponentVersionException`

Thrown when version mismatch prevents loading.

```csharp
catch (ComponentVersionException ex)
{
    Console.WriteLine($"Component: {ex.ComponentName}");
    Console.WriteLine($"Saved: v{ex.SerializedVersion}");
    Console.WriteLine($"Current: v{ex.CurrentVersion}");
}
```

### Generated Classes

#### `ComponentMigrationMetadata`

Provides version lookup for components.

```csharp
// Generic (compile-time optimized)
int version = ComponentMigrationMetadata.GetVersion<Health>();

// Runtime type lookup
int version = ComponentMigrationMetadata.GetVersion(typeof(Health));

// Type name lookup
int version = ComponentMigrationMetadata.GetVersion("MyGame.Health");

// Get all versions
var allVersions = ComponentMigrationMetadata.GetAllVersions();
```

---

## Related Documentation

- [Serialization Guide](serialization.md) - World snapshots and save/load
- [ADR-015: Component Schema Migrations](adr/015-component-schema-migrations.md) - Design decision record
- [Sample: SchemaMigration](../samples/SchemaMigration/) - Working example with v1→v2→v3 evolution
