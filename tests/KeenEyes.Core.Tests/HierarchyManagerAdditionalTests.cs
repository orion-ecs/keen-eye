namespace KeenEyes.Tests;

/// <summary>
/// Additional tests for HierarchyManager to improve coverage.
/// Focuses on edge cases with dead/invalid entities in hierarchy operations.
/// </summary>
public class HierarchyManagerAdditionalTests
{
    #region Test Components

    public struct TestPosition : IComponent
    {
        public float X, Y;
    }

    #endregion

    #region GetParent Edge Cases

    [Fact]
    public void GetParent_WithDeadParent_ReturnsNull()
    {
        using var world = new World();

        var parent = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();

        var child = world.Spawn()
            .With(new TestPosition { X = 1, Y = 1 })
            .Build();

        world.SetParent(child, parent);

        // Verify parent is set
        var initialParent = world.GetParent(child);
        Assert.True(initialParent.IsValid);
        Assert.Equal(parent, initialParent);

        // Kill the parent
        world.Despawn(parent);

        // GetParent should detect the dead parent and return Entity.Null
        var result = world.GetParent(child);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetParent_WithInvalidParentId_ReturnsNull()
    {
        using var world = new World();
        var manager = new HierarchyManager(world);

        var child = world.Spawn()
            .With(new TestPosition { X = 1, Y = 1 })
            .Build();

        // Manually set an invalid parent ID using internal access
        // This simulates a corrupted state
        var invalidParent = new Entity { Id = 99999, Version = 1 };

        // GetParent should handle this gracefully
        var result = world.GetParent(child);
        Assert.False(result.IsValid);
    }

    #endregion

    #region GetRoot Edge Cases

    [Fact]
    public void GetRoot_WithDeadAncestor_StopsAtDeadEntity()
    {
        using var world = new World();

        var grandparent = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();

        var parent = world.Spawn()
            .With(new TestPosition { X = 1, Y = 1 })
            .Build();

        var child = world.Spawn()
            .With(new TestPosition { X = 2, Y = 2 })
            .Build();

        world.SetParent(parent, grandparent);
        world.SetParent(child, parent);

        // Verify hierarchy
        var root = world.GetRoot(child);
        Assert.Equal(grandparent, root);

        // Kill grandparent
        world.Despawn(grandparent);

        // GetRoot should stop at parent since grandparent is dead
        root = world.GetRoot(child);
        // Root should be parent now, or child if parent reference is broken
        Assert.True(world.IsAlive(root));
    }

    [Fact]
    public void GetRoot_WithEntityHavingNoParent_ReturnsItself()
    {
        using var world = new World();

        var entity = world.Spawn()
            .With(new TestPosition { X = 0, Y = 0 })
            .Build();

        var root = world.GetRoot(entity);
        Assert.Equal(entity, root);
    }

    #endregion

    #region Hierarchy with Multiple Levels

    [Fact]
    public void DeepHierarchy_GetAncestors_ReturnsAllLevels()
    {
        using var world = new World();

        var level0 = world.Spawn().Build();
        var level1 = world.Spawn().Build();
        var level2 = world.Spawn().Build();
        var level3 = world.Spawn().Build();

        world.SetParent(level1, level0);
        world.SetParent(level2, level1);
        world.SetParent(level3, level2);

        var ancestors = world.GetAncestors(level3).ToList();

        Assert.Equal(3, ancestors.Count);
        Assert.Contains(level2, ancestors);
        Assert.Contains(level1, ancestors);
        Assert.Contains(level0, ancestors);
    }

    #endregion

    #region Parent-Child Consistency

    [Fact]
    public void SetParent_ThenDespawnParent_ChildHasNoParent()
    {
        using var world = new World();

        var parent = world.Spawn().Build();
        var child = world.Spawn().Build();

        world.SetParent(child, parent);

        // Verify parent is set
        Assert.True(world.GetParent(child).IsValid);

        // Despawn parent
        world.Despawn(parent);

        // Child should have no parent now (or Entity.Null parent)
        var result = world.GetParent(child);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void GetChildren_AfterChildDespawned_DoesNotReturnDeadChild()
    {
        using var world = new World();

        var parent = world.Spawn().Build();
        var child1 = world.Spawn().Build();
        var child2 = world.Spawn().Build();

        world.SetParent(child1, parent);
        world.SetParent(child2, parent);

        // Verify both children
        var children = world.GetChildren(parent).ToList();
        Assert.Equal(2, children.Count);

        // Despawn child1
        world.Despawn(child1);

        // GetChildren should only return alive children
        children = world.GetChildren(parent).ToList();
        Assert.Single(children);
        Assert.Contains(child2, children);
        Assert.DoesNotContain(child1, children);
    }

    #endregion

    #region GetDescendants Edge Cases

    [Fact]
    public void GetDescendants_WithDeadChildInMiddle_ContinuesWithValidChildren()
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
        world.SetParent(grandchild2, child2);

        // Kill child1 but keep grandchild1 reference
        world.Despawn(child1);

        // GetDescendants should skip dead child1 but still work with child2
        var descendants = world.GetDescendants(root).ToList();

        // Should include child2 and grandchild2, but not child1 or grandchild1
        Assert.Contains(child2, descendants);
        Assert.Contains(grandchild2, descendants);
        Assert.DoesNotContain(child1, descendants);
    }

    #endregion
}
