namespace KeenEyes.Debugging.Tests;

/// <summary>
/// Unit tests for the <see cref="EntityInspector"/> class.
/// </summary>
public sealed class EntityInspectorTests
{
#pragma warning disable CS0649 // Field is never assigned to
    [Component]
    private partial struct TestComponent : IComponent
    {
        public int Value;
    }

    [Component]
    private partial struct HealthComponent : IComponent
    {
        public int Current;
        public int Max;
    }

    [TagComponent]
    private partial struct PlayerTag : IComponent;
#pragma warning restore CS0649

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullWorld_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new EntityInspector(null!));
    }

    [Fact]
    public void Constructor_WithValidWorld_Succeeds()
    {
        // Arrange
        using var world = new World();

        // Act & Assert - should not throw
        var inspector = new EntityInspector(world);
        Assert.NotNull(inspector);
    }

    #endregion

    #region Inspect Tests

    [Fact]
    public void Inspect_DeadEntity_ThrowsInvalidOperationException()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn().Build();
        world.Despawn(entity);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => inspector.Inspect(entity));
    }

    [Fact]
    public void Inspect_EntityWithNoComponents_ReturnsEmptyComponentList()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn().Build();

        // Act
        var info = inspector.Inspect(entity);

        // Assert
        Assert.Equal(entity, info.Entity);
        Assert.Empty(info.Components);
        Assert.Null(info.Name);
        Assert.Null(info.Parent);
        Assert.Empty(info.Children);
    }

    [Fact]
    public void Inspect_EntityWithComponents_ReturnsComponentList()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .With(new HealthComponent { Current = 100, Max = 100 })
            .Build();

        // Act
        var info = inspector.Inspect(entity);

        // Assert
        Assert.Equal(entity, info.Entity);
        Assert.Equal(2, info.Components.Count);
        Assert.Contains(info.Components, c => c.TypeName == nameof(TestComponent));
        Assert.Contains(info.Components, c => c.TypeName == nameof(HealthComponent));
    }

    [Fact]
    public void Inspect_NamedEntity_ReturnsEntityName()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn("TestEntity").Build();

        // Act
        var info = inspector.Inspect(entity);

        // Assert
        Assert.Equal("TestEntity", info.Name);
    }

    [Fact]
    public void Inspect_EntityWithParent_ReturnsParent()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        // Act
        var childInfo = inspector.Inspect(child);

        // Assert
        Assert.NotNull(childInfo.Parent);
        Assert.Equal(parent, childInfo.Parent!.Value);
    }

    [Fact]
    public void Inspect_EntityWithChildren_ReturnsChildren()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);

        // Act
        var parentInfo = inspector.Inspect(parent);

        // Assert
        Assert.Equal(2, parentInfo.Children.Count);
        Assert.Contains(child1, parentInfo.Children);
        Assert.Contains(child2, parentInfo.Children);
    }

    [Fact]
    public void Inspect_CompleteEntityHierarchy_ReturnsFullInformation()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var parent = world.Spawn("Parent")
            .With(new TestComponent { Value = 1 })
            .Build();

        var child = world.Spawn("Child")
            .With(new TestComponent { Value = 2 })
            .With(new HealthComponent { Current = 50, Max = 100 })
            .Build();
        world.SetParent(child, parent);

        // Act
        var info = inspector.Inspect(child);

        // Assert
        Assert.Equal(child, info.Entity);
        Assert.Equal("Child", info.Name);
        Assert.Equal(2, info.Components.Count);
        Assert.Equal(parent, info.Parent!.Value);
        Assert.Empty(info.Children);
    }

    [Fact]
    public void Inspect_ComponentInfo_ContainsTypeAndSize()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var info = inspector.Inspect(entity);

        // Assert
        var componentInfo = info.Components.First();
        Assert.Equal(nameof(TestComponent), componentInfo.TypeName);
        Assert.Equal(typeof(TestComponent), componentInfo.Type);
        Assert.True(componentInfo.SizeInBytes > 0);
    }

    #endregion

    #region GetAllEntities Tests

    [Fact]
    public void GetAllEntities_EmptyWorld_ReturnsEmptyList()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        // Act
        var entities = inspector.GetAllEntities();

        // Assert
        Assert.Empty(entities);
    }

    [Fact]
    public void GetAllEntities_WithEntities_ReturnsAllLivingEntities()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        // Act
        var entities = inspector.GetAllEntities();

        // Assert
        Assert.Equal(3, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.Contains(entity2, entities);
        Assert.Contains(entity3, entities);
    }

    [Fact]
    public void GetAllEntities_AfterDespawn_ExcludesDeadEntities()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity1 = world.Spawn().Build();
        var entity2 = world.Spawn().Build();
        var entity3 = world.Spawn().Build();

        world.Despawn(entity2);

        // Act
        var entities = inspector.GetAllEntities();

        // Assert
        Assert.Equal(2, entities.Count);
        Assert.Contains(entity1, entities);
        Assert.DoesNotContain(entity2, entities);
        Assert.Contains(entity3, entities);
    }

    [Fact]
    public void GetAllEntities_ReturnsSnapshot()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity1 = world.Spawn().Build();

        // Act
        var snapshot1 = inspector.GetAllEntities();

        var entity2 = world.Spawn().Build();

        var snapshot2 = inspector.GetAllEntities();

        // Assert - first snapshot should not include entity2
        Assert.Single(snapshot1);
        Assert.Equal(2, snapshot2.Count);
    }

    #endregion

    #region HasComponent Tests

    [Fact]
    public void HasComponent_Generic_EntityHasComponent_ReturnsTrue()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var result = inspector.HasComponent<TestComponent>(entity);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasComponent_Generic_EntityDoesNotHaveComponent_ReturnsFalse()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn().Build();

        // Act
        var result = inspector.HasComponent<TestComponent>(entity);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasComponent_NonGeneric_EntityHasComponent_ReturnsTrue()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn()
            .With(new TestComponent { Value = 42 })
            .Build();

        // Act
        var result = inspector.HasComponent(entity, typeof(TestComponent));

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasComponent_NonGeneric_EntityDoesNotHaveComponent_ReturnsFalse()
    {
        // Arrange
        using var world = new World();
        var inspector = new EntityInspector(world);

        var entity = world.Spawn().Build();

        // Act
        var result = inspector.HasComponent(entity, typeof(TestComponent));

        // Assert
        Assert.False(result);
    }

    #endregion
}
