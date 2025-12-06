namespace KeenEyes.Tests;

/// <summary>
/// Tests for edge cases and boundary conditions across the ECS framework.
/// Covers scenarios like dead entities, empty queries, invalid operations, etc.
/// </summary>
public class EdgeCaseTests
{
    #region Test Components

    private struct Position : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Velocity : IComponent
    {
        public float X;
        public float Y;
    }

    private struct Health : IComponent
    {
        public int Current;
        public int Max;
    }

    private struct Rotation : IComponent
    {
        public float Angle;
    }

    private struct EnemyTag : ITagComponent;

    #endregion

    #region World - Dead Entity and Invalid Operation Tests

    [Fact]
    public void World_GetEntityByName_ReturnsNull_WhenEntityPoolReturnsNegativeVersion()
    {
        using var world = new World();

        // Create a named entity
        var entity = world.Spawn("TestEntity")
            .With(new Position())
            .Build();

        // Despawn it
        world.Despawn(entity);

        // The name mapping might still exist but the version should be incremented
        // Try to get by name - should return Entity.Null because entity is destroyed
        var result = world.GetEntityByName("TestEntity");

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void World_GetName_ReturnsNull_ForDestroyedEntity()
    {
        using var world = new World();

        var entity = world.Spawn("MyEntity").With(new Position()).Build();
        world.Despawn(entity);

        var name = world.GetName(entity);

        Assert.Null(name);
    }

    [Fact]
    public void World_GetComponents_ReturnsEmpty_ForDeadEntity()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        var components = world.GetComponents(entity).ToList();

        Assert.Empty(components);
    }

    [Fact]
    public void World_Set_DeadEntity_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new Position { X = 1 }));
    }

    [Fact]
    public void World_Set_UnregisteredComponent_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        // Velocity is not on the entity
        Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new Velocity { X = 1 }));
    }

    [Fact]
    public void World_Add_DeadEntity_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new Velocity { X = 1 }));
    }

    [Fact]
    public void World_Remove_DeadEntity_ReturnsFalse()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        var result = world.Remove<Position>(entity);

        Assert.False(result);
    }

    [Fact]
    public void World_Remove_UnregisteredComponent_ReturnsFalse()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        // Health is not registered
        var result = world.Remove<Health>(entity);

        Assert.False(result);
    }

    [Fact]
    public void World_Has_DeadEntity_ReturnsFalse()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        Assert.False(world.Has<Position>(entity));
    }

    [Fact]
    public void World_Has_UnregisteredComponent_ReturnsFalse()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        // Health is not registered in this world
        Assert.False(world.Has<Health>(entity));
    }

    [Fact]
    public void World_Get_DeadEntity_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Despawn(entity);

        Assert.Throws<InvalidOperationException>(() => world.Get<Position>(entity));
    }

    [Fact]
    public void World_Get_UnregisteredComponent_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        // Health is not registered
        Assert.Throws<InvalidOperationException>(() => world.Get<Health>(entity));
    }

    [Fact]
    public void World_Get_EntityWithoutComponent_Throws()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();
        world.Components.Register<Velocity>();

        // Entity doesn't have Velocity
        Assert.Throws<InvalidOperationException>(() => world.Get<Velocity>(entity));
    }

    #endregion

    #region QueryEnumerator - Empty and Exhausted Enumeration Tests

    [Fact]
    public void QueryEnumerator_Current_ReturnsNull_WhenNoMatchingArchetypes()
    {
        using var world = new World();
        // Create entity with different component than what we query for
        world.Spawn().With(new Position()).Build();

        // Query for Velocity which has no matching archetypes
        var enumerator = world.Query<Velocity>().GetEnumerator();

        // Access Current when no archetypes match
        var current = enumerator.Current;

        Assert.Equal(Entity.Null, current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_Current_ReturnsNull_AfterExhausted()
    {
        using var world = new World();
        world.Spawn().With(new Position()).Build();

        var enumerator = world.Query<Position>().GetEnumerator();
        while (enumerator.MoveNext()) { }

        // Access Current after enumeration complete - archetypeIndex >= archetypes.Count
        var current = enumerator.Current;

        Assert.Equal(Entity.Null, current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_TwoComponents_Reset_Works()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var enumerator = world.Query<Position, Velocity>().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_TwoComponents_Current_ReturnsNull_WhenNoMatchingArchetypes()
    {
        using var world = new World();
        // Create entity with only Position, not Position+Velocity
        world.Spawn().With(new Position()).Build();

        // Query for Position+Velocity which has no matching archetypes
        var enumerator = world.Query<Position, Velocity>().GetEnumerator();
        var current = enumerator.Current;

        Assert.Equal(Entity.Null, current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_ThreeComponents_Reset_Works()
    {
        using var world = new World();
        world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();

        var enumerator = world.Query<Position, Velocity, Health>().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_ThreeComponents_Current_ReturnsNull_WhenNoMatchingArchetypes()
    {
        using var world = new World();
        // Create entity with only two components, not all three
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Query for all three which has no matching archetypes
        var enumerator = world.Query<Position, Velocity, Health>().GetEnumerator();
        var current = enumerator.Current;

        Assert.Equal(Entity.Null, current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_FourComponents_Reset_Works()
    {
        using var world = new World();
        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .With(new Rotation())
            .Build();

        var enumerator = world.Query<Position, Velocity, Health, Rotation>().GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();

        Assert.True(enumerator.MoveNext());
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_FourComponents_Current_ReturnsNull_WhenNoMatchingArchetypes()
    {
        using var world = new World();
        // Create entity with only three components, not all four
        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .Build();

        // Query for all four which has no matching archetypes
        var enumerator = world.Query<Position, Velocity, Health, Rotation>().GetEnumerator();
        var current = enumerator.Current;

        Assert.Equal(Entity.Null, current);
        enumerator.Dispose();
    }

    [Fact]
    public void QueryEnumerator_TwoComponents_IEnumeratorCurrent_ReturnsBoxed()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).With(new Velocity()).Build();

        System.Collections.IEnumerator enumerator = world.Query<Position, Velocity>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_ThreeComponents_IEnumeratorCurrent_ReturnsBoxed()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).With(new Velocity()).With(new Health()).Build();

        System.Collections.IEnumerator enumerator = world.Query<Position, Velocity, Health>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_FourComponents_IEnumeratorCurrent_ReturnsBoxed()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .With(new Rotation())
            .Build();

        System.Collections.IEnumerator enumerator = world.Query<Position, Velocity, Health, Rotation>().GetEnumerator();
        enumerator.MoveNext();

        Assert.Equal(entity, enumerator.Current);
    }

    [Fact]
    public void QueryEnumerator_TwoComponents_Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        var enumerator = world.Query<Position, Velocity>().GetEnumerator();
        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    [Fact]
    public void QueryEnumerator_ThreeComponents_Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        var enumerator = world.Query<Position, Velocity, Health>().GetEnumerator();
        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    [Fact]
    public void QueryEnumerator_FourComponents_Dispose_CanBeCalledMultipleTimes()
    {
        using var world = new World();
        var enumerator = world.Query<Position, Velocity, Health, Rotation>().GetEnumerator();
        enumerator.Dispose();
        enumerator.Dispose(); // Should not throw
    }

    #endregion

    #region QueryManager - Cache Statistics and Invalidation Tests

    [Fact]
    public void QueryManager_HitRate_ReturnsZero_WhenNoQueries()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);
        var queryManager = new QueryManager(manager);

        Assert.Equal(0.0, queryManager.HitRate);
    }

    [Fact]
    public void QueryManager_InvalidateQuery_RemovesFromCache()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();
        var descriptor = QueryDescriptor.FromDescription(description);

        // Execute query to cache it
        queryManager.GetMatchingArchetypes(descriptor);
        Assert.Equal(1, queryManager.CachedQueryCount);

        // Invalidate
        queryManager.InvalidateQuery(descriptor);

        // Cache should be empty
        Assert.Equal(0, queryManager.CachedQueryCount);
    }

    [Fact]
    public void QueryManager_InvalidateCache_ClearsAll()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var desc1 = new QueryDescription();
        desc1.AddWrite<Position>();
        var desc2 = new QueryDescription();
        desc2.AddWrite<Velocity>();

        queryManager.GetMatchingArchetypes(desc1);
        queryManager.GetMatchingArchetypes(desc2);
        Assert.Equal(2, queryManager.CachedQueryCount);

        queryManager.InvalidateCache();

        Assert.Equal(0, queryManager.CachedQueryCount);
    }

    [Fact]
    public void QueryManager_ResetStatistics_ClearsCounters()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var queryManager = new QueryManager(manager);

        manager.GetOrCreateArchetype([typeof(Position)]);

        var description = new QueryDescription();
        description.AddWrite<Position>();

        // First query is a miss
        queryManager.GetMatchingArchetypes(description);
        // Second query is a hit
        queryManager.GetMatchingArchetypes(description);

        Assert.Equal(1, queryManager.CacheHits);
        Assert.Equal(1, queryManager.CacheMisses);

        queryManager.ResetStatistics();

        Assert.Equal(0, queryManager.CacheHits);
        Assert.Equal(0, queryManager.CacheMisses);
    }

    #endregion

    #region QueryDescriptor - Equality and Matching Tests

    [Fact]
    public void QueryDescriptor_ToString_WithoutExclusions()
    {
        var descriptor = new QueryDescriptor([typeof(Position)], []);

        var str = descriptor.ToString();

        Assert.Contains("Query", str);
        Assert.Contains("Position", str);
        Assert.DoesNotContain("Without", str);
    }

    [Fact]
    public void QueryDescriptor_ToString_WithExclusions()
    {
        var descriptor = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        var str = descriptor.ToString();

        Assert.Contains("Query", str);
        Assert.Contains("Position", str);
        Assert.Contains("Without", str);
        Assert.Contains("Velocity", str);
    }

    [Fact]
    public void QueryDescriptor_Equals_DifferentHashCode_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.NotEqual(desc1, desc2);
        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Equals_DifferentWithLength_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_Equals_DifferentWithoutLength_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_Equals_DifferentWithTypes_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);
        var desc2 = new QueryDescriptor([typeof(Position), typeof(Health)], []);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_Equals_DifferentWithoutTypes_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Health)]);

        Assert.NotEqual(desc1, desc2);
    }

    [Fact]
    public void QueryDescriptor_Operators_WorkCorrectly()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Position)], []);
        var desc3 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.True(desc1 == desc2);
        Assert.False(desc1 != desc2);
        Assert.True(desc1 != desc3);
        Assert.False(desc1 == desc3);
    }

    [Fact]
    public void QueryDescriptor_EqualsObject_NullReturnsFalse()
    {
        var desc = new QueryDescriptor([typeof(Position)], []);

        Assert.False(desc.Equals(null));
    }

    [Fact]
    public void QueryDescriptor_EqualsObject_WrongTypeReturnsFalse()
    {
        var desc = new QueryDescriptor([typeof(Position)], []);

        Assert.False(desc.Equals("not a descriptor"));
    }

    [Fact]
    public void QueryDescriptor_Matches_MissingRequiredComponent_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var desc = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);

        Assert.False(desc.Matches(archetype));
    }

    [Fact]
    public void QueryDescriptor_Matches_HasExcludedComponent_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        var desc = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        Assert.False(desc.Matches(archetype));
    }

    #endregion

    #region ArchetypeId - Equality and Component Lookup Tests

    [Fact]
    public void ArchetypeId_Equals_SameHashButDifferentLength_ReturnsFalse()
    {
        // Create two ArchetypeIds with potentially same hash but different lengths
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Position), typeof(Velocity)]);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ArchetypeId_Equals_SameHashSameLengthDifferentTypes_ReturnsFalse()
    {
        // Create archetypes with same length but different types
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Velocity)]);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ArchetypeId_Has_DefaultArchetype_ReturnsFalse()
    {
        var id = default(ArchetypeId);

        Assert.False(id.Has(typeof(Position)));
    }

    [Fact]
    public void ArchetypeId_ToString_Default_ReturnsEmptyFormat()
    {
        var id = default(ArchetypeId);

        var str = id.ToString();

        Assert.Equal("ArchetypeId()", str);
    }

    [Fact]
    public void ArchetypeId_BinarySearch_FindsTypeInMiddle()
    {
        // Test binary search with multiple types
        var id = new ArchetypeId([typeof(Health), typeof(Position), typeof(Velocity)]);

        Assert.True(id.Has(typeof(Health)));
        Assert.True(id.Has(typeof(Position)));
        Assert.True(id.Has(typeof(Velocity)));
        Assert.False(id.Has(typeof(Rotation)));
    }

    [Fact]
    public void ArchetypeId_InternalConstructor_UsesPrecomputedHash()
    {
        var types = new[] { typeof(Position), typeof(Velocity) };
        var hash = new HashCode();
        foreach (var t in types.OrderBy(t => t.FullName))
        {
            hash.Add(t);
        }
        var expectedHash = hash.ToHashCode();

        var id1 = new ArchetypeId(types);
        var id2 = new ArchetypeId(types);

        Assert.Equal(expectedHash, id1.GetHashCode());
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    #endregion

    #region EntityPool - Invalid Entity Handle Tests

    [Fact]
    public void EntityPool_Release_NegativeId_ReturnsFalse()
    {
        var pool = new EntityPool();

        var invalidEntity = new Entity(-1, 1);
        var result = pool.Release(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void EntityPool_Release_IdTooLarge_ReturnsFalse()
    {
        var pool = new EntityPool();

        var invalidEntity = new Entity(999, 1);
        var result = pool.Release(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void EntityPool_IsValid_NegativeId_ReturnsFalse()
    {
        var pool = new EntityPool();

        var invalidEntity = new Entity(-1, 1);
        var result = pool.IsValid(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void EntityPool_IsValid_IdTooLarge_ReturnsFalse()
    {
        var pool = new EntityPool();

        var invalidEntity = new Entity(999, 1);
        var result = pool.IsValid(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void EntityPool_Release_StaleVersion_ReturnsFalse()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();
        pool.Release(entity);

        // Entity now has incremented version, trying to release old version fails
        var result = pool.Release(entity);

        Assert.False(result);
    }

    #endregion

    #region ComponentArray - Modification and Access Tests

    [Fact]
    public void ComponentArray_RemoveAtSwapBack_LastElement_Works()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });
        array.Add(new Position { X = 2 });

        array.RemoveAtSwapBack(1);

        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).X);
    }

    [Fact]
    public void ComponentArray_GetReadonly_ReturnsValue()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 42, Y = 99 });

        ref readonly var pos = ref array.GetReadonly(0);

        Assert.Equal(42, pos.X);
        Assert.Equal(99, pos.Y);
    }

    [Fact]
    public void ComponentArray_Set_UpdatesValue()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });

        array.Set(0, new Position { X = 99, Y = 88 });

        Assert.Equal(99, array.GetRef(0).X);
        Assert.Equal(88, array.GetRef(0).Y);
    }

    #endregion

    #region ArchetypeChunk - Component Type Mismatch Tests

    [Fact]
    public void ArchetypeChunk_SetBoxed_TypeNotInChunk_NoOp()
    {
        var archetypeId = new ArchetypeId([typeof(Position)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(Position)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new Position { X = 1 });

        // SetBoxed with a type not in the chunk should be a no-op
        chunk.SetBoxed(typeof(Velocity), 0, new Velocity { X = 99 });

        // Position should be unchanged
        Assert.Equal(1, chunk.Get<Position>(0).X);
    }

    [Fact]
    public void ArchetypeChunk_AddComponentBoxed_TypeNotInChunk_NoOp()
    {
        var archetypeId = new ArchetypeId([typeof(Position)]);
        var chunk = new ArchetypeChunk(archetypeId, [typeof(Position)]);
        chunk.AddEntity(new Entity(1, 1));
        chunk.AddComponent(new Position { X = 1 });

        // AddComponentBoxed with a type not in the chunk should be a no-op
        chunk.AddComponentBoxed(typeof(Velocity), new Velocity { X = 99 });

        // Only Position should be in chunk
        Assert.True(chunk.Has<Position>());
        Assert.False(chunk.Has<Velocity>());
    }

    [Fact]
    public void ArchetypeChunk_CopyComponentsTo_SkipsNonSharedTypes()
    {
        var archetypeId1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var archetypeId2 = new ArchetypeId([typeof(Position)]); // No Velocity

        var source = new ArchetypeChunk(archetypeId1, [typeof(Position), typeof(Velocity)]);
        var dest = new ArchetypeChunk(archetypeId2, [typeof(Position)]);

        source.AddEntity(new Entity(1, 1));
        source.AddComponent(new Position { X = 42 });
        source.AddComponent(new Velocity { X = 99 });

        dest.AddEntity(new Entity(2, 1));

        source.CopyComponentsTo(0, dest);

        Assert.Equal(42, dest.Get<Position>(0).X);
        // Velocity not copied since dest doesn't have it
    }

    #endregion

    #region ComponentArrayPool - Pool Statistics Tests

    [Fact]
    public void ComponentArrayPool_OutstandingCount_TracksCorrectly()
    {
        var beforeRented = ComponentArrayPool<long>.TotalRented;
        var beforeReturned = ComponentArrayPool<long>.TotalReturned;

        var array1 = ComponentArrayPool<long>.Rent(10);
        var array2 = ComponentArrayPool<long>.Rent(20);

        Assert.Equal(beforeRented + 2, ComponentArrayPool<long>.TotalRented);
        Assert.Equal(beforeReturned, ComponentArrayPool<long>.TotalReturned);

        var outstanding = ComponentArrayPool<long>.OutstandingCount;
        Assert.True(outstanding >= 2);

        ComponentArrayPool<long>.Return(array1);
        ComponentArrayPool<long>.Return(array2);

        Assert.Equal(beforeRented + 2, ComponentArrayPool<long>.TotalRented);
        Assert.Equal(beforeReturned + 2, ComponentArrayPool<long>.TotalReturned);
    }

    #endregion

    #region Query Iteration - Multiple Archetype Tests

    [Fact]
    public void QueryEnumerator_MultipleArchetypes_IteratesAll()
    {
        using var world = new World();

        // Create entities in different archetypes that all have Position
        var e1 = world.Spawn().With(new Position { X = 1 }).Build();
        var e2 = world.Spawn().With(new Position { X = 2 }).With(new Velocity()).Build();
        var e3 = world.Spawn().With(new Position { X = 3 }).With(new Velocity()).With(new Health()).Build();

        var entities = world.Query<Position>().ToList();

        Assert.Equal(3, entities.Count);
        Assert.Contains(e1, entities);
        Assert.Contains(e2, entities);
        Assert.Contains(e3, entities);
    }

    [Fact]
    public void QueryEnumerator_EmptyArchetypeInMiddle_SkipsCorrectly()
    {
        using var world = new World();

        // Create archetype A with entities
        var e1 = world.Spawn().With(new Position()).Build();

        // Create archetype B (Position + Velocity)
        var e2 = world.Spawn().With(new Position()).With(new Velocity()).Build();

        // Remove all entities from archetype B
        world.Despawn(e2);

        // Query should only find e1
        var entities = world.Query<Position>().ToList();

        Assert.Single(entities);
        Assert.Contains(e1, entities);
    }

    #endregion
}
