# Getting Started

This guide walks you through building a simple simulation with KeenEyes ECS.

## Prerequisites

- .NET 10 SDK
- A code editor (VS Code, Visual Studio, Rider)

## Installation

Add the KeenEyes packages to your project:

```bash
dotnet add package KeenEyes.Core
```

The `KeenEyes.Core` package includes all necessary types. For plugin development or minimal dependencies, you may use:

```bash
dotnet add package KeenEyes.Abstractions
```

## Step 1: Define Components

Components are plain data structs. Let's define some for a 2D particle simulation:

```csharp
using KeenEyes;

// Position in 2D space
public struct Position : IComponent
{
    public float X;
    public float Y;
}

// Movement velocity
public struct Velocity : IComponent
{
    public float X;
    public float Y;
}

// Visual appearance
public struct Color : IComponent
{
    public float R;
    public float G;
    public float B;
}

// Particle lifetime
public struct Lifetime : IComponent
{
    public float Remaining;
}
```

## Step 2: Create a World and Entities

The `World` is your ECS container. Use it to spawn entities:

```csharp
using KeenEyes;

// Create the world
using var world = new World();

// Spawn a particle entity
var particle = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 10, Y = 5 })
    .With(new Color { R = 1, G = 0, B = 0 })
    .With(new Lifetime { Remaining = 2.0f })
    .Build();

Console.WriteLine($"Created particle: {particle}");
```

## Step 3: Create Systems

Systems contain the logic that processes entities. Let's create two systems:

### Movement System

Updates positions based on velocity:

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

### Lifetime System

Decreases lifetime and removes expired particles:

```csharp
public class LifetimeSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Lifetime>())
        {
            ref var lifetime = ref World.Get<Lifetime>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.Remaining <= 0)
            {
                // Queue despawn - don't modify during iteration!
                buffer.Despawn(entity);
            }
        }

        // Execute all despawns after iteration
        buffer.Flush(World);
    }
}
```

## Step 4: Register Systems and Run

```csharp
using KeenEyes;

using var world = new World();

// Spawn some particles
for (int i = 0; i < 100; i++)
{
    world.Spawn()
        .With(new Position { X = Random.Shared.NextSingle() * 100, Y = 0 })
        .With(new Velocity { X = 0, Y = Random.Shared.NextSingle() * 20 })
        .With(new Lifetime { Remaining = Random.Shared.NextSingle() * 3 })
        .Build();
}

// Register systems
world
    .AddSystem<MovementSystem>()
    .AddSystem<LifetimeSystem>();

// Game loop
var deltaTime = 0.016f;  // ~60 FPS
while (world.EntityCount > 0)
{
    world.Update(deltaTime);
    Console.WriteLine($"Particles remaining: {world.EntityCount}");
    Thread.Sleep(16);
}

Console.WriteLine("All particles expired!");
```

## Step 5: Use Source Generators (Optional)

KeenEyes provides source generators to reduce boilerplate. Add the `[Component]` attribute:

```csharp
using KeenEyes;

[Component]
public partial struct Position
{
    public float X;
    public float Y;
}
```

This generates a `WithPosition(float x, float y)` extension method:

```csharp
// Before (manual)
world.Spawn()
    .With(new Position { X = 10, Y = 20 })
    .Build();

// After (generated)
world.Spawn()
    .WithPosition(x: 10, y: 20)
    .Build();
```

## Step 6: Use Component Bundles (Optional)

For entities with many components, use bundles to group related components:

```csharp
using KeenEyes;

[Component]
public partial struct Position { public float X, Y; }

[Component]
public partial struct Velocity { public float X, Y; }

[Component]
public partial struct Color { public float R, G, B; }

// Define a bundle for particle components
[Bundle]
public partial struct ParticleBundle
{
    public Position Position;
    public Velocity Velocity;
    public Color Color;
}

// Spawn with bundle (concise)
var particle = world.Spawn()
    .With(new ParticleBundle
    {
        Position = new() { X = 0, Y = 0 },
        Velocity = new() { X = 10, Y = 5 },
        Color = new() { R = 1, G = 0, B = 0 }
    })
    .With(new Lifetime { Remaining = 2.0f })
    .Build();
```

Bundles are especially useful for commonly-used component combinations like transforms, physics bodies, or character stats. See the [Bundles Guide](bundles.md) for more details.

## Complete Example

Here's the full program:

```csharp
using KeenEyes;

// Components
public struct Position : IComponent { public float X, Y; }
public struct Velocity : IComponent { public float X, Y; }
public struct Lifetime : IComponent { public float Remaining; }

// Systems
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

public class LifetimeSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Lifetime>())
        {
            ref var lifetime = ref World.Get<Lifetime>(entity);
            lifetime.Remaining -= deltaTime;
            if (lifetime.Remaining <= 0)
                buffer.Despawn(entity);
        }
        buffer.Flush(World);
    }
}

// Main program
using var world = new World();

// Spawn particles
for (int i = 0; i < 100; i++)
{
    world.Spawn()
        .With(new Position { X = Random.Shared.NextSingle() * 100, Y = 0 })
        .With(new Velocity { X = 0, Y = Random.Shared.NextSingle() * 20 })
        .With(new Lifetime { Remaining = Random.Shared.NextSingle() * 3 })
        .Build();
}

// Register and run
world.AddSystem<MovementSystem>().AddSystem<LifetimeSystem>();

while (world.EntityCount > 0)
{
    world.Update(0.016f);
    Console.WriteLine($"Particles: {world.EntityCount}");
    Thread.Sleep(16);
}
```

## Next Steps

- [Core Concepts](concepts.md) - Understand ECS fundamentals
- [Entities Guide](entities.md) - Entity lifecycle and management
- [Components Guide](components.md) - Component patterns
- [Bundles Guide](bundles.md) - Group components for easier spawning
- [Systems Guide](systems.md) - System design patterns
- [Command Buffer](command-buffer.md) - Safe entity modification during iteration
