using System.Numerics;
using KeenEyes.Graph;
using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Tests;

public class GraphContextTests
{
    private World CreateWorldWithGraphPlugin()
    {
        var world = new World();
        world.InstallPlugin(new GraphPlugin());
        return world;
    }

    [Fact]
    public void CreateCanvas_ReturnsValidEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();

        var canvas = context.CreateCanvas();

        Assert.True(canvas.IsValid);
        Assert.True(world.Has<GraphCanvas>(canvas));
        Assert.True(world.Has<GraphCanvasTag>(canvas));
    }

    [Fact]
    public void CreateCanvas_WithName_SetsEntityName()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();

        var canvas = context.CreateCanvas("TestCanvas");

        Assert.True(canvas.IsValid);
        Assert.Equal("TestCanvas", world.GetName(canvas));
    }

    [Fact]
    public void CreateNode_ReturnsValidEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var position = new Vector2(100, 100);

        var node = context.CreateNode(canvas, nodeTypeId, position);

        Assert.True(node.IsValid);
        Assert.True(world.Has<GraphNode>(node));
        ref readonly var nodeData = ref world.Get<GraphNode>(node);
        Assert.Equal(position, nodeData.Position);
        Assert.Equal(canvas, nodeData.Canvas);
    }

    [Fact]
    public void CreateNode_SetsParentToCanvas()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();

        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        Assert.Equal(canvas, world.GetParent(node));
    }

    [Fact]
    public void CreateNode_WithInvalidCanvas_ThrowsArgumentException()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var nodeTypeId = registry.RegisterGenericNode();

        Assert.Throws<ArgumentException>(() =>
            context.CreateNode(Entity.Null, nodeTypeId, Vector2.Zero));
    }

    [Fact]
    public void CreateNode_WithUnregisteredType_ThrowsArgumentException()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();

        var canvas = context.CreateCanvas();

        Assert.Throws<ArgumentException>(() =>
            context.CreateNode(canvas, 999, Vector2.Zero));
    }

    [Fact]
    public void Connect_WithValidNodes_ReturnsConnectionEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));

        var connection = context.Connect(node1, 0, node2, 0);

        Assert.True(connection.IsValid);
        Assert.True(world.Has<GraphConnection>(connection));
    }

    [Fact]
    public void Connect_WithInvalidSourceNode_ReturnsNullEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        var connection = context.Connect(Entity.Null, 0, node, 0);

        Assert.False(connection.IsValid);
    }

    [Fact]
    public void Connect_WithInvalidTargetNode_ReturnsNullEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        var connection = context.Connect(node, 0, Entity.Null, 0);

        Assert.False(connection.IsValid);
    }

    [Fact]
    public void DeleteNode_RemovesNodeEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        context.DeleteNode(node);

        Assert.False(world.IsAlive(node));
    }

    [Fact]
    public void DeleteNode_RemovesRelatedConnections()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));
        var connection = context.Connect(node1, 0, node2, 0);

        context.DeleteNode(node1);

        Assert.False(world.IsAlive(connection));
    }

    [Fact]
    public void DeleteConnection_RemovesConnectionEntity()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));
        var connection = context.Connect(node1, 0, node2, 0);

        context.DeleteConnection(connection);

        Assert.False(world.IsAlive(connection));
        Assert.True(world.IsAlive(node1)); // Nodes should remain
        Assert.True(world.IsAlive(node2));
    }

    [Fact]
    public void SelectNode_AddsSelectedTag()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        context.SelectNode(node);

        Assert.True(world.Has<GraphNodeSelectedTag>(node));
    }

    [Fact]
    public void SelectNode_WithoutAddToSelection_ClearsPreviousSelection()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));

        context.SelectNode(node1);
        context.SelectNode(node2, addToSelection: false);

        Assert.False(world.Has<GraphNodeSelectedTag>(node1));
        Assert.True(world.Has<GraphNodeSelectedTag>(node2));
    }

    [Fact]
    public void SelectNode_WithAddToSelection_KeepsPreviousSelection()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));

        context.SelectNode(node1);
        context.SelectNode(node2, addToSelection: true);

        Assert.True(world.Has<GraphNodeSelectedTag>(node1));
        Assert.True(world.Has<GraphNodeSelectedTag>(node2));
    }

    [Fact]
    public void DeselectNode_RemovesSelectedTag()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node = context.CreateNode(canvas, nodeTypeId, Vector2.Zero);

        context.SelectNode(node);
        context.DeselectNode(node);

        Assert.False(world.Has<GraphNodeSelectedTag>(node));
    }

    [Fact]
    public void ClearSelection_RemovesAllSelectedTags()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));

        context.SelectNode(node1);
        context.SelectNode(node2, addToSelection: true);
        context.ClearSelection();

        Assert.False(world.Has<GraphNodeSelectedTag>(node1));
        Assert.False(world.Has<GraphNodeSelectedTag>(node2));
    }

    [Fact]
    public void GetSelectedNodes_ReturnsAllSelectedNodes()
    {
        using var world = CreateWorldWithGraphPlugin();
        var context = world.GetExtension<GraphContext>();
        var registry = world.GetExtension<PortRegistry>();

        var canvas = context.CreateCanvas();
        var nodeTypeId = registry.RegisterGenericNode();
        var node1 = context.CreateNode(canvas, nodeTypeId, new Vector2(0, 0));
        var node2 = context.CreateNode(canvas, nodeTypeId, new Vector2(200, 0));
        var node3 = context.CreateNode(canvas, nodeTypeId, new Vector2(400, 0));

        context.SelectNode(node1);
        context.SelectNode(node2, addToSelection: true);

        var selected = context.GetSelectedNodes().ToList();

        Assert.Equal(2, selected.Count);
        Assert.Contains(node1, selected);
        Assert.Contains(node2, selected);
        Assert.DoesNotContain(node3, selected);
    }
}
