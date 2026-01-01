using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class CreateEntityCommandTests : IDisposable
{
    private readonly World world;

    public CreateEntityCommandTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDescription()
    {
        var command = new CreateEntityCommand(world, "TestEntity");

        Assert.Equal("Create Entity 'TestEntity'", command.Description);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_CreatesEntity()
    {
        var command = new CreateEntityCommand(world, "TestEntity");

        command.Execute();

        Assert.True(command.CreatedEntity.IsValid);
        Assert.True(world.IsAlive(command.CreatedEntity));
    }

    [Fact]
    public void Execute_SetsEntityName()
    {
        var command = new CreateEntityCommand(world, "TestEntity");

        command.Execute();

        var name = world.GetName(command.CreatedEntity);
        Assert.Equal("TestEntity", name);
    }

    [Fact]
    public void Execute_WithParent_SetsParent()
    {
        var parent = world.Spawn("Parent").Build();
        var command = new CreateEntityCommand(world, "Child", parent);

        command.Execute();

        Assert.Equal(parent, world.GetParent(command.CreatedEntity));
    }

    [Fact]
    public void Execute_WithoutParent_HasNoParent()
    {
        var command = new CreateEntityCommand(world, "RootEntity");

        command.Execute();

        Assert.Equal(Entity.Null, world.GetParent(command.CreatedEntity));
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_DespawnsEntity()
    {
        var command = new CreateEntityCommand(world, "TestEntity");
        command.Execute();
        var createdEntity = command.CreatedEntity;

        command.Undo();

        Assert.False(world.IsAlive(createdEntity));
    }

    [Fact]
    public void Undo_SetsCreatedEntityToNull()
    {
        var command = new CreateEntityCommand(world, "TestEntity");
        command.Execute();

        command.Undo();

        Assert.Equal(Entity.Null, command.CreatedEntity);
    }

    [Fact]
    public void Undo_IsIdempotent()
    {
        var command = new CreateEntityCommand(world, "TestEntity");
        command.Execute();

        command.Undo();
        command.Undo(); // Should not throw

        Assert.Equal(Entity.Null, command.CreatedEntity);
    }

    #endregion

    #region TryMerge Tests

    [Fact]
    public void TryMerge_ReturnsFalse()
    {
        var command1 = new CreateEntityCommand(world, "Entity1");
        var command2 = new CreateEntityCommand(world, "Entity2");

        Assert.False(command1.TryMerge(command2));
    }

    #endregion
}
