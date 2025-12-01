# KeenEye Roadmap

This document outlines the features needed to achieve parity with [OrionECS](https://github.com/tyevco/OrionECS), the TypeScript ECS framework that KeenEye reimplements in C#.

## Current Status

KeenEye has basic ECS scaffolding in place:
- `World` container with isolated state
- `Entity` as `readonly record struct` with version tracking
- `EntityBuilder` with fluent `With<T>()` / `WithTag<T>()` API
- `IComponent` / `ITagComponent` marker interfaces
- `ComponentRegistry` with per-world component IDs
- `QueryBuilder<T1..T4>` with `With<>()` / `Without<>()` filters
- `ISystem` / `SystemBase` with Initialize/Update lifecycle
- `SystemGroup` for grouped execution
- Source generator attributes (`[Component]`, `[System]`, `[Query]`)

---

## Phase 1: Core Completion (Foundation)

Essential features needed for basic usability.

### 1.1 Entity Operations
- [ ] `World.Get<T>(entity)` - retrieve component data (currently throws `NotImplementedException`)
- [ ] `World.Set<T>(entity, value)` - update component data
- [ ] `World.Add<T>(entity, component)` - add component to existing entity
- [ ] `World.Remove<T>(entity)` - remove component from entity
- [ ] `World.GetComponents(entity)` - get all components on entity
- [ ] `World.Has<T>(entity)` - check component presence (partial implementation exists)
- [ ] Entity naming support (`World.Spawn(name)`)

### 1.2 Singletons / Resources
World-level data not tied to entities (time, input, config, etc.):
- [ ] `World.SetSingleton<T>(value)`
- [ ] `World.GetSingleton<T>()`
- [ ] `World.TryGetSingleton<T>(out T value)`
- [ ] `World.HasSingleton<T>()`
- [ ] `World.RemoveSingleton<T>()`

### 1.3 Command Buffer / Deferred Operations
Safe entity modification during iteration:
- [ ] `CommandBuffer` class
- [ ] Deferred entity creation
- [ ] Deferred component add/remove
- [ ] Deferred entity destruction (`QueueDespawn`)
- [ ] `World.Flush()` to execute buffered commands

---

## Phase 2: Archetype Storage (Performance)

Replace dictionary-based storage with cache-friendly archetypes.

### 2.1 Archetype System
- [ ] `Archetype` - component type combination storage
- [ ] `ArchetypeStorage` - contiguous component arrays
- [ ] Archetype graph for efficient transitions
- [ ] `ref T` returns for zero-copy component access

### 2.2 Object Pooling
- [ ] Entity ID recycling
- [ ] Component array pooling
- [ ] Archetype chunk pooling
- [ ] Memory usage statistics

### 2.3 Query Caching
- [ ] `QueryManager` for cached queries
- [ ] Automatic cache invalidation on archetype changes
- [ ] Query reuse across systems

---

## Phase 3: Entity Hierarchy

Parent-child relationships for scene graphs and transforms.

- [ ] `World.SetParent(child, parent)`
- [ ] `World.GetParent(entity)`
- [ ] `World.GetChildren(entity)`
- [ ] `World.AddChild(parent, child)`
- [ ] `World.RemoveChild(parent, child)`
- [ ] Cascading despawn (destroy children with parent)
- [ ] Hierarchy traversal utilities

---

## Phase 4: Events & Change Tracking

Reactive patterns for responding to entity/component changes.

### 4.1 Event System
- [ ] `World.OnComponentAdded<T>(handler)`
- [ ] `World.OnComponentRemoved<T>(handler)`
- [ ] `World.OnComponentChanged<T>(handler)`
- [ ] `World.OnEntityCreated(handler)`
- [ ] `World.OnEntityDestroyed(handler)`
- [ ] General event bus for custom events

### 4.2 Change Tracking
- [ ] `World.MarkDirty<T>(entity)`
- [ ] `World.GetDirtyEntities<T>()`
- [ ] `World.ClearDirtyFlags<T>()`
- [ ] Automatic change detection option
- [ ] Change tracking configuration

---

## Phase 5: System Enhancements

Better control over system execution.

### 5.1 Lifecycle Hooks
- [ ] `OnBeforeUpdate()` - runs before entity processing
- [ ] `OnAfterUpdate()` - runs after entity processing
- [ ] `OnEnabled()` / `OnDisabled()` callbacks

### 5.2 Runtime Control
- [ ] `system.Enabled` property
- [ ] `World.EnableSystem<T>()` / `DisableSystem<T>()`
- [ ] `World.GetSystem<T>()`

### 5.3 Execution Order
- [ ] Priority-based system sorting
- [ ] `[RunBefore(typeof(OtherSystem))]` attribute
- [ ] `[RunAfter(typeof(OtherSystem))]` attribute
- [ ] Fixed update support (separate from variable update)

---

## Phase 6: Plugin System

Extensibility for physics, networking, rendering, etc.

### 6.1 Plugin Infrastructure
- [ ] `IWorldPlugin` interface
- [ ] `PluginContext` for registration
- [ ] Plugin metadata (name, version)
- [ ] `Install()` / `Uninstall()` lifecycle

### 6.2 WorldBuilder Pattern
- [ ] `WorldBuilder` class
- [ ] `.WithPlugin<T>()` fluent API
- [ ] `.WithFixedUpdateRate(fps)` configuration
- [ ] `.WithDebugMode(enabled)` configuration
- [ ] `.WithChangeTracking(mode)` configuration

### 6.3 Runtime Plugin Management
- [ ] `World.InstallPlugin(plugin)`
- [ ] `World.UninstallPlugin(name)`
- [ ] `World.HasPlugin(name)`
- [ ] `World.GetPlugin<T>()`
- [ ] `World.GetInstalledPlugins()`

### 6.4 Extension API
- [ ] `World.SetExtension<T>(api)` - plugin-provided APIs
- [ ] `World.GetExtension<T>()`
- [ ] Type-safe extension access

---

## Phase 7: Inter-System Messaging

Decoupled communication between systems.

- [ ] `MessageManager` / `IMessageBus`
- [ ] `World.Send<T>(message)`
- [ ] `World.Subscribe<T>(handler)`
- [ ] `World.Unsubscribe<T>(handler)`
- [ ] Typed message channels
- [ ] Broadcast vs targeted messages

---

## Phase 8: Prefabs & Templates

Entity templates for content creation.

- [ ] `PrefabManager`
- [ ] `World.RegisterPrefab(name, config)`
- [ ] `World.SpawnFromPrefab(name)`
- [ ] `World.SpawnFromPrefab(name, overrides)`
- [ ] Prefab inheritance
- [ ] Nested prefabs

---

## Phase 9: Entity Tags (String-based)

Dynamic categorization beyond type-safe `ITagComponent`.

- [ ] `World.AddTag(entity, tagName)`
- [ ] `World.RemoveTag(entity, tagName)`
- [ ] `World.HasTag(entity, tagName)`
- [ ] `World.GetTags(entity)`
- [ ] Query filtering by tags (`.WithTag("enemy")`)
- [ ] Query exclusion by tags (`.WithoutTag("dead")`)

---

## Phase 10: Component Validation

Development-time safety checks.

- [ ] `[RequiresComponent(typeof(T))]` attribute
- [ ] `[ConflictsWith(typeof(T))]` attribute
- [ ] Custom validation delegates
- [ ] Validation toggle (dev vs production)

---

## Phase 11: Serialization & Snapshots

Save/load world state.

- [ ] `SnapshotManager`
- [ ] `World.CreateSnapshot()` - capture world state
- [ ] `World.RestoreSnapshot(snapshot)` - restore state
- [ ] Entity serialization
- [ ] Component serialization (opt-in via `[Component(Serializable = true)]`)
- [ ] Binary and JSON format support

---

## Phase 12: Debugging & Profiling

Development tools and diagnostics.

### 12.1 Debug Mode
- [ ] `World.DebugMode` toggle
- [ ] Verbose logging option
- [ ] Entity/component inspection

### 12.2 Profiling
- [ ] System execution timing
- [ ] Query performance metrics
- [ ] Memory usage tracking
- [ ] GC allocation monitoring

### 12.3 Visualization
- [ ] Entity inspector API
- [ ] System execution timeline
- [ ] Archetype statistics

---

## Phase 13: Manager Architecture (Optional Refactor)

Consider splitting `World` into specialized managers for better separation of concerns:

```
World (facade)
├── EntityManager
├── ComponentManager
├── SystemManager
├── QueryManager
├── PrefabManager
├── SnapshotManager
└── MessageManager
```

This is an architectural decision - the current monolithic `World` may be acceptable for simpler use cases.

---

## Future: Companion Packages

Optional packages that extend KeenEye:

| Package | Purpose |
|---------|---------|
| `KeenEye.Math` | Vector2/3, Matrix4x4, Quaternion, etc. |
| `KeenEye.Physics` | Physics simulation plugin |
| `KeenEye.Input` | Input management plugin |
| `KeenEye.Network` | Multiplayer/networking plugin |
| `KeenEye.StateMachine` | Finite state machine plugin |
| `KeenEye.AI` | Decision trees, behavior trees |
| `KeenEye.Spatial` | Quadtree/octree spatial partitioning |
| `KeenEye.Timeline` | Animation/sequencing |
| `KeenEye.Testing` | Test utilities, mock worlds |
| `KeenEye.DevTools` | Profiling, debugging, inspection |

---

## Feature Count Summary

| Category | Features | Status |
|----------|----------|--------|
| Core Entity Operations | 7 | Pending |
| Singletons/Resources | 5 | Pending |
| Command Buffer | 5 | Pending |
| Archetype Storage | 8 | Pending |
| Query Caching | 3 | Pending |
| Entity Hierarchy | 7 | Pending |
| Event System | 6 | Pending |
| Change Tracking | 5 | Pending |
| System Enhancements | 10 | Pending |
| Plugin System | 15 | Pending |
| Inter-System Messaging | 6 | Pending |
| Prefabs | 6 | Pending |
| Entity Tags | 6 | Pending |
| Component Validation | 4 | Pending |
| Serialization | 6 | Pending |
| Debug/Profiling | 10 | Pending |
| **Total** | **~109** | |

---

## Design Principles

When implementing these features, maintain KeenEye's core principles:

1. **No Static State** - All state is per-world instance
2. **Respect User Intent** - Provide helpers, let users wire things up
3. **Performance First** - Zero/minimal allocations in hot paths
4. **Source Generators for Ergonomics** - Reduce boilerplate, maintain performance
5. **Components are Structs** - Cache-friendly value semantics
6. **Entities are IDs** - Lightweight `(int Id, int Version)` with staleness detection
