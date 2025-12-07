using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for change tracking operations: marking dirty, querying, and clearing.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ChangeTrackingBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [Params(10, 50, 100)]
    public int DirtyPercentage { get; set; }

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

        // Mark some entities as dirty based on percentage
        var dirtyCount = EntityCount * DirtyPercentage / 100;
        for (var i = 0; i < dirtyCount; i++)
        {
            world.MarkDirty<Position>(entities[i]);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of marking a single entity as dirty.
    /// </summary>
    [Benchmark]
    public void MarkDirty()
    {
        // Mark an entity that isn't already dirty
        var index = EntityCount * DirtyPercentage / 100;
        if (index < EntityCount)
        {
            world.MarkDirty<Position>(entities[index]);
        }
    }

    /// <summary>
    /// Measures the cost of checking if an entity is dirty.
    /// </summary>
    [Benchmark]
    public bool IsDirty()
    {
        return world.IsDirty<Position>(entities[0]);
    }

    /// <summary>
    /// Measures the cost of getting dirty entity count.
    /// </summary>
    [Benchmark]
    public int GetDirtyCount()
    {
        return world.GetDirtyCount<Position>();
    }

    /// <summary>
    /// Measures the cost of iterating dirty entities.
    /// </summary>
    [Benchmark]
    public int IterateDirtyEntities()
    {
        var count = 0;
        foreach (var _ in world.GetDirtyEntities<Position>())
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures the cost of clearing dirty flags.
    /// </summary>
    [Benchmark]
    [IterationSetup(Target = nameof(ClearDirtyFlags))]
    public void ClearDirtyFlags()
    {
        world.ClearDirtyFlags<Position>();
    }

    [IterationCleanup(Target = nameof(ClearDirtyFlags))]
    public void RestoreDirtyFlags()
    {
        // Re-mark entities as dirty for next iteration
        var dirtyCount = EntityCount * DirtyPercentage / 100;
        for (var i = 0; i < dirtyCount; i++)
        {
            world.MarkDirty<Position>(entities[i]);
        }
    }
}

/// <summary>
/// Benchmarks for automatic change tracking via Set().
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class AutoTrackingBenchmarks
{
    private World world = null!;
    private Entity entity = default;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entity = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of Set() without auto-tracking.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void SetWithoutAutoTracking()
    {
        world.DisableAutoTracking<Position>();
        world.Set(entity, new Position { X = 100, Y = 200 });
    }

    /// <summary>
    /// Measures the cost of Set() with auto-tracking enabled.
    /// </summary>
    [Benchmark]
    public void SetWithAutoTracking()
    {
        world.EnableAutoTracking<Position>();
        world.Set(entity, new Position { X = 100, Y = 200 });
        world.ClearDirtyFlags<Position>();
    }

    /// <summary>
    /// Measures the cost of enabling auto-tracking.
    /// </summary>
    [Benchmark]
    public void EnableAutoTracking()
    {
        world.EnableAutoTracking<Position>();
    }

    /// <summary>
    /// Measures the cost of disabling auto-tracking.
    /// </summary>
    [Benchmark]
    public void DisableAutoTracking()
    {
        world.DisableAutoTracking<Position>();
    }

    /// <summary>
    /// Measures the cost of checking auto-tracking status.
    /// </summary>
    [Benchmark]
    public bool IsAutoTrackingEnabled()
    {
        return world.IsAutoTrackingEnabled<Position>();
    }
}

/// <summary>
/// Benchmarks comparing manual marking vs ref modification patterns.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ChangeTrackingPatternBenchmarks
{
    private World world = null!;
    private Entity[] entities = null!;

    [Params(1000)]
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
    /// Measures ref modification with manual MarkDirty.
    /// </summary>
    [Benchmark(Baseline = true)]
    public void RefWithManualMark()
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            pos.Y += vel.Y;
            world.MarkDirty<Position>(entity);
        }
        world.ClearDirtyFlags<Position>();
    }

    /// <summary>
    /// Measures Set() with auto-tracking enabled.
    /// </summary>
    [Benchmark]
    public void SetWithAutoTrack()
    {
        world.EnableAutoTracking<Position>();
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref readonly var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            world.Set(entity, new Position { X = pos.X + vel.X, Y = pos.Y + vel.Y });
        }
        world.ClearDirtyFlags<Position>();
    }

    /// <summary>
    /// Measures ref modification without any tracking (baseline comparison).
    /// </summary>
    [Benchmark]
    public void RefNoTracking()
    {
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            pos.Y += vel.Y;
        }
    }
}
