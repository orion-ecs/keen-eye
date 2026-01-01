namespace KeenEyes.Tests;

/// <summary>
/// Tests for World.GetComponents(entity) component introspection.
/// </summary>
public class GetComponentsTests
{
    #region Success Path Tests - Returns Components

    [Fact]
    public void GetComponents_ReturnsSingleComponent_WhenEntityHasOneComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        var components = world.GetComponents(entity).ToList();

        Assert.Single(components);
        Assert.Equal(typeof(TestPosition), components[0].Type);
        var position = Assert.IsType<TestPosition>(components[0].Value);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void GetComponents_ReturnsMultipleComponents_WhenEntityHasMany()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }),
            (healthInfo, new TestHealth { Current = 100, Max = 100 }));

        var components = world.GetComponents(entity).ToList();

        Assert.Equal(3, components.Count);

        var types = components.Select(c => c.Type).ToHashSet();
        Assert.Contains(typeof(TestPosition), types);
        Assert.Contains(typeof(TestVelocity), types);
        Assert.Contains(typeof(TestHealth), types);
    }

    [Fact]
    public void GetComponents_ReturnsCorrectValues_ForEachComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 5f, Y = 10f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = -1f }));

        var components = world.GetComponents(entity).ToDictionary(c => c.Type, c => c.Value);

        var position = Assert.IsType<TestPosition>(components[typeof(TestPosition)]);
        Assert.Equal(5f, position.X);
        Assert.Equal(10f, position.Y);

        var velocity = Assert.IsType<TestVelocity>(components[typeof(TestVelocity)]);
        Assert.Equal(1f, velocity.X);
        Assert.Equal(-1f, velocity.Y);
    }

    [Fact]
    public void GetComponents_ReturnsDefaultValues_WhenComponentHasDefaultValue()
    {
        using var world = new World();
        var healthInfo = world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponent(healthInfo, default(TestHealth));

        var components = world.GetComponents(entity).ToList();

        Assert.Single(components);
        Assert.Equal(typeof(TestHealth), components[0].Type);
        var health = Assert.IsType<TestHealth>(components[0].Value);
        Assert.Equal(0, health.Current);
        Assert.Equal(0, health.Max);
    }

    [Fact]
    public void GetComponents_WorksWithEntityBuilder()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 5f, Y = 10f })
            .With(new TestVelocity { X = 1f, Y = 2f })
            .Build();

        var components = world.GetComponents(entity).ToList();

        Assert.Equal(2, components.Count);
        var types = components.Select(c => c.Type).ToHashSet();
        Assert.Contains(typeof(TestPosition), types);
        Assert.Contains(typeof(TestVelocity), types);
    }

    #endregion

    #region Empty Results Tests

    [Fact]
    public void GetComponents_ReturnsEmpty_WhenEntityIsNotAlive()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var nonExistentEntity = new Entity(TestConstants.InvalidEntityId, TestConstants.DefaultEntityVersion);

        var components = world.GetComponents(nonExistentEntity);

        Assert.Empty(components);
    }

    [Fact]
    public void GetComponents_ReturnsEmpty_ForNullEntity()
    {
        using var world = new World();
        world.Components.Register<TestPosition>();

        var components = world.GetComponents(Entity.Null);

        Assert.Empty(components);
    }

    [Fact]
    public void GetComponents_ReturnsEmpty_AfterEntityDespawned()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Entity has components before despawn
        Assert.Single(world.GetComponents(entity));

        world.Despawn(entity);

        // Stale entity handle should return empty
        Assert.Empty(world.GetComponents(entity));
    }

    [Fact]
    public void GetComponents_ReturnsEmpty_WhenEntityHasNoComponents()
    {
        using var world = new World();

        // Create entity with no components
        var entity = world.CreateEntity([]);

        var components = world.GetComponents(entity);

        Assert.Empty(components);
    }

    #endregion

    #region Stale Entity Handle Tests

    [Fact]
    public void GetComponents_ReturnsEmpty_ForStaleEntityHandle()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        // Create and despawn an entity
        var originalEntity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });
        world.Despawn(originalEntity);

        // Create a new entity (may reuse the same ID)
        _ = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 2f, Y = 2f });

        // Original entity handle should be stale (different version)
        Assert.Empty(world.GetComponents(originalEntity));
    }

    [Fact]
    public void GetComponents_ReturnsEmpty_WhenVersionMismatch()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });

        // Create a stale handle with wrong version
        var staleHandle = new Entity(entity.Id, entity.Version + 1);

        Assert.Empty(world.GetComponents(staleHandle));
    }

    [Fact]
    public void GetComponents_ReturnsEmpty_WhenEntityDespawnedAndRecreated()
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
        Assert.Empty(world.GetComponents(staleHandle));

        // New entity should have components
        Assert.Single(world.GetComponents(entity2));
    }

    #endregion

    #region Component Modification Tests

    [Fact]
    public void GetComponents_ReflectsAddedComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        Assert.Single(world.GetComponents(entity));

        // Add velocity at runtime
        world.Add(entity, new TestVelocity { X = 1f, Y = 1f });

        var components = world.GetComponents(entity).ToList();
        Assert.Equal(2, components.Count);
        var types = components.Select(c => c.Type).ToHashSet();
        Assert.Contains(typeof(TestVelocity), types);
    }

    [Fact]
    public void GetComponents_ReflectsRemovedComponent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 10f, Y = 20f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 2f }));

        Assert.Equal(2, world.GetComponents(entity).Count());

        world.Remove<TestVelocity>(entity);

        var components = world.GetComponents(entity).ToList();
        Assert.Single(components);
        Assert.Equal(typeof(TestPosition), components[0].Type);
    }

    [Fact]
    public void GetComponents_ReflectsModifiedComponentValue()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 0f, Y = 0f });

        // Modify component via Get
        ref var position = ref world.Get<TestPosition>(entity);
        position.X = 100f;
        position.Y = 200f;

        var components = world.GetComponents(entity).ToList();
        var retrievedPosition = Assert.IsType<TestPosition>(components[0].Value);
        Assert.Equal(100f, retrievedPosition.X);
        Assert.Equal(200f, retrievedPosition.Y);
    }

    #endregion

    #region Multiple Entity Tests

    [Fact]
    public void GetComponents_IsolatedBetweenEntities()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        // Entity 1 has only position
        var entity1 = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 1f });

        // Entity 2 has position and velocity
        var entity2 = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 2f, Y = 2f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 0f }));

        var components1 = world.GetComponents(entity1).ToList();
        var components2 = world.GetComponents(entity2).ToList();

        Assert.Single(components1);
        Assert.Equal(2, components2.Count);
    }

    [Fact]
    public void GetComponents_WorksWithManyEntities()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            if (i % 2 == 0)
            {
                entities.Add(world.CreateEntityWithComponent(positionInfo, new TestPosition { X = i, Y = i }));
            }
            else
            {
                entities.Add(world.CreateEntityWithComponents(
                    (positionInfo, new TestPosition { X = i, Y = i }),
                    (velocityInfo, new TestVelocity { X = 1, Y = 0 })));
            }
        }

        for (int i = 0; i < 100; i++)
        {
            var count = world.GetComponents(entities[i]).Count();
            var expectedCount = i % 2 == 0 ? 1 : 2;
            Assert.Equal(expectedCount, count);
        }
    }

    #endregion

    #region World Isolation Tests

    [Fact]
    public void GetComponents_IsolatedBetweenWorlds()
    {
        using var world1 = new World();
        using var world2 = new World();

        var positionInfo1 = world1.Components.Register<TestPosition>();
        var entity1 = world1.CreateEntityWithComponent(positionInfo1, new TestPosition { X = 1f, Y = 1f });

        // World2 has no entities
        world2.Components.Register<TestPosition>();

        // Entity from world1 has components in world1
        Assert.Single(world1.GetComponents(entity1));

        // Entity handle used with world2 should return empty (entity doesn't exist in world2)
        Assert.Empty(world2.GetComponents(entity1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetComponents_IsIdempotent()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Calling GetComponents multiple times should return the same result
        var result1 = world.GetComponents(entity).ToList();
        var result2 = world.GetComponents(entity).ToList();
        var result3 = world.GetComponents(entity).ToList();

        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Single(result3);
        Assert.Equal(result1[0].Type, result2[0].Type);
        Assert.Equal(result2[0].Type, result3[0].Type);
    }

    [Fact]
    public void GetComponents_DoesNotModifyEntity()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 10f, Y = 20f });

        // Call GetComponents multiple times
        _ = world.GetComponents(entity).ToList();
        _ = world.GetComponents(entity).ToList();
        _ = world.GetComponents(entity).ToList();

        // Entity should still be alive and have its component unchanged
        Assert.True(world.IsAlive(entity));
        ref var position = ref world.Get<TestPosition>(entity);
        Assert.Equal(10f, position.X);
        Assert.Equal(20f, position.Y);
    }

    [Fact]
    public void GetComponents_CanBeUsedForDebugging()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 100f, Y = 200f })
            .With(new TestVelocity { X = 10f, Y = -5f })
            .Build();

        // Simulate debug output
        var debugOutput = string.Join(", ", world.GetComponents(entity)
            .Select(c => $"{c.Type.Name}={c.Value}"));

        Assert.Contains("TestPosition", debugOutput);
        Assert.Contains("TestVelocity", debugOutput);
    }

    [Fact]
    public void GetComponents_CanBeUsedForSerialization()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 50f, Y = 75f })
            .With(new TestHealth { Current = 80, Max = 100 })
            .Build();

        // Simulate serialization snapshot
        var snapshot = world.GetComponents(entity)
            .ToDictionary(c => c.Type.FullName!, c => c.Value);

        Assert.Equal(2, snapshot.Count);
        Assert.True(snapshot.ContainsKey(typeof(TestPosition).FullName!));
        Assert.True(snapshot.ContainsKey(typeof(TestHealth).FullName!));

        var position = Assert.IsType<TestPosition>(snapshot[typeof(TestPosition).FullName!]);
        Assert.Equal(50f, position.X);
        Assert.Equal(75f, position.Y);
    }

    [Fact]
    public void GetComponents_ConsistentWithHas()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();
        world.Components.Register<TestHealth>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 0f, Y = 0f }),
            (velocityInfo, new TestVelocity { X = 1f, Y = 1f }));

        var componentTypes = world.GetComponents(entity)
            .Select(c => c.Type)
            .ToHashSet();

        // GetComponents should include types that Has returns true for
        Assert.Equal(world.Has<TestPosition>(entity), componentTypes.Contains(typeof(TestPosition)));
        Assert.Equal(world.Has<TestVelocity>(entity), componentTypes.Contains(typeof(TestVelocity)));
        Assert.Equal(world.Has<TestHealth>(entity), componentTypes.Contains(typeof(TestHealth)));
    }

    #endregion

    #region Performance Awareness Tests

    [Fact]
    public void GetComponents_CanIterateLazilyWithYieldReturn()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();
        var velocityInfo = world.Components.Register<TestVelocity>();

        var entity = world.CreateEntityWithComponents(
            (positionInfo, new TestPosition { X = 1f, Y = 2f }),
            (velocityInfo, new TestVelocity { X = 3f, Y = 4f }));

        // Take only the first component - lazy evaluation should work
        var first = world.GetComponents(entity).First();

        // Should get a valid component
        Assert.NotNull(first.Type);
        Assert.NotNull(first.Value);
    }

    [Fact]
    public void GetComponents_CanBeEnumeratedMultipleTimes()
    {
        using var world = new World();
        var positionInfo = world.Components.Register<TestPosition>();

        var entity = world.CreateEntityWithComponent(positionInfo, new TestPosition { X = 1f, Y = 2f });

        var components = world.GetComponents(entity);

        // Enumerate multiple times
        var count1 = components.Count();
        var count2 = components.Count();
        var count3 = components.Count();

        Assert.Equal(1, count1);
        Assert.Equal(1, count2);
        Assert.Equal(1, count3);
    }

    #endregion
}
