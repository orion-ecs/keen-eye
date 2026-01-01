namespace KeenEyes.Tests;

/// <summary>
/// Tests for World.Has&lt;T&gt;(entity) component existence checking.
/// </summary>
public class HasComponentTests
{
    #region Success Path Tests - Returns True

    [Fact]
    public void Has_ReturnsTrue_WhenEntityHasComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        var hasPosition = world.Has<TestPosition>(entity);

        Assert.True(hasPosition);
    }

    [Fact]
    public void Has_ReturnsTrue_ForMultipleComponents()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
    }

    [Fact]
    public void Has_ReturnsTrue_WithDefaultComponentValue()
    {
        using var world = new World();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponent(healthInfo, default(TestHealth));

        Assert.True(world.Has<TestHealth>(entity));
    }

    [Fact]
    public void Has_ReturnsTrue_ForMultipleEntitiesWithSameComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });
        var entity3 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 3f, Y = 3f });

        Assert.True(world.Has<TestPosition>(entity1));
        Assert.True(world.Has<TestPosition>(entity2));
        Assert.True(world.Has<TestPosition>(entity3));
    }

    [Fact]
    public void Has_ReturnsTrue_AfterComponentAdded()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Add velocity at runtime
        world.Add(entity, new TestVelocity { X = 1f, Y = 1f });

        Assert.True(world.Has<TestVelocity>(entity));
    }

    #endregion

    #region False Return Path Tests

    [Fact]
    public void Has_ReturnsFalse_WhenEntityDoesNotHaveComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        var hasVelocity = world.Has<TestVelocity>(entity);

        Assert.False(hasVelocity);
    }

    [Fact]
    public void Has_ReturnsFalse_WhenComponentTypeNotRegistered()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // TestVelocity is not registered
        var hasVelocity = world.Has<TestVelocity>(entity);

        Assert.False(hasVelocity);
    }

    [Fact]
    public void Has_ReturnsFalse_ForNonExistentEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var nonExistentEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var hasPosition = world.Has<TestPosition>(nonExistentEntity);

        Assert.False(hasPosition);
    }

    [Fact]
    public void Has_ReturnsFalse_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var hasPosition = world.Has<TestPosition>(Entity.Null);

        Assert.False(hasPosition);
    }

    [Fact]
    public void Has_ReturnsFalse_AfterEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Entity has the component before despawn
        Assert.True(world.Has<TestPosition>(entity));

        world.Despawn(entity);

        // Stale entity handle should return false
        Assert.False(world.Has<TestPosition>(entity));
    }

    [Fact]
    public void Has_ReturnsFalse_AfterComponentRemoved()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        // Entity has both components initially
        Assert.True(world.Has<TestVelocity>(entity));

        world.Remove<TestVelocity>(entity);

        // After removal, entity should not have Velocity
        Assert.False(world.Has<TestVelocity>(entity));

        // But should still have Position
        Assert.True(world.Has<TestPosition>(entity));
    }

    #endregion

    #region Stale Entity Handle Tests

    [Fact]
    public void Has_ReturnsFalse_ForStaleEntityHandle()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        // Create and despawn an entity
        var originalEntity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        world.Despawn(originalEntity);

        // Create a new entity (may reuse the same ID)
        _ = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });

        // Original entity handle should be stale (different version)
        Assert.False(world.Has<TestPosition>(originalEntity));
    }

    [Fact]
    public void Has_ReturnsFalse_WhenVersionMismatch()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });

        // Create a stale handle with wrong version
        var staleHandle = new Entity(entity.Id, entity.Version + 1);

        Assert.False(world.Has<TestPosition>(staleHandle));
    }

    [Fact]
    public void Has_ReturnsFalse_WhenEntityDespawnedAndRecreated()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        var originalId = entity1.Id;
        var originalVersion = entity1.Version;

        world.Despawn(entity1);

        // Create new entity (may or may not reuse ID)
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });

        // The original handle should be stale regardless
        var staleHandle = new Entity(originalId, originalVersion);
        Assert.False(world.Has<TestPosition>(staleHandle));

        // New entity should be valid
        Assert.True(world.Has<TestPosition>(entity2));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Has_IsConsistentWithGet_WhenComponentExists()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // If Has returns true, Get should not throw
        if (world.Has<TestPosition>(entity))
        {
            ref var position = ref world.Get<TestPosition>(entity);
            Assert.Equal(10f, position.X);
        }
        else
        {
            Assert.Fail("Has<TestPosition> should have returned true");
        }
    }

    [Fact]
    public void Has_IsConsistentWithGet_WhenComponentNotExists()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // If Has returns false, Get should throw
        Assert.False(world.Has<TestVelocity>(entity));
        Assert.Throws<InvalidOperationException>(() => world.Get<TestVelocity>(entity));
    }

    [Fact]
    public void Has_WorksCorrectlyInLoop()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        // Create 100 entities, only even-numbered ones have Position
        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            if (i % 2 == 0)
            {
                entities.Add(world.CreateEntityWithComponent(positionInfo, new TestPosition { X = i, Y = i }));
            }
            else
            {
                // Create entity without position by using internal method with empty list
                entities.Add(world.CreateEntity([]));
            }
        }

        // Verify Has returns correct values
        for (int i = 0; i < 100; i++)
        {
            var expected = i % 2 == 0;
            Assert.Equal(expected, world.Has<TestPosition>(entities[i]));
        }
    }

    [Fact]
    public void Has_WorksWithEntityBuilder()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 5f, Y = 10f })
            .With(new TestVelocity { X = 1f, Y = 2f })
            .Build();

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
        Assert.False(world.Has<TestHealth>(entity));
    }

    [Fact]
    public void Has_DoesNotModifyEntity()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Call Has multiple times
        _ = world.Has<TestPosition>(entity);
        _ = world.Has<TestPosition>(entity);
        _ = world.Has<TestVelocity>(entity);
        _ = world.Has<TestVelocity>(entity);

        // Entity should still be alive and have its component unchanged
        Assert.True(world.IsAlive(entity));
        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void Has_IsIdempotent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Calling Has multiple times should return the same result
        var result1 = world.Has<TestPosition>(entity);
        var result2 = world.Has<TestPosition>(entity);
        var result3 = world.Has<TestPosition>(entity);

        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    #endregion

    #region Multiple World Isolation Tests

    [Fact]
    public void Has_IsolatedBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        var positionInfo1 = world1.Components.Register<TestPosition>();
        var entity1 = world1.CreateEntityWithComponent(positionInfo1, new TestPosition { X = 1f, Y = 1f });

        // World2 has no entities, just register the component
        world2.Components.Register<TestPosition>();

        // Entity from world1 should have position in world1
        Assert.True(world1.Has<TestPosition>(entity1));

        // Entity handle used with world2 should return false (entity doesn't exist in world2)
        Assert.False(world2.Has<TestPosition>(entity1));
    }

    #endregion
}
