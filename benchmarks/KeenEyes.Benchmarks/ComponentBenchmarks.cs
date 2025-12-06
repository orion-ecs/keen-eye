using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for component operations: Get, Set, Add, Remove, Has.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ComponentBenchmarks
{
    private World world = null!;
    private Entity entity;
    private Entity[] entities = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create a single entity for single-operation benchmarks
        entity = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 1, Y = 1 })
            .With(new Health { Current = 100, Max = 100 })
            .Build();

        // Create many entities for batch benchmarks
        entities = new Entity[EntityCount];
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of getting a component by reference.
    /// </summary>
    [Benchmark]
    public ref Position GetComponent()
    {
        return ref world.Get<Position>(entity);
    }

    /// <summary>
    /// Measures the cost of getting and modifying a component.
    /// </summary>
    [Benchmark]
    public void GetAndModifyComponent()
    {
        ref var pos = ref world.Get<Position>(entity);
        pos.X += 1;
        pos.Y += 1;
    }

    /// <summary>
    /// Measures the cost of getting components for all entities.
    /// </summary>
    [Benchmark]
    public void GetComponentBatch()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            _ = pos.X;
        }
    }

    /// <summary>
    /// Measures the cost of setting (replacing) a component.
    /// </summary>
    [Benchmark]
    public void SetComponent()
    {
        world.Set(entity, new Position { X = 100, Y = 200 });
    }

    /// <summary>
    /// Measures the cost of setting components for all entities.
    /// </summary>
    [Benchmark]
    public void SetComponentBatch()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Set(entities[i], new Position { X = i * 2, Y = i * 2 });
        }
    }

    /// <summary>
    /// Measures the cost of checking if an entity has a component.
    /// </summary>
    [Benchmark]
    public bool HasComponent()
    {
        return world.Has<Position>(entity);
    }

    /// <summary>
    /// Measures the cost of checking Has for a missing component.
    /// </summary>
    [Benchmark]
    public bool HasComponentMissing()
    {
        return world.Has<Rotation>(entity);
    }

    /// <summary>
    /// Measures the cost of checking Has for all entities.
    /// </summary>
    [Benchmark]
    public int HasComponentBatch()
    {
        var count = 0;
        for (var i = 0; i < EntityCount; i++)
        {
            if (world.Has<Position>(entities[i]))
            {
                count++;
            }
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for Add and Remove operations (which modify entity structure).
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ComponentAddRemoveBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [IterationSetup]
    public void SetupIteration()
    {
        world = new World();
        entities = new Entity[EntityCount];

        // Create entities with just Position for add benchmarks
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }
    }

    [IterationCleanup]
    public void CleanupIteration()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of adding a component to an entity.
    /// </summary>
    [Benchmark]
    public void AddComponent()
    {
        world.Add(entities[0], new Velocity { X = 1, Y = 1 });
    }

    /// <summary>
    /// Measures the cost of adding components to all entities.
    /// </summary>
    [Benchmark]
    public void AddComponentBatch()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Add(entities[i], new Velocity { X = 1, Y = 1 });
        }
    }

    /// <summary>
    /// Measures the cost of removing a component from an entity.
    /// </summary>
    [Benchmark]
    public bool RemoveComponent()
    {
        return world.Remove<Position>(entities[0]);
    }

    /// <summary>
    /// Measures the cost of removing components from all entities.
    /// </summary>
    [Benchmark]
    public void RemoveComponentBatch()
    {
        for (var i = 0; i < EntityCount; i++)
        {
            world.Remove<Position>(entities[i]);
        }
    }
}

/// <summary>
/// Benchmarks for different component sizes to measure memory layout effects.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ComponentSizeBenchmarks
{
    private World world = null!;
    private Entity smallEntity;
    private Entity mediumEntity;
    private Entity largeEntity;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        smallEntity = world.Spawn()
            .With(new SmallComponent { Value = 42 })
            .Build();

        mediumEntity = world.Spawn()
            .With(new MediumComponent { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8 })
            .Build();

        largeEntity = world.Spawn()
            .With(new LargeComponent())
            .Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures Get performance for small components (4 bytes).
    /// </summary>
    [Benchmark(Baseline = true)]
    public ref SmallComponent GetSmallComponent()
    {
        return ref world.Get<SmallComponent>(smallEntity);
    }

    /// <summary>
    /// Measures Get performance for medium components (64 bytes).
    /// </summary>
    [Benchmark]
    public ref MediumComponent GetMediumComponent()
    {
        return ref world.Get<MediumComponent>(mediumEntity);
    }

    /// <summary>
    /// Measures Get performance for large components (256 bytes).
    /// </summary>
    [Benchmark]
    public ref LargeComponent GetLargeComponent()
    {
        return ref world.Get<LargeComponent>(largeEntity);
    }
}

/// <summary>
/// Benchmarks for GetComponents introspection API.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class GetComponentsBenchmarks
{
    private World world = null!;
    private Entity entityWithOne;
    private Entity entityWithFour;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        entityWithOne = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();

        entityWithFour = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Velocity { X = 1, Y = 1 })
            .With(new Health { Current = 100, Max = 100 })
            .With(new Rotation { Angle = 0 })
            .Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures GetComponents for entity with one component.
    /// </summary>
    [Benchmark]
    public int GetComponentsOne()
    {
        var count = 0;
        foreach (var _ in world.GetComponents(entityWithOne))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures GetComponents for entity with four components.
    /// </summary>
    [Benchmark]
    public int GetComponentsFour()
    {
        var count = 0;
        foreach (var _ in world.GetComponents(entityWithFour))
        {
            count++;
        }
        return count;
    }
}
