using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Custom event for benchmarking.
/// </summary>
public readonly record struct DamageEvent(int EntityId, int Amount);

/// <summary>
/// Benchmarks for the event system: subscription, publishing, and lifecycle events.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EventBenchmarks
{
    private World world = null!;
    private Entity entity = default;

    [Params(0, 1, 10)]
    public int HandlerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entity = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .With(new Health { Current = 100, Max = 100 })
            .Build();

        // Subscribe handlers based on parameter
        for (var i = 0; i < HandlerCount; i++)
        {
            world.Events.Subscribe<DamageEvent>(_ => { });
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of subscribing to an event type.
    /// </summary>
    [Benchmark]
    public EventSubscription Subscribe()
    {
        var sub = world.Events.Subscribe<DamageEvent>(_ => { });
        sub.Dispose();
        return sub;
    }

    /// <summary>
    /// Measures the cost of publishing an event with registered handlers.
    /// </summary>
    [Benchmark]
    public void PublishEvent()
    {
        world.Events.Publish(new DamageEvent(entity.Id, 25));
    }

    /// <summary>
    /// Measures the cost of checking if handlers exist.
    /// </summary>
    [Benchmark]
    public bool HasHandlers()
    {
        return world.Events.HasHandlers<DamageEvent>();
    }

    /// <summary>
    /// Measures the cost of getting handler count.
    /// </summary>
    [Benchmark]
    public int GetHandlerCount()
    {
        return world.Events.GetHandlerCount<DamageEvent>();
    }
}

/// <summary>
/// Benchmarks for component lifecycle events.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class ComponentEventBenchmarks
{
    private World world = null!;
    private Entity entity = default;
    private readonly List<EventSubscription> subscriptions = [];

    [Params(0, 1, 5)]
    public int HandlerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        entity = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();

        // Subscribe handlers (minimal work to measure event overhead)
        for (var i = 0; i < HandlerCount; i++)
        {
            subscriptions.Add(world.OnComponentAdded<Health>((_, _) => { }));
            subscriptions.Add(world.OnComponentRemoved<Health>(_ => { }));
            subscriptions.Add(world.OnComponentChanged<Health>((_, _, _) => { }));
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }
        subscriptions.Clear();
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of adding a component (triggers OnComponentAdded).
    /// </summary>
    [Benchmark]
    public void AddComponentWithEvent()
    {
        world.Add(entity, new Health { Current = 100, Max = 100 });
        world.Remove<Health>(entity);
    }

    /// <summary>
    /// Measures the cost of changing a component (triggers OnComponentChanged).
    /// </summary>
    [Benchmark]
    public void SetComponentWithEvent()
    {
        world.Add(entity, new Health { Current = 100, Max = 100 });
        world.Set(entity, new Health { Current = 50, Max = 100 });
        world.Remove<Health>(entity);
    }

    /// <summary>
    /// Measures the cost of subscribing to component added events.
    /// </summary>
    [Benchmark]
    public EventSubscription SubscribeComponentAdded()
    {
        var sub = world.OnComponentAdded<Position>((_, _) => { });
        sub.Dispose();
        return sub;
    }
}

/// <summary>
/// Benchmarks for entity lifecycle events.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class EntityEventBenchmarks
{
    private World world = null!;
    private readonly List<EventSubscription> subscriptions = [];

    [Params(0, 1, 5)]
    public int HandlerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Subscribe handlers (minimal work to measure event overhead)
        for (var i = 0; i < HandlerCount; i++)
        {
            subscriptions.Add(world.OnEntityCreated((_, _) => { }));
            subscriptions.Add(world.OnEntityDestroyed(_ => { }));
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }
        subscriptions.Clear();
        world.Dispose();
    }

    /// <summary>
    /// Measures the cost of spawning an entity (triggers OnEntityCreated).
    /// </summary>
    [Benchmark]
    public Entity SpawnWithEvent()
    {
        var e = world.Spawn().Build();
        world.Despawn(e);
        return e;
    }

    /// <summary>
    /// Measures the cost of subscribing to entity created events.
    /// </summary>
    [Benchmark]
    public EventSubscription SubscribeEntityCreated()
    {
        var sub = world.OnEntityCreated((_, _) => { });
        sub.Dispose();
        return sub;
    }

    /// <summary>
    /// Measures the cost of subscribing to entity destroyed events.
    /// </summary>
    [Benchmark]
    public EventSubscription SubscribeEntityDestroyed()
    {
        var sub = world.OnEntityDestroyed(_ => { });
        sub.Dispose();
        return sub;
    }
}
