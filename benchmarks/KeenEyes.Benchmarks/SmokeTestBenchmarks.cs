using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Fast smoke test benchmarks for PR regression checks.
/// These benchmarks use fixed small parameters to run quickly (~1-2 min total)
/// while still covering the critical code paths.
/// </summary>
/// <remarks>
/// Run with: dotnet run -c Release -- --filter *SmokeTest*
/// </remarks>
[MemoryDiagnoser]
[ShortRunJob]
public class SmokeTestBenchmarks
{
    private const int EntityCount = 100;

    private World world = null!;
    private Entity[] entities = null!;
    private Entity singleEntity;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Pre-create entities for tests
        entities = new Entity[EntityCount];
        for (var i = 0; i < EntityCount; i++)
        {
            entities[i] = world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 0 })
                .Build();
        }

        singleEntity = entities[0];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    // === Entity Operations ===

    /// <summary>
    /// Spawn entity with components - most common creation pattern.
    /// </summary>
    [Benchmark]
    public Entity Entity_SpawnWithComponents()
    {
        return world.Spawn()
            .With(new Position { X = 1, Y = 2 })
            .With(new Velocity { X = 0.5f, Y = 0.5f })
            .Build();
    }

    /// <summary>
    /// Check if entity is alive - common validation.
    /// </summary>
    [Benchmark]
    public bool Entity_IsAlive()
    {
        return world.IsAlive(singleEntity);
    }

    // === Component Operations ===

    /// <summary>
    /// Get component by ref - critical hot path.
    /// </summary>
    [Benchmark]
    public ref Position Component_GetByRef()
    {
        return ref world.Get<Position>(singleEntity);
    }

    /// <summary>
    /// Has component check - common filtering operation.
    /// </summary>
    [Benchmark]
    public bool Component_Has()
    {
        return world.Has<Position>(singleEntity);
    }

    /// <summary>
    /// Set/replace component value.
    /// </summary>
    [Benchmark]
    public void Component_Set()
    {
        world.Set(singleEntity, new Position { X = 99, Y = 99 });
    }

    // === Query Operations ===

    /// <summary>
    /// Single component query iteration - baseline query performance.
    /// </summary>
    [Benchmark]
    public int Query_SingleComponent()
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
    /// Two component query - common game loop pattern.
    /// </summary>
    [Benchmark]
    public int Query_TwoComponents()
    {
        var count = 0;
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            ref readonly var vel = ref world.Get<Velocity>(entity);
            pos.X += vel.X;
            pos.Y += vel.Y;
            count++;
        }
        return count;
    }

    /// <summary>
    /// Query construction overhead (no iteration).
    /// </summary>
    [Benchmark]
    public QueryBuilder<Position, Velocity> Query_Construction()
    {
        return world.Query<Position, Velocity>();
    }

    // === Singleton Operations ===

    /// <summary>
    /// Get singleton - global game state access pattern.
    /// </summary>
    [Benchmark]
    public ref GameTime Singleton_Get()
    {
        return ref world.GetSingleton<GameTime>();
    }

    [IterationSetup(Target = nameof(Singleton_Get))]
    public void SetupSingleton()
    {
        if (!world.HasSingleton<GameTime>())
        {
            world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 0 });
        }
    }
}
