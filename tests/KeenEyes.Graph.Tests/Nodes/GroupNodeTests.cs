using KeenEyes;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Nodes;

namespace KeenEyes.Graph.Tests.Nodes;

public class GroupNodeTests
{
    [Fact]
    public void TypeId_ReturnsBuiltInGroupId()
    {
        var node = new GroupNode();

        Assert.Equal(BuiltInNodeIds.Group, node.TypeId);
    }

    [Fact]
    public void Name_ReturnsGroup()
    {
        var node = new GroupNode();

        Assert.Equal("Group", node.Name);
    }

    [Fact]
    public void Category_ReturnsUtility()
    {
        var node = new GroupNode();

        Assert.Equal("Utility", node.Category);
    }

    [Fact]
    public void InputPorts_IsEmpty()
    {
        var node = new GroupNode();

        // Group nodes have dynamic interface ports, not static ones
        Assert.Empty(node.InputPorts);
    }

    [Fact]
    public void OutputPorts_IsEmpty()
    {
        var node = new GroupNode();

        // Group nodes have dynamic interface ports, not static ones
        Assert.Empty(node.OutputPorts);
    }

    [Fact]
    public void IsCollapsible_ReturnsTrue()
    {
        var node = new GroupNode();

        Assert.True(node.IsCollapsible);
    }

    [Fact]
    public void Initialize_AddsGroupNodeDataComponent()
    {
        using var world = new World();
        world.Components.Register<GraphCanvas>();
        world.Components.Register<GraphCanvasTag>();
        world.Components.Register<GraphNode>();
        world.Components.Register<GroupNodeData>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new GroupNode();

        definition.Initialize(nodeEntity, world);

        Assert.True(world.Has<GroupNodeData>(nodeEntity));
    }

    [Fact]
    public void Initialize_CreatesInternalCanvas()
    {
        using var world = new World();
        world.Components.Register<GraphCanvas>();
        world.Components.Register<GraphCanvasTag>();
        world.Components.Register<GraphNode>();
        world.Components.Register<GroupNodeData>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new GroupNode();

        definition.Initialize(nodeEntity, world);

        var groupData = world.Get<GroupNodeData>(nodeEntity);
        Assert.True(groupData.InternalCanvas.IsValid);
        Assert.True(world.Has<GraphCanvas>(groupData.InternalCanvas));
        Assert.True(world.Has<GraphCanvasTag>(groupData.InternalCanvas));
    }

    [Fact]
    public void Initialize_SetsInternalCanvasAsChildOfNode()
    {
        using var world = new World();
        world.Components.Register<GraphCanvas>();
        world.Components.Register<GraphCanvasTag>();
        world.Components.Register<GraphNode>();
        world.Components.Register<GroupNodeData>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new GroupNode();

        definition.Initialize(nodeEntity, world);

        var groupData = world.Get<GroupNodeData>(nodeEntity);
        var parent = world.GetParent(groupData.InternalCanvas);
        Assert.Equal(nodeEntity, parent);
    }

    [Fact]
    public void Initialize_SetsLargerDefaultWidth()
    {
        using var world = new World();
        world.Components.Register<GraphCanvas>();
        world.Components.Register<GraphCanvasTag>();
        world.Components.Register<GraphNode>();
        world.Components.Register<GroupNodeData>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new GroupNode();

        definition.Initialize(nodeEntity, world);

        var nodeData = world.Get<GraphNode>(nodeEntity);
        Assert.Equal(250f, nodeData.Width);
    }

    [Fact]
    public void Initialize_SetsEmptyInterfacePorts()
    {
        using var world = new World();
        world.Components.Register<GraphCanvas>();
        world.Components.Register<GraphCanvasTag>();
        world.Components.Register<GraphNode>();
        world.Components.Register<GroupNodeData>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new GroupNode();

        definition.Initialize(nodeEntity, world);

        var groupData = world.Get<GroupNodeData>(nodeEntity);
        Assert.Empty(groupData.InterfaceInputs);
        Assert.Empty(groupData.InterfaceOutputs);
        Assert.False(groupData.IsEditing);
    }
}
