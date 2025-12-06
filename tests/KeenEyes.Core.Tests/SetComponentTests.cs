namespace KeenEyes.Tests;

/// <summary>
/// Tests for World.Set&lt;T&gt;(entity, value) component updates.
/// </summary>
public class SetComponentTests
{
    #region Success Path Tests

    [Fact]
    public void Set_UpdatesComponentValue_WhenEntityHasComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        world.Set(entity, new TestPosition { X = 100f, Y = 200f });

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
    }

    [Fact]
    public void Set_OverwritesPreviousValue_Completely()
    {
        using var world = new World();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponent(healthInfo, new TestHealth { Current = 100, Max = 100 });

        world.Set(entity, new TestHealth { Current = 50, Max = 200 });

        ref var health = ref world.Get<TestHealth>(entity);
        Assert.Equal(50, health.Current);
        Assert.Equal(200, health.Max);
    }

    [Fact]
    public void Set_WorksWithMultipleEntities()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });
        var entity3 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 3f, Y = 3f });

        world.Set(entity1, new TestPosition { X = 10f, Y = 10f });
        world.Set(entity2, new TestPosition { X = 20f, Y = 20f });
        world.Set(entity3, new TestPosition { X = 30f, Y = 30f });

        ref var pos1 = ref world.Get<TestPosition>(entity1);
        ref var pos2 = ref world.Get<TestPosition>(entity2);
        ref var pos3 = ref world.Get<TestPosition>(entity3);

        Assert.Equal(10f, pos1.X);
        Assert.Equal(20f, pos2.X);
        Assert.Equal(30f, pos3.X);
    }

    [Fact]
    public void Set_WorksWithMultipleComponentTypes()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        world.Set(entity, new TestPosition { X = 100f, Y = 200f });
        world.Set(entity, new TestVelocity { X = 10f, Y = 20f });

        ref var position = ref world.Get<TestPosition>(entity);
        ref var velocity = ref world.Get<TestVelocity>(entity);

        Assert.Equal(100f, position.X);
        Assert.Equal(200f, position.Y);
        Assert.Equal(10f, velocity.X);
        Assert.Equal(20f, velocity.Y);
    }

    [Fact]
    public void Set_DifferentEntities_HaveIndependentComponents()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        world.Set(entity1, new TestPosition { X = 999f, Y = 999f });

        ref var pos2 = ref world.Get<TestPosition>(entity2);
        Assert.Equal(0f, pos2.X); // Entity 2 should be unaffected
        Assert.Equal(0f, pos2.Y);
    }

    [Fact]
    public void Set_MultipleUpdates_LastValueWins()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        world.Set(entity, new TestPosition { X = 1f, Y = 1f });
        world.Set(entity, new TestPosition { X = 2f, Y = 2f });
        world.Set(entity, new TestPosition { X = 3f, Y = 3f });

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(3f, position.X);
        Assert.Equal(3f, position.Y);
    }

    [Fact]
    public void Set_AfterGetModification_OverwritesGetChanges()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Modify via Get<T>
        ref var position = ref world.Get<TestPosition>(entity);
        position.X = 50f;
        position.Y = 50f;

        // Overwrite via Set<T>
        world.Set(entity, new TestPosition { X = 100f, Y = 100f });

        ref var positionAgain = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, positionAgain.X);
        Assert.Equal(100f, positionAgain.Y);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public void Set_ThrowsInvalidOperationException_WhenEntityNotAlive()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        var deadEntity = new Entity(999, 1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(deadEntity, new TestPosition { X = 0f, Y = 0f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Set_ThrowsInvalidOperationException_WhenEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        world.Despawn(entity);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new TestPosition { X = 100f, Y = 100f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Set_ThrowsInvalidOperationException_WhenComponentTypeNotRegistered()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Try to set TestVelocity which is not registered
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new TestVelocity { X = 1f, Y = 1f }));

        Assert.Contains("not registered", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void Set_ThrowsInvalidOperationException_WhenEntityDoesNotHaveComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Entity has Position but not Velocity
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new TestVelocity { X = 1f, Y = 1f }));

        Assert.Contains("does not have component", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void Set_ThrowsInvalidOperationException_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(Entity.Null, new TestPosition { X = 0f, Y = 0f }));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Set_ErrorMessageSuggestsAddForMissingComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(entity, new TestVelocity { X = 1f, Y = 1f }));

        Assert.Contains("Add<T>()", exception.Message);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Set_WorksWithDefaultValues()
    {
        using var world = new World();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponent(healthInfo, new TestHealth { Current = 100, Max = 100 });

        world.Set(entity, default(TestHealth));

        ref var health = ref world.Get<TestHealth>(entity);
        Assert.Equal(0, health.Current);
        Assert.Equal(0, health.Max);
    }

    [Fact]
    public void Set_StaleEntity_ThrowsAfterDespawnAndRespawn()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        // Create and despawn an entity
        var originalEntity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        world.Despawn(originalEntity);

        // Create a new entity (may reuse the same ID)
        var newEntity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });

        // Original entity handle should be stale (different version)
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Set(originalEntity, new TestPosition { X = 999f, Y = 999f }));

        Assert.Contains("not alive", exception.Message);

        // New entity should work fine
        world.Set(newEntity, new TestPosition { X = 100f, Y = 100f });
        ref var pos = ref world.Get<TestPosition>(newEntity);
        Assert.Equal(100f, pos.X);
    }

    [Fact]
    public void Set_ValueIsPassedByRef_NoUnnecessaryCopies()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // This test verifies the 'in' parameter works correctly
        // The actual optimization benefit is at compile time, but we can verify correctness
        var newPosition = new TestPosition { X = 42f, Y = 24f };
        world.Set(entity, in newPosition);

        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(42f, position.X);
        Assert.Equal(24f, position.Y);
    }

    #endregion
}
