# KeenEyes Roadmap

This document outlines the features needed to achieve parity with [OrionECS](https://github.com/tyevco/OrionECS), the TypeScript ECS framework that KeenEyes reimplements in C#.

## Current Status

KeenEyes has basic ECS scaffolding in place:
- `World` container with isolated state
- `Entity` as `readonly record struct` with version tracking
- `EntityBuilder` with fluent `With<T>()` / `WithTag<T>()` API
- `IComponent` / `ITagComponent` marker interfaces
- `ComponentRegistry` with per-world component IDs
- `QueryBuilder` with `With<>()` / `Without<>()` filters
- `ISystem` / `SystemBase` with Initialize/Update lifecycle
- `SystemGroup` for grouped execution
- Source generator attributes (`[Component]`, `[System]`, `[Query]`)

---

## Phase 1: Core Completion (Foundation)

Essential features needed for basic usability.

### 1.1 Entity Operations
- [x] `World.Get<T>(entity)` - retrieve component data
- [x] `World.Set<T>(entity, value)` - update component data
- [x] `World.Add<T>(entity, component)` - add component to existing entity
- [x] `World.Remove<T>(entity)` - remove component from entity
- [x] `World.GetComponents(entity)` - get all components on entity
- [x] `World.Has<T>(entity)` - check component presence
- [x] Entity naming support (`World.Spawn(name)`, `World.GetEntityByName(name)`)

### 1.2 Singletons / Resources
World-level data not tied to entities (time, input, config, etc.):
- [x] `World.SetSingleton<T>(value)`
- [x] `World.GetSingleton<T>()`
- [x] `World.TryGetSingleton<T>(out T value)`
- [x] `World.HasSingleton<T>()`
- [x] `World.RemoveSingleton<T>()`

### 1.3 Command Buffer / Deferred Operations
Safe entity modification during iteration:
- [x] `CommandBuffer` class
- [x] Deferred entity creation (`CommandBuffer.Spawn()`)
- [x] Deferred component add/remove (`AddComponent`, `RemoveComponent`, `SetComponent`)
- [x] Deferred entity destruction (`CommandBuffer.Despawn()`)
- [x] `CommandBuffer.Flush(world)` to execute buffered commands

---

## Phase 2: Archetype Storage (Performance)

Replace dictionary-based storage with cache-friendly archetypes.

### 2.1 Archetype System
- [x] `Archetype` - component type combination storage
- [x] `ArchetypeChunk` - contiguous component arrays with fixed-size chunks
- [x] Archetype manager for efficient entity migrations
- [x] `ref T` returns for zero-copy component access

### 2.2 Object Pooling
- [x] Entity ID recycling (`EntityPool`)
- [x] Component array pooling (`ComponentArrayPool`)
- [x] Archetype chunk pooling (`ChunkPool`)
- [x] Memory usage statistics (`World.GetMemoryStats()`)

### 2.3 Query Caching
- [x] `QueryManager` for cached queries
- [x] Automatic cache invalidation on archetype changes
- [x] Query reuse across systems

---

## Phase 3: Entity Hierarchy

Parent-child relationships for scene graphs and transforms.

- [x] `World.SetParent(child, parent)`
- [x] `World.GetParent(entity)`
- [x] `World.GetChildren(entity)`
- [x] `World.AddChild(parent, child)`
- [x] `World.RemoveChild(parent, child)`
- [x] Cascading despawn (destroy children with parent)
- [x] Hierarchy traversal utilities

---

## Phase 4: Events & Change Tracking

Reactive patterns for responding to entity/component changes.

### 4.1 Event System
- [x] `World.OnComponentAdded<T>(handler)`
- [x] `World.OnComponentRemoved<T>(handler)`
- [x] `World.OnComponentChanged<T>(handler)`
- [x] `World.OnEntityCreated(handler)`
- [x] `World.OnEntityDestroyed(handler)`
- [x] General event bus for custom events

### 4.2 Change Tracking
- [x] `World.MarkDirty<T>(entity)`
- [x] `World.GetDirtyEntities<T>()`
- [x] `World.ClearDirtyFlags<T>()`
- [x] Automatic change detection option
- [x] Change tracking configuration

---

## Phase 5: System Enhancements

Better control over system execution.

### 5.1 Lifecycle Hooks
- [x] `OnBeforeUpdate()` - runs before entity processing
- [x] `OnAfterUpdate()` - runs after entity processing
- [x] `OnEnabled()` / `OnDisabled()` callbacks

### 5.2 Runtime Control
- [x] `system.Enabled` property
- [x] `World.EnableSystem<T>()` / `DisableSystem<T>()`
- [x] `World.GetSystem<T>()`

### 5.3 Execution Order
- [x] Priority-based system sorting (Phase and Order parameters)
- [x] `[RunBefore(typeof(OtherSystem))]` attribute
- [x] `[RunAfter(typeof(OtherSystem))]` attribute
- [x] Topological sorting with cycle detection
- [x] `World.FixedUpdate()` method for fixed timestep physics

---

## Phase 6: Plugin System

Extensibility for physics, networking, rendering, etc.

### 6.1 Plugin Infrastructure
- [x] `IWorldPlugin` interface
- [x] `PluginContext` for registration
- [x] Plugin metadata (name, version)
- [x] `Install()` / `Uninstall()` lifecycle

### 6.2 WorldBuilder Pattern
- [x] `WorldBuilder` class
- [x] `.WithPlugin<T>()` fluent API
- [x] `.WithFixedUpdateRate(fps)` configuration
- [x] `.WithDebugMode(enabled)` configuration
- [x] `.WithChangeTracking(mode)` configuration

### 6.3 Runtime Plugin Management
- [x] `World.InstallPlugin(plugin)`
- [x] `World.UninstallPlugin(name)`
- [x] `World.HasPlugin(name)`
- [x] `World.GetPlugin<T>()`
- [x] `World.GetInstalledPlugins()`

### 6.4 Extension API
- [x] `World.SetExtension<T>(api)` - plugin-provided APIs
- [x] `World.GetExtension<T>()`
- [x] Type-safe extension access

---

## Phase 7: Inter-System Messaging

Decoupled communication between systems.

- [x] `MessageManager` / `IMessageBus`
- [x] `World.Send<T>(message)`
- [x] `World.Subscribe<T>(handler)`
- [x] Unsubscription via `IDisposable` (EventSubscription)
- [x] Typed message channels
- [x] Message queuing for deferred delivery (`QueueMessage`, `ProcessQueuedMessages`)

---

## Phase 8: Prefabs & Templates

Entity templates for content creation.

- [x] `PrefabManager`
- [x] `World.RegisterPrefab(name, config)`
- [x] `World.SpawnFromPrefab(name)`
- [x] `World.SpawnFromPrefab(name, overrides)`
- [x] Prefab inheritance
- [x] Nested prefabs

---

## Phase 9: Entity Tags (String-based)

Dynamic categorization beyond type-safe `ITagComponent`.

- [x] `World.AddTag(entity, tagName)`
- [x] `World.RemoveTag(entity, tagName)`
- [x] `World.HasTag(entity, tagName)`
- [x] `World.GetTags(entity)`
- [x] Query filtering by tags (`.WithTag("enemy")`)
- [x] Query exclusion by tags (`.WithoutTag("dead")`)

---

## Phase 10: Component Validation

Development-time safety checks.

- [x] `[RequiresComponent(typeof(T))]` attribute
- [x] `[ConflictsWith(typeof(T))]` attribute
- [x] Custom validation delegates
- [x] Validation toggle (dev vs production)

---

## Phase 11: Serialization & Snapshots

Save/load world state.

- [x] `SnapshotManager`
- [x] `World.CreateSnapshot()` - capture world state
- [x] `World.RestoreSnapshot(snapshot)` - restore state
- [x] Entity serialization
- [x] Component serialization (opt-in via `[Component(Serializable = true)]`)
- [x] Binary and JSON format support

---

## Phase 12: Logging System

Pluggable logging with multiple providers.

### 12.1 Log Provider Architecture
- [x] `ILogProvider` interface
- [x] `LogManager` for provider registration
- [x] Multiple simultaneous providers
- [x] Provider-specific configuration

### 12.2 Built-in Providers
- [x] Console log provider (default)
- [x] File log provider
- [x] Debug output provider (IDE integration)
- [x] Null provider (disable logging)

### 12.3 Logging Features
- [x] Log levels (Trace, Debug, Info, Warning, Error, Fatal)
- [x] Log filtering by level and category
- [x] Structured logging with properties
- [x] Log scopes for context
- [x] Timestamp and source formatting
- [x] Performance-friendly (zero overhead when disabled)

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

## Phase 15: Manager Architecture

Refactor `World` into specialized managers for better separation of concerns.
See [ADR-001](docs/adr/001-world-manager-architecture.md) for the full decision record.

**Status:** In Progress (Issue [#82](https://github.com/orion-ecs/keen-eye/issues/82))

### Current State

The `World` class has grown to 3,000+ lines with 10+ responsibilities, violating SRP.

### Target Architecture

```
World (facade, ~300-400 lines)
├── HierarchyManager      - Parent-child entity relationships
├── SystemManager         - System registration, ordering, execution
├── PluginManager         - Plugin lifecycle
├── SingletonManager      - Global resource storage
├── ExtensionManager      - Plugin-provided APIs
├── ArchetypeManager      - (existing) Component storage
├── QueryManager          - (existing) Query caching
└── ComponentRegistry     - (existing) Component type registry
```

### Extraction Order

- [x] Extract HierarchyManager (~676 lines)
- [x] Extract SystemManager (~365 lines)
- [x] Extract PluginManager (~169 lines)
- [x] Extract SingletonManager (~209 lines)
- [x] Extract ExtensionManager (~107 lines)
- [x] Reduce World to thin facade (~300-400 lines)

### Constraints

- Managers are `internal` (not public API)
- `World` remains the single entry point
- Public API unchanged - no breaking changes
- Unit tests added for each manager before extraction

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
- [x] Entity replication
- [x] Component sync strategies (Authoritative, Interpolated, Predicted, OwnerAuthoritative)
- [x] Client-side prediction
- [x] Server reconciliation
- [x] Network plugin foundation (NetworkServerPlugin, NetworkClientPlugin)
- [x] Delta compression for bandwidth optimization
- [x] Entity hierarchy replication
- [x] Ownership transfer
- [x] Late-joiner full snapshots

---

## Phase 17: Advanced Performance

Maximum performance optimizations.

### 17.1 Parallelization
- [x] Parallel system execution
- [x] Job system integration
- [x] Thread-safe command buffers
- [x] Parallel query iteration
- [x] Dependency graph for safe parallelism

### 17.2 Native AOT Support
- [x] Trimming-safe APIs
- [x] Source generator compatibility with AOT
- [x] Reflection-free serialization
- [x] Benchmark AOT vs JIT performance (tracked in #366)

### 17.3 Advanced Spatial Partitioning
- [x] Configurable grid sizes
- [x] Hierarchical spatial structures
- [x] Broadphase/narrowphase separation
- [x] Spatial query optimization

### 17.4 Component Composition
- [x] Component inheritance/mixins
- [x] Component bundles (group common components)
- [x] `[Bundle]` attribute for source generation

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

A full-featured browser-based game editor like Unity/Godot, powered by KeenEyes.

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

Optional packages that extend KeenEyes:

### Implemented Plugins

| Package | Purpose | Status |
|---------|---------|--------|
| `KeenEyes.Spatial` | Quadtree/octree spatial partitioning | ✅ Complete |
| `KeenEyes.Physics` | BepuPhysics integration | ✅ Complete |
| `KeenEyes.Persistence` | Serialization with encryption | ✅ Complete |
| `KeenEyes.Parallelism` | Parallel system execution | ✅ Complete |
| `KeenEyes.Graphics` | Silk.NET OpenGL rendering | ✅ Complete |
| `KeenEyes.Logging` | Pluggable logging providers | ✅ Complete |

### Planned Plugins (Milestones Created)

| Package | Purpose | Milestone |
|---------|---------|-----------|
| `KeenEyes.Graphics.Abstractions` | Graphics contracts (IRenderer, IRenderPipeline) | #14 |
| `KeenEyes.Input.Abstractions` | Input contracts (IInputSource, IInputManager) | #14 |
| `KeenEyes.Input` | Silk.NET input implementation | #14 |
| `KeenEyes.UI` | Retained-mode UI as ECS entities | #15 |
| `KeenEyes.Audio` | OpenAL audio via Silk.NET | #16 |
| `KeenEyes.Particles` | High-performance particle systems | #18 |
| `KeenEyes.Assets` | Reference-counted asset management | #19 |
| `KeenEyes.Animation` | Sprite, skeletal, and property animation | #20 |
| `KeenEyes.AI` | State machines, behavior trees, utility AI | #21 |

### Research Required (See docs/research/engine-systems-roadmap.md)

| Package | Purpose | Status |
|---------|---------|--------|
| `KeenEyes.Navigation` | Pathfinding & NavMesh | Research Issue #430 |
| `KeenEyes.Scenes` | Scene management & streaming | Research Issue #431 |
| `KeenEyes.Localization` | Multi-language text & assets | Research Issue #432 |
| `KeenEyes.Network` | Multiplayer/networking plugin | ✅ Complete |

### Additional Plugins from OrionECS
| Plugin | Purpose |
|--------|---------|
| `KeenEyes.Canvas2D` | 2D canvas rendering |
| `KeenEyes.Interaction` | User interaction systems |
| `KeenEyes.Budgets` | Resource budget management |
| `KeenEyes.ComponentPropagation` | Parent-to-child component inheritance |
| `KeenEyes.TransformPropagation` | Automatic transform inheritance |

---

## Feature Count Summary

### Engine Core (Phases 1-11)
| Category | Features | Status |
|----------|----------|--------|
| Core Entity Operations | 7 | ✅ Complete |
| Singletons/Resources | 5 | ✅ Complete |
| Command Buffer | 5 | ✅ Complete |
| Archetype Storage | 8 | ✅ Complete |
| Query Caching | 3 | ✅ Complete |
| Entity Hierarchy | 7 | ✅ Complete |
| Event System | 6 | ✅ Complete |
| Change Tracking | 5 | ✅ Complete |
| System Enhancements | 12 | ✅ Complete |
| Plugin System | 15 | ✅ Complete |
| Inter-System Messaging | 6 | ✅ Complete |
| Prefabs | 6 | ✅ Complete |
| Entity Tags | 6 | ✅ Complete |
| Component Validation | 4 | ✅ Complete |
| Serialization | 6 | ✅ Complete |
| **Subtotal** | **~99** | ✅ Complete |

### Developer Experience (Phases 12-14)
| Category | Features | Status |
|----------|----------|--------|
| Log Provider Architecture | 4 | ✅ Complete |
| Built-in Log Providers | 4 | ✅ Complete |
| Logging Features | 6 | ✅ Complete |
| ECS-Specific Logging | 5 | Pending |
| Debug Mode | 3 | Pending |
| Profiling | 4 | Pending |
| Visualization | 3 | Pending |
| Test World Builder | 5 | Pending |
| Assertion Helpers | 6 | Pending |
| Mock Utilities | 4 | Pending |
| Snapshot Testing | 4 | Pending |
| Test Fixtures | 4 | Pending |
| **Subtotal** | **~52** | Partial |

### Production & Advanced (Phases 16-17)
| Category | Features | Status |
|----------|----------|--------|
| Multi-World Support | 4 | Pending |
| Schema Evolution | 4 | Pending |
| Enhanced Save/Load | 11 | Partial (KeenEyes.Persistence) |
| Network Sync | 9 | ✅ Complete (KeenEyes.Network) |
| Parallelization | 5 | ✅ Complete |
| Native AOT | 4 | ✅ Complete |
| Advanced Spatial | 4 | ✅ Complete |
| Component Composition | 3 | ✅ Complete |
| **Subtotal** | **~40** | Partial |

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

When implementing these features, maintain KeenEyes's core principles:

1. **No Static State** - All state is per-world instance
2. **Respect User Intent** - Provide helpers, let users wire things up
3. **Performance First** - Zero/minimal allocations in hot paths
4. **Source Generators for Ergonomics** - Reduce boilerplate, maintain performance
5. **Components are Structs** - Cache-friendly value semantics
6. **Entities are IDs** - Lightweight `(int Id, int Version)` with staleness detection

---

## Source Generator Opportunities

Source generators can eliminate boilerplate, enable AOT compatibility, and provide compile-time safety. Below are all features that could benefit from source generation.

### Already Planned (from CLAUDE.md)

| Attribute | Generates |
|-----------|-----------|
| `[Component]` | `WithComponentName(...)` fluent builder methods |
| `[TagComponent]` | Parameterless `WithTagName()` methods |
| `[System]` | Metadata properties (Phase, Order, Group) |
| `[Query]` | Efficient query iterators |

### Core ECS Enhancements

#### Component Registration (Phase 1)
```csharp
[Component]
public partial struct Position { public float X, Y; }

// Generates:
// - ComponentId constant
// - WithPosition(float x, float y) extension on EntityBuilder
// - Ref accessor helpers
```

#### Singleton Accessors (Phase 1)
```csharp
[Singleton]
public partial class GameTime { public float Delta; public float Total; }

// Generates:
// - world.GameTime property (typed accessor)
// - Compile-time existence check
```

#### Query Optimization (Phase 2)
```csharp
[Query]
public partial struct MovementQuery : IQuery<Position, Velocity> { }

// Generates:
// - Specialized enumerator for this component combination
// - Archetype-aware iteration
// - Inline component access (no dictionary lookups)
```

### Validation & Safety

#### Component Dependencies (Phase 10)
```csharp
[Component]
[RequiresComponent(typeof(Transform))]
[ConflictsWith(typeof(StaticBody))]
public partial struct RigidBody { }

// Generates:
// - Compile-time warnings if dependencies not added
// - Runtime validation in debug mode
// - EntityBuilder guards
```

#### Entity Tags (Phase 9)
```csharp
[Tags]
public enum EntityTags { Player, Enemy, Projectile, Pickup }

// Generates:
// - Type-safe tag constants
// - .WithTag(EntityTags.Player) overload
// - .HasTag(EntityTags.Enemy) overload
// - Query: .WithTag(EntityTags.Player)
```

### Serialization & Networking

#### Component Serialization (Phase 11)
```csharp
[Component(Serializable = true)]
public partial struct Health { public int Current; public int Max; }

// Generates:
// - Binary serializer/deserializer (reflection-free)
// - JSON serializer (optional)
// - Schema version tracking
// - AOT-compatible serialization
```

#### Network Replication (Phase 16)
```csharp
[Component]
[Replicated(Interpolated = true)]
public partial struct NetworkTransform { public float X, Y, Rotation; }

// Generates:
// - Delta compression serializer
// - Interpolation helpers
// - Ownership/authority checks
// - Bandwidth estimation
```

#### Schema Migration (Phase 16)
```csharp
[Component(Version = 2)]
[MigrateFrom(typeof(HealthV1), nameof(MigrateFromV1))]
public partial struct Health { public int Current; public int Max; public int Shield; }

// Generates:
// - Version detection during deserialization
// - Automatic migration pipeline
// - Backward compatibility layer
```

### Systems & Execution

#### System Dependencies (Phase 5)
```csharp
[System(Phase = SystemPhase.Update, Order = 10)]
[RunAfter(typeof(InputSystem))]
[RunBefore(typeof(RenderSystem))]
[WriteComponents(typeof(Position))]
[ReadComponents(typeof(Velocity))]
public partial class MovementSystem : SystemBase { }

// Generates:
// - Dependency graph edges
// - Parallelization safety analysis
// - Automatic system ordering
```

#### System Queries (Phase 5)
```csharp
public partial class MovementSystem : SystemBase
{
    [Query]
    private partial IEnumerable<(Entity, ref Position, ref readonly Velocity)> GetMovables();
}

// Generates:
// - Optimized query method implementation
// - Cached query descriptor
// - Read/write component tracking
```

### Component Bundles (Phase 17)
```csharp
[Bundle]
public partial struct TransformBundle
{
    public Position Position;
    public Rotation Rotation;
    public Scale Scale;
}

// Generates:
// - .WithTransformBundle(pos, rot, scale) on EntityBuilder
// - Bundle-aware archetype optimization
// - Shorthand query: Query<TransformBundle>()
```

### Events & Change Tracking

#### Reactive Components (Phase 4)
```csharp
[Component]
[TrackChanges]
public partial struct Health { public int Current; public int Max; }

// Generates:
// - Automatic dirty flag management
// - OnHealthChanged event hookup
// - Previous value tracking (optional)
```

#### Event Handlers (Phase 4)
```csharp
public partial class DamageSystem : SystemBase
{
    [OnComponentChanged(typeof(Health))]
    private partial void HandleHealthChanged(Entity entity, Health oldValue, Health newValue);
}

// Generates:
// - Event subscription in OnInitialize
// - Unsubscription in Dispose
// - Type-safe handler wiring
```

### Prefabs & Templates

#### Prefab Definition (Phase 8)
```csharp
[Prefab("Enemy/Goblin")]
public partial struct GoblinPrefab
{
    public Position Position;
    public Health Health = new() { Current = 100, Max = 100 };
    public Enemy EnemyTag;
}

// Generates:
// - Prefab registration on world startup
// - world.SpawnGoblin(position) typed method
// - Override support: world.SpawnGoblin(position, health: new(...))
```

### Testing Utilities (Phase 14)

#### Test Assertions
```csharp
[Component]
public partial struct Health { public int Current; public int Max; }

// Generates (in KeenEyes.Testing):
// - entity.ShouldHaveHealth()
// - entity.ShouldHaveHealth(h => h.Current > 0)
// - health.ShouldEqual(expected)
```

### Logging & Diagnostics

#### Log Categories (Phase 12)
```csharp
[LogCategory]
public static partial class EcsLogs
{
    public static partial void EntityCreated(Entity entity);
    public static partial void ComponentAdded<T>(Entity entity);
    public static partial void SystemExecuted(string name, double ms);
}

// Generates:
// - High-performance logging methods
// - Structured log parameters
// - Compile-time log level filtering
// - Zero allocation when disabled
```

### Editor & Inspector (Long-term)

#### Property Metadata
```csharp
[Component]
public partial struct EnemyConfig
{
    [Range(0, 100)]
    [Tooltip("Movement speed in units per second")]
    public float Speed;

    [Header("Combat")]
    [Min(1)]
    public int Damage;

    [ColorPicker]
    public uint TintColor;
}

// Generates:
// - Inspector UI descriptors
// - Validation code
// - Default value initialization
// - Undo/redo support metadata
```

### Summary: Generator-Enhanced Attributes

| Attribute | Phase | Purpose |
|-----------|-------|---------|
| `[Component]` | 1 | Builder methods, registration |
| `[TagComponent]` | 1 | Zero-size tag helpers |
| `[Singleton]` | 1 | Typed singleton accessors |
| `[Query]` | 2 | Optimized query iterators |
| `[Bundle]` | 17 | Component grouping |
| `[System]` | 5 | Metadata, ordering |
| `[RunBefore]` / `[RunAfter]` | 5 | System dependencies |
| `[ReadComponents]` / `[WriteComponents]` | 5 | Parallelization safety |
| `[RequiresComponent]` | 10 | Validation |
| `[ConflictsWith]` | 10 | Validation |
| `[TrackChanges]` | 4 | Change detection |
| `[OnComponentChanged]` | 4 | Event handlers |
| `[Serializable]` | 11 | Binary/JSON serialization |
| `[Replicated]` | 16 | Network sync |
| `[MigrateFrom]` | 16 | Schema evolution |
| `[Prefab]` | 8 | Entity templates |
| `[Tags]` | 9 | Type-safe string tags |
| `[LogCategory]` | 12 | Structured logging |
| `[Range]`, `[Tooltip]`, etc. | Editor | Inspector metadata |

### Implementation Priority

**High Value, Lower Complexity:**
1. `[Component]` enhancements (builder methods) - already started
2. `[Query]` optimized iterators - major perf win
3. `[Serializable]` reflection-free serialization - AOT requirement
4. `[LogCategory]` high-perf logging - common need

**High Value, Higher Complexity:**
5. `[System]` with dependency analysis - enables parallelization
6. `[TrackChanges]` reactive components - common pattern
7. `[Prefab]` typed spawning - great DX
8. `[Replicated]` network code gen - significant boilerplate reduction

**Future/Editor:**
9. Inspector metadata attributes - editor-dependent
10. `[MigrateFrom]` schema evolution - production need

---

## C#/.NET Features to Leverage

Modern .NET provides powerful features for building high-performance ECS frameworks. This section maps relevant .NET features to each roadmap phase.

### Phase 1: Core Completion

| Feature | Usage |
|---------|-------|
| `ref` returns | Zero-copy component access: `ref T Get<T>(Entity e)` |
| `in` parameters | Read-only refs for queries: `in Position pos` |
| `Span<T>` | Stack-allocated component lists in builders |
| `readonly record struct` | Entity type: `readonly record struct Entity(int Id, int Version)` |
| Generic constraints | `where T : struct, IComponent` |
| `Unsafe.SizeOf<T>()` | Component size calculation without boxing |
| `CollectionsMarshal.GetValueRefOrAddDefault` | Fast dictionary access |

```csharp
// Example: Zero-copy component access
public ref T Get<T>(Entity entity) where T : struct, IComponent
{
    ref var storage = ref CollectionsMarshal.GetValueRefOrNullRef(components, entity.Id);
    return ref Unsafe.As<byte, T>(ref storage[offset]);
}
```

### Phase 2: Archetype Storage

| Feature | Usage |
|---------|-------|
| `Span<T>` / `Memory<T>` | Contiguous component storage views |
| `ArrayPool<T>.Shared` | Pooled archetype chunk allocation |
| `Unsafe.As<TFrom, TTo>()` | Type punning for generic component access |
| `MemoryMarshal.Cast<T1, T2>()` | Reinterpret component arrays |
| `NativeMemory.AlignedAlloc()` | Cache-aligned allocations |
| `[SkipLocalsInit]` | Avoid zeroing large arrays |
| `[InlineArray(N)]` | Fixed-size inline component storage (C# 12) |

```csharp
// Example: Archetype chunk with pooled storage
public sealed class ArchetypeChunk
{
    private byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

    public Span<T> GetComponentSpan<T>(int offset, int count) where T : struct
        => MemoryMarshal.Cast<byte, T>(buffer.AsSpan(offset, count * Unsafe.SizeOf<T>()));
}
```

### Phase 3: Entity Hierarchy

| Feature | Usage |
|---------|-------|
| `Span<Entity>` | Return children without allocation |
| `stackalloc` | Small child lists on stack |
| `ValueListBuilder<T>` | Growing lists without heap allocation |

### Phase 4: Events & Change Tracking

| Feature | Usage |
|---------|-------|
| `Action<T>` / `Func<T>` | Lightweight event delegates |
| `WeakReference<T>` | Prevent memory leaks in event subscriptions |
| `ConditionalWeakTable<K,V>` | Associate change tracking data without modifying types |
| `[Flags] enum` | Dirty flag bitfields |

```csharp
// Example: Weak event subscription
public sealed class EventBus<T>
{
    private readonly List<WeakReference<Action<T>>> handlers = [];

    public void Subscribe(Action<T> handler)
        => handlers.Add(new WeakReference<Action<T>>(handler));
}
```

### Phase 5: System Enhancements

| Feature | Usage |
|---------|-------|
| `IComparer<T>` | System priority sorting |
| `TopologicalSort` | Dependency-based ordering |
| `Lazy<T>` | Deferred system initialization |
| `[CallerMemberName]` | Automatic system naming |

### Phase 6: Plugin System

| Feature | Usage |
|---------|-------|
| `AssemblyLoadContext` | Plugin isolation and unloading |
| `Type.GetType()` with assembly | Cross-assembly type resolution |
| `Activator.CreateInstance<T>()` | Plugin instantiation |
| Generic interfaces | `IWorldPlugin`, `IPluginContext` |

### Phase 7: Inter-System Messaging

| Feature | Usage |
|---------|-------|
| `System.Threading.Channels` | High-perf async message queues |
| `Channel<T>.CreateUnbounded()` | Lock-free message passing |
| `ConcurrentQueue<T>` | Simple thread-safe queues |
| `IObservable<T>` / `IObserver<T>` | Reactive message patterns |

```csharp
// Example: High-performance message bus
public sealed class MessageBus
{
    private readonly Channel<object> channel = Channel.CreateUnbounded<object>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
}
```

### Phase 8: Prefabs & Templates

| Feature | Usage |
|---------|-------|
| `Expression<T>` | Compile prefab factories at runtime |
| `Delegate.CreateDelegate()` | Fast delegate creation |
| `FrozenDictionary<K,V>` | Immutable prefab registry (.NET 8+) |

### Phase 9: Entity Tags

| Feature | Usage |
|---------|-------|
| `FrozenSet<string>` | Immutable tag lookups (.NET 8+) |
| `StringComparer.Ordinal` | Fast string comparison |
| `string.GetHashCode(StringComparison)` | Consistent hashing |
| `HashSet<string>` | Tag storage per entity |

### Phase 10: Component Validation

| Feature | Usage |
|---------|-------|
| Roslyn Analyzers | Compile-time validation rules |
| `DiagnosticDescriptor` | Custom compiler warnings |
| `[Conditional("DEBUG")]` | Debug-only validation |

### Phase 11: Serialization

| Feature | Usage |
|---------|-------|
| `System.Text.Json` source generators | AOT-friendly JSON |
| `JsonSerializerContext` | Compile-time serialization |
| `BinaryReader` / `BinaryWriter` | Fast binary I/O |
| `IBufferWriter<byte>` | Zero-copy serialization |
| `MemoryPack` (library) | Fastest binary serializer |
| `ReadOnlySpan<byte>` | Zero-copy deserialization |

```csharp
// Example: Source-generated JSON serialization
[JsonSerializable(typeof(WorldSnapshot))]
[JsonSourceGenerationOptions(WriteIndented = false)]
public partial class SnapshotJsonContext : JsonSerializerContext { }
```

### Phase 12: Logging

| Feature | Usage |
|---------|-------|
| `Microsoft.Extensions.Logging` | Standard logging abstraction |
| `LoggerMessage.Define()` | High-perf cached log delegates |
| `[LoggerMessage]` attribute | Source-generated logging (.NET 6+) |
| `ILogger<T>` | Category-based logging |
| `EventSource` | ETW tracing on Windows |

```csharp
// Example: High-performance logging
public static partial class EcsLoggers
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Entity {EntityId} created")]
    public static partial void EntityCreated(ILogger logger, int entityId);
}
```

### Phase 13: Debugging & Profiling

| Feature | Usage |
|---------|-------|
| `Stopwatch.GetTimestamp()` | High-resolution timing |
| `Stopwatch.GetElapsedTime()` | Allocation-free elapsed time (.NET 7+) |
| `DiagnosticSource` | Structured diagnostics |
| `Activity` | Distributed tracing |
| `EventCounter` | Performance counters |
| `GC.GetAllocatedBytesForCurrentThread()` | Per-thread allocation tracking |
| `[DebuggerDisplay]` | Better debugger experience |
| `[DebuggerTypeProxy]` | Custom debugger views |

```csharp
// Example: Zero-allocation timing
public readonly struct SystemTimer
{
    private readonly long start = Stopwatch.GetTimestamp();
    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(start);
}
```

### Phase 14: Testing

| Feature | Usage |
|---------|-------|
| `xUnit` / `NUnit` | Test frameworks |
| `[Theory]` / `[InlineData]` | Parameterized tests |
| `Verify` (library) | Snapshot testing |
| `NSubstitute` / `Moq` | Mocking |
| `BenchmarkDotNet` | Performance regression tests |
| `ITestOutputHelper` | Test logging |

### Phase 16: Production Readiness

| Feature | Usage |
|---------|-------|
| Native AOT | Ahead-of-time compilation |
| `[DynamicallyAccessedMembers]` | Trimming hints |
| `[RequiresUnreferencedCode]` | Mark reflection-heavy APIs |
| `[UnconditionalSuppressMessage]` | Suppress false trimming warnings |
| `PublishAot=true` | Enable AOT publishing |
| `JsonSerializer` with context | AOT-safe JSON |
| Source generators | Reflection-free code gen |

```xml
<!-- Example: AOT-compatible project settings -->
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <TrimMode>full</TrimMode>
  <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
</PropertyGroup>
```

### Phase 17: Advanced Performance

| Feature | Usage |
|---------|-------|
| `Vector<T>` / `Vector128<T>` | SIMD operations |
| `Parallel.ForEach()` | Parallel iteration |
| `IThreadPoolWorkItem` | Custom thread pool work |
| `ThreadPool.UnsafeQueueUserWorkItem()` | Low-overhead scheduling |
| `SpinLock` / `SpinWait` | Lock-free synchronization |
| `Interlocked.*` | Atomic operations |
| `[MethodImpl(AggressiveInlining)]` | Force inlining |
| `[MethodImpl(AggressiveOptimization)]` | Tier-1 JIT immediately |
| Hardware intrinsics | `System.Runtime.Intrinsics` |

```csharp
// Example: SIMD-accelerated position update
public static void UpdatePositions(Span<Position> positions, Span<Velocity> velocities, float dt)
{
    var dtVec = Vector256.Create(dt);
    for (int i = 0; i <= positions.Length - Vector256<float>.Count; i += Vector256<float>.Count)
    {
        var pos = Vector256.LoadUnsafe(ref Unsafe.As<Position, float>(ref positions[i]));
        var vel = Vector256.LoadUnsafe(ref Unsafe.As<Velocity, float>(ref velocities[i]));
        var result = Vector256.FusedMultiplyAdd(vel, dtVec, pos);
        result.StoreUnsafe(ref Unsafe.As<Position, float>(ref positions[i]));
    }
}
```

### Phase 18: Deterministic Replay

| Feature | Usage |
|---------|-------|
| `BinaryReader` / `BinaryWriter` | Efficient binary I/O |
| `GZipStream` / `BrotliStream` | Compression |
| `RandomNumberGenerator` | Secure seed generation |
| `System.Random` with seed | Deterministic randomness |
| `FileStream` with `FileOptions.Asynchronous` | Async file I/O |
| `PipeReader` / `PipeWriter` | High-perf streaming |

### Phase 19: IDE Extension

| Feature | Usage |
|---------|-------|
| Roslyn `IIncrementalGenerator` | Incremental source generation |
| `Microsoft.CodeAnalysis` | Code analysis APIs |
| `DiagnosticAnalyzer` | Custom warnings/errors |
| `CodeFixProvider` | Automated code fixes |
| `ISourceGenerator` | Legacy source generation |
| Language Server Protocol | IDE-agnostic tooling |

### General Best Practices

| Practice | Description |
|----------|-------------|
| `readonly struct` | Prevents defensive copies |
| `record struct` | Value equality + deconstruction |
| `init` properties | Immutable after construction |
| `required` members | Enforce initialization (C# 11) |
| `file` scoped types | Reduce namespace pollution (C# 11) |
| Collection expressions | `[1, 2, 3]` syntax (C# 12) |
| Primary constructors | Less boilerplate (C# 12) |
| `params Span<T>` | Stack-allocated varargs (C# 13) |
| `allows ref struct` | Generic ref struct support (C# 13) |
| `Lock` object | Better than `object` for locking (.NET 9) |

### .NET Version Requirements

| Feature | Minimum Version |
|---------|-----------------|
| `Span<T>`, `Memory<T>` | .NET Core 2.1 |
| Default interface methods | .NET Core 3.0 |
| Source generators | .NET 5.0 |
| `LoggerMessage` attribute | .NET 6.0 |
| `Stopwatch.GetElapsedTime()` | .NET 7.0 |
| `FrozenDictionary/Set` | .NET 8.0 |
| Native AOT (production) | .NET 8.0 |
| `params Span<T>` | .NET 9.0 |
| `Lock` class | .NET 9.0 |
| **KeenEyes Target** | **.NET 10** |
