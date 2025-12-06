namespace KeenEyes.Tests;

/// <summary>
/// Test component for builder tests.
/// </summary>
public struct BuilderTestPosition : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test component for builder tests.
/// </summary>
public struct BuilderTestVelocity : IComponent
{
    public float X;
    public float Y;
}

/// <summary>
/// Test tag component for builder tests.
/// </summary>
public struct BuilderTestTag : ITagComponent;

/// <summary>
/// Another test tag component for builder tests.
/// </summary>
public struct AnotherBuilderTag : ITagComponent;

/// <summary>
/// Tests for EntityBuilder class.
/// </summary>
public class EntityBuilderTests
{
    #region Basic Construction

    [Fact]
    public void EntityBuilder_World_ReturnsCorrectWorld()
    {
        using var world = new World();

        var builder = world.Spawn();

        Assert.Same(world, builder.World);
    }

    [Fact]
    public void EntityBuilder_Build_CreatesValidEntity()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(entity.IsValid);
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void EntityBuilder_Build_EntityHasComponents()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 10f, Y = 20f })
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
        ref var position = ref world.Get<BuilderTestPosition>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    #endregion

    #region With Method

    [Fact]
    public void EntityBuilder_With_ReturnsSelf()
    {
        using var world = new World();

        var builder = world.Spawn();
        var result = builder.With(new BuilderTestPosition { X = 0f, Y = 0f });

        Assert.Same(builder, result);
    }

    [Fact]
    public void EntityBuilder_With_AddsComponent()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 5f, Y = 10f })
            .Build();

        ref var position = ref world.Get<BuilderTestPosition>(entity);
        Assert.Equal(5f, position.X);
        Assert.Equal(10f, position.Y);
    }

    [Fact]
    public void EntityBuilder_With_MultipleTimes_AddsAllComponents()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 2f })
            .With(new BuilderTestVelocity { X = 3f, Y = 4f })
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
        Assert.True(world.Has<BuilderTestVelocity>(entity));

        ref var position = ref world.Get<BuilderTestPosition>(entity);
        ref var velocity = ref world.Get<BuilderTestVelocity>(entity);

        Assert.Equal(1f, position.X);
        Assert.Equal(2f, position.Y);
        Assert.Equal(3f, velocity.X);
        Assert.Equal(4f, velocity.Y);
    }

    [Fact]
    public void EntityBuilder_With_RegistersComponentType()
    {
        using var world = new World();

        world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(world.Components.IsRegistered<BuilderTestPosition>());
    }

    #endregion

    #region WithTag Method

    [Fact]
    public void EntityBuilder_WithTag_ReturnsSelf()
    {
        using var world = new World();

        var builder = world.Spawn();
        var result = builder.WithTag<BuilderTestTag>();

        Assert.Same(builder, result);
    }

    [Fact]
    public void EntityBuilder_WithTag_AddsTagComponent()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .WithTag<BuilderTestTag>()
            .Build();

        Assert.True(world.Has<BuilderTestTag>(entity));
    }

    [Fact]
    public void EntityBuilder_WithTag_MultipleTags()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .WithTag<BuilderTestTag>()
            .WithTag<AnotherBuilderTag>()
            .Build();

        Assert.True(world.Has<BuilderTestTag>(entity));
        Assert.True(world.Has<AnotherBuilderTag>(entity));
    }

    [Fact]
    public void EntityBuilder_WithTag_RegistersAsTag()
    {
        using var world = new World();

        world.Spawn()
            .WithTag<BuilderTestTag>()
            .Build();

        var info = world.Components.Get<BuilderTestTag>();
        Assert.NotNull(info);
        Assert.True(info.IsTag);
    }

    #endregion

    #region Chaining and Fluent API

    [Fact]
    public void EntityBuilder_FluentChaining_WorksCorrectly()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 2f })
            .With(new BuilderTestVelocity { X = 3f, Y = 4f })
            .WithTag<BuilderTestTag>()
            .WithTag<AnotherBuilderTag>()
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
        Assert.True(world.Has<BuilderTestVelocity>(entity));
        Assert.True(world.Has<BuilderTestTag>(entity));
        Assert.True(world.Has<AnotherBuilderTag>(entity));
    }

    #endregion

    #region Multiple Entities

    [Fact]
    public void EntityBuilder_MultipleEntities_EachHasOwnComponents()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new BuilderTestPosition { X = 2f, Y = 2f })
            .Build();

        ref var pos1 = ref world.Get<BuilderTestPosition>(entity1);
        ref var pos2 = ref world.Get<BuilderTestPosition>(entity2);

        Assert.Equal(1f, pos1.X);
        Assert.Equal(2f, pos2.X);
    }

    [Fact]
    public void EntityBuilder_MultipleEntities_HaveUniqueIds()
    {
        using var world = new World();

        var entity1 = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();
        var entity2 = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void EntityBuilder_ReuseSpawn_ClearsState()
    {
        using var world = new World();

        // First entity with Position
        var entity1 = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 1f })
            .Build();

        // Second entity with only Velocity (no Position)
        var entity2 = world.Spawn()
            .With(new BuilderTestVelocity { X = 2f, Y = 2f })
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity1));
        Assert.False(world.Has<BuilderTestVelocity>(entity1));

        Assert.False(world.Has<BuilderTestPosition>(entity2));
        Assert.True(world.Has<BuilderTestVelocity>(entity2));
    }

    #endregion

    #region Interface Implementation

    [Fact]
    public void EntityBuilder_IEntityBuilder_With_Works()
    {
        using var world = new World();

        IEntityBuilder builder = world.Spawn();
        builder = builder.With(new BuilderTestPosition { X = 5f, Y = 10f });

        // Build through the concrete type
        var entity = ((EntityBuilder)builder).Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
    }

    [Fact]
    public void EntityBuilder_IEntityBuilder_WithTag_Works()
    {
        using var world = new World();

        IEntityBuilder builder = world.Spawn();
        builder = builder
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .WithTag<BuilderTestTag>();

        var entity = ((EntityBuilder)builder).Build();

        Assert.True(world.Has<BuilderTestTag>(entity));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EntityBuilder_EmptyEntity_IsValid()
    {
        using var world = new World();

        var entity = world.Spawn().Build();

        Assert.True(entity.IsValid);
        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void EntityBuilder_WithDefaultComponent_Works()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(default(BuilderTestPosition))
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
        ref var position = ref world.Get<BuilderTestPosition>(entity);
        Assert.Equal(0f, position.X);
        Assert.Equal(0f, position.Y);
    }

    #endregion
}

/// <summary>
/// Tests for World.Has&lt;T&gt; method.
/// </summary>
public class WorldHasTests
{
    [Fact]
    public void Has_EntityWithComponent_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.True(world.Has<BuilderTestPosition>(entity));
    }

    [Fact]
    public void Has_EntityWithoutComponent_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        Assert.False(world.Has<BuilderTestVelocity>(entity));
    }

    [Fact]
    public void Has_DeadEntity_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        Assert.False(world.Has<BuilderTestPosition>(entity));
    }

    [Fact]
    public void Has_InvalidEntity_ReturnsFalse()
    {
        using var world = new World();
        var invalidEntity = new Entity(999, 1);

        Assert.False(world.Has<BuilderTestPosition>(invalidEntity));
    }

    [Fact]
    public void Has_UnregisteredComponentType_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // BuilderTestPosition is not registered yet
        Assert.False(world.Has<BuilderTestPosition>(entity));
    }
}

/// <summary>
/// Tests for World.Despawn method.
/// </summary>
public class WorldDespawnTests
{
    [Fact]
    public void Despawn_AliveEntity_ReturnsTrue()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        var result = world.Despawn(entity);

        Assert.True(result);
    }

    [Fact]
    public void Despawn_AliveEntity_EntityNoLongerAlive()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void Despawn_DeadEntity_ReturnsFalse()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);
        var result = world.Despawn(entity);

        Assert.False(result);
    }

    [Fact]
    public void Despawn_InvalidEntity_ReturnsFalse()
    {
        using var world = new World();
        var invalidEntity = new Entity(999, 1);

        var result = world.Despawn(invalidEntity);

        Assert.False(result);
    }

    [Fact]
    public void Despawn_IncrementsVersion()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        world.Despawn(entity);

        // After despawning, if we create a new entity with same ID,
        // the original entity handle should be stale
        var newEntity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        // Original entity should not be alive (version mismatch)
        Assert.False(world.IsAlive(entity));
    }
}

/// <summary>
/// Tests for World.GetAllEntities method.
/// </summary>
public class WorldGetAllEntitiesTests
{
    [Fact]
    public void GetAllEntities_NoEntities_ReturnsEmpty()
    {
        using var world = new World();

        var entities = world.GetAllEntities().ToList();

        Assert.Empty(entities);
    }

    [Fact]
    public void GetAllEntities_SingleEntity_ReturnsThatEntity()
    {
        using var world = new World();
        var entity = world.Spawn()
            .With(new BuilderTestPosition { X = 0f, Y = 0f })
            .Build();

        var entities = world.GetAllEntities().ToList();

        Assert.Single(entities);
        Assert.Contains(entity, entities);
    }

    [Fact]
    public void GetAllEntities_MultipleEntities_ReturnsAll()
    {
        using var world = new World();
        var entity1 = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new BuilderTestPosition { X = 2f, Y = 2f })
            .Build();
        var entity3 = world.Spawn()
            .With(new BuilderTestPosition { X = 3f, Y = 3f })
            .Build();

        var entities = world.GetAllEntities().ToList();

        Assert.Equal(3, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.Contains(entity3, entities);
    }

    [Fact]
    public void GetAllEntities_AfterDespawn_ExcludesDespawned()
    {
        using var world = new World();
        var entity1 = world.Spawn()
            .With(new BuilderTestPosition { X = 1f, Y = 1f })
            .Build();
        var entity2 = world.Spawn()
            .With(new BuilderTestPosition { X = 2f, Y = 2f })
            .Build();

        world.Despawn(entity1);

        var entities = world.GetAllEntities().ToList();

        Assert.Single(entities);
        Assert.Contains(entity2, entities);
        Assert.DoesNotContain(entity1, entities);
    }
}
