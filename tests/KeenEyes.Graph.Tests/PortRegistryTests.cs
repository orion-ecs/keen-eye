using System.Numerics;
using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Tests;

public class PortRegistryTests
{
    [Fact]
    public void RegisterNodeType_ReturnsUniqueId()
    {
        var registry = new PortRegistry();

        var id1 = registry.RegisterNodeType("Add", "Math", [], []);
        var id2 = registry.RegisterNodeType("Subtract", "Math", [], []);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void RegisterNodeType_WithExplicitId_UsesProvidedId()
    {
        var registry = new PortRegistry();

        registry.RegisterNodeType(100, "Custom", "Custom", [], []);

        Assert.True(registry.IsRegistered(100));
        Assert.False(registry.IsRegistered(1));
    }

    [Fact]
    public void RegisterNodeType_WithDuplicateId_ThrowsArgumentException()
    {
        var registry = new PortRegistry();

        registry.RegisterNodeType(1, "First", "General", [], []);

        Assert.Throws<ArgumentException>(() =>
            registry.RegisterNodeType(1, "Second", "General", [], []));
    }

    [Fact]
    public void GetNodeType_WithValidId_ReturnsNodeTypeInfo()
    {
        var registry = new PortRegistry();
        var inputs = new[] { PortDefinition.Input("A", PortTypeId.Float, 0) };
        var outputs = new[] { PortDefinition.Output("Result", PortTypeId.Float, 0) };

        var id = registry.RegisterNodeType("Add", "Math", inputs, outputs);
        var info = registry.GetNodeType(id);

        Assert.Equal("Add", info.Name);
        Assert.Equal("Math", info.Category);
        Assert.Single(info.InputPorts);
        Assert.Single(info.OutputPorts);
    }

    [Fact]
    public void GetNodeType_WithInvalidId_ThrowsKeyNotFoundException()
    {
        var registry = new PortRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.GetNodeType(999));
    }

    [Fact]
    public void TryGetNodeType_WithValidId_ReturnsTrueAndInfo()
    {
        var registry = new PortRegistry();
        var id = registry.RegisterNodeType("Test", "General", [], []);

        var result = registry.TryGetNodeType(id, out var info);

        Assert.True(result);
        Assert.Equal("Test", info.Name);
    }

    [Fact]
    public void TryGetNodeType_WithInvalidId_ReturnsFalse()
    {
        var registry = new PortRegistry();

        var result = registry.TryGetNodeType(999, out _);

        Assert.False(result);
    }

    [Fact]
    public void GetAllNodeTypes_ReturnsAllRegisteredTypes()
    {
        var registry = new PortRegistry();
        registry.RegisterNodeType("A", "Cat1", [], []);
        registry.RegisterNodeType("B", "Cat2", [], []);
        registry.RegisterNodeType("C", "Cat1", [], []);

        var all = registry.GetAllNodeTypes().ToList();

        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void GetNodeTypesByCategory_ReturnsOnlyMatchingTypes()
    {
        var registry = new PortRegistry();
        registry.RegisterNodeType("A", "Math", [], []);
        registry.RegisterNodeType("B", "Logic", [], []);
        registry.RegisterNodeType("C", "Math", [], []);

        var mathTypes = registry.GetNodeTypesByCategory("Math").ToList();

        Assert.Equal(2, mathTypes.Count);
        Assert.All(mathTypes, t => Assert.Equal("Math", t.Category));
    }

    [Fact]
    public void GetCategories_ReturnsUniqueCategories()
    {
        var registry = new PortRegistry();
        registry.RegisterNodeType("A", "Math", [], []);
        registry.RegisterNodeType("B", "Logic", [], []);
        registry.RegisterNodeType("C", "Math", [], []);

        var categories = registry.GetCategories().ToList();

        Assert.Equal(2, categories.Count);
        Assert.Contains("Math", categories);
        Assert.Contains("Logic", categories);
    }

    [Fact]
    public void RegisterGenericNode_CreatesPlaceholderNode()
    {
        var registry = new PortRegistry();

        var id = registry.RegisterGenericNode();

        Assert.True(registry.IsRegistered(id));
        var info = registry.GetNodeType(id);
        Assert.Equal("Node", info.Name);
        Assert.Equal(2, info.InputPorts.Length);
        Assert.Single(info.OutputPorts);
    }

    [Fact]
    public void Count_ReflectsNumberOfRegisteredTypes()
    {
        var registry = new PortRegistry();

        Assert.Equal(0, registry.Count);

        registry.RegisterNodeType("A", "General", [], []);
        Assert.Equal(1, registry.Count);

        registry.RegisterNodeType("B", "General", [], []);
        Assert.Equal(2, registry.Count);
    }
}
