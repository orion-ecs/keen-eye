# KeenEyes Documentation

Welcome to the KeenEyes ECS framework documentation.

## What is KeenEyes?

KeenEyes is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Key Features

- **No Static State** - All state is instance-based. Each `World` is completely isolated.
- **Components are Structs** - Cache-friendly, value semantics for optimal performance.
- **Entities are IDs** - Lightweight `(int Id, int Version)` tuples for staleness detection.
- **Fluent Queries** - `world.Query<A, B>().With<C>().Without<D>()`
- **Source Generators** - Reduce boilerplate while maintaining performance.

## Getting Started

```csharp
using KeenEyes.Core;

// Create a world
using var world = new World();

// Create an entity with components using the fluent builder
var entity = world.Spawn()
    .With(new Position { X = 10, Y = 20 })
    .With(new Velocity { X = 1, Y = 0 })
    .Build();

// Query and process entities
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);
    // Process entities with Position and Velocity
}
```

## Documentation

### Tutorials
- [Getting Started](getting-started.md) - Build your first ECS application

### Concepts
- [Core Concepts](concepts.md) - Understand ECS fundamentals

### Guides
- [Entities](entities.md) - Entity lifecycle and management
- [Components](components.md) - Component patterns and best practices
- [Queries](queries.md) - Filtering and iterating entities
- [Systems](systems.md) - System design patterns
- [Command Buffer](command-buffer.md) - Safe entity modification during iteration
- [Singletons](singletons.md) - World-level resources
- [Relationships](relationships.md) - Parent-child entity hierarchies
- [Events](events.md) - Component and entity lifecycle events
- [Change Tracking](change-tracking.md) - Track component modifications

## API Reference

See the [API Documentation](../api/index.md) for detailed reference.
