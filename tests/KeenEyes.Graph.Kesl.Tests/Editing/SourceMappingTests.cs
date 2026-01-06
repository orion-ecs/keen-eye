using KeenEyes.Graph.Kesl.Editing;
using KeenEyes.Shaders.Compiler.Lexing;

namespace KeenEyes.Graph.Kesl.Tests.Editing;

public class SourceMappingTests
{
    #region AddMapping Tests

    [Fact]
    public void AddMapping_StoresMapping()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));

        mapping.AddMapping(node, span);

        var retrieved = mapping.GetSourceSpan(node);
        Assert.NotNull(retrieved);
        Assert.Equal(span, retrieved.Value);
    }

    [Fact]
    public void AddMapping_OverwritesPrevious()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var span1 = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));
        var span2 = new SourceSpan(
            new SourceLocation("test.kesl", 5, 0),
            new SourceLocation("test.kesl", 5, 20));

        mapping.AddMapping(node, span1);
        mapping.AddMapping(node, span2);

        var retrieved = mapping.GetSourceSpan(node);
        Assert.NotNull(retrieved);
        Assert.Equal(span2, retrieved!.Value);
    }

    #endregion

    #region GetSourceSpan Tests

    [Fact]
    public void GetSourceSpan_UnmappedNode_ReturnsNull()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();

        var result = mapping.GetSourceSpan(node);

        Assert.Null(result);
    }

    #endregion

    #region GetNodeAtLocation Tests

    [Fact]
    public void GetNodeAtLocation_ExactMatch_ReturnsNode()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var start = new SourceLocation("test.kesl", 1, 0);
        var span = new SourceSpan(start, new SourceLocation("test.kesl", 1, 10));

        mapping.AddMapping(node, span);

        var result = mapping.GetNodeAtLocation(start);
        Assert.NotNull(result);
        Assert.Equal(node, result.Value);
    }

    [Fact]
    public void GetNodeAtLocation_WithinSpan_ReturnsNode()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));

        mapping.AddMapping(node, span);

        var locationWithinSpan = new SourceLocation("test.kesl", 1, 5);
        var result = mapping.GetNodeAtLocation(locationWithinSpan);
        Assert.NotNull(result);
        Assert.Equal(node, result.Value);
    }

    [Fact]
    public void GetNodeAtLocation_OutsideSpan_ReturnsNull()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));

        mapping.AddMapping(node, span);

        var locationOutside = new SourceLocation("test.kesl", 2, 0);
        var result = mapping.GetNodeAtLocation(locationOutside);
        Assert.Null(result);
    }

    #endregion

    #region Variable Mapping Tests

    [Fact]
    public void AddVariableMapping_StoresMapping()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateSetVariable("myVar");
        var mapping = new SourceMapping();

        mapping.AddVariableMapping("myVar", node);

        var result = mapping.GetNodeForVariable("myVar");
        Assert.NotNull(result);
        Assert.Equal(node, result.Value);
    }

    [Fact]
    public void GetNodeForVariable_UnmappedVariable_ReturnsNull()
    {
        var mapping = new SourceMapping();

        var result = mapping.GetNodeForVariable("nonExistent");

        Assert.Null(result);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllMappings()
    {
        using var builder = new TestGraphBuilder();
        var node = builder.CreateFloatConstant(1.0f);
        var mapping = new SourceMapping();
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));

        mapping.AddMapping(node, span);
        mapping.AddVariableMapping("test", node);
        mapping.Clear();

        Assert.Null(mapping.GetSourceSpan(node));
        Assert.Null(mapping.GetNodeForVariable("test"));
    }

    #endregion

    #region SourceSpan Tests

    [Fact]
    public void SourceSpan_Contains_LocationAtStart_ReturnsTrue()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));
        var location = new SourceLocation("test.kesl", 1, 0);

        Assert.True(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_LocationAtEnd_ReturnsTrue()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));
        var location = new SourceLocation("test.kesl", 1, 10);

        Assert.True(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_LocationInMiddle_ReturnsTrue()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));
        var location = new SourceLocation("test.kesl", 1, 5);

        Assert.True(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_DifferentFile_ReturnsFalse()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 0),
            new SourceLocation("test.kesl", 1, 10));
        var location = new SourceLocation("other.kesl", 1, 5);

        Assert.False(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_LineBefore_ReturnsFalse()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 5, 0),
            new SourceLocation("test.kesl", 5, 10));
        var location = new SourceLocation("test.kesl", 4, 5);

        Assert.False(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_LineAfter_ReturnsFalse()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 5, 0),
            new SourceLocation("test.kesl", 5, 10));
        var location = new SourceLocation("test.kesl", 6, 5);

        Assert.False(span.Contains(location));
    }

    [Fact]
    public void SourceSpan_Contains_MultiLineSpan_MiddleLine_ReturnsTrue()
    {
        var span = new SourceSpan(
            new SourceLocation("test.kesl", 1, 5),
            new SourceLocation("test.kesl", 10, 15));
        var location = new SourceLocation("test.kesl", 5, 0);

        Assert.True(span.Contains(location));
    }

    #endregion
}
