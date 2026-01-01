namespace KeenEyes.Tests;

/// <summary>
/// Tests for World.Add&lt;T&gt;(entity, component) runtime component addition.
/// </summary>
public class AddComponentTests
{
    #region Success Path Tests

    [Fact]
    public void Add_AddsComponentToEntity_WhenEntityExists()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        // Create entity without the component
        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 10f, Y = 20f });

        Assert.True(world.Has<TestPosition>(entity));
        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void Add_ComponentCanBeRetrievedViaGet()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, new TestHealth { Current = 100, Max = 100 });

        ref var health = ref world.Get<TestHealth>(entity);
        Assert.Equal(100, health.Current);
        Assert.Equal(100, health.Max);
    }

    [Fact]
    public void Add_MultipleComponentsToSameEntity()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 1f, Y = 2f });
        world.Add(entity, new TestVelocity { X = 3f, Y = 4f });
        world.Add(entity, new TestHealth { Current = 50, Max = 100 });

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));
        Assert.True(world.Has<TestHealth>(entity));

        ref var pos = ref world.Get<TestPosition>(entity);
        ref var vel = ref world.Get<TestVelocity>(entity);
        ref var health = ref world.Get<TestHealth>(entity);

        Assert.Equal(1f, pos.X);
        Assert.Equal(3f, vel.X);
        Assert.Equal(50, health.Current);
    }

    [Fact]
    public void Add_SameComponentTypeToDifferentEntities()
    {
        using var world = new World();

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        world.Add(entity1, new TestPosition { X = 10f, Y = 10f });
        world.Add(entity2, new TestPosition { X = 20f, Y = 20f });
        world.Add(entity3, new TestPosition { X = 30f, Y = 30f });

        Assert.Equal(10f, world.Get<TestPosition>(entity1).X);
        Assert.Equal(20f, world.Get<TestPosition>(entity2).X);
        Assert.Equal(30f, world.Get<TestPosition>(entity3).X);
    }

    [Fact]
    public void Add_HasReturnsTrueAfterAdd()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        Assert.False(world.Has<TestPosition>(entity));

        world.Add(entity, new TestPosition { X = 0f, Y = 0f });

        Assert.True(world.Has<TestPosition>(entity));
    }

    [Fact]
    public void Add_ToEntityWithExistingDifferentComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 2f });

        world.Add(entity, new TestVelocity { X = 5f, Y = 6f });

        Assert.True(world.Has<TestPosition>(entity));
        Assert.True(world.Has<TestVelocity>(entity));

        ref var pos = ref world.Get<TestPosition>(entity);
        ref var vel = ref world.Get<TestVelocity>(entity);

        Assert.Equal(1f, pos.X);
        Assert.Equal(5f, vel.X);
    }

    [Fact]
    public void Add_AutoRegistersComponentType()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        // TestPosition is not explicitly registered, Add should auto-register
        world.Add(entity, new TestPosition { X = 42f, Y = 24f });

        Assert.True(world.Components.IsRegistered<TestPosition>());
        Assert.True(world.Has<TestPosition>(entity));
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public void Add_ThrowsInvalidOperationException_WhenEntityNotAlive()
    {
        using var world = new World();
        var deadEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(deadEntity, new TestPosition { X = 0f, Y = 0f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Add_ThrowsInvalidOperationException_WhenEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        world.Despawn(entity);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new TestVelocity { X = 1f, Y = 1f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Add_ThrowsInvalidOperationException_WhenEntityAlreadyHasComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new TestPosition { X = 100f, Y = 200f }));

        Assert.Contains("already has component", exception.Message);
        Assert.Contains("TestPosition", exception.Message);
    }

    [Fact]
    public void Add_ThrowsInvalidOperationException_ForNullEntity()
    {
        using var world = new World();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(Entity.Null, new TestPosition { X = 0f, Y = 0f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Add_DuplicateDetection_AfterRuntimeAdd()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 1f, Y = 1f });

        // Try to add same component type again
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new TestPosition { X = 2f, Y = 2f }));

        Assert.Contains("already has component", exception.Message);
        Assert.Contains("TestPosition", exception.Message);
    }

    [Fact]
    public void Add_ErrorMessageSuggestsSetForDuplicateComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(entity, new TestPosition { X = 1f, Y = 1f }));

        Assert.Contains("Set<T>()", exception.Message);
    }

    #endregion

    #region Query Integration Tests

    [Fact]
    public void Add_EntityMatchedByQueryAfterAdd()
    {
        using var world = new World();

        // Create entity without Velocity
        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        // Query for entities with Position and Velocity - should be empty
        var queryBefore = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Empty(queryBefore);

        // Add Velocity at runtime
        world.Add(entity, new TestVelocity { X = 1f, Y = 1f });

        // Now query should match the entity
        var queryAfter = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Single(queryAfter);
        Assert.Equal(entity, queryAfter[0]);
    }

    [Fact]
    public void Add_EntityNotMatchedByQueryBeforeAdd()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .Build();

        // Query for Position and Velocity - entity only has Position
        var query = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Empty(query);

        // Query for just Position - should match
        var posQuery = world.Query<TestPosition>().ToList();
        Assert.Single(posQuery);
    }

    [Fact]
    public void Add_MultipleEntities_QueryMatchesCorrectly()
    {
        using var world = new World();

        // Create three entities with just Position
        var entity1 = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        var entity2 = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();
        var entity3 = world.Spawn().With(new TestPosition { X = 3f, Y = 3f }).Build();

        // Add Velocity to only entity2
        world.Add(entity2, new TestVelocity { X = 10f, Y = 10f });

        // Query for Position+Velocity should only match entity2
        var query = world.Query<TestPosition, TestVelocity>().ToList();
        Assert.Single(query);
        Assert.Equal(entity2, query[0]);

        // Query for just Position should match all three
        var posQuery = world.Query<TestPosition>().ToList();
        Assert.Equal(3, posQuery.Count);
    }

    [Fact]
    public void Add_QueryWithThreeComponents_MatchesAfterAddingThird()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0f, Y = 0f })
            .With(new TestVelocity { X = 1f, Y = 1f })
            .Build();

        // Query for three components - entity only has two
        var queryBefore = world.Query<TestPosition, TestVelocity, TestHealth>().ToList();
        Assert.Empty(queryBefore);

        // Add the third component
        world.Add(entity, new TestHealth { Current = 100, Max = 100 });

        // Now query should match
        var queryAfter = world.Query<TestPosition, TestVelocity, TestHealth>().ToList();
        Assert.Single(queryAfter);
        Assert.Equal(entity, queryAfter[0]);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Add_WorksWithDefaultValues()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, default(TestHealth));

        ref var health = ref world.Get<TestHealth>(entity);
        Assert.Equal(0, health.Current);
        Assert.Equal(0, health.Max);
    }

    [Fact]
    public void Add_StaleEntity_ThrowsAfterDespawnAndRespawn()
    {
        using var world = new World();

        // Create and despawn an entity
        var originalEntity = world.Spawn().With(new TestPosition { X = 1f, Y = 1f }).Build();
        world.Despawn(originalEntity);

        // Create a new entity
        var newEntity = world.Spawn().With(new TestPosition { X = 2f, Y = 2f }).Build();

        // Original entity handle should be stale (different version)
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Add(originalEntity, new TestVelocity { X = 1f, Y = 1f }));

        Assert.Contains("not alive", exception.Message);

        // New entity should work fine
        world.Add(newEntity, new TestVelocity { X = 5f, Y = 5f });
        Assert.True(world.Has<TestVelocity>(newEntity));
    }

    [Fact]
    public void Add_ValueIsPassedByRef_NoUnnecessaryCopies()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        // This test verifies the 'in' parameter works correctly
        var newPosition = new TestPosition { X = 42f, Y = 24f };
        world.Add(entity, in newPosition);

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(42f, position.X);
        Assert.Equal(24f, position.Y);
    }

    [Fact]
    public void Add_CanModifyAddedComponentViaGet()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 0f, Y = 0f });

        ref var position = ref world.Get<TestPosition>(entity);
        position.X = 100f;
        position.Y = 200f;

        ref var positionAgain = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, positionAgain.X);
        Assert.Equal(200f, positionAgain.Y);
    }

    [Fact]
    public void Add_CanSetAddedComponentViaSet()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        world.Add(entity, new TestPosition { X = 0f, Y = 0f });
        world.Set(entity, new TestPosition { X = 999f, Y = 888f });

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(999f, position.X);
        Assert.Equal(888f, position.Y);
    }

    [Fact]
    public void Add_DifferentEntities_HaveIndependentComponents()
    {
        using var world = new World();

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();

        world.Add(entity1, new TestPosition { X = 0f, Y = 0f });
        world.Add(entity2, new TestPosition { X = 0f, Y = 0f });

        // Modify entity1's component
        ref var pos1 = ref world.Get<TestPosition>(entity1);
        pos1.X = 999f;
        pos1.Y = 999f;

        // Entity2 should be unaffected
        ref var pos2 = ref world.Get<TestPosition>(entity2);
        Assert.Equal(0f, pos2.X);
        Assert.Equal(0f, pos2.Y);
    }

    #endregion

    #region Tag Component Tests

    /// <summary>
    /// Test tag component for AddComponentTests.
    /// </summary>
    public struct AddTestTag : ITagComponent;

    [Fact]
    public void Add_TagToEntityWithExistingComponents_Works()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 2f })
            .Build();

        world.Add(entity, default(AddTestTag));

        Assert.True(world.Has<AddTestTag>(entity), "Entity should have tag after Add");
        Assert.True(world.Has<TestPosition>(entity), "Entity should still have Position");
    }

    [Fact]
    public void Add_TagToEntityWithMultipleExistingComponents_Works()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1f, Y = 2f })
            .With(new TestVelocity { X = 3f, Y = 4f })
            .With(new TestHealth { Current = 100, Max = 100 })
            .Build();

        world.Add(entity, default(AddTestTag));

        Assert.True(world.Has<AddTestTag>(entity), "Entity should have tag after Add");
        Assert.True(world.Has<TestPosition>(entity), "Entity should still have Position");
        Assert.True(world.Has<TestVelocity>(entity), "Entity should still have Velocity");
        Assert.True(world.Has<TestHealth>(entity), "Entity should still have Health");

        // Verify component data is preserved
        ref var pos = ref world.Get<TestPosition>(entity);
        Assert.Equal(1f, pos.X);
        Assert.Equal(2f, pos.Y);
    }

    #endregion
}
