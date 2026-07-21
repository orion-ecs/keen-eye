# Physics

The `KeenEyes.Physics` library integrates [BepuPhysics v2](https://github.com/bepu/bepuphysics2) into the ECS, giving entities rigid-body dynamics, collision detection, and constraint solving. It is installed as a plugin so a `World` only pays for physics when it opts in.

## Overview

`KeenEyes.Physics` adds:

- **Rigid bodies** — `RigidBody` marks an entity as dynamic, kinematic, or static.
- **Collision shapes** — `PhysicsShape` describes spheres, boxes, capsules, and cylinders.
- **Materials and filtering** — `PhysicsMaterial` (friction/restitution/damping) and `CollisionFilter` (layer/mask + trigger support).
- **A stepped simulation** — `PhysicsStepSystem` advances BepuPhysics on a fixed timestep and publishes collision events; `PhysicsSyncSystem` optionally interpolates transforms for smooth rendering.
- **A query/control API** — the `PhysicsWorld` extension exposes raycasts, overlap queries, forces/impulses, and velocity control.

Bodies read and write the same `Transform3D`, `Velocity3D`, and `AngularVelocity3D` components used elsewhere in the engine (from `KeenEyes.Common`), so physics composes naturally with rendering, animation, and gameplay systems that already consume those components.

## Quick Start

### Installation

```csharp
using KeenEyes;
using KeenEyes.Physics;

using var world = new World();

// Install with default configuration (60 Hz, Earth gravity)
world.InstallPlugin(new PhysicsPlugin());
```

Installing `PhysicsPlugin` registers the `RigidBody`, `PhysicsShape`, `PhysicsMaterial`, and `CollisionFilter` components, creates a `PhysicsWorld` extension, and adds:

- `PhysicsStepSystem` in `SystemPhase.FixedUpdate` — advances the simulation.
- `PhysicsSyncSystem` in `SystemPhase.LateUpdate` — only added when `PhysicsConfig.EnableInterpolation` is true.

To customize the simulation, pass a `PhysicsConfig`:

```csharp
using System.Numerics;
using KeenEyes.Physics.Core;

world.InstallPlugin(new PhysicsPlugin(new PhysicsConfig
{
    FixedTimestep = 1f / 60f,
    Gravity = new Vector3(0, -9.81f, 0),
    VelocityIterations = 10,
    SubstepCount = 2,
}));
```

`PhysicsPlugin`'s constructor validates the supplied `PhysicsConfig` via `PhysicsConfig.Validate()` and throws `ArgumentException` if, for example, `FixedTimestep` is non-positive or exceeds 0.1 seconds.

### Your First Rigid Body

```csharp
using KeenEyes.Common;
using KeenEyes.Physics.Components;

// A static ground plane - never moves
var ground = world.Spawn()
    .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
    .With(RigidBody.Static())
    .With(PhysicsShape.Box(100, 1, 100))
    .Build();

// A dynamic ball that falls and bounces
var ball = world.Spawn()
    .With(new Transform3D(new Vector3(0, 10, 0), Quaternion.Identity, Vector3.One))
    .With(new Velocity3D(0, 0, 0))
    .With(RigidBody.Dynamic(mass: 1.0f))
    .With(PhysicsShape.Sphere(radius: 0.5f))
    .With(PhysicsMaterial.Rubber)
    .Build();
```

`PhysicsPlugin` listens for `RigidBody` being added to an entity and creates the corresponding BepuPhysics body as soon as the entity also has `Transform3D` and `PhysicsShape`. Removing the `RigidBody` component, or destroying the entity, removes the body from the simulation.

### Stepping the Simulation

Physics runs in `SystemPhase.FixedUpdate`. Advance it by calling `World.FixedUpdate` on a fixed-timestep loop, or `World.Update` to run every phase (including `FixedUpdate`) for the frame:

```csharp
float dt = 1f / 60f;
float accumulator = 0f;

// Inside your game loop:
accumulator += frameTime;
while (accumulator >= dt)
{
    world.FixedUpdate(dt);
    accumulator -= dt;
}
```

`PhysicsStepSystem.Update` delegates to `PhysicsWorld.Step`, which itself accumulates elapsed time and runs as many fixed-size BepuPhysics timesteps as needed (bounded by `PhysicsConfig.MaxStepsPerFrame`) to catch up.

## Core Concepts

### RigidBody

`RigidBody` is the component that opts an entity into simulation. It carries a `BodyType` (`RigidBodyType.Dynamic`, `Kinematic`, or `Static`), a `Mass` (used only for dynamic bodies), and an `Activity` description that controls sleeping:

```csharp
public struct RigidBody : IComponent
{
    public float Mass;
    public RigidBodyType BodyType;
    public ActivityDescription Activity;

    public static RigidBody Dynamic(float mass) => /* ... */;
    public static RigidBody Kinematic() => /* ... */;
    public static RigidBody Static() => /* ... */;
}
```

- **Dynamic** bodies respond to gravity, forces, and collisions.
- **Kinematic** bodies are moved directly (via `Transform3D`/`Velocity3D`) and can push dynamic bodies but are never pushed themselves.
- **Static** bodies never move and are the cheapest to simulate — use them for level geometry.

`ActivityDescription` controls when a dynamic body is allowed to sleep: `SleepThreshold` is the velocity magnitude below which a body becomes a sleep candidate, and `MinimumTimestepsBeforeSleep` is how many consecutive sub-threshold steps are required before it actually sleeps. Use `ActivityDescription.NeverSleep` for bodies that must always stay simulated.

### PhysicsShape

`PhysicsShape` describes the collision geometry. Factory methods keep the underlying `Size` encoding (which varies per shape type) out of user code:

```csharp
var sphere = PhysicsShape.Sphere(radius: 0.5f);
var box = PhysicsShape.Box(width: 2, height: 2, depth: 2);
var capsule = PhysicsShape.Capsule(radius: 0.5f, length: 2f);
var cylinder = PhysicsShape.Cylinder(radius: 0.5f, length: 2f);
```

### PhysicsMaterial

`PhysicsMaterial` is an optional component describing collision response — `Friction`, `Restitution` (bounciness), `LinearDamping`, and `AngularDamping`. When two colliding bodies both have a material, friction is averaged and the higher restitution wins. If a body has no `PhysicsMaterial`, `PhysicsMaterial.Default` is used. Presets are provided for common cases: `PhysicsMaterial.Rubber`, `.Ice`, `.Metal`, and `.Wood`.

### CollisionFilter

`CollisionFilter` controls *which* bodies can collide, using a layer/mask bitmask, and whether a body is a trigger:

```csharp
const uint PlayerLayer = 1 << 0;
const uint EnemyLayer = 1 << 1;

var player = world.Spawn()
    .With(new CollisionFilter { Layer = PlayerLayer, Mask = EnemyLayer })
    // ... rigid body / shape / transform
    .Build();
```

Two bodies collide only if each one's `Layer` appears in the other's `Mask` — `CollisionFilter.CanCollideWith` implements this bidirectional check. Setting `IsTrigger` to `true` (or using `CollisionFilter.Trigger(...)`) makes a body detect overlaps and still raise collision events without generating a physical response — useful for pickups, damage zones, and checkpoints. Entities without a `CollisionFilter` behave as `CollisionFilter.Default` (layer 1, collides with everything).

### PhysicsConfig

`PhysicsConfig` is passed to `PhysicsPlugin`'s constructor and controls the simulation as a whole:

| Property | Purpose |
|----------|---------|
| `FixedTimestep` | Physics tick rate in seconds (default `1/60`). |
| `MaxStepsPerFrame` | Caps how many fixed steps can run in one frame call (default 3), to avoid a spiral of death. |
| `Gravity` | Gravity vector applied to dynamic bodies (default `(0, -9.81, 0)`). |
| `VelocityIterations` | Constraint solver velocity iterations (default 8). |
| `SubstepCount` | Position-correction substeps (default 1). |
| `EnableInterpolation` | Whether `PhysicsSyncSystem` is registered to interpolate rendering transforms (default `true`). |
| `InitialBodyCapacity`, `InitialStaticCapacity`, `InitialConstraintCapacity` | Pre-sizing hints to avoid reallocations. |

### The PhysicsWorld Extension

Physics operations that don't fit a component/system model — raycasts, overlap queries, and applying forces — live on the `PhysicsWorld` extension, retrieved with `World.GetExtension<PhysicsWorld>()`:

```csharp
var physics = world.GetExtension<PhysicsWorld>();

// Raycast
if (physics.Raycast(origin, direction, maxDistance: 50f, out var hit))
{
    // hit.Entity, hit.Position, hit.Normal, hit.Distance
}

// Overlap queries
foreach (var entity in physics.OverlapSphere(center, radius: 5f))
{
    // ...
}

// Forces and impulses
physics.ApplyForce(ball, new Vector3(0, 100, 0));
physics.ApplyImpulse(ball, new Vector3(5, 0, 0));
physics.SetVelocity(ball, Vector3.Zero);
```

`PhysicsWorld` also exposes `Gravity`, `BodyCount`/`StaticCount`, `WakeUp`/`IsAwake`, `SetGravity`, and `SetSolverIterations` for runtime tuning.

### Collision Events

Collisions are published through the ECS messaging system rather than callbacks, so any system can subscribe:

```csharp
using KeenEyes.Physics.Events;

var subscription = world.Subscribe<CollisionStartedEvent>(collision =>
{
    // collision.EntityA, collision.EntityB, collision.ContactNormal,
    // collision.ContactPoint, collision.PenetrationDepth, collision.IsTrigger
});

world.Subscribe<CollisionEvent>(collision => { /* fires every step while touching */ });
world.Subscribe<CollisionEndedEvent>(collision => { /* fires once separation occurs */ });
```

- `CollisionStartedEvent` fires once when two bodies first make contact.
- `CollisionEvent` fires every physics step for each active collision pair (including the first).
- `CollisionEndedEvent` fires once when a previously-colliding pair separates.

Events are published at the end of each fixed step, after `PhysicsStepSystem` has run the BepuPhysics timestep and synced results back to `Transform3D`/`Velocity3D`.

### Interpolation for Rendering

When `PhysicsConfig.EnableInterpolation` is `true` (the default), `PhysicsSyncSystem` runs in `SystemPhase.LateUpdate` and blends each dynamic body's `Transform3D` between its previous and current physics state using `PhysicsWorld.InterpolationAlpha`. This smooths visuals when the render rate differs from the fixed physics rate, without affecting the physics state itself (gameplay code reading `Transform3D` during `FixedUpdate` still sees exact simulation results).

## Performance Considerations

- **Prefer static bodies for non-moving geometry.** Static bodies skip integration and are cheaper to store and query than sleeping dynamic bodies.
- **Tune `VelocityIterations`/`SubstepCount` deliberately.** Higher values improve stacking stability and penetration recovery at a direct CPU cost; the sample project raises both for stable box-stacking scenes.
- **Size capacities up front.** `InitialBodyCapacity`, `InitialStaticCapacity`, and `InitialConstraintCapacity` avoid reallocation churn when you know roughly how many bodies a world will hold.
- **Let bodies sleep.** The default `ActivityDescription` allows dynamic bodies to sleep once their velocity drops below `SleepThreshold` for `MinimumTimestepsBeforeSleep` steps; only override this with `ActivityDescription.NeverSleep` for bodies that truly need to stay active (e.g., player-controlled bodies awaiting input).
- **Cap runaway catch-up.** `MaxStepsPerFrame` bounds how many fixed steps `PhysicsWorld.Step` will run in a single call, trading simulation accuracy for frame stability if the game falls behind.

## Next Steps

- [Physics Integration Cookbook](cookbook/physics-integration.md) - Worked integration patterns
- [Common Components](common.md) - `Transform3D`, `Velocity3D`, and other shared components physics reads/writes
- [Systems Guide](systems.md) - How `SystemPhase.FixedUpdate` and `SystemPhase.LateUpdate` fit into the frame
- [Messaging](messaging.md) - Subscribing to `CollisionEvent` and other world messages
- [Plugins Guide](plugins.md) - How `IWorldPlugin` installation and extensions work
