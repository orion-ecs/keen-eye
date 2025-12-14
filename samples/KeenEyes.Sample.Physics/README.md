# KeenEyes Physics Sample

This sample demonstrates the **KeenEyes.Physics** plugin, which provides BepuPhysics v2 integration for the KeenEyes ECS framework.

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Component Reference](#component-reference)
  - [RigidBody](#rigidbody)
  - [PhysicsShape](#physicsshape)
  - [PhysicsMaterial](#physicsmaterial)
  - [CollisionFilter](#collisionfilter)
- [Physics World API](#physics-world-api)
  - [Raycasting](#raycasting)
  - [Overlap Queries](#overlap-queries)
  - [Forces and Impulses](#forces-and-impulses)
  - [Velocity Control](#velocity-control)
- [Collision Events](#collision-events)
- [Performance Tuning](#performance-tuning)
- [Troubleshooting](#troubleshooting)

## Installation

Add a reference to `KeenEyes.Physics` in your project:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/KeenEyes.Core.csproj" />
  <ProjectReference Include="path/to/KeenEyes.Physics.csproj" />
</ItemGroup>
```

## Quick Start

```csharp
using KeenEyes;
using KeenEyes.Physics;
using KeenEyes.Physics.Components;
using KeenEyes.Physics.Core;
using System.Numerics;

// 1. Create world and install physics plugin
using var world = new World();
world.InstallPlugin(new PhysicsPlugin());

// 2. Create a static ground plane
var ground = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(RigidBody.Static())
    .With(PhysicsShape.Box(100, 1, 100))
    .Build();

// 3. Create a dynamic ball that falls
var ball = world.Spawn()
    .With(new Transform3D(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One))
    .With(new Velocity3D(0, 0, 0))
    .With(RigidBody.Dynamic(1.0f))
    .With(PhysicsShape.Sphere(0.5f))
    .With(PhysicsMaterial.Rubber)
    .Build();

// 4. Run the simulation
float dt = 1f / 60f;
for (int i = 0; i < 180; i++)  // 3 seconds
{
    world.Update(SystemPhase.FixedUpdate, dt);
}
```

## Component Reference

### RigidBody

Defines the physics behavior of an entity. Three body types are available:

| Type | Factory Method | Description |
|------|----------------|-------------|
| **Dynamic** | `RigidBody.Dynamic(mass)` | Responds to forces, gravity, and collisions. Use for moving objects. |
| **Kinematic** | `RigidBody.Kinematic()` | Moves via code but ignores physics forces. Use for platforms, doors. |
| **Static** | `RigidBody.Static()` | Never moves. Optimized for level geometry. |

```csharp
// Dynamic body with mass
.With(RigidBody.Dynamic(5.0f))

// Kinematic body (scripted movement)
.With(RigidBody.Kinematic())

// Static body (immovable)
.With(RigidBody.Static())

// Dynamic with custom activity settings (sleep behavior)
.With(new RigidBody(mass: 1.0f, activity: new ActivityDescription(
    sleepThreshold: 0.01f,
    minimumTimestepsBeforeSleep: 32)))
```

### PhysicsShape

Defines the collision shape for an entity. Available shapes:

| Shape | Factory Method | Parameters |
|-------|----------------|------------|
| **Sphere** | `PhysicsShape.Sphere(radius)` | `radius`: Sphere radius |
| **Box** | `PhysicsShape.Box(w, h, d)` | Full width, height, depth |
| **Capsule** | `PhysicsShape.Capsule(radius, length)` | Radius and total length |
| **Cylinder** | `PhysicsShape.Cylinder(radius, length)` | Radius and total length |

```csharp
// Sphere with 0.5 unit radius
.With(PhysicsShape.Sphere(0.5f))

// 2x2x2 box (full dimensions, not half-extents)
.With(PhysicsShape.Box(2, 2, 2))

// Capsule for character controllers
.With(PhysicsShape.Capsule(0.5f, 2.0f))

// Cylinder
.With(PhysicsShape.Cylinder(1.0f, 2.0f))
```

### PhysicsMaterial

Controls how surfaces interact during collisions:

| Property | Range | Description |
|----------|-------|-------------|
| **Friction** | 0-1 | Resistance to sliding (0 = ice, 1 = rubber) |
| **Restitution** | 0-1 | Bounciness (0 = absorbs energy, 1 = perfect bounce) |
| **LinearDamping** | 0+ | Air resistance for linear velocity |
| **AngularDamping** | 0+ | Air resistance for rotation |

**Preset Materials:**

```csharp
.With(PhysicsMaterial.Default)  // Moderate friction, low bounce
.With(PhysicsMaterial.Rubber)   // High friction, high bounce
.With(PhysicsMaterial.Ice)      // Very low friction, low bounce
.With(PhysicsMaterial.Metal)    // Moderate friction, moderate bounce
.With(PhysicsMaterial.Wood)     // Moderate friction, low bounce
```

**Custom Material:**

```csharp
.With(new PhysicsMaterial(
    friction: 0.3f,
    restitution: 0.7f,
    linearDamping: 0.05f,
    angularDamping: 0.02f))
```

### CollisionFilter

Controls which entities can collide with each other using layers and masks:

```csharp
// Define collision layers as bit flags
const uint PlayerLayer = 1 << 0;   // 0x00000001
const uint EnemyLayer = 1 << 1;    // 0x00000002
const uint BulletLayer = 1 << 2;   // 0x00000004
const uint WallLayer = 1 << 3;     // 0x00000008

// Player collides with enemies and walls, not bullets
var player = world.Spawn()
    .With(new CollisionFilter(
        layer: PlayerLayer,
        mask: EnemyLayer | WallLayer))
    // ...
    .Build();

// Trigger zone (detects overlap but no physical response)
var pickupZone = world.Spawn()
    .With(CollisionFilter.Trigger(layer: 1, mask: PlayerLayer))
    // ...
    .Build();
```

## Physics World API

Access the physics API through `world.GetExtension<PhysicsWorld>()`:

```csharp
var physics = world.GetExtension<PhysicsWorld>();
```

### Raycasting

Cast rays to detect objects in the physics world:

```csharp
var origin = new Vector3(0, 2, 0);
var direction = Vector3.UnitX;  // +X direction
var maxDistance = 100f;

if (physics.Raycast(origin, direction, maxDistance, out RayHit hit))
{
    Console.WriteLine($"Hit entity: {hit.Entity}");
    Console.WriteLine($"Hit position: {hit.Position}");
    Console.WriteLine($"Surface normal: {hit.Normal}");
    Console.WriteLine($"Distance: {hit.Distance}");
}
```

### Overlap Queries

Find all entities within a shape:

```csharp
// Find all entities within a sphere
var center = new Vector3(0, 5, 0);
var radius = 10f;

foreach (var entity in physics.OverlapSphere(center, radius))
{
    Console.WriteLine($"Found entity: {entity}");
}

// Find all entities within a box
var boxCenter = new Vector3(0, 5, 0);
var halfExtents = new Vector3(5, 5, 5);
var rotation = Quaternion.Identity;

foreach (var entity in physics.OverlapBox(boxCenter, halfExtents, rotation))
{
    Console.WriteLine($"Found entity: {entity}");
}
```

### Forces and Impulses

Apply forces and impulses to dynamic bodies:

```csharp
// Apply continuous force (multiplied by timestep internally)
physics.ApplyForce(entity, new Vector3(0, 100, 0));

// Apply instant impulse (velocity change)
physics.ApplyImpulse(entity, new Vector3(10, 5, 0));

// Apply angular impulse (spin)
physics.ApplyAngularImpulse(entity, new Vector3(0, 5, 0));

// Apply force at a specific point (creates torque)
var worldPoint = new Vector3(1, 2, 0);
physics.ApplyForceAtPosition(entity, new Vector3(0, 0, 10), worldPoint);
```

### Velocity Control

Directly control velocity:

```csharp
// Set linear velocity
physics.SetVelocity(entity, new Vector3(5, 0, 0));

// Set angular velocity
physics.SetAngularVelocity(entity, new Vector3(0, 2, 0));

// Get current velocities
var linearVel = physics.GetVelocity(entity);
var angularVel = physics.GetAngularVelocity(entity);

// Wake up a sleeping body
physics.WakeUp(entity);

// Check if body is awake
bool awake = physics.IsAwake(entity);
```

## Collision Events

Subscribe to collision events for gameplay logic:

```csharp
// Called every frame while objects are touching
var collisionSub = world.Subscribe<CollisionEvent>(collision =>
{
    var entityA = collision.EntityA;
    var entityB = collision.EntityB;
    var contactPoint = collision.ContactPoint;
    var normal = collision.ContactNormal;
    var depth = collision.PenetrationDepth;
    var isTrigger = collision.IsTrigger;

    // Handle collision...
});

// Called once when collision begins
var startSub = world.Subscribe<CollisionStartedEvent>(collision =>
{
    // Play impact sound, spawn particles, etc.
    PlayImpactSound(collision.ContactPoint);
});

// Called once when collision ends
var endSub = world.Subscribe<CollisionEndedEvent>(collision =>
{
    // Stop continuous effects, etc.
});

// Don't forget to dispose subscriptions when done
collisionSub.Dispose();
startSub.Dispose();
endSub.Dispose();
```

## Performance Tuning

Configure physics for your needs:

```csharp
world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
{
    // Physics timestep (lower = more accurate, slower)
    FixedTimestep = 1f / 60f,  // 60 Hz (default)

    // Max steps per frame (prevents spiral of death)
    MaxStepsPerFrame = 3,

    // Gravity vector
    Gravity = new Vector3(0, -9.81f, 0),

    // Solver iterations (higher = more stable stacking)
    VelocityIterations = 8,
    SubstepCount = 1,

    // Interpolation for smooth rendering
    EnableInterpolation = true,

    // Pre-allocate capacity
    InitialBodyCapacity = 1024,
    InitialStaticCapacity = 1024,
    InitialConstraintCapacity = 2048
}));
```

**Tuning Tips:**

| Scenario | Recommendation |
|----------|----------------|
| Stable stacking | Increase `VelocityIterations` (10-15) and `SubstepCount` (2-4) |
| Fast-moving objects | Decrease `FixedTimestep` or use continuous collision detection |
| Large worlds | Increase initial capacities to avoid reallocations |
| Mobile/web | Lower `VelocityIterations` (4-6), increase `FixedTimestep` (1/30) |

## Troubleshooting

### Objects Fall Through Ground

1. Ensure ground has `RigidBody.Static()` and a `PhysicsShape`
2. Check that `Transform3D` positions are correct
3. For fast objects, reduce `FixedTimestep`

### Stacks Are Unstable

1. Increase `VelocityIterations` to 10-15
2. Increase `SubstepCount` to 2-4
3. Use `PhysicsMaterial.Wood` or lower restitution

### Physics Not Running

1. Ensure `PhysicsPlugin` is installed: `world.InstallPlugin(new PhysicsPlugin())`
2. Call `world.Update(SystemPhase.FixedUpdate, dt)` in your game loop
3. Dynamic bodies need both `RigidBody.Dynamic()` AND `PhysicsShape`

### Entity Has No Physics Body Error

Ensure entity has all required components:
- `Transform3D` (position/rotation)
- `RigidBody` (dynamic, kinematic, or static)
- `PhysicsShape` (collision shape)

```csharp
// Complete physics entity setup
var entity = world.Spawn()
    .With(new Transform3D(position, rotation, scale))  // Required
    .With(new Velocity3D(0, 0, 0))                     // Optional
    .With(RigidBody.Dynamic(1.0f))                     // Required
    .With(PhysicsShape.Sphere(0.5f))                   // Required
    .With(PhysicsMaterial.Default)                     // Optional
    .Build();
```

### CollisionEvents Not Firing

1. Check that both entities have valid physics bodies
2. Verify `CollisionFilter` masks allow collision
3. Ensure you're subscribed before simulation starts
4. Triggers (`IsTrigger = true`) still fire events but have no physical response

## Running This Sample

```bash
cd samples/KeenEyes.Sample.Physics
dotnet run
```

The sample runs three demonstrations:
1. **Falling Objects** - Various shapes/materials dropping under gravity
2. **Stacking & Collision** - Box stacking and collision event handling
3. **Raycasting** - Object detection using rays and overlap queries
