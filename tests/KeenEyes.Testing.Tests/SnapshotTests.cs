using KeenEyes.Testing.Snapshots;

namespace KeenEyes.Testing.Tests;

/// <summary>
/// Unit tests for snapshot testing functionality.
/// </summary>
public partial class SnapshotTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct Position
    {
        public float X;
        public float Y;
    }

    [Component]
    private partial struct Velocity
    {
        public float VX;
        public float VY;
    }

    [Component]
    private partial struct Health
    {
        public int Current;
        public int Max;
    }
#pragma warning restore CS0649

    #region EntitySnapshot Tests

    [Fact]
    public void EntitySnapshot_Create_CapturesEntityId()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);

        // Assert
        Assert.Equal(entity.Id, snapshot.EntityId);
        Assert.Equal(entity.Version, snapshot.Version);
    }

    [Fact]
    public void EntitySnapshot_Create_CapturesEntityName()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn("TestEntity").With(new Position { X = 10, Y = 20 }).Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);

        // Assert
        Assert.Equal("TestEntity", snapshot.Name);
    }

    [Fact]
    public void EntitySnapshot_Create_CapturesComponentTypes()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .With(new Velocity { VX = 5, VY = 5 })
            .Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);

        // Assert
        Assert.Equal(2, snapshot.ComponentCount);
        Assert.True(snapshot.HasComponent("Position"));
        Assert.True(snapshot.HasComponent("Velocity"));
    }

    [Fact]
    public void EntitySnapshot_Create_CapturesFieldValues()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn()
            .With(new Position { X = 10, Y = 20 })
            .Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);

        // Assert
        Assert.Equal(10f, snapshot.GetFieldValue<float>("Position", "X"));
        Assert.Equal(20f, snapshot.GetFieldValue<float>("Position", "Y"));
    }

    [Fact]
    public void EntitySnapshot_Create_DeadEntity_Throws()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        world.Despawn(entity);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => EntitySnapshot.Create(world, entity));
    }

    [Fact]
    public void EntitySnapshot_HasComponent_Generic_Works()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);

        // Assert
        Assert.True(snapshot.HasComponent<Position>());
        Assert.False(snapshot.HasComponent<Velocity>());
    }

    [Fact]
    public void EntitySnapshot_GetComponent_ReturnsFieldData()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);
        var positionData = snapshot.GetComponent("Position");

        // Assert
        Assert.NotNull(positionData);
        Assert.Equal(10f, positionData["X"]);
        Assert.Equal(20f, positionData["Y"]);
    }

    [Fact]
    public void EntitySnapshot_GetComponent_NonExistent_ReturnsNull()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        // Act
        var snapshot = EntitySnapshot.Create(world, entity);
        var velocityData = snapshot.GetComponent("Velocity");

        // Assert
        Assert.Null(velocityData);
    }

    #endregion

    #region WorldSnapshot Tests

    [Fact]
    public void WorldSnapshot_Create_CapturesAllEntities()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
        world.Spawn().With(new Position { X = 10, Y = 10 }).Build();
        world.Spawn().With(new Position { X = 20, Y = 20 }).Build();

        // Act
        var snapshot = WorldSnapshot.Create(world);

        // Assert
        Assert.Equal(3, snapshot.EntityCount);
    }

    [Fact]
    public void WorldSnapshot_Create_EmptyWorld_HasNoEntities()
    {
        // Arrange
        using var world = new World();

        // Act
        var snapshot = WorldSnapshot.Create(world);

        // Assert
        Assert.Equal(0, snapshot.EntityCount);
    }

    [Fact]
    public void WorldSnapshot_Create_CapturesTimestamp()
    {
        // Arrange
        using var world = new World();
        var beforeCreate = DateTime.UtcNow;

        // Act
        var snapshot = WorldSnapshot.Create(world);
        var afterCreate = DateTime.UtcNow;

        // Assert
        Assert.InRange(snapshot.Timestamp, beforeCreate, afterCreate);
    }

    [Fact]
    public void WorldSnapshot_GetEntity_ReturnsCorrectEntity()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var snapshot = WorldSnapshot.Create(world);

        // Act
        var entitySnapshot = snapshot.GetEntity(entity.Id);

        // Assert
        Assert.NotNull(entitySnapshot);
        Assert.Equal(10f, entitySnapshot.GetFieldValue<float>("Position", "X"));
    }

    [Fact]
    public void WorldSnapshot_GetEntity_NonExistent_ReturnsNull()
    {
        // Arrange
        using var world = new World();
        var snapshot = WorldSnapshot.Create(world);

        // Act
        var entitySnapshot = snapshot.GetEntity(999);

        // Assert
        Assert.Null(entitySnapshot);
    }

    [Fact]
    public void WorldSnapshot_EntitiesWithComponent_ReturnsMatching()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
        world.Spawn().With(new Position { X = 10, Y = 10 }).With(new Velocity { VX = 1, VY = 1 }).Build();
        world.Spawn().With(new Velocity { VX = 2, VY = 2 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act
        var positionEntities = snapshot.EntitiesWithComponent<Position>().ToList();
        var velocityEntities = snapshot.EntitiesWithComponent<Velocity>().ToList();

        // Assert
        Assert.Equal(2, positionEntities.Count);
        Assert.Equal(2, velocityEntities.Count);
    }

    [Fact]
    public void WorldSnapshot_AllComponentTypes_ReturnsUniqueTypes()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
        world.Spawn().With(new Position { X = 10, Y = 10 }).With(new Velocity { VX = 1, VY = 1 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act
        var allTypes = snapshot.AllComponentTypes;

        // Assert
        Assert.Equal(2, allTypes.Count);
        Assert.Contains("Position", allTypes);
        Assert.Contains("Velocity", allTypes);
    }

    [Fact]
    public void WorldSnapshot_Create_WithSpecificEntities_OnlyIncludesThose()
    {
        // Arrange
        using var world = new World();
        var entity1 = world.Spawn().With(new Position { X = 0, Y = 0 }).Build();
        var entity2 = world.Spawn().With(new Position { X = 10, Y = 10 }).Build();
        world.Spawn().With(new Position { X = 20, Y = 20 }).Build(); // Not included

        // Act
        var snapshot = WorldSnapshot.Create(world, [entity1, entity2]);

        // Assert
        Assert.Equal(2, snapshot.EntityCount);
        Assert.NotNull(snapshot.GetEntity(entity1.Id));
        Assert.NotNull(snapshot.GetEntity(entity2.Id));
    }

    #endregion

    #region SnapshotComparer Tests

    [Fact]
    public void SnapshotComparer_Compare_IdenticalSnapshots_ReturnsEqual()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot1 = WorldSnapshot.Create(world);
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.True(comparison.AreEqual);
        Assert.Equal(0, comparison.DifferenceCount);
    }

    [Fact]
    public void SnapshotComparer_Compare_EntityAdded_DetectsDifference()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Spawn().With(new Position { X = 30, Y = 40 }).Build();
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.False(comparison.AreEqual);
        Assert.Single(comparison.GetDifferences(DifferenceType.EntityAdded));
    }

    [Fact]
    public void SnapshotComparer_Compare_EntityRemoved_DetectsDifference()
    {
        // Arrange
        using var world = new World();
        var entity1 = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var entity2 = world.Spawn().With(new Position { X = 30, Y = 40 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Despawn(entity2);
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.False(comparison.AreEqual);
        Assert.Single(comparison.GetDifferences(DifferenceType.EntityRemoved));
    }

    [Fact]
    public void SnapshotComparer_Compare_FieldChanged_DetectsDifference()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Get<Position>(entity).X = 100;
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.False(comparison.AreEqual);
        Assert.Single(comparison.GetDifferences(DifferenceType.FieldChanged));
    }

    [Fact]
    public void SnapshotComparer_Compare_ComponentAdded_DetectsDifference()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Add(entity, new Velocity { VX = 5, VY = 5 });
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.False(comparison.AreEqual);
        Assert.Single(comparison.GetDifferences(DifferenceType.ComponentAdded));
    }

    [Fact]
    public void SnapshotComparer_Compare_ComponentRemoved_DetectsDifference()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).With(new Velocity { VX = 5, VY = 5 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Remove<Velocity>(entity);
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);

        // Assert
        Assert.False(comparison.AreEqual);
        Assert.Single(comparison.GetDifferences(DifferenceType.ComponentRemoved));
    }

    [Fact]
    public void SnapshotComparer_Compare_GetReport_FormatsCorrectly()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        var snapshot1 = WorldSnapshot.Create(world);

        world.Get<Position>(entity).X = 100;
        var snapshot2 = WorldSnapshot.Create(world);

        // Act
        var comparison = SnapshotComparer.Compare(snapshot1, snapshot2);
        var report = comparison.GetReport();

        // Assert
        Assert.Contains("difference(s)", report);
        Assert.Contains("Position", report);
        Assert.Contains("X", report);
    }

    #endregion

    #region SnapshotAssertions Tests

    [Fact]
    public void SnapshotAssertions_ShouldEqual_WhenEqual_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var expected = WorldSnapshot.Create(world);
        var actual = WorldSnapshot.Create(world);

        // Act & Assert - should not throw
        actual.ShouldEqual(expected);
    }

    [Fact]
    public void SnapshotAssertions_ShouldEqual_WhenDifferent_Throws()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var expected = WorldSnapshot.Create(world);
        world.Get<Position>(entity).X = 100;
        var actual = WorldSnapshot.Create(world);

        // Act & Assert
        Assert.Throws<AssertionException>(() => actual.ShouldEqual(expected));
    }

    [Fact]
    public void SnapshotAssertions_ShouldHaveEntityCount_WhenMatches_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();
        world.Spawn().With(new Position { X = 30, Y = 40 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert - should not throw
        snapshot.ShouldHaveEntityCount(2);
    }

    [Fact]
    public void SnapshotAssertions_ShouldHaveEntityCount_WhenDifferent_Throws()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert
        Assert.Throws<AssertionException>(() => snapshot.ShouldHaveEntityCount(5));
    }

    [Fact]
    public void SnapshotAssertions_ShouldContainEntity_WhenExists_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert - should not throw
        snapshot.ShouldContainEntity(entity.Id);
    }

    [Fact]
    public void SnapshotAssertions_ShouldContainEntity_WhenMissing_Throws()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert
        Assert.Throws<AssertionException>(() => snapshot.ShouldContainEntity(999));
    }

    [Fact]
    public void SnapshotAssertions_ShouldHaveEntitiesWithComponent_WhenExist_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert - should not throw
        snapshot.ShouldHaveEntitiesWithComponent<Position>();
    }

    [Fact]
    public void SnapshotAssertions_ShouldHaveEntitiesWithComponent_WhenNone_Throws()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert
        Assert.Throws<AssertionException>(() =>
            snapshot.ShouldHaveEntitiesWithComponent<Velocity>());
    }

    [Fact]
    public void EntitySnapshotAssertions_ShouldHaveComponent_WhenExists_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = EntitySnapshot.Create(world, entity);

        // Act & Assert - should not throw
        snapshot.ShouldHaveComponent<Position>();
    }

    [Fact]
    public void EntitySnapshotAssertions_ShouldHaveComponent_WhenMissing_Throws()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = EntitySnapshot.Create(world, entity);

        // Act & Assert
        Assert.Throws<AssertionException>(() =>
            snapshot.ShouldHaveComponent<Velocity>());
    }

    [Fact]
    public void SnapshotAssertions_Chaining_Works()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var snapshot = WorldSnapshot.Create(world);

        // Act & Assert - all assertions should pass and chain
        snapshot
            .ShouldHaveEntityCount(1)
            .ShouldContainEntity(entity.Id)
            .ShouldHaveEntitiesWithComponent<Position>();
    }

    [Fact]
    public void SnapshotComparison_ShouldBeEqual_WhenEqual_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var expected = WorldSnapshot.Create(world);
        var actual = WorldSnapshot.Create(world);
        var comparison = SnapshotComparer.Compare(expected, actual);

        // Act & Assert - should not throw
        comparison.ShouldBeEqual();
    }

    [Fact]
    public void SnapshotComparison_ShouldNotBeEqual_WhenDifferent_DoesNotThrow()
    {
        // Arrange
        using var world = new World();
        var entity = world.Spawn().With(new Position { X = 10, Y = 20 }).Build();

        var expected = WorldSnapshot.Create(world);
        world.Get<Position>(entity).X = 100;
        var actual = WorldSnapshot.Create(world);
        var comparison = SnapshotComparer.Compare(expected, actual);

        // Act & Assert - should not throw
        comparison.ShouldNotBeEqual();
    }

    #endregion
}
