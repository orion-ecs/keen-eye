using BenchmarkDotNet.Attributes;

namespace KeenEyes.AotVsJit.Benchmarks;

/// <summary>
/// Benchmarks component access (Get/Set) performance in AOT vs JIT.
/// Component access is a critical hot path operation.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ComponentAccessBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entities = new Entity[EntityCount];

        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
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
    /// Benchmark: Get component by reference.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public float GetComponent()
    {
        var sum = 0f;
        for (var i = 0; i < EntityCount; i++)
        {
            ref readonly var pos = ref world.Get<Position>(entities[i]);
            sum += pos.X + pos.Y + pos.Z;
        }
        return sum;
    }

    /// <summary>
    /// Benchmark: Get and modify component.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public int ModifyComponent()
    {
        var count = 0;
        for (var i = 0; i < EntityCount; i++)
        {
            ref var pos = ref world.Get<Position>(entities[i]);
            pos.X += 1;
            pos.Y += 1;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Benchmark: Get multiple components.
    /// Expected: ±5% performance difference between AOT and JIT.
    /// </summary>
    [Benchmark]
    public float GetMultipleComponents()
    {
        var sum = 0f;
        for (var i = 0; i < EntityCount; i++)
        {
            ref readonly var pos = ref world.Get<Position>(entities[i]);
            ref readonly var vel = ref world.Get<Velocity>(entities[i]);
            sum += pos.X + vel.X + pos.Y + vel.Y;
        }
        return sum;
    }
}
