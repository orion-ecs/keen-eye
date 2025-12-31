using System.Text;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Generates GLSL compute shader code from KESL AST.
/// </summary>
public sealed class GlslGenerator
{
    private readonly StringBuilder _sb = new();
    private int _indent;
    private int _bindingIndex;

    /// <summary>
    /// Generates GLSL code for a compute shader declaration.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated GLSL code.</returns>
    public string Generate(ComputeDeclaration compute)
    {
        _sb.Clear();
        _indent = 0;
        _bindingIndex = 0;

        // Header
        AppendLine("#version 450");
        AppendLine();

        // Generate buffer declarations for each component binding
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue; // 'without' doesn't need a buffer
            }

            GenerateBufferDeclaration(binding);
        }
        AppendLine();

        // Generate uniform declarations for params
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                AppendLine($"uniform {ToGlslType(param.Type)} {param.Name};");
            }
            AppendLine();
        }

        // Entity count uniform (always needed)
        AppendLine("uniform uint entityCount;");
        AppendLine();

        // Workgroup size
        AppendLine("layout(local_size_x = 64) in;");
        AppendLine();

        // Main function
        AppendLine("void main() {");
        _indent++;

        // Entity index with bounds check
        AppendLine("uint idx = gl_GlobalInvocationID.x;");
        AppendLine("if (idx >= entityCount) return;");
        AppendLine();

        // Generate execute block statements
        foreach (var stmt in compute.Execute.Body)
        {
            GenerateStatement(stmt, compute.Query.Bindings);
        }

        _indent--;
        AppendLine("}");

        return _sb.ToString();
    }

    private void GenerateBufferDeclaration(QueryBinding binding)
    {
        var qualifier = binding.AccessMode == AccessMode.Read ? "readonly " : "";
        var bufferName = $"{binding.ComponentName}Buffer";

        AppendLine($"layout(std430, binding = {_bindingIndex}) {qualifier}buffer {bufferName} {{");
        _indent++;

        // Generate as a struct array - actual fields would come from component metadata
        // For now, generate a placeholder struct
        AppendLine($"{binding.ComponentName}Data {binding.ComponentName}[];");

        _indent--;
        AppendLine("};");
        AppendLine();

        _bindingIndex++;
    }

    private void GenerateStatement(Statement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateExpression(exprStmt.Expression, bindings));
                _sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateExpression(assignStmt.Target, bindings, isLValue: true));
                _sb.Append(" = ");
                _sb.Append(GenerateExpression(assignStmt.Value, bindings));
                _sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateExpression(compoundStmt.Target, bindings, isLValue: true));
                _sb.Append(' ');
                _sb.Append(compoundStmt.Operator switch
                {
                    CompoundOperator.PlusEquals => "+=",
                    CompoundOperator.MinusEquals => "-=",
                    CompoundOperator.StarEquals => "*=",
                    CompoundOperator.SlashEquals => "/=",
                    _ => throw new InvalidOperationException()
                });
                _sb.Append(' ');
                _sb.Append(GenerateExpression(compoundStmt.Value, bindings));
                _sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateIfStatement(ifStmt, bindings);
                break;

            case ForStatement forStmt:
                GenerateForStatement(forStmt, bindings);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                _indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateStatement(s, bindings);
                }
                _indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateIfStatement(IfStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        _sb.Append("if (");
        _sb.Append(GenerateExpression(stmt.Condition, bindings));
        _sb.AppendLine(") {");

        _indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateStatement(s, bindings);
        }
        _indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("} else {");
            _indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateStatement(s, bindings);
            }
            _indent--;
        }

        AppendLine("}");
    }

    private void GenerateForStatement(ForStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        _sb.Append($"for (int {stmt.VariableName} = ");
        _sb.Append(GenerateExpression(stmt.Start, bindings));
        _sb.Append($"; {stmt.VariableName} < ");
        _sb.Append(GenerateExpression(stmt.End, bindings));
        _sb.AppendLine($"; {stmt.VariableName}++) {{");

        _indent++;
        foreach (var s in stmt.Body)
        {
            GenerateStatement(s, bindings);
        }
        _indent--;

        AppendLine("}");
    }

    private string GenerateExpression(Expression expr, IReadOnlyList<QueryBinding> bindings, bool isLValue = false)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => IsComponentName(id.Name, bindings)
                ? $"{id.Name}[idx]"
                : id.Name,

            MemberAccessExpression member => GenerateMemberAccess(member, bindings, isLValue),

            BinaryExpression binary => $"({GenerateExpression(binary.Left, bindings)} {GetBinaryOp(binary.Operator)} {GenerateExpression(binary.Right, bindings)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateExpression(unary.Operand, bindings)})",

            CallExpression call => GenerateCall(call, bindings),

            HasExpression has => $"has_{has.ComponentName}", // Placeholder - would need runtime support

            ParenthesizedExpression paren => $"({GenerateExpression(paren.Inner, bindings)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateMemberAccess(MemberAccessExpression member, IReadOnlyList<QueryBinding> bindings, bool isLValue)
    {
        // Check if the base is a component name
        if (member.Object is IdentifierExpression id && IsComponentName(id.Name, bindings))
        {
            // Component.field -> Component[idx].field
            return $"{id.Name}[idx].{member.MemberName}";
        }

        // Regular member access
        return $"{GenerateExpression(member.Object, bindings)}.{member.MemberName}";
    }

    private string GenerateCall(CallExpression call, IReadOnlyList<QueryBinding> bindings)
    {
        // Map common math functions to GLSL equivalents
        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateExpression(a, bindings)));
        return $"{funcName}({args})";
    }

    private static string MapFunctionName(string name)
    {
        // Most math functions have the same name in GLSL
        return name switch
        {
            "sqrt" => "sqrt",
            "abs" => "abs",
            "min" => "min",
            "max" => "max",
            "clamp" => "clamp",
            "sin" => "sin",
            "cos" => "cos",
            "tan" => "tan",
            "asin" => "asin",
            "acos" => "acos",
            "atan" => "atan",
            "pow" => "pow",
            "exp" => "exp",
            "log" => "log",
            "floor" => "floor",
            "ceil" => "ceil",
            "round" => "round",
            "fract" => "fract",
            "mod" => "mod",
            "mix" => "mix",
            "step" => "step",
            "smoothstep" => "smoothstep",
            "length" => "length",
            "distance" => "distance",
            "dot" => "dot",
            "cross" => "cross",
            "normalize" => "normalize",
            "reflect" => "reflect",
            "refract" => "refract",
            _ => name
        };
    }

    private static bool IsComponentName(string name, IReadOnlyList<QueryBinding> bindings)
    {
        return bindings.Any(b => b.ComponentName == name && b.AccessMode != AccessMode.Without);
    }

    private static string GetBinaryOp(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Less => "<",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.Greater => ">",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.And => "&&",
        BinaryOperator.Or => "||",
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

    private static string GetUnaryOp(UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "-",
        UnaryOperator.Not => "!",
        _ => throw new ArgumentOutOfRangeException(nameof(op))
    };

    private static string ToGlslType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Float2 => "vec2",
                PrimitiveTypeKind.Float3 => "vec3",
                PrimitiveTypeKind.Float4 => "vec4",
                PrimitiveTypeKind.Int => "int",
                PrimitiveTypeKind.Int2 => "ivec2",
                PrimitiveTypeKind.Int3 => "ivec3",
                PrimitiveTypeKind.Int4 => "ivec4",
                PrimitiveTypeKind.Uint => "uint",
                PrimitiveTypeKind.Bool => "bool",
                PrimitiveTypeKind.Mat4 => "mat4",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        throw new NotSupportedException($"Type {type.GetType().Name} not supported");
    }

    private static string FormatFloat(float value)
    {
        var str = value.ToString("G9");
        if (!str.Contains('.') && !str.Contains('E') && !str.Contains('e'))
        {
            str += ".0";
        }
        return str;
    }

    private void AppendLine(string? text = null)
    {
        if (text != null)
        {
            _sb.Append(GenerateIndent());
            _sb.AppendLine(text);
        }
        else
        {
            _sb.AppendLine();
        }
    }

    private void Append(string text)
    {
        _sb.Append(text);
    }

    private string GenerateIndent()
    {
        return new string(' ', _indent * 4);
    }
}
