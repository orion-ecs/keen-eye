# KeenEyes.Abstractions

The **KeenEyes.Abstractions** package provides lightweight interfaces and types for authoring plugins and extensions without depending on the full `KeenEyes.Core` runtime.

## Philosophy: Swappable Subsystems

KeenEyes is designed as a **fully-featured game engine** that remains **completely customizable**. The abstractions layer is the foundation that makes this possible.

### The Problem with Monolithic Engines

Traditional game engines often tightly couple their subsystems:

```
❌ Monolithic: Everything depends on everything
┌─────────────────────────────────────────┐
│   Rendering ←→ Physics ←→ Audio ←→ ECS │
│        ↕          ↕         ↕          │
│      Input ←→ Networking ←→ Assets     │
└─────────────────────────────────────────┘
```

This creates problems:
- Can't use a different physics engine without rewriting rendering code
- Testing requires the entire engine
- Updates to one subsystem can break others

### The KeenEyes Solution

KeenEyes uses abstraction boundaries that allow any subsystem to be swapped:

```
✅ Modular: Subsystems are independent
┌──────────┐  ┌──────────┐  ┌──────────┐
│ Graphics │  │  Audio   │  │ Physics  │  ← Swappable implementations
└────┬─────┘  └────┬─────┘  └────┬─────┘
     │             │             │
     └─────────────┼─────────────┘
                   ↓
          ┌───────────────┐
          │  KeenEyes.Core │  ← ECS runtime
          └───────┬───────┘
                  ↓
          ┌───────────────────┐
          │ Abstractions      │  ← Stable interfaces
          └───────────────────┘
```

**Real-world example:**

```csharp
// Default: Use built-in implementations
using var world = new WorldBuilder()
    .WithPlugin<SilkGraphicsPlugin>()    // OpenGL via Silk.NET
    .WithPlugin<OpenALAudioPlugin>()     // OpenAL audio
    .Build();

// Alternative: Swap in your preferred libraries
using var world = new WorldBuilder()
    .WithPlugin<MonoGameGraphicsPlugin>() // MonoGame rendering
    .WithPlugin<FmodAudioPlugin>()        // FMOD audio
    .WithPlugin<JoltPhysicsPlugin>()      // Jolt physics
    .Build();
```

The ECS core and your game logic don't change - only the plugins differ.

### Even the Core is Replaceable

The abstraction runs all the way down. `IWorld` itself is an interface - if you need a different ECS implementation (sparse sets instead of archetypes, SIMD-optimized queries, distributed entity storage), you can build your own:

```csharp
public class MySparseSetWorld : IWorld
{
    // Your custom ECS implementation
    // All plugins targeting IWorld still work
}
```

`KeenEyes.Core` is the default implementation, not a requirement. This ensures the ecosystem of plugins and tools remains useful even if you outgrow the default core.

## Purpose

When building plugins, libraries, or extensions for KeenEyes, you often want to:

- Keep dependencies minimal for faster compilation and smaller packages
- Avoid coupling to implementation details
- Enable testing with mock implementations
- Support multiple KeenEyes versions with a stable interface
- **Allow users to swap your implementation for alternatives**

The Abstractions package solves this by providing only the essential interfaces and types needed to define systems, components, and plugins.

## Installation

Reference only `KeenEyes.Abstractions` in your plugin project:

```xml
<PackageReference Include="KeenEyes.Abstractions" Version="1.0.0" />
```

Applications consuming your plugin will reference `KeenEyes.Core`, which implements all the abstractions.

## Core Interfaces

### IWorld

The `IWorld` interface defines essential world operations that systems and plugins need:

```csharp
public interface IWorld : IDisposable
{
    // World identification
    Guid Id { get; }
    string? Name { get; set; }

    // Entity operations
    int EntityCount { get; }
    bool IsAlive(Entity entity);
    bool Despawn(Entity entity);
    IEntityBuilder Spawn();
    IEntityBuilder Spawn(string? name);
    void Add<T>(Entity entity, in T component) where T : struct, IComponent;
    void Set<T>(Entity entity, in T component) where T : struct, IComponent;

    // Component operations
    ref T Get<T>(Entity entity) where T : struct, IComponent;
    bool Has<T>(Entity entity) where T : struct, IComponent;
    bool Remove<T>(Entity entity) where T : struct, IComponent;

    // Query operations - returns IQueryBuilder for fluent filtering
    IQueryBuilder Query<T1>() where T1 : struct, IComponent;
    IQueryBuilder Query<T1, T2>() /* ... */;
    IQueryBuilder Query<T1, T2, T3>() /* ... */;
    IQueryBuilder Query<T1, T2, T3, T4>() /* ... */;

    // Change tracking
    void EnableAutoTracking<T>() where T : struct, IComponent;
    void DisableAutoTracking<T>() where T : struct, IComponent;
    IEnumerable<Entity> GetDirtyEntities<T>() where T : struct, IComponent;
    void ClearDirtyFlags<T>() where T : struct, IComponent;

    // Component events
    EventSubscription OnComponentAdded<T>(Action<Entity, T> handler) where T : struct, IComponent;
    EventSubscription OnComponentRemoved<T>(Action<Entity> handler) where T : struct, IComponent;
    EventSubscription OnComponentChanged<T>(Action<Entity, T, T> handler) where T : struct, IComponent;

    // Entity lifecycle events
    EventSubscription OnEntityCreated(Action<Entity, string?> handler);
    EventSubscription OnEntityDestroyed(Action<Entity> handler);

    // Hierarchy operations
    void SetParent(Entity child, Entity parent);
    Entity GetParent(Entity entity);
    IEnumerable<Entity> GetChildren(Entity entity);

    // Extension operations
    T GetExtension<T>() where T : class;
    bool TryGetExtension<T>(out T? extension) where T : class;
    bool HasExtension<T>() where T : class;

    // Messaging operations
    void Send<T>(T message);
    EventSubscription Subscribe<T>(Action<T> handler);
    bool HasMessageSubscribers<T>();

    // Additional entity operations
    IEnumerable<Entity> GetAllEntities();
    IEnumerable<(Type Type, object Value)> GetComponents(Entity entity);
    void SetComponent(Entity entity, Type componentType, object value);

    // System execution
    void Update(float deltaTime);

    // Random number generation (deterministic if world seeded)
    int NextInt(int maxValue);
    int NextInt(int minValue, int maxValue);
    float NextFloat();
    double NextDouble();
    bool NextBool();
    bool NextBool(float probability);
}
```

The interface provides complete functionality for most plugin needs, including entity spawning, component operations, messaging, change tracking, and event subscriptions.

### IWorldPlugin

Plugins encapsulate related systems, components, and functionality:

```csharp
public interface IWorldPlugin
{
    string Name { get; }
    void Install(IPluginContext context);
    void Uninstall(IPluginContext context);
}
```

### IPluginContext

The plugin context provides access to system registration, extension APIs, and capabilities:

```csharp
public interface IPluginContext
{
    IWorld World { get; }
    IWorldPlugin Plugin { get; }

    // System registration (tracked for automatic cleanup)
    T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0) where T : ISystem, new();
    T AddSystem<T>(SystemPhase phase, int order, Type[] runsBefore, Type[] runsAfter) where T : ISystem, new();
    ISystem AddSystem(ISystem system, SystemPhase phase = SystemPhase.Update, int order = 0);
    ISystem AddSystem(ISystem system, SystemPhase phase, int order, Type[] runsBefore, Type[] runsAfter);
    SystemGroup AddSystemGroup(SystemGroup group, SystemPhase phase = SystemPhase.Update, int order = 0);

    // Extension management
    T GetExtension<T>() where T : class;
    bool TryGetExtension<T>(out T? extension) where T : class;
    void SetExtension<T>(T extension) where T : class;
    bool RemoveExtension<T>() where T : class;

    // Component registration
    void RegisterComponent<T>(bool isTag = false) where T : struct, IComponent;

    // Capability access (for advanced features without casting)
    T GetCapability<T>() where T : class;
    bool TryGetCapability<T>(out T? capability) where T : class;
    bool HasCapability<T>() where T : class;
}
```

### ISystem

Systems contain the logic that operates on entities:

```csharp
public interface ISystem : IDisposable
{
    bool Enabled { get; set; }
    void Initialize(IWorld world);
    void Update(float deltaTime);
}
```

### ISystemLifecycle

Optional lifecycle hooks for systems needing before/after update callbacks:

```csharp
public interface ISystemLifecycle
{
    void OnBeforeUpdate(float deltaTime);
    void OnAfterUpdate(float deltaTime);
}
```

### IComponent / ITagComponent

Marker interfaces for components:

```csharp
public interface IComponent;
public interface ITagComponent : IComponent;
```

### ICommandBuffer

Interface for queuing deferred entity operations:

```csharp
public interface ICommandBuffer
{
    int Count { get; }

    // Entity spawning
    EntityCommands Spawn();
    EntityCommands Spawn(string? name);

    // Entity operations
    void Despawn(Entity entity);
    void Despawn(int placeholderId);

    // Component operations
    void AddComponent<T>(Entity entity, T component) where T : struct, IComponent;
    void AddComponent<T>(int placeholderId, T component) where T : struct, IComponent;
    void RemoveComponent<T>(Entity entity) where T : struct, IComponent;
    void RemoveComponent<T>(int placeholderId) where T : struct, IComponent;
    void SetComponent<T>(Entity entity, T component) where T : struct, IComponent;
    void SetComponent<T>(int placeholderId, T component) where T : struct, IComponent;

    // Execution
    Dictionary<int, Entity> Flush(IWorld world);
    void Clear();
}
```

### IEntityBuilder

Interface for building entities with components:

```csharp
public interface IEntityBuilder
{
    IEntityBuilder With<T>(T component) where T : struct, IComponent;
    IEntityBuilder WithTag<T>() where T : struct, ITagComponent;
    IEntityBuilder WithParent(Entity parent);
    Entity Build();
}

// Generic version for type-safe fluent chaining
public interface IEntityBuilder<TSelf> : IEntityBuilder
    where TSelf : IEntityBuilder<TSelf>
{
    new TSelf With<T>(T component) where T : struct, IComponent;
    new TSelf WithTag<T>() where T : struct, ITagComponent;
    new TSelf WithParent(Entity parent);
}
```

The generic version enables type-safe method chaining while the non-generic version allows usage through the interface.

## Types

### Entity

A lightweight identifier for entities:

```csharp
public readonly record struct Entity(int Id, int Version)
{
    public static readonly Entity Null = new(-1, 0);
    public bool IsValid => Id >= 0;
}
```

### EntityCommands

A fluent builder for queuing entity spawns in command buffers:

```csharp
public sealed class EntityCommands : IEntityBuilder<EntityCommands>
{
    public int PlaceholderId { get; }
    public string? Name { get; }

    public EntityCommands With<T>(T component) where T : struct, IComponent;
    public EntityCommands WithTag<T>() where T : struct, ITagComponent;

    // Build() not supported - must use CommandBuffer.Flush()
    Entity Build();
}
```

Used via `CommandBuffer.Spawn()` to queue entity creation with components.

### SystemGroup

Groups multiple systems for organized execution:

```csharp
var physicsGroup = new SystemGroup("Physics")
    .Add<BroadphaseSystem>(order: 0)
    .Add<NarrowphaseSystem>(order: 10)
    .Add<SolverSystem>(order: 20);
```

## Writing a Plugin

Here's a complete example of a plugin using only Abstractions:

```csharp
using KeenEyes;

namespace MyPlugin;

// Define components using the marker interface
[Component]
public partial struct Velocity : IComponent
{
    public float X;
    public float Y;
}

// Define systems using the ISystem interface
public class MovementSystem : ISystem
{
    private IWorld? world;

    public bool Enabled { get; set; } = true;

    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    public void Update(float deltaTime)
    {
        if (world is null) return;

        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);

            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }

    public void Dispose() { }
}

// Define the plugin
public class MovementPlugin : IWorldPlugin
{
    public string Name => "Movement";

    public void Install(IPluginContext context)
    {
        context.AddSystem<MovementSystem>(SystemPhase.Update, order: 0);
    }

    public void Uninstall(IPluginContext context)
    {
        // Systems registered via context are automatically cleaned up
    }
}
```

## Exposing Custom APIs

Plugins can expose custom APIs through extensions:

```csharp
// Define the API interface
public interface IPhysicsWorld
{
    bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit);
    IEnumerable<Entity> QuerySphere(Vector3 center, float radius);
}

// Implementation (in your plugin)
internal class PhysicsWorld : IPhysicsWorld
{
    private readonly IWorld world;

    public PhysicsWorld(IWorld world)
    {
        this.world = world;
    }

    public bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit)
    {
        // Implementation...
    }

    public IEnumerable<Entity> QuerySphere(Vector3 center, float radius)
    {
        // Implementation...
    }
}

// Register in plugin
public class PhysicsPlugin : IWorldPlugin
{
    public string Name => "Physics";

    public void Install(IPluginContext context)
    {
        context.AddSystem<BroadphaseSystem>(SystemPhase.FixedUpdate, order: 0);
        context.AddSystem<NarrowphaseSystem>(SystemPhase.FixedUpdate, order: 10);
        context.AddSystem<SolverSystem>(SystemPhase.FixedUpdate, order: 20);

        // Expose the physics API
        context.SetExtension<IPhysicsWorld>(new PhysicsWorld(context.World));
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<IPhysicsWorld>();
    }
}

// Usage in application code
var physics = world.GetExtension<IPhysicsWorld>();
if (physics.Raycast(origin, direction, out var hit))
{
    Console.WriteLine($"Hit entity: {hit.Entity}");
}
```

## System Groups

Organize related systems using `SystemGroup`:

```csharp
public class RenderingPlugin : IWorldPlugin
{
    public string Name => "Rendering";

    public void Install(IPluginContext context)
    {
        var renderGroup = new SystemGroup("Render Pipeline")
            .Add<CullingSystem>(order: 0)
            .Add<ShadowMapSystem>(order: 10)
            .Add<OpaquePassSystem>(order: 20)
            .Add<TransparentPassSystem>(order: 30)
            .Add<PostProcessSystem>(order: 40);

        context.AddSystemGroup(renderGroup, SystemPhase.Render, order: 0);
    }

    public void Uninstall(IPluginContext context) { }
}
```

## When to Use Abstractions vs Core

| Use Case | Package |
|----------|---------|
| Writing plugins/extensions | `KeenEyes.Abstractions` |
| Writing unit tests with mocks | `KeenEyes.Abstractions` |
| Building applications | `KeenEyes.Core` |
| Advanced world operations | `KeenEyes.Core` |
| Entity spawning/building | `KeenEyes.Core` |

## Advanced: Accessing Core Features

When you need features beyond the abstractions (like entity spawning), cast to the concrete type:

```csharp
public void Initialize(IWorld world)
{
    this.world = world;

    // Cast to access full API when needed
    if (world is World concreteWorld)
    {
        var prefab = concreteWorld.CreatePrefab()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 1, Y = 0 })
            .Build();
    }
}
```

This approach keeps your plugin's core logic decoupled while still allowing access to advanced features when necessary.

## Using Command Buffers in Plugins

Plugins can use command buffers for deferred entity operations without depending on `KeenEyes.Core`:

```csharp
using KeenEyes;

public class SpawnerSystem : ISystem
{
    private readonly ICommandBuffer buffer = new CommandBuffer();
    private IWorld? world;

    public void Initialize(IWorld world)
    {
        this.world = world;
    }

    public void Update(float deltaTime)
    {
        if (world is null) return;

        // Queue spawns
        buffer.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 1, Y = 0 });

        // Queue component additions
        foreach (var entity in world.Query<Health>())
        {
            ref readonly var health = ref world.Get<Health>(entity);
            if (health.Current <= 0)
            {
                buffer.AddComponent(entity, new Dead());
            }
        }

        // Execute all queued operations
        buffer.Flush(world);
    }

    public void Dispose() { }
    public bool Enabled { get; set; } = true;
}
```

See the [Command Buffer Guide](command-buffer.md) for detailed usage patterns and best practices.

## Generated Extension Methods

Component generator creates extension methods that work seamlessly with `IEntityBuilder`:

```csharp
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

// Generated methods work with both generic and non-generic builders:

// Generic version (type-safe chaining)
public static TSelf WithPosition<TSelf>(this TSelf builder, float x, float y)
    where TSelf : IEntityBuilder<TSelf>

// Non-generic version (interface usage)
public static IEntityBuilder WithPosition(this IEntityBuilder builder, float x, float y)
```

This dual-generation enables:
- **Type-safe chaining** with concrete types (`EntityBuilder`, `EntityCommands`)
- **Interface compatibility** for plugin usage through `IWorld.Spawn()` and `CommandBuffer.Spawn()`

## Creating Swappable Subsystems

When building a subsystem (physics, audio, rendering, etc.) that users might want to swap, follow this pattern:

### 1. Define the Interface

Create an interface in a shared abstractions package:

```csharp
// In MyPhysics.Abstractions
public interface IPhysicsWorld
{
    void SetGravity(float x, float y, float z);
    bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit);
    void AddForce(Entity entity, Vector3 force);
}

public interface IPhysicsPlugin : IWorldPlugin
{
    IPhysicsWorld PhysicsWorld { get; }
}
```

### 2. Implement the Plugin

Create one or more implementations:

```csharp
// In MyPhysics.Jolt (uses Jolt physics)
public class JoltPhysicsPlugin : IPhysicsPlugin
{
    public string Name => "Physics.Jolt";
    private JoltPhysicsWorld? physicsWorld;

    public IPhysicsWorld PhysicsWorld => physicsWorld
        ?? throw new InvalidOperationException("Plugin not installed");

    public void Install(IPluginContext context)
    {
        physicsWorld = new JoltPhysicsWorld(context.World);
        context.SetExtension<IPhysicsWorld>(physicsWorld);
        context.AddSystem<JoltPhysicsSystem>(SystemPhase.FixedUpdate);
    }

    public void Uninstall(IPluginContext context)
    {
        context.RemoveExtension<IPhysicsWorld>();
        physicsWorld?.Dispose();
    }
}

// In MyPhysics.Bullet (alternative implementation)
public class BulletPhysicsPlugin : IPhysicsPlugin
{
    public string Name => "Physics.Bullet";
    // ... same interface, different implementation
}
```

### 3. Consume via Interface

Game code depends only on the interface:

```csharp
public class PlayerController : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Works with ANY physics implementation
        var physics = World.GetExtension<IPhysicsWorld>();

        foreach (var entity in World.Query<Player, Position>())
        {
            if (physics.Raycast(position, Vector3.Down, 1.0f, out var hit))
            {
                // Player is grounded
            }
        }
    }
}
```

### 4. User Chooses Implementation

Users pick the implementation at startup:

```csharp
// User prefers Jolt physics
using var world = new WorldBuilder()
    .WithPlugin<JoltPhysicsPlugin>()
    .Build();

// Another user prefers Bullet
using var world = new WorldBuilder()
    .WithPlugin<BulletPhysicsPlugin>()
    .Build();

// Game code works identically with both!
```

### Key Principles

1. **Interface in abstractions** - The contract lives in a package with no implementation dependencies
2. **Implementation in separate packages** - Each backend is its own package
3. **Register via extension** - Use `SetExtension<IInterface>()` so consumers use the interface type
4. **Same plugin name category** - Use names like `"Physics.Jolt"`, `"Physics.Bullet"` for clarity

This pattern enables the KeenEyes ecosystem where users can mix and match subsystems freely.

## Package Contents

The `KeenEyes.Abstractions` package includes:

- **Interfaces**: `IWorld`, `IWorldPlugin`, `IPluginContext`, `ISystem`, `ISystemLifecycle`, `IComponent`, `ITagComponent`, `IBundle`, `ICommandBuffer`, `IEntityBuilder`, `IEntityBuilder<TSelf>`, `IQueryBuilder`
- **Capability Interfaces**: `ISystemHookCapability`, `IPersistenceCapability`, `IHierarchyCapability`, `IValidationCapability`, `ITagCapability`, `IStatisticsCapability`, `IPrefabCapability`, `ISnapshotCapability`, `IInspectionCapability`
- **Types**: `Entity`, `EntityCommands`, `SystemGroup`, `SystemBase`, `EventSubscription`, `MemoryStats`, `EntityPrefab`
- **Enums**: `SystemPhase`, `ValidationMode`
- **Attributes**: `[Component]`, `[TagComponent]`, `[System]`, `[Bundle]`, `[Query]`, `[RunBefore]`, `[RunAfter]`, `[PluginExtension]`, and more
- **Internal**: `ICommand` (command execution interface)

The package has minimal dependencies, making it ideal for library authors who want to keep their dependency footprint small.
