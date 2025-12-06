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
var world = new World();

// Create entities with components
var entity = world.CreateEntity();

// Query entities
foreach (var e in world.Query<Position, Velocity>())
{
    ref var pos = ref world.Get<Position>(e);
    ref readonly var vel = ref world.Get<Velocity>(e);

    pos.X += vel.X;
    pos.Y += vel.Y;
}

// Clean up
world.Dispose();
```

## Key Concepts

| Concept | Description |
|---------|-------------|
| **World** | Isolated container for entities, components, and systems |
| **Entity** | Lightweight ID with version for staleness detection |
| **Component** | Plain data struct attached to entities |
| **Query** | Fluent API for filtering and iterating entities |

Browse the namespace documentation below for detailed API information.
