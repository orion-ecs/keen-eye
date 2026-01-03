# ADR-015: Component Schema Migrations

**Status:** Proposed
**Date:** 2026-01-03
**Issue:** [#352](https://github.com/orion-ecs/keen-eye/issues/352)

## Context

Production games evolve over time - components gain new fields, remove deprecated ones, or restructure data. Without schema evolution support:

- **Save file breakage:** Old saves can't load after component changes
- **Manual migration:** Developers must write custom conversion code for each change
- **Deployment friction:** Updates become risky, requiring data wipes
- **Testing burden:** QA must retest from scratch after schema changes

The serialization system (Phase 11) provides the foundation for persisting world state, but has no mechanism for handling version mismatches between saved data and current component definitions.

### Current State

Components are serialized with their current structure:

```csharp
[Component]
public partial struct Health
{
    public int Current;
    public int Max;
}
```

If this component changes (e.g., adding `Shield` field), existing save files become incompatible:
- Binary deserialization fails (size mismatch)
- JSON deserialization silently drops/ignores fields
- No way to transform old data to new format

### Requirements

1. **Version tracking** - Know what version of a component was serialized
2. **Migration functions** - Transform old data to new format
3. **Automatic pipeline** - Migrations run transparently during load
4. **AOT compatibility** - No reflection, works with Native AOT
5. **Tooling** - Batch upgrader for existing save files

## Decision

Implement a **versioned component migration system** with three layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Source Generators                         │
│  - Generate version metadata                                 │
│  - Generate migration delegates                              │
│  - Generate compatibility checks                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│              Migration Pipeline (Runtime)                    │
│  - Version detection during deserialization                  │
│  - Migration chain execution                                 │
│  - Validation and error handling                             │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                   Tooling (CLI)                              │
│  - Batch save file upgrader                                  │
│  - Dry-run mode                                              │
│  - Migration reports                                         │
└─────────────────────────────────────────────────────────────┘
```

### Component Versioning

Components declare their version via attribute:

```csharp
[Component(Version = 1)]
public partial struct Health
{
    public int Current;
    public int Max;
}
```

Version defaults to `1` if not specified. Source generators produce metadata:

```csharp
// Generated
public static partial class ComponentMigrationMetadata
{
    public static int GetVersion<T>() where T : struct, IComponent;
    public static int GetVersion(Type componentType);
}
```

### Migration Functions

Migration functions transform data from one version to the next:

```csharp
[Component(Version = 2)]
[MigrateFrom(1, nameof(MigrateFromV1))]
public partial struct Health
{
    public int Current;
    public int Max;
    public int Shield;

    private static Health MigrateFromV1(HealthV1 old)
    {
        return new Health
        {
            Current = old.Current,
            Max = old.Max,
            Shield = 0  // Default for new field
        };
    }
}

// Previous version preserved for migration
[Obsolete("Use Health instead")]
public readonly struct HealthV1
{
    public readonly int Current;
    public readonly int Max;
}
```

### Migration Chain Resolution

The source generator builds a migration graph:

```csharp
// Generated
public static partial class ComponentMigrationRegistry
{
    private static readonly Dictionary<(Type, int, int), Delegate> migrations = new()
    {
        [(typeof(Health), 1, 2)] = (Func<HealthV1, Health>)Health.MigrateFromV1,
        [(typeof(Health), 2, 3)] = (Func<HealthV2, Health>)Health.MigrateFromV2,
    };

    public static T Migrate<T>(object source, int fromVersion) where T : struct, IComponent
    {
        var currentVersion = GetVersion<T>();
        var current = source;

        // Chain migrations: v1 → v2 → v3
        for (int v = fromVersion; v < currentVersion; v++)
        {
            var migration = migrations[(typeof(T), v, v + 1)];
            current = migration.DynamicInvoke(current);
        }

        return (T)current;
    }
}
```

### Serialization Integration

Version metadata is stored in serialized data:

**Binary format:**
```
[ComponentTypeId: int32]
[Version: int16]          // NEW: version tag
[DataLength: int32]
[ComponentData: bytes]
```

**JSON format:**
```json
{
  "components": {
    "Health": {
      "$version": 2,
      "Current": 100,
      "Max": 100,
      "Shield": 50
    }
  }
}
```

Deserializers check version before instantiation:

```csharp
public T DeserializeComponent<T>(BinaryReader reader) where T : struct, IComponent
{
    var serializedVersion = reader.ReadInt16();
    var currentVersion = ComponentMigrationMetadata.GetVersion<T>();

    if (serializedVersion == currentVersion)
    {
        // Fast path: direct deserialization
        return DeserializeDirect<T>(reader);
    }
    else if (serializedVersion < currentVersion)
    {
        // Migration path: deserialize old version, then migrate
        var oldData = DeserializeVersioned(typeof(T), serializedVersion, reader);
        return ComponentMigrationRegistry.Migrate<T>(oldData, serializedVersion);
    }
    else
    {
        // Future version: cannot downgrade
        throw new ComponentVersionException(
            $"Cannot load {typeof(T).Name} v{serializedVersion} " +
            $"(current version is v{currentVersion}). " +
            "Update the game to load this save file.");
    }
}
```

### Default Value Injection

For simple additions (new fields with defaults), explicit migration functions can be skipped:

```csharp
[Component(Version = 2)]
public partial struct Health
{
    public int Current;
    public int Max;

    [DefaultValue(0)]  // Auto-migrate from v1 with this default
    public int Shield;
}
```

The source generator produces an automatic migration:

```csharp
// Generated
private static Health AutoMigrateFromV1(HealthV1 old)
{
    return new Health
    {
        Current = old.Current,
        Max = old.Max,
        Shield = 0  // From [DefaultValue]
    };
}
```

### Batch Upgrader Tool

CLI tool for upgrading save files:

```bash
# Preview migrations
dotnet keeneyes migrate --path ./saves/ --dry-run

# Output:
# save1.dat:
#   Health v1 → v3 (12 entities)
#   Inventory v2 → v3 (5 entities)
# save2.dat:
#   Health v1 → v3 (8 entities)

# Apply migrations
dotnet keeneyes migrate --path ./saves/ --backup

# Creates backup: ./saves/save1.dat.backup
# Upgrades: ./saves/save1.dat
```

## Alternatives Considered

### Option 1: Type Name Versioning

Use distinct type names for each version (`Health`, `HealthV2`, `HealthV3`).

```csharp
public struct Health { ... }      // v1
public struct HealthV2 { ... }    // v2
public struct HealthV3 { ... }    // v3
```

**Rejected because:**
- Queries must change when versions change (`Query<HealthV3>()`)
- No single "current version" type
- Migration code scattered across type definitions
- Poor ergonomics for users

### Option 2: Schema-Based Migration (like EF Migrations)

Generate migration files for each schema change:

```
Migrations/
  20260101_AddShieldToHealth.cs
  20260115_RestructureHealth.cs
```

**Rejected because:**
- Overkill for component changes (ECS components are simpler than DB schemas)
- Requires migration file management
- More complex tooling
- Doesn't fit ECS mental model

### Option 3: Automatic Field Mapping

Automatically map fields by name, ignore missing/extra fields:

```csharp
// Old: { Current, Max }
// New: { Current, Max, Shield }
// Auto-map Current→Current, Max→Max, Shield=default
```

**Rejected because:**
- Breaks on field renames
- No control over complex transformations
- Silent data loss on removed fields
- Insufficient for restructuring

### Option 4: External Migration Scripts

Define migrations in separate config/script files:

```yaml
migrations:
  Health:
    1-to-2:
      add: { Shield: 0 }
    2-to-3:
      rename: { Current: Vitality.Current, Max: Vitality.Max }
```

**Rejected because:**
- Separate language to learn
- Limited expressiveness
- No type safety
- Harder to test

## Consequences

### Positive

1. **Save compatibility** - Games can evolve components without breaking saves
2. **Explicit migrations** - Developers control exactly how data transforms
3. **AOT compatible** - Source-generated delegates, no reflection
4. **Tooling support** - Batch upgrader for existing save files
5. **Gradual adoption** - Version defaults to 1, migration optional

### Negative

1. **Version tracking overhead** - 2 bytes per component in binary format
2. **Old version types** - Must keep `HealthV1`, `HealthV2` for migrations
3. **Migration complexity** - Multi-step migrations can be hard to reason about
4. **Testing burden** - Each migration path needs testing

### Risks

1. **Circular dependencies** - Migration A→B→C→A (detected by generator)
2. **Missing migrations** - Version gap with no handler (runtime error)
3. **Cross-component migrations** - Component A needs data from Component B (not supported initially)
4. **Performance during load** - Many migrations on large saves

## Implementation Phases

### Phase 1: Version Infrastructure ([#697](https://github.com/orion-ecs/keen-eye/issues/697))
- `[Component(Version = n)]` attribute support
- Version metadata in serialization format
- `ComponentMigrationMetadata` source generator
- Version mismatch detection (throw, don't migrate yet)

### Phase 2: Migration Pipeline ([#698](https://github.com/orion-ecs/keen-eye/issues/698))
- `[MigrateFrom]` attribute
- `ComponentMigrationRegistry` source generator
- Single-step migration execution
- Integration with `IComponentSerializer`

### Phase 3: Migration Chaining ([#699](https://github.com/orion-ecs/keen-eye/issues/699))
- Multi-step migration (v1 → v2 → v3)
- Migration graph validation
- Cycle detection
- Gap detection

### Phase 4: Default Value Injection ([#700](https://github.com/orion-ecs/keen-eye/issues/700))
- `[DefaultValue]` attribute for new fields
- Auto-generated migrations for simple additions
- Combine with explicit migrations

### Phase 5: Batch Upgrader Tool ([#701](https://github.com/orion-ecs/keen-eye/issues/701))
- `dotnet keeneyes migrate` command
- Dry-run mode
- Backup creation
- Progress reporting

### Phase 6: Documentation and Samples ([#702](https://github.com/orion-ecs/keen-eye/issues/702))
- Migration best practices guide
- Sample showing 3-version evolution
- Troubleshooting guide
- API documentation

## Related

- [#352](https://github.com/orion-ecs/keen-eye/issues/352) - Parent issue (Component Schema Evolution)
- [#96](https://github.com/orion-ecs/keen-eye/issues/96) - Epic (Phase 15-16 Production Ready)
- [#697](https://github.com/orion-ecs/keen-eye/issues/697) - Version Infrastructure
- [#698](https://github.com/orion-ecs/keen-eye/issues/698) - Migration Pipeline
- [#699](https://github.com/orion-ecs/keen-eye/issues/699) - Migration Chaining
- [#700](https://github.com/orion-ecs/keen-eye/issues/700) - Default Value Injection
- [#701](https://github.com/orion-ecs/keen-eye/issues/701) - Batch Upgrader Tool
- [#702](https://github.com/orion-ecs/keen-eye/issues/702) - Documentation and Samples
- ADR-004: Reflection Elimination (AOT compatibility constraints)
- ADR-007: Capability-Based Plugin Architecture (serialization capability)
