using System.Text;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Generates HLSL compute shader code from KESL AST.
/// </summary>
/// <remarks>
/// <para>
/// HLSL (High-Level Shader Language) is used for DirectX platforms including Windows.
/// Key differences from GLSL include:
/// </para>
/// <list type="bullet">
/// <item><description>vec2/3/4 → float2/3/4</description></item>
/// <item><description>mat4 → float4x4</description></item>
/// <item><description>StructuredBuffer/RWStructuredBuffer instead of std430 layout</description></item>
/// <item><description>register(t#) for read-only, register(u#) for read-write</description></item>
/// <item><description>[numthreads(x,y,z)] attribute instead of layout(local_size_x = n)</description></item>
/// </list>
/// </remarks>
public sealed class HlslGenerator : IShaderGenerator
{
    private readonly StringBuilder sb = new();
    private int indent;
    private int srvIndex; // Shader Resource View (read-only buffers)
    private int uavIndex; // Unordered Access View (read-write buffers)

    /// <inheritdoc />
    public ShaderBackend Backend => ShaderBackend.HLSL;

    /// <inheritdoc />
    public string FileExtension => "hlsl";

    /// <summary>
    /// Generates HLSL code for a compute shader declaration.
    /// </summary>
    /// <param name="compute">The compute shader AST.</param>
    /// <returns>The generated HLSL code.</returns>
    public string Generate(ComputeDeclaration compute)
    {
        sb.Clear();
        indent = 0;
        srvIndex = 0;
        uavIndex = 0;

        // Generate struct definitions for component data
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }
            GenerateStructDefinition(binding);
        }
        AppendLine();

        // Generate buffer declarations
        foreach (var binding in compute.Query.Bindings)
        {
            if (binding.AccessMode == AccessMode.Without)
            {
                continue;
            }
            GenerateBufferDeclaration(binding);
        }
        AppendLine();

        // Generate constant buffer for params and entity count
        GenerateConstantBuffer(compute);
        AppendLine();

        // Main compute shader function
        AppendLine("[numthreads(64, 1, 1)]");
        AppendLine("void CSMain(uint3 DTid : SV_DispatchThreadID)");
        AppendLine("{");
        indent++;

        // Entity index with bounds check
        AppendLine("uint idx = DTid.x;");
        AppendLine("if (idx >= entityCount) return;");
        AppendLine();

        // Generate execute block statements
        foreach (var stmt in compute.Execute.Body)
        {
            GenerateStatement(stmt, compute.Query.Bindings);
        }

        indent--;
        AppendLine("}");

        return sb.ToString();
    }

    private void GenerateStructDefinition(QueryBinding binding)
    {
        AppendLine($"struct {binding.ComponentName}Data");
        AppendLine("{");
        indent++;

        // Placeholder - actual fields would come from component metadata
        // Generate float X, Y, Z as common case for Position/Velocity
        AppendLine("float X;");
        AppendLine("float Y;");
        AppendLine("float Z;");

        indent--;
        AppendLine("};");
        AppendLine();
    }

    private void GenerateBufferDeclaration(QueryBinding binding)
    {
        if (binding.AccessMode == AccessMode.Read || binding.AccessMode == AccessMode.Optional)
        {
            // Read-only buffer uses StructuredBuffer and t# register
            AppendLine($"StructuredBuffer<{binding.ComponentName}Data> {binding.ComponentName} : register(t{srvIndex});");
            srvIndex++;
        }
        else // Write
        {
            // Read-write buffer uses RWStructuredBuffer and u# register
            AppendLine($"RWStructuredBuffer<{binding.ComponentName}Data> {binding.ComponentName} : register(u{uavIndex});");
            uavIndex++;
        }
    }

    private void GenerateConstantBuffer(ComputeDeclaration compute)
    {
        AppendLine("cbuffer Params : register(b0)");
        AppendLine("{");
        indent++;

        // Generate params
        if (compute.Params != null)
        {
            foreach (var param in compute.Params.Parameters)
            {
                AppendLine($"{ToHlslType(param.Type)} {param.Name};");
            }
        }

        // Entity count is always needed
        AppendLine("uint entityCount;");

        indent--;
        AppendLine("};");
    }

    private void GenerateStatement(Statement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(exprStmt.Expression, bindings));
                sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(assignStmt.Target, bindings));
                sb.Append(" = ");
                sb.Append(GenerateExpression(assignStmt.Value, bindings));
                sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                sb.Append(GenerateExpression(compoundStmt.Target, bindings));
                sb.Append(' ');
                sb.Append(compoundStmt.Operator switch
                {
                    CompoundOperator.PlusEquals => "+=",
                    CompoundOperator.MinusEquals => "-=",
                    CompoundOperator.StarEquals => "*=",
                    CompoundOperator.SlashEquals => "/=",
                    _ => throw new InvalidOperationException()
                });
                sb.Append(' ');
                sb.Append(GenerateExpression(compoundStmt.Value, bindings));
                sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateIfStatement(ifStmt, bindings);
                break;

            case ForStatement forStmt:
                GenerateForStatement(forStmt, bindings);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateStatement(s, bindings);
                }
                indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateIfStatement(IfStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        sb.Append("if (");
        sb.Append(GenerateExpression(stmt.Condition, bindings));
        sb.AppendLine(")");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateStatement(s, bindings);
        }
        indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("}");
            AppendLine("else");
            AppendLine("{");
            indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateStatement(s, bindings);
            }
            indent--;
        }

        AppendLine("}");
    }

    private void GenerateForStatement(ForStatement stmt, IReadOnlyList<QueryBinding> bindings)
    {
        Append(GenerateIndent());
        sb.Append($"for (int {stmt.VariableName} = ");
        sb.Append(GenerateExpression(stmt.Start, bindings));
        sb.Append($"; {stmt.VariableName} < ");
        sb.Append(GenerateExpression(stmt.End, bindings));
        sb.AppendLine($"; {stmt.VariableName}++)");
        AppendLine("{");

        indent++;
        foreach (var s in stmt.Body)
        {
            GenerateStatement(s, bindings);
        }
        indent--;

        AppendLine("}");
    }

    private string GenerateExpression(Expression expr, IReadOnlyList<QueryBinding> bindings)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => IsComponentName(id.Name, bindings)
                ? $"{id.Name}[idx]"
                : id.Name,

            MemberAccessExpression member => GenerateMemberAccess(member, bindings),

            BinaryExpression binary => $"({GenerateExpression(binary.Left, bindings)} {GetBinaryOp(binary.Operator)} {GenerateExpression(binary.Right, bindings)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateExpression(unary.Operand, bindings)})",

            CallExpression call => GenerateCall(call, bindings),

            HasExpression has => $"has_{has.ComponentName}", // Placeholder

            ParenthesizedExpression paren => $"({GenerateExpression(paren.Inner, bindings)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateMemberAccess(MemberAccessExpression member, IReadOnlyList<QueryBinding> bindings)
    {
        if (member.Object is IdentifierExpression id && IsComponentName(id.Name, bindings))
        {
            return $"{id.Name}[idx].{member.MemberName}";
        }
        return $"{GenerateExpression(member.Object, bindings)}.{member.MemberName}";
    }

    private string GenerateCall(CallExpression call, IReadOnlyList<QueryBinding> bindings)
    {
        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateExpression(a, bindings)));
        return $"{funcName}({args})";
    }

    private static string MapFunctionName(string name)
    {
        // HLSL function mapping - most are the same, some differ
        return name switch
        {
            // Math functions - same in HLSL
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
            "step" => "step",
            "smoothstep" => "smoothstep",
            "length" => "length",
            "distance" => "distance",
            "dot" => "dot",
            "cross" => "cross",
            "normalize" => "normalize",
            "reflect" => "reflect",
            "refract" => "refract",

            // GLSL to HLSL differences
            "fract" => "frac", // fract() in GLSL is frac() in HLSL
            "mod" => "fmod",   // mod() in GLSL is fmod() in HLSL
            "mix" => "lerp",   // mix() in GLSL is lerp() in HLSL
            "atan2" => "atan2",

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

    private static string ToHlslType(TypeRef type)
    {
        if (type is PrimitiveType pt)
        {
            return pt.Kind switch
            {
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Float2 => "float2",
                PrimitiveTypeKind.Float3 => "float3",
                PrimitiveTypeKind.Float4 => "float4",
                PrimitiveTypeKind.Int => "int",
                PrimitiveTypeKind.Int2 => "int2",
                PrimitiveTypeKind.Int3 => "int3",
                PrimitiveTypeKind.Int4 => "int4",
                PrimitiveTypeKind.Uint => "uint",
                PrimitiveTypeKind.Bool => "bool",
                PrimitiveTypeKind.Mat4 => "float4x4",
                _ => throw new ArgumentOutOfRangeException(nameof(type), pt.Kind, "Unsupported primitive type")
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
        // HLSL uses 'f' suffix for float literals
        str += "f";
        return str;
    }

    private void AppendLine(string? text = null)
    {
        if (text != null)
        {
            sb.Append(GenerateIndent());
            sb.AppendLine(text);
        }
        else
        {
            sb.AppendLine();
        }
    }

    private void Append(string text)
    {
        sb.Append(text);
    }

    private string GenerateIndent()
    {
        return new string(' ', indent * 4);
    }
}
