# Core Concepts

KeenEyes is built on the Entity Component System (ECS) architectural pattern. This page explains the fundamental concepts you need to understand.

## What is ECS?

ECS separates **data** from **logic**:

- **Entities** are unique identifiers (just IDs)
- **Components** are plain data attached to entities
- **Systems** contain logic that processes entities with specific components

This separation enables:
- Cache-friendly memory layouts for high performance
- Flexible composition (entities are defined by their components, not inheritance)
- Easy parallelization (systems operate on independent data)

## World

The `World` is the container for everything in your ECS. It holds:

- All entities and their components
- Registered systems
- Singletons (world-level resources)
- Component registry (type information)

```csharp
using var world = new World();
```

**Key principle:** Each `World` is completely isolated. There's no shared static state between worlds. This enables:

- Running multiple independent simulations
- Easy unit testing with isolated worlds
- No hidden initialization order dependencies

## Entity

An `Entity` is just an ID with a version number:

```csharp
public readonly record struct Entity(int Id, int Version);
```

- **Id**: Unique identifier within the world
- **Version**: Generation counter to detect stale references

Entities don't store data themselves - they're just handles. Components give entities their properties and behavior.

```csharp
var entity = world.Spawn().Build();  // Create an empty entity

// Check if an entity reference is still valid
if (world.IsAlive(entity))
{
    // Entity still exists
}
```

### Named Entities

Entities can optionally have names for debugging and lookup:

```csharp
var player = world.Spawn("Player").Build();

// Later, retrieve by name
var found = world.GetEntityByName("Player");
```

## Component

Components are plain data structs that attach to entities. They implement `IComponent`:

```csharp
public struct Position : IComponent
{
    public float X;
    public float Y;
}

public struct Velocity : IComponent
{
    public float X;
    public float Y;
}

public struct Health : IComponent
{
    public int Current;
    public int Max;
}
```

### Tag Components

Tag components have no data - they just mark entities:

```csharp
public struct Player : ITagComponent { }
public struct Enemy : ITagComponent { }
public struct Disabled : ITagComponent { }
```

### Component Guidelines

- **Keep components small and focused** - One responsibility per component
- **Use structs, not classes** - Value semantics and cache-friendly
- **No logic in components** - Components are pure data
- **Prefer composition** - Many small components > one large component

## Query

Queries filter and iterate over entities with specific components:

```csharp
// All entities with Position AND Velocity
foreach (var entity in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(entity);
    ref readonly var vel = ref world.Get<Velocity>(entity);

    pos.X += vel.X;
    pos.Y += vel.Y;
}
```

### Query Filters

Use `With<T>()` and `Without<T>()` to refine queries:

```csharp
// Entities with Position, Velocity, AND the Enemy tag
foreach (var entity in world.Query<Position, Velocity>().With<Enemy>())
{
    // Process enemies only
}

// Entities with Position, Velocity, but NOT Disabled
foreach (var entity in world.Query<Position, Velocity>().Without<Disabled>())
{
    // Skip disabled entities
}

// Combine filters
foreach (var entity in world.Query<Position>().With<Enemy>().Without<Disabled>())
{
    // Active enemies only
}
```

## System

Systems contain the logic that processes entities. They implement `ISystem` or extend `SystemBase`:

```csharp
public class MovementSystem : SystemBase
{
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
}
```

### Registering Systems

```csharp
world
    .AddSystem<InputSystem>()
    .AddSystem<MovementSystem>()
    .AddSystem<RenderSystem>();

// Update all systems
world.Update(deltaTime: 0.016f);
```

## Singletons

Singletons are world-level data not tied to any entity:

```csharp
// Set a singleton
world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 0f });

// Get a singleton (by reference for zero-copy access)
ref var time = ref world.GetSingleton<GameTime>();
time.TotalTime += time.DeltaTime;

// Check if singleton exists
if (world.HasSingleton<GameConfig>())
{
    // ...
}
```

Common uses for singletons:
- Game time / delta time
- Input state
- Configuration
- Random number generators
- Asset references

## Next Steps

- [Getting Started](getting-started.md) - Build your first ECS application
- [Entities Guide](entities.md) - Deep dive into entity management
- [Components Guide](components.md) - Component patterns and best practices
- [Queries Guide](queries.md) - Advanced query techniques
- [Systems Guide](systems.md) - System patterns and execution
