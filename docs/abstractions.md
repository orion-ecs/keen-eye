# KeenEyes.Abstractions

The **KeenEyes.Abstractions** package provides lightweight interfaces and types for authoring plugins and extensions without depending on the full `KeenEyes.Core` runtime.

## Purpose

When building plugins, libraries, or extensions for KeenEyes, you often want to:

- Keep dependencies minimal for faster compilation and smaller packages
- Avoid coupling to implementation details
- Enable testing with mock implementations
- Support multiple KeenEyes versions with a stable interface

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
    // Entity spawning
    IEntityBuilder Spawn();
    IEntityBuilder Spawn(string? name);

    // Entity operations
    bool IsAlive(Entity entity);
    bool Despawn(Entity entity);

    // Component operations
    ref T Get<T>(Entity entity) where T : struct, IComponent;
    bool Has<T>(Entity entity) where T : struct, IComponent;
    bool Remove<T>(Entity entity) where T : struct, IComponent;
    void Add<T>(Entity entity, T component) where T : struct, IComponent;
    void Set<T>(Entity entity, T component) where T : struct, IComponent;

    // Query operations
    IEnumerable<Entity> Query<T1>() where T1 : struct, IComponent;
    IEnumerable<Entity> Query<T1, T2>() /* ... */;
    IEnumerable<Entity> Query<T1, T2, T3>() /* ... */;
    IEnumerable<Entity> Query<T1, T2, T3, T4>() /* ... */;

    // Extension operations
    T GetExtension<T>() where T : class;
    bool TryGetExtension<T>(out T? extension) where T : class;
    bool HasExtension<T>() where T : class;
}
```

The interface provides complete functionality for most plugin needs, including entity spawning and component operations.

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

The plugin context provides access to system registration and extension APIs:

```csharp
public interface IPluginContext
{
    IWorld World { get; }
    IWorldPlugin Plugin { get; }

    // System registration
    T AddSystem<T>(SystemPhase phase = SystemPhase.Update, int order = 0)
        where T : ISystem, new();

    // Extension management
    void SetExtension<T>(T extension) where T : class;
    bool RemoveExtension<T>() where T : class;
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
    Entity Build();
}

// Generic version for type-safe fluent chaining
public interface IEntityBuilder<out TSelf> : IEntityBuilder
    where TSelf : IEntityBuilder<TSelf>
{
    new TSelf With<T>(T component) where T : struct, IComponent;
    new TSelf WithTag<T>() where T : struct, ITagComponent;
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

## Package Contents

The `KeenEyes.Abstractions` package includes:

- **Interfaces**: `IWorld`, `IWorldPlugin`, `IPluginContext`, `ISystem`, `ISystemLifecycle`, `IComponent`, `ITagComponent`, `ICommandBuffer`, `IEntityBuilder`, `IEntityBuilder<TSelf>`
- **Types**: `Entity`, `EntityCommands`, `SystemGroup`
- **Enums**: `SystemPhase` (via `KeenEyes.Generators.Attributes` dependency)
- **Internal**: `ICommand` (command execution interface)

The package has minimal dependencies, making it ideal for library authors who want to keep their dependency footprint small.
