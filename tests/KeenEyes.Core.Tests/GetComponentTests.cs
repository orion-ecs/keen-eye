namespace KeenEyes.Tests;

/// <summary>
/// Test component for position data.
/// </summary>
public struct TestPosition : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test component for velocity data.
/// </summary>
public struct TestVelocity : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test component for health data.
/// </summary>
public struct TestHealth : IComponent
{
    public int Current;
    public int Max;
}

/// <summary>
/// Tests for World.Get&lt;T&gt;(entity) component retrieval.
/// </summary>
public class GetComponentTests
{
    #region Success Path Tests

    [Fact]
    public void Get_ReturnsComponentValue_WhenEntityHasComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        ref var position = ref world.Get<TestPosition>(entity);

        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void Get_ReturnsRef_ModificationsArePersisted()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        ref var position = ref world.Get<TestPosition>(entity);
        position.X = 100f;
        position.Y = 200f;

        ref var positionAgain = ref world.Get<TestPosition>(entity);
        Assert.Equal(100f, positionAgain.X);
        Assert.Equal(200f, positionAgain.Y);
    }

    [Fact]
    public void Get_WorksWithMultipleEntities()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });
        var entity3 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 3f, Y = 3f });

        ref var pos1 = ref world.Get<TestPosition>(entity1);
        ref var pos2 = ref world.Get<TestPosition>(entity2);
        ref var pos3 = ref world.Get<TestPosition>(entity3);

        Assert.Equal(1f, pos1.X);
        Assert.Equal(2f, pos2.X);
        Assert.Equal(3f, pos3.X);
    }

    [Fact]
    public void Get_WorksWithMultipleComponentTypes()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        ref var position = ref world.Get<TestPosition>(entity);
        ref var velocity = ref world.Get<TestVelocity>(entity);

        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
        Assert.Equal(1f, velocity.X);
        Assert.Equal(2f, velocity.Y);
    }

    [Fact]
    public void Get_DifferentEntities_HaveIndependentComponents()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        ref var pos1 = ref world.Get<TestPosition>(entity1);
        pos1.X = 999f;

        ref var pos2 = ref world.Get<TestPosition>(entity2);
        Assert.Equal(0f, pos2.X); // Entity 2 should be unaffected
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public void Get_ThrowsInvalidOperationException_WhenEntityNotAlive()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        var deadEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Get<TestPosition>(deadEntity));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Get_ThrowsInvalidOperationException_WhenEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        world.Despawn(entity);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Get<TestPosition>(entity));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void Get_ThrowsInvalidOperationException_WhenComponentTypeNotRegistered()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Try to get TestVelocity which is not registered
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Get<TestVelocity>(entity));

        Assert.Contains("not registered", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void Get_ThrowsInvalidOperationException_WhenEntityDoesNotHaveComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Entity has Position but not Velocity
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Get<TestVelocity>(entity));

        Assert.Contains("does not have component", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void Get_ThrowsInvalidOperationException_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.Get<TestPosition>(Entity.Null));

        Assert.Contains("not alive", exception.Message);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Get_WorksWithDefaultValues()
    {
        using var world = new World();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponent(healthInfo, default(TestHealth));

        ref var health = ref world.Get<TestHealth>(entity);

        Assert.Equal(0, health.Current);
        Assert.Equal(0, health.Max);
    }

    [Fact]
    public void Get_StaleEntity_ThrowsAfterDespawnAndRespawn()
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
            world.Get<TestPosition>(originalEntity));

        Assert.Contains("not alive", exception.Message);

        // New entity should work fine
        ref var pos = ref world.Get<TestPosition>(newEntity);
        Assert.Equal(2f, pos.X);
    }

    #endregion
}

/// <summary>
/// Tests for World.GetReadonly&lt;T&gt;(entity) readonly component retrieval.
/// </summary>
public class GetReadonlyComponentTests
{
    #region Success Path Tests

    [Fact]
    public void GetReadonly_ReturnsComponentValue_WhenEntityHasComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        ref readonly var position = ref world.GetReadonly<TestPosition>(entity);

        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void GetReadonly_WorksWithMultipleEntities()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        var entity2 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });
        var entity3 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 3f, Y = 3f });

        ref readonly var pos1 = ref world.GetReadonly<TestPosition>(entity1);
        ref readonly var pos2 = ref world.GetReadonly<TestPosition>(entity2);
        ref readonly var pos3 = ref world.GetReadonly<TestPosition>(entity3);

        Assert.Equal(1f, pos1.X);
        Assert.Equal(2f, pos2.X);
        Assert.Equal(3f, pos3.X);
    }

    [Fact]
    public void GetReadonly_WorksWithMultipleComponentTypes()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        ref readonly var position = ref world.GetReadonly<TestPosition>(entity);
        ref readonly var velocity = ref world.GetReadonly<TestVelocity>(entity);

        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
        Assert.Equal(1f, velocity.X);
        Assert.Equal(2f, velocity.Y);
    }

    [Fact]
    public void GetReadonly_ReturnsUpdatedValue_AfterModificationViaGet()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Modify via Get<T>()
        ref var position = ref world.Get<TestPosition>(entity);
        position.X = 100f;
        position.Y = 200f;

        // Read via GetReadonly<T>()
        ref readonly var positionReadonly = ref world.GetReadonly<TestPosition>(entity);
        Assert.Equal(100f, positionReadonly.X);
        Assert.Equal(200f, positionReadonly.Y);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public void GetReadonly_ThrowsInvalidOperationException_WhenEntityNotAlive()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();
        var deadEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetReadonly<TestPosition>(deadEntity));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void GetReadonly_ThrowsInvalidOperationException_WhenEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });
        world.Despawn(entity);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetReadonly<TestPosition>(entity));

        Assert.Contains("not alive", exception.Message);
    }

    [Fact]
    public void GetReadonly_ThrowsInvalidOperationException_WhenComponentTypeNotRegistered()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetReadonly<TestVelocity>(entity));

        Assert.Contains("not registered", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void GetReadonly_ThrowsInvalidOperationException_WhenEntityDoesNotHaveComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetReadonly<TestVelocity>(entity));

        Assert.Contains("does not have component", exception.Message);
        Assert.Contains("TestVelocity", exception.Message);
    }

    [Fact]
    public void GetReadonly_ThrowsInvalidOperationException_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetReadonly<TestPosition>(Entity.Null));

        Assert.Contains("not alive", exception.Message);
    }

    #endregion
}

/// <summary>
/// Extension methods to help with test setup.
/// </summary>
internal static class WorldTestExtensions
{
    /// <summary>
    /// Creates an entity with a single component for testing.
    /// </summary>
    public static Entity CreateEntityWithComponent<T>(this World world, ComponentInfo info, T component)
        where T : struct, IComponent
    {
        var components = new List<(ComponentInfo Info, object Data)>
        {
            (info, component)
        };
        return world.CreateEntity(components);
    }

    /// <summary>
    /// Creates an entity with multiple components for testing.
    /// </summary>
    public static Entity CreateEntityWithComponents(
        this World world,
        params (ComponentInfo Info, object Data)[] components)
    {
        return world.CreateEntity([.. components]);
    }
}
