using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// Compiles a KESL shader graph to an AST representation.
/// </summary>
/// <remarks>
/// <para>
/// The graph compiler converts a visual shader graph into a KESL AST (Abstract Syntax Tree)
/// that can then be converted to GLSL or other shader formats. The compilation process:
/// </para>
/// <list type="number">
/// <item>Finds the root ComputeShader node</item>
/// <item>Collects QueryBinding nodes to build the query block</item>
/// <item>Collects Parameter nodes to build the params block</item>
/// <item>Topologically sorts remaining nodes by dependency</item>
/// <item>Builds the execute block from sorted expressions</item>
/// </list>
/// </remarks>
public sealed class KeslGraphCompiler
{
    private static readonly SourceLocation GeneratedLocation = new("graph://generated", 0, 0);

    private readonly GraphTraverser traverser = new();
    private readonly CompilationContext context = new();

    /// <summary>
    /// Compiles a shader graph to an AST.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The compilation result with the AST or errors.</returns>
    public CompilationResult Compile(Entity canvas, IWorld world)
    {
        context.Clear();

        // Find root ComputeShader node
        var rootNode = traverser.FindRootNode(canvas, world);
        if (rootNode is null)
        {
            return CompilationResult.Error("No ComputeShader node found in graph");
        }

        // Get shader name
        var shaderData = world.Has<ComputeShaderNodeData>(rootNode.Value)
            ? world.Get<ComputeShaderNodeData>(rootNode.Value)
            : ComputeShaderNodeData.Default;

        // Collect query bindings
        var queryBindings = CollectQueryBindings(canvas, world);

        // Collect parameters
        var parameters = CollectParameters(canvas, world);

        // Build the execute block from connected nodes
        var executeStatements = BuildExecuteBlock(rootNode.Value, canvas, world);

        // Check for compilation errors
        if (context.Errors.Count > 0)
        {
            return CompilationResult.Failure([.. context.Errors]);
        }

        // Build the AST
        var queryBlock = new QueryBlock(queryBindings, GeneratedLocation);
        var paramsBlock = parameters.Count > 0
            ? new ParamsBlock(parameters, GeneratedLocation)
            : null;
        var executeBlock = new ExecuteBlock(executeStatements, GeneratedLocation);

        var declaration = new ComputeDeclaration(
            shaderData.ShaderName,
            queryBlock,
            paramsBlock,
            executeBlock,
            GeneratedLocation);

        return CompilationResult.Success(declaration);
    }

    private List<QueryBinding> CollectQueryBindings(Entity canvas, IWorld world)
    {
        var bindings = new List<QueryBinding>();

        foreach (var entity in world.Query<GraphNode, QueryBindingNodeData>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            var bindingData = world.Get<QueryBindingNodeData>(entity);
            var accessMode = bindingData.IsReadOnly ? AccessMode.Read : AccessMode.Write;

            bindings.Add(new QueryBinding(
                accessMode,
                bindingData.ComponentTypeName,
                GeneratedLocation));
        }

        return bindings;
    }

    private List<ParamDeclaration> CollectParameters(Entity canvas, IWorld world)
    {
        var parameters = new List<ParamDeclaration>();

        foreach (var entity in world.Query<GraphNode, ParameterNodeData>())
        {
            var nodeData = world.Get<GraphNode>(entity);
            if (nodeData.Canvas != canvas)
            {
                continue;
            }

            var paramData = world.Get<ParameterNodeData>(entity);
            var typeRef = PortTypeToTypeRef(paramData.ParameterType);

            parameters.Add(new ParamDeclaration(
                paramData.ParameterName,
                typeRef,
                GeneratedLocation));
        }

        return parameters;
    }

    private List<Statement> BuildExecuteBlock(Entity rootNode, Entity canvas, IWorld world)
    {
        var statements = new List<Statement>();
        var expressionBuilder = new ExpressionBuilder(traverser);

        // Get nodes in topological order
        var sortedNodes = traverser.TopologicalSort(canvas, world);
        if (sortedNodes.Count == 0)
        {
            context.AddError(Entity.Null, null, "Graph contains cycles or has no nodes", "KESL030");
            return statements;
        }

        // Process nodes that need variable assignments
        // (nodes with multiple output connections)
        foreach (var node in sortedNodes)
        {
            var nodeData = world.Get<GraphNode>(node);

            // Skip non-expression nodes
            if (IsStructuralNode(nodeData.NodeTypeId))
            {
                continue;
            }

            // Check if this node's output is used multiple times
            var outputCount = CountOutputConnections(node, canvas, world);
            if (outputCount > 1)
            {
                // Generate a temporary variable
                var varName = context.GenerateTempVar();
                context.SetNodeVariable(node, varName);

                var expr = expressionBuilder.BuildExpression(node, nodeData.NodeTypeId, canvas, world, context);
                if (expr is not null)
                {
                    statements.Add(new AssignmentStatement(
                        new IdentifierExpression(varName, GeneratedLocation),
                        expr,
                        GeneratedLocation));
                }
            }
        }

        // Build the main output expression from the root node's execute input
        BuildRootExecuteStatements(rootNode, canvas, world, expressionBuilder, statements);

        return statements;
    }

    private void BuildRootExecuteStatements(
        Entity rootNode,
        Entity canvas,
        IWorld world,
        ExpressionBuilder expressionBuilder,
        List<Statement> statements)
    {
        // The ComputeShader node typically has an "Execute" flow input
        // that connects to the actual shader logic
        foreach (var (sourceNode, _) in traverser.GetInputConnections(rootNode, 0, canvas, world))
        {
            BuildNodeStatements(sourceNode, canvas, world, expressionBuilder, statements);
        }
    }

    private void BuildNodeStatements(
        Entity node,
        Entity canvas,
        IWorld world,
        ExpressionBuilder expressionBuilder,
        List<Statement> statements)
    {
        var nodeData = world.Get<GraphNode>(node);

        switch (nodeData.NodeTypeId)
        {
            case KeslNodeIds.SetVariable:
                BuildSetVariableStatement(node, canvas, world, expressionBuilder, statements);
                break;

            case KeslNodeIds.If:
                BuildIfStatement(node, canvas, world, expressionBuilder, statements);
                break;

            case KeslNodeIds.ForLoop:
                BuildForStatement(node, canvas, world, expressionBuilder, statements);
                break;

            default:
                // For expression nodes, build an expression statement
                var expr = expressionBuilder.BuildExpression(node, nodeData.NodeTypeId, canvas, world, context);
                if (expr is not null)
                {
                    statements.Add(new ExpressionStatement(expr, GeneratedLocation));
                }
                break;
        }
    }

    private void BuildSetVariableStatement(
        Entity node,
        Entity canvas,
        IWorld world,
        ExpressionBuilder expressionBuilder,
        List<Statement> statements)
    {
        var varData = world.Has<VariableNodeData>(node)
            ? world.Get<VariableNodeData>(node)
            : VariableNodeData.Default;

        // Get the value input
        Expression? valueExpr = null;
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 1, canvas, world))
        {
            var sourceData = world.Get<GraphNode>(sourceNode);
            valueExpr = expressionBuilder.BuildExpression(sourceNode, sourceData.NodeTypeId, canvas, world, context);
            break;
        }

        if (valueExpr is null)
        {
            valueExpr = new FloatLiteralExpression(0f, GeneratedLocation);
        }

        statements.Add(new AssignmentStatement(
            new IdentifierExpression(varData.VariableName, GeneratedLocation),
            valueExpr,
            GeneratedLocation));
    }

    private void BuildIfStatement(
        Entity node,
        Entity canvas,
        IWorld world,
        ExpressionBuilder expressionBuilder,
        List<Statement> statements)
    {
        // Get condition expression (input 0)
        Expression? condition = null;
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 0, canvas, world))
        {
            var sourceData = world.Get<GraphNode>(sourceNode);
            condition = expressionBuilder.BuildExpression(sourceNode, sourceData.NodeTypeId, canvas, world, context);
            break;
        }

        if (condition is null)
        {
            condition = new BoolLiteralExpression(false, GeneratedLocation);
        }

        // Build then branch (input 1 - True flow)
        var thenStatements = new List<Statement>();
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 1, canvas, world))
        {
            BuildNodeStatements(sourceNode, canvas, world, expressionBuilder, thenStatements);
        }

        // Build else branch (input 2 - False flow)
        var elseStatements = new List<Statement>();
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 2, canvas, world))
        {
            BuildNodeStatements(sourceNode, canvas, world, expressionBuilder, elseStatements);
        }

        statements.Add(new IfStatement(
            condition,
            thenStatements,
            elseStatements.Count > 0 ? elseStatements : null,
            GeneratedLocation));
    }

    private void BuildForStatement(
        Entity node,
        Entity canvas,
        IWorld world,
        ExpressionBuilder expressionBuilder,
        List<Statement> statements)
    {
        var loopData = world.Has<ForLoopNodeData>(node)
            ? world.Get<ForLoopNodeData>(node)
            : ForLoopNodeData.Default;

        // Get start expression (input 0)
        Expression? start = null;
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 0, canvas, world))
        {
            var sourceData = world.Get<GraphNode>(sourceNode);
            start = expressionBuilder.BuildExpression(sourceNode, sourceData.NodeTypeId, canvas, world, context);
            break;
        }

        // Get end expression (input 1)
        Expression? end = null;
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 1, canvas, world))
        {
            var sourceData = world.Get<GraphNode>(sourceNode);
            end = expressionBuilder.BuildExpression(sourceNode, sourceData.NodeTypeId, canvas, world, context);
            break;
        }

        start ??= new IntLiteralExpression(0, GeneratedLocation);
        end ??= new IntLiteralExpression(10, GeneratedLocation);

        // Build loop body (input 2 - Body flow)
        var bodyStatements = new List<Statement>();
        foreach (var (sourceNode, _) in traverser.GetInputConnections(node, 2, canvas, world))
        {
            BuildNodeStatements(sourceNode, canvas, world, expressionBuilder, bodyStatements);
        }

        statements.Add(new ForStatement(
            loopData.IndexName,
            start,
            end,
            bodyStatements,
            GeneratedLocation));
    }

    private int CountOutputConnections(Entity node, Entity canvas, IWorld world)
    {
        var count = 0;
        foreach (var entity in world.Query<GraphConnection>())
        {
            var connection = world.Get<GraphConnection>(entity);
            if (connection.Canvas == canvas && connection.SourceNode == node)
            {
                count++;
            }
        }
        return count;
    }

    private static bool IsStructuralNode(int nodeTypeId)
    {
        return nodeTypeId switch
        {
            KeslNodeIds.ComputeShader => true,
            KeslNodeIds.QueryBinding => true,
            KeslNodeIds.Parameter => true,
            _ => false
        };
    }

    private static TypeRef PortTypeToTypeRef(PortTypeId portType)
    {
        var kind = portType switch
        {
            PortTypeId.Float => PrimitiveTypeKind.Float,
            PortTypeId.Float2 => PrimitiveTypeKind.Float2,
            PortTypeId.Float3 => PrimitiveTypeKind.Float3,
            PortTypeId.Float4 => PrimitiveTypeKind.Float4,
            PortTypeId.Int => PrimitiveTypeKind.Int,
            PortTypeId.Int2 => PrimitiveTypeKind.Int2,
            PortTypeId.Int3 => PrimitiveTypeKind.Int3,
            PortTypeId.Int4 => PrimitiveTypeKind.Int4,
            PortTypeId.Bool => PrimitiveTypeKind.Bool,
            _ => PrimitiveTypeKind.Float
        };

        return new PrimitiveType(kind, GeneratedLocation);
    }
}
