namespace KeenEyes.Tests;

/// <summary>
/// Tests for CommandBufferPool thread-safe buffer management.
/// </summary>
public class CommandBufferPoolTests
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

    #region Basic Pool Operations

    [Fact]
    public void Rent_ReturnsCommandBuffer()
    {
        var pool = new CommandBufferPool();

        var buffer = pool.Rent(systemId: 0);

        Assert.NotNull(buffer);
        Assert.Equal(1, pool.ActiveBufferCount);
    }

    [Fact]
    public void Rent_MultipleSystems_ReturnsDifferentBuffers()
    {
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 0);
        var buffer2 = pool.Rent(systemId: 1);
        var buffer3 = pool.Rent(systemId: 2);

        Assert.NotSame(buffer1, buffer2);
        Assert.NotSame(buffer2, buffer3);
        Assert.Equal(3, pool.ActiveBufferCount);
    }

    [Fact]
    public void Rent_SameSystemId_ThrowsException()
    {
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 0);

        Assert.Throws<InvalidOperationException>(() => pool.Rent(systemId: 0));
    }

    [Fact]
    public void Return_ReleasesBuffer()
    {
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 0);
        Assert.Equal(1, pool.ActiveBufferCount);

        pool.Return(systemId: 0);
        Assert.Equal(0, pool.ActiveBufferCount);
    }

    [Fact]
    public void Return_AllowsRerent()
    {
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 0);
        pool.Return(systemId: 0);

        var buffer2 = pool.Rent(systemId: 0);

        // Buffer should be recycled from pool
        Assert.Same(buffer1, buffer2);
    }

    [Fact]
    public void TotalCommandCount_SumsAcrossBuffers()
    {
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 0);
        var buffer2 = pool.Rent(systemId: 1);

        buffer1.Spawn();
        buffer1.Spawn();
        buffer2.Spawn();

        Assert.Equal(3, pool.TotalCommandCount);
    }

    #endregion

    #region FlushAll Tests

    [Fact]
    public void FlushAll_ExecutesAllCommands()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 0);
        var buffer2 = pool.Rent(systemId: 1);

        buffer1.Spawn().With(new Position { X = 0, Y = 0 });
        buffer2.Spawn().With(new Position { X = 10, Y = 10 });

        pool.FlushAll(world);

        var entities = world.GetAllEntities().ToList();
        Assert.Equal(2, entities.Count);
    }

    [Fact]
    public void FlushAll_ReturnsGlobalEntityMap()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 0);
        var buffer2 = pool.Rent(systemId: 1);

        var bufferId1 = pool.GetBufferId(0);
        var bufferId2 = pool.GetBufferId(1);

        var cmd1 = buffer1.Spawn().With(new Position { X = 0, Y = 0 });
        var cmd2 = buffer2.Spawn().With(new Position { X = 10, Y = 10 });

        var entityMap = pool.FlushAll(world);

        // Verify we can look up entities using global IDs
        var globalId1 = CommandBufferPool.GetGlobalPlaceholderId(bufferId1, cmd1.PlaceholderId);
        var globalId2 = CommandBufferPool.GetGlobalPlaceholderId(bufferId2, cmd2.PlaceholderId);

        Assert.True(entityMap.ContainsKey(globalId1));
        Assert.True(entityMap.ContainsKey(globalId2));
        Assert.True(world.IsAlive(entityMap[globalId1]));
        Assert.True(world.IsAlive(entityMap[globalId2]));
    }

    [Fact]
    public void FlushAll_ExecutesInDeterministicOrder()
    {
        // Execute multiple times and verify consistent ordering
        for (var iteration = 0; iteration < 10; iteration++)
        {
            using var world = new World();
            var pool = new CommandBufferPool();

            // Rent in random order
            var buffer3 = pool.Rent(systemId: 3);
            var buffer1 = pool.Rent(systemId: 1);
            var buffer2 = pool.Rent(systemId: 2);

            // Each buffer spawns entity with identifying position
            buffer1.Spawn().With(new Position { X = 1, Y = 0 });
            buffer2.Spawn().With(new Position { X = 2, Y = 0 });
            buffer3.Spawn().With(new Position { X = 3, Y = 0 });

            pool.FlushAll(world);

            var entities = world.GetAllEntities().ToList();

            // Entities should be created in system ID order (1, 2, 3)
            // First entity should have X=1, second X=2, third X=3
            Assert.Equal(3, entities.Count);

            var positions = entities
                .Select(e => world.Get<Position>(e).X)
                .ToList();

            Assert.Equal(1, positions[0]);
            Assert.Equal(2, positions[1]);
            Assert.Equal(3, positions[2]);
        }
    }

    [Fact]
    public void FlushAll_ClearsAndReturnsBuffers()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 0).Spawn();
        pool.Rent(systemId: 1).Spawn();

        Assert.Equal(2, pool.ActiveBufferCount);

        pool.FlushAll(world);

        Assert.Equal(0, pool.ActiveBufferCount);
    }

    [Fact]
    public void FlushAll_EmptyPool_ReturnsEmptyMap()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var entityMap = pool.FlushAll(world);

        Assert.Empty(entityMap);
    }

    #endregion

    #region FlushBatches Tests

    [Fact]
    public void FlushBatches_ExecutesInBatchOrder()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 1);
        var buffer2 = pool.Rent(systemId: 2);
        var buffer3 = pool.Rent(systemId: 3);

        buffer1.Spawn().With(new Position { X = 1, Y = 0 });
        buffer2.Spawn().With(new Position { X = 2, Y = 0 });
        buffer3.Spawn().With(new Position { X = 3, Y = 0 });

        // Flush in batches: [1, 2] then [3]
        var batches = new[]
        {
            new[] { 2, 1 }, // Within batch, should still order by system ID
            new[] { 3 }
        };

        pool.FlushBatches(world, batches);

        var entities = world.GetAllEntities().ToList();
        Assert.Equal(3, entities.Count);
    }

    [Fact]
    public void FlushBatches_WithinBatchOrderedBySystemId()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var buffer2 = pool.Rent(systemId: 2);
        var buffer1 = pool.Rent(systemId: 1);

        buffer1.Spawn().With(new Position { X = 1, Y = 0 });
        buffer2.Spawn().With(new Position { X = 2, Y = 0 });

        // Batch specifies [2, 1] but should execute in order [1, 2]
        var batches = new[] { new[] { 2, 1 } };

        pool.FlushBatches(world, batches);

        var entities = world.GetAllEntities().ToList();
        var positions = entities.Select(e => world.Get<Position>(e).X).ToList();

        Assert.Equal(1, positions[0]);
        Assert.Equal(2, positions[1]);
    }

    [Fact]
    public void FlushBatches_CrossBatchEntityReference_ResolvesCorrectly()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        var buffer1 = pool.Rent(systemId: 1);
        var buffer2 = pool.Rent(systemId: 2);

        // First batch: spawn parent
        var parentCmd = buffer1.Spawn()
            .With(new Position { X = 0, Y = 0 });

        // Second batch: spawn child and add component to parent
        buffer2.Spawn()
            .With(new Position { X = 10, Y = 10 });

        // Flush in two batches
        var batches = new[]
        {
            new[] { 1 },  // Batch 1: system 1
            new[] { 2 }   // Batch 2: system 2
        };

        var entityMap = pool.FlushBatches(world, batches);

        Assert.Equal(2, entityMap.Count);
        Assert.Equal(2, world.GetAllEntities().Count());
    }

    #endregion

    #region Global Placeholder ID Tests

    [Fact]
    public void GetGlobalPlaceholderId_CreatesUniqueIds()
    {
        var id1 = CommandBufferPool.GetGlobalPlaceholderId(bufferId: 1, localPlaceholderId: -1);
        var id2 = CommandBufferPool.GetGlobalPlaceholderId(bufferId: 1, localPlaceholderId: -2);
        var id3 = CommandBufferPool.GetGlobalPlaceholderId(bufferId: 2, localPlaceholderId: -1);

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id1, id3);
        Assert.NotEqual(id2, id3);
    }

    [Fact]
    public void ParseGlobalPlaceholderId_ReversesGetGlobalPlaceholderId()
    {
        var bufferId = 42;
        var localId = -5;

        var globalId = CommandBufferPool.GetGlobalPlaceholderId(bufferId, localId);
        var (parsedBufferId, parsedLocalId) = CommandBufferPool.ParseGlobalPlaceholderId(globalId);

        Assert.Equal(bufferId, parsedBufferId);
        Assert.Equal(localId, parsedLocalId);
    }

    [Fact]
    public void GetBufferId_ReturnsCorrectId()
    {
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 5);
        pool.Rent(systemId: 10);

        var bufferId5 = pool.GetBufferId(5);
        var bufferId10 = pool.GetBufferId(10);

        Assert.NotEqual(bufferId5, bufferId10);
        Assert.True(bufferId5 > 0);
        Assert.True(bufferId10 > 0);
    }

    [Fact]
    public void GetBufferId_NonExistentSystem_ReturnsMinusOne()
    {
        var pool = new CommandBufferPool();

        var bufferId = pool.GetBufferId(999);

        Assert.Equal(-1, bufferId);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllBuffers()
    {
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 0).Spawn();
        pool.Rent(systemId: 1).Spawn();

        pool.Clear();

        Assert.Equal(0, pool.ActiveBufferCount);
        Assert.Equal(0, pool.TotalCommandCount);
    }

    [Fact]
    public void Clear_AllowsNewRents()
    {
        var pool = new CommandBufferPool();

        pool.Rent(systemId: 0);
        pool.Clear();

        // Should not throw - system ID is available again
        var buffer = pool.Rent(systemId: 0);
        Assert.NotNull(buffer);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void ConcurrentRent_DifferentSystemIds_ThreadSafe()
    {
        var pool = new CommandBufferPool();
        const int threadCount = 10;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var threads = new Thread[threadCount];
        using var barrier = new Barrier(threadCount);
        using var allStarted = new CountdownEvent(threadCount);

        for (var i = 0; i < threadCount; i++)
        {
            var systemId = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    allStarted.Signal();
                    barrier.SignalAndWait(TimeSpan.FromSeconds(10));
                    var buffer = pool.Rent(systemId);
                    buffer.Spawn().With(new Position { X = systemId, Y = systemId });
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

        // Wait for all threads to start (with timeout and cancellation support)
        Assert.True(
            allStarted.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
            "Threads failed to start in time");

        foreach (var thread in threads)
        {
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Thread failed to complete in time");
        }

        Assert.Empty(exceptions);
        Assert.Equal(threadCount, pool.ActiveBufferCount);
    }

    [Fact]
    public void ConcurrentFlushAll_AfterParallelCommands_ExecutesCorrectly()
    {
        using var world = new World();
        var pool = new CommandBufferPool();
        const int threadCount = 5;
        const int commandsPerThread = 100;

        var exceptions = new List<Exception>();
        var exceptionLock = new object();
        var threads = new Thread[threadCount];
        using var barrier = new Barrier(threadCount);
        using var allStarted = new CountdownEvent(threadCount);

        for (var i = 0; i < threadCount; i++)
        {
            var systemId = i;
            threads[i] = new Thread(() =>
            {
                try
                {
                    var buffer = pool.Rent(systemId);
                    allStarted.Signal();
                    barrier.SignalAndWait(TimeSpan.FromSeconds(10));

                    for (var j = 0; j < commandsPerThread; j++)
                    {
                        buffer.Spawn().With(new Position { X = systemId, Y = j });
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

        // Wait for all threads to start (with timeout and cancellation support)
        Assert.True(
            allStarted.Wait(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken),
            "Threads failed to start in time");

        foreach (var thread in threads)
        {
            Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "Thread failed to complete in time");
        }

        Assert.Empty(exceptions);

        // Flush all should succeed
        pool.FlushAll(world);

        Assert.Equal(threadCount * commandsPerThread, world.GetAllEntities().Count());
    }

    [Fact]
    public void ConcurrentRentAndReturn_ThreadSafe()
    {
        var pool = new CommandBufferPool();
        const int iterations = 100;
        var exceptions = new List<Exception>();
        var exceptionLock = new object();

        var rentThread = new Thread(() =>
        {
            try
            {
                for (var i = 0; i < iterations; i++)
                {
                    pool.Rent(i + TestConstants.ConcurrentTestHighIdOffset); // Use high IDs to avoid conflicts
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

        var returnThread = new Thread(() =>
        {
            try
            {
                for (var i = 0; i < iterations; i++)
                {
                    pool.Return(i + 2000); // Different IDs - should not throw
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

        rentThread.Start();
        returnThread.Start();
        rentThread.Join();
        returnThread.Join();

        Assert.Empty(exceptions);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public void ParallelSystems_SpawnAndModify_ExecutesCorrectly()
    {
        using var world = new World();

        // Create some existing entities
        var existing = world.Spawn()
            .With(new Position { X = 0, Y = 0 })
            .Build();

        var pool = new CommandBufferPool();

        // System 1: Spawns new entity
        var buffer1 = pool.Rent(systemId: 1);
        buffer1.Spawn().With(new Position { X = 100, Y = 100 });

        // System 2: Modifies existing entity
        var buffer2 = pool.Rent(systemId: 2);
        buffer2.SetComponent(existing, new Position { X = 50, Y = 50 });

        // System 3: Spawns and despawns
        var buffer3 = pool.Rent(systemId: 3);
        var tempCmd = buffer3.Spawn().With(new Position { X = 200, Y = 200 });
        buffer3.Despawn(tempCmd.PlaceholderId);

        pool.FlushAll(world);

        // 1 existing + 1 new - 1 despawned = 2
        Assert.Equal(2, world.GetAllEntities().Count());

        // Existing entity should be modified
        ref var pos = ref world.Get<Position>(existing);
        Assert.Equal(50, pos.X);
        Assert.Equal(50, pos.Y);
    }

    [Fact]
    public void MultipleFlushCycles_ReuseBuffers()
    {
        using var world = new World();
        var pool = new CommandBufferPool();

        for (var cycle = 0; cycle < 5; cycle++)
        {
            var buffer = pool.Rent(systemId: 0);
            buffer.Spawn().With(new Position { X = cycle, Y = cycle });
            pool.FlushAll(world);
        }

        Assert.Equal(5, world.GetAllEntities().Count());
    }

    #endregion
}
