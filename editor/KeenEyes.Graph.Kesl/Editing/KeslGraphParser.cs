using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Editing;

/// <summary>
/// Parses KESL source code into a visual graph representation.
/// </summary>
/// <remarks>
/// <para>
/// The parser uses the existing <see cref="KeslCompiler"/> to parse KESL source
/// into an AST, then creates corresponding graph nodes for each AST element.
/// </para>
/// </remarks>
public sealed class KeslGraphParser
{
    private readonly NodeTypeRegistry nodeTypeRegistry;

    /// <summary>
    /// Initializes a new graph parser.
    /// </summary>
    /// <param name="nodeTypeRegistry">The node type registry for creating nodes.</param>
    public KeslGraphParser(NodeTypeRegistry nodeTypeRegistry)
    {
        this.nodeTypeRegistry = nodeTypeRegistry;
    }

    /// <summary>
    /// Parses KESL source code into a graph.
    /// </summary>
    /// <param name="source">The KESL source code.</param>
    /// <param name="canvas">The target canvas entity.</param>
    /// <param name="world">The world to create nodes in.</param>
    /// <returns>A parse result with the source mapping or errors.</returns>
    public ParseResult Parse(string source, Entity canvas, IWorld world)
    {
        var mapping = new SourceMapping();

        // Use existing compiler to parse to AST
        var compileResult = KeslCompiler.Compile(source);
        if (compileResult.Diagnostics.Count > 0)
        {
            var errors = compileResult.Diagnostics.Select(d =>
                new ParseError(d.Message, d.Span.Start)).ToList();
            return ParseResult.Failure(errors);
        }

        if (compileResult.SourceFile is null)
        {
            return ParseResult.Failure([new ParseError("No source file parsed", new SourceLocation("", 0, 0))]);
        }

        // Find compute declaration
        var computeDecl = compileResult.SourceFile.Declarations
            .OfType<ComputeDeclaration>()
            .FirstOrDefault();

        if (computeDecl is null)
        {
            return ParseResult.Failure([new ParseError("No compute declaration found in source", new SourceLocation("", 0, 0))]);
        }

        // Build graph from AST
        var context = new ParseContext(canvas, world, mapping);
        CreateNodesFromDeclaration(computeDecl, context);

        // Auto-layout the nodes
        AutoLayoutNodes(context);

        return ParseResult.Success(mapping);
    }

    private void CreateNodesFromDeclaration(ComputeDeclaration declaration, ParseContext context)
    {
        // Create root ComputeShader node
        var rootNode = CreateNode(KeslNodeIds.ComputeShader, new Vector2(600, 100), context);
        if (context.World.Has<ComputeShaderNodeData>(rootNode))
        {
            var data = context.World.Get<ComputeShaderNodeData>(rootNode);
            data.ShaderName = declaration.Name;
            context.World.Set(rootNode, data);
        }
        context.Mapping.AddMapping(rootNode, CreateSpan(declaration.Location));
        context.RootNode = rootNode;

        // Create QueryBinding nodes
        var queryY = 100f;
        foreach (var binding in declaration.Query.Bindings)
        {
            var queryNode = CreateNode(KeslNodeIds.QueryBinding, new Vector2(100, queryY), context);
            if (context.World.Has<QueryBindingNodeData>(queryNode))
            {
                var data = context.World.Get<QueryBindingNodeData>(queryNode);
                data.ComponentTypeName = binding.ComponentName;
                data.IsReadOnly = binding.AccessMode == AccessMode.Read;
                data.BindingName = binding.ComponentName.ToLowerInvariant();
                context.World.Set(queryNode, data);
            }
            context.Mapping.AddMapping(queryNode, CreateSpan(binding.Location));
            context.Mapping.AddVariableMapping(binding.ComponentName.ToLowerInvariant(), queryNode);
            queryY += 80f;
        }

        // Create Parameter nodes
        if (declaration.Params is not null)
        {
            var paramY = 100f;
            foreach (var param in declaration.Params.Parameters)
            {
                var paramNode = CreateNode(KeslNodeIds.Parameter, new Vector2(100, queryY + paramY), context);
                if (context.World.Has<ParameterNodeData>(paramNode))
                {
                    var data = context.World.Get<ParameterNodeData>(paramNode);
                    data.ParameterName = param.Name;
                    data.ParameterType = TypeRefToPortType(param.Type);
                    context.World.Set(paramNode, data);
                }
                context.Mapping.AddMapping(paramNode, CreateSpan(param.Location));
                context.Mapping.AddVariableMapping(param.Name, paramNode);
                paramY += 80f;
            }
        }

        // Create nodes from execute block statements
        var stmtY = 100f;
        foreach (var statement in declaration.Execute.Body)
        {
            CreateNodeFromStatement(statement, new Vector2(350, stmtY), context);
            stmtY += 100f;
        }
    }

    private void CreateNodeFromStatement(Statement statement, Vector2 position, ParseContext context)
    {
        switch (statement)
        {
            case AssignmentStatement assign:
                CreateNodesFromAssignment(assign, position, context);
                break;

            case IfStatement ifStmt:
                CreateNodeFromIf(ifStmt, position, context);
                break;

            case ForStatement forStmt:
                CreateNodeFromFor(forStmt, position, context);
                break;

            case ExpressionStatement exprStmt:
                CreateNodeFromExpression(exprStmt.Expression, position, context);
                break;
        }
    }

    private void CreateNodesFromAssignment(AssignmentStatement assign, Vector2 position, ParseContext context)
    {
        if (assign.Target is IdentifierExpression identifier)
        {
            // SetVariable node
            var setVarNode = CreateNode(KeslNodeIds.SetVariable, position, context);
            if (context.World.Has<VariableNodeData>(setVarNode))
            {
                var data = context.World.Get<VariableNodeData>(setVarNode);
                data.VariableName = identifier.Name;
                context.World.Set(setVarNode, data);
            }
            context.Mapping.AddMapping(setVarNode, CreateSpan(assign.Location));
            context.Mapping.AddVariableMapping(identifier.Name, setVarNode);

            // Create value expression node
            var valueNode = CreateNodeFromExpression(assign.Value, position + new Vector2(-200, 0), context);
            if (valueNode.IsValid)
            {
                CreateConnection(valueNode, 0, setVarNode, 1, context);
            }
        }
    }

    private void CreateNodeFromIf(IfStatement ifStmt, Vector2 position, ParseContext context)
    {
        var ifNode = CreateNode(KeslNodeIds.If, position, context);
        context.Mapping.AddMapping(ifNode, CreateSpan(ifStmt.Location));

        // Create condition expression
        var conditionNode = CreateNodeFromExpression(ifStmt.Condition, position + new Vector2(-200, 0), context);
        if (conditionNode.IsValid)
        {
            CreateConnection(conditionNode, 0, ifNode, 0, context);
        }
    }

    private void CreateNodeFromFor(ForStatement forStmt, Vector2 position, ParseContext context)
    {
        var forNode = CreateNode(KeslNodeIds.ForLoop, position, context);
        if (context.World.Has<ForLoopNodeData>(forNode))
        {
            var data = context.World.Get<ForLoopNodeData>(forNode);
            data.IndexName = forStmt.VariableName;
            context.World.Set(forNode, data);
        }
        context.Mapping.AddMapping(forNode, CreateSpan(forStmt.Location));

        // Create start expression
        var startNode = CreateNodeFromExpression(forStmt.Start, position + new Vector2(-200, -50), context);
        if (startNode.IsValid)
        {
            CreateConnection(startNode, 0, forNode, 0, context);
        }

        // Create end expression
        var endNode = CreateNodeFromExpression(forStmt.End, position + new Vector2(-200, 50), context);
        if (endNode.IsValid)
        {
            CreateConnection(endNode, 0, forNode, 1, context);
        }
    }

    private Entity CreateNodeFromExpression(Expression expression, Vector2 position, ParseContext context)
    {
        switch (expression)
        {
            case BinaryExpression binary:
                return CreateNodeFromBinary(binary, position, context);

            case UnaryExpression unary:
                return CreateNodeFromUnary(unary, position, context);

            case CallExpression call:
                return CreateNodeFromCall(call, position, context);

            case FloatLiteralExpression floatLit:
                return CreateFloatConstant(floatLit.Value, floatLit.Location, position, context);

            case IntLiteralExpression intLit:
                return CreateIntConstant(intLit.Value, intLit.Location, position, context);

            case BoolLiteralExpression boolLit:
                return CreateBoolConstant(boolLit.Value, boolLit.Location, position, context);

            case IdentifierExpression ident:
                return CreateGetVariable(ident.Name, ident.Location, position, context);

            default:
                return Entity.Null;
        }
    }

    private Entity CreateNodeFromBinary(BinaryExpression binary, Vector2 position, ParseContext context)
    {
        var nodeTypeId = binary.Operator switch
        {
            BinaryOperator.Add => KeslNodeIds.Add,
            BinaryOperator.Subtract => KeslNodeIds.Subtract,
            BinaryOperator.Multiply => KeslNodeIds.Multiply,
            BinaryOperator.Divide => KeslNodeIds.Divide,
            BinaryOperator.And => KeslNodeIds.And,
            BinaryOperator.Or => KeslNodeIds.Or,
            BinaryOperator.Equal or BinaryOperator.NotEqual or
            BinaryOperator.Less or BinaryOperator.LessEqual or
            BinaryOperator.Greater or BinaryOperator.GreaterEqual => KeslNodeIds.Compare,
            _ => 0
        };

        if (nodeTypeId == 0)
        {
            return Entity.Null;
        }

        var node = CreateNode(nodeTypeId, position, context);
        context.Mapping.AddMapping(node, CreateSpan(binary.Location));

        // Set compare operator if needed
        if (nodeTypeId == KeslNodeIds.Compare && context.World.Has<CompareNodeData>(node))
        {
            var data = context.World.Get<CompareNodeData>(node);
            data.Operator = binary.Operator switch
            {
                BinaryOperator.Equal => ComparisonOperator.Equal,
                BinaryOperator.NotEqual => ComparisonOperator.NotEqual,
                BinaryOperator.Less => ComparisonOperator.LessThan,
                BinaryOperator.LessEqual => ComparisonOperator.LessThanOrEqual,
                BinaryOperator.Greater => ComparisonOperator.GreaterThan,
                BinaryOperator.GreaterEqual => ComparisonOperator.GreaterThanOrEqual,
                _ => ComparisonOperator.Equal
            };
            context.World.Set(node, data);
        }

        // Create child nodes
        var leftNode = CreateNodeFromExpression(binary.Left, position + new Vector2(-200, -50), context);
        var rightNode = CreateNodeFromExpression(binary.Right, position + new Vector2(-200, 50), context);

        if (leftNode.IsValid)
        {
            CreateConnection(leftNode, 0, node, 0, context);
        }
        if (rightNode.IsValid)
        {
            CreateConnection(rightNode, 0, node, 1, context);
        }

        return node;
    }

    private Entity CreateNodeFromUnary(UnaryExpression unary, Vector2 position, ParseContext context)
    {
        var nodeTypeId = unary.Operator switch
        {
            UnaryOperator.Negate => KeslNodeIds.Negate,
            UnaryOperator.Not => KeslNodeIds.Not,
            _ => 0
        };

        if (nodeTypeId == 0)
        {
            return Entity.Null;
        }

        var node = CreateNode(nodeTypeId, position, context);
        context.Mapping.AddMapping(node, CreateSpan(unary.Location));

        var operandNode = CreateNodeFromExpression(unary.Operand, position + new Vector2(-200, 0), context);
        if (operandNode.IsValid)
        {
            CreateConnection(operandNode, 0, node, 0, context);
        }

        return node;
    }

    private Entity CreateNodeFromCall(CallExpression call, Vector2 position, ParseContext context)
    {
        var nodeTypeId = call.FunctionName switch
        {
            "sin" => KeslNodeIds.Sin,
            "cos" => KeslNodeIds.Cos,
            "tan" => KeslNodeIds.Tan,
            "asin" => KeslNodeIds.Asin,
            "acos" => KeslNodeIds.Acos,
            "atan" => KeslNodeIds.Atan,
            "atan2" => KeslNodeIds.Atan2,
            "abs" => KeslNodeIds.Abs,
            "floor" => KeslNodeIds.Floor,
            "ceil" => KeslNodeIds.Ceil,
            "round" => KeslNodeIds.Round,
            "sign" => KeslNodeIds.Sign,
            "fract" => KeslNodeIds.Frac,
            "sqrt" => KeslNodeIds.Sqrt,
            "exp" => KeslNodeIds.Exp,
            "log" => KeslNodeIds.Log,
            "log2" => KeslNodeIds.Log2,
            "pow" => KeslNodeIds.Pow,
            "min" => KeslNodeIds.Min,
            "max" => KeslNodeIds.Max,
            "clamp" => KeslNodeIds.Clamp,
            "mix" => KeslNodeIds.Lerp,
            "step" => KeslNodeIds.Step,
            "smoothstep" => KeslNodeIds.Smoothstep,
            "mod" => KeslNodeIds.Modulo,
            "vec2" => KeslNodeIds.ConstructVector2,
            "vec3" => KeslNodeIds.ConstructVector3,
            "vec4" => KeslNodeIds.ConstructVector4,
            "normalize" => KeslNodeIds.Normalize,
            "length" => KeslNodeIds.Length,
            "dot" => KeslNodeIds.DotProduct,
            "cross" => KeslNodeIds.CrossProduct,
            "distance" => KeslNodeIds.Distance,
            "reflect" => KeslNodeIds.Reflect,
            _ => 0
        };

        if (nodeTypeId == 0)
        {
            return Entity.Null;
        }

        var node = CreateNode(nodeTypeId, position, context);
        context.Mapping.AddMapping(node, CreateSpan(call.Location));

        // Connect arguments
        var yOffset = -(call.Arguments.Count - 1) * 25f;
        for (int i = 0; i < call.Arguments.Count; i++)
        {
            var argNode = CreateNodeFromExpression(call.Arguments[i], position + new Vector2(-200, yOffset), context);
            if (argNode.IsValid)
            {
                CreateConnection(argNode, 0, node, i, context);
            }
            yOffset += 50f;
        }

        return node;
    }

    private Entity CreateFloatConstant(float value, SourceLocation location, Vector2 position, ParseContext context)
    {
        var node = CreateNode(KeslNodeIds.FloatConstant, position, context);
        if (context.World.Has<FloatConstantData>(node))
        {
            var data = context.World.Get<FloatConstantData>(node);
            data.Value = value;
            context.World.Set(node, data);
        }
        context.Mapping.AddMapping(node, CreateSpan(location));
        return node;
    }

    private Entity CreateIntConstant(int value, SourceLocation location, Vector2 position, ParseContext context)
    {
        var node = CreateNode(KeslNodeIds.IntConstant, position, context);
        if (context.World.Has<IntConstantData>(node))
        {
            var data = context.World.Get<IntConstantData>(node);
            data.Value = value;
            context.World.Set(node, data);
        }
        context.Mapping.AddMapping(node, CreateSpan(location));
        return node;
    }

    private Entity CreateBoolConstant(bool value, SourceLocation location, Vector2 position, ParseContext context)
    {
        var node = CreateNode(KeslNodeIds.BoolConstant, position, context);
        if (context.World.Has<BoolConstantData>(node))
        {
            var data = context.World.Get<BoolConstantData>(node);
            data.Value = value;
            context.World.Set(node, data);
        }
        context.Mapping.AddMapping(node, CreateSpan(location));
        return node;
    }

    private Entity CreateGetVariable(string name, SourceLocation location, Vector2 position, ParseContext context)
    {
        var node = CreateNode(KeslNodeIds.GetVariable, position, context);
        if (context.World.Has<VariableNodeData>(node))
        {
            var data = context.World.Get<VariableNodeData>(node);
            data.VariableName = name;
            context.World.Set(node, data);
        }
        context.Mapping.AddMapping(node, CreateSpan(location));
        return node;
    }

    private Entity CreateNode(int typeId, Vector2 position, ParseContext context)
    {
        return nodeTypeRegistry.CreateNode(context.Canvas, typeId, position, context.World);
    }

    private void CreateConnection(Entity sourceNode, int sourcePort, Entity targetNode, int targetPort, ParseContext context)
    {
        var connection = context.World.Spawn()
            .With(new GraphConnection
            {
                SourceNode = sourceNode,
                SourcePortIndex = sourcePort,
                TargetNode = targetNode,
                TargetPortIndex = targetPort,
                Canvas = context.Canvas
            })
            .Build();

        context.World.SetParent(connection, context.Canvas);
    }

    private static SourceSpan CreateSpan(SourceLocation location)
    {
        // Create a minimal span at the location
        return new SourceSpan(location, location);
    }

    private static PortTypeId TypeRefToPortType(TypeRef typeRef)
    {
        if (typeRef is PrimitiveType primitive)
        {
            return primitive.Kind switch
            {
                PrimitiveTypeKind.Float => PortTypeId.Float,
                PrimitiveTypeKind.Float2 => PortTypeId.Float2,
                PrimitiveTypeKind.Float3 => PortTypeId.Float3,
                PrimitiveTypeKind.Float4 => PortTypeId.Float4,
                PrimitiveTypeKind.Int => PortTypeId.Int,
                PrimitiveTypeKind.Int2 => PortTypeId.Int2,
                PrimitiveTypeKind.Int3 => PortTypeId.Int3,
                PrimitiveTypeKind.Int4 => PortTypeId.Int4,
                PrimitiveTypeKind.Bool => PortTypeId.Bool,
                _ => PortTypeId.Any
            };
        }
        return PortTypeId.Any;
    }

    private static void AutoLayoutNodes(ParseContext context)
    {
        // Simple auto-layout: just space nodes out
        // A more sophisticated layout algorithm would analyze the graph structure
        // and arrange nodes to minimize edge crossings
    }

    private sealed class ParseContext(Entity canvas, IWorld world, SourceMapping mapping)
    {
        public Entity Canvas => canvas;
        public IWorld World => world;
        public SourceMapping Mapping => mapping;
        public Entity RootNode { get; set; }
    }
}

/// <summary>
/// Result of parsing KESL source to a graph.
/// </summary>
public sealed class ParseResult
{
    private ParseResult(bool isSuccess, SourceMapping? mapping, IReadOnlyList<ParseError> errors)
    {
        IsSuccess = isSuccess;
        Mapping = mapping;
        Errors = errors;
    }

    /// <summary>
    /// Gets whether parsing succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the source mapping (null if parsing failed).
    /// </summary>
    public SourceMapping? Mapping { get; }

    /// <summary>
    /// Gets parse errors.
    /// </summary>
    public IReadOnlyList<ParseError> Errors { get; }

    /// <summary>
    /// Creates a successful parse result.
    /// </summary>
    public static ParseResult Success(SourceMapping mapping)
        => new(true, mapping, []);

    /// <summary>
    /// Creates a failed parse result.
    /// </summary>
    public static ParseResult Failure(IReadOnlyList<ParseError> errors)
        => new(false, null, errors);
}

/// <summary>
/// A parse error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Location">The source location.</param>
public readonly record struct ParseError(string Message, SourceLocation Location);
