using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Compiler;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests.Compiler;

public class KeslGraphCompilerTests
{
    private readonly KeslGraphCompiler compiler = new();

    #region Basic Compilation Tests

    [Fact]
    public void Compile_EmptyGraph_ReturnsFailure()
    {
        using var builder = new TestGraphBuilder();

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Compile_NoComputeShader_ReturnsFailure()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateAddNode();

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("ComputeShader"));
    }

    [Fact]
    public void Compile_MinimalShader_ReturnsSuccess()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ComputeDeclaration);
        Assert.Equal("TestShader", result.ComputeDeclaration.Name);
    }

    #endregion

    #region Query Block Tests

    [Fact]
    public void Compile_WithQueryBindings_CollectsBindings()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateQueryBinding("Velocity", AccessMode.Write);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ComputeDeclaration!.Query.Bindings.Count);
        Assert.Contains(result.ComputeDeclaration.Query.Bindings, b => b.ComponentName == "Position");
        Assert.Contains(result.ComputeDeclaration.Query.Bindings, b => b.ComponentName == "Velocity");
    }

    [Fact]
    public void Compile_QueryBinding_PreservesAccessMode()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateQueryBinding("Velocity", AccessMode.Write);

        var result = compiler.Compile(builder.Canvas, builder.World);

        var posBinding = result.ComputeDeclaration!.Query.Bindings.First(b => b.ComponentName == "Position");
        var velBinding = result.ComputeDeclaration.Query.Bindings.First(b => b.ComponentName == "Velocity");

        Assert.Equal(AccessMode.Read, posBinding.AccessMode);
        Assert.Equal(AccessMode.Write, velBinding.AccessMode);
    }

    #endregion

    #region Params Block Tests

    [Fact]
    public void Compile_WithParameters_CollectsParams()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateParameter("deltaTime", PortTypeId.Float);
        builder.CreateParameter("speed", PortTypeId.Float);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.ComputeDeclaration!.Params);
        Assert.Equal(2, result.ComputeDeclaration.Params.Parameters.Count);
        Assert.Contains(result.ComputeDeclaration.Params.Parameters, p => p.Name == "deltaTime");
        Assert.Contains(result.ComputeDeclaration.Params.Parameters, p => p.Name == "speed");
    }

    [Fact]
    public void Compile_NoParameters_ParamsBlockIsNull()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Read);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.True(result.ComputeDeclaration!.Params == null || result.ComputeDeclaration.Params.Parameters.Count == 0);
    }

    #endregion

    #region Execute Block Tests

    [Fact]
    public void Compile_WithSetVariable_GeneratesAssignment()
    {
        using var builder = new TestGraphBuilder();
        var compute = builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);
        var constant = builder.CreateFloatConstant(1.0f);
        var setVar = builder.CreateSetVariable("Position.X");
        // Connect constant output to setVar value input (port 1)
        builder.Connect(constant, 0, setVar, 1);
        // Connect setVar execute output (port 0) to compute execute input (port 0)
        builder.Connect(setVar, 0, compute, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.ComputeDeclaration!.Execute.Body);
    }

    [Fact]
    public void Compile_WithMathExpression_BuildsExpression()
    {
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("TestShader");
        builder.CreateQueryBinding("Position", AccessMode.Write);
        var const1 = builder.CreateFloatConstant(1.0f);
        var const2 = builder.CreateFloatConstant(2.0f);
        var add = builder.CreateAddNode();
        var setVar = builder.CreateSetVariable("Position.X");

        builder.Connect(const1, 0, add, 0);
        builder.Connect(const2, 0, add, 1);
        builder.Connect(add, 0, setVar, 0);

        var result = compiler.Compile(builder.Canvas, builder.World);

        // Should create an assignment with a binary add expression
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region Multiple Roots Behavior

    [Fact]
    public void Compile_MultipleComputeShaders_UsesFirstOne()
    {
        // Note: The compiler doesn't validate for multiple roots - that's done by validation rules
        // The compiler just finds and uses the first ComputeShader node
        using var builder = new TestGraphBuilder();
        builder.CreateComputeShader("Shader1");
        builder.CreateQueryBinding("Position", AccessMode.Read);
        builder.CreateComputeShader("Shader2");

        var result = compiler.Compile(builder.Canvas, builder.World);

        // Should succeed using the first shader (Shader1)
        Assert.True(result.IsSuccess);
        Assert.Equal("Shader1", result.ComputeDeclaration!.Name);
    }

    #endregion
}
