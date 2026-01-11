using System.Text;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Shaders.Compiler.CodeGen;

/// <summary>
/// Generates GLSL compute shader code from KESL AST.
/// </summary>
public sealed class GlslGenerator : IShaderGenerator
{
    /// <inheritdoc />
    public ShaderBackend Backend => ShaderBackend.GLSL;

    /// <inheritdoc />
    public string FileExtension => "glsl";

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

    /// <summary>
    /// Generates GLSL code for a vertex shader declaration.
    /// </summary>
    /// <param name="vertex">The vertex shader AST.</param>
    /// <returns>The generated GLSL code.</returns>
    public string Generate(VertexDeclaration vertex)
    {
        _sb.Clear();
        _indent = 0;

        // Header
        AppendLine("#version 450");
        AppendLine();

        // Generate input attributes (from vertex buffer)
        foreach (var attr in vertex.Inputs.Attributes)
        {
            var location = attr.LocationIndex.HasValue ? $"layout(location = {attr.LocationIndex.Value}) " : "";
            AppendLine($"{location}in {ToGlslType(attr.Type)} {attr.Name};");
        }
        AppendLine();

        // Generate output interface block
        if (vertex.Outputs.Attributes.Count > 0)
        {
            AppendLine("out VS_OUT {");
            _indent++;
            foreach (var attr in vertex.Outputs.Attributes)
            {
                AppendLine($"{ToGlslType(attr.Type)} {attr.Name};");
            }
            _indent--;
            AppendLine("} vs_out;");
            AppendLine();
        }

        // Generate uniform declarations for params
        if (vertex.Params != null)
        {
            foreach (var param in vertex.Params.Parameters)
            {
                AppendLine($"uniform {ToGlslType(param.Type)} {param.Name};");
            }
            AppendLine();
        }

        // Main function
        AppendLine("void main() {");
        _indent++;

        // Generate execute block statements
        foreach (var stmt in vertex.Execute.Body)
        {
            GenerateVertexStatement(stmt, vertex.Inputs.Attributes, vertex.Outputs.Attributes);
        }

        _indent--;
        AppendLine("}");

        return _sb.ToString();
    }

    /// <summary>
    /// Generates GLSL code for a fragment shader declaration.
    /// </summary>
    /// <param name="fragment">The fragment shader AST.</param>
    /// <returns>The generated GLSL code.</returns>
    public string Generate(FragmentDeclaration fragment)
    {
        _sb.Clear();
        _indent = 0;

        // Header
        AppendLine("#version 450");
        AppendLine();

        // Generate input interface block (from vertex shader)
        if (fragment.Inputs.Attributes.Count > 0)
        {
            AppendLine("in VS_OUT {");
            _indent++;
            foreach (var attr in fragment.Inputs.Attributes)
            {
                AppendLine($"{ToGlslType(attr.Type)} {attr.Name};");
            }
            _indent--;
            AppendLine("} fs_in;");
            AppendLine();
        }

        // Generate output attributes
        foreach (var attr in fragment.Outputs.Attributes)
        {
            var location = attr.LocationIndex.HasValue ? $"layout(location = {attr.LocationIndex.Value}) " : "";
            AppendLine($"{location}out {ToGlslType(attr.Type)} {attr.Name};");
        }
        AppendLine();

        // Generate uniform declarations for params
        if (fragment.Params != null)
        {
            foreach (var param in fragment.Params.Parameters)
            {
                AppendLine($"uniform {ToGlslType(param.Type)} {param.Name};");
            }
            AppendLine();
        }

        // Main function
        AppendLine("void main() {");
        _indent++;

        // Generate execute block statements
        foreach (var stmt in fragment.Execute.Body)
        {
            GenerateFragmentStatement(stmt, fragment.Inputs.Attributes, fragment.Outputs.Attributes);
        }

        _indent--;
        AppendLine("}");

        return _sb.ToString();
    }

    private void GenerateVertexStatement(Statement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateVertexExpression(exprStmt.Expression, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateVertexExpression(assignStmt.Target, inputs, outputs));
                _sb.Append(" = ");
                _sb.Append(GenerateVertexExpression(assignStmt.Value, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateVertexExpression(compoundStmt.Target, inputs, outputs));
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
                _sb.Append(GenerateVertexExpression(compoundStmt.Value, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateVertexIfStatement(ifStmt, inputs, outputs);
                break;

            case ForStatement forStmt:
                GenerateVertexForStatement(forStmt, inputs, outputs);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                _indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateVertexStatement(s, inputs, outputs);
                }
                _indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateFragmentStatement(Statement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        switch (stmt)
        {
            case ExpressionStatement exprStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateFragmentExpression(exprStmt.Expression, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case AssignmentStatement assignStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateFragmentExpression(assignStmt.Target, inputs, outputs));
                _sb.Append(" = ");
                _sb.Append(GenerateFragmentExpression(assignStmt.Value, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateFragmentExpression(compoundStmt.Target, inputs, outputs));
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
                _sb.Append(GenerateFragmentExpression(compoundStmt.Value, inputs, outputs));
                _sb.AppendLine(";");
                break;

            case IfStatement ifStmt:
                GenerateFragmentIfStatement(ifStmt, inputs, outputs);
                break;

            case ForStatement forStmt:
                GenerateFragmentForStatement(forStmt, inputs, outputs);
                break;

            case BlockStatement blockStmt:
                AppendLine("{");
                _indent++;
                foreach (var s in blockStmt.Statements)
                {
                    GenerateFragmentStatement(s, inputs, outputs);
                }
                _indent--;
                AppendLine("}");
                break;
        }
    }

    private void GenerateVertexIfStatement(IfStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        _sb.Append("if (");
        _sb.Append(GenerateVertexExpression(stmt.Condition, inputs, outputs));
        _sb.AppendLine(") {");

        _indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateVertexStatement(s, inputs, outputs);
        }
        _indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("} else {");
            _indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateVertexStatement(s, inputs, outputs);
            }
            _indent--;
        }

        AppendLine("}");
    }

    private void GenerateFragmentIfStatement(IfStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        _sb.Append("if (");
        _sb.Append(GenerateFragmentExpression(stmt.Condition, inputs, outputs));
        _sb.AppendLine(") {");

        _indent++;
        foreach (var s in stmt.ThenBranch)
        {
            GenerateFragmentStatement(s, inputs, outputs);
        }
        _indent--;

        if (stmt.ElseBranch != null)
        {
            AppendLine("} else {");
            _indent++;
            foreach (var s in stmt.ElseBranch)
            {
                GenerateFragmentStatement(s, inputs, outputs);
            }
            _indent--;
        }

        AppendLine("}");
    }

    private void GenerateVertexForStatement(ForStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        _sb.Append($"for (int {stmt.VariableName} = ");
        _sb.Append(GenerateVertexExpression(stmt.Start, inputs, outputs));
        _sb.Append($"; {stmt.VariableName} < ");
        _sb.Append(GenerateVertexExpression(stmt.End, inputs, outputs));
        _sb.AppendLine($"; {stmt.VariableName}++) {{");

        _indent++;
        foreach (var s in stmt.Body)
        {
            GenerateVertexStatement(s, inputs, outputs);
        }
        _indent--;

        AppendLine("}");
    }

    private void GenerateFragmentForStatement(ForStatement stmt, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        Append(GenerateIndent());
        _sb.Append($"for (int {stmt.VariableName} = ");
        _sb.Append(GenerateFragmentExpression(stmt.Start, inputs, outputs));
        _sb.Append($"; {stmt.VariableName} < ");
        _sb.Append(GenerateFragmentExpression(stmt.End, inputs, outputs));
        _sb.AppendLine($"; {stmt.VariableName}++) {{");

        _indent++;
        foreach (var s in stmt.Body)
        {
            GenerateFragmentStatement(s, inputs, outputs);
        }
        _indent--;

        AppendLine("}");
    }

    private string GenerateVertexExpression(Expression expr, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => TransformVertexIdentifier(id.Name, inputs, outputs),

            MemberAccessExpression member => GenerateVertexMemberAccess(member, inputs, outputs),

            BinaryExpression binary => $"({GenerateVertexExpression(binary.Left, inputs, outputs)} {GetBinaryOp(binary.Operator)} {GenerateVertexExpression(binary.Right, inputs, outputs)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateVertexExpression(unary.Operand, inputs, outputs)})",

            CallExpression call => GenerateVertexCall(call, inputs, outputs),

            HasExpression => throw new NotSupportedException("'has' expression not supported in vertex shaders"),

            ParenthesizedExpression paren => $"({GenerateVertexExpression(paren.Inner, inputs, outputs)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateFragmentExpression(Expression expr, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        return expr switch
        {
            IntLiteralExpression intLit => intLit.Value.ToString(),
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",

            IdentifierExpression id => TransformFragmentIdentifier(id.Name, inputs, outputs),

            MemberAccessExpression member => GenerateFragmentMemberAccess(member, inputs, outputs),

            BinaryExpression binary => $"({GenerateFragmentExpression(binary.Left, inputs, outputs)} {GetBinaryOp(binary.Operator)} {GenerateFragmentExpression(binary.Right, inputs, outputs)})",

            UnaryExpression unary => $"{GetUnaryOp(unary.Operator)}({GenerateFragmentExpression(unary.Operand, inputs, outputs)})",

            CallExpression call => GenerateFragmentCall(call, inputs, outputs),

            HasExpression => throw new NotSupportedException("'has' expression not supported in fragment shaders"),

            ParenthesizedExpression paren => $"({GenerateFragmentExpression(paren.Inner, inputs, outputs)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private static string TransformVertexIdentifier(string name, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if it's an output variable - transform to vs_out.name
        if (outputs.Any(o => o.Name == name))
        {
            return $"vs_out.{name}";
        }

        // Input variables and other identifiers remain as-is
        return name;
    }

    private static string TransformFragmentIdentifier(string name, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if it's an input variable from vertex shader - transform to fs_in.name
        if (inputs.Any(i => i.Name == name))
        {
            return $"fs_in.{name}";
        }

        // Output variables and other identifiers remain as-is
        return name;
    }

    private string GenerateVertexMemberAccess(MemberAccessExpression member, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if the base is an output variable
        if (member.Object is IdentifierExpression id && outputs.Any(o => o.Name == id.Name))
        {
            return $"vs_out.{id.Name}.{member.MemberName}";
        }

        return $"{GenerateVertexExpression(member.Object, inputs, outputs)}.{member.MemberName}";
    }

    private string GenerateFragmentMemberAccess(MemberAccessExpression member, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        // Check if the base is an input variable from vertex shader
        if (member.Object is IdentifierExpression id && inputs.Any(i => i.Name == id.Name))
        {
            return $"fs_in.{id.Name}.{member.MemberName}";
        }

        return $"{GenerateFragmentExpression(member.Object, inputs, outputs)}.{member.MemberName}";
    }

    private string GenerateVertexCall(CallExpression call, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateVertexExpression(a, inputs, outputs)));
        return $"{funcName}({args})";
    }

    private string GenerateFragmentCall(CallExpression call, IReadOnlyList<AttributeDeclaration> inputs, IReadOnlyList<AttributeDeclaration> outputs)
    {
        var funcName = MapFunctionName(call.FunctionName);
        var args = string.Join(", ", call.Arguments.Select(a => GenerateFragmentExpression(a, inputs, outputs)));
        return $"{funcName}({args})";
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
                _sb.Append(GenerateExpression(assignStmt.Target, bindings));
                _sb.Append(" = ");
                _sb.Append(GenerateExpression(assignStmt.Value, bindings));
                _sb.AppendLine(";");
                break;

            case CompoundAssignmentStatement compoundStmt:
                Append(GenerateIndent());
                _sb.Append(GenerateExpression(compoundStmt.Target, bindings));
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

            HasExpression has => $"has_{has.ComponentName}", // Placeholder - would need runtime support

            ParenthesizedExpression paren => $"({GenerateExpression(paren.Inner, bindings)})",

            _ => throw new NotSupportedException($"Expression type {expr.GetType().Name} not supported")
        };
    }

    private string GenerateMemberAccess(MemberAccessExpression member, IReadOnlyList<QueryBinding> bindings)
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
