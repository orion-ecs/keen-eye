# ADR-015: Component Schema Migrations

**Status:** Accepted
**Revision:** v2
**Implementation:** Partial
**First accepted:** 2026-01-03 · **Last amended:** 2026-07-26
**Relates to:** [ADR-004](004-reflection-elimination.md) (AOT / no reflection) · [ADR-007](007-capability-based-plugin-architecture.md) (plugin capabilities) · [#352](https://github.com/orion-ecs/keen-eye/issues/352) · [#96](https://github.com/orion-ecs/keen-eye/issues/96)

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

KeenEyes implements a **versioned component migration system** with three layers:

```
┌─────────────────────────────────────────────────────────────┐
│                    Source Generators                         │
│  - Generate version metadata                                 │
│  - Generate component migrators                              │
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
    public static int GetVersion(string componentTypeName);
}
```

### Migration Functions

Migration methods transform data from one version to the next. They are annotated with `[MigrateFrom(fromVersion)]` directly (there is no method-name argument — the generator discovers each method by attribute placement) and receive the old data as a `JsonElement` rather than a typed previous-version struct:

```csharp
[Component(Version = 2)]
public partial struct Health
{
    public int Current;
    public int Max;
    public int Shield;

    [MigrateFrom(1)]
    private static Health MigrateFromV1(JsonElement oldData)
    {
        return new Health
        {
            Current = oldData.GetProperty("current").GetInt32(),
            Max = oldData.GetProperty("max").GetInt32(),
            Shield = 0  // Default for new field
        };
    }
}
```

Because the old data arrives as JSON, previous-version types (`HealthV1`, etc.) do not need to be preserved — only the current struct definition exists.

### Migration Chain Resolution

Migration chaining is exposed through the `IComponentMigrator` interface, implemented by the source-generated `ComponentSerializer` with strongly-typed dispatch — no delegate dictionary and no `DynamicInvoke` (which would be reflection-adjacent):

```csharp
public interface IComponentMigrator
{
    bool CanMigrate(string typeName, int fromVersion, int toVersion);
    bool CanMigrate(Type type, int fromVersion, int toVersion);

    JsonElement? Migrate(string typeName, JsonElement data, int fromVersion, int toVersion);
    JsonElement? Migrate(Type type, JsonElement data, int fromVersion, int toVersion);

    IEnumerable<int> GetMigrationVersions(string typeName);
    IEnumerable<int> GetMigrationVersions(Type type);
}
```

Multi-step chains execute inside the generated migrator: migrating v1 → v4 invokes v1→v2, v2→v3, then v3→v4 in sequence. `MigrationGraph` and the `KEEN114` analyzer warning detect gaps in the chain at build time; cycles are structurally impossible because each migration steps exactly one version forward toward the current version.

### Serialization Integration

Version metadata is stored in serialized data:

**Binary format** (snapshot format v2; v1 files default every component to version 1):
```
[ComponentTypeId: int32]
[Version: int16]          // version tag
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

Version handling is integrated into `SnapshotManager` restoration rather than a per-serializer `DeserializeComponent<T>` method. Each `SerializedComponent` carries its schema version, and on restore `SnapshotManager` compares it against the generated serializer's `GetVersion(type)`:

- **Version matches** — fast path: direct deserialization.
- **Serialized version is older** — the old `JsonElement` data is migrated through `IComponentMigrator` before deserialization.
- **Serialized version is newer, or no migration path exists** — restore throws `ComponentVersionException` (a save from a newer game build cannot be downgraded).

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

The source generator produces the automatic migration: existing fields are copied from the old JSON data and annotated fields receive their `[DefaultValue]` — no hand-written migration method is needed. The `Currency` component in `samples/KeenEyes.Sample.SchemaMigration` demonstrates a three-version evolution using only `[DefaultValue]` attributes.

### Batch Upgrader Tool

The `keeneyes migrate` command ships with dry-run, backup, glob-pattern, output-directory, verbose, and continue-on-error support, and analyzes component versions in both JSON and binary save files:

```bash
# Preview which files contain versioned components
dotnet keeneyes migrate --path ./saves/ --dry-run

# Validate files, creating backups first
dotnet keeneyes migrate --path ./saves/ --backup

# Creates backup: ./saves/save1.dat.backup
```

**Not yet implemented: offline data transformation.** Because the CLI cannot load the game's generated serializer, it validates and copies files rather than rewriting component data — actual migration executes when the game loads the save, via `SnapshotManager`. For the same reason the analysis reports the versions found in a file but cannot determine true target versions. Rewriting data offline would require the user to supply a serializer assembly (future work).

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
3. **AOT compatible** - Source-generated migrators, no reflection
4. **Tooling support** - `keeneyes migrate` CLI analyzes and backs up existing save files (offline data rewriting not yet implemented)
5. **Gradual adoption** - Version defaults to 1, migration optional

### Negative

1. **Version tracking overhead** - 2 bytes per component in binary format
2. **Untyped migration input** - Migration methods read old data from a `JsonElement`, so field access inside migrations is stringly-typed and not compiler-checked (the trade-off for not keeping old version types around)
3. **Migration complexity** - Multi-step migrations can be hard to reason about
4. **Testing burden** - Each migration path needs testing

### Risks

1. **Circular dependencies** - Structurally impossible as-built: each migration steps exactly one version forward, so the real risk is gaps in the chain
2. **Missing migrations** - Version gap with no handler (KEEN114 analyzer warning at build time; `ComponentVersionException` at load)
3. **Cross-component migrations** - Component A needs data from Component B (not supported initially)
4. **Performance during load** - Many migrations on large saves

## Implementation Phases

### Phase 1: Version Infrastructure ([#697](https://github.com/orion-ecs/keen-eye/issues/697)) — ✅ Shipped
- [x] `[Component(Version = n)]` attribute support
- [x] Version metadata in serialization format (int16 tag in binary snapshot format v2)
- [x] `ComponentMigrationMetadata` source generator
- [x] Version mismatch detection (`ComponentVersionException`)

### Phase 2: Migration Pipeline ([#698](https://github.com/orion-ecs/keen-eye/issues/698)) — ✅ Shipped
- [x] `[MigrateFrom]` attribute
- [x] Generated migrator (shipped as `IComponentMigrator` implemented by the generated `ComponentSerializer`, not a `ComponentMigrationRegistry`)
- [x] Single-step migration execution
- [x] Integration with `IComponentSerializer`

### Phase 3: Migration Chaining ([#699](https://github.com/orion-ecs/keen-eye/issues/699)) — ✅ Shipped
- [x] Multi-step migration (v1 → v2 → v3)
- [x] Migration graph validation (`MigrationGraph`)
- [x] Cycle detection (`MigrationGraph.HasCycles`; cycles are structurally impossible as-built)
- [x] Gap detection (KEEN114 analyzer warning)

### Phase 4: Default Value Injection ([#700](https://github.com/orion-ecs/keen-eye/issues/700)) — ✅ Shipped
- [x] `[DefaultValue]` attribute for new fields
- [x] Auto-generated migrations for simple additions
- [x] Combine with explicit migrations

### Phase 5: Batch Upgrader Tool ([#701](https://github.com/orion-ecs/keen-eye/issues/701)) — ✅ Shipped (with caveat)
- [x] `dotnet keeneyes migrate` command (plus `--pattern`, `--output`, `--continue-on-error`)
- [x] Dry-run mode
- [x] Backup creation
- [x] Progress reporting
- Not yet implemented: offline component-data rewriting — the CLI validates and copies files, and migration executes at game load (would require a user-supplied serializer assembly)

### Phase 6: Documentation and Samples ([#702](https://github.com/orion-ecs/keen-eye/issues/702)) — ✅ Shipped
- [x] Migration best practices guide (`docs/migrations.md`)
- [x] Sample showing 3-version evolution (`samples/KeenEyes.Sample.SchemaMigration`)
- [x] Troubleshooting guide
- [x] API documentation

## Related

- [#352](https://github.com/orion-ecs/keen-eye/issues/352) - Parent issue (Component Schema Evolution)
- [#96](https://github.com/orion-ecs/keen-eye/issues/96) - Epic (Phase 15-16 Production Ready)
- [#697](https://github.com/orion-ecs/keen-eye/issues/697) - Version Infrastructure
- [#698](https://github.com/orion-ecs/keen-eye/issues/698) - Migration Pipeline
- [#699](https://github.com/orion-ecs/keen-eye/issues/699) - Migration Chaining
- [#700](https://github.com/orion-ecs/keen-eye/issues/700) - Default Value Injection
- [#701](https://github.com/orion-ecs/keen-eye/issues/701) - Batch Upgrader Tool
- [#702](https://github.com/orion-ecs/keen-eye/issues/702) - Documentation and Samples
- [ADR-004: Reflection Elimination](004-reflection-elimination.md) (AOT compatibility constraints)
- [ADR-007: Capability-Based Plugin Architecture](007-capability-based-plugin-architecture.md) (serialization capability)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Status corrected Proposed → Accepted — all six phase issues (#697–#702) and parent #352 closed. Implementation marked Partial: Phases 1–4 and 6 fully shipped; Phase 5's `keeneyes migrate` CLI analyzes, backs up, and copies save files but does not rewrite component data offline (migration runs at game load). Decision amended to the as-built API: `[MigrateFrom(fromVersion)]` methods taking `JsonElement` (no preserved `HealthV1`-style types), `IComponentMigrator` on the generated `ComponentSerializer` instead of a `ComponentMigrationRegistry` delegate dictionary with `DynamicInvoke`, version handling in `SnapshotManager` restore, and the circular-dependency risk reclassified as structurally impossible (KEEN114 gap detection is the real safeguard).
- **v1 — 2026-01-03 (#352):** Proposed — versioned component schema migration system ([Component(Version)], [MigrateFrom], [DefaultValue], source-generated migrators, serialized version tags, and a CLI batch upgrader) so save files remain loadable as component definitions evolve.
