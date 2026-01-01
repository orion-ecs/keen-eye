using KeenEyes;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Tests.Commands;

public class ReparentEntityCommandTests : IDisposable
{
    private readonly World world;

    public ReparentEntityCommandTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsDescription_ForNewParent()
    {
        var child = world.Spawn("Child").Build();
        var parent = world.Spawn("NewParent").Build();
        var command = new ReparentEntityCommand(world, child, parent);

        Assert.Equal("Move 'Child' under 'NewParent'", command.Description);
    }

    [Fact]
    public void Constructor_SetsDescription_ForMoveToRoot()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);
        var command = new ReparentEntityCommand(world, child, Entity.Null);

        Assert.Equal("Move 'Child' to root", command.Description);
    }

    [Fact]
    public void Constructor_UsesEntityId_WhenNoName()
    {
        var child = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var command = new ReparentEntityCommand(world, child, parent);

        Assert.Contains($"Entity {child.Id}", command.Description);
    }

    #endregion

    #region Execute Tests

    [Fact]
    public void Execute_SetsNewParent()
    {
        var child = world.Spawn("Child").Build();
        var parent = world.Spawn("NewParent").Build();
        var command = new ReparentEntityCommand(world, child, parent);

        command.Execute();

        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void Execute_MovesToRoot()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);
        var command = new ReparentEntityCommand(world, child, Entity.Null);

        command.Execute();

        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    [Fact]
    public void Execute_ChangesParent_FromOneToAnother()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, oldParent);
        var command = new ReparentEntityCommand(world, child, newParent);

        command.Execute();

        Assert.Equal(newParent, world.GetParent(child));
    }

    [Fact]
    public void Execute_PreservesGrandchildren()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        var grandchild = world.Spawn("Grandchild").Build();
        world.SetParent(child, oldParent);
        world.SetParent(grandchild, child);
        var command = new ReparentEntityCommand(world, child, newParent);

        command.Execute();

        Assert.Equal(child, world.GetParent(grandchild));
    }

    #endregion

    #region Undo Tests

    [Fact]
    public void Undo_RestoresOriginalParent()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, oldParent);
        var command = new ReparentEntityCommand(world, child, newParent);
        command.Execute();

        command.Undo();

        Assert.Equal(oldParent, world.GetParent(child));
    }

    [Fact]
    public void Undo_RestoresRoot_WhenOriginallyRoot()
    {
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        var command = new ReparentEntityCommand(world, child, newParent);
        command.Execute();

        command.Undo();

        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    [Fact]
    public void Undo_MovesToRoot_WhenOldParentDespawned()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, oldParent);
        var command = new ReparentEntityCommand(world, child, newParent);
        command.Execute();

        // Despawn the old parent
        world.Despawn(oldParent);

        command.Undo();

        // Should move to root since old parent is dead
        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    [Fact]
    public void Undo_IsIdempotent()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, oldParent);
        var command = new ReparentEntityCommand(world, child, newParent);
        command.Execute();

        command.Undo();
        command.Undo(); // Should not throw

        Assert.Equal(oldParent, world.GetParent(child));
    }

    #endregion

    #region TryMerge Tests

    [Fact]
    public void TryMerge_ReturnsFalse()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var child = world.Spawn("Child").Build();

        var command1 = new ReparentEntityCommand(world, child, parent1);
        var command2 = new ReparentEntityCommand(world, child, parent2);

        Assert.False(command1.TryMerge(command2));
    }

    #endregion

    #region Execute/Undo Cycle Tests

    [Fact]
    public void ExecuteUndoExecute_RestoresNewParent()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, oldParent);
        var command = new ReparentEntityCommand(world, child, newParent);

        command.Execute();
        Assert.Equal(newParent, world.GetParent(child));

        command.Undo();
        Assert.Equal(oldParent, world.GetParent(child));

        command.Execute();
        Assert.Equal(newParent, world.GetParent(child));
    }

    [Fact]
    public void MultipleReparents_ChainedUndoRedo()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var parent3 = world.Spawn("Parent3").Build();
        var child = world.Spawn("Child").Build();

        // Important: Create each command AFTER executing the previous one
        // so it captures the correct old parent state
        var command1 = new ReparentEntityCommand(world, child, parent1);
        command1.Execute();
        Assert.Equal(parent1, world.GetParent(child));

        var command2 = new ReparentEntityCommand(world, child, parent2);
        command2.Execute();
        Assert.Equal(parent2, world.GetParent(child));

        var command3 = new ReparentEntityCommand(world, child, parent3);
        command3.Execute();
        Assert.Equal(parent3, world.GetParent(child));

        command3.Undo();
        Assert.Equal(parent2, world.GetParent(child));

        command2.Undo();
        Assert.Equal(parent1, world.GetParent(child));

        command1.Undo();
        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    #endregion

    #region Complex Hierarchy Tests

    [Fact]
    public void Execute_ReparentingParent_DoesNotAffectChildren()
    {
        var grandparent = world.Spawn("Grandparent").Build();
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var newGrandparent = world.Spawn("NewGrandparent").Build();
        var command = new ReparentEntityCommand(world, parent, newGrandparent);

        command.Execute();

        Assert.Equal(newGrandparent, world.GetParent(parent));
        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void Execute_ReparentingToChild_ThrowsCircularHierarchyException()
    {
        // Reparenting an entity under its own descendant would create a circular hierarchy
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        var command = new ReparentEntityCommand(world, parent, child);

        // Execute should throw because it would create a circular hierarchy
        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    #endregion
}
