# ADR-007: Capability-Based Plugin Architecture

**Status:** Accepted
**Revision:** v2
**Implementation:** Shipped
**First accepted:** 2025-12-20 · **Last amended:** 2026-07-26
**Relates to:** [ADR-001](001-world-manager-architecture.md) (World managers) · [ADR-003](003-command-buffer-abstraction.md) (CommandBuffer)

## Context

Plugins currently access World functionality through two mechanisms:

1. **IPluginContext** - Limited interface for plugin installation
2. **Casting to World** - `(World)context.World` for full access

This causes problems:

### Testing Difficulty
```csharp
public void Install(IPluginContext context)
{
    var world = (World)context.World;  // Requires concrete World
    world.RegisterPrefab("Enemy", enemyPrefab);
}
```

Testing this plugin requires a full `World` instance, which:
- Creates archetype managers, query caches, etc.
- Has initialization overhead
- Cannot be mocked for specific behavior testing

### Hidden Dependencies
Plugins silently depend on `World` features without declaring them. There's no way to know what a plugin needs without reading its implementation.

### Fragile Code
If `World`'s public API changes, plugins that cast to `World` break silently at runtime, not compile time.

## Decision

Extract cohesive World features into **capability interfaces**. Plugins request specific capabilities rather than casting to `World`.

### Capability Interfaces

| Capability | Purpose | Location |
|------------|---------|----------|
| `ISystemHookCapability` | Before/after system execution hooks | Abstractions |
| `IPersistenceCapability` | World snapshot save/load | Abstractions |
| `IHierarchyCapability` | Parent-child entity relationships | Abstractions |
| `IValidationCapability` | Component validation configuration | Abstractions |
| `ITagCapability` | String-based entity tagging | Abstractions |
| `IStatisticsCapability` | Memory profiling | Abstractions |
| `IInspectionCapability` | Entity inspection for debugging | Abstractions |
| `ISnapshotCapability` | Basic world snapshot operations | Abstractions |
| `ISerializationCapability` | AOT-aware serialization via `IComponentRegistry` | Abstractions |
| `ISaveLoadCapability` | World save/load orchestration (extends `IPersistenceCapability`) | Core* |

*Core capabilities depend on Core types and therefore live in `KeenEyes.Core`.

An `IPrefabCapability` (entity templates) originally shipped with this ADR but was removed along with the runtime prefab API, which was superseded by source-generated spawn methods.

**Note on ISnapshotCapability vs ISerializationCapability:**

`ISnapshotCapability` provides simple snapshot operations (`GetComponents`, `GetAllSingletons`, `SetSingleton`, `Clear`) without exposing Core types. This allows plugins that only need basic snapshot functionality to depend solely on Abstractions.

`ISerializationCapability` extends `ISnapshotCapability` and adds `IComponentRegistry` access, which is required for AOT-compatible serialization where component registration happens at runtime. Both interfaces live in Abstractions: `ISerializationCapability` exposes the abstraction types `IComponentRegistry`/`IComponentInfo` (defined alongside it), not Core's `ComponentInfo`, so no Core dependency is required.

### New Plugin Pattern

```csharp
public void Install(IPluginContext context)
{
    // Request specific capability
    if (context.TryGetCapability<ITagCapability>(out var tags))
    {
        tags.AddTag(entity, "Enemy");
    }

    // Or require it (throws if unavailable)
    var hierarchy = context.GetCapability<IHierarchyCapability>();
    hierarchy.SetParent(child, parent);
}
```

### Mock Implementations for Testing

`KeenEyes.Testing` ships seven capability mocks — `MockHierarchyCapability`, `MockInspectionCapability`, `MockPersistenceCapability`, `MockStatisticsCapability`, `MockSystemHookCapability`, `MockTagCapability`, `MockValidationCapability` — and `MockPluginContext.SetCapability<T>()` wires them into a plugin context:

```csharp
// Test plugin without real World
var mockHooks = new MockSystemHookCapability();
var mockContext = new MockPluginContext()
    .SetCapability<ISystemHookCapability>(mockHooks);

plugin.Install(mockContext);

// Verify behavior
Assert.True(mockHooks.WasHookAdded);
Assert.Equal(1, mockHooks.HookCount);
```

### IWorld Already Provides Core Hierarchy

Analysis revealed that `IWorld` already includes basic hierarchy operations:
- `SetParent(Entity child, Entity parent)`
- `GetParent(Entity entity)`
- `GetChildren(Entity entity)`

UI systems (`UIRenderSystem`, `UILayoutSystem`, `UIHitTester`) were casting to `World` unnecessarily. These now use `IWorld` directly.

## Consequences

### Positive

1. **Testability** - Plugins can be tested with mocks, no real World needed
2. **Explicit dependencies** - Plugins declare what capabilities they need
3. **Compile-time safety** - Interface changes cause compilation errors
4. **Smaller test scope** - Test only the capability being used
5. **Better documentation** - Capability interfaces document available features

### Negative

1. **More interfaces** - Additional abstraction layer to understand
2. **Migration work** - Existing plugins need updating (if any cast to World)
3. **Capability discovery** - Developers must learn which capabilities exist

### Neutral

1. **World still works** - All capabilities are implemented by World
2. **No performance impact** - Interface dispatch is negligible
3. **Gradual adoption** - Plugins can migrate incrementally

## Implementation

### Phase 1: Core Capabilities ✅
- `IHierarchyCapability`, `IValidationCapability`, `ITagCapability`, `IStatisticsCapability` — along with `IInspectionCapability`, `IPersistenceCapability`, `ISnapshotCapability`, `ISerializationCapability`, and `ISystemHookCapability` — live in Abstractions; `ISaveLoadCapability` lives in Core
- `World` implements all capability interfaces
- Seven mock capabilities plus `MockPluginContext.SetCapability<T>()` ship in `KeenEyes.Testing`
- `IPrefabCapability` shipped in this phase but was later removed with the runtime prefab API (superseded by source-generated spawn methods)

### Phase 2: UI System Cleanup ✅
- Updated `UIRenderSystem`, `UILayoutSystem`, `UIHitTester` to use `IWorld.GetChildren()` instead of casting to `World`

### Phase 3: Documentation ✅
- Created this ADR
- Updated plugins.md with capability usage
- Added testing documentation

## Related

- [ADR-001](001-world-manager-architecture.md): World Manager Architecture (internal managers)
- [ADR-003](003-command-buffer-abstraction.md): Command Buffer Abstraction (similar pattern for ICommandBuffer)

---

## Changelog

- **v2 — 2026-07-26 (living-ADR conversion):** Implementation marked Shipped (status was already Accepted). Capability table corrected to as-built layout: `ISerializationCapability` lives in Abstractions and exposes `IComponentRegistry`/`IComponentInfo` (not Core's `ComponentInfo`); `IPrefabCapability` removed with the runtime prefab API (superseded by source-generated spawn methods) and Core's `ISaveLoadCapability` added in its place. Code examples rewritten against shipped APIs: `ITagCapability`/`IHierarchyCapability` plugin pattern and `MockPluginContext.SetCapability<T>()` with the seven shipped mocks, replacing the never-shipped `PluginContextBuilder`/`MockPrefabCapability`.
- **v1 — 2025-12-20 (ef18fedd):** Accepted — extract World features into capability interfaces so plugins request explicit capabilities via IPluginContext instead of casting to World; shipped with interfaces, World implementations, Testing mocks, and UI-system IWorld cleanup.
