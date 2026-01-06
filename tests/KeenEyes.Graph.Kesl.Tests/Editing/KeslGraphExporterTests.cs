using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Editing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Editing;

public class KeslGraphExporterTests
{
    private readonly KeslGraphExporter exporter = new();

    #region Basic Export Tests

    [Fact]
    public void Export_EmptyGraph_ReturnsFailure()
    {
        using var builder = new TestGraphBuilder();

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Export_MinimalShader_ReturnsSuccess()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Source);
        Assert.Contains("compute TestShader", result.Source);
    }

    [Fact]
    public void Export_WithQueryBinding_IncludesQueryBlock()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateQueryBinding("Velocity", AccessMode.Write);

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Contains("query", result.Source);
        // Check for component names in query
        Assert.Contains("Position", result.Source);
        Assert.Contains("Velocity", result.Source);
    }

    [Fact]
    public void Export_WithParameter_IncludesParamsBlock()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateParameter("deltaTime", PortTypeId.Float);

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Contains("params", result.Source);
        Assert.Contains("deltaTime", result.Source);
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void Export_WithTabIndentation_UsesTabs()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        exporter.Options = new ExportOptions { UseTabs = true };
        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Contains("\t", result.Source);
    }

    [Fact]
    public void Export_WithSpaceIndentation_UsesSpaces()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        exporter.Options = new ExportOptions { UseTabs = false, IndentSize = 2 };
        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Contains("  ", result.Source); // Two spaces for indent
        Assert.DoesNotContain("\t", result.Source);
    }

    #endregion

    #region AccessMode Tests

    [Fact]
    public void Export_ReadAccessMode_GeneratesRead()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.Contains("read Position", result.Source);
    }

    [Fact]
    public void Export_WriteAccessMode_GeneratesWrite()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Velocity", AccessMode.Write);

        var result = exporter.Export(builder.Canvas, builder.World);

        Assert.Contains("write Velocity", result.Source);
    }

    #endregion
}
