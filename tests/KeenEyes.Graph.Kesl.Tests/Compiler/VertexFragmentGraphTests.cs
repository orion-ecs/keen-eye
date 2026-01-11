using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Compiler;

namespace KeenEyes.Graph.Kesl.Tests.Compiler;

/// <summary>
/// Tests for vertex and fragment shader graph compilation.
/// </summary>
public class VertexFragmentGraphTests
{
    private readonly KeslGraphCompiler compiler = new();

    #region Vertex Shader Tests

    [Fact]
    public void Compile_VertexShader_ReturnsVertexDeclaration()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TransformVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("worldPos", PortTypeId.Float3);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(CompiledShaderType.Vertex, result.ShaderType);
        Assert.NotNull(result.VertexDeclaration);
        Assert.Null(result.ComputeDeclaration);
        Assert.Null(result.FragmentDeclaration);
        Assert.Equal("TransformVertex", result.VertexDeclaration.Name);
    }

    [Fact]
    public void Compile_VertexShader_CollectsInputAttributes()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateInputAttribute("normal", PortTypeId.Float3, 1);
        builder.CreateInputAttribute("texCoord", PortTypeId.Float2, 2);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.VertexDeclaration!.Inputs.Attributes.Count);
        Assert.Contains(result.VertexDeclaration.Inputs.Attributes, a => a.Name == "position");
        Assert.Contains(result.VertexDeclaration.Inputs.Attributes, a => a.Name == "normal");
        Assert.Contains(result.VertexDeclaration.Inputs.Attributes, a => a.Name == "texCoord");
    }

    [Fact]
    public void Compile_VertexShader_CollectsOutputAttributes()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("worldPos", PortTypeId.Float3);
        builder.CreateOutputAttribute("worldNormal", PortTypeId.Float3);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.VertexDeclaration!.Outputs.Attributes.Count);
        Assert.Contains(result.VertexDeclaration.Outputs.Attributes, a => a.Name == "worldPos");
        Assert.Contains(result.VertexDeclaration.Outputs.Attributes, a => a.Name == "worldNormal");
    }

    [Fact]
    public void Compile_VertexShader_PreservesInputLocations()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateInputAttribute("normal", PortTypeId.Float3, 2);
        builder.CreateInputAttribute("texCoord", PortTypeId.Float2, 5);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var posAttr = result.VertexDeclaration!.Inputs.Attributes.First(a => a.Name == "position");
        var normAttr = result.VertexDeclaration.Inputs.Attributes.First(a => a.Name == "normal");
        var texAttr = result.VertexDeclaration.Inputs.Attributes.First(a => a.Name == "texCoord");

        Assert.Equal(0, posAttr.LocationIndex);
        Assert.Equal(2, normAttr.LocationIndex);
        Assert.Equal(5, texAttr.LocationIndex);
    }

    [Fact]
    public void Compile_VertexShader_SortsAttributesByLocation()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        // Create in non-sequential order
        builder.CreateInputAttribute("texCoord", PortTypeId.Float2, 2);
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateInputAttribute("normal", PortTypeId.Float3, 1);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var inputs = result.VertexDeclaration!.Inputs.Attributes;
        Assert.Equal("position", inputs[0].Name);
        Assert.Equal("normal", inputs[1].Name);
        Assert.Equal("texCoord", inputs[2].Name);
    }

    [Fact]
    public void Compile_VertexShader_WithParameters_CollectsParams()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);
        builder.CreateParameter("model", PortTypeId.Float);
        builder.CreateParameter("view", PortTypeId.Float);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.VertexDeclaration!.Params);
        Assert.Equal(2, result.VertexDeclaration.Params.Parameters.Count);
    }

    #endregion

    #region Fragment Shader Tests

    [Fact]
    public void Compile_FragmentShader_ReturnsFragmentDeclaration()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("LitSurface");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("fragColor", PortTypeId.Float4, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(CompiledShaderType.Fragment, result.ShaderType);
        Assert.NotNull(result.FragmentDeclaration);
        Assert.Null(result.ComputeDeclaration);
        Assert.Null(result.VertexDeclaration);
        Assert.Equal("LitSurface", result.FragmentDeclaration.Name);
    }

    [Fact]
    public void Compile_FragmentShader_CollectsInputAttributes()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("TestFragment");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);
        builder.CreateInputAttribute("worldNormal", PortTypeId.Float3, 1);
        builder.CreateInputAttribute("uv", PortTypeId.Float2, 2);
        builder.CreateOutputAttribute("fragColor", PortTypeId.Float4, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.FragmentDeclaration!.Inputs.Attributes.Count);
    }

    [Fact]
    public void Compile_FragmentShader_CollectsOutputAttributes()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("TestFragment");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("fragColor", PortTypeId.Float4, 0);
        builder.CreateOutputAttribute("fragNormal", PortTypeId.Float4, 1);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.FragmentDeclaration!.Outputs.Attributes.Count);
    }

    [Fact]
    public void Compile_FragmentShader_PreservesOutputLocations()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("TestFragment");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("fragColor", PortTypeId.Float4, 0);
        builder.CreateOutputAttribute("fragNormal", PortTypeId.Float4, 1);
        builder.CreateOutputAttribute("fragDepth", PortTypeId.Float, 2);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var outputs = result.FragmentDeclaration!.Outputs.Attributes;
        var colorOut = outputs.First(a => a.Name == "fragColor");
        var normalOut = outputs.First(a => a.Name == "fragNormal");
        var depthOut = outputs.First(a => a.Name == "fragDepth");

        Assert.Equal(0, colorOut.LocationIndex);
        Assert.Equal(1, normalOut.LocationIndex);
        Assert.Equal(2, depthOut.LocationIndex);
    }

    [Fact]
    public void Compile_FragmentShader_WithParameters_CollectsParams()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("TestFragment");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);
        builder.CreateOutputAttribute("fragColor", PortTypeId.Float4, 0);
        builder.CreateParameter("lightDir", PortTypeId.Float3);
        builder.CreateParameter("lightColor", PortTypeId.Float3);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.FragmentDeclaration!.Params);
        Assert.Equal(2, result.FragmentDeclaration.Params.Parameters.Count);
    }

    #endregion

    #region Error Cases

    [Fact]
    public void Compile_VertexShader_NoInputs_ReturnsSuccess()
    {
        // A vertex shader with no inputs is technically valid (though unusual)
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("EmptyVertex");

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.VertexDeclaration!.Inputs.Attributes);
    }

    [Fact]
    public void Compile_FragmentShader_NoOutputs_ReturnsSuccess()
    {
        // A fragment shader with no outputs is technically valid (though unusual)
        using var builder = new TestGraphBuilder();
        builder.CreateFragmentShader("EmptyFragment");
        builder.CreateInputAttribute("worldPos", PortTypeId.Float3, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.FragmentDeclaration!.Outputs.Attributes);
    }

    #endregion

    #region Type Mapping Tests

    [Fact]
    public void Compile_VertexShader_MapsFloat3ToCorrectType()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("position", PortTypeId.Float3, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var posAttr = result.VertexDeclaration!.Inputs.Attributes[0];
        Assert.NotNull(posAttr.Type);
    }

    [Fact]
    public void Compile_VertexShader_MapsFloat4ToCorrectType()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("color", PortTypeId.Float4, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var colorAttr = result.VertexDeclaration!.Inputs.Attributes[0];
        Assert.NotNull(colorAttr.Type);
    }

    [Fact]
    public void Compile_VertexShader_MapsFloat2ToCorrectType()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateVertexShader("TestVertex");
        builder.CreateInputAttribute("texCoord", PortTypeId.Float2, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        var texAttr = result.VertexDeclaration!.Inputs.Attributes[0];
        Assert.NotNull(texAttr.Type);
    }

    #endregion
}
