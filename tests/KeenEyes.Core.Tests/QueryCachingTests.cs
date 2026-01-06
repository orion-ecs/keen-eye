using EnemyTag = KeenEyes.Tests.TestEnemyTag;
using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the query caching system.
/// </summary>
[Collection("ThreadTests")]
public class QueryCachingTests
{
    #region QueryDescriptor Tests

    [Fact]
    public void QueryDescriptor_SameTypes_Equal()
    {
        var desc1 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity), typeof(Position)], []);

        Assert.Equal(desc1, desc2);
        Assert.Equal(desc1.GetHashCode(), desc2.GetHashCode());
    }

    [Fact]
    public void QueryDescriptor_DifferentTypes_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_WithVsWithout_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([], [typeof(Position)]);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_SameWithAndWithout_Equal()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        Assert.Equal(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_FromDescription_CreatesCorrectDescriptor()
    {
        var description = new QueryDescription();
        description.AddWrite<Position>();
        description.AddWith<Velocity>();
        description.AddWithout<Health>();

        var descriptor = QueryDescriptor.FromDescription(description);

        Assert.Contains(typeof(Position), descriptor.With.ToList());
        Assert.Contains(typeof(Velocity), descriptor.With.ToList());
        Assert.Contains(typeof(Health), descriptor.Without.ToList());
    }

    [Fact]
    public void QueryDescriptor_ToString_ReturnsReadableFormat()
    {
        var desc = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var str = desc.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Without", str);
        Assert.Contains("Velocity", str);
    }

    #endregion

    #region QueryManager Tests

    [Fact]
    public void QueryManager_CachesQueryResults()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create an archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query - cache miss
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);
        Assert.Equal(0, queryManager.CacheHits);

        // Second query - cache hit
        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);
        Assert.Equal(1, queryManager.CacheHits);
    }

    [Fact]
    public void QueryManager_IncrementallyUpdatesCache()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Create first archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        // Query - caches result with 1 archetype
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Create second matching archetype
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Same query - should include new archetype (incremental update)
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, result2.Count);
    }

    [Fact]
    public void QueryManager_InvalidateCache_ClearsAll()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CachedQueryCount);

        queryManager.InvalidateCache();
        Assert.Equal(0, queryManager.CachedQueryCount);
    }

    [Fact]
    public void QueryManager_ResetStatistics_ClearsCounters()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);

        Assert.True(queryManager.CacheHits > 0 || queryManager.CacheMisses > 0);

        queryManager.ResetStatistics();

        Assert.Equal(0, queryManager.CacheHits);
        Assert.Equal(0, queryManager.CacheMisses);
    }

    [Fact]
    public void QueryManager_HitRate_CalculatesCorrectly()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // 1 miss
        queryManager.GetMatchingArchetypes(description);

        // 3 hits
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);
        queryManager.GetMatchingArchetypes(description);

        // Hit rate = 3 / 4 * 100 = 75%
        Assert.Equal(75.0, queryManager.HitRate);
    }

    #endregion

    #region World Query Caching Integration Tests

    [Fact]
    public void World_QueryCaching_ImprovesCacheHitRate()
    {
        using var world = new World();

        // Create some entities
        for (var i = 0; i < 10; i++)
        {
            world.Spawn().With(new Position { X = i }).Build();
        }

        // Run same query multiple times
        for (var i = 0; i < 10; i++)
        {
            foreach (var entity in world.Query<Position>())
            {
                _ = world.Get<Position>(entity);
            }
        }

        var stats = world.GetMemoryStats();

        // Should have good cache hit rate after multiple queries
        Assert.True(stats.QueryCacheHitRate > 50.0);
    }

    [Fact]
    public void World_DifferentQueries_CachedSeparately()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Run different queries
        var posQuery = world.Query<Position>().ToList();
        var velQuery = world.Query<Velocity>().ToList();
        var bothQuery = world.Query<Position, Velocity>().ToList();

        var stats = world.GetMemoryStats();

        // Should have at least 3 cached queries
        Assert.True(stats.CachedQueryCount >= 3);

        // Verify query results are correct
        Assert.Equal(2, posQuery.Count);
        Assert.Equal(2, velQuery.Count);
        Assert.Single(bothQuery);
    }

    [Fact]
    public void World_QueryWithFilter_CachedCorrectly()
    {
        using var world = new World();

        // Create entities with different component combinations
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Health()).Build();

        // Query Position without Velocity
        var filtered = world.Query<Position>().Without<Velocity>().ToList();

        Assert.Equal(2, filtered.Count);

        // Run same query again - should hit cache
        var filtered2 = world.Query<Position>().Without<Velocity>().ToList();
        Assert.Equal(2, filtered2.Count);

        var stats = world.GetMemoryStats();
        Assert.True(stats.QueryCacheHits >= 1);
    }

    [Fact]
    public void World_NewArchetype_UpdatesQueryCache()
    {
        using var world = new World();

        // Create entity with Position
        world.Spawn().With(new Position()).Build();

        // Run query (caches result)
        var result1 = world.Query<Position>().ToList();
        Assert.Single(result1);

        // Add component to entity creates new archetype
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Query should now return both entities
        var result2 = world.Query<Position>().ToList();
        Assert.Equal(2, result2.Count);
    }

    #endregion

    #region QueryEnumerator Coverage Tests

    [Fact]
    public void QueryEnumerator_Current_AfterEnumerationComplete_ReturnsNull()
    {
        using var world = new World();
        world.Spawn().With(new Position()).Build();

        var query = world.Query<Position>();
        using var enumerator = query.GetEnumerator();

        // Move past the last element
        while (enumerator.MoveNext())
        {
            // Consume all
        }

        // After enumeration complete, MoveNext returns false
        Assert.False(enumerator.MoveNext());

        // Current should return Entity.Null when past end
        Assert.Equal(Entity.Null, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_IEnumeratorCurrent_ReturnsCorrectEntity()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).Build();

        var query = world.Query<Position>();
        System.Collections.IEnumerator enumerator = query.GetEnumerator();

        enumerator.MoveNext();
        object current = enumerator.Current;

        Assert.IsType<Entity>(current);
        Assert.Equal(entity, (Entity)current);
    }

    [Fact]
    public void QueryEnumerator_Reset_AllowsReenumeration()
    {
        using var world = new World();
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).Build();

        var query = world.Query<Position>();
        using var enumerator = query.GetEnumerator();

        // First enumeration
        var count1 = 0;
        while (enumerator.MoveNext())
        {
            count1++;
        }

        // Reset
        enumerator.Reset();

        // Second enumeration
        var count2 = 0;
        while (enumerator.MoveNext())
        {
            count2++;
        }

        Assert.Equal(2, count1);
        Assert.Equal(2, count2);
    }

    [Fact]
    public void QueryEnumerator_Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        world.Spawn().With(new Position()).Build();

        var query = world.Query<Position>();
        var enumerator = query.GetEnumerator();

        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    [Fact]
    public void QueryEnumerator_TwoComponent_EnumeratesCorrectly()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).Build(); // Won't match

        var count = 0;
        foreach (var entity in world.Query<Position, Velocity>())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void QueryEnumerator_TwoComponent_Reset_Works()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var query = world.Query<Position, Velocity>();
        using var enumerator = query.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
    }

    [Fact]
    public void QueryEnumerator_ThreeComponent_EnumeratesCorrectly()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build(); // Won't match

        var count = 0;
        foreach (var entity in world.Query<Position, Velocity, Health>())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void QueryEnumerator_ThreeComponent_Reset_Works()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();

        var query = world.Query<Position, Velocity, Health>();
        using var enumerator = query.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
    }

    private struct TestTag : ITagComponent;

    [Fact]
    public void QueryEnumerator_FourComponent_EnumeratesCorrectly()
    {
        using var world = new World();
        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .WithTag<TestTag>()
            .Build();

        var count = 0;
        foreach (var entity in world.Query<Position, Velocity, Health, TestTag>())
        {
            count++;
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public void QueryEnumerator_FourComponent_Reset_Works()
    {
        using var world = new World();
        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .WithTag<TestTag>()
            .Build();

        var query = world.Query<Position, Velocity, Health, TestTag>();
        using var enumerator = query.GetEnumerator();

        enumerator.MoveNext();
        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
    }

    [Fact]
    public void QueryEnumerator_EmptyWorld_NoIteration()
    {
        using var world = new World();

        var count = 0;
        foreach (var entity in world.Query<Position>())
        {
            count++;
        }

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryEnumerator_EmptyWorld_CurrentReturnsNull()
    {
        using var world = new World();

        var query = world.Query<Position>();
        using var enumerator = query.GetEnumerator();

        Assert.False(enumerator.MoveNext());
        Assert.Equal(Entity.Null, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_MultipleArchetypes_EnumeratesAll()
    {
        using var world = new World();

        // Create entities in different archetypes
        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();
        world.Spawn().With(new Position()).With(new Health()).Build();

        var count = 0;
        foreach (var entity in world.Query<Position>())
        {
            count++;
        }

        Assert.Equal(3, count);
    }

    #endregion

    #region QueryDescriptor Additional Coverage Tests

    [Fact]
    public void QueryDescriptor_Default_IsEmpty()
    {
        var desc = new QueryDescriptor([], []);

        Assert.Empty(desc.With);
        Assert.Empty(desc.Without);
    }

    [Fact]
    public void QueryDescriptor_Equals_BothEmpty_ReturnsTrue()
    {
        var desc1 = new QueryDescriptor([], []);
        var desc2 = new QueryDescriptor([], []);

        Assert.Equal(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_Equals_Null_ReturnsFalse()
    {
        var desc = new QueryDescriptor([typeof(Position)], []);

        Assert.False(desc.Equals(null));
    }

    [Fact]
    public void QueryDescriptor_Equals_Object_ReturnsFalseForWrongType()
    {
        var desc = new QueryDescriptor([typeof(Position)], []);

        Assert.False(desc.Equals("not a descriptor"));
    }

    [Fact]
    public void QueryDescriptor_DifferentWithoutSets_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Health)]);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_DifferentWithLength_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);
        var desc2 = new QueryDescriptor([typeof(Position)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_DifferentWithoutLength_NotEqual()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity), typeof(Health)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_DifferentWithOrder_NotEqual()
    {
        // QueryDescriptor uses sorted arrays, but if elements differ at any position, should not equal
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_DifferentWithoutOrder_NotEqual()
    {
        var desc1 = new QueryDescriptor([], [typeof(Position)]);
        var desc2 = new QueryDescriptor([], [typeof(Velocity)]);

        Assert.NotEqual(desc1, desc2);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void QueryManager_ConcurrentAccess_ThreadSafe()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create initial archetypes
        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Velocity)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        const int threadCount = 10;
        const int iterationsPerThread = TestConstants.ConcurrentIterationsPerThread;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();

        var threads = new Thread[threadCount];
        var startBarrier = new Barrier(threadCount);

        for (var i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    startBarrier.SignalAndWait();

                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        var result = queryManager.GetMatchingArchetypes(description);
                        Assert.NotNull(result);
                        Assert.True(result.Count >= 1);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            if (!thread.Join(TestConstants.ThreadJoinTimeout))
            {
                throw new TimeoutException($"Thread did not complete within {TestConstants.ThreadJoinTimeoutSeconds} seconds");
            }
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public void QueryManager_ConcurrentAccessWithArchetypeCreation_ThreadSafe()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create initial archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        const int readerThreadCount = 5;
        const int iterationsPerThread = 500;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var stopFlag = new ManualResetEventSlim(false);

        // Reader threads that continuously query
        var readerThreads = new Thread[readerThreadCount];
        for (var i = 0; i < readerThreadCount; i++)
        {
            readerThreads[i] = new Thread(() =>
            {
                try
                {
                    for (var j = 0; j < iterationsPerThread && !stopFlag.IsSet; j++)
                    {
                        var result = queryManager.GetMatchingArchetypes(description);
                        Assert.NotNull(result);
                        // May be 1, 2, or 3 depending on when archetypes are created
                        Assert.True(result.Count >= 1);

                        // Small delay to spread out access
                        Thread.SpinWait(10);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        // Writer thread that creates new archetypes
        var writerThread = new Thread(() =>
        {
            try
            {
                Thread.Sleep(TestConstants.ThreadSleepMediumMs); // Let readers start first
                manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);
                Thread.Sleep(TestConstants.ThreadSleepMediumMs);
                manager.GetOrCreateArchetype([typeof(Position), typeof(Health)]);
            }
            catch (Exception ex)
            {
                lock (exceptionLock)
                {
                    exceptions.Add(ex);
                }
            }
        });

        foreach (var thread in readerThreads)
        {
            thread.Start();
        }

        writerThread.Start();

        if (!writerThread.Join(TestConstants.ThreadJoinTimeout))
        {
            throw new TimeoutException($"Writer thread did not complete within {TestConstants.ThreadJoinTimeoutSeconds} seconds");
        }
        stopFlag.Set();

        foreach (var thread in readerThreads)
        {
            if (!thread.Join(TestConstants.ThreadJoinTimeout))
            {
                throw new TimeoutException($"Reader thread did not complete within {TestConstants.ThreadJoinTimeoutSeconds} seconds");
            }
        }

        Assert.Empty(exceptions);

        // Verify final state
        var finalResult = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(3, finalResult.Count);
    }

    [Fact]
    public void QueryManager_DifferentQueriesConcurrently_ThreadSafe()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create archetypes
        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Velocity)]);
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);
        manager.GetOrCreateArchetype([typeof(Health)]);

        var posDescription = new QueryDescription();
        posDescription.AddWrite<Position>();

        var velDescription = new QueryDescription();
        velDescription.AddWrite<Velocity>();

        var healthDescription = new QueryDescription();
        healthDescription.AddWrite<Health>();

        const int threadCount = 9; // 3 threads per query type
        const int iterationsPerThread = 500;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var startBarrier = new Barrier(threadCount);

        var threads = new Thread[threadCount];
        var descriptions = new[] { posDescription, velDescription, healthDescription };

        for (var i = 0; i < threadCount; i++)
        {
            var desc = descriptions[i % 3];
            threads[i] = new Thread(() =>
            {
                try
                {
                    startBarrier.SignalAndWait();

                    for (var j = 0; j < iterationsPerThread; j++)
                    {
                        var result = queryManager.GetMatchingArchetypes(desc);
                        Assert.NotNull(result);
                        Assert.True(result.Count >= 1);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            if (!thread.Join(TestConstants.ThreadJoinTimeout))
            {
                throw new TimeoutException($"Thread did not complete within {TestConstants.ThreadJoinTimeoutSeconds} seconds");
            }
        }

        Assert.Empty(exceptions);

        // Verify all queries are cached
        Assert.True(queryManager.CachedQueryCount >= 3);
    }

    [Fact]
    public void ArchetypeCache_ConcurrentAddAndRead_ThreadSafe()
    {
        // This tests the internal ArchetypeCache directly through QueryManager behavior
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Prime the cache with initial archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Get initial cached result
        var initialResult = queryManager.GetMatchingArchetypes(description);
        Assert.Single(initialResult);

        const int readerCount = 5;
        const int archetypesToAdd = 50;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();

        var readerThreads = new Thread[readerCount];
        var writerComplete = new ManualResetEventSlim(false);

        // Start reader threads
        for (var i = 0; i < readerCount; i++)
        {
            readerThreads[i] = new Thread(() =>
            {
                try
                {
                    while (!writerComplete.IsSet)
                    {
                        var result = queryManager.GetMatchingArchetypes(description);
                        Assert.NotNull(result);
                        // Result count should only grow
                        Assert.True(result.Count >= 1);

                        // Iterate to ensure snapshot is stable
                        var count = 0;
                        foreach (var archetype in result)
                        {
                            Assert.NotNull(archetype);
                            count++;
                        }

                        Assert.Equal(result.Count, count);
                        Thread.SpinWait(5);
                    }
                }
                catch (Exception ex)
                {
                    lock (exceptionLock)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            readerThreads[i].Start();
        }

        // Writer creates many archetypes that match the query
        try
        {
            for (var i = 0; i < archetypesToAdd; i++)
            {
                // Create archetypes that include Position but have different combinations
                var types = new List<Type> { typeof(Position) };

                // Add Velocity to some to vary the archetypes
                if (i % 2 == 0)
                {
                    types.Add(typeof(Velocity));
                }

                manager.GetOrCreateArchetype([.. types]);
                Thread.SpinWait(10);
            }
        }
        finally
        {
            writerComplete.Set();
        }

        foreach (var thread in readerThreads)
        {
            if (!thread.Join(TestConstants.ThreadJoinTimeout))
            {
                throw new TimeoutException($"Reader thread did not complete within {TestConstants.ThreadJoinTimeoutSeconds} seconds");
            }
        }

        Assert.Empty(exceptions);

        // Final result should have all matching archetypes (Position only + Position+Velocity)
        var finalResult = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, finalResult.Count);
    }

    #endregion

    #region Query Cache Invalidation Edge Cases

    [Fact]
    public void QueryManager_InvalidateAll_ClearsAllCachedQueries()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Velocity)]);

        var posDescription = new QueryDescription();
        posDescription.AddWrite<Position>();

        var velDescription = new QueryDescription();
        velDescription.AddWrite<Velocity>();

        // Cache two different queries
        queryManager.GetMatchingArchetypes(posDescription);
        queryManager.GetMatchingArchetypes(velDescription);

        Assert.Equal(2, queryManager.CachedQueryCount);

        // Invalidate all caches
        queryManager.InvalidateCache();

        Assert.Equal(0, queryManager.CachedQueryCount);
    }

    [Fact]
    public void QueryManager_SameQueryRepeated_UsesCache()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query - cache miss
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheMisses);

        // Same query again - cache hit
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(1, queryManager.CacheHits);

        // Results should be identical (same cached instance)
        Assert.Same(result1, result2);
    }

    [Fact]
    public void QueryManager_OnArchetypeCreated_UpdatesMatchingCaches()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);
        var queryManager = new QueryManager(manager);

        // Create first archetype
        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Query and cache
        var result1 = queryManager.GetMatchingArchetypes(description);
        Assert.Single(result1);

        // Create new archetype that matches the cached query
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        // Query again - should include new archetype
        var result2 = queryManager.GetMatchingArchetypes(description);
        Assert.Equal(2, result2.Count);
    }

    #endregion
}
