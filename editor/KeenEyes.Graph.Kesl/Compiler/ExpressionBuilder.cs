using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler.Lexing;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Compiler;

/// <summary>
/// Builds AST expressions from graph nodes.
/// </summary>
/// <remarks>
/// <para>
/// The expression builder converts individual graph nodes into KESL AST expressions.
/// It handles the mapping from visual node types to their corresponding AST representations.
/// </para>
/// </remarks>
public sealed class ExpressionBuilder
{
    private static readonly SourceLocation GeneratedLocation = new("graph://generated", 0, 0);

    /// <summary>
    /// Builds an expression for a node.
    /// </summary>
    /// <param name="node">The node entity.</param>
    /// <param name="nodeTypeId">The node type ID.</param>
    /// <param name="canvas">The graph canvas.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <param name="context">The compilation context.</param>
    /// <returns>The AST expression, or null if the node doesn't produce an expression.</returns>
    public Expression? BuildExpression(
        Entity node,
        int nodeTypeId,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        return nodeTypeId switch
        {
            // Binary math operators
            KeslNodeIds.Add => BuildBinaryOp(node, BinaryOperator.Add, canvas, world, context),
            KeslNodeIds.Subtract => BuildBinaryOp(node, BinaryOperator.Subtract, canvas, world, context),
            KeslNodeIds.Multiply => BuildBinaryOp(node, BinaryOperator.Multiply, canvas, world, context),
            KeslNodeIds.Divide => BuildBinaryOp(node, BinaryOperator.Divide, canvas, world, context),

            // Math functions
            KeslNodeIds.Min => BuildCall(node, "min", 2, canvas, world, context),
            KeslNodeIds.Max => BuildCall(node, "max", 2, canvas, world, context),
            KeslNodeIds.Pow => BuildCall(node, "pow", 2, canvas, world, context),
            KeslNodeIds.Modulo => BuildCall(node, "mod", 2, canvas, world, context),
            KeslNodeIds.Atan2 => BuildCall(node, "atan2", 2, canvas, world, context),
            KeslNodeIds.Step => BuildCall(node, "step", 2, canvas, world, context),
            KeslNodeIds.Abs => BuildCall(node, "abs", 1, canvas, world, context),
            KeslNodeIds.Floor => BuildCall(node, "floor", 1, canvas, world, context),
            KeslNodeIds.Ceil => BuildCall(node, "ceil", 1, canvas, world, context),
            KeslNodeIds.Round => BuildCall(node, "round", 1, canvas, world, context),
            KeslNodeIds.Sign => BuildCall(node, "sign", 1, canvas, world, context),
            KeslNodeIds.Frac => BuildCall(node, "fract", 1, canvas, world, context),
            KeslNodeIds.Sqrt => BuildCall(node, "sqrt", 1, canvas, world, context),
            KeslNodeIds.Exp => BuildCall(node, "exp", 1, canvas, world, context),
            KeslNodeIds.Log => BuildCall(node, "log", 1, canvas, world, context),
            KeslNodeIds.Log2 => BuildCall(node, "log2", 1, canvas, world, context),
            KeslNodeIds.Sin => BuildCall(node, "sin", 1, canvas, world, context),
            KeslNodeIds.Cos => BuildCall(node, "cos", 1, canvas, world, context),
            KeslNodeIds.Tan => BuildCall(node, "tan", 1, canvas, world, context),
            KeslNodeIds.Asin => BuildCall(node, "asin", 1, canvas, world, context),
            KeslNodeIds.Acos => BuildCall(node, "acos", 1, canvas, world, context),
            KeslNodeIds.Atan => BuildCall(node, "atan", 1, canvas, world, context),
            KeslNodeIds.Negate => BuildUnaryOp(node, UnaryOperator.Negate, canvas, world, context),

            // Interpolation
            KeslNodeIds.Clamp => BuildCall(node, "clamp", 3, canvas, world, context),
            KeslNodeIds.Lerp => BuildCall(node, "mix", 3, canvas, world, context),
            KeslNodeIds.Smoothstep => BuildCall(node, "smoothstep", 3, canvas, world, context),

            // Vector construction
            KeslNodeIds.ConstructVector2 => BuildCall(node, "vec2", 2, canvas, world, context),
            KeslNodeIds.ConstructVector3 => BuildCall(node, "vec3", 3, canvas, world, context),
            KeslNodeIds.ConstructVector4 => BuildCall(node, "vec4", 4, canvas, world, context),

            // Vector operations
            KeslNodeIds.Normalize => BuildCall(node, "normalize", 1, canvas, world, context),
            KeslNodeIds.Length => BuildCall(node, "length", 1, canvas, world, context),
            KeslNodeIds.LengthSquared => BuildLengthSquared(node, canvas, world, context),
            KeslNodeIds.DotProduct => BuildCall(node, "dot", 2, canvas, world, context),
            KeslNodeIds.CrossProduct => BuildCall(node, "cross", 2, canvas, world, context),
            KeslNodeIds.Distance => BuildCall(node, "distance", 2, canvas, world, context),
            KeslNodeIds.DistanceSquared => BuildDistanceSquared(node, canvas, world, context),
            KeslNodeIds.Reflect => BuildCall(node, "reflect", 2, canvas, world, context),

            // Logic
            KeslNodeIds.And => BuildBinaryOp(node, BinaryOperator.And, canvas, world, context),
            KeslNodeIds.Or => BuildBinaryOp(node, BinaryOperator.Or, canvas, world, context),
            KeslNodeIds.Not => BuildUnaryOp(node, UnaryOperator.Not, canvas, world, context),
            KeslNodeIds.Compare => BuildCompare(node, canvas, world, context),
            KeslNodeIds.Select => BuildSelect(node, canvas, world, context),

            // Constants
            KeslNodeIds.FloatConstant => BuildFloatConstant(node, world),
            KeslNodeIds.IntConstant => BuildIntConstant(node, world),
            KeslNodeIds.BoolConstant => BuildBoolConstant(node, world),

            // Variables
            KeslNodeIds.GetVariable => BuildGetVariable(node, world, context),

            _ => null
        };
    }

    private BinaryExpression BuildBinaryOp(
        Entity node,
        BinaryOperator op,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        var left = GetInputExpression(node, 0, canvas, world, context);
        var right = GetInputExpression(node, 1, canvas, world, context);

        return new BinaryExpression(left, op, right, GeneratedLocation);
    }

    private UnaryExpression BuildUnaryOp(
        Entity node,
        UnaryOperator op,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        var operand = GetInputExpression(node, 0, canvas, world, context);
        return new UnaryExpression(op, operand, GeneratedLocation);
    }

    private CallExpression BuildCall(
        Entity node,
        string functionName,
        int argCount,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        var args = new List<Expression>();
        for (int i = 0; i < argCount; i++)
        {
            args.Add(GetInputExpression(node, i, canvas, world, context));
        }

        return new CallExpression(functionName, args, GeneratedLocation);
    }

    private Expression BuildLengthSquared(
        Entity node,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        // length squared = dot(v, v)
        var input = GetInputExpression(node, 0, canvas, world, context);
        return new CallExpression("dot", [input, input], GeneratedLocation);
    }

    private Expression BuildDistanceSquared(
        Entity node,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        // distance squared = length squared of (a - b)
        var a = GetInputExpression(node, 0, canvas, world, context);
        var b = GetInputExpression(node, 1, canvas, world, context);
        var diff = new BinaryExpression(a, BinaryOperator.Subtract, b, GeneratedLocation);
        return new CallExpression("dot", [diff, diff], GeneratedLocation);
    }

    private Expression BuildCompare(
        Entity node,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        var left = GetInputExpression(node, 0, canvas, world, context);
        var right = GetInputExpression(node, 1, canvas, world, context);

        var compareData = world.Has<CompareNodeData>(node)
            ? world.Get<CompareNodeData>(node)
            : CompareNodeData.Default;

        var op = compareData.Operator switch
        {
            ComparisonOperator.Equal => BinaryOperator.Equal,
            ComparisonOperator.NotEqual => BinaryOperator.NotEqual,
            ComparisonOperator.LessThan => BinaryOperator.Less,
            ComparisonOperator.LessThanOrEqual => BinaryOperator.LessEqual,
            ComparisonOperator.GreaterThan => BinaryOperator.Greater,
            ComparisonOperator.GreaterThanOrEqual => BinaryOperator.GreaterEqual,
            _ => BinaryOperator.Equal
        };

        return new BinaryExpression(left, op, right, GeneratedLocation);
    }

    private Expression BuildSelect(
        Entity node,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        // GLSL doesn't have ternary in the same way - use mix with condition
        // This is a simplification; proper ternary would need different handling
        var condition = GetInputExpression(node, 0, canvas, world, context);
        var trueValue = GetInputExpression(node, 1, canvas, world, context);
        var falseValue = GetInputExpression(node, 2, canvas, world, context);

        // mix(falseValue, trueValue, float(condition))
        return new CallExpression("mix",
            [falseValue, trueValue, new CallExpression("float", [condition], GeneratedLocation)],
            GeneratedLocation);
    }

    private static Expression BuildFloatConstant(Entity node, IWorld world)
    {
        var data = world.Has<FloatConstantData>(node)
            ? world.Get<FloatConstantData>(node)
            : FloatConstantData.Default;

        return new FloatLiteralExpression(data.Value, GeneratedLocation);
    }

    private static Expression BuildIntConstant(Entity node, IWorld world)
    {
        var data = world.Has<IntConstantData>(node)
            ? world.Get<IntConstantData>(node)
            : IntConstantData.Default;

        return new IntLiteralExpression(data.Value, GeneratedLocation);
    }

    private static Expression BuildBoolConstant(Entity node, IWorld world)
    {
        var data = world.Has<BoolConstantData>(node)
            ? world.Get<BoolConstantData>(node)
            : BoolConstantData.Default;

        return new BoolLiteralExpression(data.Value, GeneratedLocation);
    }

    private static Expression BuildGetVariable(Entity node, IWorld world, CompilationContext context)
    {
        var data = world.Has<VariableNodeData>(node)
            ? world.Get<VariableNodeData>(node)
            : VariableNodeData.Default;

        return new IdentifierExpression(data.VariableName, GeneratedLocation);
    }

    private Expression GetInputExpression(
        Entity node,
        int portIndex,
        Entity canvas,
        IWorld world,
        CompilationContext context)
    {
        // Find what's connected to this input
        foreach (var (sourceNode, _) in GraphTraverser.GetInputConnections(node, portIndex, canvas, world))
        {
            // If the source node has a variable assigned, use that
            var varName = context.GetNodeVariable(sourceNode);
            if (varName is not null)
            {
                return new IdentifierExpression(varName, GeneratedLocation);
            }

            // Otherwise, recursively build the expression
            var nodeData = world.Get<GraphNode>(sourceNode);
            var expr = BuildExpression(sourceNode, nodeData.NodeTypeId, canvas, world, context);
            if (expr is not null)
            {
                return expr;
            }
        }

        // No connection - return default (0)
        return new FloatLiteralExpression(0f, GeneratedLocation);
    }
}
