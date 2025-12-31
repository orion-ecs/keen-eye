# Framework Editor Feasibility - Research Report

**Date:** December 2024 (Updated: December 2024)
**Author:** Claude (Anthropic)
**Purpose:** Evaluate the feasibility of building a rudimentary editor for the KeenEyes ECS framework, including live entity editing, debugging/diagnostics, and hot reload capabilities

## Executive Summary

Building a rudimentary editor for KeenEyes is **highly feasible**. The framework now provides an extraordinarily comprehensive set of foundational pieces including: plugin system, entity inspection APIs, change tracking, event subscriptions, inter-system messaging, **complete debugging plugin** (profiler, entity inspector, memory/GC tracking), **parallelism with dependency visualization**, **save/load with delta saves and encryption**, **physics integration**, **spatial partitioning**, **binary serialization**, **multi-world support**, **deterministic RNG**, and **global system hooks**.

**Recommended approach:** Build the editor as a set of modular plugins that can be installed into any World instance. This leverages the existing plugin architecture and maintains the framework's core principle of per-world isolation.

| Capability | Current Status | Effort to Complete |
|------------|----------------|-------------------|
| Plugin Architecture | ✅ Complete | Ready to use |
| Entity Inspection | ✅ Complete | Ready to use |
| Change Tracking | ✅ Complete | Ready to use |
| Event System | ✅ Complete | Ready to use |
| Inter-System Messaging | ✅ Complete | Ready to use |
| Memory Statistics | ✅ Complete | Ready to use |
| Serialization & Snapshots | ✅ Complete | Ready to use |
| Prefabs/Templates | ✅ Complete | Ready to use |
| String-Based Tags | ✅ Complete | Ready to use |
| Pluggable Logging | ✅ Complete | Ready to use |
| Component Validation | ✅ Complete | Ready to use |
| Testing Utilities | ✅ Complete | Ready to use |
| Graphics Plugin | ✅ Complete | Reference implementation |
| **Debugging Plugin** | ✅ Complete | Profiler, Inspector, Memory/GC tracking |
| **Parallelism Plugin** | ✅ Complete | Parallel scheduler, profiler, dependency graphs |
| **Save/Load System** | ✅ Complete | Full/delta saves, encryption, compression |
| **Physics (BepuPhysics)** | ✅ Complete | 3D rigid body simulation |
| **Spatial Partitioning** | ✅ Complete | Grid, Quadtree, Octree, frustum culling |
| **Binary Serialization** | ✅ Complete | 50-80% smaller, AOT-compatible |
| **Multi-World Support** | ✅ Complete | World ID/Name, full isolation |
| **Deterministic RNG** | ✅ Complete | Per-world seeded random |
| **Global System Hooks** | ✅ Complete | Before/after hooks for profiling |
| **Component Bundles** | ✅ Complete | Generated bundle methods |
| **Component Mixins** | ✅ Complete | Compile-time field composition |
| **Project Templates** | ✅ Complete | `dotnet new keeneyes-game` |
| Undo/Redo | ❌ Missing | Medium - build on snapshots |
| Hot Reload | ❌ Missing | High - AssemblyLoadContext |
| Per-Field Inspection | ❌ Missing | Low - reflection wrapper |

> **Note:** The framework has seen massive development since initial research. The **KeenEyes.Debugging** plugin provides complete profiling and inspection. The **KeenEyes.Parallelism** plugin includes dependency graph generation. Save/load now supports delta saves with encryption. The framework provides **~95% of the infrastructure** needed for a full editor. Only undo/redo and hot reload remain.

---

## Existing Framework Capabilities

### Plugin System

**Location:** `src/KeenEyes.Core/PluginManager.cs`, `src/KeenEyes.Core/Plugins/`

The plugin system enables modular, per-world functionality through the `IWorldPlugin` interface. Each world has independent plugin instances, enabling editor/game separation.

**Key APIs:**
- `World.InstallPlugin<T>()` / `World.UninstallPlugin<T>()`
- `PluginContext.AddSystem<T>()` - Tracked system registration with auto-cleanup
- `PluginContext.SetExtension<T>()` - Expose custom APIs
- `World.GetPlugin<T>()` / `World.HasPlugin<T>()`

**Example:**
```csharp
public class EditorPlugin : IWorldPlugin
{
    public string Name => "Editor.Core";

    public void Install(PluginContext context)
    {
        var editorApi = new EditorAPI(context.World);
        context.SetExtension(editorApi);
        context.AddSystem<InspectorSystem>(SystemPhase.PostRender);
    }

    public void Uninstall(PluginContext context) { }
}
```

### Entity Inspection

**Location:** `src/KeenEyes.Core/World.cs` (lines 244-660)

The framework provides comprehensive entity inspection APIs suitable for editor use.

**Available APIs:**
| Method | Purpose |
|--------|---------|
| `GetAllEntities()` | Enumerate all entities in world |
| `GetComponents(entity)` | Get all components as `(Type, object)` tuples |
| `GetName(entity)` | Get entity's debug name |
| `GetEntityByName(name)` | O(1) lookup by name |
| `Has<T>(entity)` | Check component presence |
| `Get<T>(entity)` | Access component by reference |
| `Set<T>(entity, value)` | Update component (triggers events) |
| `Add<T>(entity, value)` | Add component dynamically |
| `Remove<T>(entity)` | Remove component dynamically |
| `IsAlive(entity)` | Check entity validity |

**Example:**
```csharp
// List all entities with their components
foreach (var entity in world.GetAllEntities())
{
    var name = world.GetName(entity) ?? $"Entity_{entity.Id}";
    Console.WriteLine($"{name}:");

    foreach (var (type, value) in world.GetComponents(entity))
    {
        Console.WriteLine($"  {type.Name}: {value}");
    }
}
```

### Event System

**Location:** `src/KeenEyes.Core/EventManager.cs`, `src/KeenEyes.Core/Events/`

The event system enables reactive UI updates when entities or components change.

**Component Lifecycle Events:**
```csharp
world.OnComponentAdded<T>(Action<Entity, T>)
world.OnComponentRemoved<T>(Action<Entity>)
world.OnComponentChanged<T>(Action<Entity, T, T>)  // old, new values
```

**Entity Lifecycle Events:**
```csharp
world.OnEntityCreated(Action<Entity, string?>)  // Entity and optional name
world.OnEntityDestroyed(Action<Entity>)
```

**Custom Event Bus:**
```csharp
var subscription = world.Events.Subscribe<MyEvent>(handler);
world.Events.Publish(in myEvent);
world.Events.HasHandlers<MyEvent>();  // Optimization check
```

### Change Tracking

**Location:** `src/KeenEyes.Core/Events/ChangeTracker.cs`

Per-component-type dirty tracking for detecting modified entities.

**APIs:**
```csharp
world.MarkDirty<Position>(entity)           // Manual marking
world.IsDirty<Position>(entity)             // Check single entity
world.GetDirtyEntities<Position>()          // Get all dirty entities
world.GetDirtyCount<Position>()             // Count for statistics
world.ClearDirtyFlags<Position>()           // Reset after processing
world.EnableAutoTracking<Position>()        // Auto-mark on Set<T>()
```

**Editor Use Cases:**
- Detect modified entities for save prompts
- Undo/redo: Know which entities changed per transaction
- Network sync: Only replicate changed components
- UI refresh: Update inspector only for dirty entities

### Memory Statistics

**Location:** `src/KeenEyes.Core/Pooling/MemoryStats.cs`

Comprehensive memory and performance statistics already available.

**Available Metrics:**
| Metric | Description |
|--------|-------------|
| `ActiveEntityCount` | Currently alive entities |
| `TotalAllocatedEntities` | Total ever created |
| `RecycledEntityCount` | Reused entity IDs |
| `RecycleEfficiency` | Recycling percentage |
| `ArchetypeCount` | Number of unique archetypes |
| `ComponentTypeCount` | Registered component types |
| `SystemCount` | Registered systems |
| `QueryCacheCount` | Cached queries |
| `QueryCacheHitRate` | Cache efficiency |
| `EstimatedComponentStorageBytes` | Memory estimate |

**Usage:**
```csharp
var stats = world.GetMemoryStats();
Console.WriteLine(stats.ToString());  // Human-readable output
```

### Hierarchy Support

**Location:** `src/KeenEyes.Core/HierarchyManager.cs`

Full parent-child relationship management for scene graphs.

**APIs:**
```csharp
world.SetParent(child, parent)
world.GetParent(entity)
world.GetChildren(entity)
world.GetDescendants(entity)    // All descendants recursively
world.GetAncestors(entity)      // All ancestors to root
world.GetRoot(entity)           // Top-most ancestor
world.DespawnRecursive(entity)  // Despawn with children
```

### Debugging Plugin (NEW)

**Location:** `src/KeenEyes.Debugging/`

The framework now includes a complete debugging plugin providing profiling, entity inspection, memory tracking, and GC monitoring—ready for editor integration.

**Key Classes:**
| Class | Purpose |
|-------|---------|
| `EntityInspector` | Runtime entity introspection |
| `Profiler` | System timing profiler |
| `MemoryTracker` | Memory usage tracking |
| `GCTracker` | Per-system GC allocation tracking |

**Usage:**
```csharp
// Install the debugging plugin
world.InstallPlugin<DebugPlugin>();

// Get profiler data
var profiler = world.GetExtension<Profiler>();
foreach (var profile in profiler.GetSystemProfiles())
{
    Console.WriteLine($"{profile.SystemType.Name}: {profile.AverageMs:F2}ms");
}

// Inspect entities
var inspector = world.GetExtension<EntityInspector>();
var components = inspector.GetComponents(entity);
```

### Parallelism Plugin (NEW)

**Location:** `src/KeenEyes.Parallelism/`

Provides parallel system execution with automatic dependency analysis and profiling.

**Key Classes:**
| Class | Purpose |
|-------|---------|
| `ParallelSystemScheduler` | Schedules systems into parallel batches |
| `ParallelProfiler` | Generates DOT graphs and bottleneck detection |
| `JobScheduler` | Low-level job scheduling |
| `ComponentDependencies` | Tracks read/write dependencies |

**Editor Benefits:**
```csharp
// Generate dependency graph for visualization
var profiler = world.GetExtension<ParallelProfiler>();
string dotGraph = profiler.GenerateDependencyGraph();  // DOT format

// Get bottleneck analysis
var bottlenecks = profiler.IdentifyBottlenecks();
```

### Save/Load System (NEW)

**Location:** `src/KeenEyes.Core/SaveManager.cs`, `src/KeenEyes.Core/Serialization/`

Complete save/load system with delta saves, compression, encryption, and checksums.

**Features:**
- Full world snapshots (JSON and binary)
- Delta saves (only store changes since baseline)
- Compression (GZip)
- Encryption (AES)
- Checksum validation
- Async operations

**Key Classes:**
| Class | Purpose |
|-------|---------|
| `SaveManager` | Save slot management |
| `DeltaDiffer` | Computes delta differences |
| `DeltaRestorer` | Applies delta patches |
| `SnapshotManager` | Snapshot creation/restoration |

**Usage:**
```csharp
// Create a snapshot
var snapshot = world.CreateSnapshot();
string json = snapshot.ToJson();

// Save with delta (only changes since baseline)
var delta = world.CreateDeltaSave(baselineSnapshot);

// Save with encryption
await saveManager.SaveAsync("slot1", snapshot, new SaveOptions
{
    Encrypt = true,
    Compress = true,
    Password = "secret"
});
```

### Spatial Partitioning (NEW)

**Location:** `src/KeenEyes.Spatial/`

Efficient spatial queries with Grid, Quadtree, and Octree strategies plus frustum culling.

**Key Classes:**
| Class | Purpose |
|-------|---------|
| `SpatialQueryApi` | Main query interface |
| `GridPartitioner` | Grid-based partitioning |
| `QuadtreePartitioner` | 2D quadtree |
| `OctreePartitioner` | 3D octree |
| `Frustum` | View frustum culling |

**Editor Benefits:**
```csharp
// Visualize spatial structure
var spatial = world.GetExtension<SpatialQueryApi>();

// Query entities in view frustum
var visible = spatial.QueryFrustum(cameraFrustum);

// Query radius for selection
var nearby = spatial.QueryRadius(clickPosition, selectionRadius);
```

### Physics Integration (NEW)

**Location:** `src/KeenEyes.Physics/`

Complete BepuPhysics v2 integration for 3D rigid body simulation.

**Key Classes:**
| Class | Purpose |
|-------|---------|
| `PhysicsPlugin` | Main plugin |
| `PhysicsWorld` | Physics API wrapper |
| `RigidBody` | Rigid body component |
| `PhysicsShape` | Collision shapes |

**Editor Benefits:**
- Visualize collision shapes
- Display velocity vectors
- Apply test forces/impulses
- Step physics manually

### Multi-World Support (NEW)

**Location:** `src/KeenEyes.Core/World.cs`

Each World instance now has a unique ID and optional name for debugging.

```csharp
// Create named worlds for editor
var gameWorld = new World(name: "Game");
var editorWorld = new World(name: "Editor");

Console.WriteLine($"World ID: {gameWorld.Id}, Name: {gameWorld.Name}");

// Compare world instances
if (entity.WorldId == gameWorld.Id) { ... }
```

### Deterministic RNG (NEW)

**Location:** `src/KeenEyes.Core/World.Random.cs`

Per-world isolated random number generator with optional seeding for replay.

```csharp
// Create deterministic world for replay
var world = new World(seed: 12345);

// Use world's RNG
float speed = world.NextFloat(1.0f, 10.0f);
int damage = world.NextInt(5, 20);
bool crit = world.NextBool();
```

### Global System Hooks (NEW)

**Location:** `src/KeenEyes.Core/World.SystemHooks.cs`

Register callbacks before/after system execution without modifying systems.

```csharp
// Add profiling hook
world.AddSystemHook((system, phase) =>
{
    if (phase == HookPhase.Before)
        stopwatch.Restart();
    else
        RecordTiming(system.GetType(), stopwatch.Elapsed);
});

// Conditional system execution
world.AddSystemHook((system, phase) =>
{
    if (phase == HookPhase.Before && isPaused)
        return HookResult.Skip;  // Skip system execution
    return HookResult.Continue;
});
```

### Component Bundles (NEW)

**Location:** `editor/KeenEyes.Generators/BundleGenerator.cs`

Group commonly-used components with generated builder methods.

```csharp
[Bundle]
public partial struct TransformBundle
{
    public Position Position;
    public Rotation Rotation;
    public Scale Scale;
}

// Usage - generated methods
var entity = world.Spawn()
    .With(new TransformBundle { ... })
    .Build();

// Query by bundle
foreach (var e in world.Query<TransformBundle>())
{
    ref var bundle = ref world.GetBundle<TransformBundle>(e);
}
```

### Component Mixins (NEW)

**Location:** `editor/KeenEyes.Generators/MixinGenerator.cs`

Compile-time field composition for component reuse.

```csharp
// Define mixin
public struct Damageable
{
    public int Health;
    public int MaxHealth;
}

// Use in component
[Component]
[Mixin(typeof(Damageable))]
public partial struct Enemy
{
    public float Speed;
    // Health and MaxHealth fields are copied here at compile time
}
```

### Binary Serialization (NEW)

**Location:** `src/KeenEyes.Core/Serialization/`

High-performance binary format (50-80% smaller than JSON).

```csharp
// Binary snapshot
byte[] binary = world.CreateSnapshot().ToBinary();

// Restore from binary
var snapshot = WorldSnapshot.FromBinary(binary);
snapshot.RestoreTo(world);
```

### Project Templates (NEW)

**Location:** `templates/`

Quick-start templates for new projects.

```bash
# Install templates
dotnet new install KeenEyes.Templates

# Create new game project
dotnet new keeneyes-game -n MyGame

# Create new plugin project
dotnet new keeneyes-plugin -n MyPlugin
```

---

## Required Editor Plugins

### 1. EditorCorePlugin (Foundation)

**Purpose:** Central editor API and coordination

**Responsibilities:**
- Entity selection state management
- Entity browser/enumeration
- Integration point for other editor plugins

```csharp
public class EditorCorePlugin : IWorldPlugin
{
    public string Name => "Editor.Core";

    public void Install(PluginContext context)
    {
        var editorApi = new EditorAPI(context.World);
        context.SetExtension(editorApi);

        // Track entity lifecycle
        context.World.OnEntityCreated((entity, name) =>
            editorApi.NotifyEntityCreated(entity, name));
        context.World.OnEntityDestroyed(entity =>
            editorApi.NotifyEntityDestroyed(entity));
    }

    public void Uninstall(PluginContext context) { }
}

public class EditorAPI
{
    private readonly World world;
    private Entity selectedEntity;
    private readonly List<Entity> multiSelection = [];

    public EditorAPI(World world) => this.world = world;

    // Selection
    public Entity SelectedEntity => selectedEntity;
    public void Select(Entity entity) => selectedEntity = entity;
    public void ClearSelection() => selectedEntity = Entity.Null;

    // Entity enumeration
    public IEnumerable<Entity> GetAllEntities() => world.GetAllEntities();
    public string? GetEntityName(Entity e) => world.GetName(e);
    public Entity FindByName(string name) => world.GetEntityByName(name);

    // Component inspection
    public IEnumerable<(Type, object)> GetComponents(Entity e)
        => world.GetComponents(e);

    // Hierarchy
    public IEnumerable<Entity> GetRootEntities()
        => world.GetAllEntities().Where(e => world.GetParent(e) == Entity.Null);
    public IEnumerable<Entity> GetChildren(Entity e) => world.GetChildren(e);
}
```

### 2. EditorInspectorPlugin (Property Editing)

**Purpose:** Live editing of entity components via reflection

**New Capabilities Needed:**
- Per-field component introspection
- Property metadata attributes (`[Range]`, `[Tooltip]`, `[Header]`)
- Custom property drawers

```csharp
public class EditorInspectorPlugin : IWorldPlugin
{
    public string Name => "Editor.Inspector";

    public void Install(PluginContext context)
    {
        var inspector = new ComponentInspector(context.World);
        context.SetExtension(inspector);
    }

    public void Uninstall(PluginContext context) { }
}

public class ComponentInspector
{
    private readonly World world;

    public ComponentInspector(World world) => this.world = world;

    /// <summary>
    /// Get all public instance fields for a component type.
    /// </summary>
    public IEnumerable<FieldMetadata> GetEditableFields(Type componentType)
    {
        return componentType
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => !f.IsInitOnly)
            .Select(f => new FieldMetadata
            {
                Name = f.Name,
                FieldType = f.FieldType,
                Range = f.GetCustomAttribute<RangeAttribute>(),
                Tooltip = f.GetCustomAttribute<TooltipAttribute>()?.Text,
                Header = f.GetCustomAttribute<HeaderAttribute>()?.Text
            });
    }

    /// <summary>
    /// Get a field value from an entity's component.
    /// </summary>
    public object? GetFieldValue(Entity entity, Type componentType, string fieldName)
    {
        var component = world.GetComponents(entity)
            .FirstOrDefault(c => c.Item1 == componentType);

        if (component == default) return null;

        var field = componentType.GetField(fieldName);
        return field?.GetValue(component.Item2);
    }

    /// <summary>
    /// Set a field value on an entity's component.
    /// </summary>
    public void SetFieldValue(Entity entity, Type componentType, string fieldName, object value)
    {
        var components = world.GetComponents(entity).ToList();
        var componentPair = components.FirstOrDefault(c => c.Item1 == componentType);

        if (componentPair == default) return;

        var component = componentPair.Item2;
        var field = componentType.GetField(fieldName);
        field?.SetValue(component, value);

        // Apply back via dynamic invocation
        SetComponentDynamic(entity, componentType, component);
    }

    private void SetComponentDynamic(Entity entity, Type componentType, object component)
    {
        // Use reflection to call generic Set<T>
        var method = typeof(World).GetMethod("Set")!.MakeGenericMethod(componentType);
        method.Invoke(world, [entity, component]);
    }
}

public record struct FieldMetadata
{
    public string Name;
    public Type FieldType;
    public RangeAttribute? Range;
    public string? Tooltip;
    public string? Header;
}
```

### 3. EditorHistoryPlugin (Undo/Redo)

**Purpose:** Transaction-based undo/redo system

**Implementation Approach:**
- Leverage existing `ChangeTracker.GetDirtyEntities<T>()`
- Store component snapshots per transaction
- Use `CommandBuffer` for batched undo operations

```csharp
public class EditorHistoryPlugin : IWorldPlugin
{
    public string Name => "Editor.History";

    public void Install(PluginContext context)
    {
        var history = new UndoRedoManager(context.World);
        context.SetExtension(history);
    }

    public void Uninstall(PluginContext context) { }
}

public class UndoRedoManager
{
    private readonly World world;
    private readonly Stack<Transaction> undoStack = new();
    private readonly Stack<Transaction> redoStack = new();
    private Transaction? currentTransaction;

    public UndoRedoManager(World world) => this.world = world;

    public bool CanUndo => undoStack.Count > 0;
    public bool CanRedo => redoStack.Count > 0;

    /// <summary>
    /// Begin recording changes for a new transaction.
    /// </summary>
    public void BeginTransaction(string name)
    {
        currentTransaction = new Transaction { Name = name };
    }

    /// <summary>
    /// Record a component change in the current transaction.
    /// </summary>
    public void RecordChange<T>(Entity entity, T oldValue, T newValue) where T : struct
    {
        currentTransaction?.Changes.Add(new ComponentChange
        {
            Entity = entity,
            ComponentType = typeof(T),
            OldValue = oldValue,
            NewValue = newValue
        });
    }

    /// <summary>
    /// End the current transaction and push to undo stack.
    /// </summary>
    public void EndTransaction()
    {
        if (currentTransaction != null && currentTransaction.Changes.Count > 0)
        {
            undoStack.Push(currentTransaction);
            redoStack.Clear();  // Clear redo on new action
        }
        currentTransaction = null;
    }

    /// <summary>
    /// Undo the last transaction.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var transaction = undoStack.Pop();

        // Apply changes in reverse order
        foreach (var change in transaction.Changes.AsEnumerable().Reverse())
        {
            ApplyChange(change.Entity, change.ComponentType, change.OldValue);
        }

        redoStack.Push(transaction);
    }

    /// <summary>
    /// Redo the last undone transaction.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var transaction = redoStack.Pop();

        foreach (var change in transaction.Changes)
        {
            ApplyChange(change.Entity, change.ComponentType, change.NewValue);
        }

        undoStack.Push(transaction);
    }

    private void ApplyChange(Entity entity, Type componentType, object value)
    {
        var method = typeof(World).GetMethod("Set")!.MakeGenericMethod(componentType);
        method.Invoke(world, [entity, value]);
    }
}

public class Transaction
{
    public required string Name;
    public List<ComponentChange> Changes = [];
}

public struct ComponentChange
{
    public Entity Entity;
    public Type ComponentType;
    public object OldValue;
    public object NewValue;
}
```

### 4. EditorDiagnosticsPlugin (Profiling)

**Purpose:** System timing, memory stats, and performance visualization

```csharp
public class EditorDiagnosticsPlugin : IWorldPlugin
{
    public string Name => "Editor.Diagnostics";

    public void Install(PluginContext context)
    {
        var profiler = new SystemProfiler(context.World);
        context.SetExtension(profiler);
    }

    public void Uninstall(PluginContext context) { }
}

public class SystemProfiler
{
    private readonly World world;
    private readonly Dictionary<Type, SystemProfile> profiles = new();
    private readonly Dictionary<Type, long> startTimestamps = new();

    public SystemProfiler(World world) => this.world = world;

    /// <summary>
    /// Get current memory statistics.
    /// </summary>
    public MemoryStats GetMemoryStats() => world.GetMemoryStats();

    /// <summary>
    /// Record system execution start.
    /// </summary>
    public void BeforeSystemUpdate(Type systemType)
    {
        startTimestamps[systemType] = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Record system execution end and update profile.
    /// </summary>
    public void AfterSystemUpdate(Type systemType)
    {
        if (!startTimestamps.TryGetValue(systemType, out var start)) return;

        var elapsed = Stopwatch.GetElapsedTime(start);

        if (!profiles.TryGetValue(systemType, out var profile))
        {
            profile = new SystemProfile { SystemType = systemType };
            profiles[systemType] = profile;
        }

        profile.RecordSample(elapsed.TotalMilliseconds);
    }

    /// <summary>
    /// Get profiling data for all systems.
    /// </summary>
    public IEnumerable<SystemProfile> GetProfiles() => profiles.Values;

    /// <summary>
    /// Get profiling data for a specific system.
    /// </summary>
    public SystemProfile? GetProfile(Type systemType)
        => profiles.GetValueOrDefault(systemType);

    /// <summary>
    /// Reset all profiling data.
    /// </summary>
    public void Reset() => profiles.Clear();
}

public class SystemProfile
{
    public required Type SystemType;

    private readonly List<double> samples = [];
    private const int MaxSamples = 120;  // ~2 seconds at 60fps

    public void RecordSample(double milliseconds)
    {
        samples.Add(milliseconds);
        if (samples.Count > MaxSamples)
            samples.RemoveAt(0);
    }

    public double AverageMs => samples.Count > 0 ? samples.Average() : 0;
    public double MaxMs => samples.Count > 0 ? samples.Max() : 0;
    public double MinMs => samples.Count > 0 ? samples.Min() : 0;
    public double LastMs => samples.Count > 0 ? samples[^1] : 0;
    public int SampleCount => samples.Count;
}
```

### 5. EditorSerializationPlugin (Save/Load)

**Purpose:** Scene serialization and prefab support

**Dependencies:** Requires Phase 11 (Serialization) from roadmap

**Design:**
```csharp
public class EditorSerializationPlugin : IWorldPlugin
{
    public string Name => "Editor.Serialization";

    public void Install(PluginContext context)
    {
        var serializer = new WorldSerializer(context.World);
        context.SetExtension(serializer);
    }

    public void Uninstall(PluginContext context) { }
}

public class WorldSerializer
{
    private readonly World world;

    public WorldSerializer(World world) => this.world = world;

    /// <summary>
    /// Serialize an entity to JSON.
    /// </summary>
    public string SerializeEntity(Entity entity)
    {
        var data = new EntityData
        {
            Name = world.GetName(entity),
            Components = []
        };

        foreach (var (type, value) in world.GetComponents(entity))
        {
            data.Components.Add(new ComponentData
            {
                TypeName = type.AssemblyQualifiedName!,
                Value = JsonSerializer.Serialize(value, type)
            });
        }

        return JsonSerializer.Serialize(data);
    }

    /// <summary>
    /// Deserialize an entity from JSON.
    /// </summary>
    public Entity DeserializeEntity(string json)
    {
        var data = JsonSerializer.Deserialize<EntityData>(json)!;
        var builder = world.Spawn(data.Name);

        foreach (var component in data.Components)
        {
            var type = Type.GetType(component.TypeName)!;
            var value = JsonSerializer.Deserialize(component.Value, type)!;
            // Use reflection to call generic Add<T>
            AddComponentDynamic(builder, type, value);
        }

        return builder.Build();
    }

    /// <summary>
    /// Serialize entire world state.
    /// </summary>
    public string SerializeWorld()
    {
        var entities = world.GetAllEntities()
            .Select(e => SerializeEntity(e))
            .ToList();

        return JsonSerializer.Serialize(entities);
    }

    private void AddComponentDynamic(EntityBuilder builder, Type type, object value)
    {
        // Implementation via reflection
    }
}

public record EntityData
{
    public string? Name { get; init; }
    public List<ComponentData> Components { get; init; } = [];
}

public record ComponentData
{
    public required string TypeName { get; init; }
    public required string Value { get; init; }
}
```

---

## Hot Reload Implementation

Hot reload is the most complex feature. Three approaches are viable:

### Approach A: .NET Hot Reload (Limited)

Uses the built-in Hot Reload for method body changes.

**Supported Changes:**
- Method body modifications
- Lambda expression changes
- Static field initializers

**NOT Supported:**
- Adding/removing types
- Changing type signatures
- Adding/removing methods

**Pros:** Built into .NET, zero effort
**Cons:** Very limited scope, won't work for new systems/components

### Approach B: AssemblyLoadContext Plugin Isolation (Recommended)

Load game assemblies into isolated, collectible contexts that can be unloaded.

```csharp
public class HotReloadManager
{
    private readonly World world;
    private AssemblyLoadContext? gameContext;
    private Assembly? gameAssembly;
    private readonly List<Type> registeredSystemTypes = [];

    public HotReloadManager(World world) => this.world = world;

    /// <summary>
    /// Load game assembly into isolated context.
    /// </summary>
    public void LoadGameAssembly(string path)
    {
        // Unload previous if exists
        if (gameContext != null)
        {
            UnloadGameAssembly();
        }

        // Create new isolated, collectible context
        gameContext = new AssemblyLoadContext("GameCode", isCollectible: true);
        gameAssembly = gameContext.LoadFromAssemblyPath(path);

        // Discover and register systems
        RegisterSystemsFromAssembly(gameAssembly);
    }

    /// <summary>
    /// Unload the game assembly.
    /// </summary>
    public void UnloadGameAssembly()
    {
        // CRITICAL: Remove all references first!
        // 1. Unregister all systems from this assembly
        foreach (var systemType in registeredSystemTypes)
        {
            // world.RemoveSystem(systemType);  // Needs API addition
        }
        registeredSystemTypes.Clear();

        // 2. Unload context
        gameContext?.Unload();
        gameContext = null;
        gameAssembly = null;

        // 3. Force GC to collect unloaded assembly
        for (int i = 0; i < 10; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }

    /// <summary>
    /// Recompile and reload game code while preserving world state.
    /// </summary>
    public async Task ReloadGameCodeAsync(string projectPath)
    {
        // 1. Serialize world state
        var serializer = world.GetExtension<WorldSerializer>();
        var snapshot = serializer.SerializeWorld();

        // 2. Unload current assembly
        UnloadGameAssembly();

        // 3. Recompile (external process)
        var buildResult = await RunBuildAsync(projectPath);
        if (!buildResult.Success)
        {
            throw new CompilationException(buildResult.Errors);
        }

        // 4. Reload assembly
        LoadGameAssembly(buildResult.OutputPath);

        // 5. Note: Entity data survives because it's stored as byte arrays
        // Only systems/code needs re-registration
    }

    private void RegisterSystemsFromAssembly(Assembly assembly)
    {
        var systemTypes = assembly.GetTypes()
            .Where(t => typeof(ISystem).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var systemType in systemTypes)
        {
            // Get phase and order from attributes
            var attr = systemType.GetCustomAttribute<SystemAttribute>();
            var phase = attr?.Phase ?? SystemPhase.Update;
            var order = attr?.Order ?? 0;

            var system = (ISystem)Activator.CreateInstance(systemType)!;
            world.AddSystem(system, phase, order);
            registeredSystemTypes.Add(systemType);
        }
    }

    private Task<BuildResult> RunBuildAsync(string projectPath)
    {
        // Implementation: run `dotnet build` and capture output
        throw new NotImplementedException();
    }
}

public record BuildResult(bool Success, string OutputPath, string[] Errors);
public class CompilationException(string[] errors) : Exception(string.Join("\n", errors));
```

**Key Insight:** The `World` and entity data survive reloads. Component data persists because it's stored as byte arrays in archetype chunks. Only the code (systems, plugins) needs reloading.

### Approach C: Roslyn Script Compilation

Compile C# scripts at runtime using Roslyn.

```csharp
public class ScriptCompiler
{
    private readonly List<MetadataReference> references;

    public ScriptCompiler()
    {
        references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(World).Assembly.Location),
            // Add other required references
        ];
    }

    public Assembly CompileScript(string code, string assemblyName = "GameScripts")
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);

        var compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(references)
            .AddSyntaxTrees(syntaxTree);

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.GetMessage())
                .ToArray();
            throw new CompilationException(errors);
        }

        ms.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(ms.ToArray());
    }
}
```

**Pros:** In-process compilation, no external tools
**Cons:** Large dependency (Roslyn ~50MB), slower than pre-compiled

### Recommended Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      Editor Process                          │
│  ┌─────────────────────┐  ┌─────────────────────────────┐   │
│  │    Editor Core      │  │    AssemblyLoadContext      │   │
│  │    (permanent)      │  │    "GameCode" (collectible) │   │
│  │                     │  │  ┌─────────────────────────┐│   │
│  │  - EditorAPI        │  │  │ Game.dll                ││   │
│  │  - Inspector        │  │  │ - Components            ││   │
│  │  - Undo/Redo        │  │  │ - Systems               ││   │
│  │  - Profiler         │  │  │ - Plugins               ││   │
│  │  - Serialization    │  │  └─────────────────────────┘│   │
│  └──────────┬──────────┘  └──────────────┬──────────────┘   │
│             │                            │                   │
│             └────────────┬───────────────┘                   │
│                          │                                   │
│                  ┌───────▼───────┐                           │
│                  │     World     │                           │
│                  │   (survives   │                           │
│                  │    reload)    │                           │
│                  └───────────────┘                           │
└─────────────────────────────────────────────────────────────┘
```

---

## Dependencies on Roadmap Phases

| Editor Feature | Required Phase | Status |
|----------------|----------------|--------|
| Entity Inspection | Phase 1 | ✅ Complete |
| Change Detection | Phase 4 | ✅ Complete |
| Event Subscriptions | Phase 4 | ✅ Complete |
| Plugin System | Phase 6 | ✅ Complete |
| Messaging | Phase 7 | ✅ Complete |
| Prefabs | Phase 8 | ✅ Complete |
| String Tags | Phase 9 | ✅ Complete |
| Component Validation | Phase 10 | ✅ Complete |
| Serialization | Phase 11 | ✅ Complete |
| Logging | Phase 12 | ✅ Complete |
| Debug Mode | Phase 13 | ✅ Complete (KeenEyes.Debugging) |
| System Profiling | Phase 13 | ✅ Complete (KeenEyes.Debugging) |
| Parallel Profiling | - | ✅ Complete (KeenEyes.Parallelism) |
| Spatial Queries | - | ✅ Complete (KeenEyes.Spatial) |
| Physics | - | ✅ Complete (KeenEyes.Physics) |
| Save/Load | - | ✅ Complete (SaveManager) |
| Binary Serialization | - | ✅ Complete |
| Delta Saves | - | ✅ Complete |

---

## Implementation Roadmap

### Phase A: Foundation (1-2 weeks effort)

1. **EditorCorePlugin**
   - Entity selection state
   - Entity enumeration
   - Basic API extension

2. **Per-field component inspector**
   - Reflection-based field discovery
   - Field value get/set

3. **EditorDiagnosticsPlugin**
   - Wrap existing `MemoryStats`
   - Add system timing

### Phase B: History & Persistence (2-3 weeks effort)

1. **Undo/Redo system**
   - Transaction-based history
   - Component snapshots
   - Stack-based undo/redo

2. **Basic serialization**
   - JSON entity/component save/load
   - Scene files

3. **Entity templates**
   - Basic prefab definition
   - Prefab instantiation

### Phase C: Hot Reload (3-4 weeks effort)

1. **AssemblyLoadContext isolation**
   - Separate game code
   - Collectible context

2. **Type registry rebuild**
   - Re-register components after reload
   - Handle type changes

3. **System hot-swap**
   - Unregister/re-register systems
   - Preserve enabled state

4. **State preservation**
   - Serialize/restore during reload
   - Handle component schema changes

### Phase D: Visual Editor (Future)

1. **UI Framework integration**
   - Avalonia, MAUI, or web-based (Blazor)

2. **Scene view**
   - 2D/3D visualization
   - Transform gizmos

3. **Property drawers**
   - Custom editors per component type
   - Range sliders, color pickers, etc.

4. **Prefab system**
   - Full prefab hierarchy
   - Prefab overrides

---

## Minimal Viable Editor

For a **rudimentary editor**, the minimum features needed are:

| Feature | Implementation | Lines of Code |
|---------|----------------|---------------|
| Entity Browser | List `GetAllEntities()` with names | ~100 |
| Component Inspector | Reflection over `GetComponents()` | ~200 |
| Play/Pause | `EnableSystem`/`DisableSystem` all | ~50 |
| Console | Hook `EventBus`, display events | ~100 |
| Stats Panel | Display `MemoryStats` | ~50 |

**Total: ~500 lines** on top of existing APIs for a basic terminal or simple GUI editor.

---

## Feature Comparison: Editor Frameworks

When choosing a UI framework for the editor:

| Framework | Pros | Cons | Best For |
|-----------|------|------|----------|
| **Avalonia** | Cross-platform, XAML, mature | Separate render context | Desktop editor |
| **MAUI** | Microsoft-backed, native | Windows-centric, complex | Windows-first |
| **ImGui.NET** | Immediate mode, game-native | Less polished | In-game overlay |
| **Blazor** | Web tech, familiar | WebAssembly limitations | Browser editor |
| **Terminal.Gui** | Simple, lightweight | Text-only | CLI/debug tools |

**Recommendation:** Start with **Terminal.Gui** or **ImGui.NET** for rapid prototyping, graduate to **Avalonia** for production editor.

---

## Gaps Summary

| Gap | Description | Solution |
|-----|-------------|----------|
| **Per-field introspection** | Need field metadata | Reflection wrapper (~100 lines) |
| **Property attributes** | `[Range]`, `[Tooltip]`, etc. | Define attributes, inspector reads |
| **Undo/Redo** | No transaction history | Build on ChangeTracker + Snapshots |
| **Hot Reload** | No runtime code swapping | AssemblyLoadContext approach |

> **Resolved Since Initial Research (Cumulative):**
> - ~~Serialization~~ → `SnapshotManager` with JSON, binary, and AOT support
> - ~~Prefabs~~ → `PrefabManager` with inheritance support
> - ~~RemoveSystem API~~ → Now available via SystemManager
> - ~~System BeforeUpdate hook~~ → `ISystemLifecycle` interface added
> - ~~Debug Mode~~ → Complete `KeenEyes.Debugging` plugin
> - ~~System Profiling~~ → `Profiler` class with timing history
> - ~~Entity Inspector~~ → `EntityInspector` for runtime introspection
> - ~~Memory Tracking~~ → `MemoryTracker` and `GCTracker`
> - ~~Parallel Profiling~~ → `ParallelProfiler` with DOT graphs
> - ~~Save Slots~~ → `SaveManager` with full/delta saves
> - ~~Encryption~~ → AES encryption via `EncryptedPersistenceApi`
> - ~~Binary Format~~ → 50-80% smaller binary serialization
> - ~~Delta Saves~~ → `DeltaDiffer` and `DeltaRestorer`
> - ~~Spatial Queries~~ → Grid, Quadtree, Octree partitioners
> - ~~Frustum Culling~~ → `Frustum` class with SIMD optimization
> - ~~Physics~~ → BepuPhysics v2 integration
> - ~~Multi-World~~ → World ID/Name for debugging
> - ~~Deterministic RNG~~ → Per-world seeded random
> - ~~System Hooks~~ → Global before/after callbacks
> - ~~Bundles~~ → Component grouping with generated methods
> - ~~Mixins~~ → Compile-time field composition
> - ~~Project Templates~~ → `dotnet new keeneyes-game`

---

## Conclusion

Building a rudimentary editor for KeenEyes is **feasible with minimal effort**. The architecture was designed with editor-friendly patterns and now includes comprehensive tooling:

### Core Infrastructure
- **Per-world isolation** enables editor/game separation
- **Event system** enables reactive UI updates
- **Plugin system** enables modular editor features
- **Entity inspection APIs** already exist and are comprehensive
- **Change tracking** provides foundation for undo/redo

### Debugging & Profiling
- **KeenEyes.Debugging plugin** provides complete profiling, entity inspection, and memory tracking
- **KeenEyes.Parallelism plugin** provides system dependency graphs and bottleneck detection
- **Global system hooks** enable non-invasive profiling

### Serialization & Persistence
- **Serialization & Snapshots** provide complete save/load support (JSON and binary)
- **Delta saves** enable efficient incremental saves
- **Encryption and compression** for secure, compact save files
- **Prefab system** enables entity templates with inheritance

### Advanced Features
- **Spatial partitioning** (Grid, Quadtree, Octree) for spatial visualization
- **Physics integration** (BepuPhysics) for physics debugging
- **Multi-world support** for separate editor/game worlds
- **Deterministic RNG** for replay and testing
- **Component bundles and mixins** for rapid entity creation
- **Project templates** for quick-start development

### Designer Experience
- **Pluggable logging** enables debug output and diagnostics
- **String tags** enable designer-friendly entity categorization
- **Component validation** enables dependency visualization
- **Testing utilities** enable deterministic editor testing
- **Graphics plugin** provides reference for rendering integration

The main work involves:
1. **Building the UI layer** (choose framework based on needs)
2. **Adding undo/redo** (layer on ChangeTracker + Snapshots)
3. **Hot reload** (AssemblyLoadContext approach recommended)

The framework provides approximately **95% of the infrastructure** needed for a full-featured editor. The remaining 5% is UI/UX, undo/redo transactions, and hot reload—all of which have clear implementation paths using the existing APIs.

---

## Sources

### .NET Hot Reload
- [.NET Hot Reload Runtime API - GitHub Issue](https://github.com/dotnet/runtime/issues/45689)
- [.NET Hot Reload Support for ASP.NET Core - Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/test/hot-reload?view=aspnetcore-9.0)
- [Extend .NET Hot Reload - Visual Studio Documentation](https://learn.microsoft.com/en-us/visualstudio/debugger/hot-reload-metadataupdatehandler?view=vs-2022)
- [Introducing .NET Hot Reload - .NET Blog](https://devblogs.microsoft.com/dotnet/introducing-net-hot-reload/)

### AssemblyLoadContext and Dynamic Loading
- [Dynamic Assembly Loading and Unloading - Stack Overflow](https://stackoverflow.com/questions/63616618/how-to-dynamically-load-and-unload-reload-a-dll-assembly)
- [C# Scripting Engine Hot Reloading - Kah Wei Blog](https://kahwei.dev/2023/08/07/c-scripting-engine-part-7-hot-reloading/)
- [Runtime NuGet Package Loading - Rick Strahl](https://weblog.west-wind.com/posts/2025/Jun/09/Adding-Runtime-NuGet-Package-Loading-to-an-Application)

### KeenEyes Framework - Core
- Plugin System: `src/KeenEyes.Core/PluginManager.cs`
- Event System: `src/KeenEyes.Core/EventManager.cs`
- Change Tracking: `src/KeenEyes.Core/Events/ChangeTracker.cs`
- Memory Stats: `src/KeenEyes.Core/Pooling/MemoryStats.cs`
- Entity Inspection: `src/KeenEyes.Core/World.cs`
- Hierarchy: `src/KeenEyes.Core/HierarchyManager.cs`
- Messaging: `src/KeenEyes.Core/MessageManager.cs`
- Multi-World: `src/KeenEyes.Core/World.cs` (Id, Name properties)
- Deterministic RNG: `src/KeenEyes.Core/World.Random.cs`
- System Hooks: `src/KeenEyes.Core/World.SystemHooks.cs`

### KeenEyes Framework - Serialization
- Snapshot Manager: `src/KeenEyes.Core/Serialization/SnapshotManager.cs`
- Save Manager: `src/KeenEyes.Core/SaveManager.cs`
- Delta Differ: `src/KeenEyes.Core/Serialization/DeltaDiffer.cs`
- Delta Restorer: `src/KeenEyes.Core/Serialization/DeltaRestorer.cs`
- Binary Format: `src/KeenEyes.Core/Serialization/SaveFileFormat.cs`
- Encryption: `src/KeenEyes.Persistence/EncryptedPersistenceApi.cs`

### KeenEyes Framework - Plugins
- Debugging Plugin: `src/KeenEyes.Debugging/`
- Parallelism Plugin: `src/KeenEyes.Parallelism/`
- Spatial Plugin: `src/KeenEyes.Spatial/`
- Physics Plugin: `src/KeenEyes.Physics/`
- Graphics Plugin: `src/KeenEyes.Graphics/`

### KeenEyes Framework - Source Generators
- Bundle Generator: `editor/KeenEyes.Generators/BundleGenerator.cs`
- Mixin Generator: `editor/KeenEyes.Generators/MixinGenerator.cs`
- Component Generator: `editor/KeenEyes.Generators/ComponentGenerator.cs`

### KeenEyes Framework - Templates
- Game Template: `templates/keeneyes-game/`
- Plugin Template: `templates/keeneyes-plugin/`

### Editor Architecture References
- [Unity Editor Scripting Documentation](https://docs.unity3d.com/Manual/ExtendingTheEditor.html)
- [Godot Editor Tutorials](https://docs.godotengine.org/en/stable/tutorials/editor/index.html)
- [Flecs ECS Explorer](https://github.com/flecs-hub/explorer)

---

## Appendix: Attribute Definitions for Inspector

Suggested attributes for component field metadata:

```csharp
namespace KeenEyes.Editor.Attributes;

/// <summary>
/// Restricts a numeric field to a range.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class RangeAttribute(float min, float max) : Attribute
{
    public float Min => min;
    public float Max => max;
}

/// <summary>
/// Displays a tooltip in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Struct)]
public sealed class TooltipAttribute(string text) : Attribute
{
    public string Text => text;
}

/// <summary>
/// Adds a header above a field in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HeaderAttribute(string text) : Attribute
{
    public string Text => text;
}

/// <summary>
/// Adds spacing before a field in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class SpaceAttribute(float height = 8) : Attribute
{
    public float Height => height;
}

/// <summary>
/// Hides a field from the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute;

/// <summary>
/// Shows a color picker for uint/Color fields.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ColorPickerAttribute : Attribute;

/// <summary>
/// Marks a field as read-only in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ReadOnlyAttribute : Attribute;
```

Usage example:

```csharp
[Component]
public partial struct EnemyConfig
{
    [Header("Movement")]
    [Range(0, 100)]
    [Tooltip("Movement speed in units per second")]
    public float Speed;

    [Space]
    [Header("Combat")]
    [Range(1, 1000)]
    public int MaxHealth;

    [Range(1, 100)]
    public int Damage;

    [ColorPicker]
    public uint TintColor;

    [HideInInspector]
    public int InternalState;
}
```
