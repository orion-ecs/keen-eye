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

    #region Vertex Shader - Basic Structure

    [Fact]
    public void GenerateVertexShader_ContainsVsInputStruct()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("struct VS_INPUT", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsVsOutputStruct()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("struct VS_OUTPUT", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsMainFunction()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("VS_OUTPUT VSMain(VS_INPUT input)", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsSvPositionInOutput()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("float4 position : SV_POSITION;", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_ContainsReturnOutput()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("return output;", hlsl);
    }

    #endregion

    #region Vertex Shader - Input Semantics

    [Fact]
    public void GenerateVertexShader_PositionInput_HasPositionSemantic()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("float3 position : POSITION;", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_NormalInput_HasNormalSemantic()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithMultipleInputs);

        Assert.Contains("float3 normal : NORMAL;", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_TexCoordInput_HasTexCoordSemantic()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithMultipleInputs);

        Assert.Contains("float2 texCoord : TEXCOORD0;", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_CustomInput_UsesLocationAsSemantic()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithCustomInput);

        Assert.Contains("float3 customData : TEXCOORD5;", hlsl);
    }

    #endregion

    #region Vertex Shader - Output Semantics

    [Fact]
    public void GenerateVertexShader_OutputsHaveTexcoordSemantics()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithMultipleOutputs);

        Assert.Contains("float3 worldPos : TEXCOORD0;", hlsl);
        Assert.Contains("float3 worldNormal : TEXCOORD1;", hlsl);
        Assert.Contains("float2 uv : TEXCOORD2;", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_OutputAssignment_TransformsToOutputPrefix()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("output.outPos = input.position;", hlsl);
    }

    #endregion

    #region Vertex Shader - Uniforms (cbuffer)

    [Fact]
    public void GenerateVertexShader_WithParams_GeneratesCbuffer()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithParams);

        Assert.Contains("cbuffer VertexParams : register(b0)", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_WithParams_ContainsMatrixParams()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithParams);

        Assert.Contains("float4x4 model;", hlsl);
        Assert.Contains("float4x4 view;", hlsl);
        Assert.Contains("float4x4 projection;", hlsl);
    }

    #endregion

    #region Vertex Shader - Type Mappings

    [Fact]
    public void GenerateVertexShader_Float3Type_MapsToFloat3()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("float3 position", hlsl);
        Assert.Contains("float3 outPos", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_Float2Type_MapsToFloat2()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithMultipleInputs);

        Assert.Contains("float2 texCoord", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_Mat4Type_MapsToFloat4x4()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithParams);

        Assert.Contains("float4x4 model;", hlsl);
    }

    #endregion

    #region Fragment Shader - Basic Structure

    [Fact]
    public void GenerateFragmentShader_ContainsPsInputStruct()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("struct PS_INPUT", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_PsInput_ContainsSvPosition()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("float4 position : SV_POSITION;", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_SingleOutput_UsesDirectReturnType()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("float4 PSMain(PS_INPUT input) : SV_TARGET", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_SingleOutput_DeclaresLocalVariable()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("float4 fragColor = (float4)0;", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_SingleOutput_ReturnsVariable()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("return fragColor;", hlsl);
    }

    #endregion

    #region Fragment Shader - Multiple Outputs

    [Fact]
    public void GenerateFragmentShader_MultipleOutputs_GeneratesPsOutputStruct()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithMultipleOutputs);

        Assert.Contains("struct PS_OUTPUT", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_MultipleOutputs_UsesSvTargetSemantics()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithMultipleOutputs);

        Assert.Contains("float4 fragColor : SV_TARGET0;", hlsl);
        Assert.Contains("float4 brightColor : SV_TARGET1;", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_MultipleOutputs_UsesStructReturnType()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithMultipleOutputs);

        Assert.Contains("PS_OUTPUT PSMain(PS_INPUT input)", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_MultipleOutputs_ReturnsOutputStruct()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithMultipleOutputs);

        Assert.Contains("PS_OUTPUT output = (PS_OUTPUT)0;", hlsl);
        Assert.Contains("return output;", hlsl);
    }

    #endregion

    #region Fragment Shader - Inputs

    [Fact]
    public void GenerateFragmentShader_InputsHaveTexcoordSemantics()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithMultipleInputs);

        Assert.Contains("float3 worldPos : TEXCOORD0;", hlsl);
        Assert.Contains("float3 worldNormal : TEXCOORD1;", hlsl);
        Assert.Contains("float2 uv : TEXCOORD2;", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_InputAccess_TransformsToInputPrefix()
    {
        var hlsl = GenerateFragmentHlsl(SimpleFragmentShader);

        Assert.Contains("input.color", hlsl);
    }

    #endregion

    #region Fragment Shader - Uniforms (cbuffer)

    [Fact]
    public void GenerateFragmentShader_WithParams_GeneratesCbuffer()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithParams);

        Assert.Contains("cbuffer PixelParams : register(b0)", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithParams_ContainsParams()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithParams);

        Assert.Contains("float3 lightDir;", hlsl);
        Assert.Contains("float3 lightColor;", hlsl);
    }

    #endregion

    #region Vertex/Fragment Expression Generation

    [Fact]
    public void GenerateVertexShader_BinaryExpression_GeneratesCorrectly()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithBinaryExpression);

        Assert.Contains("output.outPos = (input.position + offset);", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_FunctionCall_GeneratesCorrectly()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithFunctionCalls);

        Assert.Contains("normalize(input.worldNormal)", hlsl);
        Assert.Contains("max(dot(", hlsl);
    }

    #endregion

    #region Vertex/Fragment Control Flow

    [Fact]
    public void GenerateVertexShader_IfStatement_GeneratesCorrectly()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithIfStatement);

        Assert.Contains("if ((input.position.x > 0.0f))", hlsl);
        Assert.Contains("else", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_ForLoop_GeneratesCorrectly()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithForLoop);

        Assert.Contains("for (int i = 0; i < 4; i++)", hlsl);
    }

    #endregion

    #region HLSL-Specific Features

    [Fact]
    public void GenerateVertexShader_OutputInitialization_UsesHlslCastSyntax()
    {
        var hlsl = GenerateVertexHlsl(SimpleVertexShader);

        Assert.Contains("VS_OUTPUT output = (VS_OUTPUT)0;", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_FloatLiterals_HaveFloatSuffix()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithFunctionCalls);

        Assert.Contains("0.0f", hlsl);
    }

    #endregion

    #region Texture and Sampler Support

    [Fact]
    public void GenerateFragmentShader_WithTextures_GeneratesTexture2DDeclarations()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithTextures);

        Assert.Contains("Texture2D diffuseMap : register(t0);", hlsl);
        Assert.Contains("Texture2D normalMap : register(t1);", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithSamplers_GeneratesSamplerStateDeclarations()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithSamplers);

        Assert.Contains("SamplerState linearSampler : register(s0);", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithTextureCube_GeneratesTextureCubeDeclaration()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithTextureCube);

        Assert.Contains("TextureCube skybox : register(t0);", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_WithTexture3D_GeneratesTexture3DDeclaration()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithTexture3D);

        Assert.Contains("Texture3D volumeMap : register(t0);", hlsl);
    }

    [Fact]
    public void GenerateFragmentShader_SampleFunction_UsesHlslSampleSyntax()
    {
        var hlsl = GenerateFragmentHlsl(FragmentShaderWithSample);

        // In HLSL, sample(texture, sampler, uv) becomes texture.Sample(sampler, uv)
        Assert.Contains("diffuseMap.Sample(linearSampler, input.uv)", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_WithTextures_GeneratesTextureDeclaration()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithTextures);

        Assert.Contains("Texture2D heightMap : register(t0);", hlsl);
    }

    [Fact]
    public void GenerateVertexShader_WithSamplers_GeneratesSamplerDeclaration()
    {
        var hlsl = GenerateVertexHlsl(VertexShaderWithSamplers);

        Assert.Contains("SamplerState linearSampler : register(s0);", hlsl);
    }

    #endregion

    #region Compute Shader Test Sources

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

    #endregion

    #region Vertex Shader Test Sources

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

    private const string VertexShaderWithCustomInput = @"
        vertex CustomVertex {
            in {
                position: float3 @ 0
                customData: float3 @ 5
            }
            out {
                outPos: float3
            }
            execute() {
                outPos = position + customData;
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

    #endregion

    #region Texture/Sampler Shader Test Sources

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

    private const string FragmentShaderWithSamplers = @"
        fragment SampledSurface {
            in {
                uv: float2
            }
            out {
                fragColor: float4 @ 0
            }
            samplers {
                linearSampler: sampler @ 0
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
        fragment TexturedWithSample {
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

    private const string VertexShaderWithSamplers = @"
        vertex SampledVertex {
            in {
                position: float3 @ 0
            }
            out {
                outPos: float3
            }
            samplers {
                linearSampler: sampler @ 0
            }
            execute() {
                outPos = position;
            }
        }
    ";

    #endregion

    #region Fragment Shader Test Sources

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

    #region Geometry Shader Support

    [Fact]
    public void GenerateGeometryShader_ContainsMaxVertexCountAttribute()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("[maxvertexcount(6)]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_ContainsGsMainFunction()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("void GSMain(", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_ContainsGsInputStruct()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("struct GS_INPUT", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_ContainsGsOutputStruct()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("struct GS_OUTPUT", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_TrianglesInput_UsesTriangle()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("triangle GS_INPUT input[3]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_LineStripOutput_UsesLineStream()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("inout LineStream<GS_OUTPUT>", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_EmitVertex_UsesAppend()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderWithEmit);

        Assert.Contains("outputStream.Append(", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_EndPrimitive_UsesRestartStrip()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderWithEndPrimitive);

        Assert.Contains("outputStream.RestartStrip();", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_InputArrayAccess_TransformsToInput()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderWithArrayAccess);

        Assert.Contains("input[0].position", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_OutputAssignment_TransformsToOutput()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("output.color", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_PointsInput_UsesPoint()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderPointsInput);

        Assert.Contains("point GS_INPUT input[1]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_LinesInput_UsesLine()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderLinesInput);

        Assert.Contains("line GS_INPUT input[2]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_LinesAdjacencyInput_UsesLineAdj()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderLinesAdjacencyInput);

        Assert.Contains("lineadj GS_INPUT input[4]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_TrianglesAdjacencyInput_UsesTriangleAdj()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderTrianglesAdjacencyInput);

        Assert.Contains("triangleadj GS_INPUT input[6]", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_PointsOutput_UsesPointStream()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderPointsOutput);

        Assert.Contains("inout PointStream<GS_OUTPUT>", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_TriangleStripOutput_UsesTriangleStream()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderTriangleStripOutput);

        Assert.Contains("inout TriangleStream<GS_OUTPUT>", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_WithParams_GeneratesCbuffer()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderWithParams);

        Assert.Contains("cbuffer GeometryParams : register(b0)", hlsl);
        Assert.Contains("float4 wireColor;", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_ForLoop_GeneratesCorrectly()
    {
        var hlsl = GenerateGeometryHlsl(GeometryShaderWithForLoop);

        Assert.Contains("for (int i = 0; i < 3; i++)", hlsl);
    }

    [Fact]
    public void GenerateGeometryShader_OutputHasSvPosition()
    {
        var hlsl = GenerateGeometryHlsl(SimpleGeometryShader);

        Assert.Contains("float4 position : SV_POSITION;", hlsl);
    }

    #endregion

    #region Geometry Shader Test Sources

    private const string SimpleGeometryShader = @"
        geometry WireframeExpander {
            layout {
                input: triangles
                output: line_strip
                max_vertices: 6
            }
            in {
                position: float3
            }
            out {
                color: float4
            }
            execute() {
                color = position;
            }
        }
    ";

    private const string GeometryShaderWithEmit = @"
        geometry EmitTest {
            layout {
                input: triangles
                output: triangle_strip
                max_vertices: 3
            }
            in {
                position: float3
            }
            out {
                outPos: float3
            }
            execute() {
                emit(position);
            }
        }
    ";

    private const string GeometryShaderWithEndPrimitive = @"
        geometry EndPrimTest {
            layout {
                input: triangles
                output: line_strip
                max_vertices: 6
            }
            in {
                position: float3
            }
            out {
                outPos: float3
            }
            execute() {
                emit(position);
                endPrimitive();
            }
        }
    ";

    private const string GeometryShaderWithArrayAccess = @"
        geometry ArrayAccessTest {
            layout {
                input: triangles
                output: triangle_strip
                max_vertices: 3
            }
            in {
                position: float3
            }
            out {
                outPos: float3
            }
            execute() {
                outPos = vertices[0].position;
            }
        }
    ";

    private const string GeometryShaderPointsInput = @"
        geometry PointsInputTest {
            layout {
                input: points
                output: points
                max_vertices: 1
            }
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

    private const string GeometryShaderLinesInput = @"
        geometry LinesInputTest {
            layout {
                input: lines
                output: line_strip
                max_vertices: 2
            }
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

    private const string GeometryShaderLinesAdjacencyInput = @"
        geometry LinesAdjInputTest {
            layout {
                input: lines_adjacency
                output: line_strip
                max_vertices: 4
            }
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

    private const string GeometryShaderTrianglesAdjacencyInput = @"
        geometry TrianglesAdjInputTest {
            layout {
                input: triangles_adjacency
                output: triangle_strip
                max_vertices: 6
            }
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

    private const string GeometryShaderPointsOutput = @"
        geometry PointsOutputTest {
            layout {
                input: triangles
                output: points
                max_vertices: 1
            }
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

    private const string GeometryShaderTriangleStripOutput = @"
        geometry TriangleStripOutputTest {
            layout {
                input: triangles
                output: triangle_strip
                max_vertices: 3
            }
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

    private const string GeometryShaderWithParams = @"
        geometry ParamsTest {
            layout {
                input: triangles
                output: line_strip
                max_vertices: 6
            }
            in {
                position: float3
            }
            out {
                color: float4
            }
            params {
                wireColor: float4
            }
            execute() {
                color = wireColor;
            }
        }
    ";

    private const string GeometryShaderWithForLoop = @"
        geometry ForLoopTest {
            layout {
                input: triangles
                output: line_strip
                max_vertices: 6
            }
            in {
                position: float3
            }
            out {
                outPos: float3
            }
            execute() {
                for (i: 0..3) {
                    emit(vertices[i].position);
                }
                endPrimitive();
            }
        }
    ";

    #endregion

    #region Helper Methods

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

    private static string GenerateVertexHlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var vertex = result.SourceFile!.Declarations.OfType<VertexDeclaration>().First();
        return KeslCompiler.GenerateHlsl(vertex);
    }

    private static string GenerateFragmentHlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var fragment = result.SourceFile!.Declarations.OfType<FragmentDeclaration>().First();
        return KeslCompiler.GenerateHlsl(fragment);
    }

    private static string GenerateGeometryHlsl(string source)
    {
        var result = KeslCompiler.Compile(source);
        Assert.False(result.HasErrors, $"Compilation errors: {string.Join(", ", result.Errors.Select(e => e.Message))}");

        var geometry = result.SourceFile!.Declarations.OfType<GeometryDeclaration>().First();
        return KeslCompiler.GenerateHlsl(geometry);
    }

    #endregion
}
