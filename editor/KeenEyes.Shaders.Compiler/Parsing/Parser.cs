using KeenEyes.Shaders.Compiler.Diagnostics;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.Parsing;

/// <summary>
/// Recursive descent parser for KESL (KeenEyes Shader Language).
/// </summary>
public sealed class Parser
{
    private readonly List<Token> _tokens;
    private readonly List<CompilerError> _errors;
    private int _current;

    /// <summary>
    /// Creates a new parser for the given tokens.
    /// </summary>
    /// <param name="tokens">The tokens to parse.</param>
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _errors = [];
        _current = 0;
    }

    /// <summary>
    /// Gets the parse errors encountered during parsing.
    /// </summary>
    public IReadOnlyList<CompilerError> Errors => _errors;

    /// <summary>
    /// Gets whether the parser encountered any errors.
    /// </summary>
    public bool HasErrors => _errors.Count > 0;

    /// <summary>
    /// Parses the token stream into a source file AST.
    /// </summary>
    public SourceFile Parse()
    {
        var declarations = new List<Declaration>();
        var startLocation = Current.Location;

        while (!IsAtEnd())
        {
            try
            {
                var decl = ParseDeclaration();
                if (decl != null)
                {
                    declarations.Add(decl);
                }
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        return new SourceFile(declarations, startLocation);
    }

    private Declaration? ParseDeclaration()
    {
        if (Check(TokenKind.Component))
        {
            return ParseComponentDeclaration();
        }

        if (Check(TokenKind.Compute))
        {
            return ParseComputeDeclaration();
        }

        if (Check(TokenKind.Vertex))
        {
            return ParseVertexDeclaration();
        }

        if (Check(TokenKind.Fragment))
        {
            return ParseFragmentDeclaration();
        }

        if (Check(TokenKind.Geometry))
        {
            return ParseGeometryDeclaration();
        }

        if (Check(TokenKind.Pipeline))
        {
            return ParsePipelineDeclaration();
        }

        // Throw to trigger synchronization - otherwise we'd loop infinitely
        throw Error(Current, "Expected 'component', 'compute', 'vertex', 'fragment', 'geometry', or 'pipeline' declaration", KeslErrorCodes.ExpectedDeclaration);
    }

    private ComponentDeclaration ParseComponentDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Component, "Expected 'component'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected component name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after component name", KeslErrorCodes.ExpectedOpenBrace);

        var fields = new List<FieldDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            fields.Add(ParseFieldDeclaration());

            // Allow optional comma or newline separation
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after component fields", KeslErrorCodes.ExpectedCloseBrace);

        return new ComponentDeclaration(name, fields, location);
    }

    private FieldDeclaration ParseFieldDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected field name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after field name", KeslErrorCodes.MissingToken);
        var type = ParseType();

        return new FieldDeclaration(name, type, location);
    }

    private ComputeDeclaration ParseComputeDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Compute, "Expected 'compute'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected shader name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after shader name", KeslErrorCodes.ExpectedOpenBrace);

        // Parse query block (required)
        var query = ParseQueryBlock();

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        // Parse execute block (required)
        var execute = ParseExecuteBlock();

        Consume(TokenKind.RightBrace, "Expected '}' after shader body", KeslErrorCodes.ExpectedCloseBrace);

        return new ComputeDeclaration(name, query, paramsBlock, execute, location);
    }

    private VertexDeclaration ParseVertexDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Vertex, "Expected 'vertex'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected shader name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after shader name", KeslErrorCodes.ExpectedOpenBrace);

        // Parse input block (required)
        var inputs = ParseInputBlock();

        // Parse output block (required)
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        // Parse execute block (required)
        var execute = ParseExecuteBlock();

        Consume(TokenKind.RightBrace, "Expected '}' after shader body", KeslErrorCodes.ExpectedCloseBrace);

        return new VertexDeclaration(name, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private FragmentDeclaration ParseFragmentDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Fragment, "Expected 'fragment'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected shader name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after shader name", KeslErrorCodes.ExpectedOpenBrace);

        // Parse input block (required)
        var inputs = ParseInputBlock();

        // Parse output block (required)
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        // Parse execute block (required)
        var execute = ParseExecuteBlock();

        Consume(TokenKind.RightBrace, "Expected '}' after shader body", KeslErrorCodes.ExpectedCloseBrace);

        return new FragmentDeclaration(name, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private GeometryDeclaration ParseGeometryDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Geometry, "Expected 'geometry'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected shader name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after shader name", KeslErrorCodes.ExpectedOpenBrace);

        // Parse layout block (required for geometry shaders)
        var layout = ParseGeometryLayoutBlock();

        // Parse input block (required)
        var inputs = ParseInputBlock();

        // Parse output block (required)
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        // Parse execute block (required)
        var execute = ParseExecuteBlock();

        Consume(TokenKind.RightBrace, "Expected '}' after shader body", KeslErrorCodes.ExpectedCloseBrace);

        return new GeometryDeclaration(name, layout, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private GeometryLayoutBlock ParseGeometryLayoutBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Layout, "Expected 'layout'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'layout'", KeslErrorCodes.ExpectedOpenBrace);

        // Parse input topology
        Consume(TokenKind.Input, "Expected 'input' in layout block", KeslErrorCodes.MissingToken);
        Consume(TokenKind.Colon, "Expected ':' after 'input'", KeslErrorCodes.MissingToken);
        var inputTopology = ParseInputTopology();

        // Parse output topology
        Consume(TokenKind.Output, "Expected 'output' in layout block", KeslErrorCodes.MissingToken);
        Consume(TokenKind.Colon, "Expected ':' after 'output'", KeslErrorCodes.MissingToken);
        var outputTopology = ParseOutputTopology();

        // Parse max_vertices
        Consume(TokenKind.MaxVertices, "Expected 'max_vertices' in layout block", KeslErrorCodes.MissingToken);
        Consume(TokenKind.Colon, "Expected ':' after 'max_vertices'", KeslErrorCodes.MissingToken);
        var maxVerticesToken = Consume(TokenKind.IntLiteral, "Expected integer for max_vertices", KeslErrorCodes.MissingToken);
        var maxVertices = maxVerticesToken.IntValue;

        Consume(TokenKind.RightBrace, "Expected '}' after layout block", KeslErrorCodes.ExpectedCloseBrace);

        return new GeometryLayoutBlock(inputTopology, outputTopology, maxVertices, location);
    }

    private GeometryInputTopology ParseInputTopology()
    {
        var topologyName = Consume(TokenKind.Identifier, "Expected topology name", KeslErrorCodes.ExpectedTopology).Text;
        return topologyName.ToLowerInvariant() switch
        {
            "points" => GeometryInputTopology.Points,
            "lines" => GeometryInputTopology.Lines,
            "lines_adjacency" => GeometryInputTopology.LinesAdjacency,
            "triangles" => GeometryInputTopology.Triangles,
            "triangles_adjacency" => GeometryInputTopology.TrianglesAdjacency,
            _ => throw Error(Previous, $"Unknown input topology '{topologyName}'. Expected 'points', 'lines', 'lines_adjacency', 'triangles', or 'triangles_adjacency'", KeslErrorCodes.ExpectedTopology)
        };
    }

    private GeometryOutputTopology ParseOutputTopology()
    {
        var topologyName = Consume(TokenKind.Identifier, "Expected topology name", KeslErrorCodes.ExpectedTopology).Text;
        return topologyName.ToLowerInvariant() switch
        {
            "points" => GeometryOutputTopology.Points,
            "line_strip" => GeometryOutputTopology.LineStrip,
            "triangle_strip" => GeometryOutputTopology.TriangleStrip,
            _ => throw Error(Previous, $"Unknown output topology '{topologyName}'. Expected 'points', 'line_strip', or 'triangle_strip'", KeslErrorCodes.ExpectedTopology)
        };
    }

    private PipelineDeclaration ParsePipelineDeclaration()
    {
        var location = Current.Location;
        Consume(TokenKind.Pipeline, "Expected 'pipeline'", KeslErrorCodes.MissingToken);
        var name = Consume(TokenKind.Identifier, "Expected pipeline name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.LeftBrace, "Expected '{' after pipeline name", KeslErrorCodes.ExpectedOpenBrace);

        PipelineStage? vertexStage = null;
        PipelineStage? geometryStage = null;
        PipelineStage? fragmentStage = null;

        // Parse stages in any order
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            if (Check(TokenKind.Vertex))
            {
                vertexStage = ParsePipelineStage(name, TokenKind.Vertex);
            }
            else if (Check(TokenKind.Geometry))
            {
                geometryStage = ParsePipelineStage(name, TokenKind.Geometry);
            }
            else if (Check(TokenKind.Fragment))
            {
                fragmentStage = ParsePipelineStage(name, TokenKind.Fragment);
            }
            else
            {
                throw Error(Current, "Expected 'vertex', 'geometry', or 'fragment' stage in pipeline", KeslErrorCodes.ExpectedDeclaration);
            }
        }

        Consume(TokenKind.RightBrace, "Expected '}' after pipeline body", KeslErrorCodes.ExpectedCloseBrace);

        return new PipelineDeclaration(name, vertexStage, geometryStage, fragmentStage, location);
    }

    private PipelineStage ParsePipelineStage(string pipelineName, TokenKind stageKind)
    {
        var location = Current.Location;
        Consume(stageKind, $"Expected '{stageKind.ToString().ToLowerInvariant()}'", KeslErrorCodes.MissingToken);

        // Check if this is a reference (name: SomeShader) or inline definition (block { ... })
        if (Check(TokenKind.Colon))
        {
            // Reference syntax: vertex: ShaderName
            Consume(TokenKind.Colon, "Expected ':'", KeslErrorCodes.MissingToken);
            var referenceName = Consume(TokenKind.Identifier, "Expected shader name", KeslErrorCodes.ExpectedIdentifier).Text;
            return new PipelineStage(referenceName, null, location);
        }
        else if (Check(TokenKind.LeftBrace))
        {
            // Inline definition syntax: vertex { in { ... } out { ... } execute() { ... } }
            // Generate inline shader name as PipelineName_stage (e.g., MyPipeline_vertex)
            var stageName = stageKind.ToString().ToLowerInvariant();
            var inlineShaderName = $"{pipelineName}_{stageName}";

            Declaration inlineShader = stageKind switch
            {
                TokenKind.Vertex => ParseInlineVertexShader(inlineShaderName, location),
                TokenKind.Geometry => ParseInlineGeometryShader(inlineShaderName, location),
                TokenKind.Fragment => ParseInlineFragmentShader(inlineShaderName, location),
                _ => throw Error(Current, $"Unexpected stage kind: {stageKind}", KeslErrorCodes.UnexpectedToken)
            };
            return new PipelineStage(null, inlineShader, location);
        }
        else
        {
            throw Error(Current, "Expected ':' for shader reference or '{' for inline definition", KeslErrorCodes.UnexpectedToken);
        }
    }

    private VertexDeclaration ParseInlineVertexShader(string shaderName, SourceLocation location)
    {
        Consume(TokenKind.LeftBrace, "Expected '{'", KeslErrorCodes.ExpectedOpenBrace);

        var inputs = ParseInputBlock();
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        var execute = ParseExecuteBlock();
        Consume(TokenKind.RightBrace, "Expected '}'", KeslErrorCodes.ExpectedCloseBrace);

        return new VertexDeclaration(shaderName, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private GeometryDeclaration ParseInlineGeometryShader(string shaderName, SourceLocation location)
    {
        Consume(TokenKind.LeftBrace, "Expected '{'", KeslErrorCodes.ExpectedOpenBrace);

        var layout = ParseGeometryLayoutBlock();
        var inputs = ParseInputBlock();
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        var execute = ParseExecuteBlock();
        Consume(TokenKind.RightBrace, "Expected '}'", KeslErrorCodes.ExpectedCloseBrace);

        return new GeometryDeclaration(shaderName, layout, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private FragmentDeclaration ParseInlineFragmentShader(string shaderName, SourceLocation location)
    {
        Consume(TokenKind.LeftBrace, "Expected '{'", KeslErrorCodes.ExpectedOpenBrace);

        var inputs = ParseInputBlock();
        var outputs = ParseOutputBlock();

        // Parse optional textures block
        TexturesBlock? texturesBlock = null;
        if (Check(TokenKind.Textures))
        {
            texturesBlock = ParseTexturesBlock();
        }

        // Parse optional samplers block
        SamplersBlock? samplersBlock = null;
        if (Check(TokenKind.Samplers))
        {
            samplersBlock = ParseSamplersBlock();
        }

        // Parse optional params block
        ParamsBlock? paramsBlock = null;
        if (Check(TokenKind.Params))
        {
            paramsBlock = ParseParamsBlock();
        }

        var execute = ParseExecuteBlock();
        Consume(TokenKind.RightBrace, "Expected '}'", KeslErrorCodes.ExpectedCloseBrace);

        return new FragmentDeclaration(shaderName, inputs, outputs, texturesBlock, samplersBlock, paramsBlock, execute, location);
    }

    private InputBlock ParseInputBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.In, "Expected 'in'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'in'", KeslErrorCodes.ExpectedOpenBrace);

        var attributes = new List<AttributeDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            attributes.Add(ParseAttributeDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after input attributes", KeslErrorCodes.ExpectedCloseBrace);

        return new InputBlock(attributes, location);
    }

    private OutputBlock ParseOutputBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Out, "Expected 'out'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'out'", KeslErrorCodes.ExpectedOpenBrace);

        var attributes = new List<AttributeDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            attributes.Add(ParseAttributeDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after output attributes", KeslErrorCodes.ExpectedCloseBrace);

        return new OutputBlock(attributes, location);
    }

    private AttributeDeclaration ParseAttributeDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected attribute name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after attribute name", KeslErrorCodes.MissingToken);
        var type = ParseType();

        // Optional location binding (@ 0)
        int? locationIndex = null;
        if (Match(TokenKind.At))
        {
            var locToken = Consume(TokenKind.IntLiteral, "Expected location index after '@'", KeslErrorCodes.MissingToken);
            locationIndex = locToken.IntValue;
        }

        return new AttributeDeclaration(name, type, locationIndex, location);
    }

    private QueryBlock ParseQueryBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Query, "Expected 'query'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'query'", KeslErrorCodes.ExpectedOpenBrace);

        var bindings = new List<QueryBinding>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            bindings.Add(ParseQueryBinding());
        }

        Consume(TokenKind.RightBrace, "Expected '}' after query bindings", KeslErrorCodes.ExpectedCloseBrace);

        return new QueryBlock(bindings, location);
    }

    private QueryBinding ParseQueryBinding()
    {
        var location = Current.Location;

        var accessMode = Current.Kind switch
        {
            TokenKind.Read => AccessMode.Read,
            TokenKind.Write => AccessMode.Write,
            TokenKind.Optional => AccessMode.Optional,
            TokenKind.Without => AccessMode.Without,
            _ => throw Error(Current, "Expected 'read', 'write', 'optional', or 'without'", KeslErrorCodes.ExpectedBindingMode)
        };
        Advance();

        var componentName = Consume(TokenKind.Identifier, "Expected component name", KeslErrorCodes.ExpectedIdentifier).Text;

        return new QueryBinding(accessMode, componentName, location);
    }

    private ParamsBlock ParseParamsBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Params, "Expected 'params'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'params'", KeslErrorCodes.ExpectedOpenBrace);

        var parameters = new List<ParamDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            parameters.Add(ParseParamDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after parameters", KeslErrorCodes.ExpectedCloseBrace);

        return new ParamsBlock(parameters, location);
    }

    private ParamDeclaration ParseParamDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected parameter name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after parameter name", KeslErrorCodes.MissingToken);
        var type = ParseType();

        return new ParamDeclaration(name, type, location);
    }

    private TexturesBlock ParseTexturesBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Textures, "Expected 'textures'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'textures'", KeslErrorCodes.ExpectedOpenBrace);

        var textures = new List<TextureDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            textures.Add(ParseTextureDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after texture declarations", KeslErrorCodes.ExpectedCloseBrace);

        return new TexturesBlock(textures, location);
    }

    private TextureDeclaration ParseTextureDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected texture name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after texture name", KeslErrorCodes.MissingToken);

        var textureKind = Current.Kind switch
        {
            TokenKind.Texture2D => TextureKind.Texture2D,
            TokenKind.TextureCube => TextureKind.TextureCube,
            TokenKind.Texture3D => TextureKind.Texture3D,
            _ => throw Error(Current, "Expected texture type (texture2D, textureCube, or texture3D)", KeslErrorCodes.ExpectedTypeName)
        };
        Advance();

        // Binding slot (required for textures): @ 0
        Consume(TokenKind.At, "Expected '@' for texture binding slot", KeslErrorCodes.MissingToken);
        var slotToken = Consume(TokenKind.IntLiteral, "Expected binding slot number", KeslErrorCodes.MissingToken);
        var bindingSlot = slotToken.IntValue;

        return new TextureDeclaration(name, textureKind, bindingSlot, location);
    }

    private SamplersBlock ParseSamplersBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Samplers, "Expected 'samplers'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftBrace, "Expected '{' after 'samplers'", KeslErrorCodes.ExpectedOpenBrace);

        var samplers = new List<SamplerDeclaration>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            samplers.Add(ParseSamplerDeclaration());
            Match(TokenKind.Comma);
        }

        Consume(TokenKind.RightBrace, "Expected '}' after sampler declarations", KeslErrorCodes.ExpectedCloseBrace);

        return new SamplersBlock(samplers, location);
    }

    private SamplerDeclaration ParseSamplerDeclaration()
    {
        var location = Current.Location;
        var name = Consume(TokenKind.Identifier, "Expected sampler name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after sampler name", KeslErrorCodes.MissingToken);
        Consume(TokenKind.Sampler, "Expected 'sampler' type", KeslErrorCodes.ExpectedTypeName);

        // Binding slot (required for samplers): @ 0
        Consume(TokenKind.At, "Expected '@' for sampler binding slot", KeslErrorCodes.MissingToken);
        var slotToken = Consume(TokenKind.IntLiteral, "Expected binding slot number", KeslErrorCodes.MissingToken);
        var bindingSlot = slotToken.IntValue;

        return new SamplerDeclaration(name, bindingSlot, location);
    }

    private ExecuteBlock ParseExecuteBlock()
    {
        var location = Current.Location;
        Consume(TokenKind.Execute, "Expected 'execute'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftParen, "Expected '(' after 'execute'", KeslErrorCodes.ExpectedOpenParen);
        Consume(TokenKind.RightParen, "Expected ')' after 'execute('", KeslErrorCodes.ExpectedCloseParen);

        var statements = ParseBlock();

        return new ExecuteBlock(statements, location);
    }

    private List<Statement> ParseBlock()
    {
        Consume(TokenKind.LeftBrace, "Expected '{'", KeslErrorCodes.ExpectedOpenBrace);

        var statements = new List<Statement>();
        while (!Check(TokenKind.RightBrace) && !IsAtEnd())
        {
            var stmt = ParseStatement();
            if (stmt != null)
            {
                statements.Add(stmt);
            }
        }

        Consume(TokenKind.RightBrace, "Expected '}'", KeslErrorCodes.ExpectedCloseBrace);

        return statements;
    }

    private Statement? ParseStatement()
    {
        if (Check(TokenKind.If))
        {
            return ParseIfStatement();
        }

        if (Check(TokenKind.For))
        {
            return ParseForStatement();
        }

        if (Check(TokenKind.Emit))
        {
            return ParseEmitStatement();
        }

        if (Check(TokenKind.EndPrimitive))
        {
            return ParseEndPrimitiveStatement();
        }

        // Expression or assignment statement
        return ParseExpressionStatement();
    }

    private EmitStatement ParseEmitStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.Emit, "Expected 'emit'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftParen, "Expected '(' after 'emit'", KeslErrorCodes.ExpectedOpenParen);
        var position = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after emit position", KeslErrorCodes.ExpectedCloseParen);
        Consume(TokenKind.Semicolon, "Expected ';' after emit statement", KeslErrorCodes.ExpectedSemicolon);

        return new EmitStatement(position, location);
    }

    private EndPrimitiveStatement ParseEndPrimitiveStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.EndPrimitive, "Expected 'endPrimitive'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftParen, "Expected '(' after 'endPrimitive'", KeslErrorCodes.ExpectedOpenParen);
        Consume(TokenKind.RightParen, "Expected ')' after 'endPrimitive('", KeslErrorCodes.ExpectedCloseParen);
        Consume(TokenKind.Semicolon, "Expected ';' after endPrimitive statement", KeslErrorCodes.ExpectedSemicolon);

        return new EndPrimitiveStatement(location);
    }

    private IfStatement ParseIfStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.If, "Expected 'if'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftParen, "Expected '(' after 'if'", KeslErrorCodes.ExpectedOpenParen);
        var condition = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after condition", KeslErrorCodes.ExpectedCloseParen);

        var thenBranch = ParseBlock();

        List<Statement>? elseBranch = null;
        if (Match(TokenKind.Else))
        {
            elseBranch = ParseBlock();
        }

        return new IfStatement(condition, thenBranch, elseBranch, location);
    }

    private ForStatement ParseForStatement()
    {
        var location = Current.Location;
        Consume(TokenKind.For, "Expected 'for'", KeslErrorCodes.MissingToken);
        Consume(TokenKind.LeftParen, "Expected '(' after 'for'", KeslErrorCodes.ExpectedOpenParen);
        var variable = Consume(TokenKind.Identifier, "Expected loop variable name", KeslErrorCodes.ExpectedIdentifier).Text;
        Consume(TokenKind.Colon, "Expected ':' after variable", KeslErrorCodes.MissingToken);
        var start = ParseExpression();
        Consume(TokenKind.DotDot, "Expected '..' in range", KeslErrorCodes.MissingToken);
        var end = ParseExpression();
        Consume(TokenKind.RightParen, "Expected ')' after range", KeslErrorCodes.ExpectedCloseParen);

        var body = ParseBlock();

        return new ForStatement(variable, start, end, body, location);
    }

    private Statement ParseExpressionStatement()
    {
        var location = Current.Location;
        var expr = ParseExpression();

        // Check for assignment
        if (Match(TokenKind.Equal))
        {
            var value = ParseExpression();
            Consume(TokenKind.Semicolon, "Expected ';' after assignment", KeslErrorCodes.ExpectedSemicolon);
            return new AssignmentStatement(expr, value, location);
        }

        // Check for compound assignment
        if (Current.Kind is TokenKind.PlusEqual or TokenKind.MinusEqual
            or TokenKind.StarEqual or TokenKind.SlashEqual)
        {
            var op = Current.Kind switch
            {
                TokenKind.PlusEqual => CompoundOperator.PlusEquals,
                TokenKind.MinusEqual => CompoundOperator.MinusEquals,
                TokenKind.StarEqual => CompoundOperator.StarEquals,
                TokenKind.SlashEqual => CompoundOperator.SlashEquals,
                _ => throw new InvalidOperationException()
            };
            Advance();
            var value = ParseExpression();
            Consume(TokenKind.Semicolon, "Expected ';' after compound assignment", KeslErrorCodes.ExpectedSemicolon);
            return new CompoundAssignmentStatement(expr, op, value, location);
        }

        Consume(TokenKind.Semicolon, "Expected ';' after expression", KeslErrorCodes.ExpectedSemicolon);
        return new ExpressionStatement(expr, location);
    }

    private TypeRef ParseType()
    {
        var location = Current.Location;
        var kind = Current.Kind switch
        {
            TokenKind.Float => PrimitiveTypeKind.Float,
            TokenKind.Float2 => PrimitiveTypeKind.Float2,
            TokenKind.Float3 => PrimitiveTypeKind.Float3,
            TokenKind.Float4 => PrimitiveTypeKind.Float4,
            TokenKind.Int => PrimitiveTypeKind.Int,
            TokenKind.Int2 => PrimitiveTypeKind.Int2,
            TokenKind.Int3 => PrimitiveTypeKind.Int3,
            TokenKind.Int4 => PrimitiveTypeKind.Int4,
            TokenKind.Uint => PrimitiveTypeKind.Uint,
            TokenKind.Bool => PrimitiveTypeKind.Bool,
            TokenKind.Mat4 => PrimitiveTypeKind.Mat4,
            _ => throw Error(Current, "Expected type name", KeslErrorCodes.ExpectedTypeName)
        };
        Advance();

        return new PrimitiveType(kind, location);
    }

    #region Expression Parsing

    private Expression ParseExpression()
    {
        return ParseLogicalOr();
    }

    private Expression ParseLogicalOr()
    {
        var expr = ParseLogicalAnd();

        while (Match(TokenKind.PipePipe))
        {
            var location = Previous.Location;
            var right = ParseLogicalAnd();
            expr = new BinaryExpression(expr, BinaryOperator.Or, right, location);
        }

        return expr;
    }

    private Expression ParseLogicalAnd()
    {
        var expr = ParseEquality();

        while (Match(TokenKind.AmpAmp))
        {
            var location = Previous.Location;
            var right = ParseEquality();
            expr = new BinaryExpression(expr, BinaryOperator.And, right, location);
        }

        return expr;
    }

    private Expression ParseEquality()
    {
        var expr = ParseComparison();

        while (Match(TokenKind.EqualEqual, TokenKind.BangEqual))
        {
            var op = Previous.Kind == TokenKind.EqualEqual
                ? BinaryOperator.Equal
                : BinaryOperator.NotEqual;
            var location = Previous.Location;
            var right = ParseComparison();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseComparison()
    {
        var expr = ParseTerm();

        while (Match(TokenKind.Less, TokenKind.LessEqual, TokenKind.Greater, TokenKind.GreaterEqual))
        {
            var op = Previous.Kind switch
            {
                TokenKind.Less => BinaryOperator.Less,
                TokenKind.LessEqual => BinaryOperator.LessEqual,
                TokenKind.Greater => BinaryOperator.Greater,
                TokenKind.GreaterEqual => BinaryOperator.GreaterEqual,
                _ => throw new InvalidOperationException()
            };
            var location = Previous.Location;
            var right = ParseTerm();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseTerm()
    {
        var expr = ParseFactor();

        while (Match(TokenKind.Plus, TokenKind.Minus))
        {
            var op = Previous.Kind == TokenKind.Plus
                ? BinaryOperator.Add
                : BinaryOperator.Subtract;
            var location = Previous.Location;
            var right = ParseFactor();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseFactor()
    {
        var expr = ParseUnary();

        while (Match(TokenKind.Star, TokenKind.Slash))
        {
            var op = Previous.Kind == TokenKind.Star
                ? BinaryOperator.Multiply
                : BinaryOperator.Divide;
            var location = Previous.Location;
            var right = ParseUnary();
            expr = new BinaryExpression(expr, op, right, location);
        }

        return expr;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenKind.Bang, TokenKind.Minus))
        {
            var op = Previous.Kind == TokenKind.Bang
                ? UnaryOperator.Not
                : UnaryOperator.Negate;
            var location = Previous.Location;
            var operand = ParseUnary();
            return new UnaryExpression(op, operand, location);
        }

        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        var expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenKind.Dot))
            {
                var memberName = Consume(TokenKind.Identifier, "Expected member name after '.'", KeslErrorCodes.ExpectedIdentifier).Text;
                expr = new MemberAccessExpression(expr, memberName, Previous.Location);
            }
            else if (Match(TokenKind.LeftParen))
            {
                // Function call - rewrite the expression
                if (expr is IdentifierExpression id)
                {
                    var args = ParseArguments();
                    Consume(TokenKind.RightParen, "Expected ')' after arguments", KeslErrorCodes.ExpectedCloseParen);
                    expr = new CallExpression(id.Name, args, id.Location);
                }
                else
                {
                    throw Error(Previous, "Can only call functions by name", KeslErrorCodes.InvalidExpression);
                }
            }
            else if (Match(TokenKind.LeftBracket))
            {
                // Array index access
                var index = ParseExpression();
                Consume(TokenKind.RightBracket, "Expected ']' after array index", KeslErrorCodes.ExpectedCloseBracket);
                expr = new IndexExpression(expr, index, Previous.Location);
            }
            else
            {
                break;
            }
        }

        return expr;
    }

    private List<Expression> ParseArguments()
    {
        var args = new List<Expression>();

        if (!Check(TokenKind.RightParen))
        {
            do
            {
                args.Add(ParseExpression());
            } while (Match(TokenKind.Comma));
        }

        return args;
    }

    private Expression ParsePrimary()
    {
        var location = Current.Location;

        // Literals
        if (Match(TokenKind.IntLiteral))
        {
            return new IntLiteralExpression(Previous.IntValue, location);
        }

        if (Match(TokenKind.FloatLiteral))
        {
            return new FloatLiteralExpression(Previous.FloatValue, location);
        }

        if (Match(TokenKind.True))
        {
            return new BoolLiteralExpression(true, location);
        }

        if (Match(TokenKind.False))
        {
            return new BoolLiteralExpression(false, location);
        }

        // Has expression
        if (Match(TokenKind.Has))
        {
            var componentName = Consume(TokenKind.Identifier, "Expected component name after 'has'", KeslErrorCodes.ExpectedIdentifier).Text;
            return new HasExpression(componentName, location);
        }

        // Identifier
        if (Match(TokenKind.Identifier))
        {
            return new IdentifierExpression(Previous.Text, location);
        }

        // Grouped expression
        if (Match(TokenKind.LeftParen))
        {
            var inner = ParseExpression();
            Consume(TokenKind.RightParen, "Expected ')' after expression", KeslErrorCodes.ExpectedCloseParen);
            return new ParenthesizedExpression(inner, location);
        }

        throw Error(Current, "Expected expression", KeslErrorCodes.InvalidExpression);
    }

    #endregion

    #region Helper Methods

    private Token Current => _tokens[_current];
    private Token Previous => _tokens[_current - 1];
    private bool IsAtEnd() => Current.Kind == TokenKind.EndOfFile;

    private bool Check(TokenKind kind)
    {
        return !IsAtEnd() && Current.Kind == kind;
    }

    private bool Match(params TokenKind[] kinds)
    {
        foreach (var kind in kinds)
        {
            if (Check(kind))
            {
                Advance();
                return true;
            }
        }
        return false;
    }

    private Token Advance()
    {
        if (!IsAtEnd())
        {
            _current++;
        }
        return Previous;
    }

    private Token Consume(TokenKind kind, string message, string? code = null)
    {
        if (Check(kind))
        {
            return Advance();
        }
        throw Error(Current, message, code);
    }

    private ParseException Error(Token token, string message, string? code = null)
    {
        _errors.Add(new CompilerError(message, token.Location, code));
        return new ParseException(message);
    }

    private void Synchronize()
    {
        Advance();

        while (!IsAtEnd())
        {
            // Synchronize at statement boundaries
            if (Previous.Kind == TokenKind.Semicolon) return;
            if (Previous.Kind == TokenKind.RightBrace) return;

            // Only synchronize at top-level declaration keywords
            // (not Query, Params, Execute, etc. which are only valid inside declarations)
            switch (Current.Kind)
            {
                case TokenKind.Component:
                case TokenKind.Compute:
                case TokenKind.Vertex:
                case TokenKind.Fragment:
                case TokenKind.Geometry:
                case TokenKind.Pipeline:
                    return;
            }

            Advance();
        }
    }

    #endregion
}

/// <summary>
/// Represents a compiler error with location information.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The source location of the error.</param>
/// <param name="Code">The optional error code (e.g., "KESL200").</param>
public record CompilerError(string Message, SourceLocation Location, string? Code = null)
{
    /// <summary>
    /// Returns a formatted error message.
    /// </summary>
    public override string ToString() =>
        Code is not null
            ? $"{Location}: error {Code}: {Message}"
            : $"{Location}: error: {Message}";
}

/// <summary>
/// Internal exception used for parser error recovery.
/// </summary>
internal sealed class ParseException : Exception
{
    public ParseException(string message) : base(message) { }
}
