using KeenEyes.Graph.Kesl.Compiler;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Preview;

/// <summary>
/// Compiles and executes shaders on preview entity data.
/// </summary>
/// <remarks>
/// <para>
/// The ShaderExecutor interprets compiled shader AST to modify preview entity
/// component values. This provides a CPU-side simulation of shader execution
/// for visualization purposes in the editor.
/// </para>
/// <para>
/// The executor handles common statement types (assignment, compound assignment,
/// if/else, for loops) and expression types (binary operations, member access,
/// function calls, literals, identifiers).
/// </para>
/// </remarks>
public sealed class ShaderExecutor
{
    private readonly KeslGraphCompiler compiler = new();

    private ComputeDeclaration? currentAst;
    private IReadOnlyList<QueryBinding> currentBindings = Array.Empty<QueryBinding>();
    private string lastError = "";

    /// <summary>
    /// Gets the last error message from compilation.
    /// </summary>
    public string LastError => lastError;

    /// <summary>
    /// Gets whether the last compilation had an error.
    /// </summary>
    public bool HasError => !string.IsNullOrEmpty(lastError);

    /// <summary>
    /// Gets the current query bindings from the compiled shader.
    /// </summary>
    public IReadOnlyList<QueryBinding> CurrentBindings => currentBindings;

    /// <summary>
    /// Compiles the shader from the graph canvas.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>True if compilation succeeded, false otherwise.</returns>
    public bool Compile(Entity canvas, IWorld world)
    {
        try
        {
            lastError = "";
            var result = compiler.Compile(canvas, world);

            if (!result.IsSuccess || result.Declaration is null)
            {
                lastError = result.Errors.Count > 0
                    ? string.Join("\n", result.Errors.Select(e => e.Message))
                    : "Compilation failed with unknown error";
                currentAst = null;
                currentBindings = Array.Empty<QueryBinding>();
                return false;
            }

            currentAst = result.Declaration;
            currentBindings = result.Declaration.Query.Bindings;
            return true;
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            currentAst = null;
            currentBindings = Array.Empty<QueryBinding>();
            return false;
        }
    }

    /// <summary>
    /// Executes the compiled shader on preview entities.
    /// </summary>
    /// <remarks>
    /// Modifies entity components in-place based on the shader's execute block.
    /// The deltaTime parameter is available to the shader as a built-in parameter.
    /// </remarks>
    /// <param name="entityManager">The preview entity manager containing entities to process.</param>
    /// <param name="deltaTime">The delta time value available to the shader.</param>
    public void Execute(PreviewEntityManager entityManager, float deltaTime)
    {
        if (currentAst is null)
        {
            return;
        }

        // Build parameters dictionary
        var parameters = new Dictionary<string, float>
        {
            ["deltaTime"] = deltaTime,
            ["dt"] = deltaTime
        };

        // Add user-defined parameters with default values
        if (currentAst.Params is not null)
        {
            foreach (var param in currentAst.Params.Parameters)
            {
                if (!parameters.ContainsKey(param.Name))
                {
                    parameters[param.Name] = GetDefaultParameterValue(param.Type);
                }
            }
        }

        // Execute shader on each preview entity
        foreach (var entity in entityManager.Entities)
        {
            var context = new ExecutionContext(entity, parameters);
            ExecuteBlock(currentAst.Execute.Body, context);
        }
    }

    private void ExecuteBlock(IReadOnlyList<Statement> statements, ExecutionContext context)
    {
        foreach (var statement in statements)
        {
            ExecuteStatement(statement, context);
        }
    }

    private void ExecuteStatement(Statement statement, ExecutionContext context)
    {
        switch (statement)
        {
            case AssignmentStatement assign:
                ExecuteAssignment(assign, context);
                break;

            case CompoundAssignmentStatement compound:
                ExecuteCompoundAssignment(compound, context);
                break;

            case IfStatement ifStmt:
                ExecuteIf(ifStmt, context);
                break;

            case ForStatement forStmt:
                ExecuteFor(forStmt, context);
                break;

            case ExpressionStatement exprStmt:
                // Expression statements are evaluated for side effects only
                EvaluateExpression(exprStmt.Expression, context);
                break;

            case BlockStatement block:
                ExecuteBlock(block.Statements, context);
                break;
        }
    }

    private void ExecuteAssignment(AssignmentStatement assign, ExecutionContext context)
    {
        var value = EvaluateExpression(assign.Value, context);
        SetValue(assign.Target, value, context);
    }

    private void ExecuteCompoundAssignment(CompoundAssignmentStatement compound, ExecutionContext context)
    {
        var currentValue = EvaluateExpression(compound.Target, context);
        var operandValue = EvaluateExpression(compound.Value, context);

        var newValue = compound.Operator switch
        {
            CompoundOperator.PlusEquals => currentValue + operandValue,
            CompoundOperator.MinusEquals => currentValue - operandValue,
            CompoundOperator.StarEquals => currentValue * operandValue,
            CompoundOperator.SlashEquals => IsTrue(operandValue) ? currentValue / operandValue : 0f,
            _ => currentValue
        };

        SetValue(compound.Target, newValue, context);
    }

    private void ExecuteIf(IfStatement ifStmt, ExecutionContext context)
    {
        var conditionValue = EvaluateExpression(ifStmt.Condition, context);

        if (IsTrue(conditionValue))
        {
            ExecuteBlock(ifStmt.ThenBranch, context);
        }
        else if (ifStmt.ElseBranch is not null)
        {
            ExecuteBlock(ifStmt.ElseBranch, context);
        }
    }

    private void ExecuteFor(ForStatement forStmt, ExecutionContext context)
    {
        var startValue = (int)EvaluateExpression(forStmt.Start, context);
        var endValue = (int)EvaluateExpression(forStmt.End, context);

        for (int i = startValue; i < endValue; i++)
        {
            context.SetVariable(forStmt.VariableName, i);
            ExecuteBlock(forStmt.Body, context);
        }
    }

    private float EvaluateExpression(Expression expression, ExecutionContext context)
    {
        return expression switch
        {
            FloatLiteralExpression floatLit => floatLit.Value,
            IntLiteralExpression intLit => intLit.Value,
            BoolLiteralExpression boolLit => boolLit.Value ? 1f : 0f,
            IdentifierExpression ident => context.GetVariable(ident.Name),
            MemberAccessExpression member => EvaluateMemberAccess(member, context),
            BinaryExpression binary => EvaluateBinary(binary, context),
            UnaryExpression unary => EvaluateUnary(unary, context),
            CallExpression call => EvaluateCall(call, context),
            ParenthesizedExpression paren => EvaluateExpression(paren.Inner, context),
            HasExpression has => context.Entity.Components.ContainsKey(has.ComponentName) ? 1f : 0f,
            _ => 0f
        };
    }

    private static float EvaluateMemberAccess(MemberAccessExpression member, ExecutionContext context)
    {
        // Handle Component.Field pattern
        if (member.Object is IdentifierExpression componentIdent
            && context.Entity.Components.TryGetValue(componentIdent.Name, out var component)
            && component.Fields.TryGetValue(member.MemberName, out var value))
        {
            return value;
        }

        return 0f;
    }

    private float EvaluateBinary(BinaryExpression binary, ExecutionContext context)
    {
        var left = EvaluateExpression(binary.Left, context);
        var right = EvaluateExpression(binary.Right, context);

        return binary.Operator switch
        {
            BinaryOperator.Add => left + right,
            BinaryOperator.Subtract => left - right,
            BinaryOperator.Multiply => left * right,
            BinaryOperator.Divide => IsTrue(right) ? left / right : 0f,
            BinaryOperator.Less => left < right ? 1f : 0f,
            BinaryOperator.LessEqual => left <= right ? 1f : 0f,
            BinaryOperator.Greater => left > right ? 1f : 0f,
            BinaryOperator.GreaterEqual => left >= right ? 1f : 0f,
            BinaryOperator.Equal => Math.Abs(left - right) < float.Epsilon ? 1f : 0f,
            BinaryOperator.NotEqual => Math.Abs(left - right) >= float.Epsilon ? 1f : 0f,
            BinaryOperator.And => IsTrue(left) && IsTrue(right) ? 1f : 0f,
            BinaryOperator.Or => IsTrue(left) || IsTrue(right) ? 1f : 0f,
            _ => 0f
        };
    }

    private float EvaluateUnary(UnaryExpression unary, ExecutionContext context)
    {
        var operand = EvaluateExpression(unary.Operand, context);

        return unary.Operator switch
        {
            UnaryOperator.Negate => -operand,
            UnaryOperator.Not => IsTrue(operand) ? 0f : 1f,
            _ => operand
        };
    }

    private float EvaluateCall(CallExpression call, ExecutionContext context)
    {
        var args = call.Arguments.Select(a => EvaluateExpression(a, context)).ToArray();

        return call.FunctionName.ToLowerInvariant() switch
        {
            "sqrt" => args.Length >= 1 ? MathF.Sqrt(args[0]) : 0f,
            "abs" => args.Length >= 1 ? MathF.Abs(args[0]) : 0f,
            "sin" => args.Length >= 1 ? MathF.Sin(args[0]) : 0f,
            "cos" => args.Length >= 1 ? MathF.Cos(args[0]) : 0f,
            "tan" => args.Length >= 1 ? MathF.Tan(args[0]) : 0f,
            "asin" => args.Length >= 1 ? MathF.Asin(args[0]) : 0f,
            "acos" => args.Length >= 1 ? MathF.Acos(args[0]) : 0f,
            "atan" => args.Length >= 1 ? MathF.Atan(args[0]) : 0f,
            "atan2" => args.Length >= 2 ? MathF.Atan2(args[0], args[1]) : 0f,
            "pow" => args.Length >= 2 ? MathF.Pow(args[0], args[1]) : 0f,
            "exp" => args.Length >= 1 ? MathF.Exp(args[0]) : 0f,
            "log" => args.Length >= 1 ? MathF.Log(args[0]) : 0f,
            "floor" => args.Length >= 1 ? MathF.Floor(args[0]) : 0f,
            "ceil" => args.Length >= 1 ? MathF.Ceiling(args[0]) : 0f,
            "round" => args.Length >= 1 ? MathF.Round(args[0]) : 0f,
            "fract" => args.Length >= 1 ? args[0] - MathF.Floor(args[0]) : 0f,
            "mod" => args.Length >= 2 && IsTrue(args[1]) ? args[0] % args[1] : 0f,
            "min" => args.Length >= 2 ? MathF.Min(args[0], args[1]) : (args.Length >= 1 ? args[0] : 0f),
            "max" => args.Length >= 2 ? MathF.Max(args[0], args[1]) : (args.Length >= 1 ? args[0] : 0f),
            "clamp" => args.Length >= 3 ? MathF.Min(MathF.Max(args[0], args[1]), args[2]) : 0f,
            "mix" or "lerp" => args.Length >= 3 ? args[0] + (args[1] - args[0]) * args[2] : 0f,
            "step" => args.Length >= 2 ? (args[1] >= args[0] ? 1f : 0f) : 0f,
            "smoothstep" => args.Length >= 3 ? SmoothStep(args[0], args[1], args[2]) : 0f,
            "length" => args.Length >= 1 ? MathF.Abs(args[0]) : 0f,
            "normalize" => args.Length >= 1 && IsTrue(args[0]) ? args[0] / MathF.Abs(args[0]) : 0f,
            _ => 0f
        };
    }

    private static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = MathF.Min(MathF.Max((x - edge0) / (edge1 - edge0), 0f), 1f);
        return t * t * (3f - 2f * t);
    }

    private static void SetValue(Expression target, float value, ExecutionContext context)
    {
        switch (target)
        {
            case IdentifierExpression ident:
                context.SetVariable(ident.Name, value);
                break;

            case MemberAccessExpression member when member.Object is IdentifierExpression componentIdent:
                if (context.Entity.Components.TryGetValue(componentIdent.Name, out var component))
                {
                    component.Fields[member.MemberName] = value;
                }
                break;
        }
    }

    private static bool IsTrue(float value) => Math.Abs(value) > float.Epsilon;

    private static float GetDefaultParameterValue(TypeRef typeRef)
    {
        // Default to 0 for all parameter types
        return 0f;
    }

    /// <summary>
    /// Execution context for shader interpretation.
    /// </summary>
    private sealed class ExecutionContext
    {
        private readonly Dictionary<string, float> variables = [];

        public PreviewEntity Entity { get; }

        public ExecutionContext(PreviewEntity entity, Dictionary<string, float> parameters)
        {
            Entity = entity;

            // Copy parameters to variables
            foreach (var (name, value) in parameters)
            {
                variables[name] = value;
            }
        }

        public float GetVariable(string name)
        {
            if (variables.TryGetValue(name, out var value))
            {
                return value;
            }

            // Check if it's a component reference without field
            // (shouldn't happen with proper AST, but handle gracefully)
            return 0f;
        }

        public void SetVariable(string name, float value)
        {
            variables[name] = value;
        }
    }
}
