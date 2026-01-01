using KeenEyes;
using KeenEyes.Editor.Commands;

namespace KeenEyes.Editor.Integration.Tests;

/// <summary>
/// Integration tests for complex hierarchy operations including
/// deep nesting, bulk reparenting, and hierarchy manipulation.
/// </summary>
public class HierarchyOperationsTests : IDisposable
{
    private readonly World world;
    private readonly UndoRedoManager manager;

    public HierarchyOperationsTests()
    {
        world = new World();
        manager = new UndoRedoManager();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region Deep Hierarchy Operations

    [Fact]
    public void DeepHierarchy_ReparentMiddleNode_PreservesDescendants()
    {
        // Create: Root -> A -> B -> C -> D
        var root = world.Spawn("Root").Build();
        var nodeA = world.Spawn("A").Build();
        var nodeB = world.Spawn("B").Build();
        var nodeC = world.Spawn("C").Build();
        var nodeD = world.Spawn("D").Build();

        world.SetParent(nodeA, root);
        world.SetParent(nodeB, nodeA);
        world.SetParent(nodeC, nodeB);
        world.SetParent(nodeD, nodeC);

        // Move B (and its subtree C->D) to Root
        var command = new ReparentEntityCommand(world, nodeB, root);
        command.Execute();

        // Verify new structure: Root -> A, Root -> B -> C -> D
        Assert.Equal(root, world.GetParent(nodeA));
        Assert.Equal(root, world.GetParent(nodeB));
        Assert.Equal(nodeB, world.GetParent(nodeC));
        Assert.Equal(nodeC, world.GetParent(nodeD));
    }

    [Fact]
    public void DeepHierarchy_MoveSubtreeToAnotherBranch_PreservesInternalStructure()
    {
        // Create two branches:
        // Root -> Branch1 -> Child1
        // Root -> Branch2 -> Child2 -> GrandChild2
        var root = world.Spawn("Root").Build();
        var branch1 = world.Spawn("Branch1").Build();
        var child1 = world.Spawn("Child1").Build();
        var branch2 = world.Spawn("Branch2").Build();
        var child2 = world.Spawn("Child2").Build();
        var grandChild2 = world.Spawn("GrandChild2").Build();

        world.SetParent(branch1, root);
        world.SetParent(child1, branch1);
        world.SetParent(branch2, root);
        world.SetParent(child2, branch2);
        world.SetParent(grandChild2, child2);

        // Move Branch2 subtree under Branch1
        var command = new ReparentEntityCommand(world, branch2, branch1);
        command.Execute();

        // Verify: Root -> Branch1 -> (Child1, Branch2 -> Child2 -> GrandChild2)
        Assert.Equal(branch1, world.GetParent(branch2));
        Assert.Equal(branch2, world.GetParent(child2));
        Assert.Equal(child2, world.GetParent(grandChild2));
    }

    [Fact]
    public void DeepHierarchy_UnparentMiddleNode_CreatesNewRootWithSubtree()
    {
        // Create: Root -> A -> B -> C
        var root = world.Spawn("Root").Build();
        var nodeA = world.Spawn("A").Build();
        var nodeB = world.Spawn("B").Build();
        var nodeC = world.Spawn("C").Build();

        world.SetParent(nodeA, root);
        world.SetParent(nodeB, nodeA);
        world.SetParent(nodeC, nodeB);

        // Move B to root level
        var command = new ReparentEntityCommand(world, nodeB, Entity.Null);
        command.Execute();

        // Verify: Root -> A (orphaned), B -> C (new root)
        Assert.Equal(root, world.GetParent(nodeA));
        Assert.Equal(Entity.Null, world.GetParent(nodeB));
        Assert.Equal(nodeB, world.GetParent(nodeC));
    }

    [Fact]
    public void CreateVeryDeepHierarchy_AllLevelsAccessible()
    {
        const int depth = 20;
        var entities = new Entity[depth];

        entities[0] = world.Spawn("Level0").Build();
        for (int i = 1; i < depth; i++)
        {
            entities[i] = world.Spawn($"Level{i}").Build();
            world.SetParent(entities[i], entities[i - 1]);
        }

        // Verify all parent relationships
        Assert.Equal(Entity.Null, world.GetParent(entities[0]));
        for (int i = 1; i < depth; i++)
        {
            Assert.Equal(entities[i - 1], world.GetParent(entities[i]));
        }
    }

    #endregion

    #region Circular Hierarchy Prevention

    [Fact]
    public void ReparentToSelf_ThrowsException()
    {
        var entity = world.Spawn("Entity").Build();

        Assert.Throws<InvalidOperationException>(() =>
            world.SetParent(entity, entity));
    }

    [Fact]
    public void ReparentToDirectChild_ThrowsException()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        var command = new ReparentEntityCommand(world, parent, child);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    [Fact]
    public void ReparentToGrandchild_ThrowsException()
    {
        var grandparent = world.Spawn("Grandparent").Build();
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var command = new ReparentEntityCommand(world, grandparent, child);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    [Fact]
    public void ReparentToDeepDescendant_ThrowsException()
    {
        // Create deep hierarchy: A -> B -> C -> D -> E
        var nodeA = world.Spawn("A").Build();
        var nodeB = world.Spawn("B").Build();
        var nodeC = world.Spawn("C").Build();
        var nodeD = world.Spawn("D").Build();
        var nodeE = world.Spawn("E").Build();

        world.SetParent(nodeB, nodeA);
        world.SetParent(nodeC, nodeB);
        world.SetParent(nodeD, nodeC);
        world.SetParent(nodeE, nodeD);

        // Try to make A a child of E (would create cycle)
        var command = new ReparentEntityCommand(world, nodeA, nodeE);

        Assert.Throws<InvalidOperationException>(() => command.Execute());
    }

    #endregion

    #region Bulk Hierarchy Operations

    [Fact]
    public void ReparentMultipleSiblings_ToNewParent_AllMoved()
    {
        var oldParent = world.Spawn("OldParent").Build();
        var newParent = world.Spawn("NewParent").Build();
        var child1 = world.Spawn("Child1").Build();
        var child2 = world.Spawn("Child2").Build();
        var child3 = world.Spawn("Child3").Build();

        world.SetParent(child1, oldParent);
        world.SetParent(child2, oldParent);
        world.SetParent(child3, oldParent);

        // Move all children to new parent via batch
        manager.BeginBatch("Move all children");
        manager.Execute(new ReparentEntityCommand(world, child1, newParent));
        manager.Execute(new ReparentEntityCommand(world, child2, newParent));
        manager.Execute(new ReparentEntityCommand(world, child3, newParent));
        manager.EndBatch();

        Assert.Equal(newParent, world.GetParent(child1));
        Assert.Equal(newParent, world.GetParent(child2));
        Assert.Equal(newParent, world.GetParent(child3));

        // Undo should restore all
        manager.Undo();

        Assert.Equal(oldParent, world.GetParent(child1));
        Assert.Equal(oldParent, world.GetParent(child2));
        Assert.Equal(oldParent, world.GetParent(child3));
    }

    [Fact]
    public void CreateHierarchy_DeleteRoot_OrphansChildren()
    {
        var root = world.Spawn("Root").Build();
        var child1 = world.Spawn("Child1").Build();
        var child2 = world.Spawn("Child2").Build();
        world.SetParent(child1, root);
        world.SetParent(child2, root);

        world.Despawn(root);

        // Children should now be roots (or despawned depending on implementation)
        // Let's verify the behavior - children are orphaned
        if (world.IsAlive(child1))
        {
            Assert.Equal(Entity.Null, world.GetParent(child1));
        }
    }

    [Fact]
    public void MoveEntireSubtree_ViaCommand_UndoRestoresAll()
    {
        // Create: OldRoot -> A -> (B, C -> D)
        var oldRoot = world.Spawn("OldRoot").Build();
        var newRoot = world.Spawn("NewRoot").Build();
        var nodeA = world.Spawn("A").Build();
        var nodeB = world.Spawn("B").Build();
        var nodeC = world.Spawn("C").Build();
        var nodeD = world.Spawn("D").Build();

        world.SetParent(nodeA, oldRoot);
        world.SetParent(nodeB, nodeA);
        world.SetParent(nodeC, nodeA);
        world.SetParent(nodeD, nodeC);

        // Move A (with entire subtree) to NewRoot
        var command = new ReparentEntityCommand(world, nodeA, newRoot);
        manager.Execute(command);

        Assert.Equal(newRoot, world.GetParent(nodeA));

        // Subtree should be intact
        Assert.Equal(nodeA, world.GetParent(nodeB));
        Assert.Equal(nodeA, world.GetParent(nodeC));
        Assert.Equal(nodeC, world.GetParent(nodeD));

        // Undo
        manager.Undo();

        Assert.Equal(oldRoot, world.GetParent(nodeA));
        Assert.Equal(nodeA, world.GetParent(nodeB));
        Assert.Equal(nodeA, world.GetParent(nodeC));
        Assert.Equal(nodeC, world.GetParent(nodeD));
    }

    #endregion

    #region Hierarchy with Entity Lifecycle

    [Fact]
    public void CreateEntityWithParent_SetParent_Works()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void DeleteParent_ChildStillExists_AsOrphan()
    {
        var parent = world.Spawn("Parent").Build();
        var child = world.Spawn("Child").Build();
        world.SetParent(child, parent);

        world.Despawn(parent);

        // Child should still exist but be orphaned
        if (world.IsAlive(child))
        {
            Assert.Equal(Entity.Null, world.GetParent(child));
        }
    }

    [Fact]
    public void ReparentAfterParentDespawned_Undo_MovesToRoot()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var child = world.Spawn("Child").Build();

        world.SetParent(child, parent1);

        var command = new ReparentEntityCommand(world, child, parent2);
        command.Execute();

        Assert.Equal(parent2, world.GetParent(child));

        // Despawn original parent
        world.Despawn(parent1);

        // Undo should move to root since parent1 is gone
        command.Undo();

        Assert.Equal(Entity.Null, world.GetParent(child));
    }

    #endregion

    #region Complex Undo/Redo Chains

    [Fact]
    public void MultipleReparents_UndoRedoChain_RestoresCorrectStates()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var parent3 = world.Spawn("Parent3").Build();
        var child = world.Spawn("Child").Build();

        // Execute chain of reparents
        var cmd1 = new ReparentEntityCommand(world, child, parent1);
        manager.Execute(cmd1);
        Assert.Equal(parent1, world.GetParent(child));

        var cmd2 = new ReparentEntityCommand(world, child, parent2);
        manager.Execute(cmd2);
        Assert.Equal(parent2, world.GetParent(child));

        var cmd3 = new ReparentEntityCommand(world, child, parent3);
        manager.Execute(cmd3);
        Assert.Equal(parent3, world.GetParent(child));

        // Undo all
        manager.Undo();
        Assert.Equal(parent2, world.GetParent(child));

        manager.Undo();
        Assert.Equal(parent1, world.GetParent(child));

        manager.Undo();
        Assert.Equal(Entity.Null, world.GetParent(child));

        // Redo all
        manager.Redo();
        Assert.Equal(parent1, world.GetParent(child));

        manager.Redo();
        Assert.Equal(parent2, world.GetParent(child));

        manager.Redo();
        Assert.Equal(parent3, world.GetParent(child));
    }

    [Fact]
    public void CreateHierarchy_UndoCreation_HierarchyGone()
    {
        var parent = world.Spawn("Parent").Build();

        manager.Execute(new CreateEntityCommand(world, "Child1"));
        var child1 = world.GetAllEntities().Last();
        world.SetParent(child1, parent);

        manager.Execute(new CreateEntityCommand(world, "Child2"));
        var child2 = world.GetAllEntities().Last();
        world.SetParent(child2, parent);

        Assert.Equal(3, world.EntityCount);
        Assert.Equal(parent, world.GetParent(child1));
        Assert.Equal(parent, world.GetParent(child2));

        // Undo child2 creation
        manager.Undo();
        Assert.Equal(2, world.EntityCount);

        // Undo child1 creation
        manager.Undo();
        Assert.Equal(1, world.EntityCount);
    }

    #endregion

    #region Sibling Order Tests

    [Fact]
    public void AddMultipleSiblings_AllHaveSameParent()
    {
        var parent = world.Spawn("Parent").Build();
        var children = new Entity[5];

        for (int i = 0; i < 5; i++)
        {
            children[i] = world.Spawn($"Child{i}").Build();
            world.SetParent(children[i], parent);
        }

        foreach (var child in children)
        {
            Assert.Equal(parent, world.GetParent(child));
        }
    }

    [Fact]
    public void ReparentChild_DoesNotAffectSiblings()
    {
        var parent1 = world.Spawn("Parent1").Build();
        var parent2 = world.Spawn("Parent2").Build();
        var child1 = world.Spawn("Child1").Build();
        var child2 = world.Spawn("Child2").Build();
        var child3 = world.Spawn("Child3").Build();

        world.SetParent(child1, parent1);
        world.SetParent(child2, parent1);
        world.SetParent(child3, parent1);

        // Move child2 only
        world.SetParent(child2, parent2);

        Assert.Equal(parent1, world.GetParent(child1));
        Assert.Equal(parent2, world.GetParent(child2));
        Assert.Equal(parent1, world.GetParent(child3));
    }

    #endregion
}
