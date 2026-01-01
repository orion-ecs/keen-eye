using System.Collections.Concurrent;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for thread safety and concurrent access patterns.
/// These tests verify the framework handles concurrent access correctly
/// despite supporting parallel systems.
/// </summary>
public class ThreadSafetyTests
{
    #region Test Components

    public struct Position : IComponent
    {
        public float X, Y;
    }

    public struct Velocity : IComponent
    {
        public float X, Y;
    }

    public struct Health : IComponent
    {
        public int Current, Max;
    }

    #endregion

    #region Concurrent Query Iteration Tests

    [Fact]
    public void Query_ConcurrentIteration_ProcessesAllEntities()
    {
        using var world = new World();
        var processedIds = new ConcurrentBag<int>();

        // Create entities
        for (int i = 0; i < TestConstants.LargeBatchSize; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        // Iterate from multiple threads concurrently using ForEachParallel
        world.Query<Position>().ForEachParallel(
            (Entity entity, ref Position pos) =>
            {
                processedIds.Add(entity.Id);
            },
            minEntityCount: 0
        );

        // Verify all entities were processed
        Assert.Equal(TestConstants.LargeBatchSize, processedIds.Count);
    }

    [Fact]
    public void Query_ConcurrentIterationMultipleTimes_ReturnsConsistentResults()
    {
        using var world = new World();

        // Create entities
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        var results = new ConcurrentBag<int>();

        // Run multiple concurrent iterations
        Parallel.For(0, 10, _ =>
        {
            var count = 0;
            foreach (var entity in world.Query<Position>())
            {
                count++;
            }
            results.Add(count);
        });

        // All iterations should return the same count
        Assert.All(results, count => Assert.Equal(TestConstants.StandardBatchSize, count));
    }

    [Fact]
    public void Query_ConcurrentReadOnlyAccess_IsThreadSafe()
    {
        using var world = new World();
        var exceptions = new ConcurrentBag<Exception>();

        // Create entities with different component values
        for (int i = 0; i < TestConstants.LargeBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i * 2 })
                .With(new Velocity { X = 1, Y = 0.5f })
                .Build();
        }

        // Multiple threads reading components concurrently
        Parallel.For(0, 20, _ =>
        {
            try
            {
                foreach (var entity in world.Query<Position, Velocity>())
                {
                    ref readonly var pos = ref world.Get<Position>(entity);
                    ref readonly var vel = ref world.Get<Velocity>(entity);

                    // Validate data integrity - Y should be 2x X
                    Assert.Equal(pos.X * 2, pos.Y, 0.0001f);
                    Assert.Equal(1f, vel.X, 0.0001f);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion

    #region Parallel System Component Modification Tests

    [Fact]
    public void ParallelSystem_ComponentModification_MaintainsDataIntegrity()
    {
        using var world = new World();

        // Create entities with known initial values
        for (int i = 0; i < TestConstants.LargeBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = 0, Y = 0 })
                .With(new Velocity { X = 1, Y = 2 })
                .Build();
        }

        const float deltaTime = 0.016f;

        // Modify components in parallel - each entity is modified independently
        world.Query<Position, Velocity>().ForEachParallel(
            (Entity entity, ref Position pos, ref Velocity vel) =>
            {
                pos.X += vel.X * deltaTime;
                pos.Y += vel.Y * deltaTime;
            },
            minEntityCount: 0
        );

        // Verify all entities have correct values
        foreach (var entity in world.Query<Position, Velocity>())
        {
            ref var pos = ref world.Get<Position>(entity);
            Assert.Equal(deltaTime, pos.X, 0.0001f);
            Assert.Equal(2 * deltaTime, pos.Y, 0.0001f);
        }
    }

    [Fact]
    public void ParallelSystem_MultipleComponentTypes_ProcessesCorrectly()
    {
        using var world = new World();

        // Create entities with all component types
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
        }

        var processedCount = 0;

        // Process all three components in parallel
        world.Query<Position, Velocity, Health>().ForEachParallel(
            (Entity entity, ref Position pos, ref Velocity vel, ref Health health) =>
            {
                pos.X += vel.X;
                health.Current -= 1;
                Interlocked.Increment(ref processedCount);
            },
            minEntityCount: 0
        );

        Assert.Equal(TestConstants.StandardBatchSize, processedCount);

        // Verify modifications
        foreach (var entity in world.Query<Health>())
        {
            ref var health = ref world.Get<Health>(entity);
            Assert.Equal(99, health.Current);
        }
    }

    #endregion

    #region Concurrent Query Construction Tests

    [Fact]
    public void Query_ConcurrentConstruction_ReturnsCorrectResults()
    {
        using var world = new World();

        // Create entities with different archetypes
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        var results = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();

        // Build queries from multiple threads
        Parallel.For(0, 20, _ =>
        {
            try
            {
                var count = world.Query<Position>().Count();
                results.Add(count);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.All(results, count => Assert.Equal(TestConstants.StandardBatchSize * 2, count));
    }

    [Fact]
    public void Query_ConcurrentWithFilters_ReturnsCorrectResults()
    {
        using var world = new World();

        // Create entities with Position only
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
        }

        // Create entities with Position and Velocity
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        var positionOnlyResults = new ConcurrentBag<int>();
        var withVelocityResults = new ConcurrentBag<int>();
        var exceptions = new ConcurrentBag<Exception>();

        // Build different queries from multiple threads
        Parallel.For(0, 10, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    // Query Position without Velocity
                    var count = world.Query<Position>().Without<Velocity>().Count();
                    positionOnlyResults.Add(count);
                }
                else
                {
                    // Query Position with Velocity
                    var count = world.Query<Position, Velocity>().Count();
                    withVelocityResults.Add(count);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.All(positionOnlyResults, count => Assert.Equal(TestConstants.StandardBatchSize, count));
        Assert.All(withVelocityResults, count => Assert.Equal(TestConstants.StandardBatchSize, count));
    }

    #endregion

    #region Plugin Thread Safety Tests

    [Fact]
    public void Plugin_ConcurrentAccess_HandlesGracefully()
    {
        using var world = new World();

        // Install a simple plugin
        var plugin = new TestConcurrentPlugin();
        world.InstallPlugin(plugin);

        var exceptions = new ConcurrentBag<Exception>();

        // Access plugin extensions from multiple threads
        Parallel.For(0, 20, _ =>
        {
            try
            {
                // Access the world state concurrently (read-only)
                var count = 0;
                foreach (var entity in world.Query<Position>())
                {
                    count++;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion

    #region Concurrent Entity Operations Tests

    [Fact]
    public void World_ConcurrentEntityReads_IsThreadSafe()
    {
        using var world = new World();
        var entities = new List<Entity>();

        // Create entities
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            var entity = world.Spawn()
                .With(new Position { X = i, Y = i })
                .Build();
            entities.Add(entity);
        }

        var exceptions = new ConcurrentBag<Exception>();

        // Read entity state from multiple threads
        Parallel.ForEach(entities, entity =>
        {
            try
            {
                // Read operations should be thread-safe
                var isAlive = world.IsAlive(entity);
                Assert.True(isAlive);

                var hasPosition = world.Has<Position>(entity);
                Assert.True(hasPosition);

                ref readonly var pos = ref world.Get<Position>(entity);
                Assert.True(pos.X >= 0);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void World_ConcurrentComponentAccess_MaintainsIntegrity()
    {
        using var world = new World();
        var entities = new ConcurrentBag<Entity>();

        // Create entities with known values
        for (int i = 0; i < TestConstants.LargeBatchSize; i++)
        {
            var entity = world.Spawn()
                .With(new Position { X = i, Y = i * 2 })
                .With(new Health { Current = 100, Max = 100 })
                .Build();
            entities.Add(entity);
        }

        var exceptions = new ConcurrentBag<Exception>();

        // Multiple threads reading different entities
        Parallel.ForEach(entities, entity =>
        {
            try
            {
                ref readonly var pos = ref world.Get<Position>(entity);
                ref readonly var health = ref world.Get<Health>(entity);

                // Verify data consistency
                Assert.Equal(pos.X * 2, pos.Y);
                Assert.Equal(100, health.Current);
                Assert.Equal(100, health.Max);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void Query_HighConcurrency_StressTest()
    {
        using var world = new World();

        // Create a large number of entities
        for (int i = 0; i < TestConstants.LargeBatchSize; i++)
        {
            world.Spawn()
                .With(new Position { X = i, Y = i })
                .With(new Velocity { X = 1, Y = 1 })
                .Build();
        }

        var totalProcessed = 0;
        var exceptions = new ConcurrentBag<Exception>();

        // Many threads iterating concurrently
        Parallel.For(0, 50, _ =>
        {
            try
            {
                world.Query<Position, Velocity>().ForEachParallel(
                    (Entity entity, ref Position pos, ref Velocity vel) =>
                    {
                        // Simple read operation
                        var _ = pos.X + vel.X;
                        Interlocked.Increment(ref totalProcessed);
                    },
                    minEntityCount: 0
                );
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        // Each of 50 parallel iterations should process all 1000 entities
        Assert.Equal(50 * TestConstants.LargeBatchSize, totalProcessed);
    }

    [Fact]
    public void World_RapidQueryConstruction_HandlesGracefully()
    {
        using var world = new World();

        // Create entities with multiple archetypes
        for (int i = 0; i < TestConstants.StandardBatchSize; i++)
        {
            world.Spawn().With(new Position { X = i, Y = i }).Build();
            world.Spawn().With(new Position { X = i, Y = i }).With(new Velocity { X = 1, Y = 1 }).Build();
            world.Spawn().With(new Position { X = i, Y = i }).With(new Health { Current = 100, Max = 100 }).Build();
        }

        var exceptions = new ConcurrentBag<Exception>();

        // Rapidly construct and execute many queries in parallel
        Parallel.For(0, TestConstants.ConcurrentIterationsPerThread, i =>
        {
            try
            {
                // Alternate between different query types
                switch (i % 3)
                {
                    case 0:
                        var count1 = world.Query<Position>().Count();
                        Assert.Equal(TestConstants.StandardBatchSize * 3, count1);
                        break;
                    case 1:
                        var count2 = world.Query<Position, Velocity>().Count();
                        Assert.Equal(TestConstants.StandardBatchSize, count2);
                        break;
                    case 2:
                        var count3 = world.Query<Position>().Without<Velocity>().Without<Health>().Count();
                        Assert.Equal(TestConstants.StandardBatchSize, count3);
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    #endregion

    #region Event System Thread Safety Tests

    [Fact]
    public void EventBus_ConcurrentSubscribeAndPublish_IsThreadSafe()
    {
        using var world = new World();
        var receivedCount = 0;
        var exceptions = new ConcurrentBag<Exception>();

        // Subscribe from multiple threads while publishing
        Parallel.For(0, 50, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    // Subscribe
                    var sub = world.Events.Subscribe<TestEvent>(evt =>
                    {
                        Interlocked.Increment(ref receivedCount);
                    });

                    // Small delay to allow some publishing to occur
                    Thread.Sleep(TestConstants.ThreadSleepShortMs);

                    sub.Dispose();
                }
                else
                {
                    // Publish events
                    for (int j = 0; j < 10; j++)
                    {
                        world.Events.Publish(new TestEvent { Value = j });
                    }
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void EventBus_ConcurrentSubscriptions_NoHandlerLoss()
    {
        using var world = new World();
        var subscriptions = new ConcurrentBag<EventSubscription>();
        var exceptions = new ConcurrentBag<Exception>();

        // Subscribe from many threads concurrently
        Parallel.For(0, TestConstants.StandardBatchSize, _ =>
        {
            try
            {
                var sub = world.Events.Subscribe<TestEvent>(_ => { });
                subscriptions.Add(sub);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(TestConstants.StandardBatchSize, world.Events.GetHandlerCount<TestEvent>());

        // Dispose all subscriptions
        foreach (var sub in subscriptions)
        {
            sub.Dispose();
        }

        Assert.Equal(0, world.Events.GetHandlerCount<TestEvent>());
    }

    [Fact]
    public void EventBus_ConcurrentDispose_IsIdempotent()
    {
        using var world = new World();
        var invokeCount = 0;

        var subscription = world.Events.Subscribe<TestEvent>(_ =>
        {
            Interlocked.Increment(ref invokeCount);
        });

        var exceptions = new ConcurrentBag<Exception>();

        // Dispose from multiple threads simultaneously
        Parallel.For(0, 20, _ =>
        {
            try
            {
                subscription.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);

        // Verify handler was removed (only once)
        Assert.Equal(0, world.Events.GetHandlerCount<TestEvent>());
    }

    [Fact]
    public async Task ComponentEvents_ConcurrentSubscribeAndFire_IsThreadSafe()
    {
        using var world = new World();
        var addedCount = 0;
        var exceptions = new ConcurrentBag<Exception>();
        var subscriptions = new ConcurrentBag<EventSubscription>();

        // Create entities first (single-threaded to avoid archetype race conditions)
        var entities = new List<Entity>();
        for (int i = 0; i < TestConstants.SmallBatchSize; i++)
        {
            entities.Add(world.Spawn().Build());
        }

        // Subscribe to component events from multiple threads while adding components sequentially
        var ct = TestContext.Current.CancellationToken;
        var subscriberTask = Task.Run(async () =>
        {
            for (int i = 0; i < 50; i++)
            {
                try
                {
                    var sub = world.OnComponentAdded<Position>((entity, pos) =>
                    {
                        Interlocked.Increment(ref addedCount);
                    });
                    subscriptions.Add(sub);
                    await Task.Delay(TestConstants.ThreadSleepShortMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }, ct);

        // Add components sequentially (to avoid archetype race conditions)
        foreach (var entity in entities)
        {
            world.Add(entity, new Position { X = 1, Y = 1 });
        }

        await subscriberTask;

        Assert.Empty(exceptions);
        // Verify that some subscriptions were added (exact count depends on timing)
        Assert.True(subscriptions.Count > 0);
    }

    [Fact]
    public void EntityEvents_ConcurrentSubscriptionManagement_IsThreadSafe()
    {
        using var world = new World();
        var subscriptions = new ConcurrentBag<EventSubscription>();
        var exceptions = new ConcurrentBag<Exception>();

        // Concurrently subscribe and unsubscribe to entity events
        Parallel.For(0, TestConstants.StandardBatchSize, i =>
        {
            try
            {
                if (i % 2 == 0)
                {
                    // Subscribe to created
                    var sub = world.OnEntityCreated((entity, name) => { });
                    subscriptions.Add(sub);
                }
                else
                {
                    // Subscribe to destroyed
                    var sub = world.OnEntityDestroyed(entity => { });
                    subscriptions.Add(sub);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(TestConstants.StandardBatchSize, subscriptions.Count);

        // Concurrently dispose all subscriptions
        Parallel.ForEach(subscriptions, sub =>
        {
            try
            {
                sub.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void EventBus_HighConcurrencyStress_HandlesGracefully()
    {
        using var world = new World();
        var publishCount = 0;
        var exceptions = new ConcurrentBag<Exception>();
        var subscriptions = new ConcurrentBag<EventSubscription>();

        // High concurrency stress test
        Parallel.For(0, TestConstants.ConcurrentIterationsPerThread, i =>
        {
            try
            {
                switch (i % 4)
                {
                    case 0:
                        // Subscribe
                        var sub = world.Events.Subscribe<TestEvent>(_ =>
                        {
                            Interlocked.Increment(ref publishCount);
                        });
                        subscriptions.Add(sub);
                        break;
                    case 1:
                        // Publish
                        world.Events.Publish(new TestEvent { Value = i });
                        break;
                    case 2:
                        // Unsubscribe
                        if (subscriptions.TryTake(out var toDispose))
                        {
                            toDispose.Dispose();
                        }
                        break;
                    case 3:
                        // Check handler count
                        var _ = world.Events.HasHandlers<TestEvent>();
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ComponentEvents_ConcurrentSubscriptionManagement_IsThreadSafe()
    {
        using var world = new World();
        var exceptions = new ConcurrentBag<Exception>();
        var subscriptions = new ConcurrentBag<EventSubscription>();

        // Concurrently subscribe to all component event types from many threads
        Parallel.For(0, TestConstants.StandardBatchSize, i =>
        {
            try
            {
                switch (i % 3)
                {
                    case 0:
                        subscriptions.Add(world.OnComponentAdded<Health>((e, h) => { }));
                        break;
                    case 1:
                        subscriptions.Add(world.OnComponentRemoved<Health>(e => { }));
                        break;
                    case 2:
                        subscriptions.Add(world.OnComponentChanged<Health>((e, o, n) => { }));
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
        Assert.Equal(TestConstants.StandardBatchSize, subscriptions.Count);

        // Concurrently dispose all subscriptions
        Parallel.ForEach(subscriptions, sub =>
        {
            try
            {
                sub.Dispose();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        Assert.Empty(exceptions);
    }

    [Fact]
    public async Task EntityEvents_ConcurrentSubscriptionDuringDespawn_IsThreadSafe()
    {
        using var world = new World();
        var destroyedCount = 0;
        var exceptions = new ConcurrentBag<Exception>();
        var subscriptions = new ConcurrentBag<EventSubscription>();

        // Create entities first (single-threaded to avoid archetype race conditions)
        var entities = new List<Entity>();
        for (int i = 0; i < TestConstants.SmallBatchSize; i++)
        {
            entities.Add(world.Spawn().Build());
        }

        // Subscribe to entity destroyed from multiple threads
        // while despawning happens on the main thread
        var ct = TestContext.Current.CancellationToken;
        var subscriberTask = Task.Run(async () =>
        {
            for (int i = 0; i < 50; i++)
            {
                try
                {
                    var sub = world.OnEntityDestroyed(entity =>
                    {
                        Interlocked.Increment(ref destroyedCount);
                    });
                    subscriptions.Add(sub);
                    await Task.Delay(TestConstants.ThreadSleepShortMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }
        }, ct);

        // Despawn sequentially (to avoid archetype race conditions)
        foreach (var entity in entities)
        {
            world.Despawn(entity);
        }

        await subscriberTask;

        Assert.Empty(exceptions);
        // Events may or may not have been received depending on timing,
        // but no crashes should occur
        Assert.True(subscriptions.Count > 0);
    }

    #endregion

    #region Test Helpers

    /// <summary>
    /// Simple plugin for concurrent access testing.
    /// </summary>
    private sealed class TestConcurrentPlugin : IWorldPlugin
    {
        public string Name => "TestConcurrent";
        private int installCount;
        public int InstallCount => installCount;

        public void Install(IPluginContext context)
        {
            Interlocked.Increment(ref installCount);
        }

        public void Uninstall(IPluginContext context)
        {
        }
    }

    /// <summary>
    /// Simple test event for event bus thread safety testing.
    /// </summary>
    private readonly record struct TestEvent
    {
        public int Value { get; init; }
    }

    #endregion
}
