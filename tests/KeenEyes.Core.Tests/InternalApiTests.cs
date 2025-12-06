using System.Collections;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for internal APIs and edge cases in the ECS framework.
/// These tests exercise code paths that are less commonly used but still important.
/// </summary>
public class InternalApiTests
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

    #endregion

    #region World - Internal QueryManager Access

    [Fact]
    public void World_Queries_Property_ReturnsQueryManager()
    {
        using var world = new World();

        // Access the internal Queries property
        var queryManager = world.Queries;

        Assert.NotNull(queryManager);
    }

    [Fact]
    public void World_GetEntityByName_WithRemovedName_ReturnsNull()
    {
        using var world = new World();

        // Create a named entity
        var entity = world.Spawn("TestEntity")
            .With(new Position())
            .Build();

        // Despawn it - this triggers the version < 0 path or dead entity path
        world.Despawn(entity);

        // Try to get by name - should return Entity.Null
        var result = world.GetEntityByName("TestEntity");

        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void World_GetMatchingEntities_ReturnsAllMatchingEntities()
    {
        using var world = new World();

        // Create entities with different component combinations
        var entity1 = world.Spawn().With(new Position()).Build();
        var entity2 = world.Spawn().With(new Position()).With(new Velocity()).Build();
        var entity3 = world.Spawn().With(new Velocity()).Build();

        // Create a QueryDescription for Position
        var description = new QueryDescription();
        description.AddWrite<Position>();

        // Use the internal GetMatchingEntities method
        var matchingEntities = world.GetMatchingEntities(description).ToList();

        Assert.Equal(2, matchingEntities.Count);
        Assert.Contains(entity1, matchingEntities);
        Assert.Contains(entity2, matchingEntities);
        Assert.DoesNotContain(entity3, matchingEntities);
    }

    [Fact]
    public void World_GetMatchingEntities_WithMultipleArchetypes_IteratesAll()
    {
        using var world = new World();

        // Create entities in different archetypes that all match Position
        var entity1 = world.Spawn().With(new Position()).Build();
        var entity2 = world.Spawn().With(new Position()).With(new Velocity()).Build();
        var entity3 = world.Spawn().With(new Position()).With(new Health()).Build();

        var description = new QueryDescription();
        description.AddWrite<Position>();

        var matchingEntities = world.GetMatchingEntities(description).ToList();

        Assert.Equal(3, matchingEntities.Count);
    }

    #endregion

    #region ArchetypeManager - Error Paths

    [Fact]
    public void ArchetypeManager_RemoveEntity_WithUnknownEntity_ReturnsFalse()
    {
        using var world = new World();

        // Create an entity in a different world to get a valid but unknown entity
        using var otherWorld = new World();
        var unknownEntity = otherWorld.Spawn().With(new Position()).Build();

        // Try to remove an entity that doesn't exist in this world's archetype manager
        var result = world.ArchetypeManager.RemoveEntity(unknownEntity);

        Assert.False(result);
    }

    [Fact]
    public void ArchetypeManager_AddComponent_WithUnknownEntity_Throws()
    {
        using var world = new World();

        // Create a fake entity that doesn't exist
        var unknownEntity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            world.ArchetypeManager.AddComponent(unknownEntity, new Position()));
    }

    [Fact]
    public void ArchetypeManager_RemoveComponent_WithUnknownEntity_ReturnsFalse()
    {
        using var world = new World();

        // Create an entity in a different world
        using var otherWorld = new World();
        var unknownEntity = otherWorld.Spawn().With(new Position()).Build();

        var result = world.ArchetypeManager.RemoveComponent<Position>(unknownEntity);

        Assert.False(result);
    }

    [Fact]
    public void ArchetypeManager_GetEntityLocation_WithUnknownEntity_Throws()
    {
        using var world = new World();

        var unknownEntity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            world.ArchetypeManager.GetEntityLocation(unknownEntity));
    }

    [Fact]
    public void ArchetypeManager_TryGetEntityLocation_WithKnownEntity_ReturnsTrue()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        var result = world.ArchetypeManager.TryGetEntityLocation(entity, out var archetype, out var index);

        Assert.True(result);
        Assert.NotNull(archetype);
        Assert.True(index >= 0);
    }

    [Fact]
    public void ArchetypeManager_GetComponentTypes_WithKnownEntity_ReturnsTypes()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .Build();

        var componentTypes = world.ArchetypeManager.GetComponentTypes(entity).ToList();

        Assert.Equal(2, componentTypes.Count);
        Assert.Contains(typeof(Position), componentTypes);
        Assert.Contains(typeof(Velocity), componentTypes);
    }

    #endregion

    #region Archetype - Chunks Property

    [Fact]
    public void Archetype_Chunks_ReturnsChunkList()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position()).Build();

        world.ArchetypeManager.TryGetEntityLocation(entity, out var archetype, out _);

        var chunks = archetype!.Chunks;

        Assert.NotNull(chunks);
        Assert.True(chunks.Count > 0);
    }

    #endregion

    #region ArchetypeId - Equality Edge Cases

    [Fact]
    public void ArchetypeId_Equals_WithDefaultArrays_ReturnsFalse()
    {
        // Create an ArchetypeId with actual components
        var id1 = new ArchetypeId([typeof(Position)]);

        // Create a default ArchetypeId (empty)
        var id2 = default(ArchetypeId);

        // Comparing with default should return false
        Assert.False(id1.Equals(id2));
    }

    [Fact]
    public void ArchetypeId_Equals_WithDifferentLengths_ReturnsFalse()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Position), typeof(Velocity)]);

        Assert.False(id1.Equals(id2));
        Assert.False(id2.Equals(id1));
    }

    [Fact]
    public void ArchetypeId_Equals_WithDifferentTypes_ReturnsFalse()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Velocity)]);

        Assert.False(id1.Equals(id2));
    }

    [Fact]
    public void ArchetypeId_Equals_WithSameTypes_ReturnsTrue()
    {
        var id1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var id2 = new ArchetypeId([typeof(Position), typeof(Velocity)]);

        Assert.True(id1.Equals(id2));
    }

    [Fact]
    public void ArchetypeId_With_CreatesNewId()
    {
        // The internal constructor is used by With<T> operations
        var baseId = new ArchetypeId([typeof(Position)]);
        var extendedId = baseId.With<Velocity>();

        // The extended ID should have a different hash
        Assert.NotEqual(baseId.GetHashCode(), extendedId.GetHashCode());

        // But should still work correctly for equality
        var sameId = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        Assert.Equal(extendedId, sameId);
    }

    #endregion

    #region ComponentArray - Large Array (Non-Pooled) Path

    [Fact]
    public void ComponentArray_WithLargeCapacity_UsesNonPooledArray()
    {
        // Create a very large array to trigger non-pooled path
        // MaxPooledArraySize is 1024 * 1024
        const int largeCapacity = 2 * 1024 * 1024;

        var array = new ComponentArray<Position>(largeCapacity);

        // Should still work
        array.Add(new Position { X = 1, Y = 2 });

        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).X);

        array.Dispose();
    }

    #endregion

    #region QueryDescriptor - Equality Mismatches

    [Fact]
    public void QueryDescriptor_Equals_WithDifferentWithLengths_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Equals_WithDifferentWithoutLengths_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Health)]);
        var desc2 = new QueryDescriptor([typeof(Position)], []);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Equals_WithDifferentWithTypes_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Equals_WithDifferentWithoutTypes_ReturnsFalse()
    {
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Health)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Constructor_WithWithoutTypes_SortsCorrectly()
    {
        // Order shouldn't matter - should produce equal descriptors
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Health), typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Velocity), typeof(Health)]);

        Assert.Equal(desc1, desc2);
    }

    #endregion

    #region QueryBuilder - IEnumerable Explicit Implementations

    [Fact]
    public void QueryBuilder2_IEnumerable_GetEnumerator_Works()
    {
        using var world = new World();

        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var query = world.Query<Position, Velocity>();

        // Use the explicit IEnumerable interface
        var entities = new List<Entity>();
        foreach (var entity in (IEnumerable)query)
        {
            entities.Add((Entity)entity);
        }

        Assert.Single(entities);
    }

    [Fact]
    public void QueryBuilder3_IEnumerable_GetEnumerator_Works()
    {
        using var world = new World();

        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .Build();

        var query = world.Query<Position, Velocity, Health>();

        var entities = new List<Entity>();
        foreach (var entity in (IEnumerable)query)
        {
            entities.Add((Entity)entity);
        }

        Assert.Single(entities);
    }

    [Fact]
    public void QueryBuilder4_IEnumerable_GetEnumerator_Works()
    {
        using var world = new World();

        world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .With(new Health())
            .With(new Rotation())
            .Build();

        var query = world.Query<Position, Velocity, Health, Rotation>();

        var entities = new List<Entity>();
        foreach (var entity in (IEnumerable)query)
        {
            entities.Add((Entity)entity);
        }

        Assert.Single(entities);
    }

    #endregion

    #region ChunkPoolStats - Record Properties

    [Fact]
    public void ChunkPoolStats_Properties_AreAccessible()
    {
        var stats = new ChunkPoolStats(
            TotalRented: 100,
            TotalReturned: 80,
            TotalCreated: 50,
            TotalDiscarded: 10,
            PooledCount: 20,
            ArchetypeCount: 5);

        Assert.Equal(100, stats.TotalRented);
        Assert.Equal(80, stats.TotalReturned);
        Assert.Equal(50, stats.TotalCreated);
        Assert.Equal(10, stats.TotalDiscarded);
        Assert.Equal(20, stats.PooledCount);
        Assert.Equal(5, stats.ArchetypeCount);
    }

    [Fact]
    public void ChunkPoolStats_HitRate_CalculatesCorrectly()
    {
        var stats = new ChunkPoolStats(
            TotalRented: 100,
            TotalReturned: 80,
            TotalCreated: 25,
            TotalDiscarded: 10,
            PooledCount: 20,
            ArchetypeCount: 5);

        // HitRate = 1.0 - (TotalCreated / TotalRented) = 1.0 - (25/100) = 0.75
        Assert.Equal(0.75, stats.HitRate, precision: 2);
    }

    [Fact]
    public void ChunkPoolStats_HitRate_WithZeroRented_ReturnsZero()
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

    #endregion

    #region Entity and ComponentId - Record Properties

    [Fact]
    public void Entity_RecordProperties_AreAccessible()
    {
        var entity = new Entity(Id: 42, Version: 3);

        Assert.Equal(42, entity.Id);
        Assert.Equal(3, entity.Version);
    }

    [Fact]
    public void ComponentId_RecordProperties_AreAccessible()
    {
        var id = new ComponentId(Value: 7);

        Assert.Equal(7, id.Value);
    }

    [Fact]
    public void ComponentId_CompareTo_Works()
    {
        var id1 = new ComponentId(5);
        var id2 = new ComponentId(10);
        var id3 = new ComponentId(5);

        Assert.True(id1.CompareTo(id2) < 0);
        Assert.True(id2.CompareTo(id1) > 0);
        Assert.Equal(0, id1.CompareTo(id3));
    }

    #endregion

    #region EntityPool - Edge Cases

    [Fact]
    public void EntityPool_GetVersion_WithUnallocatedId_ReturnsNegative()
    {
        var pool = new EntityPool();

        // Try to get version for an ID that was never allocated
        var version = pool.GetVersion(999);

        Assert.True(version < 0);
    }

    [Fact]
    public void EntityPool_IsValid_WithReleasedEntity_ReturnsFalse()
    {
        var pool = new EntityPool();

        var entity = pool.Acquire();
        pool.Release(entity);

        // The old entity reference should no longer be valid
        Assert.False(pool.IsValid(entity));
    }

    #endregion

    #region ComponentArrayPool - Static Pool Behavior

    [Fact]
    public void ComponentArrayPool_RentAndReturn_TracksStats()
    {
        var beforeRented = ComponentArrayPool<Position>.TotalRented;
        var beforeReturned = ComponentArrayPool<Position>.TotalReturned;

        // Rent an array
        var array = ComponentArrayPool<Position>.Rent(16);
        Assert.NotNull(array);
        Assert.True(array.Length >= 16);

        // Verify rental was tracked
        Assert.Equal(beforeRented + 1, ComponentArrayPool<Position>.TotalRented);

        // Return it
        ComponentArrayPool<Position>.Return(array);

        // Verify return was tracked
        Assert.Equal(beforeReturned + 1, ComponentArrayPool<Position>.TotalReturned);
    }

    [Fact]
    public void ComponentArrayPool_OutstandingCount_TracksActiveRentals()
    {
        var before = ComponentArrayPool<Velocity>.OutstandingCount;

        var array = ComponentArrayPool<Velocity>.Rent(16);
        Assert.Equal(before + 1, ComponentArrayPool<Velocity>.OutstandingCount);

        ComponentArrayPool<Velocity>.Return(array);
        Assert.Equal(before, ComponentArrayPool<Velocity>.OutstandingCount);
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public void World_GetEntityByName_AfterDespawn_VersionNegativePath()
    {
        using var world = new World();

        // Create a named entity and despawn it
        var entity = world.Spawn("TestEntity").With(new Position()).Build();
        world.Despawn(entity);

        // Now create another entity with the same name
        // This tests the version check path
        var entity2 = world.Spawn("TestEntity2").With(new Position()).Build();
        world.Despawn(entity2);

        // The name mapping still exists but entity is dead
        var result = world.GetEntityByName("TestEntity2");
        Assert.Equal(Entity.Null, result);
    }

    [Fact]
    public void QueryDescriptor_Equals_WithDifferentHashCodes_ReturnsFalse()
    {
        // Force a hash code mismatch by using types with different hashes
        var desc1 = new QueryDescriptor([typeof(Position)], []);
        var desc2 = new QueryDescriptor([typeof(Velocity)], []);

        // These have different hash codes, so equality fails fast
        Assert.False(desc1.Equals(desc2));
        Assert.NotEqual(desc1.GetHashCode(), desc2.GetHashCode());
    }

    [Fact]
    public void QueryDescriptor_Equals_SameLengthDifferentWithTypes_ReturnsFalse()
    {
        // Same lengths but different 'with' types - this tests the with[i] != other.with[i] path
        var desc1 = new QueryDescriptor([typeof(Position), typeof(Velocity)], []);
        var desc2 = new QueryDescriptor([typeof(Position), typeof(Health)], []);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void QueryDescriptor_Equals_SameLengthDifferentWithoutTypes_ReturnsFalse()
    {
        // Same lengths but different 'without' types - this tests the without[i] != other.without[i] path
        var desc1 = new QueryDescriptor([typeof(Position)], [typeof(Velocity)]);
        var desc2 = new QueryDescriptor([typeof(Position)], [typeof(Health)]);

        Assert.False(desc1.Equals(desc2));
    }

    [Fact]
    public void EntityPool_Release_WithInvalidId_ReturnsFalse()
    {
        var pool = new EntityPool();

        // Try to release an entity with an ID that was never allocated
        var invalidEntity = new Entity(-1, 1);
        var result = pool.Release(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void EntityPool_Release_WithOutOfRangeId_ReturnsFalse()
    {
        var pool = new EntityPool();

        // Acquire one entity
        pool.Acquire();

        // Try to release an entity with an ID beyond what was allocated
        var outOfRangeEntity = new Entity(999, 1);
        var result = pool.Release(outOfRangeEntity);

        Assert.False(result);
    }

    #endregion
}
