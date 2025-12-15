using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for query construction and iteration with different arities.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryBenchmarks
{
    private World world = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with different component combinations
        // Half have Position only, half have Position + Velocity
        for (var i = 0; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }

        for (var i = EntityCount / 2; i < EntityCount; i++)
        {
            world.Spawn()
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
    /// Measures query iteration with one component type.
    /// </summary>
    [Benchmark]
    public int QuerySingleComponent()
    {
        var count = 0;
        foreach (var entity in world.Query<Position>())
        {
            ref var pos = ref world.Get<Position>(entity);
            pos.X += 1;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query iteration with two component types.
    /// </summary>
    [Benchmark]
    public int QueryTwoComponents()
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
    /// Measures the overhead of constructing a query (without iteration).
    /// </summary>
    [Benchmark]
    public QueryBuilder QueryConstruction()
    {
        return world.Query<Position>();
    }

    /// <summary>
    /// Measures the overhead of constructing a two-component query.
    /// </summary>
    [Benchmark]
    public QueryBuilder QueryConstructionTwoComponents()
    {
        return world.Query<Position, Velocity>();
    }
}

/// <summary>
/// Benchmarks for query filtering with With and Without.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryFilterBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with various combinations:
        // 25% have Position + ActiveTag
        // 25% have Position + FrozenTag
        // 25% have Position + ActiveTag + FrozenTag
        // 25% have just Position
        for (var i = 0; i < EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .WithTag<ActiveTag>()
                .Build();
        }

        for (var i = EntityCount / 4; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .WithTag<FrozenTag>()
                .Build();
        }

        for (var i = EntityCount / 2; i < 3 * EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .WithTag<ActiveTag>()
                .WithTag<FrozenTag>()
                .Build();
        }

        for (var i = 3 * EntityCount / 4; i < EntityCount; i++)
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
    /// Measures unfiltered query (baseline).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int QueryNoFilter()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with one With filter.
    /// </summary>
    [Benchmark]
    public int QueryWithFilter()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>().With<ActiveTag>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with one Without filter.
    /// </summary>
    [Benchmark]
    public int QueryWithoutFilter()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>().Without<FrozenTag>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with both With and Without filters.
    /// </summary>
    [Benchmark]
    public int QueryWithAndWithoutFilters()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>().With<ActiveTag>().Without<FrozenTag>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures complex query with multiple With filters.
    /// </summary>
    [Benchmark]
    public int QueryMultipleWithFilters()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>().With<ActiveTag>().With<FrozenTag>())
        {
            count++;
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for query scalability with varying entity counts.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryScalingBenchmarks
{
    private World world = null!;

    [Params(100, 1000, 10000, 100000)]
    public int EntityCount { get; set; }

    [Params(0.1, 0.5, 1.0)]
    public double MatchRatio { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        var matchingCount = (int)(EntityCount * MatchRatio);

        // Create matching entities
        for (var i = 0; i < matchingCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        // Create non-matching entities
        for (var i = matchingCount; i < EntityCount; i++)
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
    /// Measures query iteration time as entity count and match ratio vary.
    /// </summary>
    [Benchmark]
    public int QueryIteration()
    {
        var count = 0;
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            count++;
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for queries with three and four component types.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryArityBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with 1-4 components
        for (var i = 0; i < EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }

        for (var i = EntityCount / 4; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        for (var i = EntityCount / 2; i < 3 * EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        for (var i = 3 * EntityCount / 4; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Rotation { Angle = 0 })
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures query with one component (baseline).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int QueryOneComponent()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with two components.
    /// </summary>
    [Benchmark]
    public int QueryTwoComponents()
    {
        var count = 0;
        foreach (var _ in world.Query<Position, Velocity>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with three components.
    /// </summary>
    [Benchmark]
    public int QueryThreeComponents()
    {
        var count = 0;
        foreach (var _ in world.Query<Position, Velocity, Health>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query with four components.
    /// </summary>
    [Benchmark]
    public int QueryFourComponents()
    {
        var count = 0;
        foreach (var _ in world.Query<Position, Velocity, Health, Rotation>())
        {
            count++;
        }
        return count;
    }
}

/// <summary>
/// Benchmarks for empty query results.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EmptyQueryBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities without Rotation component
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
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
    /// Measures query that matches all entities (baseline).
    /// </summary>
    [Benchmark(Baseline = true)]
    public int QueryAllMatch()
    {
        var count = 0;
        foreach (var _ in world.Query<Position>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures query that matches no entities.
    /// </summary>
    [Benchmark]
    public int QueryNoMatch()
    {
        var count = 0;
        foreach (var _ in world.Query<Rotation>())
        {
            count++;
        }
        return count;
    }
}
