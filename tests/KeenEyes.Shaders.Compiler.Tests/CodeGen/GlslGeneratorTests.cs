using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.CodeGen;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Tests.CodeGen;

/// <summary>
/// Tests for the GlslGenerator class.
/// </summary>
public class GlslGeneratorTests
{
    #region Vertex Shader - Basic Structure

    [Fact]
    public void GenerateVertexShader_ContainsVersionDirective()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        Assert.Contains("#version 450", glsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsMainFunction()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        Assert.Contains("void main()", glsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsInputAttributes()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        Assert.Contains("layout(location = 0) in vec3 position;", glsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsOutputInterfaceBlock()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        Assert.Contains("out VS_OUT {", glsl);
        Assert.Contains("} vs_out;", glsl);
    }

    #endregion

    #region Vertex Shader - Input Attributes

    [Fact]
    public void GenerateVertexShader_MultipleInputs_AllHaveLayoutLocations()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithMultipleInputs);

        Assert.Contains("layout(location = 0) in vec3 position;", glsl);
        Assert.Contains("layout(location = 1) in vec3 normal;", glsl);
        Assert.Contains("layout(location = 2) in vec2 texCoord;", glsl);
    }

    [Fact]
    public void GenerateVertexShader_InputWithoutLocation_OmitsLayoutQualifier()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithoutInputLocation);

        Assert.Contains("in vec3 position;", glsl);
        Assert.DoesNotContain("layout(location =", glsl);
    }

    #endregion

    #region Vertex Shader - Outputs

    [Fact]
    public void GenerateVertexShader_OutputsInInterfaceBlock()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithMultipleOutputs);

        // Check that outputs are inside the VS_OUT block
        Assert.Contains("out VS_OUT {", glsl);
        Assert.Contains("vec3 worldPos;", glsl);
        Assert.Contains("vec3 worldNormal;", glsl);
        Assert.Contains("vec2 uv;", glsl);
    }

    [Fact]
    public void GenerateVertexShader_OutputAssignment_TransformsToVsOut()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        // Output assignments should be transformed to vs_out.name
        Assert.Contains("vs_out.outPos = position;", glsl);
    }

    #endregion

    #region Vertex Shader - Uniforms

    [Fact]
    public void GenerateVertexShader_WithParams_GeneratesUniforms()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithParams);

        Assert.Contains("uniform mat4 model;", glsl);
        Assert.Contains("uniform mat4 view;", glsl);
        Assert.Contains("uniform mat4 projection;", glsl);
    }

    #endregion

    #region Vertex Shader - Type Mappings

    [Fact]
    public void GenerateVertexShader_Float3Type_MapsToVec3()
    {
        var glsl = GenerateVertexGlsl(SimpleVertexShader);

        Assert.Contains("vec3 position", glsl);
        Assert.Contains("vec3 outPos", glsl);
    }

    [Fact]
    public void GenerateVertexShader_Float2Type_MapsToVec2()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithMultipleInputs);

        Assert.Contains("vec2 texCoord", glsl);
    }

    [Fact]
    public void GenerateVertexShader_Mat4Type_MapToMat4()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithParams);

        Assert.Contains("uniform mat4 model;", glsl);
    }

    #endregion

    #region Fragment Shader - Basic Structure

    [Fact]
    public void GenerateFragmentShader_ContainsVersionDirective()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        Assert.Contains("#version 450", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_ContainsMainFunction()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        Assert.Contains("void main()", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_ContainsInputInterfaceBlock()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        Assert.Contains("in VS_OUT {", glsl);
        Assert.Contains("} fs_in;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_ContainsOutputWithLocation()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        Assert.Contains("layout(location = 0) out vec4 fragColor;", glsl);
    }

    #endregion

    #region Fragment Shader - Inputs

    [Fact]
    public void GenerateFragmentShader_InputsInInterfaceBlock()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithMultipleInputs);

        Assert.Contains("in VS_OUT {", glsl);
        Assert.Contains("vec3 worldPos;", glsl);
        Assert.Contains("vec3 worldNormal;", glsl);
        Assert.Contains("vec2 uv;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_InputAccess_TransformsToFsIn()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        // Input access should be transformed to fs_in.name
        Assert.Contains("fs_in.color", glsl);
    }

    #endregion

    #region Fragment Shader - Uniforms

    [Fact]
    public void GenerateFragmentShader_WithParams_GeneratesUniforms()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithParams);

        Assert.Contains("uniform vec3 lightDir;", glsl);
        Assert.Contains("uniform vec3 lightColor;", glsl);
    }

    #endregion

    #region Fragment Shader - Outputs

    [Fact]
    public void GenerateFragmentShader_OutputAssignment_RemainsUnchanged()
    {
        var glsl = GenerateFragmentGlsl(SimpleFragmentShader);

        // Output assignments should NOT be prefixed (outputs are direct variables)
        Assert.Contains("fragColor = fs_in.color;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_MultipleOutputs_AllHaveLocations()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithMultipleOutputs);

        Assert.Contains("layout(location = 0) out vec4 fragColor;", glsl);
        Assert.Contains("layout(location = 1) out vec4 brightColor;", glsl);
    }

    #endregion

    #region Backend Properties

    [Fact]
    public void GlslGenerator_Backend_ReturnsGLSL()
    {
        var generator = new GlslGenerator();

        Assert.Equal(ShaderBackend.GLSL, generator.Backend);
    }

    [Fact]
    public void GlslGenerator_FileExtension_ReturnsGlsl()
    {
        var generator = new GlslGenerator();

        Assert.Equal("glsl", generator.FileExtension);
    }

    #endregion

    #region Expression Generation

    [Fact]
    public void GenerateVertexShader_BinaryExpression_GeneratesCorrectly()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithBinaryExpression);

        Assert.Contains("vs_out.outPos = (position + offset);", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_FunctionCall_GeneratesCorrectly()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithFunctionCalls);

        Assert.Contains("normalize(fs_in.worldNormal)", glsl);
        Assert.Contains("max(dot(", glsl);
    }

    #endregion

    #region Control Flow

    [Fact]
    public void GenerateVertexShader_IfStatement_GeneratesCorrectly()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithIfStatement);

        // Binary expressions are wrapped in parentheses by the generator
        Assert.Contains("if ((position.x > 0.0)) {", glsl);
        Assert.Contains("} else {", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_ForLoop_GeneratesCorrectly()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithForLoop);

        Assert.Contains("for (int i = 0; i < 4; i++) {", glsl);
    }

    #endregion

    #region Texture and Sampler Support

    [Fact]
    public void GenerateFragmentShader_WithTextures_GeneratesSampler2DUniforms()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithTextures);

        Assert.Contains("layout(binding = 0) uniform sampler2D diffuseMap;", glsl);
        Assert.Contains("layout(binding = 1) uniform sampler2D normalMap;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithTextureCube_GeneratesSamplerCubeUniform()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithTextureCube);

        Assert.Contains("layout(binding = 0) uniform samplerCube skybox;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithTexture3D_GeneratesSampler3DUniform()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithTexture3D);

        Assert.Contains("layout(binding = 0) uniform sampler3D volumeMap;", glsl);
    }

    [Fact]
    public void GenerateFragmentShader_SampleFunction_MapsToTexture()
    {
        var glsl = GenerateFragmentGlsl(FragmentShaderWithSample);

        // In GLSL, sample() becomes texture() and only takes (sampler2d, uv) since they're combined
        Assert.Contains("texture(diffuseMap, fs_in.uv)", glsl);
    }

    [Fact]
    public void GenerateVertexShader_WithTextures_GeneratesUniform()
    {
        var glsl = GenerateVertexGlsl(VertexShaderWithTextures);

        Assert.Contains("layout(binding = 0) uniform sampler2D heightMap;", glsl);
    }

    #endregion

    #region Test Shader Sources

    private const string FragmentShaderWithTextures = @"
        fragment TexturedSurface {
            in {
                uv: float2
            }
            out {
                fragColor: float4 @ 0
            }
            textures {
                diffuseMap: texture2D @ 0
                normalMap: texture2D @ 1
            }
            execute() {
                fragColor = uv;
            }
        }
    ";

    private const string FragmentShaderWithTextureCube = @"
        fragment SkyboxSurface {
            in {
                direction: float3
            }
            out {
                fragColor: float4 @ 0
            }
            textures {
                skybox: textureCube @ 0
            }
            execute() {
                fragColor = direction;
            }
        }
    ";

    private const string FragmentShaderWithTexture3D = @"
        fragment VolumeSurface {
            in {
                uvw: float3
            }
            out {
                fragColor: float4 @ 0
            }
            textures {
                volumeMap: texture3D @ 0
            }
            execute() {
                fragColor = uvw;
            }
        }
    ";

    private const string FragmentShaderWithSample = @"
        fragment SampledSurface {
            in {
                uv: float2
            }
            out {
                fragColor: float4 @ 0
            }
            textures {
                diffuseMap: texture2D @ 0
            }
            samplers {
                linearSampler: sampler @ 0
            }
            execute() {
                fragColor = sample(diffuseMap, linearSampler, uv);
            }
        }
    ";

    private const string VertexShaderWithTextures = @"
        vertex DisplacementVertex {
            in {
                position: float3 @ 0
                uv: float2 @ 1
            }
            out {
                outPos: float3
            }
            textures {
                heightMap: texture2D @ 0
            }
            execute() {
                outPos = position;
            }
        }
    ";

    private const string SimpleVertexShader = @"
        vertex SimpleVertex {
            in {
                position: float3 @ 0
            }
            out {
                outPos: float3
            }
            execute() {
                outPos = position;
            }
        }
    ";

    private const string VertexShaderWithMultipleInputs = @"
        vertex TransformVertex {
            in {
                position: float3 @ 0
                normal: float3 @ 1
                texCoord: float2 @ 2
            }
            out {
                worldPos: float3
            }
            execute() {
                worldPos = position;
            }
        }
    ";

    private const string VertexShaderWithoutInputLocation = @"
        vertex NoLocationVertex {
            in {
                position: float3
            }
            out {
                outPos: float3
            }
            execute() {
                outPos = position;
            }
        }
    ";

    private const string VertexShaderWithMultipleOutputs = @"
        vertex MultiOutputVertex {
            in {
                position: float3 @ 0
                normal: float3 @ 1
                texCoord: float2 @ 2
            }
            out {
                worldPos: float3
                worldNormal: float3
                uv: float2
            }
            execute() {
                worldPos = position;
                worldNormal = normal;
                uv = texCoord;
            }
        }
    ";

    private const string VertexShaderWithParams = @"
        vertex ParamVertex {
            in {
                position: float3 @ 0
            }
            out {
                outPos: float3
            }
            params {
                model: mat4
                view: mat4
                projection: mat4
            }
            execute() {
                outPos = position;
            }
        }
    ";

    private const string VertexShaderWithBinaryExpression = @"
        vertex BinaryVertex {
            in {
                position: float3 @ 0
            }
            out {
                outPos: float3
            }
            params {
                offset: float3
            }
            execute() {
                outPos = position + offset;
            }
        }
    ";

    private const string VertexShaderWithIfStatement = @"
        vertex IfVertex {
            in {
                position: float3 @ 0
            }
            out {
                outPos: float3
            }
            execute() {
                if (position.x > 0.0) {
                    outPos = position;
                } else {
                    outPos = position;
                }
            }
        }
    ";

    private const string SimpleFragmentShader = @"
        fragment SimpleFragment {
            in {
                color: float4
            }
            out {
                fragColor: float4 @ 0
            }
            execute() {
                fragColor = color;
            }
        }
    ";

    private const string FragmentShaderWithMultipleInputs = @"
        fragment MultiInputFragment {
            in {
                worldPos: float3
                worldNormal: float3
                uv: float2
            }
            out {
                fragColor: float4 @ 0
            }
            execute() {
                fragColor = worldNormal;
            }
        }
    ";

    private const string FragmentShaderWithParams = @"
        fragment ParamFragment {
            in {
                worldNormal: float3
            }
            out {
                fragColor: float4 @ 0
            }
            params {
                lightDir: float3
                lightColor: float3
            }
            execute() {
                fragColor = lightColor;
            }
        }
    ";

    private const string FragmentShaderWithMultipleOutputs = @"
        fragment MultiOutputFragment {
            in {
                color: float4
            }
            out {
                fragColor: float4 @ 0
                brightColor: float4 @ 1
            }
            execute() {
                fragColor = color;
                brightColor = color;
            }
        }
    ";

    private const string FragmentShaderWithFunctionCalls = @"
        fragment FunctionFragment {
            in {
                worldNormal: float3
            }
            out {
                fragColor: float4 @ 0
            }
            params {
                lightDir: float3
            }
            execute() {
                fragColor = max(dot(normalize(worldNormal), lightDir), 0.0);
            }
        }
    ";

    private const string FragmentShaderWithForLoop = @"
        fragment ForLoopFragment {
            in {
                color: float4
            }
            out {
                fragColor: float4 @ 0
            }
            execute() {
                for (i: 0..4) {
                    fragColor = color;
                }
            }
        }
    ";

    #endregion

    #region Helper Methods

    private static string GenerateVertexGlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var vertex = result.SourceFile!.Declarations.OfType<VertexDeclaration>().First();
        return KeslCompiler.GenerateGlsl(vertex);
    }

    private static string GenerateFragmentGlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var fragment = result.SourceFile!.Declarations.OfType<FragmentDeclaration>().First();
        return KeslCompiler.GenerateGlsl(fragment);
    }

    #endregion
}
