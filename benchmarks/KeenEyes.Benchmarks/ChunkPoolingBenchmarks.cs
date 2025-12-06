using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for chunk pooling performance at large entity scales.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ChunkPoolingBenchmarks
{
    private World world = null!;

    [Params(10_000, 50_000, 100_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [IterationSetup]
    public void ResetWorld()
    {
        world.Dispose();
        world = new World();
    }

    /// <summary>
    /// Measures the cost of spawning many entities with chunked storage.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int SpawnManyEntities()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }
        return EntityCount;
    }

    /// <summary>
    /// Measures the cost of spawning entities with four components.
    /// </summary>
    [Benchmark]
    public int SpawnManyEntitiesFourComponents()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Rotation { Angle = 0 })
                .Build();
        }
        return EntityCount;
    }

    /// <summary>
    /// Measures spawning with large components to stress chunk allocation.
    /// </summary>
    [Benchmark]
    public int SpawnManyEntitiesLargeComponent()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new LargeComponent())
                .Build();
        }
        return EntityCount;
    }
}

/// <summary>
/// Benchmarks for chunk pool reuse efficiency.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ChunkPoolReuseBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(10_000, 50_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entities = new Entity[EntityCount];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [IterationSetup]
    public void ResetWorld()
    {
        world.Dispose();
        world = new World();
    }

    /// <summary>
    /// Measures spawn-then-despawn cycle to test chunk pool reuse.
    /// </summary>
    [Benchmark]
    public ChunkPoolStats SpawnDespawnCycle()
    {
        // First spawn wave
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        // Despawn all (chunks should return to pool)
        for (var i = 0; i < EntityCount; i++)
        {
            world.Despawn(entities[i]);
        }

        // Second spawn wave (should reuse pooled chunks)
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        return world.ArchetypeManager.ChunkPool.GetStats();
    }

    /// <summary>
    /// Measures partial despawn and respawn to test incremental chunk reuse.
    /// </summary>
    [Benchmark]
    public int PartialDespawnRespawn()
    {
        // Initial spawn
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        // Despawn half
        for (var i = 0; i < EntityCount / 2; i++)
        {
            world.Despawn(entities[i]);
        }

        // Respawn half (tests swap-back and chunk fragmentation)
        for (var i = 0; i < EntityCount / 2; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        return EntityCount;
    }
}

/// <summary>
/// Benchmarks for query iteration at large scales with chunked storage.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LargeScaleQueryBenchmarks
{
    private World world = null!;

    [Params(50_000, 100_000, 200_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities - half with Position+Velocity, half with Position only
        for (var i = 0; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        for (var i = EntityCount / 2; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures iteration over all entities at large scale.
    /// </summary>
    [Benchmark(Baseline = true)]
    public float QueryAllPositions()
    {
        var sum = 0f;
        foreach (var entity in world.Query<Position>())
        {
            ref var pos = ref world.Get<Position>(entity);
            sum += pos.X;
        }
        return sum;
    }

    /// <summary>
    /// Measures iteration with component mutation at large scale.
    /// </summary>
    [Benchmark]
    public int QueryAndUpdatePositions()
    {
        var count = 0;
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            pos.Y += vel.Y;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures filtered query at large scale.
    /// </summary>
    [Benchmark]
    public int QueryWithFilter()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>().With<Velocity>())
        {
            count++;
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for archetype transitions at large scales.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ArchetypeTransitionBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(10_000, 50_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
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

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [IterationSetup]
    public void ResetEntities()
    {
        // Remove any added components
        for (var i = 0; i < EntityCount; i++)
        {
            if (world.Has<Velocity>(entities[i]))
            {
                world.Remove<Velocity>(entities[i]);
            }
        }
    }

    /// <summary>
    /// Measures adding a component to many entities (archetype transitions).
    /// </summary>
    [Benchmark]
    public int AddComponentToAll()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Add(entities[i], new Velocity { X = 1, Y = 0 });
        }
        return EntityCount;
    }
}

/// <summary>
/// Benchmarks measuring chunk count and memory efficiency.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ChunkEfficiencyBenchmarks
{
    private World world = null!;

    [Params(1_000, 10_000, 100_000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    [IterationSetup]
    public void ResetWorld()
    {
        world.Dispose();
        world = new World();
    }

    /// <summary>
    /// Measures entities spawned into a single archetype (optimal chunk packing).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int SingleArchetypeSpawn()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }
        return world.ArchetypeManager.Count;
    }

    /// <summary>
    /// Measures entities spawned across multiple archetypes (chunk fragmentation).
    /// </summary>
    [Benchmark]
    public int MultipleArchetypeSpawn()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            var builder = world.Spawn()
                .With(new Position { X = i, Y = i });

            // Every 4th entity gets different component combinations
            switch (i % 4)
            {
                case 0:
                    builder.With(new Velocity { X = 1, Y = 0 });
                    break;
                case 1:
                    builder.With(new Health { Current = 100, Max = 100 });
                    break;
                case 2:
                    builder.With(new Velocity { X = 1, Y = 0 })
                           .With(new Health { Current = 100, Max = 100 });
                    break;
                case 3:
                    builder.With(new Rotation { Angle = 0 });
                    break;
            }

            builder.Build();
        }
        return world.ArchetypeManager.Count;
    }
}
