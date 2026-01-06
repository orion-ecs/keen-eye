using KeenEyes;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Nodes;

namespace KeenEyes.Graph.Tests.Nodes;

public class CommentNodeTests
{
    [Fact]
    public void TypeId_ReturnsBuiltInCommentId()
    {
        var node = new CommentNode();

        Assert.Equal(BuiltInNodeIds.Comment, node.TypeId);
    }

    [Fact]
    public void Name_ReturnsComment()
    {
        var node = new CommentNode();

        Assert.Equal("Comment", node.Name);
    }

    [Fact]
    public void Category_ReturnsUtility()
    {
        var node = new CommentNode();

        Assert.Equal("Utility", node.Category);
    }

    [Fact]
    public void InputPorts_IsEmpty()
    {
        var node = new CommentNode();

        Assert.Empty(node.InputPorts);
    }

    [Fact]
    public void OutputPorts_IsEmpty()
    {
        var node = new CommentNode();

        Assert.Empty(node.OutputPorts);
    }

    [Fact]
    public void IsCollapsible_ReturnsFalse()
    {
        var node = new CommentNode();

        Assert.False(node.IsCollapsible);
    }

    [Fact]
    public void Initialize_AddsCommentNodeDataComponent()
    {
        using var world = new World();
        world.Components.Register<CommentNodeData>();
        var nodeEntity = world.Spawn().Build();
        var definition = new CommentNode();

        definition.Initialize(nodeEntity, world);

        Assert.True(world.Has<CommentNodeData>(nodeEntity));
    }

    [Fact]
    public void Initialize_SetsDefaultCommentData()
    {
        using var world = new World();
        world.Components.Register<CommentNodeData>();
        var nodeEntity = world.Spawn().Build();
        var definition = new CommentNode();

        definition.Initialize(nodeEntity, world);

        var data = world.Get<CommentNodeData>(nodeEntity);
        Assert.Equal(CommentNodeData.Default.Text, data.Text);
        Assert.Equal(CommentNodeData.Default.FontScale, data.FontScale);
    }
}
