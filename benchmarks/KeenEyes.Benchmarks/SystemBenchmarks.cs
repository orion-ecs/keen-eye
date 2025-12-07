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

/// <summary>
/// System with lifecycle hooks for benchmarking overhead.
/// </summary>
public class LifecycleHookSystem : SystemBase
{
    public int BeforeCount { get; private set; }
    public int UpdateCount { get; private set; }
    public int AfterCount { get; private set; }

    protected override void OnBeforeUpdate(float deltaTime)
    {
        BeforeCount++;
    }

    public override void Update(float deltaTime)
    {
        foreach (var entity in World.Query<Position, Velocity>())
        {
            ref var pos = ref World.Get<Position>(entity);
            ref var vel = ref World.Get<Velocity>(entity);
            pos.X += vel.X * deltaTime;
            pos.Y += vel.Y * deltaTime;
        }
        UpdateCount++;
    }

    protected override void OnAfterUpdate(float deltaTime)
    {
        AfterCount++;
    }
}

/// <summary>
/// System without lifecycle hooks for comparison.
/// </summary>
public class NoHooksSystem : SystemBase
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
/// Benchmarks for system lifecycle hooks overhead.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class LifecycleHookBenchmarks
{
    private World worldWithHooks = null!;
    private World worldWithoutHooks = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        worldWithHooks = new World();
        worldWithoutHooks = new World();

        // Create identical entity populations
        for (var i = 0; i < EntityCount; i++)
        {
            worldWithHooks.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();

            worldWithoutHooks.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        worldWithHooks.AddSystem<LifecycleHookSystem>();
        worldWithoutHooks.AddSystem<NoHooksSystem>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        worldWithHooks.Dispose();
        worldWithoutHooks.Dispose();
    }

    /// <summary>
    /// Measures system update with lifecycle hooks (OnBeforeUpdate, OnAfterUpdate).
    /// </summary>
    [Benchmark(Baseline = true)]
    public void SystemWithHooks()
    {
        worldWithHooks.Update(0.016f);
    }

    /// <summary>
    /// Measures system update without lifecycle hooks for comparison.
    /// </summary>
    [Benchmark]
    public void SystemWithoutHooks()
    {
        worldWithoutHooks.Update(0.016f);
    }
}

/// <summary>
/// Phase-specific system for ordering benchmarks.
/// </summary>
public class PhaseSystem : SystemBase
{
    public int UpdateCount { get; private set; }

    public override void Update(float deltaTime)
    {
        UpdateCount++;
    }
}

/// <summary>
/// Benchmarks for system phase ordering.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SystemOrderingBenchmarks
{
    private World worldOrdered = null!;
    private World worldUnordered = null!;

    [Params(5, 10, 20)]
    public int SystemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        worldOrdered = new World();
        worldUnordered = new World();

        // Create some entities
        for (var i = 0; i < 1000; i++)
        {
            worldOrdered.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();

            worldUnordered.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }

        // Add systems with explicit phases (requires sorting)
        var phases = new[] { SystemPhase.EarlyUpdate, SystemPhase.Update, SystemPhase.LateUpdate, SystemPhase.Render };
        for (var i = 0; i < SystemCount; i++)
        {
            var phase = phases[i % phases.Length];
            worldOrdered.AddSystem(new PhaseSystem(), phase, order: i);
        }

        // Add systems without phases (default ordering)
        for (var i = 0; i < SystemCount; i++)
        {
            worldUnordered.AddSystem(new PhaseSystem());
        }

        // Force initial sort
        worldOrdered.Update(0.016f);
        worldUnordered.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        worldOrdered.Dispose();
        worldUnordered.Dispose();
    }

    /// <summary>
    /// Measures update with systems sorted across multiple phases.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void OrderedSystems()
    {
        worldOrdered.Update(0.016f);
    }

    /// <summary>
    /// Measures update with systems in default order.
    /// </summary>
    [Benchmark]
    public void UnorderedSystems()
    {
        worldUnordered.Update(0.016f);
    }
}

/// <summary>
/// Benchmarks for runtime system enable/disable.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class RuntimeControlBenchmarks
{
    private World world = null!;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        for (var i = 0; i < 1000; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        world.AddSystem<MovementSystem>();
        world.AddSystem<RotationSystem>();
        world.AddSystem<HealthDecaySystem>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures GetSystem lookup by type.
    /// </summary>
    [Benchmark]
    public MovementSystem? GetSystem()
    {
        return world.GetSystem<MovementSystem>();
    }

    /// <summary>
    /// Measures disabling a system.
    /// </summary>
    [Benchmark]
    public bool DisableSystem()
    {
        var result = world.DisableSystem<MovementSystem>();
        world.EnableSystem<MovementSystem>(); // Reset for next iteration
        return result;
    }

    /// <summary>
    /// Measures enabling a disabled system.
    /// </summary>
    [Benchmark]
    public bool EnableSystem()
    {
        world.DisableSystem<MovementSystem>(); // Setup for benchmark
        return world.EnableSystem<MovementSystem>();
    }

    /// <summary>
    /// Measures update with some systems disabled.
    /// </summary>
    [Benchmark]
    public void UpdateWithDisabledSystems()
    {
        world.DisableSystem<HealthDecaySystem>();
        world.Update(0.016f);
        world.EnableSystem<HealthDecaySystem>(); // Reset
    }
}

/// <summary>
/// Benchmarks for World.FixedUpdate() method.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class FixedUpdateBenchmarks
{
    private World world = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        for (var i = 0; i < EntityCount; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        // Add systems to different phases
        world.AddSystem<MovementSystem>(SystemPhase.FixedUpdate);
        world.AddSystem<HealthDecaySystem>(SystemPhase.Update);
        world.AddSystem<RotationSystem>(SystemPhase.LateUpdate);

        // Force initial sort
        world.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures FixedUpdate which only runs FixedUpdate phase systems.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void FixedUpdateOnly()
    {
        world.FixedUpdate(0.016f);
    }

    /// <summary>
    /// Measures full Update which runs all phases.
    /// </summary>
    [Benchmark]
    public void FullUpdate()
    {
        world.Update(0.016f);
    }
}

/// <summary>
/// System with dependency constraint for benchmarking topological sort.
/// </summary>
public class DependentSystemA : SystemBase
{
    public override void Update(float deltaTime) { }
}

/// <summary>
/// System with dependency constraint for benchmarking topological sort.
/// </summary>
public class DependentSystemB : SystemBase
{
    public override void Update(float deltaTime) { }
}

/// <summary>
/// Benchmarks for system dependency ordering (RunBefore/RunAfter with topological sort).
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class DependencyOrderingBenchmarks
{
    private World worldWithDependencies = null!;
    private World worldWithoutDependencies = null!;

    [Params(5, 10, 20)]
    public int SystemCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        worldWithDependencies = new World();
        worldWithoutDependencies = new World();

        // Create some entities
        for (var i = 0; i < 1000; i++)
        {
            worldWithDependencies.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();

            worldWithoutDependencies.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
        }

        // Add systems with dependencies (creates a chain: A -> B)
        for (var i = 0; i < SystemCount; i++)
        {
            if (i % 2 == 0)
            {
                worldWithDependencies.AddSystem(
                    new DependentSystemA(),
                    SystemPhase.Update,
                    order: 0,
                    runsBefore: [typeof(DependentSystemB)],
                    runsAfter: []);
            }
            else
            {
                worldWithDependencies.AddSystem(new DependentSystemB(), SystemPhase.Update);
            }
        }

        // Add systems without dependencies
        for (var i = 0; i < SystemCount; i++)
        {
            if (i % 2 == 0)
            {
                worldWithoutDependencies.AddSystem(new DependentSystemA(), SystemPhase.Update, order: i);
            }
            else
            {
                worldWithoutDependencies.AddSystem(new DependentSystemB(), SystemPhase.Update, order: i);
            }
        }

        // Force initial sort (includes topological sort for dependencies)
        worldWithDependencies.Update(0.016f);
        worldWithoutDependencies.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        worldWithDependencies.Dispose();
        worldWithoutDependencies.Dispose();
    }

    /// <summary>
    /// Measures update with dependency-ordered systems (topological sort).
    /// </summary>
    [Benchmark(Baseline = true)]
    public void WithDependencies()
    {
        worldWithDependencies.Update(0.016f);
    }

    /// <summary>
    /// Measures update with order-only systems (no topological sort).
    /// </summary>
    [Benchmark]
    public void WithoutDependencies()
    {
        worldWithoutDependencies.Update(0.016f);
    }
}
