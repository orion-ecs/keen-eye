# Why ECS?

Entity Component System is an architectural pattern that separates **identity** (entities), **data** (components), and **behavior** (systems). This page explains why KeenEyes chose ECS over alternatives.

## The Problem with Traditional OOP

Traditional game objects combine identity, data, and behavior in one class:

```csharp
public class Enemy : GameObject
{
    public float Health { get; set; }
    public float Speed { get; set; }
    public Vector2 Position { get; set; }

    public void Update()
    {
        // Movement logic
        // AI logic
        // Animation logic
        // All mixed together
    }
}
```

### Problems

1. **The Diamond Problem**

   What if an enemy can be:
   - Rideable (like a mount)
   - Flying
   - Poisonous
   - Undead

   With inheritance:
   ```
   Enemy → FlyingEnemy → FlyingUndeadEnemy → ???
   ```

   You can't inherit from multiple classes. Multiple inheritance (where available) creates ambiguity.

2. **Scattered Logic**

   "Move all entities" requires touching every class with movement. "Render all entities" requires touching every class with sprites. Changes ripple across the entire codebase.

3. **Poor Cache Performance**

   Objects are scattered in heap memory. Iterating 10,000 enemies means 10,000 pointer dereferences to random memory locations.

4. **Hard to Test**

   To test enemy movement, you must instantiate an entire `Enemy` with all its dependencies.

## How ECS Solves These Problems

### Problem 1: Composition Over Inheritance

ECS composes behavior from small, focused components:

```csharp
var flyingUndeadMount = world.Spawn()
    .With(new Position { X = 0, Y = 100 })
    .With(new Health { Current = 200, Max = 200 })
    .WithTag<Flying>()
    .WithTag<Undead>()
    .WithTag<Mountable>()
    .With(new PoisonAura { Damage = 5, Range = 50 })
    .WithTag<Enemy>()
    .Build();
```

No inheritance needed. Any combination of components is valid.

### Problem 2: Centralized Logic

Systems process all entities with matching components:

```csharp
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Process ALL entities with Position and Velocity
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

One system handles movement for players, enemies, projectiles, particles - anything with `Position` and `Velocity`.

### Problem 3: Cache-Friendly Memory

Components of the same type are stored contiguously:

```
Traditional OOP (cache unfriendly):
[Enemy1.Position, Enemy1.Health, Enemy1.AI, ...]
    ↓ (memory jump)
[Enemy2.Position, Enemy2.Health, Enemy2.AI, ...]
    ↓ (memory jump)
[Enemy3.Position, ...]

ECS (cache friendly):
Positions: [E1.Pos, E2.Pos, E3.Pos, E4.Pos, ...]  ← Sequential
Healths:   [E1.HP,  E2.HP,  E3.HP,  E4.HP,  ...]  ← Sequential
```

When iterating positions, the CPU prefetcher loads the next positions automatically. No random memory access.

### Problem 4: Testable Systems

Systems are pure logic operating on data:

```csharp
[Fact]
public void MovementSystem_AppliesVelocityToPosition()
{
    using var world = new World();
    world.AddSystem<MovementSystem>();

    var entity = world.Spawn()
        .With(new Position { X = 0, Y = 0 })
        .With(new Velocity { X = 10, Y = 5 })
        .Build();

    world.Update(1f); // deltaTime = 1 second

    ref readonly var pos = ref world.Get<Position>(entity);
    Assert.Equal(10f, pos.X);
    Assert.Equal(5f, pos.Y);
}
```

No mocks, no stubs, no complex setup. Just data in, data out.

## ECS vs Other Patterns

### ECS vs Component-Based Architecture (Unity-style)

Unity's `MonoBehaviour` uses components but keeps behavior in the component:

```csharp
public class Mover : MonoBehaviour
{
    public float speed;
    void Update()
    {
        transform.position += Vector3.forward * speed * Time.deltaTime;
    }
}
```

**Differences from ECS:**
- Logic lives in component (`Update()` method)
- Components store references to other components
- Iteration happens per-object, not per-system
- No archetype-based storage

**When Unity-style is better:**
- Rapid prototyping
- Small projects
- Team unfamiliar with ECS

**When ECS is better:**
- Performance-critical applications
- Large entity counts (>10,000)
- Complex entity variations
- Parallel processing requirements

### ECS vs Data-Oriented Design (DOD)

DOD is a philosophy; ECS is an implementation of that philosophy:

- **DOD says:** Organize data for how it's processed
- **ECS provides:** A specific way to do that (entities, components, systems)

You can do DOD without ECS:
```csharp
// DOD without ECS
var positions = new Position[1000];
var velocities = new Velocity[1000];

for (int i = 0; i < 1000; i++)
{
    positions[i].X += velocities[i].X;
}
```

ECS adds:
- Entity identity and lifecycle
- Dynamic component addition/removal
- Query filtering
- System scheduling

### ECS vs Actor Model

Actor model (Akka, Orleans) uses isolated actors with message passing:

```csharp
public class EnemyActor : Actor
{
    public void OnDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
            Context.Self.Stop();
    }
}
```

**Differences:**
- Actors encapsulate state and behavior
- Communication via messages (async)
- Location transparency (distributed)
- No bulk iteration

**When Actors are better:**
- Distributed systems
- Naturally event-driven domains
- When isolation is critical

**When ECS is better:**
- Local simulation (games, physics)
- Bulk processing requirements
- Performance-critical hot loops

## Real-World Performance Impact

Benchmarks comparing 100,000 entities:

| Operation | Traditional OOP | ECS |
|-----------|----------------|-----|
| Iterate all positions | 8.5ms | 0.4ms |
| Add component to 1000 entities | 2.1ms | 0.3ms |
| Query entities by 2 components | 12ms | 0.8ms |
| Spawn 10,000 entities | 45ms | 5ms |

*Results vary by hardware and implementation.*

The difference grows with entity count. At 1 million entities, the gap becomes 100x or more.

## When NOT to Use ECS

ECS isn't always the answer:

1. **Simple applications** - Overhead isn't justified for a few hundred entities
2. **Highly sequential logic** - If order matters more than data layout
3. **Rich domain models** - Business apps benefit more from DDD patterns
4. **Team expertise** - OOP is more widely understood

## KeenEyes' ECS Implementation

KeenEyes implements ECS with:

- **Archetype storage** - Entities with same components stored together
- **Zero-copy queries** - `ref` returns for component access
- **Parallel execution** - Systems can run concurrently
- **Source generators** - Boilerplate reduction without reflection

See [Core Concepts](../concepts.md) for details on using these features.

## Further Reading

- [Data-Oriented Design](https://www.dataorienteddesign.com/dodbook/) by Richard Fabian
- [ECS FAQ](https://github.com/SanderMertens/ecs-faq) by Sander Mertens
- [GDC Talk: Overwatch ECS](https://www.youtube.com/watch?v=W3aieHjyNvw) by Tim Ford
- [ADR-001: World Manager Architecture](../adr/001-world-manager-architecture.md) - KeenEyes internals
