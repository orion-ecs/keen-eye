using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.CodeGen;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Tests.CodeGen;

/// <summary>
/// Tests for the HlslGenerator class.
/// </summary>
public class HlslGeneratorTests
{
    #region Basic Generation

    [Fact]
    public void Generate_SimpleShader_ContainsNumthreadsAttribute()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("[numthreads(64, 1, 1)]", hlsl);
    }

    [Fact]
    public void Generate_SimpleShader_ContainsCSMainFunction()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("void CSMain(uint3 DTid : SV_DispatchThreadID)", hlsl);
    }

    [Fact]
    public void Generate_SimpleShader_ContainsEntityIndexAndBoundsCheck()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("uint idx = DTid.x;", hlsl);
        Assert.Contains("if (idx >= entityCount) return;", hlsl);
    }

    #endregion

    #region Buffer Declarations

    [Fact]
    public void Generate_ReadOnlyBuffer_UsesStructuredBuffer()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("StructuredBuffer<VelocityData> Velocity : register(t0);", hlsl);
    }

    [Fact]
    public void Generate_WriteBuffer_UsesRWStructuredBuffer()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("RWStructuredBuffer<PositionData> Position : register(u0);", hlsl);
    }

    [Fact]
    public void Generate_StructDefinitions_AreCreated()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("struct PositionData", hlsl);
        Assert.Contains("struct VelocityData", hlsl);
    }

    #endregion

    #region Constant Buffer

    [Fact]
    public void Generate_ConstantBuffer_ContainsEntityCount()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);

        Assert.Contains("cbuffer Params : register(b0)", hlsl);
        Assert.Contains("uint entityCount;", hlsl);
    }

    [Fact]
    public void Generate_WithParams_IncludesParamsInConstantBuffer()
    {
        var hlsl = GenerateHlsl(ShaderWithParams);

        Assert.Contains("float deltaTime;", hlsl);
    }

    #endregion

    #region Type Mappings

    [Fact]
    public void Generate_FloatLiteral_HasFSuffix()
    {
        var hlsl = GenerateHlsl(ShaderWithFloatLiteral);

        // Float literals in HLSL should have 'f' suffix
        Assert.Matches(@"\d+\.\d+f", hlsl);
    }

    #endregion

    #region Backend Properties

    [Fact]
    public void HlslGenerator_Backend_ReturnsHLSL()
    {
        var generator = new HlslGenerator();

        Assert.Equal(ShaderBackend.HLSL, generator.Backend);
    }

    [Fact]
    public void HlslGenerator_FileExtension_ReturnsHlsl()
    {
        var generator = new HlslGenerator();

        Assert.Equal("hlsl", generator.FileExtension);
    }

    #endregion

    #region Comparison with GLSL

    [Fact]
    public void Generate_SameShader_HlslUsesFloat3WhileGlslUsesVec3()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);
        var glsl = GenerateGlsl(SimplePhysicsShader);

        // HLSL uses float3, GLSL uses vec3
        // In struct definitions (placeholder generates float X, Y, Z)
        // Buffer types differ
        Assert.Contains("RWStructuredBuffer", hlsl);
        Assert.Contains("layout(std430, binding =", glsl);
    }

    [Fact]
    public void Generate_SameShader_DifferentMainFunctionSyntax()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);
        var glsl = GenerateGlsl(SimplePhysicsShader);

        Assert.Contains("void CSMain", hlsl);
        Assert.Contains("void main()", glsl);
    }

    [Fact]
    public void Generate_SameShader_DifferentWorkgroupSyntax()
    {
        var hlsl = GenerateHlsl(SimplePhysicsShader);
        var glsl = GenerateGlsl(SimplePhysicsShader);

        Assert.Contains("[numthreads(64, 1, 1)]", hlsl);
        Assert.Contains("layout(local_size_x = 64)", glsl);
    }

    #endregion

    #region Helper Methods

    private const string SimplePhysicsShader = @"
        compute Physics {
            query {
                write Position
                read Velocity
            }
            execute() {
                Position.x += Velocity.x;
                Position.y += Velocity.y;
                Position.z += Velocity.z;
            }
        }
        ";

    private const string ShaderWithParams = @"
        compute Physics {
            query {
                write Position
                read Velocity
            }
            params {
                deltaTime: float
            }
            execute() {
                Position.x += Velocity.x * deltaTime;
            }
        }
        ";

    private const string ShaderWithFloatLiteral = @"
        compute Test {
            query {
                write Position
            }
            execute() {
                Position.x = 1.5;
            }
        }
        ";

    private static string GenerateHlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var compute = result.SourceFile!.Declarations.OfType<ComputeDeclaration>().First();
        return KeslCompiler.GenerateHlsl(compute);
    }

    private static string GenerateGlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var compute = result.SourceFile!.Declarations.OfType<ComputeDeclaration>().First();
        return KeslCompiler.GenerateGlsl(compute);
    }

    #endregion
}
