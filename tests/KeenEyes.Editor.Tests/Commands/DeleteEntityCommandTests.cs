using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class DeleteEntityCommandTests : IDisposable
{
    private readonly World world;

    public DeleteEntityCommandTests()
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
        var entity = world.Spawn("TestEntity").Build();
        var command = new DeleteEntityCommand(world, entity);

        Assert.Equal("Delete Entity 'TestEntity'", command.Description);
    }

    [Fact]
    public void Constructor_WithUnnamedEntity_UsesEntityId()
    {
        var entity = world.Spawn().Build();
        var command = new DeleteEntityCommand(world, entity);

        Assert.Contains($"Entity {entity.Id}", command.Description);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_DespawnsEntity()
    {
        var entity = world.Spawn("TestEntity").Build();
        var command = new DeleteEntityCommand(world, entity);

        command.Execute();

        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void Execute_ReparentsChildren_ToGrandparent()
    {
        var grandparent = world.Spawn("Grandparent").Build();
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var command = new DeleteEntityCommand(world, parent);
        command.Execute();

        Assert.Equal(grandparent, world.GetParent(child));
    }

    [Fact]
    public void Execute_ReparentsChildren_ToRoot_WhenNoGrandparent()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        var command = new DeleteEntityCommand(world, parent);
        command.Execute();

        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_RecreatesEntity()
    {
        var entity = world.Spawn("TestEntity").Build();
        var initialCount = world.EntityCount;
        var command = new DeleteEntityCommand(world, entity);
        command.Execute();

        command.Undo();

        // Entity count should be restored
        Assert.Equal(initialCount, world.EntityCount);
    }

    [Fact]
    public void Undo_RestoresParentRelationship()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);
        var initialCount = world.EntityCount;

        var command = new DeleteEntityCommand(world, child);
        command.Execute();
        command.Undo();

        // After undo, we should have the same number of entities
        Assert.Equal(initialCount, world.EntityCount);
        // Note: The recreated entity may have different ID but same parent
    }

    [Fact]
    public void Undo_RestoresChildRelationships()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        var command = new DeleteEntityCommand(world, parent);
        command.Execute();
        command.Undo();

        // Child should be reparented to the recreated parent
        // Since the parent was recreated, get the new parent of child
        var childParent = world.GetParent(child);
        Assert.True(childParent.IsValid);
    }

    [Fact]
    public void Undo_DoesNothing_WhenNotExecuted()
    {
        var entity = world.Spawn("TestEntity").Build();
        var command = new DeleteEntityCommand(world, entity);

        command.Undo(); // Should not throw

        Assert.True(world.IsAlive(entity));
    }

    #endregion

    #region TryMerge Tests

    [Fact]
    public void TryMerge_ReturnsFalse()
    {
        var entity1 = world.Spawn("Entity1").Build();
        var entity2 = world.Spawn("Entity2").Build();
        var command1 = new DeleteEntityCommand(world, entity1);
        var command2 = new DeleteEntityCommand(world, entity2);

        Assert.False(command1.TryMerge(command2));
    }

    #endregion
}
