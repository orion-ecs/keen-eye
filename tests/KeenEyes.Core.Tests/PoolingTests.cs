namespace KeenEyes.Tests;

/// <summary>
/// Tests for the object pooling system.
/// </summary>
public class PoolingTests
{
    #region EntityPool Tests

    [Fact]
    public void EntityPool_Acquire_ReturnsValidEntity()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();

        Assert.True(entity.IsValid);
        Assert.Equal(0, entity.Id);
        Assert.Equal(1, entity.Version);
    }

    [Fact]
    public void EntityPool_Acquire_IncrementsIds()
    {
        var pool = new EntityPool();

        var entity1 = pool.Acquire();
        var entity2 = pool.Acquire();
        var entity3 = pool.Acquire();

        Assert.Equal(0, entity1.Id);
        Assert.Equal(1, entity2.Id);
        Assert.Equal(2, entity3.Id);
    }

    [Fact]
    public void EntityPool_Release_IncrementsVersion()
    {
        var pool = new EntityPool();

        var entity1 = pool.Acquire();
        pool.Release(entity1);

        var entity2 = pool.Acquire();

        Assert.Equal(entity1.Id, entity2.Id); // Same ID recycled
        Assert.Equal(entity1.Version + 1, entity2.Version); // Version incremented
    }

    [Fact]
    public void EntityPool_IsValid_DetectsStaleHandles()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();
        Assert.True(pool.IsValid(entity));

        pool.Release(entity);
        Assert.False(pool.IsValid(entity));

        var newEntity = pool.Acquire();
        Assert.True(pool.IsValid(newEntity));
        Assert.False(pool.IsValid(entity)); // Old handle still invalid
    }

    [Fact]
    public void EntityPool_Release_ReturnsFalseForInvalidEntity()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();
        Assert.True(pool.Release(entity));
        Assert.False(pool.Release(entity)); // Already released
    }

    [Fact]
    public void EntityPool_RecycleCount_TracksReuses()
    {
        var pool = new EntityPool();

        Assert.Equal(0, pool.RecycleCount);

        var e1 = pool.Acquire();
        pool.Release(e1);
        var e2 = pool.Acquire(); // Recycled

        Assert.Equal(1, pool.RecycleCount);

        pool.Release(e2);
        pool.Acquire(); // Recycled again

        Assert.Equal(2, pool.RecycleCount);
    }

    [Fact]
    public void EntityPool_TotalAllocated_TracksNewAllocations()
    {
        var pool = new EntityPool();

        Assert.Equal(0, pool.TotalAllocated);

        pool.Acquire();
        Assert.Equal(1, pool.TotalAllocated);

        pool.Acquire();
        Assert.Equal(2, pool.TotalAllocated);

        pool.Acquire();
        Assert.Equal(3, pool.TotalAllocated);
    }

    [Fact]
    public void EntityPool_ActiveCount_ReflectsCurrentState()
    {
        var pool = new EntityPool();

        Assert.Equal(0, pool.ActiveCount);

        var e1 = pool.Acquire();
        Assert.Equal(1, pool.ActiveCount);

        var e2 = pool.Acquire();
        Assert.Equal(2, pool.ActiveCount);

        pool.Release(e1);
        Assert.Equal(1, pool.ActiveCount);

        pool.Release(e2);
        Assert.Equal(0, pool.ActiveCount);
    }

    [Fact]
    public void EntityPool_AvailableCount_TracksRecycledIds()
    {
        var pool = new EntityPool();

        Assert.Equal(0, pool.AvailableCount);

        var e1 = pool.Acquire();
        var e2 = pool.Acquire();

        pool.Release(e1);
        Assert.Equal(1, pool.AvailableCount);

        pool.Release(e2);
        Assert.Equal(2, pool.AvailableCount);

        pool.Acquire(); // Uses recycled ID
        Assert.Equal(1, pool.AvailableCount);
    }

    [Fact]
    public void EntityPool_Clear_ResetsAllState()
    {
        var pool = new EntityPool();

        pool.Acquire();
        pool.Acquire();
        var e3 = pool.Acquire();
        pool.Release(e3);

        pool.Clear();

        Assert.Equal(0, pool.TotalAllocated);
        Assert.Equal(0, pool.ActiveCount);
        Assert.Equal(0, pool.AvailableCount);
        Assert.Equal(0, pool.RecycleCount);
    }

    [Fact]
    public void EntityPool_GetVersion_ReturnsCurrentVersion()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();
        Assert.Equal(1, pool.GetVersion(entity.Id));

        pool.Release(entity);
        Assert.Equal(2, pool.GetVersion(entity.Id));
    }

    [Fact]
    public void EntityPool_GetVersion_ReturnsNegativeForInvalidId()
    {
        var pool = new EntityPool();

        Assert.Equal(-1, pool.GetVersion(999));
    }

    #endregion

    #region ComponentArrayPool Tests

    [Fact]
    public void ComponentArrayPool_Rent_ReturnsArray()
    {
        var array = ComponentArrayPool<int>.Rent(10);

        Assert.NotNull(array);
        Assert.True(array.Length >= 10);

        ComponentArrayPool<int>.Return(array);
    }

    [Fact]
    public void ComponentArrayPool_Return_ClearsIfRequested()
    {
        var array = ComponentArrayPool<int>.Rent(10);
        array[0] = 42;

        ComponentArrayPool<int>.Return(array, clearArray: true);

        // After return, array should be cleared (but we can't verify directly)
        // The test mainly ensures no exception is thrown
    }

    [Fact]
    public void ComponentArrayPool_TotalRented_TracksRentals()
    {
        var before = ComponentArrayPool<double>.TotalRented;

        var array1 = ComponentArrayPool<double>.Rent(10);
        var array2 = ComponentArrayPool<double>.Rent(20);

        Assert.Equal(before + 2, ComponentArrayPool<double>.TotalRented);

        ComponentArrayPool<double>.Return(array1);
        ComponentArrayPool<double>.Return(array2);
    }

    [Fact]
    public void ComponentArrayPool_TotalReturned_TracksReturns()
    {
        var before = ComponentArrayPool<float>.TotalReturned;

        var array = ComponentArrayPool<float>.Rent(10);
        ComponentArrayPool<float>.Return(array);

        Assert.Equal(before + 1, ComponentArrayPool<float>.TotalReturned);
    }

    #endregion

    #region MemoryStats Tests

    [Fact]
    public void MemoryStats_RecycleEfficiency_CalculatesCorrectly()
    {
        var stats = new MemoryStats
        {
            EntitiesAllocated = 100,
            EntitiesActive = 50,
            EntityRecycleCount = 25
        };

        // destroyed = 100 - 50 = 50
        // efficiency = 25 / 50 * 100 = 50%
        Assert.Equal(50.0, stats.RecycleEfficiency);
    }

    [Fact]
    public void MemoryStats_RecycleEfficiency_HandlesZeroDestroyed()
    {
        var stats = new MemoryStats
        {
            EntitiesAllocated = 10,
            EntitiesActive = 10,
            EntityRecycleCount = 0
        };

        Assert.Equal(0.0, stats.RecycleEfficiency);
    }

    [Fact]
    public void MemoryStats_QueryCacheHitRate_CalculatesCorrectly()
    {
        var stats = new MemoryStats
        {
            QueryCacheHits = 80,
            QueryCacheMisses = 20
        };

        Assert.Equal(80.0, stats.QueryCacheHitRate);
    }

    [Fact]
    public void MemoryStats_QueryCacheHitRate_HandlesZeroQueries()
    {
        var stats = new MemoryStats
        {
            QueryCacheHits = 0,
            QueryCacheMisses = 0
        };

        Assert.Equal(0.0, stats.QueryCacheHitRate);
    }

    [Fact]
    public void MemoryStats_ToString_ReturnsFormattedString()
    {
        var stats = new MemoryStats
        {
            EntitiesAllocated = 100,
            EntitiesActive = 75,
            EntitiesRecycled = 10,
            EntityRecycleCount = 15,
            ArchetypeCount = 5,
            ComponentTypeCount = 8,
            SystemCount = 3,
            CachedQueryCount = 4,
            QueryCacheHits = 100,
            QueryCacheMisses = 10,
            EstimatedComponentBytes = 2048
        };

        var str = stats.ToString();

        Assert.Contains("75 active", str);
        Assert.Contains("100 allocated", str);
        Assert.Contains("5", str); // ArchetypeCount
        Assert.Contains("8 types", str); // ComponentTypeCount
    }

    #endregion

    #region World Pooling Integration Tests

    private struct TestPosition : IComponent
    {
        public float X;
        public float Y;

        public TestPosition() { }
    }

    [Fact]
    public void World_EntityRecycling_WorksCorrectly()
    {
        using var world = new World();

        var entities = new List<Entity>();

        // Create 10 entities
        for (var i = 0; i < 10; i++)
        {
            entities.Add(world.Spawn().With(new TestPosition { X = i }).Build());
        }

        // Despawn all
        foreach (var entity in entities)
        {
            world.Despawn(entity);
        }

        // Create 10 more - should reuse IDs
        var newEntities = new List<Entity>();
        for (var i = 0; i < 10; i++)
        {
            newEntities.Add(world.Spawn().With(new TestPosition { X = i + 10 }).Build());
        }

        // All should be alive with recycled IDs
        foreach (var entity in newEntities)
        {
            Assert.True(world.IsAlive(entity));
        }

        // Original entities should not be alive
        foreach (var entity in entities)
        {
            Assert.False(world.IsAlive(entity));
        }

        var stats = world.GetMemoryStats();
        Assert.Equal(10, stats.EntitiesActive);
        Assert.Equal(10, stats.EntityRecycleCount);
    }

    [Fact]
    public void World_StaleEntityHandle_CorrectlyDetected()
    {
        using var world = new World();

        var entity = world.Spawn().With(new TestPosition()).Build();
        var staleHandle = entity;

        world.Despawn(entity);

        // Create new entity that might get same ID
        var newEntity = world.Spawn().With(new TestPosition()).Build();

        // Stale handle should be invalid even if ID matches
        Assert.False(world.IsAlive(staleHandle));
        Assert.True(world.IsAlive(newEntity));
    }

    #endregion

    #region Non-Generic ComponentArrayPool Tests

    [Fact]
    public void ComponentArrayPool_NonGeneric_Rent_ReturnsArray()
    {
        var array = ComponentArrayPool.Rent(typeof(TestPosition), 10);

        Assert.NotNull(array);
        Assert.IsType<TestPosition[]>(array);
        Assert.True(array.Length >= 10);

        ComponentArrayPool.Return(typeof(TestPosition), array);
    }

    [Fact]
    public void ComponentArrayPool_NonGeneric_RentAndReturn_WorksForDifferentTypes()
    {
        var posArray = ComponentArrayPool.Rent(typeof(TestPosition), 5);
        var intArray = ComponentArrayPool.Rent(typeof(int), 8);

        Assert.IsType<TestPosition[]>(posArray);
        Assert.IsType<int[]>(intArray);
        Assert.True(posArray.Length >= 5);
        Assert.True(intArray.Length >= 8);

        ComponentArrayPool.Return(typeof(TestPosition), posArray);
        ComponentArrayPool.Return(typeof(int), intArray, clearArray: true);
    }

    [Fact]
    public void ComponentArrayPool_NonGeneric_Return_WithClear()
    {
        var array = ComponentArrayPool.Rent(typeof(TestPosition), 3);

        // Set some values
        var typedArray = (TestPosition[])array;
        typedArray[0] = new TestPosition { X = 1, Y = 2 };

        // Return with clear
        ComponentArrayPool.Return(typeof(TestPosition), array, clearArray: true);

        // Rent again - should be cleared
        var newArray = ComponentArrayPool.Rent(typeof(TestPosition), 3);
        var newTyped = (TestPosition[])newArray;
        Assert.Equal(0, newTyped[0].X);

        ComponentArrayPool.Return(typeof(TestPosition), newArray);
    }

    #endregion

    #region ChunkPool Tests

    [Fact]
    public void ChunkPool_Rent_CreatesNewChunkWhenPoolEmpty()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        var chunk = pool.Rent(archetypeId, [typeof(TestPosition)]);

        Assert.NotNull(chunk);
        Assert.Equal(archetypeId, chunk.ArchetypeId);
        Assert.Equal(1, pool.TotalRented);
        Assert.Equal(1, pool.TotalCreated);
    }

    [Fact]
    public void ChunkPool_Return_PoolsEmptyChunk()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = pool.Rent(archetypeId, [typeof(TestPosition)]);

        var returned = pool.Return(chunk);

        Assert.True(returned);
        Assert.Equal(1, pool.TotalReturned);
        Assert.Equal(1, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_Rent_ReusesPooledChunk()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk1 = pool.Rent(archetypeId, [typeof(TestPosition)]);
        pool.Return(chunk1);

        var chunk2 = pool.Rent(archetypeId, [typeof(TestPosition)]);

        Assert.Same(chunk1, chunk2);
        Assert.Equal(2, pool.TotalRented);
        Assert.Equal(1, pool.TotalCreated); // Only one created
    }

    [Fact]
    public void ChunkPool_Return_DiscardsNonEmptyChunk()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = pool.Rent(archetypeId, [typeof(TestPosition)]);

        // Add an entity to make chunk non-empty
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 1 });

        var returned = pool.Return(chunk);

        Assert.False(returned);
        Assert.Equal(1, pool.TotalDiscarded);
        Assert.Equal(0, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_Return_DiscardsWhenPoolFull()
    {
        var pool = new ChunkPool(maxChunksPerArchetype: 1);
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        var chunk1 = pool.Rent(archetypeId, [typeof(TestPosition)]);
        var chunk2 = pool.Rent(archetypeId, [typeof(TestPosition)]);

        pool.Return(chunk1); // First return succeeds
        var returned = pool.Return(chunk2); // Second should be discarded

        Assert.False(returned);
        Assert.Equal(1, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_ReuseRate_ReturnsCorrectValue()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        // Rent and return to create reuse scenario
        var chunk1 = pool.Rent(archetypeId, [typeof(TestPosition)]); // created = 1, rented = 1
        pool.Return(chunk1);
        pool.Rent(archetypeId, [typeof(TestPosition)]); // rented = 2, created still = 1

        // ReuseRate = 1 - (created / rented) = 1 - (1/2) = 0.5
        Assert.Equal(0.5, pool.ReuseRate);
    }

    [Fact]
    public void ChunkPool_ReuseRate_ReturnsZeroWhenNoRentals()
    {
        var pool = new ChunkPool();

        Assert.Equal(0, pool.ReuseRate);
    }

    [Fact]
    public void ChunkPool_ClearArchetype_RemovesPooledChunks()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        var chunk = pool.Rent(archetypeId, [typeof(TestPosition)]);
        pool.Return(chunk);

        Assert.Equal(1, pool.PooledCount);

        pool.ClearArchetype(archetypeId);

        Assert.Equal(0, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_ClearArchetype_NonExistentArchetype_NoOp()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        // Should not throw
        pool.ClearArchetype(archetypeId);

        Assert.Equal(0, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_Clear_RemovesAllPooledChunks()
    {
        var pool = new ChunkPool();
        var archetypeId1 = new ArchetypeId([typeof(TestPosition)]);
        var archetypeId2 = new ArchetypeId([typeof(int)]);

        var chunk1 = pool.Rent(archetypeId1, [typeof(TestPosition)]);
        var chunk2 = pool.Rent(archetypeId2, [typeof(int)]);
        pool.Return(chunk1);
        pool.Return(chunk2);

        Assert.Equal(2, pool.PooledCount);

        pool.Clear();

        Assert.Equal(0, pool.PooledCount);
    }

    [Fact]
    public void ChunkPool_GetStats_ReturnsCorrectValues()
    {
        var pool = new ChunkPool();
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);

        var chunk1 = pool.Rent(archetypeId, [typeof(TestPosition)]);
        var chunk2 = pool.Rent(archetypeId, [typeof(TestPosition)]);
        pool.Return(chunk1);

        var stats = pool.GetStats();

        Assert.Equal(2, stats.TotalRented);
        Assert.Equal(2, stats.TotalCreated);
        Assert.Equal(1, stats.TotalReturned);
        Assert.Equal(0, stats.TotalDiscarded);
        Assert.Equal(1, stats.PooledCount);
        Assert.Equal(1, stats.ArchetypeCount);
    }

    [Fact]
    public void ChunkPoolStats_HitRate_CalculatesCorrectly()
    {
        var stats = new ChunkPoolStats(
            TotalRented: 10,
            TotalReturned: 5,
            TotalCreated: 4,
            TotalDiscarded: 0,
            PooledCount: 3,
            ArchetypeCount: 2);

        // HitRate = 1 - (created / rented) = 1 - (4/10) = 0.6
        Assert.Equal(0.6, stats.HitRate);
    }

    [Fact]
    public void ChunkPoolStats_HitRate_ZeroRented_ReturnsZero()
    {
        var stats = new ChunkPoolStats(
            TotalRented: 0,
            TotalReturned: 0,
            TotalCreated: 0,
            TotalDiscarded: 0,
            PooledCount: 0,
            ArchetypeCount: 0);

        Assert.Equal(0, stats.HitRate);
    }

    [Fact]
    public void ChunkPool_PooledCount_TracksMultipleArchetypes()
    {
        var pool = new ChunkPool();
        var archetypeId1 = new ArchetypeId([typeof(TestPosition)]);
        var archetypeId2 = new ArchetypeId([typeof(int)]);

        var chunk1a = pool.Rent(archetypeId1, [typeof(TestPosition)]);
        var chunk1b = pool.Rent(archetypeId1, [typeof(TestPosition)]);
        var chunk2 = pool.Rent(archetypeId2, [typeof(int)]);

        pool.Return(chunk1a);
        pool.Return(chunk1b);
        pool.Return(chunk2);

        Assert.Equal(3, pool.PooledCount);
    }

    #endregion

    #region FixedComponentArray Tests

    private struct IntComponent : IComponent
    {
        public int Value;
    }

    [Fact]
    public void FixedComponentArray_Add_StoresComponent()
    {
        var array = new FixedComponentArray<IntComponent>(10);

        var index = array.Add(new IntComponent { Value = 42 });

        Assert.Equal(0, index);
        Assert.Equal(1, array.Count);
        Assert.Equal(42, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_GetRef_AllowsModification()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });

        ref var comp = ref array.GetRef(0);
        comp.Value = 99;

        Assert.Equal(99, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_GetReadonly_ReturnsValue()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 42 });

        ref readonly var comp = ref array.GetReadonly(0);

        Assert.Equal(42, comp.Value);
    }

    [Fact]
    public void FixedComponentArray_Set_UpdatesValue()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });

        array.Set(0, new IntComponent { Value = 99 });

        Assert.Equal(99, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_AsSpan_ReturnsValidSpan()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });
        array.Add(new IntComponent { Value = 2 });

        var span = array.AsSpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].Value);
        Assert.Equal(2, span[1].Value);
    }

    [Fact]
    public void FixedComponentArray_AsReadOnlySpan_ReturnsValidSpan()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });
        array.Add(new IntComponent { Value = 2 });

        var span = array.AsReadOnlySpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].Value);
        Assert.Equal(2, span[1].Value);
    }

    [Fact]
    public void FixedComponentArray_RemoveAtSwapBack_RemovesCorrectly()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });
        array.Add(new IntComponent { Value = 2 });
        array.Add(new IntComponent { Value = 3 });

        array.RemoveAtSwapBack(0);

        Assert.Equal(2, array.Count);
        Assert.Equal(3, array.GetRef(0).Value); // Last swapped to first
        Assert.Equal(2, array.GetRef(1).Value);
    }

    [Fact]
    public void FixedComponentArray_RemoveAtSwapBack_LastElement()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });
        array.Add(new IntComponent { Value = 2 });

        array.RemoveAtSwapBack(1);

        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_RemoveAtSwapBack_InvalidIndex_Throws()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });

        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(5));
    }

    [Fact]
    public void FixedComponentArray_AddBoxed_WorksCorrectly()
    {
        var array = new FixedComponentArray<IntComponent>(10);

        var index = array.AddBoxed(new IntComponent { Value = 42 });

        Assert.Equal(0, index);
        Assert.Equal(42, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_GetBoxed_ReturnsValue()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 42 });

        var result = array.GetBoxed(0);

        Assert.IsType<IntComponent>(result);
        Assert.Equal(42, ((IntComponent)result).Value);
    }

    [Fact]
    public void FixedComponentArray_SetBoxed_UpdatesValue()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });

        array.SetBoxed(0, new IntComponent { Value = 99 });

        Assert.Equal(99, array.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_Clear_EmptiesArray()
    {
        var array = new FixedComponentArray<IntComponent>(10);
        array.Add(new IntComponent { Value = 1 });
        array.Add(new IntComponent { Value = 2 });

        array.Clear();

        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void FixedComponentArray_Clear_EmptyArray_NoOp()
    {
        var array = new FixedComponentArray<IntComponent>(10);

        array.Clear(); // Should not throw

        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void FixedComponentArray_CopyTo_FixedArray()
    {
        var source = new FixedComponentArray<IntComponent>(10);
        source.Add(new IntComponent { Value = 42 });
        var dest = new FixedComponentArray<IntComponent>(10);

        source.CopyTo(0, dest);

        Assert.Equal(1, dest.Count);
        Assert.Equal(42, dest.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_CopyTo_GrowableArray()
    {
        var source = new FixedComponentArray<IntComponent>(10);
        source.Add(new IntComponent { Value = 42 });
        var dest = new ComponentArray<IntComponent>();

        source.CopyTo(0, dest);

        Assert.Equal(1, dest.Count);
        Assert.Equal(42, dest.GetRef(0).Value);
    }

    [Fact]
    public void FixedComponentArray_CopyTo_WrongType_Throws()
    {
        var source = new FixedComponentArray<IntComponent>(10);
        source.Add(new IntComponent { Value = 42 });
        var dest = new FixedComponentArray<TestPosition>(10);

        Assert.Throws<InvalidOperationException>(() => source.CopyTo(0, dest));
    }

    [Fact]
    public void FixedComponentArray_Capacity_ReturnsCorrectValue()
    {
        var array = new FixedComponentArray<IntComponent>(25);

        Assert.Equal(25, array.Capacity);
    }

    [Fact]
    public void FixedComponentArray_ComponentType_ReturnsCorrectType()
    {
        var array = new FixedComponentArray<IntComponent>(10);

        Assert.Equal(typeof(IntComponent), array.ComponentType);
    }

    #endregion

    #region ArchetypeChunk Tests

    [Fact]
    public void ArchetypeChunk_AddEntity_StoresEntity()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        var index = chunk.AddEntity(new Entity(1, 1));

        Assert.Equal(0, index);
        Assert.Equal(1, chunk.Count);
        Assert.False(chunk.IsEmpty);
    }

    [Fact]
    public void ArchetypeChunk_AddComponent_StoresComponent()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));

        chunk.AddComponent(new TestPosition { X = 42, Y = 99 });

        Assert.Equal(42, chunk.Get<TestPosition>(0).X);
        Assert.Equal(99, chunk.Get<TestPosition>(0).Y);
    }

    [Fact]
    public void ArchetypeChunk_RemoveEntity_RemovesCorrectly()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        var entity = new Entity(1, 1);
        chunk.AddEntity(entity);
        chunk.AddComponent(new TestPosition { X = 1 });

        var swapped = chunk.RemoveEntity(entity);

        Assert.Null(swapped);
        Assert.Equal(0, chunk.Count);
        Assert.True(chunk.IsEmpty);
    }

    [Fact]
    public void ArchetypeChunk_RemoveEntity_SwapsBack()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(2, 1);
        var entity3 = new Entity(3, 1);

        chunk.AddEntity(entity1);
        chunk.AddComponent(new TestPosition { X = 1 });
        chunk.AddEntity(entity2);
        chunk.AddComponent(new TestPosition { X = 2 });
        chunk.AddEntity(entity3);
        chunk.AddComponent(new TestPosition { X = 3 });

        var swapped = chunk.RemoveEntity(entity1);

        Assert.Equal(entity3, swapped); // entity3 was swapped into position 0
        Assert.Equal(2, chunk.Count);
        Assert.Equal(3, chunk.Get<TestPosition>(0).X); // entity3's component
    }

    [Fact]
    public void ArchetypeChunk_IsFull_ReturnsCorrectValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)], capacity: 2);

        Assert.False(chunk.IsFull);

        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition());
        Assert.False(chunk.IsFull);

        chunk.AddEntity(new Entity(2, 1));
        chunk.AddComponent(new TestPosition());
        Assert.True(chunk.IsFull);
    }

    [Fact]
    public void ArchetypeChunk_FreeSpace_ReturnsCorrectValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)], capacity: 5);

        Assert.Equal(5, chunk.FreeSpace);

        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition());

        Assert.Equal(4, chunk.FreeSpace);
    }

    [Fact]
    public void ArchetypeChunk_Contains_ReturnsCorrectValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        var entity = new Entity(1, 1);
        chunk.AddEntity(entity);
        chunk.AddComponent(new TestPosition());

        Assert.True(chunk.Contains(entity));
        Assert.False(chunk.Contains(new Entity(999, 1)));
    }

    [Fact]
    public void ArchetypeChunk_GetEntityIndex_ReturnsCorrectIndex()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        var entity = new Entity(1, 1);
        chunk.AddEntity(entity);
        chunk.AddComponent(new TestPosition());

        Assert.Equal(0, chunk.GetEntityIndex(entity));
        Assert.Equal(-1, chunk.GetEntityIndex(new Entity(999, 1)));
    }

    [Fact]
    public void ArchetypeChunk_GetEntity_ReturnsCorrectEntity()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        var entity = new Entity(1, 1);
        chunk.AddEntity(entity);
        chunk.AddComponent(new TestPosition());

        Assert.Equal(entity, chunk.GetEntity(0));
    }

    [Fact]
    public void ArchetypeChunk_GetEntities_ReturnsAllEntities()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(2, 1);
        chunk.AddEntity(entity1);
        chunk.AddComponent(new TestPosition());
        chunk.AddEntity(entity2);
        chunk.AddComponent(new TestPosition());

        var entities = chunk.GetEntities();

        Assert.Equal(2, entities.Length);
        Assert.Equal(entity1, entities[0]);
        Assert.Equal(entity2, entities[1]);
    }

    [Fact]
    public void ArchetypeChunk_Has_ReturnsCorrectValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        Assert.True(chunk.Has<TestPosition>());
        Assert.True(chunk.Has(typeof(TestPosition)));
        Assert.False(chunk.Has<IntComponent>());
        Assert.False(chunk.Has(typeof(IntComponent)));
    }

    [Fact]
    public void ArchetypeChunk_Set_UpdatesComponent()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 1 });

        chunk.Set(0, new TestPosition { X = 99 });

        Assert.Equal(99, chunk.Get<TestPosition>(0).X);
    }

    [Fact]
    public void ArchetypeChunk_GetReadonly_ReturnsValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 42 });

        ref readonly var pos = ref chunk.GetReadonly<TestPosition>(0);

        Assert.Equal(42, pos.X);
    }

    [Fact]
    public void ArchetypeChunk_GetSpan_ReturnsValidSpan()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 1 });
        chunk.AddEntity(new Entity(2, 1));
        chunk.AddComponent(new TestPosition { X = 2 });

        var span = chunk.GetSpan<TestPosition>();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
    }

    [Fact]
    public void ArchetypeChunk_GetReadOnlySpan_ReturnsValidSpan()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 1 });

        var span = chunk.GetReadOnlySpan<TestPosition>();

        Assert.Equal(1, span.Length);
        Assert.Equal(1, span[0].X);
    }

    [Fact]
    public void ArchetypeChunk_AddEntity_WhenFull_Throws()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)], capacity: 1);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition());

        Assert.Throws<InvalidOperationException>(() => chunk.AddEntity(new Entity(2, 1)));
    }

    [Fact]
    public void ArchetypeChunk_RemoveEntity_NotInChunk_ReturnsNull()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        var result = chunk.RemoveEntity(new Entity(999, 1));

        Assert.Null(result);
    }

    [Fact]
    public void ArchetypeChunk_Reset_ClearsAllData()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 42 });

        chunk.Reset();

        Assert.Equal(0, chunk.Count);
        Assert.True(chunk.IsEmpty);
    }

    [Fact]
    public void ArchetypeChunk_GetBoxed_ReturnsValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 42 });

        var result = chunk.GetBoxed(typeof(TestPosition), 0);

        Assert.IsType<TestPosition>(result);
        Assert.Equal(42, ((TestPosition)result).X);
    }

    [Fact]
    public void ArchetypeChunk_GetBoxed_TypeNotInChunk_Throws()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition());

        Assert.Throws<InvalidOperationException>(() => chunk.GetBoxed(typeof(IntComponent), 0));
    }

    [Fact]
    public void ArchetypeChunk_SetBoxed_UpdatesValue()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition { X = 1 });

        chunk.SetBoxed(typeof(TestPosition), 0, new TestPosition { X = 99 });

        Assert.Equal(99, chunk.Get<TestPosition>(0).X);
    }

    [Fact]
    public void ArchetypeChunk_AddComponentBoxed_AddsComponent()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));

        chunk.AddComponentBoxed(typeof(TestPosition), new TestPosition { X = 42 });

        Assert.Equal(42, chunk.Get<TestPosition>(0).X);
    }

    [Fact]
    public void ArchetypeChunk_CopyComponentsTo_CopiesSharedComponents()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var source = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        var dest = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);

        source.AddEntity(new Entity(1, 1));
        source.AddComponent(new TestPosition { X = 42 });
        dest.AddEntity(new Entity(2, 1));

        source.CopyComponentsTo(0, dest);

        Assert.Equal(42, dest.Get<TestPosition>(0).X);
    }

    [Fact]
    public void ArchetypeChunk_Dispose_CleansUp()
    {
        var archetypeId = new ArchetypeId([typeof(TestPosition)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(TestPosition)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new TestPosition());

        chunk.Dispose();
        chunk.Dispose(); // Should not throw on double dispose

        Assert.Equal(0, chunk.Count);
    }

    #endregion
}
