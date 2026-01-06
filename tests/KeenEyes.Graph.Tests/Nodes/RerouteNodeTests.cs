using KeenEyes;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Nodes;

namespace KeenEyes.Graph.Tests.Nodes;

public class RerouteNodeTests
{
    [Fact]
    public void TypeId_ReturnsBuiltInRerouteId()
    {
        var node = new RerouteNode();

        Assert.Equal(BuiltInNodeIds.Reroute, node.TypeId);
    }

    [Fact]
    public void Name_ReturnsReroute()
    {
        var node = new RerouteNode();

        Assert.Equal("Reroute", node.Name);
    }

    [Fact]
    public void Category_ReturnsUtility()
    {
        var node = new RerouteNode();

        Assert.Equal("Utility", node.Category);
    }

    [Fact]
    public void InputPorts_HasOneAnyPort()
    {
        var node = new RerouteNode();

        Assert.Single(node.InputPorts);
        var port = node.InputPorts[0];
        Assert.Equal("In", port.Name);
        Assert.Equal(PortTypeId.Any, port.TypeId);
        Assert.Equal(PortDirection.Input, port.Direction);
    }

    [Fact]
    public void OutputPorts_HasOneAnyPort()
    {
        var node = new RerouteNode();

        Assert.Single(node.OutputPorts);
        var port = node.OutputPorts[0];
        Assert.Equal("Out", port.Name);
        Assert.Equal(PortTypeId.Any, port.TypeId);
        Assert.Equal(PortDirection.Output, port.Direction);
    }

    [Fact]
    public void IsCollapsible_ReturnsFalse()
    {
        var node = new RerouteNode();

        Assert.False(node.IsCollapsible);
    }

    [Fact]
    public void Initialize_SetsMinimalWidth()
    {
        using var world = new World();
        world.Components.Register<GraphNode>();
        var nodeEntity = world.Spawn()
            .With(new GraphNode { Width = 100f })
            .Build();
        var definition = new RerouteNode();

        definition.Initialize(nodeEntity, world);

        var nodeData = world.Get<GraphNode>(nodeEntity);
        Assert.Equal(60f, nodeData.Width);
    }
}
