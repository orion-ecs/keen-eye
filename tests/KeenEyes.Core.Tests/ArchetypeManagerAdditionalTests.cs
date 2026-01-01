using Health = KeenEyes.Tests.TestHealth;
using Position = KeenEyes.Tests.TestPosition;
using Velocity = KeenEyes.Tests.TestVelocity;

namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for ArchetypeManager to improve coverage.
/// </summary>
public class ArchetypeManagerAdditionalTests
{
    #region Clear Method Tests

    [Fact]
    public void ArchetypeManager_Clear_RemovesAllArchetypes()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        // Create archetypes
        manager.GetOrCreateArchetype([typeof(Position)]);
        manager.GetOrCreateArchetype([typeof(Velocity)]);
        manager.GetOrCreateArchetype([typeof(Position), typeof(Velocity)]);

        Assert.Equal(3, manager.ArchetypeCount);

        manager.Clear();

        Assert.Equal(0, manager.ArchetypeCount);
    }

    [Fact]
    public void ArchetypeManager_Clear_RemovesAllEntities()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        // Add entities
        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(2, 1);

        manager.AddEntity(entity1, [(posInfo, new Position { X = 1, Y = 2 })]);
        manager.AddEntity(entity2, [(posInfo, new Position { X = 3, Y = 4 })]);

        Assert.Equal(2, manager.EntityCount);

        manager.Clear();

        Assert.Equal(0, manager.EntityCount);
    }

    [Fact]
    public void ArchetypeManager_Clear_PreservesChunkPool()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        var pool = new ChunkPool();
        using var manager = new ArchetypeManager(registry, pool);

        // Create archetype with entities (will allocate chunks)
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);
        for (int i = 0; i < 10; i++)
        {
            archetype.AddEntity(new Entity(i, 1));
            archetype.AddComponent(new Position { X = i, Y = i });
        }

        // Pool should still be usable after clear
        manager.Clear();

        // Create new archetype should reuse chunks from pool
        var newArchetype = manager.GetOrCreateArchetype([typeof(Position)]);
        Assert.NotNull(newArchetype);
    }

    [Fact]
    public void ArchetypeManager_Clear_LeavesManagerUsable()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        // Add some data
        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        manager.Clear();

        // Should be able to add new entities after clear
        var newEntity = new Entity(2, 1);
        manager.AddEntity(newEntity, [(posInfo, new Position { X = 3, Y = 4 })]);

        Assert.Equal(1, manager.EntityCount);
        Assert.True(manager.IsTracked(newEntity));
    }

    [Fact]
    public void ArchetypeManager_Clear_OnEmpty_NoOp()
    {
        var registry = new ComponentRegistry();
        using var manager = new ArchetypeManager(registry);

        // Clear empty manager should not throw
        manager.Clear();

        Assert.Equal(0, manager.ArchetypeCount);
        Assert.Equal(0, manager.EntityCount);
    }

    [Fact]
    public void ArchetypeManager_Clear_DisposesArchetypes()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        var pool = new ChunkPool();
        using var manager = new ArchetypeManager(registry, pool);

        // Create archetype with entities
        var archetype = manager.GetOrCreateArchetype([typeof(Position)]);
        archetype.AddEntity(new Entity(1, 1));
        archetype.AddComponent(new Position { X = 1, Y = 2 });

        var chunkCountBefore = archetype.ChunkCount;
        Assert.True(chunkCountBefore > 0);

        manager.Clear();

        // Archetype should be disposed (chunks returned to pool)
        // Note: We can't directly verify archetype disposal as it's disposed,
        // but we can verify the manager state
        Assert.Equal(0, manager.ArchetypeCount);
    }

    [Fact]
    public void ArchetypeManager_ClearThenDispose_Works()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        var manager = new ArchetypeManager(registry);
        manager.GetOrCreateArchetype([typeof(Position)]);

        manager.Clear();
        manager.Dispose(); // Should not throw

        Assert.Equal(0, manager.ArchetypeCount);
    }

    #endregion

    #region AddComponentBoxed Tests

    [Fact]
    public void ArchetypeManager_AddComponentBoxed_MigratesEntity()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        Assert.False(manager.Has<Velocity>(entity));

        // Add boxed Velocity component
        manager.AddComponentBoxed(entity, typeof(Velocity), new Velocity { X = 3, Y = 4 });

        Assert.True(manager.Has<Velocity>(entity));
        var vel = manager.Get<Velocity>(entity);
        Assert.Equal(3, vel.X);
        Assert.Equal(4, vel.Y);
    }

    [Fact]
    public void ArchetypeManager_AddComponentBoxed_AlreadyHas_Throws()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);
        manager.AddComponent(entity, new Velocity { X = 3, Y = 4 });

        Assert.Throws<InvalidOperationException>(() =>
            manager.AddComponentBoxed(entity, typeof(Velocity), new Velocity { X = 5, Y = 6 }));
    }

    [Fact]
    public void ArchetypeManager_AddComponentBoxed_NotTracked_Throws()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            manager.AddComponentBoxed(entity, typeof(Position), new Position()));
    }

    #endregion

    #region SetBoxed Tests

    [Fact]
    public void ArchetypeManager_SetBoxed_UpdatesComponent()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        manager.SetBoxed(entity, typeof(Position), new Position { X = 10, Y = 20 });

        var pos = manager.Get<Position>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    [Fact]
    public void ArchetypeManager_SetBoxed_NotTracked_Throws()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            manager.SetBoxed(entity, typeof(Position), new Position()));
    }

    #endregion

    #region RemoveComponent by Type Tests

    [Fact]
    public void ArchetypeManager_RemoveComponent_ByType_RemovesComponent()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        var velInfo = registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [
            (posInfo, new Position { X = 1, Y = 2 }),
            (velInfo, new Velocity { X = 3, Y = 4 })
        ]);

        Assert.True(manager.Has<Velocity>(entity));

        var removed = manager.RemoveComponent(entity, typeof(Velocity));

        Assert.True(removed);
        Assert.False(manager.Has<Velocity>(entity));
        Assert.True(manager.Has<Position>(entity)); // Position should remain
    }

    [Fact]
    public void ArchetypeManager_RemoveComponent_ByType_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        var removed = manager.RemoveComponent(entity, typeof(Position));

        Assert.False(removed);
    }

    [Fact]
    public void ArchetypeManager_RemoveComponent_ByType_DoesNotHave_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        var removed = manager.RemoveComponent(entity, typeof(Velocity));

        Assert.False(removed);
    }

    #endregion

    #region Has by Type Tests

    [Fact]
    public void ArchetypeManager_Has_ByType_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        Assert.True(manager.Has(entity, typeof(Position)));
    }

    [Fact]
    public void ArchetypeManager_Has_ByType_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        Assert.False(manager.Has(entity, typeof(Velocity)));
    }

    [Fact]
    public void ArchetypeManager_Has_ByType_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        Assert.False(manager.Has(entity, typeof(Position)));
    }

    #endregion

    #region TryGetEntityLocation Tests

    [Fact]
    public void ArchetypeManager_TryGetEntityLocation_Tracked_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position())]);

        var found = manager.TryGetEntityLocation(entity, out var archetype, out var index);

        Assert.True(found);
        Assert.NotNull(archetype);
        Assert.Equal(0, index);
    }

    [Fact]
    public void ArchetypeManager_TryGetEntityLocation_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        var found = manager.TryGetEntityLocation(entity, out var archetype, out var index);

        Assert.False(found);
        Assert.Null(archetype);
        Assert.Equal(-1, index);
    }

    #endregion

    #region GetEntityLocation Tests

    [Fact]
    public void ArchetypeManager_GetEntityLocation_Tracked_ReturnsLocation()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position())]);

        var (archetype, index) = manager.GetEntityLocation(entity);

        Assert.NotNull(archetype);
        Assert.Equal(0, index);
    }

    [Fact]
    public void ArchetypeManager_GetEntityLocation_NotTracked_Throws()
    {
        var registry = new ComponentRegistry();
        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            manager.GetEntityLocation(entity));
    }

    #endregion

    #region RemoveEntity Tests

    [Fact]
    public void ArchetypeManager_RemoveEntity_Tracked_ReturnsTrue()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position())]);

        Assert.True(manager.IsTracked(entity));

        var removed = manager.RemoveEntity(entity);

        Assert.True(removed);
        Assert.False(manager.IsTracked(entity));
    }

    [Fact]
    public void ArchetypeManager_RemoveEntity_NotTracked_ReturnsFalse()
    {
        var registry = new ComponentRegistry();
        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        var removed = manager.RemoveEntity(entity);

        Assert.False(removed);
    }

    [Fact]
    public void ArchetypeManager_RemoveEntity_UpdatesSwappedEntityLocation()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity1 = new Entity(1, 1);
        var entity2 = new Entity(2, 1);
        var entity3 = new Entity(3, 1);

        manager.AddEntity(entity1, [(posInfo, new Position { X = 1 })]);
        manager.AddEntity(entity2, [(posInfo, new Position { X = 2 })]);
        manager.AddEntity(entity3, [(posInfo, new Position { X = 3 })]);

        // Remove entity1 (should swap entity3 into its place)
        manager.RemoveEntity(entity1);

        // entity3 should now be at index 0
        var (_, index) = manager.GetEntityLocation(entity3);
        Assert.Equal(0, index);

        // entity3's component data should still be intact
        var pos = manager.Get<Position>(entity3);
        Assert.Equal(3, pos.X);
    }

    #endregion

    #region AddComponent Tests

    [Fact]
    public void ArchetypeManager_AddComponent_Generic_MigratesEntity()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        manager.AddComponent(entity, new Velocity { X = 3, Y = 4 });

        Assert.True(manager.Has<Velocity>(entity));
        var vel = manager.Get<Velocity>(entity);
        Assert.Equal(3, vel.X);
        Assert.Equal(4, vel.Y);
    }

    [Fact]
    public void ArchetypeManager_AddComponent_Generic_AlreadyHas_Throws()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        Assert.Throws<InvalidOperationException>(() =>
            manager.AddComponent(entity, new Position { X = 3, Y = 4 }));
    }

    [Fact]
    public void ArchetypeManager_AddComponent_Generic_NotTracked_Throws()
    {
        var registry = new ComponentRegistry();
        registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(999, 1);

        Assert.Throws<InvalidOperationException>(() =>
            manager.AddComponent(entity, new Position()));
    }

    #endregion

    #region Set Tests

    [Fact]
    public void ArchetypeManager_Set_Generic_UpdatesComponent()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        manager.Set(entity, new Position { X = 10, Y = 20 });

        var pos = manager.Get<Position>(entity);
        Assert.Equal(10, pos.X);
        Assert.Equal(20, pos.Y);
    }

    #endregion

    #region Get Tests

    [Fact]
    public void ArchetypeManager_Get_Generic_ReturnsComponentRef()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [(posInfo, new Position { X = 1, Y = 2 })]);

        ref var pos = ref manager.Get<Position>(entity);

        Assert.Equal(1, pos.X);
        Assert.Equal(2, pos.Y);

        // Verify it's a reference (can modify)
        pos.X = 99;

        var pos2 = manager.Get<Position>(entity);
        Assert.Equal(99, pos2.X);
    }

    #endregion

    #region GetComponentTypes Tests

    [Fact]
    public void ArchetypeManager_GetComponentTypes_ReturnsAllTypes()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        var velInfo = registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [
            (posInfo, new Position()),
            (velInfo, new Velocity())
        ]);

        var types = manager.GetComponentTypes(entity).ToList();

        Assert.Equal(2, types.Count);
        Assert.Contains(typeof(Position), types);
        Assert.Contains(typeof(Velocity), types);
    }

    #endregion

    #region GetComponents Tests

    [Fact]
    public void ArchetypeManager_GetComponents_ReturnsAllComponents()
    {
        var registry = new ComponentRegistry();
        var posInfo = registry.Register<Position>();
        var velInfo = registry.Register<Velocity>();

        using var manager = new ArchetypeManager(registry);

        var entity = new Entity(1, 1);
        manager.AddEntity(entity, [
            (posInfo, new Position { X = 1, Y = 2 }),
            (velInfo, new Velocity { X = 3, Y = 4 })
        ]);

        var components = manager.GetComponents(entity).ToList();

        Assert.Equal(2, components.Count);

        var posComponent = components.First(c => c.Type == typeof(Position));
        var velComponent = components.First(c => c.Type == typeof(Velocity));

        var pos = (Position)posComponent.Value;
        var vel = (Velocity)velComponent.Value;

        Assert.Equal(1, pos.X);
        Assert.Equal(2, pos.Y);
        Assert.Equal(3, vel.X);
        Assert.Equal(4, vel.Y);
    }

    #endregion
}
