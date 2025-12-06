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
}
