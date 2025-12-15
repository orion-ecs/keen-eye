# Batch Operations

## Problem

You need to efficiently create, modify, or destroy many entities at once without paying per-entity overhead.

## Solution

### Batch Spawning

```csharp
public void SpawnEnemyWave(int count, float startX, float spacing)
{
    var buffer = World.GetCommandBuffer();

    for (int i = 0; i < count; i++)
    {
        buffer.Spawn()
            .With(new Position { X = startX + (i * spacing), Y = 0 })
            .With(new Health { Current = 100, Max = 100 })
            .With(new Velocity { X = 0, Y = 50 })
            .WithTag<Enemy>();
    }

    // Single batch operation
    buffer.Execute();
}
```

### Batch Component Addition

```csharp
public void ApplyBuffToAll<T>() where T : struct, ITagComponent
{
    var buffer = World.GetCommandBuffer();

    foreach (var entity in World.Query<Health>().With<T>())
    {
        buffer.Add(entity, new SpeedBuff { Multiplier = 1.5f, RemainingDuration = 10f });
        buffer.Add(entity, new DamageBoost { Multiplier = 1.2f, RemainingDuration = 10f });
    }

    buffer.Execute();
}
```

### Batch Destruction

```csharp
public void DespawnAllEnemies()
{
    var buffer = World.GetCommandBuffer();

    foreach (var entity in World.Query<Enemy>())
    {
        buffer.Despawn(entity);
    }

    buffer.Execute();
}

// Or with filtering
public void DespawnDeadEnemies()
{
    var buffer = World.GetCommandBuffer();

    foreach (var entity in World.Query<Enemy, Dead>())
    {
        buffer.Despawn(entity);
    }

    buffer.Execute();
}
```

### Batch Component Removal

```csharp
public void ClearAllDebuffs()
{
    var buffer = World.GetCommandBuffer();

    // Remove poison from all entities
    foreach (var entity in World.Query<PoisonDebuff>())
    {
        buffer.Remove<PoisonDebuff>(entity);
    }

    // Remove slow from all entities
    foreach (var entity in World.Query<SlowDebuff>())
    {
        buffer.Remove<SlowDebuff>(entity);
    }

    buffer.Execute();
}
```

### Bulk Data Transformation

```csharp
public void ResetAllHealthToMax()
{
    // Direct modification (no command buffer needed for existing components)
    foreach (var entity in World.Query<Health>())
    {
        ref var health = ref World.Get<Health>(entity);
        health.Current = health.Max;
    }
}

public void DamageAllInRadius(Position center, float radius, int damage)
{
    var buffer = World.GetCommandBuffer();

    foreach (var entity in World.Query<Position, Health>())
    {
        ref readonly var pos = ref World.Get<Position>(entity);

        float distSq = (pos.X - center.X) * (pos.X - center.X) +
                       (pos.Y - center.Y) * (pos.Y - center.Y);

        if (distSq <= radius * radius)
        {
            buffer.Add(entity, new DamageReceived { Amount = damage });
        }
    }

    buffer.Execute();
}
```

### Batch from Array/Collection

```csharp
public void SpawnFromSpawnData(SpawnData[] spawns)
{
    var buffer = World.GetCommandBuffer();

    foreach (var spawn in spawns)
    {
        var prefab = World.GetPrefab(spawn.PrefabName);
        buffer.SpawnPrefab(prefab)
            .With(new Position { X = spawn.X, Y = spawn.Y })
            .With(new Rotation { Angle = spawn.Rotation });
    }

    buffer.Execute();
}

public record struct SpawnData(string PrefabName, float X, float Y, float Rotation);
```

## Why This Works

### Deferred Execution

Command buffers accumulate operations and execute them together:
1. **No archetype churn** during recording
2. **Batch archetype moves** on execution
3. **Better cache utilization** from sequential processing

### Query Stability

Iterating while modifying is dangerous:
```csharp
// DANGEROUS: Query invalidated by Despawn
foreach (var entity in World.Query<Enemy>())
{
    World.Despawn(entity);  // Modifies underlying storage!
}
```

Command buffers defer modifications:
```csharp
// SAFE: Modifications happen after iteration
foreach (var entity in World.Query<Enemy>())
{
    buffer.Despawn(entity);  // Just records intent
}
buffer.Execute();  // All modifications happen here
```

### Memory Locality

When spawning 1000 entities with same components:
- All entities go to same archetype
- Components stored contiguously
- Better cache performance in subsequent queries

## Variations

### Parallel Batch Processing

```csharp
public void ParallelDamageCalculation()
{
    var entities = World.Query<Position, Health>().ToArray();

    // Calculate damage in parallel
    var damages = new int[entities.Length];

    Parallel.For(0, entities.Length, i =>
    {
        ref readonly var pos = ref World.Get<Position>(entities[i]);
        damages[i] = CalculateEnvironmentalDamage(pos);
    });

    // Apply damage sequentially (world modification not thread-safe)
    var buffer = World.GetCommandBuffer();
    for (int i = 0; i < entities.Length; i++)
    {
        if (damages[i] > 0)
        {
            buffer.Add(entities[i], new DamageReceived { Amount = damages[i] });
        }
    }
    buffer.Execute();
}
```

### Chunked Spawning (Spread Over Frames)

```csharp
public class ChunkedSpawnerSystem : SystemBase
{
    private Queue<SpawnRequest> pendingSpawns = new();
    private const int SpawnsPerFrame = 50;

    public void QueueSpawns(IEnumerable<SpawnRequest> requests)
    {
        foreach (var request in requests)
        {
            pendingSpawns.Enqueue(request);
        }
    }

    public override void Update(float deltaTime)
    {
        if (pendingSpawns.Count == 0)
            return;

        var buffer = World.GetCommandBuffer();
        int spawned = 0;

        while (spawned < SpawnsPerFrame && pendingSpawns.Count > 0)
        {
            var request = pendingSpawns.Dequeue();
            buffer.Spawn()
                .With(new Position { X = request.X, Y = request.Y })
                .With(request.Components);
            spawned++;
        }

        buffer.Execute();
    }
}
```

### Conditional Batch Operations

```csharp
public void ConditionalBatchUpdate()
{
    var buffer = World.GetCommandBuffer();

    foreach (var entity in World.Query<Health, Position>())
    {
        ref readonly var health = ref World.Get<Health>(entity);
        ref readonly var pos = ref World.Get<Position>(entity);

        // Different modifications based on conditions
        if (health.Current <= 0 && !World.Has<Dead>(entity))
        {
            buffer.Add<Dead>(entity);
            buffer.Add(entity, new DeathAnimation { Timer = 2f });
        }
        else if (IsInSafeZone(pos))
        {
            buffer.Add(entity, new HealReceived { Amount = 1 });
        }
        else if (IsInDamageZone(pos))
        {
            buffer.Add(entity, new DamageReceived { Amount = 5 });
        }
    }

    buffer.Execute();
}
```

### Template-Based Batch Creation

```csharp
public class BatchFactory
{
    private readonly World world;

    public BatchFactory(World world) => this.world = world;

    public void CreateGrid<T>(
        int rows,
        int cols,
        float spacing,
        Func<int, int, T> componentFactory) where T : struct, IComponent
    {
        var buffer = world.GetCommandBuffer();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                var component = componentFactory(row, col);
                buffer.Spawn()
                    .With(new Position { X = col * spacing, Y = row * spacing })
                    .With(component);
            }
        }

        buffer.Execute();
    }
}

// Usage
var factory = new BatchFactory(world);
factory.CreateGrid<TileData>(100, 100, 32f, (row, col) => new TileData
{
    Type = (row + col) % 2 == 0 ? TileType.Grass : TileType.Stone
});
```

### Batch Copy/Clone

```csharp
public void CloneEntities(IEnumerable<Entity> sources, Vector2 offset)
{
    var buffer = World.GetCommandBuffer();

    foreach (var source in sources)
    {
        var builder = buffer.Spawn();

        // Copy Position with offset
        if (World.TryGet<Position>(source, out var pos))
        {
            builder.With(new Position { X = pos.X + offset.X, Y = pos.Y + offset.Y });
        }

        // Copy other components directly
        if (World.TryGet<Health>(source, out var health))
            builder.With(health);

        if (World.TryGet<Velocity>(source, out var vel))
            builder.With(vel);

        if (World.Has<Enemy>(source))
            builder.WithTag<Enemy>();
    }

    buffer.Execute();
}
```

## Performance Comparison

| Operation | Per-Entity | Batched | Improvement |
|-----------|-----------|---------|-------------|
| Spawn 1000 entities | 15ms | 3ms | 5x |
| Despawn 1000 entities | 12ms | 2ms | 6x |
| Add component to 1000 | 8ms | 1.5ms | 5x |

*Results vary based on component complexity and hardware*

## Best Practices

1. **Always use command buffer** when modifying during iteration
2. **Batch similar operations** - component adds, removes, spawns
3. **Execute once** at the end, not repeatedly
4. **Consider frame budget** - spread very large batches over frames
5. **Profile before optimizing** - small batches may not need optimization

## See Also

- [Command Buffer](../command-buffer.md) - Full command buffer documentation
- [Entity Spawning](entity-spawning.md) - Basic spawning patterns
- [Entity Pooling](entity-pooling.md) - Alternative to frequent spawning
