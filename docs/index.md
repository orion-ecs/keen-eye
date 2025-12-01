# KeenEye Documentation

Welcome to the KeenEye ECS framework documentation.

## What is KeenEye?

KeenEye is a high-performance Entity Component System (ECS) framework for .NET 10, reimplementing [OrionECS](https://github.com/tyevco/OrionECS) in C#.

## Key Features

- **No Static State** - All state is instance-based. Each `World` is completely isolated.
- **Components are Structs** - Cache-friendly, value semantics for optimal performance.
- **Entities are IDs** - Lightweight `(int Id, int Version)` tuples for staleness detection.
- **Fluent Queries** - `world.Query<A, B>().With<C>().Without<D>()`
- **Source Generators** - Reduce boilerplate while maintaining performance.

## Getting Started

```csharp
using KeenEye.Core;

// Create a world
var world = new World();

// Create an entity with components
var entity = world.CreateEntity()
    .WithPosition(10, 20)
    .WithVelocity(1, 0)
    .Build();

// Query entities
foreach (var (pos, vel) in world.Query<Position, Velocity>())
{
    // Process entities with Position and Velocity
}
```

## API Reference

See the [API Documentation](api/index.md) for detailed reference.
