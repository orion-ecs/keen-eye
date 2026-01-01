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

    #endregion
}
