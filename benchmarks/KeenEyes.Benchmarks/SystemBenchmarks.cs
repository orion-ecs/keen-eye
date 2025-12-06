using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Simple movement system for benchmarking.
/// </summary>
public class MovementSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
    }
}

/// <summary>
/// Health decay system for benchmarking.
/// </summary>
public class HealthDecaySystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Health>())
        {
            ref var health = ref World.Get<Health>(entity);
            health.Current -= 1;
        }
    }
}

/// <summary>
/// Rotation update system for benchmarking.
/// </summary>
public class RotationSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Rotation>())
        {
            ref var rot = ref World.Get<Rotation>(entity);
            rot.Angle += deltaTime * 0.1f;
        }
    }
}

/// <summary>
/// Complex system that queries multiple component combinations.
/// </summary>
public class ComplexSystem : SystemBase
{
    public override void Update(float deltaTime)
    {
        // Update moving entities
        foreach (var entity in World.Query<Position, Velocity>().Without<FrozenTag>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }

        // Update health for active entities
        foreach (var entity in World.Query<Health>().With<ActiveTag>())
        {
            ref var health = ref World.Get<Health>(entity);
            health.Current = Math.Min(health.Current + 1, health.Max);
        }
    }
}

/// <summary>
/// Benchmarks for system execution overhead and throughput.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SystemBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with various components
        for (var i = 0; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        for (var i = EntityCount / 2; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = -1, Y = 0.5f })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        world.AddSystem<MovementSystem>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures a single system update.
    /// </summary>
    [Benchmark]
    public void SingleSystemUpdate()
    {
        world.Update(0.016f);
    }
}

/// <summary>
/// Benchmarks for multiple systems in a pipeline.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SystemPipelineBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [Params(1, 3, 5)]
    public int SystemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create entities with all required components
        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .With(new Health { Current = 100, Max = 100 })
                .With(new Rotation { Angle = 0 })
                .Build();
        }

        // Add systems based on parameter
        AddSystemsByCount();
    }

    private void AddSystemsByCount()
    {
        switch (SystemCount)
        {
            case >= 5:
                world.AddSystem<HealthDecaySystem>();
                goto case 4;
            case 4:
                world.AddSystem<MovementSystem>(); // Duplicate for more load
                goto case 3;
            case 3:
                world.AddSystem<RotationSystem>();
                goto case 2;
            case 2:
                world.AddSystem<HealthDecaySystem>();
                goto case 1;
            case 1:
                world.AddSystem<MovementSystem>();
                break;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures update with multiple systems.
    /// </summary>
    [Benchmark]
    public void MultiSystemUpdate()
    {
        world.Update(0.016f);
    }
}

/// <summary>
/// Benchmarks for system initialization overhead.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SystemInitBenchmarks
{
    private World world = null!;

    [IterationSetup]
    public void Setup()
    {
        world = new World();

        // Pre-create some entities
        for (var i = 0; i < 1000; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }
    }

    [IterationCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures system registration overhead.
    /// </summary>
    [Benchmark]
    public World AddSingleSystem()
    {
        return world.AddSystem<MovementSystem>();
    }

    /// <summary>
    /// Measures multiple system registration.
    /// </summary>
    [Benchmark]
    public World AddMultipleSystems()
    {
        return world
            .AddSystem<MovementSystem>()
            .AddSystem<HealthDecaySystem>()
            .AddSystem<RotationSystem>();
    }
}

/// <summary>
/// Benchmarks for systems with filtered queries.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class FilteredSystemBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Create varied entity population:
        // 25% active + moving
        // 25% frozen + moving
        // 25% active + health
        // 25% plain position only
        for (var i = 0; i < EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .WithTag<ActiveTag>()
                .Build();
        }

        for (var i = EntityCount / 4; i < EntityCount / 2; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .WithTag<FrozenTag>()
                .Build();
        }

        for (var i = EntityCount / 2; i < 3 * EntityCount / 4; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Health { Current = 50, Max = 100 })
                .WithTag<ActiveTag>()
                .Build();
        }

        for (var i = 3 * EntityCount / 4; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }

        world.AddSystem<ComplexSystem>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures system with filtered queries.
    /// </summary>
    [Benchmark]
    public void FilteredSystemUpdate()
    {
        world.Update(0.016f);
    }
}
