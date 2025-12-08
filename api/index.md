# API Reference

Welcome to the KeenEyes ECS API documentation.

This reference is automatically generated from XML documentation comments in the source code. For tutorials and guides, see the [Documentation](../docs/index.md).

## Namespaces

### KeenEyes

Core ECS runtime types for building high-performance entity component systems.

| Type | Description |
|------|-------------|
| @KeenEyes.World | Main ECS container - manages entities, components, systems, and queries |
| @KeenEyes.Entity | Lightweight entity handle `(Id, Version)` for staleness detection |
| @KeenEyes.EntityBuilder | Fluent builder for spawning entities with components |
| @KeenEyes.IComponent | Marker interface for component structs |
| @KeenEyes.ITagComponent | Marker interface for zero-size tag components |
| @KeenEyes.ComponentRegistry | Per-world component type registration |
| @KeenEyes.Archetype | Storage for entities sharing the same component types |
| @KeenEyes.ArchetypeManager | Manages archetype lifecycle and entity migrations |
| @KeenEyes.QueryBuilder`1 | Fluent query builder for filtering entities |
| @KeenEyes.QueryEnumerator`1 | Cache-friendly query iteration |
| @KeenEyes.ISystem | Base interface for ECS systems |
| @KeenEyes.SystemBase | Abstract base class with lifecycle hooks and runtime control |
| @KeenEyes.SystemGroup | Groups systems for collective execution |
| @KeenEyes.SystemPhase | Enum defining execution phases (EarlyUpdate, FixedUpdate, Update, LateUpdate, Render, PostRender) |
| @KeenEyes.RunBeforeAttribute | Specifies this system must run before another system (topologically sorted) |
| @KeenEyes.RunAfterAttribute | Specifies this system must run after another system (topologically sorted) |
| @KeenEyes.CommandBuffer | Queues entity operations for deferred execution |
| @KeenEyes.EntityPool | Manages entity ID recycling with versioning |
| @KeenEyes.MemoryStats | Memory usage statistics snapshot |

### KeenEyes.Events

Types for reactive programming patterns - events, subscriptions, and change tracking.

| Type | Description |
|------|-------------|
| @KeenEyes.Events.EventBus | Generic pub/sub event system for custom events |
| @KeenEyes.Events.EventSubscription | Disposable subscription handle for event cleanup |

### KeenEyes.Generators.Attributes

Attributes for Roslyn source generators that reduce boilerplate code.

| Attribute | Description |
|-----------|-------------|
| @KeenEyes.ComponentAttribute | Generates `WithComponentName()` fluent builder methods |
| @KeenEyes.TagComponentAttribute | Generates parameterless tag methods for marker components |
| @KeenEyes.DefaultValueAttribute | Specifies default values for component fields in builders |
| @KeenEyes.BuilderIgnoreAttribute | Excludes a field from generated builder parameters |
| @KeenEyes.QueryAttribute | Generates efficient query iterator structs |
| @KeenEyes.SystemAttribute | Generates system metadata (Phase, Order, Group) |

## Getting Started

The main entry point is the @KeenEyes.World class:

```csharp
using KeenEyes;

// Create an isolated ECS world
using var world = new World();

// Create an entity with components using the fluent builder
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 1, Y = 0 })
    .Build();

// Query and iterate over matching entities
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);

    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

## Key Concepts

| Concept | Description |
|---------|-------------|
| **World** | Isolated container for entities, components, and systems. Each world has its own component registry - no static state. |
| **Entity** | Lightweight `(Id, Version)` tuple. Version increments on recycle for staleness detection. |
| **Component** | Plain data struct implementing `IComponent`. Use `ITagComponent` for zero-size markers. |
| **Archetype** | Internal storage grouping entities with identical component types for cache-friendly iteration. |
| **Query** | Fluent API for filtering entities: `world.Query<A, B>().With<C>().Without<D>()` |
| **System** | Logic that processes entities. Inherit from `SystemBase` for lifecycle hooks (`OnBeforeUpdate`, `OnAfterUpdate`, `OnEnabled`, `OnDisabled`) and runtime control. |
| **SystemPhase** | Execution stage: EarlyUpdate → FixedUpdate → Update → LateUpdate → Render → PostRender. |
| **CommandBuffer** | Queues spawn/despawn/component operations for safe execution outside iteration. |
| **Singleton** | World-level resources via `SetSingleton<T>()` / `GetSingleton<T>()`. |
| **Hierarchy** | Parent-child entity relationships via `SetParent()` / `GetChildren()`. |
| **Messaging** | Inter-system communication via `Send<T>()` / `Subscribe<T>()` with immediate or queued delivery. |
| **Plugin** | Modular extensions via `IWorldPlugin` with `Install()` / `Uninstall()` lifecycle and Extension API. |
| **Events** | Lifecycle events via `OnEntityCreated()`, `OnComponentAdded<T>()`, etc. |
| **Change Tracking** | Dirty entity tracking via `MarkDirty<T>()` / `GetDirtyEntities<T>()`. |

## Quick Reference

### Entity Operations

```csharp
// Spawn
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .WithTag<Player>()
    .Build();

// Check and get components
if (world.Has<Position>(entity))
{
    ref var pos = ref world.Get<Position>(entity);
    pos.X += 10;
}

// Add/remove components
world.Add(entity, new Velocity { X = 1, Y = 0 });
world.Remove<Velocity>(entity);

// Despawn
world.Despawn(entity);
```

### Queries

```csharp
// Basic query - entities with Position AND Velocity
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);
}

// Filtered query - with Health, without Dead tag
foreach (var e in world.Query<Position>().With<Health>().Without<Dead>())
{
    // Process living entities with position and health
}
```

### Systems

```csharp
[System(Phase = SystemPhase.Update, Order = 10)]
public partial class MovementSystem : SystemBase
{
    protected override void OnBeforeUpdate(float deltaTime)
    {
        // Called before Update - setup, accumulation, etc.
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref readonly var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }

    protected override void OnAfterUpdate(float deltaTime)
    {
        // Called after Update - cleanup, statistics, etc.
    }

    protected override void OnEnabled()
    {
        // Called when system is enabled
    }

    protected override void OnDisabled()
    {
        // Called when system is disabled
    }
}

// Register with phase and order
world.AddSystem<InputSystem>(SystemPhase.EarlyUpdate, order: 0);
world.AddSystem<MovementSystem>(SystemPhase.Update, order: 10);
world.AddSystem<RenderSystem>(SystemPhase.Render, order: 0);

// System dependencies with [RunBefore] and [RunAfter]
[System(Phase = SystemPhase.Update)]
[RunAfter(typeof(InputSystem))]
[RunBefore(typeof(RenderSystem))]
public partial class AnimationSystem : SystemBase { }

// Or specify at registration time
world.AddSystem<CollisionSystem>(
    SystemPhase.Update,
    order: 0,
    runsBefore: [typeof(DamageSystem)],
    runsAfter: [typeof(MovementSystem)]);

// Run all systems (sorted by phase, then dependencies, then order)
world.Update(deltaTime);

// Run only FixedUpdate phase systems (for physics)
world.FixedUpdate(fixedDeltaTime);

// Runtime control
var system = world.GetSystem<MovementSystem>();
world.DisableSystem<MovementSystem>();  // Pauses the system
world.EnableSystem<MovementSystem>();   // Resumes the system
```

### Command Buffer (Deferred Operations)

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Health>())
{
    ref var health = ref world.Get<Health>(entity);
    if (health.Current <= 0)
    {
        // Queue for later - safe during iteration
        buffer.Despawn(entity);
    }
}

// Execute all queued commands
buffer.Flush(world);
```

### Singletons

```csharp
// Set world-level resource
world.SetSingleton(new GameConfig { Gravity = 9.8f });

// Retrieve
ref var config = ref world.GetSingleton<GameConfig>();
```

### Entity Hierarchy

```csharp
// Establish parent-child relationship
world.SetParent(child, parent);

// Query relationships
var parent = world.GetParent(child);
foreach (var child in world.GetChildren(parent))
{
    // Process children
}

// Destroy entity and all descendants
world.DespawnRecursive(rootEntity);
```

### Messaging (Inter-System Communication)

```csharp
// Define message types (struct recommended for zero-allocation)
public readonly record struct DamageMessage(Entity Target, int Amount, Entity Source);

// Subscribe to messages
var subscription = world.Subscribe<DamageMessage>(msg =>
{
    ref var health = ref world.Get<Health>(msg.Target);
    health.Current -= msg.Amount;
});

// Send immediately to all subscribers
world.Send(new DamageMessage(target, 25, attacker));

// Or queue for deferred processing
world.QueueMessage(new DamageMessage(target, 25, attacker));
world.ProcessQueuedMessages();  // Process all queued messages

// Check if anyone is listening (optimization)
if (world.HasMessageSubscribers<ExpensiveMessage>())
{
    var data = ComputeExpensiveData();
    world.Send(new ExpensiveMessage(data));
}

// Unsubscribe
subscription.Dispose();
```

### Events

```csharp
// Component lifecycle events
var sub1 = world.OnComponentAdded<Health>((entity, health) =>
{
    Console.WriteLine($"Health added to {entity}");
});

// Entity lifecycle events
var sub2 = world.OnEntityDestroyed(entity =>
{
    Console.WriteLine($"Entity {entity} destroyed");
});

// Custom events via EventBus
world.Events.Subscribe<DamageEvent>(evt =>
{
    Console.WriteLine($"Damage dealt: {evt.Amount}");
});
world.Events.Publish(new DamageEvent(target, 25));

// Cleanup subscriptions
sub1.Dispose();
sub2.Dispose();
```

### Change Tracking

```csharp
// Mark entity as dirty
world.MarkDirty<Position>(entity);

// Query dirty entities
foreach (var e in world.GetDirtyEntities<Position>())
{
    SyncToNetwork(e);
}

// Clear after processing
world.ClearDirtyFlags<Position>();

// Enable automatic tracking for Set() calls
world.EnableAutoTracking<Position>();
```

### Plugins

```csharp
// Create plugin
public class PhysicsPlugin : IWorldPlugin
{
    public string Name => "Physics";

    public void Install(PluginContext context)
    {
        context.AddSystem<GravitySystem>(SystemPhase.FixedUpdate, order: 0);
        context.SetExtension(new PhysicsWorld(context.World));
    }

    public void Uninstall(PluginContext context)
    {
        context.RemoveExtension<PhysicsWorld>();
    }
}

// Install via WorldBuilder
using var world = new WorldBuilder()
    .WithPlugin<PhysicsPlugin>()
    .Build();

// Or install directly
world.InstallPlugin<PhysicsPlugin>();

// Query plugins
if (world.HasPlugin<PhysicsPlugin>())
{
    var plugin = world.GetPlugin<PhysicsPlugin>();
}

// Access extensions
var physics = world.GetExtension<PhysicsWorld>();
physics.SetGravity(9.8f);

// Uninstall
world.UninstallPlugin<PhysicsPlugin>();
```

## Source Generator Attributes

Reduce boilerplate with source-generated code:

```csharp
using KeenEyes.Generators.Attributes;

// Generates WithPosition(float x, float y) builder method
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

// Generates WithPlayer() parameterless method
[TagComponent]
public partial struct Player { }

// Use generated methods
var entity = world.Spawn()
    .WithPosition(x: 10, y: 20)
    .WithPlayer()
    .Build();
```

## Guides

For in-depth coverage, see the documentation guides:

- [Getting Started](../docs/getting-started.md) - Build your first ECS application
- [Core Concepts](../docs/concepts.md) - Understand ECS fundamentals
- [Entities](../docs/entities.md) - Entity lifecycle and management
- [Components](../docs/components.md) - Component patterns and best practices
- [Queries](../docs/queries.md) - Filtering and iterating entities
- [Systems](../docs/systems.md) - System design patterns
- [Messaging](../docs/messaging.md) - Inter-system communication patterns
- [Plugins](../docs/plugins.md) - Modular extensions and feature packaging
- [Command Buffer](../docs/command-buffer.md) - Safe entity modification during iteration
- [Singletons](../docs/singletons.md) - World-level resources
- [Relationships](../docs/relationships.md) - Parent-child entity hierarchies
- [Events](../docs/events.md) - Component and entity lifecycle events
- [Change Tracking](../docs/change-tracking.md) - Track component modifications

Browse the namespace documentation below for detailed API information on each type.
