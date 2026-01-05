# Basic Movement System

## Problem

You want entities to move based on velocity, with support for acceleration and delta-time-independent movement.

## Solution

### Components

```csharp
[Component]
public partial struct Position
{
    public float X;
    public float Y;
}

[Component]
public partial struct Velocity
{
    public float X;
    public float Y;
}

[Component]
public partial struct Acceleration
{
    public float X;
    public float Y;
}

[Component]
public partial struct MaxSpeed
{
    public float Value;
}
```

### Movement System

```csharp
public class MovementSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.Update;

    public override void Update(float deltaTime)
    {
        // Apply acceleration to velocity
        foreach (var entity in World.Query<Velocity, Acceleration>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            ref readonly var accel = ref World.Get<Acceleration>(entity);

            vel.X += accel.X * deltaTime;
            vel.Y += accel.Y * deltaTime;
        }

        // Clamp velocity to max speed
        foreach (var entity in World.Query<Velocity, MaxSpeed>())
        {
            ref var vel = ref World.Get<Velocity>(entity);
            ref readonly var maxSpeed = ref World.Get<MaxSpeed>(entity);

            var speed = MathF.Sqrt(vel.X * vel.X + vel.Y * vel.Y);
            if (speed > maxSpeed.Value)
            {
                var scale = maxSpeed.Value / speed;
                vel.X *= scale;
                vel.Y *= scale;
            }
        }

        // Apply velocity to position
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

### Usage

```csharp
using var world = new World();
world.AddSystem<MovementSystem>();

// Create a moving entity
var bullet = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 100, Y = 0 })
    .Build();

// Create an accelerating entity with max speed
var player = world.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 0, Y = 0 })
    .With(new Acceleration { X = 50, Y = 0 })  // Accelerate right
    .With(new MaxSpeed { Value = 200 })
    .Build();

// Game loop
while (running)
{
    world.Update(deltaTime);
}
```

## Why This Works

### Separation of Concerns

Each component has one job:
- `Position`: Where the entity is
- `Velocity`: How fast it's moving
- `Acceleration`: How velocity changes
- `MaxSpeed`: Movement limit

This allows mixing and matching. A bullet has Position + Velocity but no Acceleration. A player has all four. A static obstacle has only Position.

### Delta-Time Independence

Multiplying by `deltaTime` ensures consistent movement regardless of frame rate:
- At 60 FPS (dt=0.0167): `pos += vel * 0.0167`
- At 30 FPS (dt=0.0333): `pos += vel * 0.0333`

Both produce the same distance over one second.

### Query Efficiency

Separate queries for each concern mean:
- Entities without `Acceleration` skip the acceleration loop entirely
- Entities without `MaxSpeed` skip the clamping loop
- The archetype system ensures only relevant entities are iterated

## Variations

### 2D vs 3D

For 3D, add a Z component to each struct:

```csharp
[Component]
public partial struct Position3D
{
    public float X;
    public float Y;
    public float Z;
}
```

Or use `System.Numerics.Vector3` directly:

```csharp
[Component]
public partial struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
}
```

### Friction / Drag

Add a drag component to slow entities over time:

```csharp
[Component]
public partial struct Drag
{
    public float Factor;  // 0 = no drag, 1 = instant stop
}

// In system:
foreach (var entity in World.Query<Velocity, Drag>())
{
    ref var vel = ref World.Get<Velocity>(entity);
    ref readonly var drag = ref World.Get<Drag>(entity);

    var factor = 1f - (drag.Factor * deltaTime);
    vel.X *= factor;
    vel.Y *= factor;
}
```

### Fixed Timestep

For physics-like consistency, use fixed timestep accumulation:

```csharp
public class FixedMovementSystem : SystemBase
{
    private const float FixedStep = 1f / 60f;  // 60 Hz
    private float accumulator = 0f;

    public override void Update(float deltaTime)
    {
        accumulator += deltaTime;

        while (accumulator >= FixedStep)
        {
            UpdatePhysics(FixedStep);
            accumulator -= FixedStep;
        }
    }

    private void UpdatePhysics(float dt)
    {
        // Movement code here with fixed dt
    }
}
```

## See Also

- [Core Concepts](../concepts.md) - ECS fundamentals
- [Systems Guide](../systems.md) - System patterns
- [Queries Guide](../queries.md) - Query filtering
