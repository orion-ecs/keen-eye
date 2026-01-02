# Entities Guide

Entities are the fundamental building blocks in ECS. This guide covers everything you need to know about working with entities in KeenEyes.

## What is an Entity?

An entity is just an identifier - a lightweight handle that references a collection of components:

```csharp
public readonly record struct Entity(int Id, int Version);
```

- **Id**: A unique integer identifier within the world
- **Version**: A generation counter to detect stale references

Entities don't store data themselves. Components give entities their properties.

## Creating Entities

### Basic Entity Creation

Use `World.Spawn()` to create entities:

```csharp
using var world = new World();

// Create an empty entity
var entity = world.Spawn().Build();

// Create an entity with components
var player = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 1, Y = 0 })
    .Build();
```

### Named Entities

Entities can have names for debugging and lookup:

```csharp
var player = world.Spawn("Player")
    .With(new Position { X = 0, Y = 0 })
    .Build();

var camera = world.Spawn("MainCamera")
    .With(new Position { X = 0, Y = 0 })
    .Build();

// Retrieve by name later
var found = world.GetEntityByName("Player");
```

Names must be unique within a world.

### Fluent Builder Pattern

The spawn builder supports method chaining:

```csharp
var entity = world.Spawn()
    .With(new Position { X = 100, Y = 50 })
    .With(new Velocity { X = 1, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .WithTag<Player>()  // Add a tag component
    .Build();
```

## Entity Lifecycle

### Checking Entity State

```csharp
// Check if entity reference points to a living entity
if (world.IsAlive(entity))
{
    // Entity still exists and can be accessed
}

// Check for null/invalid entity
if (entity.IsValid)
{
    // Entity handle is not Entity.Null
}
```

### Destroying Entities

Use `World.Despawn()` to remove an entity:

```csharp
world.Despawn(entity);

// Entity is now dead
Console.WriteLine(world.IsAlive(entity));  // False
```

After despawning:
- The entity ID is recycled for future use
- The version counter increments
- All components are removed
- Stale references will fail `IsAlive()` checks

### Entity Versioning

Versioning prevents use-after-free bugs:

```csharp
var entity = world.Spawn().Build();
Console.WriteLine(entity);  // Entity(0v1)

world.Despawn(entity);

var newEntity = world.Spawn().Build();
Console.WriteLine(newEntity);  // Entity(0v2) - same ID, new version

// Old reference is detected as stale
Console.WriteLine(world.IsAlive(entity));     // False (v1 is dead)
Console.WriteLine(world.IsAlive(newEntity));  // True (v2 is alive)
```

## Working with Components

### Adding Components

```csharp
// At creation time
var entity = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .Build();

// After creation
world.Add(entity, new Velocity { X = 1, Y = 0 });
```

### Getting Components

Use `ref` returns for zero-copy access:

```csharp
// Mutable access
ref var pos = ref world.Get<Position>(entity);
pos.X += 10;  // Modifies the actual component

// Read-only access (prevents accidental modification)
ref readonly var vel = ref world.Get<Velocity>(entity);
float speed = vel.X;
```

### Setting Components

Replace a component's value:

```csharp
world.Set(entity, new Position { X = 100, Y = 200 });
```

### Removing Components

```csharp
world.Remove<Velocity>(entity);
```

### Checking Component Presence

```csharp
if (world.Has<Velocity>(entity))
{
    ref var vel = ref world.Get<Velocity>(entity);
    // Use velocity...
}
```

### Getting All Components

```csharp
foreach (var (type, value) in world.GetComponents(entity))
{
    Console.WriteLine($"{type.Name}: {value}");
}
```

## Entity Count and Statistics

```csharp
// Total living entities
int count = world.EntityCount;

// Memory statistics
var stats = world.GetMemoryStats();
Console.WriteLine($"Active entities: {stats.EntitiesActive}");
Console.WriteLine($"Total allocated: {stats.EntitiesAllocated}");
Console.WriteLine($"Archetypes: {stats.ArchetypeCount}");
Console.WriteLine($"Query cache hit rate: {stats.QueryCacheHitRate:F1}%");
Console.WriteLine($"Estimated storage: {stats.EstimatedComponentBytes / 1024.0:F1} KB");
```

The `MemoryStats` struct includes:

| Property | Description |
|----------|-------------|
| `EntitiesActive` | Number of currently alive entities |
| `EntitiesAllocated` | Total entities ever allocated |
| `EntitiesRecycled` | Entity IDs available for reuse |
| `EntityRecycleCount` | Total recycling operations |
| `ArchetypeCount` | Number of active archetypes |
| `ComponentTypeCount` | Registered component types |
| `SystemCount` | Registered systems |
| `CachedQueryCount` | Cached query count |
| `QueryCacheHits` / `QueryCacheMisses` | Cache statistics |
| `EstimatedComponentBytes` | Estimated storage size |

## Null Entity

`Entity.Null` represents an invalid/missing entity:

```csharp
Entity target = Entity.Null;

if (target.IsValid)
{
    // Won't execute - Entity.Null is not valid
}

// Common pattern for optional references
public struct Target : IComponent
{
    public Entity Entity;  // Entity.Null if no target
}
```

## Deferred Entity Operations

When modifying entities during iteration, use `CommandBuffer`:

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Health>())
{
    ref var health = ref world.Get<Health>(entity);
    if (health.Current <= 0)
    {
        // Don't despawn directly during iteration!
        buffer.Despawn(entity);
    }
}

// Apply all changes after iteration
buffer.Flush(world);
```

See [Command Buffer](command-buffer.md) for details.

## Best Practices

### Do: Use Entity for References

```csharp
// Good: Store entity references
public struct Target : IComponent
{
    public Entity Entity;
}

// Good: Check if reference is still valid
if (world.IsAlive(target.Entity))
{
    ref var pos = ref world.Get<Position>(target.Entity);
}
```

### Don't: Store Component References Long-Term

```csharp
// BAD: Storing ref across frames - may become invalid!
ref var pos = ref world.Get<Position>(entity);
// ... later, after archetypes may have changed...
pos.X = 100;  // DANGER: Reference may be invalid!

// GOOD: Get fresh reference when needed
ref var pos = ref world.Get<Position>(entity);
pos.X = 100;
```

### Do: Prefer Composition Over Large Components

```csharp
// Bad: One huge component
public struct Actor : IComponent
{
    public float X, Y;
    public float VelX, VelY;
    public int Health, MaxHealth;
    // ... many more fields
}

// Good: Many small, focused components
public struct Position : IComponent { public float X, Y; }
public struct Velocity : IComponent { public float X, Y; }
public struct Health : IComponent { public int Current, Max; }
```

## Next Steps

- [Components Guide](components.md) - Component patterns and best practices
- [Queries Guide](queries.md) - Filtering and iterating entities
- [Command Buffer](command-buffer.md) - Deferred operations
