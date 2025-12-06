namespace KeenEyes.Tests;

/// <summary>
/// Tests for World.Remove&lt;T&gt;(entity) component removal.
/// </summary>
public class RemoveComponentTests
{
    #region Success Path Tests

    [Fact]
    public void Remove_RemovesComponentFromEntity_ReturnsTrue()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        Assert.True(world.Has<TestPosition>(entity));

        bool removed = world.Remove<TestPosition>(entity);

        Assert.True(removed);
        Assert.False(world.Has<TestPosition>(entity));
    }

    [Fact]
    public void Remove_EntityStillAlive_AfterComponentRemoval()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        world.Remove<TestPosition>(entity);

        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Remove_CanRemoveMultipleComponentsFromSameEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 2f })
            .With(new TestVelocity { X = 3f, Y = 4f })
            .With(new TestHealth { Current = 50, Max = 100 })
            .Build();

        Assert.True(world.Remove<TestPosition>(entity));
        Assert.True(world.Remove<TestVelocity>(entity));
        Assert.True(world.Remove<TestHealth>(entity));

        Assert.False(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));
        Assert.False(world.Has<TestHealth>(entity));
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Remove_CanRemoveSomeComponentsWhileKeepingOthers()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 2f })
            .With(new TestVelocity { X = 3f, Y = 4f })
            .Build();

        Assert.True(world.Remove<TestVelocity>(entity));

        Assert.True(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(1f, position.X);
        Assert.Equal(2f, position.Y);
    }

    [Fact]
    public void Remove_SameComponentTypeFromDifferentEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn().With(new TestPosition { X = 10f, Y = 10f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 20f, Y = 20f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 30f, Y = 30f }).Build();

        Assert.True(world.Remove<TestPosition>(entity2));

        Assert.True(world.Has<TestPosition>(entity1));
        Assert.False(world.Has<TestPosition>(entity2));
        Assert.True(world.Has<TestPosition>(entity3));

        Assert.Equal(10f, world.Get<TestPosition>(entity1).X);
        Assert.Equal(30f, world.Get<TestPosition>(entity3).X);
    }

    #endregion

    #region Idempotent Behavior Tests

    [Fact]
    public void Remove_ReturnsFalse_WhenComponentNotPresent()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        bool removed = world.Remove<TestVelocity>(entity);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenCalledTwice()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(world.Remove<TestPosition>(entity));
        Assert.False(world.Remove<TestPosition>(entity));
    }

    [Fact]
    public void Remove_Idempotent_MultipleCallsReturnFalse()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(world.Remove<TestPosition>(entity));
        Assert.False(world.Remove<TestPosition>(entity));
        Assert.False(world.Remove<TestPosition>(entity));
        Assert.False(world.Remove<TestPosition>(entity));
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenEntityNotAlive()
    {
        using var world = new World();
        var deadEntity = new Entity(999, 1);

        bool removed = world.Remove<TestPosition>(deadEntity);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenEntityDespawned()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();
        world.Despawn(entity);

        bool removed = world.Remove<TestPosition>(entity);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_ReturnsFalse_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        bool removed = world.Remove<TestPosition>(Entity.Null);

        Assert.False(removed);
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenComponentTypeNotRegistered()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        // TestPosition is not registered
        bool removed = world.Remove<TestPosition>(entity);

        Assert.False(removed);
    }

    #endregion

    #region Query Integration Tests

    [Fact]
    public void Remove_EntityNotMatchedByQueryAfterRemoval()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        // Query for entities with Position and Velocity - should match
        var queryBefore = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Single(queryBefore);

        // Remove Velocity
        world.Remove<TestVelocity>(entity);

        // Query should no longer match
        var queryAfter = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Empty(queryAfter);
    }

    [Fact]
    public void Remove_EntityStillMatchedByQueryForRemainingComponents()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        world.Remove<TestVelocity>(entity);

        // Query for just Position should still match
        var posQuery = world.Query<TestPosition>().ToList();
        Assert.Single(posQuery);
        Assert.Equal(entity, posQuery[0]);
    }

    [Fact]
    public void Remove_MultipleEntities_QueryMatchesCorrectly()
    {
        using var world = new World();

        // Create three entities with Position and Velocity
        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .With(new TestVelocity { X = 2f, Y = 2f })
            .Build();
        var entity3 = world.Spawn()
            .With(new TestPosition { X = 3f, Y = 3f })
            .With(new TestVelocity { X = 3f, Y = 3f })
            .Build();

        // Remove Velocity from entity2
        world.Remove<TestVelocity>(entity2);

        // Query for Position+Velocity should match entity1 and entity3
        var query = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Equal(2, query.Count);
        Assert.Contains(entity1, query);
        Assert.Contains(entity3, query);
        Assert.DoesNotContain(entity2, query);

        // Query for just Position should match all three
        var posQuery = world.Query<TestPosition>().ToList();
        Assert.Equal(3, posQuery.Count);
    }

    [Fact]
    public void Remove_QueryWithThreeComponents_NoLongerMatchesAfterRemovingOne()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        // Query for three components - should match
        var queryBefore = world.Query<TestPosition, TestVelocity, TestHealth>().ToList();
        Assert.Single(queryBefore);

        // Remove one component
        world.Remove<TestHealth>(entity);

        // Query for three components should no longer match
        var queryAfter = world.Query<TestPosition, TestVelocity, TestHealth>().ToList();
        Assert.Empty(queryAfter);

        // Query for remaining two should still match
        var queryTwo = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Single(queryTwo);
    }

    [Fact]
    public void Remove_EntityMatchesQueryWithWithout_AfterComponentRemoval()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        // Query for Position without Velocity
        var queryBefore = world.Query<TestPosition>().Without<TestVelocity>().ToList();
        Assert.Single(queryBefore);
        Assert.Equal(entity2, queryBefore[0]);

        // Remove Velocity from entity1
        world.Remove<TestVelocity>(entity1);

        // Now both entities should match
        var queryAfter = world.Query<TestPosition>().Without<TestVelocity>().ToList();
        Assert.Equal(2, queryAfter.Count);
    }

    #endregion

    #region Add/Remove Cycle Tests

    [Fact]
    public void Remove_CanAddComponentAgainAfterRemoval()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 10f, Y = 20f })
            .Build();

        world.Remove<TestPosition>(entity);
        Assert.False(world.Has<TestPosition>(entity));

        world.Add(entity, new TestPosition { X = 100f, Y = 200f });
        Assert.True(world.Has<TestPosition>(entity));

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
    }

    [Fact]
    public void Remove_AddRemoveCycle_WorksMultipleTimes()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        for (int i = 0; i < 5; i++)
        {
            world.Add(entity, new TestPosition { X = i, Y = i });
            Assert.True(world.Has<TestPosition>(entity));
            Assert.Equal((float)i, world.Get<TestPosition>(entity).X);

            Assert.True(world.Remove<TestPosition>(entity));
            Assert.False(world.Has<TestPosition>(entity));
        }
    }

    [Fact]
    public void Remove_QueryReflectsAddRemoveCycle()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.Single(world.Query<TestPosition>().ToList());

        world.Remove<TestPosition>(entity);
        Assert.Empty(world.Query<TestPosition>().ToList());

        world.Add(entity, new TestPosition { X = 1f, Y = 1f });
        Assert.Single(world.Query<TestPosition>().ToList());
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Remove_StaleEntity_ReturnsFalse()
    {
        using var world = new World();

        var originalEntity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 1f })
            .Build();
        world.Despawn(originalEntity);

        // Create a new entity
        var newEntity = world.Spawn()
            .With(new TestPosition { X = 2f, Y = 2f })
            .Build();

        // Original entity handle is stale
        bool removed = world.Remove<TestPosition>(originalEntity);
        Assert.False(removed);

        // New entity should work fine
        Assert.True(world.Remove<TestPosition>(newEntity));
    }

    [Fact]
    public void Remove_DoesNotAffectOtherEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new TestPosition { X = 10f, Y = 10f })
            .Build();
        var entity2 = world.Spawn()
            .With(new TestPosition { X = 20f, Y = 20f })
            .Build();

        world.Remove<TestPosition>(entity1);

        Assert.False(world.Has<TestPosition>(entity1));
        Assert.True(world.Has<TestPosition>(entity2));
        Assert.Equal(20f, world.Get<TestPosition>(entity2).X);
    }

    [Fact]
    public void Remove_EntityWithNoComponents_ReturnsFalse()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var entity = world.Spawn().Build();

        bool removed = world.Remove<TestPosition>(entity);

        Assert.False(removed);
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Remove_AllComponents_EntityStillAlive()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        world.Remove<TestPosition>(entity);
        world.Remove<TestVelocity>(entity);

        Assert.False(world.Has<TestPosition>(entity));
        Assert.False(world.Has<TestVelocity>(entity));
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void Remove_GetThrows_AfterRemoval()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Remove<TestPosition>(entity);

        Assert.Throws<InvalidOperationException>(() => world.Get<TestPosition>(entity));
    }

    [Fact]
    public void Remove_SetThrows_AfterRemoval()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Remove<TestPosition>(entity);

        Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new TestPosition { X = 1f, Y = 1f }));
    }

    [Fact]
    public void Remove_DoesNotDespawnEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Remove<TestPosition>(entity);

        // Entity should still be alive and in the world
        Assert.True(world.IsAlive(entity));
        var allEntities = world.GetAllEntities().ToList();
        Assert.Contains(entity, allEntities);
    }

    [Fact]
    public void Remove_CanDespawnAfterRemovingAllComponents()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        world.Remove<TestPosition>(entity);
        bool despawned = world.Despawn(entity);

        Assert.True(despawned);
        Assert.False(world.IsAlive(entity));
    }

    #endregion
}
