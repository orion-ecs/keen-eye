using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for entity lifecycle operations: creation, destruction, and version checking.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EntityBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Pre-create entities for destruction/versioning benchmarks
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

    [IterationSetup(Targets = [nameof(SpawnSingleEntity), nameof(SpawnEntityWithOneComponent),
        nameof(SpawnEntityWithTwoComponents), nameof(SpawnEntityWithFourComponents),
        nameof(SpawnBatchEntities), nameof(SpawnNamedEntity), nameof(SpawnNamedBatchEntities)])]
    public void ResetWorldForSpawn()
    {
        world.Dispose();
        world = new World();
    }

    [IterationSetup(Targets = [nameof(DespawnSingleEntity), nameof(DespawnBatchEntities)])]
    public void ResetWorldForDespawn()
    {
        world.Dispose();
        world = new World();
        entities = new Entity[EntityCount];
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    /// <summary>
    /// Measures the cost of spawning a single entity with no components.
    /// </summary>
    [Benchmark]
    public Entity SpawnSingleEntity()
    {
        return world.Spawn().Build();
    }

    /// <summary>
    /// Measures the cost of spawning an entity with one component.
    /// </summary>
    [Benchmark]
    public Entity SpawnEntityWithOneComponent()
    {
        return world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .Build();
    }

    /// <summary>
    /// Measures the cost of spawning an entity with two components.
    /// </summary>
    [Benchmark]
    public Entity SpawnEntityWithTwoComponents()
    {
        return world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .Build();
    }

    /// <summary>
    /// Measures the cost of spawning an entity with four components.
    /// </summary>
    [Benchmark]
    public Entity SpawnEntityWithFourComponents()
    {
        return world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .With(new Health { Current = 100, Max = 100 })
            .With(new Rotation { Angle = 0 })
            .Build();
    }

    /// <summary>
    /// Measures the cost of spawning many entities in a batch.
    /// </summary>
    [Benchmark]
    public void SpawnBatchEntities()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    /// <summary>
    /// Measures the cost of spawning a named entity.
    /// </summary>
    [Benchmark]
    public Entity SpawnNamedEntity()
    {
        return world.Spawn("Player")
            .With(new Position { X = 0, Y = 0 })
            .Build();
    }

    /// <summary>
    /// Measures the cost of spawning many named entities.
    /// </summary>
    [Benchmark]
    public void SpawnNamedBatchEntities()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    /// <summary>
    /// Measures the cost of despawning a single entity.
    /// </summary>
    [Benchmark]
    public bool DespawnSingleEntity()
    {
        return world.Despawn(entities[0]);
    }

    /// <summary>
    /// Measures the cost of despawning all entities.
    /// </summary>
    [Benchmark]
    public void DespawnBatchEntities()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Despawn(entities[i]);
        }
    }

    /// <summary>
    /// Measures the cost of checking if an entity is alive.
    /// </summary>
    [Benchmark]
    public bool IsAliveCheck()
    {
        return world.IsAlive(entities[0]);
    }

    /// <summary>
    /// Measures the cost of checking IsAlive for all entities.
    /// </summary>
    [Benchmark]
    public int IsAliveBatch()
    {
        var count = 0;
        for (var i = 0; i < EntityCount; i++)
        {
            if (world.IsAlive(entities[i]))
            {
                count++;
            }
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for entity naming operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EntityNamingBenchmarks
{
    private World world = null!;
    private Entity namedEntity;
    private Entity unnamedEntity;

    [Params(100, 1000)]
    public int NamedEntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create many named entities to test lookup performance
        for (var i = 0; i < NamedEntityCount; i++)
        {
            world.Spawn($"Entity_{i}")
                .With(new Position { X = i, Y = i })
                .Build();
        }

        // Keep reference to a specific entity for single lookups
        namedEntity = world.Spawn("TestEntity")
            .With(new Position { X = 0, Y = 0 })
            .Build();

        unnamedEntity = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of getting an entity's name.
    /// </summary>
    [Benchmark]
    public string? GetEntityName()
    {
        return world.GetName(namedEntity);
    }

    /// <summary>
    /// Measures the cost of getting name for unnamed entity.
    /// </summary>
    [Benchmark]
    public string? GetEntityNameUnnamed()
    {
        return world.GetName(unnamedEntity);
    }

    /// <summary>
    /// Measures the cost of finding an entity by name.
    /// </summary>
    [Benchmark]
    public Entity GetEntityByName()
    {
        return world.GetEntityByName("TestEntity");
    }

    /// <summary>
    /// Measures the cost of finding a non-existent entity by name.
    /// </summary>
    [Benchmark]
    public Entity GetEntityByNameNotFound()
    {
        return world.GetEntityByName("NonExistent");
    }
}
