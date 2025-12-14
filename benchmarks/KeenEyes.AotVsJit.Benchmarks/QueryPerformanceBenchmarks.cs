using BenchmarkDotNet.Attributes;

namespace KeenEyes.AotVsJit.Benchmarks;

/// <summary>
/// Benchmarks query iteration performance in AOT vs JIT.
/// This is a critical hot path operation that should have similar performance.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class QueryPerformanceBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with different component combinations
        // 50% have Position + Velocity
        // 25% have Position only
        // 25% have Position + Velocity + Health
        for (var i = 0; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .WithPosition(i, i, 0)
                .WithVelocity(1, 0, 0)
                .Build();
        }

        for (var i = 0; i < EntityCount / 4; i++)
        {
            world.Spawn()
                .WithPosition(i, i, 0)
                .Build();
        }

        for (var i = 0; i < EntityCount / 4; i++)
        {
            world.Spawn()
                .WithPosition(i, i, 0)
                .WithVelocity(1, 0, 0)
                .WithHealth(100, 100)
                .Build();
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Benchmark: Query iteration with single component.
    /// Expected: ±5% performance difference between AOT and JIT.
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
    /// Benchmark: Query iteration with two components.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int QueryTwoComponents()
    {
        var count = 0;
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            pos.Y += vel.Y;
            pos.Z += vel.Z;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Benchmark: Query iteration with three components.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int QueryThreeComponents()
    {
        var count = 0;
        foreach (var entity in world.Query<Position, Velocity, Health>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            ref readonly var health = ref world.Get<Health>(entity);

            if (health.Current > 0)
            {
                pos.X += vel.X;
                pos.Y += vel.Y;
                pos.Z += vel.Z;
            }
            count++;
        }
        return count;
    }
}
