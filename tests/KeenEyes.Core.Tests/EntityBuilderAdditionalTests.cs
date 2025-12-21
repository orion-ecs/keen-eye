namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for EntityBuilder to improve coverage.
/// Focuses on uncovered paths: WithName, WithTag (string), WithParent, and interface implementations.
/// </summary>
public class EntityBuilderAdditionalTests
{
    #region Test Components

    public struct TestPosition : IComponent
    {
        public float X, Y;
    }

    public struct TestVelocity : IComponent
    {
        public float X, Y;
    }

    public struct TestTag : ITagComponent;

    #endregion

    #region WithName Tests

    [Fact]
    public void WithName_WithValidName_SetsEntityName()
    {
        using var world = new World();

        var entity = world.Spawn()
            .WithName("TestEntity")
            .With(new TestPosition { X = 1, Y = 2 })
            .Build();

        var foundEntity = world.GetEntityByName("TestEntity");
        Assert.True(foundEntity.IsValid);
        Assert.Equal(entity, foundEntity);
    }

    [Fact]
    public void WithName_WithNullName_CreatesEntityWithoutName()
    {
        using var world = new World();

        var entity = world.Spawn()
            .WithName(null)
            .With(new TestPosition { X = 1, Y = 2 })
            .Build();

        Assert.True(world.IsAlive(entity));
    }

    [Fact]
    public void WithName_ReturnsBuilder_ForChaining()
    {
        using var world = new World();

        var builder = world.Spawn();
        var result = builder.WithName("Test");

        Assert.Same(builder, result);
    }

    #endregion

    #region WithTag String Tests

    [Fact]
    public void WithTag_String_WithValidTag_AddsTag()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .WithTag("Enemy")
            .Build();

        Assert.True(world.HasTag(entity, "Enemy"));
    }

    [Fact]
    public void WithTag_String_WithNullTag_ThrowsArgumentNullException()
    {
        using var world = new World();

        var builder = world.Spawn();

        Assert.Throws<ArgumentNullException>(() => builder.WithTag(null!));
    }

    [Fact]
    public void WithTag_String_WithEmptyTag_ThrowsArgumentException()
    {
        using var world = new World();

        var builder = world.Spawn();

        Assert.Throws<ArgumentException>(() => builder.WithTag(""));
    }

    [Fact]
    public void WithTag_String_WithWhitespaceTag_ThrowsArgumentException()
    {
        using var world = new World();

        var builder = world.Spawn();

        Assert.Throws<ArgumentException>(() => builder.WithTag("   "));
    }

    [Fact]
    public void WithTag_String_MultipleTags_AddsAllTags()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .WithTag("Enemy")
            .WithTag("Hostile")
            .WithTag("Boss")
            .Build();

        Assert.True(world.HasTag(entity, "Enemy"));
        Assert.True(world.HasTag(entity, "Hostile"));
        Assert.True(world.HasTag(entity, "Boss"));
    }

    #endregion

    #region WithParent Tests

    [Fact]
    public void WithParent_WithValidParent_EstablishesHierarchy()
    {
        using var world = new World();

        var parent = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();

        var child = world.Spawn()
            .With(new TestPosition { X = 1, Y = 1 })
            .WithParent(parent)
            .Build();

        var childParent = world.GetParent(child);
        Assert.True(childParent.IsValid);
        Assert.Equal(parent, childParent);
    }

    [Fact]
    public void WithParent_WithInvalidParent_ThrowsInvalidOperationException()
    {
        using var world = new World();

        var invalidParent = new Entity { Id = 9999, Version = 1 };

        var builder = world.Spawn()
            .With(new TestPosition { X = 1, Y = 1 })
            .WithParent(invalidParent);

        // Should throw when trying to build with invalid parent
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void WithParent_ReturnsBuilder_ForChaining()
    {
        using var world = new World();

        var parent = world.Spawn().Build();
        var builder = world.Spawn();
        var result = builder.WithParent(parent);

        Assert.Same(builder, result);
    }

    #endregion

    #region Build String Tags Tests

    [Fact]
    public void Build_AppliesStringTags_AfterEntityCreation()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .WithTag("Tag1")
            .WithTag("Tag2")
            .Build();

        Assert.True(world.HasTag(entity, "Tag1"));
        Assert.True(world.HasTag(entity, "Tag2"));
    }

    [Fact]
    public void Build_WithNoStringTags_DoesNotThrow()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 1, Y = 2 })
            .Build();

        Assert.True(world.IsAlive(entity));
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void IEntityBuilder_With_CallsConcreteImplementation()
    {
        using var world = new World();

        IEntityBuilder builder = world.Spawn();
        var result = builder.With(new TestPosition { X = 1, Y = 2 });

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEntityBuilder>(result);
    }

    [Fact]
    public void IEntityBuilder_WithTag_CallsConcreteImplementation()
    {
        using var world = new World();

        IEntityBuilder builder = world.Spawn();
        var result = builder.WithTag<TestTag>();

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEntityBuilder>(result);
    }

    [Fact]
    public void IEntityBuilder_WithParent_CallsConcreteImplementation()
    {
        using var world = new World();

        var parent = world.Spawn().Build();
        IEntityBuilder builder = world.Spawn();
        var result = builder.WithParent(parent);

        Assert.NotNull(result);
        Assert.IsAssignableFrom<IEntityBuilder>(result);
    }

    #endregion

    #region Combined Tests

    [Fact]
    public void Build_WithNameParentAndTags_AppliesAllProperties()
    {
        using var world = new World();

        var parent = world.Spawn()
            .WithName("Parent")
            .Build();

        var child = world.Spawn()
            .WithName("Child")
            .With(new TestPosition { X = 1, Y = 2 })
            .WithTag<TestTag>()
            .WithTag("StringTag")
            .WithParent(parent)
            .Build();

        // Verify name
        var foundChild = world.GetEntityByName("Child");
        Assert.True(foundChild.IsValid);
        Assert.Equal(child, foundChild);

        // Verify parent
        var childParent = world.GetParent(child);
        Assert.True(childParent.IsValid);
        Assert.Equal(parent, childParent);

        // Verify tags
        Assert.True(world.Has<TestTag>(child));
        Assert.True(world.HasTag(child, "StringTag"));

        // Verify component
        Assert.True(world.Has<TestPosition>(child));
    }

    #endregion
}
