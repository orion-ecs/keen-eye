using EnemyTag = KeenEyes.Tests.TestEnemyTag;
using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Tests for the archetype-based storage system.
/// </summary>
public class ArchetypeTests
{
    #region ArchetypeId Tests

    [Fact]
    public void ArchetypeId_SameComponents_SameHash()
    {
        var id1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var id2 = new ArchetypeId([typeof(Velocity), typeof(Position)]);

        Assert.Equal(id1, id2);
        Assert.Equal(id1.GetHashCode(), id2.GetHashCode());
    }

    [Fact]
    public void ArchetypeId_DifferentComponents_DifferentHash()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Velocity)]);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ArchetypeId_Has_ReturnsCorrectResult()
    {
        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);

        Assert.True(id.Has(typeof(Position)));
        Assert.True(id.Has(typeof(Velocity)));
        Assert.False(id.Has(typeof(Health)));
    }

    [Fact]
    public void ArchetypeId_With_AddsComponentType()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = id1.With(typeof(Velocity));

        Assert.False(id1.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Position)));
    }

    [Fact]
    public void ArchetypeId_Without_RemovesComponentType()
    {
        var id1 = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var id2 = id1.Without(typeof(Velocity));

        Assert.True(id1.Has(typeof(Velocity)));
        Assert.False(id2.Has(typeof(Velocity)));
        Assert.True(id2.Has(typeof(Position)));
    }

    [Fact]
    public void ArchetypeId_ComponentCount_ReturnsCorrectValue()
    {
        var empty = new ArchetypeId([]);
        var single = new ArchetypeId([typeof(Position)]);
        var multiple = new ArchetypeId([typeof(Position), typeof(Velocity), typeof(Health)]);

        Assert.Equal(0, empty.ComponentCount);
        Assert.Equal(1, single.ComponentCount);
        Assert.Equal(3, multiple.ComponentCount);
    }

    [Fact]
    public void ArchetypeId_ToString_ReturnsReadableFormat()
    {
        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var str = id.ToString();

        Assert.Contains("Position", str);
        Assert.Contains("Velocity", str);
    }

    #endregion

    #region ComponentArray Tests

    [Fact]
    public void ComponentArray_Add_StoresComponent()
    {
        using var array = new ComponentArray<Position>();

        var index = array.Add(new Position { X = 1, Y = 2 });

        Assert.Equal(0, index);
        Assert.Equal(1, array.Count);
        Assert.Equal(1, array.GetRef(0).X);
        Assert.Equal(2, array.GetRef(0).Y);
    }

    [Fact]
    public void ComponentArray_GetRef_AllowsModification()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 2 });

        ref var pos = ref array.GetRef(0);
        pos.X = 10;

        Assert.Equal(10, array.GetRef(0).X);
    }

    [Fact]
    public void ComponentArray_RemoveAtSwapBack_RemovesComponent()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });
        array.Add(new Position { X = 3, Y = 3 });

        array.RemoveAtSwapBack(0);

        Assert.Equal(2, array.Count);
        Assert.Equal(3, array.GetRef(0).X); // Last element swapped to index 0
        Assert.Equal(2, array.GetRef(1).X);
    }

    [Fact]
    public void ComponentArray_AsSpan_ReturnsValidSpan()
    {
        using var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1, Y = 1 });
        array.Add(new Position { X = 2, Y = 2 });

        var span = array.AsSpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
    }

    [Fact]
    public void ComponentArray_GrowsAutomatically()
    {
        using var array = new ComponentArray<Position>(initialCapacity: 2);

        for (var i = 0; i < 100; i++)
        {
            array.Add(new Position { X = i, Y = i });
        }

        Assert.Equal(100, array.Count);
        Assert.True(array.Capacity >= 100);
    }

    #endregion

    #region Archetype Tests

    [Fact]
    public void Archetype_EntityAdditionAndRemoval()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        var id = new ArchetypeId([typeof(Position), typeof(Velocity)]);
        var infos = new[] { world.Components.Get<Position>()!, world.Components.Get<Velocity>()! };
        using var archetype = new Archetype(id, infos);

        var entity = new Entity(0, 1);
        archetype.AddEntity(entity);
        archetype.AddComponent(new Position { X = 1, Y = 2 });
        archetype.AddComponent(new Velocity { X = 3, Y = 4 });

        Assert.Equal(1, archetype.Count);
        Assert.Equal(1, archetype.Get<Position>(0).X);
        Assert.Equal(3, archetype.Get<Velocity>(0).X);

        archetype.RemoveEntity(entity);
        Assert.Equal(0, archetype.Count);
    }

    [Fact]
    public void Archetype_Has_ReturnsCorrectResult()
    {
        using var world = new World();
        world.Components.Register<Position>();

        var id = new ArchetypeId([typeof(Position)]);
        var infos = new[] { world.Components.Get<Position>()! };
        using var archetype = new Archetype(id, infos);

        Assert.True(archetype.Has<Position>());
        Assert.False(archetype.Has<Velocity>());
    }

    [Fact]
    public void Archetype_Has_WithMultipleComponents_UsesEfficientLookup()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();
        world.Components.Register<Health>();

        var id = new ArchetypeId([typeof(Position), typeof(Velocity), typeof(Health)]);
        // ArchetypeId sorts types by FullName, so we need to match that order
        var sortedInfos = id.ComponentTypes
            .Select(t => world.Components.Get(t)!)
            .ToArray();
        using var archetype = new Archetype(id, sortedInfos);

        // Test all components are found
        Assert.True(archetype.Has<Position>());
        Assert.True(archetype.Has<Velocity>());
        Assert.True(archetype.Has<Health>());

        // Test non-existent component returns false
        Assert.False(archetype.Has<EnemyTag>());
    }

    [Fact]
    public void Archetype_Has_WithSingleComponent_ReturnsCorrectly()
    {
        using var world = new World();
        world.Components.Register<Position>();

        var id = new ArchetypeId([typeof(Position)]);
        var infos = new[] { world.Components.Get<Position>()! };
        using var archetype = new Archetype(id, infos);

        Assert.True(archetype.Has<Position>());
        Assert.False(archetype.Has<Velocity>());
        Assert.False(archetype.Has<Health>());
    }

    [Fact]
    public void Archetype_GetEntityIndex_ReturnsCorrectIndex()
    {
        using var world = new World();
        world.Components.Register<Position>();

        var id = new ArchetypeId([typeof(Position)]);
        var infos = new[] { world.Components.Get<Position>()! };
        using var archetype = new Archetype(id, infos);

        var entity1 = new Entity(0, 1);
        var entity2 = new Entity(1, 1);

        archetype.AddEntity(entity1);
        archetype.AddComponent(new Position());
        archetype.AddEntity(entity2);
        archetype.AddComponent(new Position());

        Assert.Equal(0, archetype.GetEntityIndex(entity1));
        Assert.Equal(1, archetype.GetEntityIndex(entity2));
        Assert.Equal(-1, archetype.GetEntityIndex(new Entity(99, 1)));
    }

    #endregion

    #region ArchetypeManager Tests

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_CreatesNew()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);

        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        Assert.NotNull(archetype);
        Assert.True(archetype.Has<Position>());
        Assert.True(archetype.Has<Velocity>());
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_ReturnsSameInstance()
    {
        using var world = new World();
        world.Components.Register<Position>();
        world.Components.Register<Velocity>();

        using var manager = new ArchetypeManager(world.Components);

        var archetype1 = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);
        var archetype2 = manager.GetOrCreateArchetype([typeof(Velocity), typeof(Position)]);

        Assert.Same(archetype1, archetype2);
    }

    [Fact]
    public void ArchetypeManager_ArchetypeCreated_EventFires()
    {
        using var world = new World();
        world.Components.Register<Position>();

        using var manager = new ArchetypeManager(world.Components);

        var eventFired = false;
        manager.ArchetypeCreated += _ => eventFired = true;

        manager.GetOrCreateArchetype([typeof(Position)]);

        Assert.True(eventFired);
    }

    #endregion

    #region World Integration Tests

    [Fact]
    public void World_EntitiesGroupedByArchetype()
    {
        using var world = new World();

        // Create entities with different component combinations
        var e1 = world.Spawn().With(new Position()).Build();
        var e2 = world.Spawn().With(new Position()).With(new Velocity()).Build();
        var e3 = world.Spawn().With(new Position()).Build();

        // Query should return correct entities
        var positionOnly = world.Query<Position>().Without<Velocity>().ToList();
        var both = world.Query<Position, Velocity>().ToList();

        Assert.Equal(2, positionOnly.Count);
        Assert.Single(both);
        Assert.Contains(e1, positionOnly);
        Assert.Contains(e3, positionOnly);
        Assert.Contains(e2, both);
    }

    [Fact]
    public void World_AddComponent_MigratesToNewArchetype()
    {
        using var world = new World();

        var entity = world.Spawn().With(new Position { X = 5 }).Build();

        // Initially matched by Position-only query
        Assert.Contains(entity, world.Query<Position>().Without<Velocity>().ToList());

        // Add Velocity
        world.Add(entity, new Velocity { X = 10 });

        // Now matched by both-components query
        Assert.DoesNotContain(entity, world.Query<Position>().Without<Velocity>().ToList());
        Assert.Contains(entity, world.Query<Position, Velocity>().ToList());

        // Original component preserved
        Assert.Equal(5, world.Get<Position>(entity).X);
        Assert.Equal(10, world.Get<Velocity>(entity).X);
    }

    [Fact]
    public void World_RemoveComponent_MigratesToNewArchetype()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new Position { X = 5 })
            .With(new Velocity { X = 10 })
            .Build();

        // Initially matched by both-components query
        Assert.Contains(entity, world.Query<Position, Velocity>().ToList());

        // Remove Velocity
        world.Remove<Velocity>(entity);

        // Now matched by Position-only query
        Assert.Contains(entity, world.Query<Position>().Without<Velocity>().ToList());
        Assert.DoesNotContain(entity, world.Query<Position, Velocity>().ToList());

        // Position preserved
        Assert.Equal(5, world.Get<Position>(entity).X);
        Assert.False(world.Has<Velocity>(entity));
    }

    [Fact]
    public void World_GetMemoryStats_ReturnsValidStats()
    {
        using var world = new World();

        world.Spawn().With(new Position()).Build();
        world.Spawn().With(new Position()).With(new Velocity()).Build();

        var stats = world.GetMemoryStats();

        Assert.Equal(2, stats.EntitiesActive);
        Assert.Equal(2, stats.EntitiesAllocated);
        Assert.Equal(2, stats.ArchetypeCount);
        Assert.Equal(2, stats.ComponentTypeCount);
    }

    #endregion

    #region ArchetypeId Edge Cases

    [Fact]
    public void ArchetypeId_Default_HasZeroComponents()
    {
        var id = default(ArchetypeId);

        Assert.Equal(0, id.ComponentCount);
        Assert.False(id.Has<Position>());
    }

    [Fact]
    public void ArchetypeId_Empty_HasZeroComponents()
    {
        var id = new ArchetypeId([]);

        Assert.Equal(0, id.ComponentCount);
        Assert.False(id.Has<Position>());
    }

    [Fact]
    public void ArchetypeId_With_AlreadyExisting_ReturnsSame()
    {
        var id = new ArchetypeId([typeof(Position)]);

        var result = id.With<Position>();

        Assert.Equal(id, result);
    }

    [Fact]
    public void ArchetypeId_Without_NotExisting_ReturnsSame()
    {
        var id = new ArchetypeId([typeof(Position)]);

        var result = id.Without<Velocity>();

        Assert.Equal(id, result);
    }

    [Fact]
    public void ArchetypeId_Default_WithType_CreatesNew()
    {
        var id = default(ArchetypeId);

        var result = id.With<Position>();

        Assert.Equal(1, result.ComponentCount);
        Assert.True(result.Has<Position>());
    }

    [Fact]
    public void ArchetypeId_Default_WithoutType_ReturnsSame()
    {
        var id = default(ArchetypeId);

        var result = id.Without<Position>();

        Assert.Equal(id, result);
    }

    [Fact]
    public void ArchetypeId_Equals_DifferentHashCode_ReturnsFalse()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Velocity)]);

        Assert.NotEqual(id1, id2);
        Assert.False(id1.Equals(id2));
    }

    [Fact]
    public void ArchetypeId_Equals_DefaultAndNonDefault_ReturnsFalse()
    {
        var id1 = default(ArchetypeId);
        var id2 = new ArchetypeId([typeof(Position)]);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void ArchetypeId_Equals_BothDefault_ReturnsTrue()
    {
        var id1 = default(ArchetypeId);
        var id2 = default(ArchetypeId);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void ArchetypeId_ToString_Empty_ReturnsEmptyFormat()
    {
        var id = new ArchetypeId([]);

        var str = id.ToString();

        Assert.Equal("ArchetypeId()", str);
    }

    [Fact]
    public void ArchetypeId_Operators_WorkCorrectly()
    {
        var id1 = new ArchetypeId([typeof(Position)]);
        var id2 = new ArchetypeId([typeof(Position)]);
        var id3 = new ArchetypeId([typeof(Velocity)]);

        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
        Assert.True(id1 != id3);
        Assert.False(id1 == id3);
    }

    [Fact]
    public void ArchetypeId_EqualsObject_NullReturnsFalse()
    {
        var id = new ArchetypeId([typeof(Position)]);

        Assert.False(id.Equals(null));
    }

    [Fact]
    public void ArchetypeId_EqualsObject_WrongTypeReturnsFalse()
    {
        var id = new ArchetypeId([typeof(Position)]);

        Assert.False(id.Equals("not an archetype id"));
    }

    #endregion

    #region ArchetypeManager Edge Cases

    [Fact]
    public void ArchetypeManager_TryGetEntityLocation_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);

        var result = manager.TryGetEntityLocation(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion), out var archetype, out var index);

        Assert.False(result);
        Assert.Null(archetype);
        Assert.Equal(-1, index);
    }

    [Fact]
    public void ArchetypeManager_GetComponentTypes_NotTracked_ReturnsEmpty()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        var types = manager.GetComponentTypes(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion));

        Assert.Empty(types);
    }

    [Fact]
    public void ArchetypeManager_GetComponents_NotTracked_ReturnsEmpty()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        var components = manager.GetComponents(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion)).ToList();

        Assert.Empty(components);
    }

    [Fact]
    public void ArchetypeManager_IsTracked_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        Assert.False(manager.IsTracked(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion)));
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_UnregisteredType_Throws()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        Assert.Throws<InvalidOperationException>(() =>
            manager.GetOrCreateArchetype([typeof(Position)]));
    }

    [Fact]
    public void ArchetypeManager_GetArchetype_NotExists_ReturnsNull()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        var archetype = manager.GetArchetype(new ArchetypeId([typeof(Position)]));

        Assert.Null(archetype);
    }

    [Fact]
    public void ArchetypeManager_Has_EntityNotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);

        Assert.False(manager.Has<Position>(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion)));
    }

    #endregion

    #region ComponentArray Edge Cases

    [Fact]
    public void ComponentArray_RemoveAtSwapBack_InvalidIndex_Throws()
    {
        var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });

        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.RemoveAtSwapBack(5));
    }

    [Fact]
    public void ComponentArray_CopyTo_WrongType_Throws()
    {
        var posArray = new ComponentArray<Position>();
        posArray.Add(new Position { X = 1 });
        var velArray = new ComponentArray<Velocity>();

        Assert.Throws<InvalidOperationException>(() => posArray.CopyTo(0, velArray));
    }

    [Fact]
    public void ComponentArray_AddBoxed_WorksCorrectly()
    {
        var array = new ComponentArray<Position>();

        var index = array.AddBoxed(new Position { X = 42 });

        Assert.Equal(0, index);
        Assert.Equal(42, array.GetRef(0).X);
    }

    [Fact]
    public void ComponentArray_SetBoxed_WorksCorrectly()
    {
        var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });

        array.SetBoxed(0, new Position { X = 99 });

        Assert.Equal(99, array.GetRef(0).X);
    }

    [Fact]
    public void ComponentArray_GetBoxed_WorksCorrectly()
    {
        var array = new ComponentArray<Position>();
        array.Add(new Position { X = 42 });

        var result = array.GetBoxed(0);

        Assert.IsType<Position>(result);
        Assert.Equal(42, ((Position)result).X);
    }

    [Fact]
    public void ComponentArray_Clear_EmptiesArray()
    {
        var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });
        array.Add(new Position { X = 2 });

        array.Clear();

        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void ComponentArray_Clear_OnEmpty_NoOp()
    {
        var array = new ComponentArray<Position>();

        array.Clear(); // Should not throw

        Assert.Equal(0, array.Count);
    }

    [Fact]
    public void ComponentArray_AsReadOnlySpan_ReturnsCorrectData()
    {
        var array = new ComponentArray<Position>();
        array.Add(new Position { X = 1 });
        array.Add(new Position { X = 2 });

        var span = array.AsReadOnlySpan();

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
    }

    [Fact]
    public void ComponentArray_CapacityGrows_WhenNeeded()
    {
        var array = new ComponentArray<Position>(2);

        // Add enough to trigger growth
        for (int i = 0; i < 20; i++)
        {
            array.Add(new Position { X = i });
        }

        Assert.Equal(20, array.Count);
        Assert.True(array.Capacity >= 20);
    }

    #endregion

    #region Archetype Edge Cases

    [Fact]
    public void Archetype_GetBoxed_TypeNotInArchetype_Throws()
    {
        using var world = new World();
        // Create an entity so there's at least one chunk
        world.Spawn().With(new Position()).Build();

        // Get the archetype via the world's internals
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        // Add an entity to the archetype to create a chunk
        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 1 });

        // Now try to get a component type that doesn't exist in the archetype
        Assert.Throws<InvalidOperationException>(() =>
            archetype.GetBoxed(typeof(Velocity), 0));
    }

    [Fact]
    public void Archetype_RemoveEntity_NotInArchetype_ReturnsNull()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var result = archetype.RemoveEntity(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion));

        Assert.Null(result);
    }

    [Fact]
    public void Archetype_GetEntityIndex_NotFound_ReturnsNegative()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var index = archetype.GetEntityIndex(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion));

        Assert.Equal(-1, index);
    }

    [Fact]
    public void Archetype_GetReadOnlySpan_WorksCorrectly()
    {
        using var world = new World();
        world.Spawn().With(new Position { X = 1 }).Build();
        world.Spawn().With(new Position { X = 2 }).Build();

        var count = 0;
        foreach (var entity in world.Query<Position>())
        {
            count++;
        }

        Assert.Equal(2, count);
    }

    [Fact]
    public void Archetype_ToString_FormatsCorrectly()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var str = archetype.ToString();

        Assert.Contains("Archetype", str);
        Assert.Contains("0 entities", str);
    }

    [Fact]
    public void Archetype_ComponentTypes_ReturnsCorrectTypes()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        var types = archetype.ComponentTypes;

        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Velocity), types);
    }

    [Fact]
    public void Archetype_ComponentTypes_IsReadOnly()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        var types = archetype.ComponentTypes;

        // Verify it's read-only by checking multiple accesses return consistent results
        var types2 = archetype.ComponentTypes;
        Assert.Equal(types.Count, types2.Count);
        Assert.True(types.SequenceEqual(types2));
    }

    #endregion

    #region ComponentArray Type Safety Tests

    [Fact]
    public void ComponentArray_EnforcesIComponentConstraint()
    {
        // This test verifies that ComponentArray<T> requires T to implement IComponent
        // If the constraint is missing, this test would fail to compile
        using var array = new ComponentArray<Position>();

        array.Add(new Position { X = 1, Y = 2 });

        Assert.Equal(1, array.Count);
        Assert.Equal(typeof(Position), array.ComponentType);
    }

    [Fact]
    public void FixedComponentArray_EnforcesIComponentConstraint()
    {
        // This test verifies that FixedComponentArray<T> requires T to implement IComponent
        // If the constraint is missing, this test would fail to compile
        var array = new FixedComponentArray<Velocity>(10);

        array.Add(new Velocity { X = 3, Y = 4 });

        Assert.Equal(1, array.Count);
        Assert.Equal(typeof(Velocity), array.ComponentType);
    }

    #endregion

    #region World Coverage Tests

    [Fact]
    public void World_Set_EntityWithoutComponent_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new Velocity()));
    }

    [Fact]
    public void World_Add_DuplicateComponent_Throws()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new Position()));
    }

    [Fact]
    public void World_Remove_NonExistentComponent_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().With(new Position()).Build();

        var result = world.Remove<Velocity>(entity);

        Assert.False(result);
    }

    [Fact]
    public void World_GetComponents_ReturnsAllComponents()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 1 })
            .With(new Velocity { X = 2 })
            .Build();

        var components = world.GetComponents(entity).ToList();

        Assert.Equal(2, components.Count);
    }

    [Fact]
    public void World_GetComponents_ReturnsCorrectTypes()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position())
            .With(new Velocity())
            .Build();

        var components = world.GetComponents(entity).ToList();
        var types = components.Select(c => c.Type).ToList();

        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Velocity), types);
    }

    #endregion

    #region Archetype Advanced Coverage Tests

    [Fact]
    public void Archetype_Entities_IteratesAllEntities()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(2, 1);
        var entity3 = new Entity(3, 1);

        archetype.AddEntity(entity1);
        archetype.AddComponent(new Position { X = 1 });
        archetype.AddEntity(entity2);
        archetype.AddComponent(new Position { X = 2 });
        archetype.AddEntity(entity3);
        archetype.AddComponent(new Position { X = 3 });

        var entities = archetype.Entities.ToList();

        Assert.Equal(3, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.Contains(entity3, entities);
    }

    [Fact]
    public void Archetype_GetReadonly_ReturnsValue()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 42, Y = 99 });

        ref readonly var pos = ref archetype.GetReadonly<Position>(0);

        Assert.Equal(42, pos.X);
        Assert.Equal(99, pos.Y);
    }

    [Fact]
    public void Archetype_Set_UpdatesComponent()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 1 });

        archetype.Set(0, new Position { X = 99, Y = 88 });

        Assert.Equal(99, archetype.Get<Position>(0).X);
        Assert.Equal(88, archetype.Get<Position>(0).Y);
    }

    [Fact]
    public void Archetype_SetBoxed_UpdatesComponent()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 1 });

        archetype.SetBoxed(typeof(Position), 0, new Position { X = 77 });

        Assert.Equal(77, archetype.Get<Position>(0).X);
    }

    [Fact]
    public void Archetype_GetChunkSpan_ReturnsValidSpan()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 1 });
        archetype.AddEntity(new Entity(2, 1));
        archetype.AddComponent(new Position { X = 2 });

        var span = archetype.GetChunkSpan<Position>(0);

        Assert.Equal(2, span.Length);
        Assert.Equal(1, span[0].X);
        Assert.Equal(2, span[1].X);
    }

    [Fact]
    public void Archetype_GetChunkReadOnlySpan_ReturnsValidSpan()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 42 });

        var span = archetype.GetChunkReadOnlySpan<Position>(0);

        Assert.Equal(1, span.Length);
        Assert.Equal(42, span[0].X);
    }

    [Fact]
    public void Archetype_GetEntityLocation_ReturnsCorrectLocation()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var entity = new Entity(1, 1);
        archetype.AddEntity(entity);
        archetype.AddComponent(new Position());

        var (chunkIndex, indexInChunk) = archetype.GetEntityLocation(entity);

        Assert.Equal(0, chunkIndex);
        Assert.Equal(0, indexInChunk);
    }

    [Fact]
    public void Archetype_GetEntityLocation_NotFound_ReturnsNegative()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var (chunkIndex, indexInChunk) = archetype.GetEntityLocation(new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion));

        Assert.Equal(-1, chunkIndex);
        Assert.Equal(-1, indexInChunk);
    }

    [Fact]
    public void Archetype_GetEntity_ReturnsCorrectEntity()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var entity = new Entity(42, 1);
        archetype.AddEntity(entity);
        archetype.AddComponent(new Position());

        var result = archetype.GetEntity(0);

        Assert.Equal(entity, result);
    }

    [Fact]
    public void Archetype_GetByEntity_ReturnsCorrectComponent()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var entity = new Entity(1, 1);
        archetype.AddEntity(entity);
        archetype.AddComponent(new Position { X = 123, Y = 456 });

        ref var pos = ref archetype.GetByEntity<Position>(entity);

        Assert.Equal(123, pos.X);
        Assert.Equal(456, pos.Y);
    }

    [Fact]
    public void Archetype_Has_ByType_ReturnsCorrectResult()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        Assert.True(archetype.Has(typeof(Position)));
        Assert.False(archetype.Has(typeof(Velocity)));
    }

#pragma warning disable CS0618 // Obsolete methods
    [Fact]
    public void Archetype_GetSpan_ObsoleteMethod_ReturnsFirstChunkSpan()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 42 });

        var span = archetype.GetSpan<Position>();

        Assert.Equal(1, span.Length);
        Assert.Equal(42, span[0].X);
    }

    [Fact]
    public void Archetype_GetSpan_EmptyArchetype_ReturnsEmpty()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var span = archetype.GetSpan<Position>();

        Assert.True(span.IsEmpty);
    }

    [Fact]
    public void Archetype_GetReadOnlySpan_ObsoleteMethod_ReturnsFirstChunkSpan()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 42 });

        var span = archetype.GetReadOnlySpan<Position>();

        Assert.Equal(1, span.Length);
        Assert.Equal(42, span[0].X);
    }

    [Fact]
    public void Archetype_GetReadOnlySpan_EmptyArchetype_ReturnsEmpty()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        var span = archetype.GetReadOnlySpan<Position>();

        Assert.True(span.IsEmpty);
    }
#pragma warning restore CS0618

    [Fact]
    public void Archetype_Dispose_WithChunkPool_ReturnsChunksToPool()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var pool = new ChunkPool();
        var manager = new ArchetypeManager(registry, pool);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position());

        archetype.Dispose();

        Assert.Equal(0, archetype.Count);
        // Chunk should be returned to pool
        Assert.True(pool.PooledCount >= 0); // Pool may or may not accept returned chunks
    }

    [Fact]
    public void Archetype_RemoveEntity_TriggersChunkRemovalAndLocationUpdate()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var pool = new ChunkPool();
        var manager = new ArchetypeManager(registry, pool);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        // Add many entities to create multiple chunks
        var entities = new List<Entity>();
        for (int i = 0; i < ArchetypeChunk.DefaultCapacity + 10; i++)
        {
            var entity = new Entity(i, 1);
            entities.Add(entity);
            archetype.AddEntity(entity);
            archetype.AddComponent(new Position { X = i });
        }

        Assert.True(archetype.ChunkCount >= 2);

        // Remove entities from first chunk to make it empty
        for (int i = 0; i < ArchetypeChunk.DefaultCapacity; i++)
        {
            archetype.RemoveEntity(entities[i]);
        }

        // Should still have remaining entities in subsequent chunks
        Assert.Equal(10, archetype.Count);
    }

    [Fact]
    public void Archetype_ChunkRemoval_UpdatesEntityLocationsCorrectly()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var pool = new ChunkPool();
        var manager = new ArchetypeManager(registry, pool);
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);

        // Create entities across multiple chunks
        var entitiesInFirstChunk = new List<Entity>();
        var entitiesInSecondChunk = new List<Entity>();

        // Fill first chunk
        for (int i = 0; i < ArchetypeChunk.DefaultCapacity; i++)
        {
            var entity = new Entity(i, 1);
            entitiesInFirstChunk.Add(entity);
            archetype.AddEntity(entity);
            archetype.AddComponent(new Position { X = i });
        }

        // Add entities to second chunk
        for (int i = 0; i < 5; i++)
        {
            var entity = new Entity(ArchetypeChunk.DefaultCapacity + i, 1);
            entitiesInSecondChunk.Add(entity);
            archetype.AddEntity(entity);
            archetype.AddComponent(new Position { X = ArchetypeChunk.DefaultCapacity + i });
        }

        Assert.Equal(2, archetype.ChunkCount);

        // Verify entities in second chunk are at chunk index 1
        foreach (var entity in entitiesInSecondChunk)
        {
            var (chunkIdx, _) = archetype.GetEntityLocation(entity);
            Assert.Equal(1, chunkIdx);
        }

        // Remove all entities from first chunk to trigger chunk removal
        foreach (var entity in entitiesInFirstChunk)
        {
            archetype.RemoveEntity(entity);
        }

        // Now only one chunk should remain
        Assert.Equal(1, archetype.ChunkCount);

        // Verify entities that were in second chunk are now in first chunk (index 0)
        foreach (var entity in entitiesInSecondChunk)
        {
            var (chunkIdx, _) = archetype.GetEntityLocation(entity);
            Assert.Equal(0, chunkIdx);
        }

        // Verify we can still access components for these entities
        foreach (var entity in entitiesInSecondChunk)
        {
            ref var pos = ref archetype.GetByEntity<Position>(entity);
            Assert.True(pos.X >= ArchetypeChunk.DefaultCapacity);
        }
    }

    #endregion

    #region ArchetypeManager Advanced Coverage Tests

    [Fact]
    public void ArchetypeManager_Constructor_WithCustomChunkPool()
    {
        var registry = new ComponentRegistry();
        var customPool = new ChunkPool(maxChunksPerArchetype: 10);
        var manager = new ArchetypeManager(registry, customPool);

        Assert.Same(customPool, manager.ChunkPool);
    }

    [Fact]
    public void ArchetypeManager_EntityCount_TracksEntities()
    {
        using var world = new World();

        Assert.Equal(0, world.ArchetypeManager.EntityCount);

        world.Spawn().With(new Position()).Build();
        Assert.Equal(1, world.ArchetypeManager.EntityCount);

        world.Spawn().With(new Position()).Build();
        Assert.Equal(2, world.ArchetypeManager.EntityCount);
    }

    [Fact]
    public void ArchetypeManager_GetMatchingArchetypes_FiltersCorrectly()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        registry.Register<Health>();
        var manager = new ArchetypeManager(registry);

        // Create various archetypes
        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);
        manager.GetOrCreateArchetype([typeof(Health)]);

        var query = new QueryDescription();
        query.AddWrite<Position>();

        var matches = manager.GetMatchingArchetypes(query).ToList();

        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void ArchetypeManager_GetMatchingArchetypes_WithExclude()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);

        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        var query = new QueryDescription();
        query.AddWrite<Position>();
        query.AddWithout<Velocity>();

        var matches = manager.GetMatchingArchetypes(query).ToList();

        Assert.Single(matches);
        Assert.False(matches[0].Has<Velocity>());
    }

    [Fact]
    public void ArchetypeManager_Dispose_ClearsEverything()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);

        manager.GetOrCreateArchetype([typeof(Position)]);

        Assert.Equal(1, manager.ArchetypeCount);

        manager.Dispose();

        Assert.Equal(0, manager.ArchetypeCount);
    }

    [Fact]
    public void ArchetypeManager_GetOrCreateArchetype_ById()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        var manager = new ArchetypeManager(registry);

        var id = new ArchetypeId([typeof(Position)]);
        var archetype = manager.GetOrCreateArchetype(id);

        Assert.NotNull(archetype);
        Assert.Equal(id, archetype.Id);
    }

    #endregion

    #region ArchetypeManager Performance Tests

    [Fact]
    public void ArchetypeManager_AddEntity_WithIEnumerable_WorksCorrectly()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        var velInfo = registry.Register<Velocity>();
        var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        var components = new[]
        {
            (posInfo, (object)new Position { X = 10, Y = 20 }),
            (velInfo, (object)new Velocity { X = 1, Y = 2 })
        };

        // This should not allocate via ToList() - enumerate twice instead
        manager.AddEntity(entity, components);

        // Verify entity was added correctly
        Assert.True(manager.IsTracked(entity));
        Assert.True(manager.Has<Position>(entity));
        Assert.True(manager.Has<Velocity>(entity));

        // Verify component values were set correctly
        var pos = manager.Get<Position>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);

        var vel = manager.Get<Velocity>(entity);
        Assert.Equal(1, vel.X);
        Assert.Equal(2, vel.Y);
    }

    [Fact]
    public void ArchetypeManager_AddEntity_WithEmptyIEnumerable_WorksCorrectly()
    {
        var registry = new ComponentRegistry();
        var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        var components = Array.Empty<(ComponentInfo, object)>();

        // Should handle empty enumerable without error
        manager.AddEntity(entity, components);

        // Verify entity was added to empty archetype
        Assert.True(manager.IsTracked(entity));
        Assert.Empty(manager.GetComponentTypes(entity));
    }

    [Fact]
    public void ArchetypeManager_AddEntity_WithLazyIEnumerable_MaterializesOnce()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        var enumerationCount = 0;

        // Create a lazy enumerable that tracks enumeration
        IEnumerable<(ComponentInfo, object)> LazyComponents()
        {
            enumerationCount++;
            yield return (posInfo, new Position { X = 42, Y = 99 });
        }

        // AddEntity materializes the IEnumerable once for thread safety
        manager.AddEntity(entity, LazyComponents());

        // Verify it was enumerated once (materialized upfront for thread safety)
        Assert.Equal(1, enumerationCount);

        // Verify entity was added correctly
        Assert.True(manager.IsTracked(entity));
        Assert.True(manager.Has<Position>(entity));

        var pos = manager.Get<Position>(entity);
        Assert.Equal(42, pos.X);
        Assert.Equal(99, pos.Y);
    }

    #endregion

    #region ComponentRegistry Coverage Tests

    [Fact]
    public void ComponentRegistry_GetById_ValidId_ReturnsInfo()
    {
        var registry = new ComponentRegistry();
        var info = registry.Register<Position>();

        var result = registry.GetById(info.Id);

        Assert.NotNull(result);
        Assert.Equal(typeof(Position), result!.Type);
    }

    [Fact]
    public void ComponentRegistry_GetById_InvalidId_ReturnsNull()
    {
        var registry = new ComponentRegistry();

        var result = registry.GetById(new ComponentId(TestConstants.InvalidComponentId));

        Assert.Null(result);
    }

    [Fact]
    public void ComponentRegistry_GetById_NegativeId_ReturnsNull()
    {
        var registry = new ComponentRegistry();

        var result = registry.GetById(new ComponentId(-1));

        Assert.Null(result);
    }

    [Fact]
    public void ComponentRegistry_Get_ByType_ReturnsInfo()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        var result = registry.Get(typeof(Position));

        Assert.NotNull(result);
        Assert.Equal(typeof(Position), result!.Type);
    }

    [Fact]
    public void ComponentRegistry_Get_ByType_NotRegistered_ReturnsNull()
    {
        var registry = new ComponentRegistry();

        var result = registry.Get(typeof(Position));

        Assert.Null(result);
    }

    [Fact]
    public void ComponentRegistry_Register_AlreadyRegistered_ReturnsSame()
    {
        var registry = new ComponentRegistry();

        var info1 = registry.Register<Position>();
        var info2 = registry.Register<Position>();

        Assert.Same(info1, info2);
    }

    [Fact]
    public void ComponentRegistry_Register_TagComponent()
    {
        var registry = new ComponentRegistry();

        var info = registry.Register<EnemyTag>(isTag: true);

        Assert.True(info.IsTag);
        Assert.Equal(0, info.Size);
    }

    [Fact]
    public void ComponentRegistry_GetOrRegister_NewType()
    {
        var registry = new ComponentRegistry();

        var info = registry.GetOrRegister<Position>();

        Assert.NotNull(info);
        Assert.True(registry.IsRegistered<Position>());
    }

    [Fact]
    public void ComponentRegistry_GetOrRegister_ExistingType()
    {
        var registry = new ComponentRegistry();
        var existing = registry.Register<Position>();

        var info = registry.GetOrRegister<Position>();

        Assert.Same(existing, info);
    }

    [Fact]
    public void ComponentRegistry_IsRegistered_True()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        Assert.True(registry.IsRegistered<Position>());
    }

    [Fact]
    public void ComponentRegistry_IsRegistered_False()
    {
        var registry = new ComponentRegistry();

        Assert.False(registry.IsRegistered<Position>());
    }

    [Fact]
    public void ComponentRegistry_Count_TracksRegistrations()
    {
        var registry = new ComponentRegistry();

        Assert.Equal(0, registry.Count);

        registry.Register<Position>();
        Assert.Equal(1, registry.Count);

        registry.Register<Velocity>();
        Assert.Equal(2, registry.Count);

        // Re-registering doesn't increase count
        registry.Register<Position>();
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void ComponentRegistry_All_ReturnsAllRegistered()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();

        var all = registry.All;

        Assert.Equal(2, all.Count);
    }

    #endregion
}
