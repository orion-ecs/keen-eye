using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Benchmarks for singleton operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SingletonBenchmarks
{
    private World world = null!;

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 0 });
        world.SetSingleton(new GameConfig { MaxEntities = 10000, Gravity = 9.8f });
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of getting a singleton by reference.
    /// </summary>
    [Benchmark]
    public ref GameTime GetSingleton()
    {
        return ref world.GetSingleton<GameTime>();
    }

    /// <summary>
    /// Measures the cost of getting and modifying a singleton.
    /// </summary>
    [Benchmark]
    public void GetAndModifySingleton()
    {
        ref var time = ref world.GetSingleton<GameTime>();
        time.TotalTime += time.DeltaTime;
    }

    /// <summary>
    /// Measures the cost of setting a singleton.
    /// </summary>
    [Benchmark]
    public void SetSingleton()
    {
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 100 });
    }

    /// <summary>
    /// Measures the cost of checking singleton existence.
    /// </summary>
    [Benchmark]
    public bool HasSingleton()
    {
        return world.HasSingleton<GameTime>();
    }

    /// <summary>
    /// Measures the cost of checking for non-existent singleton.
    /// </summary>
    [Benchmark]
    public bool HasSingletonMissing()
    {
        return world.HasSingleton<SmallComponent>();
    }

    /// <summary>
    /// Measures TryGetSingleton for existing singleton.
    /// </summary>
    [Benchmark]
    public bool TryGetSingletonExists()
    {
        return world.TryGetSingleton<GameTime>(out _);
    }

    /// <summary>
    /// Measures TryGetSingleton for missing singleton.
    /// </summary>
    [Benchmark]
    public bool TryGetSingletonMissing()
    {
        return world.TryGetSingleton<SmallComponent>(out _);
    }
}

/// <summary>
/// Benchmarks for singleton operations with many singletons registered.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SingletonScalingBenchmarks
{
    private World world = null!;

    // Additional singleton types for scaling tests
    private struct Singleton1 { public int Value; }
    private struct Singleton2 { public int Value; }
    private struct Singleton3 { public int Value; }
    private struct Singleton4 { public int Value; }
    private struct Singleton5 { public int Value; }
    private struct Singleton6 { public int Value; }
    private struct Singleton7 { public int Value; }
    private struct Singleton8 { public int Value; }
    private struct Singleton9 { public int Value; }
    private struct Singleton10 { public int Value; }

    [Params(1, 5, 10)]
    public int SingletonCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Register singletons based on count - use sequential registration pattern
        RegisterSingletons();

        // Always add the one we'll query
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 0 });
    }

    private void RegisterSingletons()
    {
        switch (SingletonCount)
        {
            case >= 10:
                world.SetSingleton(new Singleton10 { Value = 10 });
                goto case 9;
            case 9:
                world.SetSingleton(new Singleton9 { Value = 9 });
                goto case 8;
            case 8:
                world.SetSingleton(new Singleton8 { Value = 8 });
                goto case 7;
            case 7:
                world.SetSingleton(new Singleton7 { Value = 7 });
                goto case 6;
            case 6:
                world.SetSingleton(new Singleton6 { Value = 6 });
                goto case 5;
            case 5:
                world.SetSingleton(new Singleton5 { Value = 5 });
                goto case 4;
            case 4:
                world.SetSingleton(new Singleton4 { Value = 4 });
                goto case 3;
            case 3:
                world.SetSingleton(new Singleton3 { Value = 3 });
                goto case 2;
            case 2:
                world.SetSingleton(new Singleton2 { Value = 2 });
                goto case 1;
            case 1:
                world.SetSingleton(new Singleton1 { Value = 1 });
                break;
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures Get performance as singleton count scales.
    /// </summary>
    [Benchmark]
    public ref GameTime GetSingletonScaled()
    {
        return ref world.GetSingleton<GameTime>();
    }

    /// <summary>
    /// Measures Has performance as singleton count scales.
    /// </summary>
    [Benchmark]
    public bool HasSingletonScaled()
    {
        return world.HasSingleton<GameTime>();
    }
}

/// <summary>
/// Benchmarks for singleton add/remove operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SingletonAddRemoveBenchmarks
{
    private World world = null!;

    [IterationSetup]
    public void Setup()
    {
        world = new World();
        world.SetSingleton(new GameTime { DeltaTime = 0.016f, TotalTime = 0 });
    }

    [IterationCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of adding a new singleton.
    /// </summary>
    [Benchmark]
    public void AddNewSingleton()
    {
        world.SetSingleton(new GameConfig { MaxEntities = 10000, Gravity = 9.8f });
    }

    /// <summary>
    /// Measures the cost of removing a singleton.
    /// </summary>
    [Benchmark]
    public bool RemoveSingleton()
    {
        return world.RemoveSingleton<GameTime>();
    }
}
