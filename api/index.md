# API Reference

Welcome to the KeenEyes ECS API documentation.

This reference is automatically generated from XML documentation comments in the source code.

## Namespaces

- **KeenEyes.Core** - Core ECS types including `World`, `Entity`, components, and queries
- **KeenEyes.Generators.Attributes** - Attributes for source generator code generation

## Getting Started

The main entry point is the @KeenEyes.Core.World class:

```csharp
using KeenEyes.Core;

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
| **World** | Isolated container for entities, components, and systems |
| **Entity** | Lightweight ID with version for staleness detection |
| **Component** | Plain data struct attached to entities |
| **Query** | Fluent API for filtering and iterating entities |

Browse the namespace documentation below for detailed API information.
