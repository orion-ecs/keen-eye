# ADR-007: Capability-Based Plugin Architecture

**Status:** Accepted
**Date:** 2025-12-20

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
| `ISerializationCapability` | AOT-aware serialization with ComponentRegistry | Core* |
| `IPrefabCapability` | Entity templates | Core* |

*Core capabilities depend on Core types (`EntityPrefab`, `ComponentInfo`, etc.).

**Note on ISnapshotCapability vs ISerializationCapability:**

`ISnapshotCapability` provides simple snapshot operations (`GetComponents`, `GetAllSingletons`, `SetSingleton`, `Clear`) without exposing Core types. This allows plugins that only need basic snapshot functionality to depend solely on Abstractions.

`ISerializationCapability` extends `ISnapshotCapability` and adds `IComponentRegistry` access, which is required for AOT-compatible serialization where component registration happens at runtime. This interface lives in Core because it exposes `ComponentInfo`.

### New Plugin Pattern

```csharp
public void Install(IPluginContext context)
{
    // Request specific capability
    if (context.TryGetCapability<IPrefabCapability>(out var prefabs))
    {
        prefabs.RegisterPrefab("Enemy", enemyPrefab);
    }

    // Or require it (throws if unavailable)
    var hierarchy = context.GetCapability<IHierarchyCapability>();
    hierarchy.SetParent(child, parent);
}
```

### Mock Implementations for Testing

Each capability has a corresponding mock in `KeenEyes.Testing`:

```csharp
// Test plugin without real World
var mockPrefabs = new MockPrefabCapability();
var mockContext = new PluginContextBuilder()
    .WithCapability<IPrefabCapability>(mockPrefabs)
    .Build();

plugin.Install(mockContext);

// Verify behavior
Assert.Single(mockPrefabs.RegistrationOrder);
Assert.Equal("Enemy", mockPrefabs.RegistrationOrder[0]);
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
- Created `IHierarchyCapability`, `IValidationCapability`, `ITagCapability`, `IStatisticsCapability` in Abstractions
- Created `IPrefabCapability` in Core
- Updated `World` to implement all capability interfaces
- Created mock implementations in `KeenEyes.Testing`

### Phase 2: UI System Cleanup ✅
- Updated `UIRenderSystem`, `UILayoutSystem`, `UIHitTester` to use `IWorld.GetChildren()` instead of casting to `World`

### Phase 3: Documentation ✅
- Created this ADR
- Updated plugins.md with capability usage
- Added testing documentation

## Related

- ADR-001: World Manager Architecture (internal managers)
- ADR-003: Command Buffer Abstraction (similar pattern for ICommandBuffer)
