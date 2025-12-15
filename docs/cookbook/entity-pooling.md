# Entity Pooling

## Problem

You're spawning and despawning many entities (bullets, particles, enemies) and want to avoid allocation overhead.

## Solution

### Pool Component

```csharp
[TagComponent]
public partial struct Pooled : ITagComponent { }

[Component]
public partial struct PoolMember : IComponent
{
    public int PoolId;
}

[TagComponent]
public partial struct PooledActive : ITagComponent { }
```

### Entity Pool Manager

```csharp
public sealed class EntityPool
{
    private readonly World world;
    private readonly int poolId;
    private readonly Func<EntityBuilder, EntityBuilder> entitySetup;
    private readonly Queue<Entity> available = new();
    private int totalCreated;

    public EntityPool(World world, int poolId, Func<EntityBuilder, EntityBuilder> setup)
    {
        this.world = world;
        this.poolId = poolId;
        this.entitySetup = setup;
    }

    public Entity Get()
    {
        Entity entity;

        if (available.Count > 0)
        {
            // Reuse pooled entity
            entity = available.Dequeue();
            world.Add<PooledActive>(entity);
        }
        else
        {
            // Create new entity
            entity = entitySetup(world.Spawn())
                .WithTag<Pooled>()
                .WithTag<PooledActive>()
                .With(new PoolMember { PoolId = poolId })
                .Build();

            totalCreated++;
        }

        return entity;
    }

    public void Return(Entity entity)
    {
        if (!world.IsAlive(entity))
            return;

        if (!world.Has<Pooled>(entity))
            return;  // Not a pooled entity

        // Deactivate
        world.Remove<PooledActive>(entity);

        // Reset to default state (optional: add Reset component for systems to handle)
        available.Enqueue(entity);
    }

    public void Prewarm(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var entity = entitySetup(world.Spawn())
                .WithTag<Pooled>()
                .With(new PoolMember { PoolId = poolId })
                .Build();

            available.Enqueue(entity);
            totalCreated++;
        }
    }

    public int AvailableCount => available.Count;
    public int TotalCount => totalCreated;
}
```

### Pool Manager Singleton

```csharp
public sealed class PoolManager
{
    private readonly World world;
    private readonly Dictionary<int, EntityPool> pools = new();
    private int nextPoolId;

    public PoolManager(World world)
    {
        this.world = world;
    }

    public EntityPool CreatePool(Func<EntityBuilder, EntityBuilder> setup)
    {
        var pool = new EntityPool(world, nextPoolId++, setup);
        pools[pool.GetHashCode()] = pool;
        return pool;
    }

    public void ReturnAll()
    {
        foreach (var entity in world.Query<PoolMember>().With<PooledActive>())
        {
            ref readonly var member = ref world.Get<PoolMember>(entity);
            if (pools.TryGetValue(member.PoolId, out var pool))
            {
                pool.Return(entity);
            }
        }
    }
}
```

### Usage: Bullet Pool

```csharp
public class WeaponSystem : SystemBase
{
    private EntityPool bulletPool = null!;

    public override void Initialize()
    {
        var poolManager = World.GetSingleton<PoolManager>();

        bulletPool = poolManager.CreatePool(builder => builder
            .With(new Position())
            .With(new Velocity())
            .With(new Lifetime { Remaining = 5f })
            .WithTag<Bullet>());

        // Pre-create 100 bullets
        bulletPool.Prewarm(100);
    }

    public void Fire(Position origin, Velocity direction)
    {
        var bullet = bulletPool.Get();

        // Configure the pooled entity
        ref var pos = ref World.Get<Position>(bullet);
        pos = origin;

        ref var vel = ref World.Get<Velocity>(bullet);
        vel = direction;

        ref var lifetime = ref World.Get<Lifetime>(bullet);
        lifetime.Remaining = 5f;
    }
}
```

### Lifetime System (Returns to Pool)

```csharp
public class LifetimeSystem : SystemBase
{
    private PoolManager poolManager = null!;

    public override void Initialize()
    {
        poolManager = World.GetSingleton<PoolManager>();
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Lifetime>().With<PooledActive>())
        {
            ref var lifetime = ref World.Get<Lifetime>(entity);
            lifetime.Remaining -= deltaTime;

            if (lifetime.Remaining <= 0)
            {
                ref readonly var member = ref World.Get<PoolMember>(entity);
                // Return to pool instead of despawning
                ReturnToPool(entity);
            }
        }
    }

    private void ReturnToPool(Entity entity)
    {
        World.Remove<PooledActive>(entity);
        // The PoolManager will re-add to available queue
    }
}
```

## Why This Works

### Avoiding Archetype Churn

Without pooling:
1. `Spawn()` - Allocates entity, adds to archetype
2. `Despawn()` - Removes from archetype, frees entity ID

With pooling:
1. `Get()` - Adds `PooledActive` tag (minimal archetype change)
2. `Return()` - Removes `PooledActive` tag

The entity stays in similar archetypes, reducing memory shuffling.

### Tag-Based Activation

Using `PooledActive` tag means:
- Active entities: `Query<Position, Velocity>().With<PooledActive>()`
- Inactive entities: `Query<Pooled>().Without<PooledActive>()`
- All pooled: `Query<Pooled>()`

Systems naturally skip inactive entities with proper query filters.

### Prewarm for Consistent Performance

Calling `Prewarm(100)` during initialization:
- Creates all entities upfront
- Avoids allocation spikes during gameplay
- Makes frame times more consistent

### Component Reset

Pooled entities keep their components but values may be stale. Options:
1. Reset in `Get()` method (shown above)
2. Use a `Reset` component that systems process
3. Overwrite all values when activating

## Variations

### Auto-Return on Collision

```csharp
public class BulletCollisionSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var bullet in World.Query<Bullet, Position>().With<PooledActive>())
        {
            ref readonly var pos = ref World.Get<Position>(bullet);

            // Check collision
            if (Physics.CheckHit(pos, out var hit))
            {
                // Deal damage
                buffer.Add(hit.Entity, new DamageReceived { Amount = 10 });

                // Return to pool
                buffer.Remove<PooledActive>(bullet);
            }
        }

        buffer.Execute();
    }
}
```

### Multiple Pool Types

```csharp
public static class Pools
{
    public static EntityPool Bullets { get; private set; } = null!;
    public static EntityPool Particles { get; private set; } = null!;
    public static EntityPool Enemies { get; private set; } = null!;

    public static void Initialize(PoolManager manager)
    {
        Bullets = manager.CreatePool(b => b
            .With(new Position())
            .With(new Velocity())
            .WithTag<Bullet>());

        Particles = manager.CreatePool(b => b
            .With(new Position())
            .With(new ParticleData())
            .With(new Lifetime()));

        Enemies = manager.CreatePool(b => b
            .With(new Position())
            .With(new Health { Current = 100, Max = 100 })
            .With(new AIState())
            .WithTag<Enemy>());

        Bullets.Prewarm(200);
        Particles.Prewarm(1000);
        Enemies.Prewarm(50);
    }
}
```

### Dynamic Pool Growth

```csharp
public sealed class GrowingEntityPool
{
    private readonly int growthIncrement;

    public GrowingEntityPool(World world, int poolId,
        Func<EntityBuilder, EntityBuilder> setup,
        int initialSize = 100,
        int growthIncrement = 50)
    {
        this.growthIncrement = growthIncrement;
        Prewarm(initialSize);
    }

    public Entity Get()
    {
        if (available.Count == 0)
        {
            // Auto-grow when depleted
            Prewarm(growthIncrement);
        }

        return GetFromQueue();
    }
}
```

### Pool Statistics

```csharp
public struct PoolStats
{
    public int TotalCreated;
    public int Available;
    public int Active;
    public int PeakActive;
}

public sealed class EntityPool
{
    private int peakActive;

    public Entity Get()
    {
        // ... existing code ...

        int active = totalCreated - available.Count;
        if (active > peakActive)
            peakActive = active;

        return entity;
    }

    public PoolStats GetStats() => new PoolStats
    {
        TotalCreated = totalCreated,
        Available = available.Count,
        Active = totalCreated - available.Count,
        PeakActive = peakActive
    };
}

// Debug system to display pool stats
public class PoolDebugSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var stats = Pools.Bullets.GetStats();
        DebugUI.Text($"Bullets: {stats.Active}/{stats.TotalCreated} (peak: {stats.PeakActive})");
    }
}
```

### Component-Based Pooling

Alternative approach using only components (no external manager):

```csharp
[Component]
public partial struct PooledEntity : IComponent
{
    public int PoolType;
    public bool IsActive;
}

public class ComponentPoolSystem : SystemBase
{
    public Entity GetFromPool(int poolType)
    {
        // Find inactive entity of matching type
        foreach (var entity in World.Query<PooledEntity>())
        {
            ref var pooled = ref World.Get<PooledEntity>(entity);
            if (pooled.PoolType == poolType && !pooled.IsActive)
            {
                pooled.IsActive = true;
                return entity;
            }
        }

        // No available entity - create new one
        return CreateNewPooledEntity(poolType);
    }

    public void ReturnToPool(Entity entity)
    {
        ref var pooled = ref World.Get<PooledEntity>(entity);
        pooled.IsActive = false;
        // Reset other components as needed
    }
}
```

## Performance Considerations

| Approach | Spawn Cost | Memory | Query Overhead |
|----------|-----------|--------|----------------|
| No pooling | High (allocation) | Optimal | None |
| Tag-based pooling | Low (tag add) | Higher (inactive entities) | Minimal |
| Disable components | Low | Higher | Systems must check |

Pooling trades memory for consistent frame times. Best for:
- High spawn/despawn rate (bullets, particles)
- Performance-critical scenarios
- Avoiding GC pressure

## See Also

- [Entity Spawning](entity-spawning.md) - Standard spawning patterns
- [Batch Operations](batch-operations.md) - Bulk entity operations
- [Spatial Queries](spatial-queries.md) - Efficient collision detection
