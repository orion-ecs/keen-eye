# Entity Spawning Patterns

## Problem

You want to create entities efficiently at runtime, whether spawning single entities, batches, or using templates.

## Solution

### Basic Spawning

```csharp
// Simple entity with components
var entity = world.Spawn()
    .With(new Position { X = 100, Y = 200 })
    .With(new Velocity { X = 10, Y = 0 })
    .Build();

// Named entity (useful for debugging and lookup)
var player = world.Spawn("Player")
    .With(new Position { X = 0, Y = 0 })
    .With(new Health { Current = 100, Max = 100 })
    .Build();

// Tag-only entity
var marker = world.Spawn()
    .WithTag<Waypoint>()
    .Build();
```

### Spawning via Command Buffer

Use command buffers when spawning during iteration:

```csharp
public class SpawnerSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<Spawner, Position>())
        {
            ref var spawner = ref World.Get<Spawner>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            spawner.Timer -= deltaTime;
            if (spawner.Timer <= 0)
            {
                // Queue spawn for after iteration
                buffer.Spawn()
                    .With(new Position { X = pos.X, Y = pos.Y })
                    .With(new Velocity { X = 0, Y = -100 })
                    .WithTag<Bullet>();

                spawner.Timer = spawner.Interval;
            }
        }

        buffer.Execute();
    }
}
```

### Batch Spawning

For spawning many entities at once:

```csharp
public static void SpawnEnemyWave(World world, int count, float startX, float spacing)
{
    var buffer = world.GetCommandBuffer();

    for (int i = 0; i < count; i++)
    {
        buffer.Spawn()
            .With(new Position { X = startX + (i * spacing), Y = 0 })
            .With(new Health { Current = 50, Max = 50 })
            .WithTag<Enemy>();
    }

    buffer.Execute();  // Single batch operation
}
```

### Factory Pattern

Encapsulate entity creation in factory methods:

```csharp
public static class EntityFactory
{
    public static Entity CreatePlayer(World world, float x, float y)
    {
        return world.Spawn("Player")
            .With(new Position { X = x, Y = y })
            .With(new Velocity { X = 0, Y = 0 })
            .With(new Health { Current = 100, Max = 100 })
            .With(new PlayerInput())
            .WithTag<Player>()
            .Build();
    }

    public static Entity CreateEnemy(World world, float x, float y, EnemyType type)
    {
        var builder = world.Spawn()
            .With(new Position { X = x, Y = y })
            .With(new AIState { Current = AIStateType.Idle })
            .WithTag<Enemy>();

        // Add type-specific components
        switch (type)
        {
            case EnemyType.Fast:
                builder
                    .With(new Health { Current = 30, Max = 30 })
                    .With(new Speed { Value = 200 });
                break;

            case EnemyType.Tank:
                builder
                    .With(new Health { Current = 200, Max = 200 })
                    .With(new Speed { Value = 50 })
                    .With(new Armor { Value = 10 });
                break;
        }

        return builder.Build();
    }

    public static Entity CreateBullet(World world, Position origin, Velocity velocity, Entity owner)
    {
        return world.Spawn()
            .With(origin)
            .With(velocity)
            .With(new Projectile { Owner = owner, Damage = 10 })
            .Build();
    }
}
```

### Using Prefabs

Define reusable templates:

```csharp
// Define prefab once
var enemyPrefab = world.CreatePrefab("BasicEnemy")
    .With(new Health { Current = 50, Max = 50 })
    .With(new Speed { Value = 100 })
    .WithTag<Enemy>()
    .Build();

// Spawn from prefab (copies all components)
var enemy1 = world.SpawnPrefab(enemyPrefab)
    .With(new Position { X = 100, Y = 100 })  // Override/add
    .Build();

var enemy2 = world.SpawnPrefab(enemyPrefab)
    .With(new Position { X = 200, Y = 100 })
    .Build();
```

## Why This Works

### Builder Pattern Benefits

The fluent builder pattern:
- **Type-safe**: Can't add invalid component combinations
- **Readable**: Clear what components an entity has
- **Efficient**: Components allocated together in same archetype
- **Flexible**: Mix-and-match components freely

### Command Buffer for Iteration Safety

Spawning during iteration would invalidate queries. Command buffers:
- Queue operations until `Execute()`
- Batch similar operations for efficiency
- Maintain iteration safety

### Factory Encapsulation

Factory methods:
- Hide component details from callers
- Ensure consistent entity setup
- Make changes in one place
- Enable testing with mock factories

### Prefabs vs Factories

| Feature | Prefabs | Factories |
|---------|---------|-----------|
| Data-driven | Yes (can serialize) | No (code only) |
| Runtime modification | Yes | No |
| Complex logic | Limited | Full C# |
| Performance | Faster (no code) | Minimal overhead |

Use prefabs for data-driven content (level design, modding). Use factories for complex conditional logic.

## Variations

### Spawning from Events

```csharp
// Trigger component
[Component]
public partial struct SpawnRequest : IComponent
{
    public string PrefabName;
    public Vector2 Position;
}

// System processes spawn requests
public class SpawnRequestSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<SpawnRequest>())
        {
            ref readonly var request = ref World.Get<SpawnRequest>(entity);

            var prefab = World.GetPrefab(request.PrefabName);
            buffer.SpawnPrefab(prefab)
                .With(new Position { X = request.Position.X, Y = request.Position.Y });

            // Remove processed request
            buffer.Remove<SpawnRequest>(entity);
        }

        buffer.Execute();
    }
}
```

### Spawn Points

```csharp
[Component]
public partial struct SpawnPoint : IComponent
{
    public string PrefabName;
    public float Interval;
    public float Timer;
    public int MaxSpawns;
    public int CurrentSpawns;
}

public class SpawnPointSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<SpawnPoint, Position>())
        {
            ref var spawn = ref World.Get<SpawnPoint>(entity);
            ref readonly var pos = ref World.Get<Position>(entity);

            if (spawn.CurrentSpawns >= spawn.MaxSpawns)
                continue;

            spawn.Timer -= deltaTime;
            if (spawn.Timer <= 0)
            {
                var prefab = World.GetPrefab(spawn.PrefabName);
                buffer.SpawnPrefab(prefab)
                    .With(new Position { X = pos.X, Y = pos.Y });

                spawn.CurrentSpawns++;
                spawn.Timer = spawn.Interval;
            }
        }

        buffer.Execute();
    }
}
```

### Deferred Spawning with Coroutines

```csharp
[Component]
public partial struct DelayedSpawn : IComponent
{
    public string PrefabName;
    public Position Position;
    public float Delay;
}

public class DelayedSpawnSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        var buffer = World.GetCommandBuffer();

        foreach (var entity in World.Query<DelayedSpawn>())
        {
            ref var spawn = ref World.Get<DelayedSpawn>(entity);
            spawn.Delay -= deltaTime;

            if (spawn.Delay <= 0)
            {
                var prefab = World.GetPrefab(spawn.PrefabName);
                buffer.SpawnPrefab(prefab)
                    .With(spawn.Position);

                buffer.Despawn(entity);  // Remove the delay entity
            }
        }

        buffer.Execute();
    }
}

// Usage: Spawn enemy after 3 seconds
world.Spawn()
    .With(new DelayedSpawn
    {
        PrefabName = "Enemy",
        Position = new Position { X = 100, Y = 100 },
        Delay = 3f
    })
    .Build();
```

## See Also

- [Prefabs Guide](../prefabs.md) - Full prefab system documentation
- [Command Buffer](../command-buffer.md) - Safe entity operations
- [Entity Pooling](entity-pooling.md) - Reuse entities instead of creating new ones
