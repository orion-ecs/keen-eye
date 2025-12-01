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

## Phase 12: Logging System

Pluggable logging with multiple providers.

### 12.1 Log Provider Architecture
- [ ] `ILogProvider` interface
- [ ] `LogManager` for provider registration
- [ ] Multiple simultaneous providers
- [ ] Provider-specific configuration

### 12.2 Built-in Providers
- [ ] Console log provider (default)
- [ ] File log provider
- [ ] Debug output provider (IDE integration)
- [ ] Null provider (disable logging)

### 12.3 Logging Features
- [ ] Log levels (Trace, Debug, Info, Warning, Error, Fatal)
- [ ] Log filtering by level and category
- [ ] Structured logging with properties
- [ ] Log scopes for context
- [ ] Timestamp and source formatting
- [ ] Performance-friendly (zero overhead when disabled)

### 12.4 ECS-Specific Logging
- [ ] System execution logging
- [ ] Entity lifecycle logging
- [ ] Component change logging
- [ ] Query execution logging
- [ ] Configurable verbosity per category

---

## Phase 13: Debugging & Profiling

Development tools and diagnostics.

### 13.1 Debug Mode
- [ ] `World.DebugMode` toggle
- [ ] Verbose logging option
- [ ] Entity/component inspection

### 13.2 Profiling
- [ ] System execution timing
- [ ] Query performance metrics
- [ ] Memory usage tracking
- [ ] GC allocation monitoring

### 13.3 Visualization
- [ ] Entity inspector API
- [ ] System execution timeline
- [ ] Archetype statistics

---

## Phase 14: Testing Tools

Utilities for testing ECS code.

### 14.1 Test World Builder
- [ ] `TestWorldBuilder` with fluent API
- [ ] Pre-configured test worlds
- [ ] Isolated world instances per test
- [ ] Deterministic entity ID generation
- [ ] Time control (manual tick advancement)

### 14.2 Assertion Helpers
- [ ] `entity.ShouldHaveComponent<T>()`
- [ ] `entity.ShouldNotHaveComponent<T>()`
- [ ] `entity.ShouldBeAlive()` / `ShouldBeDead()`
- [ ] `component.ShouldEqual(expected)`
- [ ] `world.ShouldHaveEntityCount(n)`
- [ ] `query.ShouldMatchEntities(count)`

### 14.3 Mock Utilities
- [ ] Mock system base class
- [ ] System execution recording
- [ ] Component change tracking for tests
- [ ] Event capture and verification

### 14.4 Snapshot Testing
- [ ] World state snapshots for comparison
- [ ] Entity snapshot assertions
- [ ] Component data diffing
- [ ] Snapshot serialization format

### 14.5 Test Fixtures
- [ ] Common component fixtures
- [ ] Entity builder presets
- [ ] System test harness
- [ ] Integration test utilities

---

## Phase 15: Manager Architecture (Optional Refactor)

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

## Phase 16: Production Readiness

Features for shipping production games.

### 16.1 Multi-World Support
- [ ] Multiple independent worlds in one process
- [ ] World isolation guarantees
- [ ] Cross-world entity references (optional)
- [ ] World templates/cloning

### 16.2 Component Schema Evolution
- [ ] Component versioning
- [ ] Migration handlers for schema changes
- [ ] Backward compatibility utilities
- [ ] Data upgrade pipelines

### 16.3 Enhanced Save/Load System
Beyond basic serialization:
- [ ] Multiple save slots with metadata
- [ ] Save slot management (create, delete, copy)
- [ ] Compression (gzip, brotli)
- [ ] Optional encryption (AES-256)
- [ ] Checksum validation & corruption detection
- [ ] Cloud save synchronization
- [ ] Conflict resolution for cloud saves
- [ ] Auto-save with configurable triggers
- [ ] Incremental/delta saves
- [ ] Thumbnails and custom metadata
- [ ] Import/export for cross-platform

### 16.4 Network Synchronization
- [ ] Entity replication
- [ ] Component sync strategies
- [ ] Client-side prediction
- [ ] Server reconciliation
- [ ] Network plugin foundation

---

## Phase 17: Advanced Performance

Maximum performance optimizations.

### 17.1 Parallelization
- [ ] Parallel system execution
- [ ] Job system integration
- [ ] Thread-safe command buffers
- [ ] Parallel query iteration
- [ ] Dependency graph for safe parallelism

### 17.2 Native AOT Support
- [ ] Trimming-safe APIs
- [ ] Source generator compatibility with AOT
- [ ] Reflection-free serialization
- [ ] Benchmark AOT vs JIT performance

### 17.3 Advanced Spatial Partitioning
- [ ] Configurable grid sizes
- [ ] Hierarchical spatial structures
- [ ] Broadphase/narrowphase separation
- [ ] Spatial query optimization

### 17.4 Component Composition
- [ ] Component inheritance/mixins
- [ ] Component bundles (group common components)
- [ ] `[Bundle]` attribute for source generation

---

## Phase 18: Deterministic Replay System

Record and replay gameplay for debugging and playtesting.

### 18.1 Recording System
- [ ] `ReplayRecorder` class
- [ ] Record all inputs (keyboard, mouse, gamepad)
- [ ] Capture non-deterministic events (random seeds, timestamps)
- [ ] Log system execution order
- [ ] Interval-based snapshot capture
- [ ] Delta compression for storage efficiency

### 18.2 Playback Engine
- [ ] `ReplayPlayer` class
- [ ] Frame-perfect input replay
- [ ] Deterministic execution guarantee
- [ ] State restoration at any point
- [ ] Cross-machine determinism

### 18.3 Timeline Controls
- [ ] `ReplayTimeline` class
- [ ] Play/pause/stop
- [ ] Timeline scrubbing (seek to any frame)
- [ ] Speed adjustment (0.25x to 4x)
- [ ] Frame stepping (forward/backward)

### 18.4 Playtesting Infrastructure
- [ ] Session recording for playtesters
- [ ] Web distribution of test builds
- [ ] Crash reporting integration
- [ ] Player feedback collection
- [ ] Debug visualization during replay

---

## Phase 19: IDE Extension

Developer tooling for Visual Studio / VS Code / Rider.

### 19.1 Code Navigation
- [ ] Go to component/system definition
- [ ] Find all references
- [ ] Component/system usage analysis

### 19.2 Refactoring Support
- [ ] Rename component (updates all usages)
- [ ] Extract component from entity
- [ ] Inline component

### 19.3 Visualization Tools
- [ ] Entity hierarchy tree view
- [ ] Archetype visualization
- [ ] System dependency graph
- [ ] Query visualization

### 19.4 Debugging Integration
- [ ] Entity inspector panel
- [ ] Component value editing
- [ ] System execution visualizer
- [ ] Breakpoints on component changes

---

## Long-Term Vision: Browser-Based Editor

*Target: v1.0.0+ (2027+)*

A full-featured browser-based game editor like Unity/Godot, powered by KeenEye.

### Editor Frontend Application
- [ ] Project management UI
- [ ] Asset browser
- [ ] Drag-and-drop entity creation
- [ ] Visual scripting (optional)

### Scene Editor
- [ ] 2D/3D viewport
- [ ] Entity manipulation (transform gizmos)
- [ ] Multi-select and group operations
- [ ] Undo/redo system
- [ ] Scene hierarchy panel

### Inspector & Properties Panel
- [ ] Auto-generated editors from C# types
- [ ] Custom property drawers
- [ ] Multi-entity editing
- [ ] Property search and filtering
- [ ] Collapsible component sections
- [ ] Property metadata decorators (`[Range]`, `[Tooltip]`, etc.)

### Code Editor Integration
- [ ] In-browser C# editing (via Blazor/Monaco)
- [ ] Hot reload support
- [ ] Intellisense/autocomplete
- [ ] Error highlighting

### Editor Backend Services
- [ ] Project persistence
- [ ] Asset pipeline
- [ ] Build system integration
- [ ] Collaboration features (optional)

### Play Mode
- [ ] Run game in editor
- [ ] Pause and inspect state
- [ ] Live entity/component editing
- [ ] Performance overlay

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

### Additional Plugins from OrionECS
| Plugin | Purpose |
|--------|---------|
| `KeenEye.Canvas2D` | 2D canvas rendering |
| `KeenEye.ResourceManager` | Asset loading and caching |
| `KeenEye.Interaction` | User interaction systems |
| `KeenEye.Budgets` | Resource budget management |
| `KeenEye.ComponentPropagation` | Parent-to-child component inheritance |
| `KeenEye.TransformPropagation` | Automatic transform inheritance |

---

## Feature Count Summary

### Engine Core (Phases 1-11)
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
| **Subtotal** | **~99** | |

### Developer Experience (Phases 12-14)
| Category | Features | Status |
|----------|----------|--------|
| Log Provider Architecture | 4 | Pending |
| Built-in Log Providers | 4 | Pending |
| Logging Features | 6 | Pending |
| ECS-Specific Logging | 5 | Pending |
| Debug Mode | 3 | Pending |
| Profiling | 4 | Pending |
| Visualization | 3 | Pending |
| Test World Builder | 5 | Pending |
| Assertion Helpers | 6 | Pending |
| Mock Utilities | 4 | Pending |
| Snapshot Testing | 4 | Pending |
| Test Fixtures | 4 | Pending |
| **Subtotal** | **~52** | |

### Production & Advanced (Phases 16-17)
| Category | Features | Status |
|----------|----------|--------|
| Multi-World Support | 4 | Pending |
| Schema Evolution | 4 | Pending |
| Enhanced Save/Load | 11 | Pending |
| Network Sync | 5 | Pending |
| Parallelization | 5 | Pending |
| Native AOT | 4 | Pending |
| Advanced Spatial | 4 | Pending |
| Component Composition | 3 | Pending |
| **Subtotal** | **~40** | |

### Tooling (Phases 18-19)
| Category | Features | Status |
|----------|----------|--------|
| Replay Recording | 6 | Pending |
| Replay Playback | 5 | Pending |
| Timeline Controls | 5 | Pending |
| Playtesting Infrastructure | 5 | Pending |
| IDE Code Navigation | 3 | Pending |
| IDE Refactoring | 3 | Pending |
| IDE Visualization | 4 | Pending |
| IDE Debugging | 4 | Pending |
| **Subtotal** | **~35** | |

### Long-Term: Browser Editor
| Category | Features | Status |
|----------|----------|--------|
| Editor Frontend | 4 | Future |
| Scene Editor | 5 | Future |
| Inspector Panel | 6 | Future |
| Code Editor | 4 | Future |
| Backend Services | 4 | Future |
| Play Mode | 4 | Future |
| **Subtotal** | **~27** | |

### Grand Total: **~253 features**

---

## Design Principles

When implementing these features, maintain KeenEye's core principles:

1. **No Static State** - All state is per-world instance
2. **Respect User Intent** - Provide helpers, let users wire things up
3. **Performance First** - Zero/minimal allocations in hot paths
4. **Source Generators for Ergonomics** - Reduce boilerplate, maintain performance
5. **Components are Structs** - Cache-friendly value semantics
6. **Entities are IDs** - Lightweight `(int Id, int Version)` with staleness detection
