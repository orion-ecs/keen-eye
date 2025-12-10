# ADR-002: Complete IWorld Event System

**Status:** Accepted
**Date:** 2025-12-10
**Issue:** [#206](https://github.com/tyevco/keen-eye/issues/206) (Phase 1: Grid-Based Spatial Partitioning)

## Context

The `IWorld` abstraction interface provides plugin-facing APIs to isolate plugins from internal World implementation details. Prior to this change, `IWorld` was a **sparse interface** with incomplete coverage of lifecycle events and component operations.

### Missing from IWorld

- **Entity lifecycle events**: `OnEntityCreated`, `OnEntityDestroyed`
- **Component changed event**: `OnComponentChanged<T>` (only added/removed were exposed)
- **Component set operation**: `Set<T>` (only Get/Has/Add/Remove were exposed)

This created a critical gap for plugins that need to react to entity creation and destruction:

### Problem: Spatial Indexing

The `SpatialPlugin` needs to:
1. **Index entities when created** with `Transform3D` + `SpatialIndexed` components
2. **Remove entities from index when destroyed** (despawned)

Initial implementation:
```csharp
// In SpatialUpdateSystem.OnInitialize()
World.OnComponentAdded<SpatialIndexed>(entity =>
{
    // Index entity when SpatialIndexed tag is added
    partitioner.Add(entity);
});
```

**Issue discovered:** Component events (`OnComponentAdded<T>`) were **not firing during entity creation**. Events only fired when components were added to existing entities via `World.Add()`.

This caused 23 spatial tests to fail - entities created via `EntityBuilder` were never indexed.

### Root Cause

Entity creation path in `World.CreateEntity()`:
```csharp
internal Entity CreateEntity(List<(ComponentInfo Info, object Data)> components, string? name)
{
    // 1. Create entity in archetype storage
    var entity = archetypeManager.CreateEntity(componentIds, storageType);

    // 2. Write component data to storage (archetype arrays)
    // ...

    // 3. Fire entity created event
    eventManager.FireEntityCreated(entity, name);

    // ❌ Component events NOT fired here
    return entity;
}
```

Component events were only fired via `World.Add()`, `World.Set()`, and `World.Remove()` - **not** during the initial entity creation.

### Attempted Solutions

**Option 1: Fire component events during creation**
Add component event firing to `CreateEntity()`:
```csharp
// Fire component added events for each component
foreach (var (info, data) in components)
{
    eventManager.FireComponentAdded(entity, data); // How?
}
```

**Problem:** `FireComponentAdded<T>()` is generic, but we only have `object` (boxed) data at this point. Calling via reflection (`DynamicInvoke`) was **rejected due to performance concerns** (~20-50x slower than direct calls).

**Option 2: Only use OnEntityDestroyed**
Don't index on creation, only remove on destruction:
```csharp
World.OnEntityDestroyed(entity =>
{
    if (World.Has<SpatialIndexed>(entity)) // ❌ Can't check - entity already dead
        partitioner.Remove(entity);
});
```

**Problem:** Can't query components of destroyed entities - they're already removed from storage.

**Conclusion:** Both entity creation and destruction events are required.

## Decision

Complete the `IWorld` interface to provide full lifecycle event coverage:

```csharp
public interface IWorld
{
    // Component operations
    void Add<T>(Entity entity, in T component);     // Already existed
    void Set<T>(Entity entity, in T component);     // NEW: Set or replace component
    ref T Get<T>(Entity entity);                    // Already existed
    bool Has<T>(Entity entity);                     // Already existed
    bool Remove<T>(Entity entity);                  // Already existed

    // Component lifecycle events
    EventSubscription OnComponentAdded<T>(Action<Entity, T> handler);       // Already existed
    EventSubscription OnComponentRemoved<T>(Action<Entity> handler);        // Already existed
    EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler);  // NEW: Changed event

    // Entity lifecycle events
    EventSubscription OnEntityCreated(Action<Entity, string?> handler);     // NEW: Creation event
    EventSubscription OnEntityDestroyed(Action<Entity> handler);            // NEW: Destruction event
}
```

### Rationale for Completing IWorld

**Before:** IWorld was intentionally sparse to minimize coupling. Plugins could access most operations but lacked:
1. Component change detection (`OnComponentChanged`)
2. Entity lifecycle awareness (`OnEntityCreated`, `OnEntityDestroyed`)
3. Set operation (forced plugins to call `Remove` + `Add` or cast to concrete `World`)

**After:** IWorld now provides complete lifecycle coverage, enabling plugins to:
- React to all component lifecycle stages (added, changed, removed)
- React to all entity lifecycle stages (created, destroyed)
- Use `Set<T>` for component updates without casting to concrete `World`

This maintains plugin isolation while providing necessary event hooks for stateful plugins like SpatialPlugin.

### Implementation Strategy

1. **Expose entity events in EventManager** (already existed internally):
```csharp
// In EventManager.cs
internal EventSubscription OnEntityCreated(Action<Entity, string?> handler)
    => entityEvents.OnCreated(handler);

internal EventSubscription OnEntityDestroyed(Action<Entity> handler)
    => entityEvents.OnDestroyed(handler);
```

2. **Add to IWorld interface** and implement in World:
```csharp
// In World.cs
public EventSubscription OnEntityCreated(Action<Entity, string?> handler)
    => eventManager.OnEntityCreated(handler);

public EventSubscription OnEntityDestroyed(Action<Entity> handler)
    => eventManager.OnEntityDestroyed(handler);
```

3. **Fire component events during entity creation** using delegate-per-component-type pattern (see ADR-003 for details):
```csharp
// In World.CreateEntity()
foreach (var (info, data) in components)
{
    EnsureEventDispatcher(info); // Lazy setup, once per component type
    info.FireAddedBoxed?.Invoke(eventManager.ComponentEvents, entity, data);
}
```

4. **Update SpatialUpdateSystem** to use both events:
```csharp
// Index when SpatialIndexed component is added (during or after creation)
World.OnComponentAdded<SpatialIndexed>((entity, _) =>
{
    partitioner.Add(entity);
});

// Remove from index when entity is destroyed
World.OnEntityDestroyed(entity =>
{
    if (World.Has<SpatialIndexed>(entity)) // Safe - checked before removal
        partitioner.Remove(entity);
});
```

## Alternatives Considered

### Option 1: Keep IWorld sparse (minimal surface area)

Keep IWorld intentionally minimal with only the most essential operations:

**Rejected because:**
- Forces plugins to cast to concrete `World` for Set operations
- Plugins need full lifecycle awareness for stateful behavior (indexing, persistence, etc.)
- Creates asymmetry: components have added/removed events but not changed
- Incomplete interfaces lead to workarounds and fragile code

### Option 2: Add `OnEntitySpawned` instead of `OnEntityCreated`

Use "Spawned" terminology to match public API (`World.Spawn()`):

**Rejected because:**
- Internal method is `CreateEntity()` (not `SpawnEntity()`)
- "Created" aligns with existing naming: `EntityEventHandlers.OnCreated()`
- Consistency with `OnEntityDestroyed` (created/destroyed pair)

### Option 3: Combine into single `OnEntityLifecycle` event

Single event with an enum for lifecycle stage:
```csharp
OnEntityLifecycle(Action<Entity, EntityLifecycleStage> handler);
```

**Rejected because:**
- Forces runtime switching (`switch (stage)`) instead of separate subscriptions
- Inconsistent with component lifecycle pattern (separate events per stage)
- Less type-safe (created handler gets name, destroyed doesn't - must cast)

### Option 4: Defer until plugins request it (YAGNI)

Wait for more plugin use cases before expanding IWorld:

**Rejected because:**
- SpatialPlugin already needs it (not speculative)
- Other plugins will need entity awareness (hierarchy, persistence, etc.)
- Core ECS feature - not a niche requirement

## Consequences

### Positive

- **Plugins can react to entity lifecycle** without internal World access
- **SpatialPlugin works correctly** - entities indexed on creation, removed on destruction
- **Consistent event model** - both entities and components have lifecycle events
- **No reflection overhead** - component events use delegate-per-type pattern (see ADR-003)
- **Symmetric API** - creation and destruction both have events

### Negative

- **IWorld API surface expands** - four new methods (`Set`, `OnComponentChanged`, `OnEntityCreated`, `OnEntityDestroyed`)
- **Slight overhead during entity creation** - component events now fire (~20-30 CPU cycles per component)
- **Breaking change for IWorld implementations** - any custom implementations must add these methods (unlikely - IWorld is typically implemented only by World)
- **More API to learn** - Plugins now have more lifecycle hooks to understand

### Trade-offs

| Aspect | Before | After |
|--------|--------|-------|
| IWorld completeness | Partial (sparse) | Complete (full lifecycle) |
| Component operations | Get/Has/Add/Remove | Get/Has/Add/**Set**/Remove |
| Component events | Added/Removed | Added/**Changed**/Removed |
| Entity events | ❌ None | **Created/Destroyed** |
| Plugin casting needed | Yes (for Set) | No (Set in IWorld) |
| Entity creation overhead | Minimal | +component events (~10-15%) |
| Spatial indexing | ❌ Broken | ✅ Working |

### Future Considerations

- **Entity pre-creation events** - Allow plugins to prevent entity creation (validation)
- **Batch entity events** - Fire events after batch operations complete (performance)
- **Event ordering guarantees** - Document whether component events fire before/after entity created

## Implementation Notes

### Event Firing Order

During entity creation, events fire in this order:
1. **Component events** (`OnComponentAdded<T>`) for each component
2. **Entity created event** (`OnEntityCreated`)

This ensures:
- Component handlers can access the entity's components
- Entity created handlers see fully initialized entities

### Performance Characteristics

Entity creation overhead (from event firing):
- **Per-component cost:** ~20-30 CPU cycles (delegate invocation + handler execution)
- **Per-entity cost:** ~20-30 × component_count cycles
- **Example:** Entity with 5 components = ~100-150 cycles added

Compared to entity creation's existing overhead (~500-1000 cycles for archetype lookup, storage allocation, component copying), this is a **~10-15% increase**.

### Testing Coverage

New tests added:
- ✅ `SpatialPlugin` integration tests (entities indexed on creation)
- ✅ `World.OnEntityCreated` subscription and firing
- ✅ `World.OnEntityDestroyed` subscription and firing
- ✅ Component events fire during entity creation
- ✅ SpatialUpdateSystem indexes and removes entities correctly

All 2114 tests passing with 0 warnings.

## Related Decisions

- **ADR-003** (TBD): Reflection-Free Component Event Dispatching - Details the delegate-per-type pattern used to fire component events with boxed data
- **ADR-001**: World Manager Architecture - EventManager consolidation enables this change

## References

- Issue [#206](https://github.com/tyevco/keen-eye/issues/206) - Phase 1: Grid-Based Spatial Partitioning
- Commit: "Complete IWorld event system + fire component events during entity creation"
- Files changed:
  - `src/KeenEyes.Abstractions/IWorld.cs` - Added `Set<T>`, `OnComponentChanged<T>`, `OnEntityCreated`, `OnEntityDestroyed`
  - `src/KeenEyes.Core/World.Entities.cs` - Fire component/entity events during creation
  - `src/KeenEyes.Core/EventManager.cs` - Expose entity event subscriptions
  - `src/KeenEyes.Spatial/Systems/SpatialUpdateSystem.cs` - Subscribe to entity destroyed

### IWorld Evolution

**Phase 1** (v0.1-v0.3): Sparse interface with core operations only
- Component access: `Get`, `Has`, `Add`, `Remove`
- Component events: `OnComponentAdded`, `OnComponentRemoved`
- Entity operations: `Spawn`, `Despawn`, `IsAlive`

**Phase 2** (v0.4+, this ADR): Complete lifecycle coverage
- Added: `Set<T>` - Set or replace components
- Added: `OnComponentChanged<T>` - Track component modifications
- Added: `OnEntityCreated` - React to entity creation
- Added: `OnEntityDestroyed` - React to entity destruction

This evolution reflects the lesson that plugin isolation requires **complete** lifecycle coverage, not minimal surface area. Incomplete interfaces force workarounds (casting, fragile event ordering) that defeat the purpose of abstraction.
