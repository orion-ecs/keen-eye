using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Nodes;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Tests;

public class NodeTypeRegistryTests
{
    #region Registration Tests

    [Fact]
    public void Register_WithDefinition_StoresDefinition()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        var definition = new TestNodeDefinition(101, "TestNode", "Test");

        registry.Register(definition);

        var retrieved = registry.GetDefinition(101);
        Assert.NotNull(retrieved);
        Assert.Same(definition, retrieved);
    }

    [Fact]
    public void Register_GenericType_CreatesDefinition()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);

        registry.Register<CommentNode>();

        var definition = registry.GetDefinition(BuiltInNodeIds.Comment);
        Assert.NotNull(definition);
        Assert.IsType<CommentNode>(definition);
    }

    [Fact]
    public void Register_DuplicateTypeId_ThrowsArgumentException()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        var definition1 = new TestNodeDefinition(101, "First", "Test");
        var definition2 = new TestNodeDefinition(101, "Second", "Test");

        registry.Register(definition1);

        Assert.Throws<ArgumentException>(() => registry.Register(definition2));
    }

    #endregion

    #region Query Tests

    [Fact]
    public void GetDefinition_InvalidId_ReturnsNull()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);

        var definition = registry.GetDefinition(999);

        Assert.Null(definition);
    }

    [Fact]
    public void GetByCategory_ReturnsMatchingDefinitions()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        registry.Register(new TestNodeDefinition(101, "A", "Math"));
        registry.Register(new TestNodeDefinition(102, "B", "Logic"));
        registry.Register(new TestNodeDefinition(103, "C", "Math"));

        var mathNodes = registry.GetByCategory("Math").ToList();

        Assert.Equal(2, mathNodes.Count);
        Assert.All(mathNodes, d => Assert.Equal("Math", d.Category));
    }

    [Fact]
    public void GetByCategory_NonExistentCategory_ReturnsEmpty()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        registry.Register(new TestNodeDefinition(101, "A", "Math"));

        var results = registry.GetByCategory("NonExistent").ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void GetCategories_ReturnsUniqueCategories()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        registry.Register(new TestNodeDefinition(101, "A", "Math"));
        registry.Register(new TestNodeDefinition(102, "B", "Logic"));
        registry.Register(new TestNodeDefinition(103, "C", "Math"));

        var categories = registry.GetCategories().ToList();

        Assert.Equal(2, categories.Count);
        Assert.Contains("Math", categories);
        Assert.Contains("Logic", categories);
    }

    [Fact]
    public void GetCategories_SortedAlphabetically()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        registry.Register(new TestNodeDefinition(101, "A", "Zebra"));
        registry.Register(new TestNodeDefinition(102, "B", "Alpha"));
        registry.Register(new TestNodeDefinition(103, "C", "Middle"));

        var categories = registry.GetCategories().ToList();

        Assert.Equal("Alpha", categories[0]);
        Assert.Equal("Middle", categories[1]);
        Assert.Equal("Zebra", categories[2]);
    }

    [Fact]
    public void GetAll_ReturnsAllDefinitions()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        registry.Register(new TestNodeDefinition(101, "A", "Cat1"));
        registry.Register(new TestNodeDefinition(102, "B", "Cat2"));
        registry.Register(new TestNodeDefinition(103, "C", "Cat3"));

        var all = registry.GetAll().ToList();

        Assert.Equal(3, all.Count);
    }

    #endregion

    #region Port Registry Integration

    [Fact]
    public void Register_AlsoRegistersWithPortRegistry()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        var definition = new TestNodeDefinition(101, "TestNode", "Test");

        registry.Register(definition);

        Assert.True(portRegistry.IsRegistered(101));
        var nodeType = portRegistry.GetNodeType(101);
        Assert.Equal("TestNode", nodeType.Name);
        Assert.Equal("Test", nodeType.Category);
    }

    [Fact]
    public void Register_WithPorts_RegistersPortsCorrectly()
    {
        var portRegistry = new PortRegistry();
        var registry = new NodeTypeRegistry(portRegistry);
        var inputs = new[] { PortDefinition.Input("In", PortTypeId.Float, 0) };
        var outputs = new[] { PortDefinition.Output("Out", PortTypeId.Float, 0) };
        var definition = new TestNodeDefinition(101, "Test", "Test", inputs, outputs);

        registry.Register(definition);

        var nodeType = portRegistry.GetNodeType(101);
        Assert.Single(nodeType.InputPorts);
        Assert.Single(nodeType.OutputPorts);
        Assert.Equal("In", nodeType.InputPorts[0].Name);
        Assert.Equal("Out", nodeType.OutputPorts[0].Name);
    }

    #endregion

    #region Test Helper

    private sealed class TestNodeDefinition(
        int typeId,
        string name,
        string category,
        IReadOnlyList<PortDefinition>? inputs = null,
        IReadOnlyList<PortDefinition>? outputs = null) : INodeTypeDefinition
    {
        public int TypeId { get; } = typeId;
        public string Name { get; } = name;
        public string Category { get; } = category;
        public IReadOnlyList<PortDefinition> InputPorts { get; } = inputs ?? [];
        public IReadOnlyList<PortDefinition> OutputPorts { get; } = outputs ?? [];
        public bool IsCollapsible => false;

        public void Initialize(Entity node, IWorld world) { }
        public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
    }

    #endregion
}
