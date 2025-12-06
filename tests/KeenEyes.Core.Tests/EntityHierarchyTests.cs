namespace KeenEyes.Tests;

/// <summary>
/// Tests for entity hierarchy (parent-child relationships) functionality.
/// </summary>
public class EntityHierarchyTests
{
    #region SetParent Tests

    [Fact]
    public void SetParent_WithValidEntities_EstablishesRelationship()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, parent);

        Assert.Equal(parent, world.GetParent(child));
    }

    [Fact]
    public void SetParent_WithEntityNull_RemovesParent()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        world.SetParent(child, Entity.Null);

        Assert.False(world.GetParent(child).IsValid);
    }

    [Fact]
    public void SetParent_WithDeadChild_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.Despawn(child);

        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(child, parent));
        Assert.Contains("not alive", ex.Message);
    }

    [Fact]
    public void SetParent_WithDeadParent_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.Despawn(parent);

        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(child, parent));
        Assert.Contains("not alive", ex.Message);
    }

    [Fact]
    public void SetParent_WithSelfAsParent_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(entity, entity));
        Assert.Contains("cannot be its own parent", ex.Message);
    }

    [Fact]
    public void SetParent_WithCircularRelationship_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        // Try to make grandparent a child of child (circular)
        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(grandparent, child));
        Assert.Contains("circular hierarchy", ex.Message);
    }

    [Fact]
    public void SetParent_WithDirectCircularRelationship_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, parent);

        // Try to make parent a child of its own child
        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(parent, child));
        Assert.Contains("circular hierarchy", ex.Message);
    }

    [Fact]
    public void SetParent_ChangesExistingParent_RemovesFromOldParent()
    {
        using var world = new World();
        var oldParent = world.Spawn().Build();
        var newParent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, oldParent);
        world.SetParent(child, newParent);

        Assert.Equal(newParent, world.GetParent(child));
        Assert.Empty(world.GetChildren(oldParent));
        Assert.Single(world.GetChildren(newParent));
    }

    [Fact]
    public void SetParent_SameParentTwice_DoesNotDuplicateChild()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, parent);
        world.SetParent(child, parent);

        Assert.Single(world.GetChildren(parent));
    }

    #endregion

    #region GetParent Tests

    [Fact]
    public void GetParent_WithNoParent_ReturnsEntityNull()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var parent = world.GetParent(entity);

        Assert.False(parent.IsValid);
    }

    [Fact]
    public void GetParent_WithStaleEntity_ReturnsEntityNull()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);
        world.Despawn(child);

        var foundParent = world.GetParent(child);

        Assert.False(foundParent.IsValid);
    }

    [Fact]
    public void GetParent_WithParent_ReturnsParent()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        var foundParent = world.GetParent(child);

        Assert.Equal(parent, foundParent);
    }

    #endregion

    #region GetChildren Tests

    [Fact]
    public void GetChildren_WithNoChildren_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var children = world.GetChildren(entity).ToList();

        Assert.Empty(children);
    }

    [Fact]
    public void GetChildren_WithStaleEntity_ReturnsEmpty()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);
        world.Despawn(parent);

        var children = world.GetChildren(parent).ToList();

        Assert.Empty(children);
    }

    [Fact]
    public void GetChildren_WithMultipleChildren_ReturnsAllChildren()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var child3 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);
        world.SetParent(child3, parent);

        var children = world.GetChildren(parent).ToList();

        Assert.Equal(3, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
        Assert.Contains(child3, children);
    }

    [Fact]
    public void GetChildren_OnlyReturnsImmediateChildren()
    {
        using var world = new World();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var children = world.GetChildren(grandparent).ToList();

        Assert.Single(children);
        Assert.Equal(parent, children[0]);
        Assert.DoesNotContain(child, children);
    }

    #endregion

    #region AddChild Tests

    [Fact]
    public void AddChild_WithValidEntities_EstablishesRelationship()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.AddChild(parent, child);

        Assert.Equal(parent, world.GetParent(child));
        Assert.Contains(child, world.GetChildren(parent));
    }

    [Fact]
    public void AddChild_WithDeadParent_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.Despawn(parent);

        var ex = Assert.Throws<InvalidOperationException>(() => world.AddChild(parent, child));
        Assert.Contains("not alive", ex.Message);
    }

    [Fact]
    public void AddChild_WithDeadChild_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.Despawn(child);

        var ex = Assert.Throws<InvalidOperationException>(() => world.AddChild(parent, child));
        Assert.Contains("not alive", ex.Message);
    }

    #endregion

    #region RemoveChild Tests

    [Fact]
    public void RemoveChild_WithValidRelationship_ReturnsTrue()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        var removed = world.RemoveChild(parent, child);

        Assert.True(removed);
        Assert.False(world.GetParent(child).IsValid);
        Assert.Empty(world.GetChildren(parent));
    }

    [Fact]
    public void RemoveChild_WithNoRelationship_ReturnsFalse()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var notChild = world.Spawn().Build();

        var removed = world.RemoveChild(parent, notChild);

        Assert.False(removed);
    }

    [Fact]
    public void RemoveChild_WithDifferentParent_ReturnsFalse()
    {
        using var world = new World();
        var parent1 = world.Spawn().Build();
        var parent2 = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent1);

        var removed = world.RemoveChild(parent2, child);

        Assert.False(removed);
        Assert.Equal(parent1, world.GetParent(child));
    }

    [Fact]
    public void RemoveChild_WithDeadParent_ReturnsFalse()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);
        world.Despawn(parent);

        var removed = world.RemoveChild(parent, child);

        Assert.False(removed);
    }

    [Fact]
    public void RemoveChild_WithDeadChild_ReturnsFalse()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);
        world.Despawn(child);

        var removed = world.RemoveChild(parent, child);

        Assert.False(removed);
    }

    [Fact]
    public void RemoveChild_Idempotent_ReturnsFalseOnSecondCall()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        world.RemoveChild(parent, child);
        var removedAgain = world.RemoveChild(parent, child);

        Assert.False(removedAgain);
    }

    #endregion

    #region GetDescendants Tests

    [Fact]
    public void GetDescendants_WithNoChildren_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var descendants = world.GetDescendants(entity).ToList();

        Assert.Empty(descendants);
    }

    [Fact]
    public void GetDescendants_WithStaleEntity_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var descendants = world.GetDescendants(entity).ToList();

        Assert.Empty(descendants);
    }

    [Fact]
    public void GetDescendants_WithChildren_ReturnsAllDescendants()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var grandchild1 = world.Spawn().Build();
        var grandchild2 = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);
        world.SetParent(grandchild1, child1);
        world.SetParent(grandchild2, child1);

        var descendants = world.GetDescendants(root).ToList();

        Assert.Equal(4, descendants.Count);
        Assert.Contains(child1, descendants);
        Assert.Contains(child2, descendants);
        Assert.Contains(grandchild1, descendants);
        Assert.Contains(grandchild2, descendants);
    }

    [Fact]
    public void GetDescendants_DoesNotIncludeSelf()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, root);

        var descendants = world.GetDescendants(root).ToList();

        Assert.DoesNotContain(root, descendants);
    }

    [Fact]
    public void GetDescendants_ReturnsInBreadthFirstOrder()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child = world.Spawn().Build();
        var grandchild = world.Spawn().Build();
        var greatGrandchild = world.Spawn().Build();

        world.SetParent(child, root);
        world.SetParent(grandchild, child);
        world.SetParent(greatGrandchild, grandchild);

        var descendants = world.GetDescendants(root).ToList();

        // Breadth-first: child should come before grandchild, grandchild before greatGrandchild
        Assert.Equal(3, descendants.Count);
        Assert.True(descendants.IndexOf(child) < descendants.IndexOf(grandchild));
        Assert.True(descendants.IndexOf(grandchild) < descendants.IndexOf(greatGrandchild));
    }

    #endregion

    #region GetAncestors Tests

    [Fact]
    public void GetAncestors_WithNoParent_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var ancestors = world.GetAncestors(entity).ToList();

        Assert.Empty(ancestors);
    }

    [Fact]
    public void GetAncestors_WithStaleEntity_ReturnsEmpty()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var ancestors = world.GetAncestors(entity).ToList();

        Assert.Empty(ancestors);
    }

    [Fact]
    public void GetAncestors_WithMultipleAncestors_ReturnsAllAncestors()
    {
        using var world = new World();
        var greatGrandparent = world.Spawn().Build();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(grandparent, greatGrandparent);
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var ancestors = world.GetAncestors(child).ToList();

        Assert.Equal(3, ancestors.Count);
        Assert.Contains(parent, ancestors);
        Assert.Contains(grandparent, ancestors);
        Assert.Contains(greatGrandparent, ancestors);
    }

    [Fact]
    public void GetAncestors_DoesNotIncludeSelf()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        var ancestors = world.GetAncestors(child).ToList();

        Assert.DoesNotContain(child, ancestors);
    }

    [Fact]
    public void GetAncestors_ReturnsInOrderFromParentToRoot()
    {
        using var world = new World();
        var greatGrandparent = world.Spawn().Build();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(grandparent, greatGrandparent);
        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        var ancestors = world.GetAncestors(child).ToList();

        Assert.Equal(3, ancestors.Count);
        Assert.Equal(parent, ancestors[0]);
        Assert.Equal(grandparent, ancestors[1]);
        Assert.Equal(greatGrandparent, ancestors[2]);
    }

    #endregion

    #region GetRoot Tests

    [Fact]
    public void GetRoot_WithNoParent_ReturnsSelf()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var root = world.GetRoot(entity);

        Assert.Equal(entity, root);
    }

    [Fact]
    public void GetRoot_WithStaleEntity_ReturnsEntityNull()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var root = world.GetRoot(entity);

        Assert.False(root.IsValid);
    }

    [Fact]
    public void GetRoot_WithParent_ReturnsRoot()
    {
        using var world = new World();
        var rootEntity = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, rootEntity);

        var foundRoot = world.GetRoot(child);

        Assert.Equal(rootEntity, foundRoot);
    }

    [Fact]
    public void GetRoot_WithDeepHierarchy_ReturnsTopmost()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var level1 = world.Spawn().Build();
        var level2 = world.Spawn().Build();
        var level3 = world.Spawn().Build();
        var level4 = world.Spawn().Build();

        world.SetParent(level1, root);
        world.SetParent(level2, level1);
        world.SetParent(level3, level2);
        world.SetParent(level4, level3);

        var foundRoot = world.GetRoot(level4);

        Assert.Equal(root, foundRoot);
    }

    [Fact]
    public void GetRoot_AtDifferentLevels_ReturnsSameRoot()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var grandchild = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);
        world.SetParent(grandchild, child1);

        Assert.Equal(root, world.GetRoot(child1));
        Assert.Equal(root, world.GetRoot(child2));
        Assert.Equal(root, world.GetRoot(grandchild));
        Assert.Equal(root, world.GetRoot(root));
    }

    #endregion

    #region DespawnRecursive Tests

    [Fact]
    public void DespawnRecursive_WithNoChildren_DespawnsOnlyEntity()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        var count = world.DespawnRecursive(entity);

        Assert.Equal(1, count);
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void DespawnRecursive_WithStaleEntity_ReturnsZero()
    {
        using var world = new World();
        var entity = world.Spawn().Build();
        world.Despawn(entity);

        var count = world.DespawnRecursive(entity);

        Assert.Equal(0, count);
    }

    [Fact]
    public void DespawnRecursive_WithChildren_DespawnsAll()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);

        var count = world.DespawnRecursive(root);

        Assert.Equal(3, count);
        Assert.False(world.IsAlive(root));
        Assert.False(world.IsAlive(child1));
        Assert.False(world.IsAlive(child2));
    }

    [Fact]
    public void DespawnRecursive_WithDeepHierarchy_DespawnsAll()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child = world.Spawn().Build();
        var grandchild = world.Spawn().Build();
        var greatGrandchild = world.Spawn().Build();

        world.SetParent(child, root);
        world.SetParent(grandchild, child);
        world.SetParent(greatGrandchild, grandchild);

        var count = world.DespawnRecursive(root);

        Assert.Equal(4, count);
        Assert.False(world.IsAlive(root));
        Assert.False(world.IsAlive(child));
        Assert.False(world.IsAlive(grandchild));
        Assert.False(world.IsAlive(greatGrandchild));
    }

    [Fact]
    public void DespawnRecursive_WithBranchingHierarchy_DespawnsAll()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var grandchild1a = world.Spawn().Build();
        var grandchild1b = world.Spawn().Build();
        var grandchild2a = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);
        world.SetParent(grandchild1a, child1);
        world.SetParent(grandchild1b, child1);
        world.SetParent(grandchild2a, child2);

        var count = world.DespawnRecursive(root);

        Assert.Equal(6, count);
        Assert.False(world.IsAlive(root));
        Assert.False(world.IsAlive(child1));
        Assert.False(world.IsAlive(child2));
        Assert.False(world.IsAlive(grandchild1a));
        Assert.False(world.IsAlive(grandchild1b));
        Assert.False(world.IsAlive(grandchild2a));
    }

    [Fact]
    public void DespawnRecursive_MiddleNode_KeepsParentAlive()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child = world.Spawn().Build();
        var grandchild = world.Spawn().Build();

        world.SetParent(child, root);
        world.SetParent(grandchild, child);

        var count = world.DespawnRecursive(child);

        Assert.Equal(2, count);
        Assert.True(world.IsAlive(root));
        Assert.False(world.IsAlive(child));
        Assert.False(world.IsAlive(grandchild));
        Assert.Empty(world.GetChildren(root));
    }

    #endregion

    #region Despawn Integration Tests

    [Fact]
    public void Despawn_ParentWithChildren_OrphansChildren()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);

        world.Despawn(parent);

        Assert.True(world.IsAlive(child1));
        Assert.True(world.IsAlive(child2));
        Assert.False(world.GetParent(child1).IsValid);
        Assert.False(world.GetParent(child2).IsValid);
    }

    [Fact]
    public void Despawn_ChildWithParent_RemovesFromParent()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        world.Despawn(child);

        Assert.True(world.IsAlive(parent));
        Assert.Empty(world.GetChildren(parent));
    }

    [Fact]
    public void Despawn_MiddleNodeInHierarchy_OrphansChildrenAndRemovesFromParent()
    {
        using var world = new World();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        world.Despawn(parent);

        Assert.True(world.IsAlive(grandparent));
        Assert.True(world.IsAlive(child));
        Assert.Empty(world.GetChildren(grandparent));
        Assert.False(world.GetParent(child).IsValid);
    }

    #endregion

    #region Edge Cases and Complex Scenarios

    [Fact]
    public void Hierarchy_AfterWorldDispose_CleanedUp()
    {
        var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        world.Dispose();

        // World should be able to be disposed without issues
        // No assertions needed - test passes if no exception thrown
    }

    [Fact]
    public void Hierarchy_MultipleRoots_IndependentHierarchies()
    {
        using var world = new World();
        var root1 = world.Spawn().Build();
        var root2 = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, root1);
        world.SetParent(child2, root2);

        Assert.Equal(root1, world.GetRoot(child1));
        Assert.Equal(root2, world.GetRoot(child2));
        Assert.NotEqual(world.GetRoot(child1), world.GetRoot(child2));
    }

    [Fact]
    public void Hierarchy_ReparentBetweenHierarchies_Works()
    {
        using var world = new World();
        var root1 = world.Spawn().Build();
        var root2 = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, root1);
        Assert.Equal(root1, world.GetRoot(child));

        world.SetParent(child, root2);
        Assert.Equal(root2, world.GetRoot(child));
        Assert.Empty(world.GetChildren(root1));
        Assert.Single(world.GetChildren(root2));
    }

    [Fact]
    public void GetDescendants_WithPartiallyDeadChildren_SkipsDeadEntities()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var child3 = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);
        world.SetParent(child3, root);

        // Despawn middle child
        world.Despawn(child2);

        var descendants = world.GetDescendants(root).ToList();

        Assert.Equal(2, descendants.Count);
        Assert.Contains(child1, descendants);
        Assert.Contains(child3, descendants);
        Assert.DoesNotContain(child2, descendants);
    }

    [Fact]
    public void LargeHierarchy_PerformanceReasonable()
    {
        using var world = new World();
        var root = world.Spawn().Build();

        // Create a hierarchy with 100 children, each with 10 grandchildren
        var children = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            var child = world.Spawn().Build();
            world.SetParent(child, root);
            children.Add(child);

            for (int j = 0; j < 10; j++)
            {
                var grandchild = world.Spawn().Build();
                world.SetParent(grandchild, child);
            }
        }

        // Verify hierarchy
        Assert.Equal(100, world.GetChildren(root).Count());
        Assert.Equal(1100, world.GetDescendants(root).Count());

        // Verify recursive despawn
        var count = world.DespawnRecursive(root);
        Assert.Equal(1101, count);
    }

    [Fact]
    public void CycleDetection_WithLongChain_PreventsCycle()
    {
        using var world = new World();
        var entities = new List<Entity>();

        // Create a long chain: e0 -> e1 -> e2 -> ... -> e9
        for (int i = 0; i < 10; i++)
        {
            entities.Add(world.Spawn().Build());
        }

        for (int i = 1; i < 10; i++)
        {
            world.SetParent(entities[i], entities[i - 1]);
        }

        // Try to create a cycle by making e0 a child of e9
        var ex = Assert.Throws<InvalidOperationException>(() => world.SetParent(entities[0], entities[9]));
        Assert.Contains("circular hierarchy", ex.Message);
    }

    [Fact]
    public void SetParent_ToEntityNull_WhenNoExistingParent_DoesNotThrow()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // Should not throw even though entity has no parent
        world.SetParent(entity, Entity.Null);

        Assert.False(world.GetParent(entity).IsValid);
    }

    [Fact]
    public void RemoveChild_LastChild_CleansUpParentChildrenSet()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        // Remove the only child - should clean up internal data structures
        world.RemoveChild(parent, child);

        // Verify parent has no children
        Assert.Empty(world.GetChildren(parent));

        // Add another child to verify internal state is clean
        var newChild = world.Spawn().Build();
        world.SetParent(newChild, parent);
        Assert.Single(world.GetChildren(parent));
    }

    [Fact]
    public void Despawn_LastChild_CleansUpParentChildrenSet()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();
        world.SetParent(child, parent);

        // Despawn the only child
        world.Despawn(child);

        // Verify parent has no children
        Assert.Empty(world.GetChildren(parent));

        // Add another child to verify internal state is clean
        var newChild = world.Spawn().Build();
        world.SetParent(newChild, parent);
        Assert.Single(world.GetChildren(parent));
    }

    [Fact]
    public void Despawn_EntityWithNoHierarchy_WorksCorrectly()
    {
        using var world = new World();
        var entity = world.Spawn().Build();

        // Entity has no parent and no children
        var result = world.Despawn(entity);

        Assert.True(result);
        Assert.False(world.IsAlive(entity));
    }

    [Fact]
    public void GetChildren_AfterChildDespawned_SkipsDeadChild()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);

        // Despawn one child (this cleans up hierarchy)
        world.Despawn(child1);

        var children = world.GetChildren(parent).ToList();
        Assert.Single(children);
        Assert.Equal(child2, children[0]);
    }

    [Fact]
    public void GetAncestors_WhenParentDespawned_StopsAtOrphanedPoint()
    {
        using var world = new World();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        // Despawn the parent - this orphans the child
        world.Despawn(parent);

        // Child should now have no ancestors
        var ancestors = world.GetAncestors(child).ToList();
        Assert.Empty(ancestors);
    }

    [Fact]
    public void GetRoot_WhenParentDespawned_ReturnsSelf()
    {
        using var world = new World();
        var grandparent = world.Spawn().Build();
        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        // Despawn the parent - this orphans the child
        world.Despawn(parent);

        // Child should now be its own root
        var root = world.GetRoot(child);
        Assert.Equal(child, root);
    }

    [Fact]
    public void GetDescendants_WhenGrandchildDespawned_SkipsIt()
    {
        using var world = new World();
        var root = world.Spawn().Build();
        var child = world.Spawn().Build();
        var grandchild1 = world.Spawn().Build();
        var grandchild2 = world.Spawn().Build();

        world.SetParent(child, root);
        world.SetParent(grandchild1, child);
        world.SetParent(grandchild2, child);

        // Despawn one grandchild
        world.Despawn(grandchild1);

        var descendants = world.GetDescendants(root).ToList();
        Assert.Equal(2, descendants.Count);
        Assert.Contains(child, descendants);
        Assert.Contains(grandchild2, descendants);
        Assert.DoesNotContain(grandchild1, descendants);
    }

    [Fact]
    public void SetParent_ChangingParent_CleansUpOldParentWhenLastChild()
    {
        using var world = new World();
        var parent1 = world.Spawn().Build();
        var parent2 = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, parent1);
        Assert.Single(world.GetChildren(parent1));

        // Move child to parent2 - parent1 should have empty children
        world.SetParent(child, parent2);

        Assert.Empty(world.GetChildren(parent1));
        Assert.Single(world.GetChildren(parent2));

        // Verify we can add new children to parent1 (internal state is clean)
        var newChild = world.Spawn().Build();
        world.SetParent(newChild, parent1);
        Assert.Single(world.GetChildren(parent1));
    }

    [Fact]
    public void DespawnRecursive_WithDeepHierarchy_AllPathsTraversed()
    {
        using var world = new World();
        var root = world.Spawn().Build();

        // Create a balanced tree: root has 2 children, each has 2 children
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var grandchild1a = world.Spawn().Build();
        var grandchild1b = world.Spawn().Build();
        var grandchild2a = world.Spawn().Build();
        var grandchild2b = world.Spawn().Build();

        world.SetParent(child1, root);
        world.SetParent(child2, root);
        world.SetParent(grandchild1a, child1);
        world.SetParent(grandchild1b, child1);
        world.SetParent(grandchild2a, child2);
        world.SetParent(grandchild2b, child2);

        var count = world.DespawnRecursive(root);

        Assert.Equal(7, count);
        Assert.False(world.IsAlive(root));
        Assert.False(world.IsAlive(child1));
        Assert.False(world.IsAlive(child2));
        Assert.False(world.IsAlive(grandchild1a));
        Assert.False(world.IsAlive(grandchild1b));
        Assert.False(world.IsAlive(grandchild2a));
        Assert.False(world.IsAlive(grandchild2b));
    }

    [Fact]
    public void CleanupEntityHierarchy_WithMultipleChildren_OrphansAll()
    {
        using var world = new World();
        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();
        var child3 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);
        world.SetParent(child3, parent);

        // Verify all children are set up
        Assert.Equal(3, world.GetChildren(parent).Count());

        // Despawn parent - should orphan all children
        world.Despawn(parent);

        Assert.True(world.IsAlive(child1));
        Assert.True(world.IsAlive(child2));
        Assert.True(world.IsAlive(child3));
        Assert.False(world.GetParent(child1).IsValid);
        Assert.False(world.GetParent(child2).IsValid);
        Assert.False(world.GetParent(child3).IsValid);
    }

    #endregion
}
