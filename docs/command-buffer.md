# Command Buffer Guide

The `CommandBuffer` enables safe entity modification during iteration. This guide covers when and how to use it.

## The Problem

Modifying entities during iteration can invalidate iterators:

```csharp
// ❌ DANGEROUS: Don't do this!
foreach (var entity in world.Query<Health>())
{
    ref var health = ref world.Get<Health>(entity);
    if (health.Current <= 0)
    {
        world.Despawn(entity);  // Invalidates the iterator!
    }
}
```

When you despawn an entity or change its components, the underlying storage may be reorganized, breaking active iterators.

## The Solution: CommandBuffer

Queue operations for deferred execution:

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Health>())
{
    ref var health = ref world.Get<Health>(entity);
    if (health.Current <= 0)
    {
        buffer.Despawn(entity);  // Safe: queued for later
    }
}

// Execute all queued commands after iteration
buffer.Flush(world);
```

## CommandBuffer Operations

### Spawning Entities

```csharp
var buffer = new CommandBuffer();

// Queue entity creation
var cmd = buffer.Spawn()
    .With(new Position { X = 0, Y = 0 })
    .With(new Velocity { X = 1, Y = 0 });

// Named entities
var namedCmd = buffer.Spawn("Bullet")
    .With(new Position { X = x, Y = y });

// Execute and get real entities
var entityMap = buffer.Flush(world);

// Access created entity using placeholder ID
Entity createdEntity = entityMap[cmd.PlaceholderId];
```

### Despawning Entities

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Health>())
{
    ref readonly var health = ref world.Get<Health>(entity);
    if (health.Current <= 0)
    {
        buffer.Despawn(entity);
    }
}

buffer.Flush(world);
```

### Adding Components

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Position>().With<Enemy>())
{
    ref readonly var pos = ref world.Get<Position>(entity);
    if (IsInPlayerRange(pos))
    {
        // Mark enemy as alerted
        buffer.AddComponent(entity, new Alerted { Time = 0f });
    }
}

buffer.Flush(world);
```

### Removing Components

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Poisoned>())
{
    ref var poison = ref world.Get<Poisoned>(entity);
    poison.Duration -= deltaTime;

    if (poison.Duration <= 0)
    {
        buffer.RemoveComponent<Poisoned>(entity);
    }
}

buffer.Flush(world);
```

### Setting Components

```csharp
var buffer = new CommandBuffer();

foreach (var entity in world.Query<Position>())
{
    // Queue a position reset
    buffer.SetComponent(entity, new Position { X = 0, Y = 0 });
}

buffer.Flush(world);
```

## Placeholder Entities

When spawning, you can reference new entities before they exist:

```csharp
var buffer = new CommandBuffer();

// Create parent
var parentCmd = buffer.Spawn()
    .With(new Position { X = 0, Y = 0 });

// Create child referencing parent (using placeholder)
buffer.Spawn()
    .With(new Position { X = 10, Y = 0 })
    .With(new Parent { Entity = new Entity(parentCmd.PlaceholderId, 0) });

// Add component to placeholder entity
buffer.AddComponent(parentCmd.PlaceholderId, new Named { Name = "Parent" });

// Execute
var entityMap = buffer.Flush(world);
Entity parent = entityMap[parentCmd.PlaceholderId];
```

## Common Patterns

### Death System

```csharp
public class DeathSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref readonly var health = ref World.Get<Health>(entity);
            if (health.Current <= 0)
            {
                buffer.Despawn(entity);
            }
        }

        buffer.Flush(World);
    }
}
```

### Spawner System

```csharp
public class SpawnerSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Spawner>())
        {
            ref var spawner = ref World.Get<Spawner>(entity);
            spawner.Timer -= deltaTime;

            if (spawner.Timer <= 0)
            {
                ref readonly var pos = ref World.Get<Position>(entity);

                buffer.Spawn()
                    .With(new Position { X = pos.X, Y = pos.Y })
                    .With(new Velocity { X = 0, Y = -10 })
                    .With(spawner.Template);

                spawner.Timer = spawner.Interval;
            }
        }

        buffer.Flush(World);
    }
}
```

### Collision Response

```csharp
public class CollisionSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        var projectiles = World.Query<Position, Projectile>().ToList();
        var enemies = World.Query<Position, Health>().With<Enemy>().ToList();

        foreach (var projectile in projectiles)
        {
            ref readonly var projPos = ref World.Get<Position>(projectile);

            foreach (var enemy in enemies)
            {
                ref readonly var enemyPos = ref World.Get<Position>(enemy);

                if (CheckCollision(projPos, enemyPos))
                {
                    // Destroy projectile
                    buffer.Despawn(projectile);

                    // Apply damage
                    ref var health = ref World.Get<Health>(enemy);
                    health.Current -= 10;

                    break;  // Projectile can only hit once
                }
            }
        }

        buffer.Flush(World);
    }
}
```

### State Transitions

```csharp
public class StateTransitionSystem : SystemBase
{
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        // Transition from Idle to Moving
        foreach (var entity in World.Query<Velocity>().With<IdleState>())
        {
            ref readonly var vel = ref World.Get<Velocity>(entity);
            if (vel.X != 0 || vel.Y != 0)
            {
                buffer.RemoveComponent<IdleState>(entity);
                buffer.AddComponent(entity, default(MovingState));
            }
        }

        // Transition from Moving to Idle
        foreach (var entity in World.Query<Velocity>().With<MovingState>())
        {
            ref readonly var vel = ref World.Get<Velocity>(entity);
            if (vel.X == 0 && vel.Y == 0)
            {
                buffer.RemoveComponent<MovingState>(entity);
                buffer.AddComponent(entity, default(IdleState));
            }
        }

        buffer.Flush(World);
    }
}
```

## Best Practices

### Reuse CommandBuffer

```csharp
public class MySystem : SystemBase
{
    // ✅ Good: Reuse buffer instance
    private readonly CommandBuffer buffer = new();

    public override void Update(float deltaTime)
    {
        // Use buffer...
        buffer.Flush(World);
    }
}
```

### Flush Once Per Update

```csharp
public override void Update(float deltaTime)
{
    // ✅ Good: Single flush at end
    foreach (var entity in World.Query<A>())
    {
        buffer.Despawn(entity);
    }
    foreach (var entity in World.Query<B>())
    {
        buffer.AddComponent(entity, new C());
    }
    buffer.Flush(World);  // One flush for all operations

    // ❌ Bad: Multiple flushes
    foreach (var entity in World.Query<A>())
    {
        buffer.Despawn(entity);
        buffer.Flush(World);  // Inefficient!
    }
}
```

### Clear on Error

If you need to abort operations:

```csharp
try
{
    // Queue operations...
    buffer.Flush(World);
}
catch
{
    buffer.Clear();  // Discard queued commands
    throw;
}
```

## When to Use CommandBuffer

| Scenario | Use CommandBuffer? |
|----------|-------------------|
| Despawning entities during query | ✅ Yes |
| Spawning entities during query | ✅ Yes |
| Adding/removing components during query | ✅ Yes |
| Modifying component values | ❌ No (use `ref` directly) |
| Reading component values | ❌ No (use `ref readonly`) |
| After iteration is complete | ❌ No (direct operations are fine) |

## Thread Safety

`CommandBuffer` is **not thread-safe**. Each system should use its own buffer, or access must be synchronized externally.

## Next Steps

- [Systems Guide](systems.md) - Using buffers in systems
- [Entities Guide](entities.md) - Entity lifecycle
- [Queries Guide](queries.md) - Query patterns
