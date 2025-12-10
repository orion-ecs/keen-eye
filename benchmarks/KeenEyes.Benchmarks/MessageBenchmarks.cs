using BenchmarkDotNet.Attributes;

namespace KeenEyes.Benchmarks;

/// <summary>
/// Message type for benchmarking immediate delivery.
/// </summary>
public readonly record struct BenchmarkDamageMessage(int TargetId, int Amount);

/// <summary>
/// Message type for benchmarking queued delivery.
/// </summary>
public readonly record struct BenchmarkCollisionMessage(int Entity1, int Entity2, float Depth);

/// <summary>
/// Benchmarks for the inter-system messaging feature: subscription, immediate send, and queued delivery.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class MessageBenchmarks
{
    private World world = null!;
    private readonly List<EventSubscription> subscriptions = [];

    [Params(0, 1, 10)]
    public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Subscribe handlers based on parameter
        for (var i = 0; i < SubscriberCount; i++)
        {
            subscriptions.Add(world.Subscribe<BenchmarkDamageMessage>(_ => { }));
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
    /// Measures the cost of subscribing to a message type.
    /// </summary>
    [Benchmark]
    public EventSubscription Subscribe()
    {
        var sub = world.Subscribe<BenchmarkDamageMessage>(_ => { });
        sub.Dispose();
        return sub;
    }

    /// <summary>
    /// Measures the cost of sending a message immediately with registered handlers.
    /// </summary>
    [Benchmark]
    public void SendImmediate()
    {
        world.Send(new BenchmarkDamageMessage(42, 25));
    }

    /// <summary>
    /// Measures the cost of checking if subscribers exist (for optimization patterns).
    /// </summary>
    [Benchmark]
    public bool HasSubscribers()
    {
        return world.HasMessageSubscribers<BenchmarkDamageMessage>();
    }

    /// <summary>
    /// Measures the cost of getting subscriber count.
    /// </summary>
    [Benchmark]
    public int GetSubscriberCount()
    {
        return world.GetMessageSubscriberCount<BenchmarkDamageMessage>();
    }
}

/// <summary>
/// Benchmarks for message queuing and deferred delivery.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class MessageQueueBenchmarks
{
    private World world = null!;
    private readonly List<EventSubscription> subscriptions = [];

    [Params(1, 10, 100)]
    public int MessageCount { get; set; }

    [Params(1, 5)]
    public int SubscriberCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Subscribe handlers
        for (var i = 0; i < SubscriberCount; i++)
        {
            subscriptions.Add(world.Subscribe<BenchmarkDamageMessage>(_ => { }));
            subscriptions.Add(world.Subscribe<BenchmarkCollisionMessage>(_ => { }));
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
    /// Measures the cost of queuing a message without delivery.
    /// </summary>
    [Benchmark]
    public void QueueMessage()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
        }
        world.ClearQueuedMessages(); // Clear without processing
    }

    /// <summary>
    /// Measures the cost of queuing and then processing all messages.
    /// </summary>
    [Benchmark]
    public void QueueAndProcess()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
        }
        world.ProcessQueuedMessages();
    }

    /// <summary>
    /// Measures the cost of processing a specific message type only.
    /// </summary>
    [Benchmark]
    public void QueueAndProcessTyped()
    {
        // Queue both message types
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
            world.QueueMessage(new BenchmarkCollisionMessage(i, i + 1, 0.5f));
        }

        // Process only damage messages
        world.ProcessQueuedMessages<BenchmarkDamageMessage>();

        // Clear remaining
        world.ClearQueuedMessages();
    }

    /// <summary>
    /// Measures the cost of checking queued message count.
    /// </summary>
    [Benchmark]
    public int GetQueuedCount()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
        }
        var count = world.GetQueuedMessageCount<BenchmarkDamageMessage>();
        world.ClearQueuedMessages();
        return count;
    }

    /// <summary>
    /// Measures the cost of getting total queued count across all types.
    /// </summary>
    [Benchmark]
    public int GetTotalQueuedCount()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
            world.QueueMessage(new BenchmarkCollisionMessage(i, i + 1, 0.5f));
        }
        var count = world.GetTotalQueuedMessageCount();
        world.ClearQueuedMessages();
        return count;
    }
}

/// <summary>
/// Benchmarks comparing immediate vs queued delivery patterns.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class MessageDeliveryPatternBenchmarks
{
    private World world = null!;
    private readonly List<EventSubscription> subscriptions = [];
    private int processedCount;

    [Params(10, 100, 1000)]
    public int MessageCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        processedCount = 0;

        // Single subscriber that increments counter
        subscriptions.Add(world.Subscribe<BenchmarkDamageMessage>(_ => processedCount++));
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

    [IterationSetup]
    public void IterationSetup()
    {
        processedCount = 0;
    }

    /// <summary>
    /// Measures immediate delivery pattern - send each message immediately.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ImmediateDelivery()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.Send(new BenchmarkDamageMessage(i, 10));
        }
        return processedCount;
    }

    /// <summary>
    /// Measures deferred delivery pattern - queue all, then process.
    /// </summary>
    [Benchmark]
    public int DeferredDelivery()
    {
        for (var i = 0; i < MessageCount; i++)
        {
            world.QueueMessage(new BenchmarkDamageMessage(i, 10));
        }
        world.ProcessQueuedMessages();
        return processedCount;
    }

    /// <summary>
    /// Measures optimized pattern - check subscribers before sending.
    /// </summary>
    [Benchmark]
    public int OptimizedDelivery()
    {
        if (world.HasMessageSubscribers<BenchmarkDamageMessage>())
        {
            for (var i = 0; i < MessageCount; i++)
            {
                world.Send(new BenchmarkDamageMessage(i, 10));
            }
        }
        return processedCount;
    }
}
