# Physics Integration

## Problem

You want to integrate a physics engine (BepuPhysics, Box2D, etc.) with your ECS while keeping concerns separated.

## Solution

### Bridge Components

```csharp
// Links an ECS entity to a physics body
[Component]
public partial struct PhysicsBody : IComponent
{
    public int BodyHandle;  // Physics engine's body ID
    public BodyType Type;
}

public enum BodyType
{
    Static,
    Dynamic,
    Kinematic
}

// Physics shape definition
[Component]
public partial struct PhysicsShape : IComponent
{
    public ShapeType Type;
    public float Width;
    public float Height;
    public float Radius;
}

public enum ShapeType
{
    Box,
    Circle,
    Capsule
}

// Physics material properties
[Component]
public partial struct PhysicsMaterial : IComponent
{
    public float Friction;
    public float Restitution;  // Bounciness
    public float Density;
}
```

### Physics World Singleton

```csharp
public sealed class PhysicsWorldWrapper
{
    // Your physics engine instance (e.g., BepuPhysics.Simulation)
    public Simulation Simulation { get; }

    private readonly Dictionary<int, Entity> bodyToEntity = new();
    private readonly Dictionary<Entity, int> entityToBody = new();

    public PhysicsWorldWrapper()
    {
        Simulation = Simulation.Create(
            new BufferPool(),
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(new Vector3(0, -9.81f, 0))
        );
    }

    public void RegisterBody(Entity entity, int bodyHandle)
    {
        bodyToEntity[bodyHandle] = entity;
        entityToBody[entity] = bodyHandle;
    }

    public Entity? GetEntity(int bodyHandle)
    {
        return bodyToEntity.TryGetValue(bodyHandle, out var entity) ? entity : null;
    }

    public int? GetBodyHandle(Entity entity)
    {
        return entityToBody.TryGetValue(entity, out var handle) ? handle : null;
    }
}
```

### Physics Sync System (ECS → Physics)

```csharp
public class PhysicsSyncToEngineSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.PrePhysics;

    public override void Update(float deltaTime)
    {
        var physics = World.GetSingleton<PhysicsWorldWrapper>();

        // Sync kinematic bodies from ECS to physics
        foreach (var entity in World.Query<PhysicsBody, Position, Rotation>())
        {
            ref readonly var body = ref World.Get<PhysicsBody>(entity);

            if (body.Type != BodyType.Kinematic)
                continue;

            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var rot = ref World.Get<Rotation>(entity);

            // Update physics body position
            var bodyRef = physics.Simulation.Bodies.GetBodyReference(new BodyHandle(body.BodyHandle));
            bodyRef.Pose.Position = new Vector3(pos.X, pos.Y, 0);
            bodyRef.Pose.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, rot.Angle);
        }
    }
}
```

### Physics Step System

```csharp
public class PhysicsStepSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.Physics;

    private const float FixedTimestep = 1f / 60f;
    private float accumulator;

    public override void Update(float deltaTime)
    {
        var physics = World.GetSingleton<PhysicsWorldWrapper>();

        accumulator += deltaTime;

        while (accumulator >= FixedTimestep)
        {
            physics.Simulation.Timestep(FixedTimestep);
            accumulator -= FixedTimestep;
        }
    }
}
```

### Physics Sync System (Physics → ECS)

```csharp
public class PhysicsSyncFromEngineSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.PostPhysics;

    public override void Update(float deltaTime)
    {
        var physics = World.GetSingleton<PhysicsWorldWrapper>();

        // Sync dynamic bodies from physics to ECS
        foreach (var entity in World.Query<PhysicsBody, Position, Rotation>())
        {
            ref readonly var body = ref World.Get<PhysicsBody>(entity);

            if (body.Type != BodyType.Dynamic)
                continue;

            var bodyRef = physics.Simulation.Bodies.GetBodyReference(new BodyHandle(body.BodyHandle));

            // Update ECS components from physics state
            ref var pos = ref World.Get<Position>(entity);
            pos.X = bodyRef.Pose.Position.X;
            pos.Y = bodyRef.Pose.Position.Y;

            ref var rot = ref World.Get<Rotation>(entity);
            // Extract Z rotation from quaternion
            rot.Angle = 2f * MathF.Atan2(bodyRef.Pose.Orientation.Z, bodyRef.Pose.Orientation.W);

            // Optionally sync velocity
            if (World.Has<Velocity>(entity))
            {
                ref var vel = ref World.Get<Velocity>(entity);
                vel.X = bodyRef.Velocity.Linear.X;
                vel.Y = bodyRef.Velocity.Linear.Y;
            }
        }
    }
}
```

### Physics Body Creation

```csharp
public class PhysicsBodyFactory
{
    private readonly World ecsWorld;
    private readonly PhysicsWorldWrapper physics;

    public PhysicsBodyFactory(World ecsWorld)
    {
        this.ecsWorld = ecsWorld;
        this.physics = ecsWorld.GetSingleton<PhysicsWorldWrapper>();
    }

    public void CreateDynamicBody(Entity entity)
    {
        ref readonly var pos = ref ecsWorld.Get<Position>(entity);
        ref readonly var shape = ref ecsWorld.Get<PhysicsShape>(entity);

        var material = ecsWorld.TryGet<PhysicsMaterial>(entity, out var mat)
            ? mat
            : new PhysicsMaterial { Friction = 0.5f, Restitution = 0.3f, Density = 1f };

        // Create physics shape
        var physicsShape = shape.Type switch
        {
            ShapeType.Box => new Box(shape.Width, shape.Height, 1f),
            ShapeType.Circle => new Sphere(shape.Radius),
            _ => throw new NotSupportedException()
        };

        var shapeIndex = physics.Simulation.Shapes.Add(physicsShape);

        // Create body
        var bodyDescription = BodyDescription.CreateDynamic(
            new RigidPose(new Vector3(pos.X, pos.Y, 0)),
            physicsShape.ComputeInertia(material.Density),
            new CollidableDescription(shapeIndex, 0.1f),
            new BodyActivityDescription(0.01f)
        );

        var bodyHandle = physics.Simulation.Bodies.Add(bodyDescription);

        // Link to ECS
        ecsWorld.Add(entity, new PhysicsBody
        {
            BodyHandle = bodyHandle.Value,
            Type = BodyType.Dynamic
        });

        physics.RegisterBody(entity, bodyHandle.Value);
    }
}
```

## Why This Works

### Clear Ownership

- **ECS owns** gameplay data (health, inventory, AI state)
- **Physics engine owns** physical state (mass, forces, collisions)
- **Bridge components** link the two

### System Phase Separation

```
PrePhysics → Physics → PostPhysics → Update
    ↓           ↓           ↓
ECS→Physics  Simulate  Physics→ECS
```

This prevents:
- Reading stale physics data
- Overwriting physics changes
- Order-dependent bugs

### Fixed Timestep Physics

Physics engines need consistent time steps:
- Variable `deltaTime` causes instability
- Accumulator pattern handles frame rate variation
- ECS can still use variable time for visuals

### Entity-Body Mapping

The bidirectional map enables:
- Finding ECS entity from collision callback
- Finding physics body to apply forces
- Cleanup when entities are despawned

## Variations

### Collision Events

```csharp
// Collision data as component (added by physics callback)
[Component]
public partial struct CollisionEvent : IComponent
{
    public Entity Other;
    public Vector2 Normal;
    public float Penetration;
}

// Physics callback implementation
public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    private World ecsWorld;
    private PhysicsWorldWrapper physics;

    public void OnContactAdded(/* collision data */)
    {
        var entityA = physics.GetEntity(bodyHandleA);
        var entityB = physics.GetEntity(bodyHandleB);

        if (entityA.HasValue && entityB.HasValue)
        {
            ecsWorld.Add(entityA.Value, new CollisionEvent
            {
                Other = entityB.Value,
                Normal = normal,
                Penetration = depth
            });
        }
    }
}

// System to process collision events
public class CollisionHandlerSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<CollisionEvent, Health>())
        {
            ref readonly var collision = ref World.Get<CollisionEvent>(entity);

            // Apply damage on collision
            if (World.Has<DamageOnContact>(collision.Other))
            {
                var damage = World.Get<DamageOnContact>(collision.Other);
                buffer.Add(entity, new DamageReceived { Amount = damage.Amount });
            }

            // Remove processed event
            buffer.Remove<CollisionEvent>(entity);
        }

        buffer.Execute();
    }
}
```

### Force Application via Components

```csharp
[Component]
public partial struct ApplyForce : IComponent
{
    public Vector2 Force;
    public ForceMode Mode;
}

public enum ForceMode
{
    Force,      // Continuous force
    Impulse,    // Instant velocity change
    Acceleration // Ignores mass
}

public class ForceApplicationSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.PrePhysics;

    public override void Update(float deltaTime)
    {
        var physics = World.GetSingleton<PhysicsWorldWrapper>();
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<PhysicsBody, ApplyForce>())
        {
            ref readonly var body = ref World.Get<PhysicsBody>(entity);
            ref readonly var force = ref World.Get<ApplyForce>(entity);

            var bodyRef = physics.Simulation.Bodies.GetBodyReference(new BodyHandle(body.BodyHandle));

            switch (force.Mode)
            {
                case ForceMode.Impulse:
                    bodyRef.ApplyLinearImpulse(new Vector3(force.Force.X, force.Force.Y, 0));
                    break;
                case ForceMode.Force:
                    // Force is applied per-frame, so scale by timestep
                    bodyRef.ApplyLinearImpulse(new Vector3(force.Force.X, force.Force.Y, 0) * deltaTime);
                    break;
            }

            // Remove after applying
            buffer.Remove<ApplyForce>(entity);
        }

        buffer.Execute();
    }
}

// Usage: Jump
world.Add(player, new ApplyForce
{
    Force = new Vector2(0, 500),
    Mode = ForceMode.Impulse
});
```

### Physics Cleanup on Despawn

```csharp
public class PhysicsCleanupSystem : SystemBase
{
    public override void Initialize()
    {
        // Subscribe to entity despawn events
        World.OnEntityDespawning += HandleEntityDespawn;
    }

    private void HandleEntityDespawn(Entity entity)
    {
        if (!World.TryGet<PhysicsBody>(entity, out var body))
            return;

        var physics = World.GetSingleton<PhysicsWorldWrapper>();

        // Remove physics body
        physics.Simulation.Bodies.Remove(new BodyHandle(body.BodyHandle));

        // Unregister mapping
        // physics.UnregisterBody(entity);
    }
}
```

### Debug Visualization

```csharp
public class PhysicsDebugSystem : SystemBase
{
    public override SystemPhase Phase => SystemPhase.Render;

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<PhysicsBody, Position, PhysicsShape>())
        {
            ref readonly var pos = ref World.Get<Position>(entity);
            ref readonly var shape = ref World.Get<PhysicsShape>(entity);

            var color = World.Has<PhysicsBody>(entity)
                ? (World.Get<PhysicsBody>(entity).Type == BodyType.Static ? Color.Gray : Color.Green)
                : Color.Red;

            switch (shape.Type)
            {
                case ShapeType.Box:
                    DebugDraw.Rectangle(pos.X, pos.Y, shape.Width, shape.Height, color);
                    break;
                case ShapeType.Circle:
                    DebugDraw.Circle(pos.X, pos.Y, shape.Radius, color);
                    break;
            }
        }
    }
}
```

## See Also

- [Physics Sample](../../samples/KeenEyes.Sample.Physics/README.md) - Full BepuPhysics integration
- [Systems Guide](../systems.md) - System phases and ordering
- [Events Guide](../events.md) - Entity lifecycle events
