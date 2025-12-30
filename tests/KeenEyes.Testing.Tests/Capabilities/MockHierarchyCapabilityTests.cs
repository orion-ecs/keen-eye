using KeenEyes.Testing.Capabilities;

namespace KeenEyes.Testing.Tests.Capabilities;

public class MockHierarchyCapabilityTests
{
    #region SetParent and GetParent

    [Fact]
    public void SetParent_EstablishesParentRelationship()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);

        capability.SetParent(child, parent);

        Assert.Equal(parent, capability.GetParent(child));
    }

    [Fact]
    public void SetParent_LogsOperation()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);

        capability.SetParent(child, parent);

        Assert.Single(capability.OperationLog);
        Assert.Equal("SetParent", capability.OperationLog[0].Operation);
        Assert.Equal(child, capability.OperationLog[0].Entity);
        Assert.Equal(parent, capability.OperationLog[0].Related);
    }

    [Fact]
    public void SetParent_WithNullParent_RemovesParentRelationship()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);
        capability.SetParent(child, parent);

        capability.SetParent(child, Entity.Null);

        Assert.Equal(Entity.Null, capability.GetParent(child));
    }

    [Fact]
    public void SetParent_WithThrowOnInvalidOperation_ThrowsOnCircularReference()
    {
        var capability = new MockHierarchyCapability { ThrowOnInvalidOperation = true };
        var entity1 = new Entity(1, 0);
        var entity2 = new Entity(2, 0);
        capability.SetParent(entity2, entity1);

        Assert.Throws<InvalidOperationException>(() => capability.SetParent(entity1, entity2));
    }

    [Fact]
    public void GetParent_WithNoParent_ReturnsNullEntity()
    {
        var capability = new MockHierarchyCapability();
        var entity = new Entity(1, 0);

        var parent = capability.GetParent(entity);

        Assert.Equal(Entity.Null, parent);
    }

    #endregion

    #region GetChildren and AddChild

    [Fact]
    public void GetChildren_ReturnsChildren()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child1 = new Entity(2, 0);
        var child2 = new Entity(3, 0);
        capability.SetParent(child1, parent);
        capability.SetParent(child2, parent);

        var children = capability.GetChildren(parent).ToList();

        Assert.Equal(2, children.Count);
        Assert.Contains(child1, children);
        Assert.Contains(child2, children);
    }

    [Fact]
    public void GetChildren_WithNoChildren_ReturnsEmptyEnumerable()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);

        var children = capability.GetChildren(parent);

        Assert.Empty(children);
    }

    [Fact]
    public void AddChild_EstablishesRelationshipAndLogs()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);

        capability.AddChild(parent, child);

        Assert.Equal(parent, capability.GetParent(child));
        Assert.Contains(child, capability.GetChildren(parent));
        Assert.Equal("AddChild", capability.OperationLog[0].Operation);
    }

    #endregion

    #region RemoveChild

    [Fact]
    public void RemoveChild_RemovesRelationship()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);
        capability.SetParent(child, parent);

        var result = capability.RemoveChild(parent, child);

        Assert.True(result);
        Assert.Equal(Entity.Null, capability.GetParent(child));
        Assert.DoesNotContain(child, capability.GetChildren(parent));
    }

    [Fact]
    public void RemoveChild_WithWrongParent_ReturnsFalse()
    {
        var capability = new MockHierarchyCapability();
        var actualParent = new Entity(1, 0);
        var wrongParent = new Entity(2, 0);
        var child = new Entity(3, 0);
        capability.SetParent(child, actualParent);

        var result = capability.RemoveChild(wrongParent, child);

        Assert.False(result);
        Assert.Equal(actualParent, capability.GetParent(child));
    }

    [Fact]
    public void RemoveChild_WithNoRelationship_ReturnsFalse()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);

        var result = capability.RemoveChild(parent, child);

        Assert.False(result);
    }

    #endregion

    #region GetDescendants

    [Fact]
    public void GetDescendants_ReturnsAllDescendants()
    {
        var capability = new MockHierarchyCapability();
        var root = new Entity(1, 0);
        var child1 = new Entity(2, 0);
        var child2 = new Entity(3, 0);
        var grandchild = new Entity(4, 0);
        capability.SetParent(child1, root);
        capability.SetParent(child2, root);
        capability.SetParent(grandchild, child1);

        var descendants = capability.GetDescendants(root).ToList();

        Assert.Equal(3, descendants.Count);
        Assert.Contains(child1, descendants);
        Assert.Contains(child2, descendants);
        Assert.Contains(grandchild, descendants);
    }

    [Fact]
    public void GetDescendants_WithNoDescendants_ReturnsEmptyEnumerable()
    {
        var capability = new MockHierarchyCapability();
        var entity = new Entity(1, 0);

        var descendants = capability.GetDescendants(entity);

        Assert.Empty(descendants);
    }

    #endregion

    #region GetAncestors

    [Fact]
    public void GetAncestors_ReturnsAllAncestors()
    {
        var capability = new MockHierarchyCapability();
        var root = new Entity(1, 0);
        var parent = new Entity(2, 0);
        var child = new Entity(3, 0);
        capability.SetParent(parent, root);
        capability.SetParent(child, parent);

        var ancestors = capability.GetAncestors(child).ToList();

        Assert.Equal(2, ancestors.Count);
        Assert.Equal(parent, ancestors[0]);
        Assert.Equal(root, ancestors[1]);
    }

    [Fact]
    public void GetAncestors_WithNoAncestors_ReturnsEmptyEnumerable()
    {
        var capability = new MockHierarchyCapability();
        var root = new Entity(1, 0);

        var ancestors = capability.GetAncestors(root);

        Assert.Empty(ancestors);
    }

    #endregion

    #region GetRoot

    [Fact]
    public void GetRoot_ReturnsRootOfHierarchy()
    {
        var capability = new MockHierarchyCapability();
        var root = new Entity(1, 0);
        var parent = new Entity(2, 0);
        var child = new Entity(3, 0);
        capability.SetParent(parent, root);
        capability.SetParent(child, parent);

        var result = capability.GetRoot(child);

        Assert.Equal(root, result);
    }

    [Fact]
    public void GetRoot_WithNoParent_ReturnsSelf()
    {
        var capability = new MockHierarchyCapability();
        var entity = new Entity(1, 0);

        var root = capability.GetRoot(entity);

        Assert.Equal(entity, root);
    }

    #endregion

    #region DespawnRecursive

    [Fact]
    public void DespawnRecursive_RemovesEntityAndDescendants()
    {
        var capability = new MockHierarchyCapability();
        var root = new Entity(1, 0);
        var child1 = new Entity(2, 0);
        var child2 = new Entity(3, 0);
        var grandchild = new Entity(4, 0);
        capability.SetParent(child1, root);
        capability.SetParent(child2, root);
        capability.SetParent(grandchild, child1);

        var count = capability.DespawnRecursive(root);

        Assert.Equal(4, count); // root + 2 children + 1 grandchild
        Assert.Empty(capability.GetChildren(root));
        Assert.Equal(Entity.Null, capability.GetParent(child1));
    }

    [Fact]
    public void DespawnRecursive_LogsOperation()
    {
        var capability = new MockHierarchyCapability();
        var entity = new Entity(1, 0);

        capability.DespawnRecursive(entity);

        Assert.Single(capability.OperationLog);
        Assert.Equal("DespawnRecursive", capability.OperationLog[0].Operation);
    }

    #endregion

    #region Clear and SetupHierarchy

    [Fact]
    public void Clear_RemovesAllData()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child = new Entity(2, 0);
        capability.SetParent(child, parent);

        capability.Clear();

        Assert.Equal(Entity.Null, capability.GetParent(child));
        Assert.Empty(capability.OperationLog);
    }

    [Fact]
    public void SetupHierarchy_CreatesRelationshipsWithoutLogging()
    {
        var capability = new MockHierarchyCapability();
        var parent = new Entity(1, 0);
        var child1 = new Entity(2, 0);
        var child2 = new Entity(3, 0);

        capability.SetupHierarchy(parent, child1, child2);

        Assert.Equal(parent, capability.GetParent(child1));
        Assert.Equal(parent, capability.GetParent(child2));
        Assert.Equal(2, capability.GetChildren(parent).Count());
        Assert.Empty(capability.OperationLog); // SetupHierarchy doesn't log
    }

    #endregion
}
