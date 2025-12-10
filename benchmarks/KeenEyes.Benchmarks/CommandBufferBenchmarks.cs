using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for CommandBuffer deferred operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CommandBufferBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;
    private CommandBuffer buffer = null!;

    [Params(100, 1000)]
    public int CommandCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        buffer = new CommandBuffer();

        // Create entities for despawn/component operations
        entities = new Entity[CommandCount];
        for (var i = 0; i < CommandCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        buffer.Clear();
    }

    /// <summary>
    /// Measures the cost of queuing spawn commands.
    /// </summary>
    [Benchmark]
    public void QueueSpawnCommands()
    {
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 });
        }
    }

    /// <summary>
    /// Measures the cost of queuing despawn commands.
    /// </summary>
    [Benchmark]
    public void QueueDespawnCommands()
    {
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.Despawn(entities[i]);
        }
    }

    /// <summary>
    /// Measures the cost of queuing AddComponent commands.
    /// </summary>
    [Benchmark]
    public void QueueAddComponentCommands()
    {
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.AddComponent(entities[i], new Velocity { X = 1, Y = 0 });
        }
    }

    /// <summary>
    /// Measures the cost of queuing RemoveComponent commands.
    /// </summary>
    [Benchmark]
    public void QueueRemoveComponentCommands()
    {
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.RemoveComponent<Position>(entities[i]);
        }
    }

    /// <summary>
    /// Measures the cost of queuing SetComponent commands.
    /// </summary>
    [Benchmark]
    public void QueueSetComponentCommands()
    {
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.SetComponent(entities[i], new Position { X = i * 2, Y = i * 2 });
        }
    }
}

/// <summary>
/// Benchmarks for CommandBuffer flush (execution) performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CommandBufferFlushBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000)]
    public int CommandCount { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        world = new World();

        // Pre-create entities for component operations
        entities = new Entity[CommandCount];
        for (var i = 0; i < CommandCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures flush performance for spawn commands.
    /// </summary>
    [Benchmark]
    public Dictionary<int, Entity> FlushSpawnCommands()
    {
        var buffer = new CommandBuffer();
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 });
        }
        return buffer.Flush(world);
    }

    /// <summary>
    /// Measures flush performance for despawn commands.
    /// </summary>
    [Benchmark]
    public Dictionary<int, Entity> FlushDespawnCommands()
    {
        var buffer = new CommandBuffer();
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.Despawn(entities[i]);
        }
        return buffer.Flush(world);
    }

    /// <summary>
    /// Measures flush performance for AddComponent commands.
    /// </summary>
    [Benchmark]
    public Dictionary<int, Entity> FlushAddComponentCommands()
    {
        var buffer = new CommandBuffer();
        for (var i = 0; i < CommandCount; i++)
        {
            buffer.AddComponent(entities[i], new Velocity { X = 1, Y = 0 });
        }
        return buffer.Flush(world);
    }

    /// <summary>
    /// Measures flush performance for mixed commands.
    /// </summary>
    [Benchmark]
    public Dictionary<int, Entity> FlushMixedCommands()
    {
        var buffer = new CommandBuffer();

        // Mix of spawn, add, set, and despawn
        for (var i = 0; i < CommandCount / 4; i++)
        {
            buffer.Spawn()
                .With(new Position { X = i, Y = i });
        }

        for (var i = 0; i < CommandCount / 4; i++)
        {
            buffer.AddComponent(entities[i], new Velocity { X = 1, Y = 0 });
        }

        for (var i = CommandCount / 4; i < CommandCount / 2; i++)
        {
            buffer.SetComponent(entities[i], new Position { X = i * 10, Y = i * 10 });
        }

        for (var i = CommandCount / 2; i < 3 * CommandCount / 4; i++)
        {
            buffer.Despawn(entities[i]);
        }

        return buffer.Flush(world);
    }
}

/// <summary>
/// Benchmarks comparing direct operations vs CommandBuffer deferred operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DirectVsBufferedBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        world = new World();
        entities = new Entity[EntityCount];
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures direct entity spawning.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void DirectSpawn()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    /// <summary>
    /// Measures buffered entity spawning.
    /// </summary>
    [Benchmark]
    public void BufferedSpawn()
    {
        var buffer = new CommandBuffer();
        for (var i = 0; i < EntityCount; i++)
        {
            buffer.Spawn()
                .With(new Position { X = i, Y = i });
        }
        buffer.Flush(world);
    }

    /// <summary>
    /// Measures direct component addition.
    /// </summary>
    [Benchmark]
    public void DirectAddComponent()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Add(entities[i], new Velocity { X = 1, Y = 0 });
        }
    }

    /// <summary>
    /// Measures buffered component addition.
    /// </summary>
    [Benchmark]
    public void BufferedAddComponent()
    {
        var buffer = new CommandBuffer();
        for (var i = 0; i < EntityCount; i++)
        {
            buffer.AddComponent(entities[i], new Velocity { X = 1, Y = 0 });
        }
        buffer.Flush(world);
    }
}

/// <summary>
/// Benchmarks for using CommandBuffer during query iteration (typical use case).
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class CommandBufferDuringQueryBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [IterationSetup]
    public void IterationSetup()
    {
        world = new World();

        // Create entities with Health - some have low health
        for (var i = 0; i < EntityCount; i++)
        {
            var health = i % 10 == 0 ? 0 : 100; // 10% have 0 health
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Health { Current = health, Max = 100 })
                .Build();
        }
    }

    [IterationCleanup]
    public void IterationCleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures typical pattern: query, buffer despawns, flush.
    /// </summary>
    [Benchmark]
    public int QueryAndDespawnDead()
    {
        var buffer = new CommandBuffer();
        var despawnCount = 0;

        foreach (var entity in world.Query<Health>())
        {
            ref var health = ref world.Get<Health>(entity);
            if (health.Current <= 0)
            {
                buffer.Despawn(entity);
                despawnCount++;
            }
        }

        buffer.Flush(world);
        return despawnCount;
    }

    /// <summary>
    /// Measures pattern: spawn new entities during iteration.
    /// </summary>
    [Benchmark]
    public int QueryAndSpawnNew()
    {
        var buffer = new CommandBuffer();
        var spawnCount = 0;

        foreach (var entity in world.Query<Position>())
        {
            // Spawn a child entity for every 100th entity
            ref var pos = ref world.Get<Position>(entity);
            if ((int)pos.X % 100 == 0)
            {
                buffer.Spawn()
                    .With(new Position { X = pos.X + 1, Y = pos.Y + 1 });
                spawnCount++;
            }
        }

        buffer.Flush(world);
        return spawnCount;
    }
}
