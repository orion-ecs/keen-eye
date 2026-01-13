using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Tests;

public class ParserTests
{
    private static SourceFile Parse(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();
        var parser = new Parser(tokens);
        var result = parser.Parse();

        if (parser.HasErrors)
        {
            throw new Exception($"Parse errors: {string.Join(", ", parser.Diagnostics.Select(d => d.Message))}");
        }

        return result;
    }

    [Fact]
    public void Parse_EmptySource_ReturnsEmptySourceFile()
    {
        var result = Parse("");

        Assert.Empty(result.Declarations);
    }

    [Fact]
    public void Parse_ComponentDeclaration_ParsesCorrectly()
    {
        var source = @"
            component Position {
                x: float
                y: float
                z: float
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var component = Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.Equal("Position", component.Name);
        Assert.Equal(3, component.Fields.Count);
        Assert.Equal("x", component.Fields[0].Name);
        Assert.Equal("y", component.Fields[1].Name);
        Assert.Equal("z", component.Fields[2].Name);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesQueryBlock()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                    read Velocity
                    without Frozen
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        Assert.Equal("UpdatePhysics", compute.Name);

        Assert.Equal(3, compute.Query.Bindings.Count);
        Assert.Equal(AccessMode.Write, compute.Query.Bindings[0].AccessMode);
        Assert.Equal("Position", compute.Query.Bindings[0].ComponentName);
        Assert.Equal(AccessMode.Read, compute.Query.Bindings[1].AccessMode);
        Assert.Equal("Velocity", compute.Query.Bindings[1].ComponentName);
        Assert.Equal(AccessMode.Without, compute.Query.Bindings[2].AccessMode);
        Assert.Equal("Frozen", compute.Query.Bindings[2].ComponentName);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesParamsBlock()
    {
        var source = @"
            compute UpdatePhysics {
                query {
                    write Position
                }
                params {
                    deltaTime: float
                    gravity: float
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);

        Assert.NotNull(compute.Params);
        Assert.Equal(2, compute.Params.Parameters.Count);
        Assert.Equal("deltaTime", compute.Params.Parameters[0].Name);
        Assert.Equal("gravity", compute.Params.Parameters[1].Name);
    }

    [Fact]
    public void Parse_ComputeShader_ParsesExecuteBlock()
    {
        var source = @"
            compute UpdatePhysics {
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
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        Assert.Single(compute.Execute.Body);

        var stmt = Assert.IsType<CompoundAssignmentStatement>(compute.Execute.Body[0]);
        Assert.Equal(CompoundOperator.PlusEquals, stmt.Operator);
    }

    [Fact]
    public void Parse_IfStatement_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    if (Position.x > 0) {
                        Position.x = 0;
                    } else {
                        Position.x = 1;
                    }
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var ifStmt = Assert.IsType<IfStatement>(compute.Execute.Body[0]);

        Assert.Single(ifStmt.ThenBranch);
        Assert.NotNull(ifStmt.ElseBranch);
        Assert.Single(ifStmt.ElseBranch);
    }

    [Fact]
    public void Parse_ForStatement_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    for (i: 0..10) {
                        Position.x = Position.x + 1;
                    }
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var forStmt = Assert.IsType<ForStatement>(compute.Execute.Body[0]);

        Assert.Equal("i", forStmt.VariableName);
        Assert.IsType<IntLiteralExpression>(forStmt.Start);
        Assert.IsType<IntLiteralExpression>(forStmt.End);
    }

    [Fact]
    public void Parse_BinaryExpression_ParsesWithCorrectPrecedence()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = 1 + 2 * 3;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var binary = Assert.IsType<BinaryExpression>(assignStmt.Value);

        // Should be parsed as 1 + (2 * 3)
        Assert.Equal(BinaryOperator.Add, binary.Operator);
        Assert.IsType<IntLiteralExpression>(binary.Left);
        var right = Assert.IsType<BinaryExpression>(binary.Right);
        Assert.Equal(BinaryOperator.Multiply, right.Operator);
    }

    [Fact]
    public void Parse_UnaryExpression_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = -Position.y;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var unary = Assert.IsType<UnaryExpression>(assignStmt.Value);

        Assert.Equal(UnaryOperator.Negate, unary.Operator);
    }

    [Fact]
    public void Parse_FunctionCall_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = sqrt(Position.y);
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);
        var call = Assert.IsType<CallExpression>(assignStmt.Value);

        Assert.Equal("sqrt", call.FunctionName);
        Assert.Single(call.Arguments);
    }

    [Fact]
    public void Parse_MemberAccess_ParsesCorrectly()
    {
        var source = @"
            compute Test {
                query { write Position }
                execute() {
                    Position.x = Position.y;
                }
            }
        ";
        var result = Parse(source);

        var compute = Assert.IsType<ComputeDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(compute.Execute.Body[0]);

        var target = Assert.IsType<MemberAccessExpression>(assignStmt.Target);
        Assert.Equal("x", target.MemberName);

        var value = Assert.IsType<MemberAccessExpression>(assignStmt.Value);
        Assert.Equal("y", value.MemberName);
    }

    #region Vertex Shader Tests

    [Fact]
    public void Parse_VertexShader_ParsesInputBlock()
    {
        var source = @"
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
                }
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);
        Assert.Equal("TransformVertex", vertex.Name);

        Assert.Equal(3, vertex.Inputs.Attributes.Count);
        Assert.Equal("position", vertex.Inputs.Attributes[0].Name);
        Assert.Equal(0, vertex.Inputs.Attributes[0].LocationIndex);
        Assert.Equal("normal", vertex.Inputs.Attributes[1].Name);
        Assert.Equal(1, vertex.Inputs.Attributes[1].LocationIndex);
        Assert.Equal("texCoord", vertex.Inputs.Attributes[2].Name);
        Assert.Equal(2, vertex.Inputs.Attributes[2].LocationIndex);
    }

    [Fact]
    public void Parse_VertexShader_ParsesOutputBlock()
    {
        var source = @"
            vertex TransformVertex {
                in {
                    position: float3 @ 0
                }
                out {
                    worldPos: float3
                    worldNormal: float3
                    uv: float2
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);

        Assert.Equal(3, vertex.Outputs.Attributes.Count);
        Assert.Equal("worldPos", vertex.Outputs.Attributes[0].Name);
        Assert.Equal("worldNormal", vertex.Outputs.Attributes[1].Name);
        Assert.Equal("uv", vertex.Outputs.Attributes[2].Name);
        // Output attributes without location binding
        Assert.Null(vertex.Outputs.Attributes[0].LocationIndex);
    }

    [Fact]
    public void Parse_VertexShader_ParsesParamsBlock()
    {
        var source = @"
            vertex TransformVertex {
                in {
                    position: float3 @ 0
                }
                out {
                    clipPos: float4
                }
                params {
                    model: mat4
                    view: mat4
                    projection: mat4
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);

        Assert.NotNull(vertex.Params);
        Assert.Equal(3, vertex.Params.Parameters.Count);
        Assert.Equal("model", vertex.Params.Parameters[0].Name);
        Assert.Equal("view", vertex.Params.Parameters[1].Name);
        Assert.Equal("projection", vertex.Params.Parameters[2].Name);
    }

    [Fact]
    public void Parse_VertexShader_ParsesExecuteBlock()
    {
        var source = @"
            vertex TransformVertex {
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
        var result = Parse(source);

        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);
        Assert.Single(vertex.Execute.Body);

        var stmt = Assert.IsType<AssignmentStatement>(vertex.Execute.Body[0]);
        var target = Assert.IsType<IdentifierExpression>(stmt.Target);
        Assert.Equal("outPos", target.Name);
    }

    [Fact]
    public void Parse_VertexShader_AttributeWithoutLocation()
    {
        var source = @"
            vertex SimpleVertex {
                in {
                    position: float3
                }
                out {
                    outColor: float4 @ 0
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);

        // Input without location
        Assert.Null(vertex.Inputs.Attributes[0].LocationIndex);
        // Output with location
        Assert.Equal(0, vertex.Outputs.Attributes[0].LocationIndex);
    }

    #endregion

    #region Fragment Shader Tests

    [Fact]
    public void Parse_FragmentShader_ParsesCorrectly()
    {
        var source = @"
            fragment LitSurface {
                in {
                    worldPos: float3
                    worldNormal: float3
                    uv: float2
                }
                out {
                    fragColor: float4 @ 0
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.Equal("LitSurface", fragment.Name);

        Assert.Equal(3, fragment.Inputs.Attributes.Count);
        Assert.Single(fragment.Outputs.Attributes);
        Assert.Equal("fragColor", fragment.Outputs.Attributes[0].Name);
        Assert.Equal(0, fragment.Outputs.Attributes[0].LocationIndex);
    }

    [Fact]
    public void Parse_FragmentShader_WithParams()
    {
        var source = @"
            fragment LitSurface {
                in {
                    worldNormal: float3
                }
                out {
                    fragColor: float4 @ 0
                }
                params {
                    lightDir: float3
                    lightColor: float3
                    ambientColor: float3
                }
                execute() {
                    fragColor = lightColor;
                }
            }
        ";
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);

        Assert.NotNull(fragment.Params);
        Assert.Equal(3, fragment.Params.Parameters.Count);
        Assert.Equal("lightDir", fragment.Params.Parameters[0].Name);
    }

    [Fact]
    public void Parse_FragmentShader_WithExecuteLogic()
    {
        var source = @"
            fragment SimpleColor {
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.Single(fragment.Execute.Body);

        var stmt = Assert.IsType<AssignmentStatement>(fragment.Execute.Body[0]);
        var target = Assert.IsType<IdentifierExpression>(stmt.Target);
        Assert.Equal("fragColor", target.Name);
    }

    #endregion

    #region Texture and Sampler Tests

    [Fact]
    public void Parse_FragmentShader_WithTexturesBlock()
    {
        var source = @"
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.NotNull(fragment.Textures);
        Assert.Equal(2, fragment.Textures.Textures.Count);

        Assert.Equal("diffuseMap", fragment.Textures.Textures[0].Name);
        Assert.Equal(TextureKind.Texture2D, fragment.Textures.Textures[0].TextureKind);
        Assert.Equal(0, fragment.Textures.Textures[0].BindingSlot);

        Assert.Equal("normalMap", fragment.Textures.Textures[1].Name);
        Assert.Equal(TextureKind.Texture2D, fragment.Textures.Textures[1].TextureKind);
        Assert.Equal(1, fragment.Textures.Textures[1].BindingSlot);
    }

    [Fact]
    public void Parse_FragmentShader_WithSamplersBlock()
    {
        var source = @"
            fragment SampledSurface {
                in {
                    uv: float2
                }
                out {
                    fragColor: float4 @ 0
                }
                samplers {
                    linearSampler: sampler @ 0
                    pointSampler: sampler @ 1
                }
                execute() {
                    fragColor = uv;
                }
            }
        ";
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.NotNull(fragment.Samplers);
        Assert.Equal(2, fragment.Samplers.Samplers.Count);

        Assert.Equal("linearSampler", fragment.Samplers.Samplers[0].Name);
        Assert.Equal(0, fragment.Samplers.Samplers[0].BindingSlot);

        Assert.Equal("pointSampler", fragment.Samplers.Samplers[1].Name);
        Assert.Equal(1, fragment.Samplers.Samplers[1].BindingSlot);
    }

    [Fact]
    public void Parse_FragmentShader_WithTexturesAndSamplers()
    {
        var source = @"
            fragment FullTexturedSurface {
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.NotNull(fragment.Textures);
        Assert.NotNull(fragment.Samplers);
        Assert.Single(fragment.Textures.Textures);
        Assert.Single(fragment.Samplers.Samplers);
    }

    [Fact]
    public void Parse_VertexShader_WithTexturesBlock()
    {
        var source = @"
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
        var result = Parse(source);

        var vertex = Assert.IsType<VertexDeclaration>(result.Declarations[0]);
        Assert.NotNull(vertex.Textures);
        Assert.Single(vertex.Textures.Textures);
        Assert.Equal("heightMap", vertex.Textures.Textures[0].Name);
    }

    [Fact]
    public void Parse_TextureCubeType_ParsesCorrectly()
    {
        var source = @"
            fragment SkyboxFragment {
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.NotNull(fragment.Textures);
        Assert.Equal(TextureKind.TextureCube, fragment.Textures.Textures[0].TextureKind);
    }

    [Fact]
    public void Parse_Texture3DType_ParsesCorrectly()
    {
        var source = @"
            fragment VolumeFragment {
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        Assert.NotNull(fragment.Textures);
        Assert.Equal(TextureKind.Texture3D, fragment.Textures.Textures[0].TextureKind);
    }

    [Fact]
    public void Parse_SampleFunctionCall_ParsesCorrectly()
    {
        var source = @"
            fragment TexturedFragment {
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
        var result = Parse(source);

        var fragment = Assert.IsType<FragmentDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(fragment.Execute.Body[0]);
        var call = Assert.IsType<CallExpression>(assignStmt.Value);

        Assert.Equal("sample", call.FunctionName);
        Assert.Equal(3, call.Arguments.Count);
    }

    #endregion

    #region Mixed Declarations Tests

    [Fact]
    public void Parse_MultipleDeclarationTypes_ParsesCorrectly()
    {
        var source = @"
            component Position {
                x: float
                y: float
                z: float
            }

            vertex TransformVertex {
                in {
                    pos: float3 @ 0
                }
                out {
                    outPos: float3
                }
                execute() {
                }
            }

            fragment ColorOutput {
                in {
                    color: float4
                }
                out {
                    fragColor: float4 @ 0
                }
                execute() {
                }
            }

            compute UpdatePhysics {
                query {
                    write Position
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        Assert.Equal(4, result.Declarations.Count);
        Assert.IsType<ComponentDeclaration>(result.Declarations[0]);
        Assert.IsType<VertexDeclaration>(result.Declarations[1]);
        Assert.IsType<FragmentDeclaration>(result.Declarations[2]);
        Assert.IsType<ComputeDeclaration>(result.Declarations[3]);
    }

    #endregion

    #region Geometry Shader Tests

    [Fact]
    public void Parse_GeometryShader_ParsesLayoutBlock()
    {
        var source = @"
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
                }
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Equal("WireframeExpander", geometry.Name);

        Assert.Equal(GeometryInputTopology.Triangles, geometry.Layout.InputTopology);
        Assert.Equal(GeometryOutputTopology.LineStrip, geometry.Layout.OutputTopology);
        Assert.Equal(6, geometry.Layout.MaxVertices);
    }

    [Fact]
    public void Parse_GeometryShader_ParsesInputBlock()
    {
        var source = @"
            geometry SimpleGeom {
                layout {
                    input: points
                    output: triangle_strip
                    max_vertices: 4
                }
                in {
                    position: float3
                    normal: float3
                    texCoord: float2
                }
                out {
                    color: float4
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Equal(3, geometry.Inputs.Attributes.Count);
        Assert.Equal("position", geometry.Inputs.Attributes[0].Name);
        Assert.Equal("normal", geometry.Inputs.Attributes[1].Name);
        Assert.Equal("texCoord", geometry.Inputs.Attributes[2].Name);
    }

    [Fact]
    public void Parse_GeometryShader_ParsesOutputBlock()
    {
        var source = @"
            geometry MultiOutput {
                layout {
                    input: lines
                    output: line_strip
                    max_vertices: 2
                }
                in {
                    position: float3
                }
                out {
                    worldPos: float3
                    worldNormal: float3
                    color: float4
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Equal(3, geometry.Outputs.Attributes.Count);
        Assert.Equal("worldPos", geometry.Outputs.Attributes[0].Name);
        Assert.Equal("worldNormal", geometry.Outputs.Attributes[1].Name);
        Assert.Equal("color", geometry.Outputs.Attributes[2].Name);
    }

    [Fact]
    public void Parse_GeometryShader_ParsesParamsBlock()
    {
        var source = @"
            geometry ParamGeom {
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
                params {
                    wireColor: float4
                    wireWidth: float
                }
                execute() {
                }
            }
        ";
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.NotNull(geometry.Params);
        Assert.Equal(2, geometry.Params.Parameters.Count);
        Assert.Equal("wireColor", geometry.Params.Parameters[0].Name);
        Assert.Equal("wireWidth", geometry.Params.Parameters[1].Name);
    }

    [Fact]
    public void Parse_GeometryShader_AllInputTopologies()
    {
        var topologies = new[]
        {
            ("points", GeometryInputTopology.Points),
            ("lines", GeometryInputTopology.Lines),
            ("lines_adjacency", GeometryInputTopology.LinesAdjacency),
            ("triangles", GeometryInputTopology.Triangles),
            ("triangles_adjacency", GeometryInputTopology.TrianglesAdjacency)
        };

        foreach (var (name, expected) in topologies)
        {
            var source = $@"
                geometry Test {{
                    layout {{
                        input: {name}
                        output: points
                        max_vertices: 1
                    }}
                    in {{ position: float3 }}
                    out {{ color: float4 }}
                    execute() {{}}
                }}
            ";
            var result = Parse(source);
            var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
            Assert.Equal(expected, geometry.Layout.InputTopology);
        }
    }

    [Fact]
    public void Parse_GeometryShader_AllOutputTopologies()
    {
        var topologies = new[]
        {
            ("points", GeometryOutputTopology.Points),
            ("line_strip", GeometryOutputTopology.LineStrip),
            ("triangle_strip", GeometryOutputTopology.TriangleStrip)
        };

        foreach (var (name, expected) in topologies)
        {
            var source = $@"
                geometry Test {{
                    layout {{
                        input: triangles
                        output: {name}
                        max_vertices: 1
                    }}
                    in {{ position: float3 }}
                    out {{ color: float4 }}
                    execute() {{}}
                }}
            ";
            var result = Parse(source);
            var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
            Assert.Equal(expected, geometry.Layout.OutputTopology);
        }
    }

    [Fact]
    public void Parse_GeometryShader_EmitStatement()
    {
        var source = @"
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
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Single(geometry.Execute.Body);
        var emitStmt = Assert.IsType<EmitStatement>(geometry.Execute.Body[0]);
        Assert.IsType<IdentifierExpression>(emitStmt.Position);
    }

    [Fact]
    public void Parse_GeometryShader_EndPrimitiveStatement()
    {
        var source = @"
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
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Equal(2, geometry.Execute.Body.Count);
        Assert.IsType<EmitStatement>(geometry.Execute.Body[0]);
        Assert.IsType<EndPrimitiveStatement>(geometry.Execute.Body[1]);
    }

    [Fact]
    public void Parse_GeometryShader_ArrayIndexExpression()
    {
        var source = @"
            geometry IndexTest {
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
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        var assignStmt = Assert.IsType<AssignmentStatement>(geometry.Execute.Body[0]);
        var memberAccess = Assert.IsType<MemberAccessExpression>(assignStmt.Value);
        var indexExpr = Assert.IsType<IndexExpression>(memberAccess.Object);
        Assert.IsType<IdentifierExpression>(indexExpr.Array);
        Assert.IsType<IntLiteralExpression>(indexExpr.Index);
    }

    [Fact]
    public void Parse_GeometryShader_ForLoopWithArrayAccess()
    {
        var source = @"
            geometry LoopTest {
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
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.Equal(2, geometry.Execute.Body.Count);
        var forStmt = Assert.IsType<ForStatement>(geometry.Execute.Body[0]);
        Assert.Equal("i", forStmt.VariableName);
        Assert.Single(forStmt.Body);
    }

    [Fact]
    public void Parse_GeometryShader_WithTexturesAndSamplers()
    {
        var source = @"
            geometry TexturedGeom {
                layout {
                    input: triangles
                    output: triangle_strip
                    max_vertices: 3
                }
                in {
                    position: float3
                    uv: float2
                }
                out {
                    outPos: float3
                }
                textures {
                    heightMap: texture2D @ 0
                }
                samplers {
                    linearSampler: sampler @ 0
                }
                execute() {
                    outPos = position;
                }
            }
        ";
        var result = Parse(source);

        var geometry = Assert.IsType<GeometryDeclaration>(result.Declarations[0]);
        Assert.NotNull(geometry.Textures);
        Assert.NotNull(geometry.Samplers);
        Assert.Single(geometry.Textures.Textures);
        Assert.Single(geometry.Samplers.Samplers);
    }

    #endregion

    #region Pipeline Tests

    [Fact]
    public void Parse_Pipeline_WithReferenceStages()
    {
        var source = @"
            pipeline SimplePipeline {
                vertex: MyVertexShader
                fragment: MyFragmentShader
            }
        ";
        var result = Parse(source);

        Assert.Single(result.Declarations);
        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.Equal("SimplePipeline", pipeline.Name);
        Assert.NotNull(pipeline.Vertex);
        Assert.Equal("MyVertexShader", pipeline.Vertex.ReferenceName);
        Assert.Null(pipeline.Vertex.InlineShader);
        Assert.NotNull(pipeline.Fragment);
        Assert.Equal("MyFragmentShader", pipeline.Fragment.ReferenceName);
        Assert.Null(pipeline.Fragment.InlineShader);
        Assert.Null(pipeline.Geometry);
    }

    [Fact]
    public void Parse_Pipeline_WithGeometryStage()
    {
        var source = @"
            pipeline GeomPipeline {
                vertex: TransformVertex
                geometry: WireframeExpander
                fragment: LitSurface
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Vertex);
        Assert.NotNull(pipeline.Geometry);
        Assert.NotNull(pipeline.Fragment);
        Assert.Equal("TransformVertex", pipeline.Vertex.ReferenceName);
        Assert.Equal("WireframeExpander", pipeline.Geometry.ReferenceName);
        Assert.Equal("LitSurface", pipeline.Fragment.ReferenceName);
    }

    [Fact]
    public void Parse_Pipeline_WithInlineVertexShader()
    {
        var source = @"
            pipeline InlineVertex {
                vertex {
                    in {
                        position: float3 @ 0
                    }
                    out {
                        worldPos: float3
                    }
                    execute() {
                        worldPos = position;
                    }
                }
                fragment: DefaultFragment
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Vertex);
        Assert.Null(pipeline.Vertex.ReferenceName);
        Assert.NotNull(pipeline.Vertex.InlineShader);

        var inlineVertex = Assert.IsType<VertexDeclaration>(pipeline.Vertex.InlineShader);
        Assert.Equal("InlineVertex_vertex", inlineVertex.Name);
        Assert.Single(inlineVertex.Inputs.Attributes);
        Assert.Single(inlineVertex.Outputs.Attributes);
    }

    [Fact]
    public void Parse_Pipeline_WithInlineFragmentShader()
    {
        var source = @"
            pipeline InlineFragment {
                vertex: PassThrough
                fragment {
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
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Fragment);
        Assert.Null(pipeline.Fragment.ReferenceName);
        Assert.NotNull(pipeline.Fragment.InlineShader);

        var inlineFragment = Assert.IsType<FragmentDeclaration>(pipeline.Fragment.InlineShader);
        Assert.Equal("InlineFragment_fragment", inlineFragment.Name);
    }

    [Fact]
    public void Parse_Pipeline_WithInlineGeometryShader()
    {
        var source = @"
            pipeline InlineGeom {
                vertex: BaseVertex
                geometry {
                    layout {
                        input: triangles
                        output: line_strip
                        max_vertices: 6
                    }
                    in {
                        position: float3
                        inColor: float4
                    }
                    out {
                        color: float4
                    }
                    execute() {
                        color = inColor;
                    }
                }
                fragment: WireframeFragment
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Geometry);
        Assert.Null(pipeline.Geometry.ReferenceName);
        Assert.NotNull(pipeline.Geometry.InlineShader);

        var inlineGeometry = Assert.IsType<GeometryDeclaration>(pipeline.Geometry.InlineShader);
        Assert.Equal("InlineGeom_geometry", inlineGeometry.Name);
        Assert.Equal(GeometryInputTopology.Triangles, inlineGeometry.Layout.InputTopology);
    }

    [Fact]
    public void Parse_Pipeline_AllInlineShaders()
    {
        var source = @"
            pipeline FullInline {
                vertex {
                    in {
                        pos: float3 @ 0
                    }
                    out {
                        outPos: float3
                    }
                    execute() {
                        outPos = pos;
                    }
                }
                fragment {
                    in {
                        inColor: float4
                    }
                    out {
                        color: float4 @ 0
                    }
                    execute() {
                        color = inColor;
                    }
                }
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Vertex);
        Assert.NotNull(pipeline.Fragment);
        Assert.Null(pipeline.Geometry);

        Assert.IsType<VertexDeclaration>(pipeline.Vertex.InlineShader);
        Assert.IsType<FragmentDeclaration>(pipeline.Fragment.InlineShader);
    }

    [Fact]
    public void Parse_Pipeline_MixedReferenceAndInline()
    {
        var source = @"
            pipeline MixedPipeline {
                vertex: ExistingVertex
                fragment {
                    in {
                        inColor: float4
                    }
                    out {
                        fragColor: float4 @ 0
                    }
                    execute() {
                        fragColor = inColor;
                    }
                }
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        Assert.NotNull(pipeline.Vertex);
        Assert.Equal("ExistingVertex", pipeline.Vertex.ReferenceName);
        Assert.NotNull(pipeline.Fragment);
        Assert.Null(pipeline.Fragment.ReferenceName);
        Assert.NotNull(pipeline.Fragment.InlineShader);
    }

    [Fact]
    public void Parse_Pipeline_WithTexturesAndSamplers()
    {
        var source = @"
            pipeline TexturedPipeline {
                vertex: SimpleVertex
                fragment {
                    in {
                        inColor: float4
                    }
                    out {
                        fragColor: float4 @ 0
                    }
                    textures {
                        diffuse: texture2D @ 0
                    }
                    samplers {
                        linearSampler: sampler @ 0
                    }
                    execute() {
                        fragColor = inColor;
                    }
                }
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        var fragment = Assert.IsType<FragmentDeclaration>(pipeline.Fragment!.InlineShader);
        Assert.NotNull(fragment.Textures);
        Assert.NotNull(fragment.Samplers);
        Assert.Single(fragment.Textures.Textures);
        Assert.Single(fragment.Samplers.Samplers);
    }

    [Fact]
    public void Parse_Pipeline_WithParams()
    {
        var source = @"
            pipeline ParamsPipeline {
                vertex {
                    in {
                        position: float3 @ 0
                    }
                    out {
                        outPos: float3
                    }
                    params {
                        mvp: mat4
                    }
                    execute() {
                        outPos = position;
                    }
                }
                fragment: DefaultFrag
            }
        ";
        var result = Parse(source);

        var pipeline = Assert.IsType<PipelineDeclaration>(result.Declarations[0]);
        var vertex = Assert.IsType<VertexDeclaration>(pipeline.Vertex!.InlineShader);
        Assert.NotNull(vertex.Params);
        Assert.Single(vertex.Params.Parameters);
        Assert.Equal("mvp", vertex.Params.Parameters[0].Name);
    }

    #endregion
}
