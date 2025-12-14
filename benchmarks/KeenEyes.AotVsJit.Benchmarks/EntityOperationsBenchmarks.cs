using BenchmarkDotNet.Attributes;

namespace KeenEyes.AotVsJit.Benchmarks;

/// <summary>
/// Benchmarks entity spawn and despawn performance in AOT vs JIT.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EntityOperationsBenchmarks
{
    private World world = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [IterationSetup]
    public void Setup()
    {
        world = new World();
    }

    [IterationCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Benchmark: Spawn entities with components.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int SpawnEntities()
    {
        var count = 0;
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .WithPosition(i, i, 0)
                .WithVelocity(1, 0, 0)
                .Build();
            count++;
        }
        return count;
    }

    /// <summary>
    /// Benchmark: Spawn and despawn entities.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int SpawnAndDespawnEntities()
    {
        var entities = new Entity[EntityCount];

        // Spawn
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .WithPosition(i, i, 0)
                .WithVelocity(1, 0, 0)
                .Build();
        }

        // Despawn
        for (var i = 0; i < EntityCount; i++)
        {
            world.Despawn(entities[i]);
        }

        return EntityCount;
    }

    /// <summary>
    /// Benchmark: Spawn entities with many components.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int SpawnEntitiesWithManyComponents()
    {
        var count = 0;
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .WithPosition(i, i, 0)
                .WithVelocity(1, 0, 0)
                .WithHealth(100, 100)
                .WithDamage(10)
                .WithEnemyTag()
                .Build();
            count++;
        }
        return count;
    }
}
