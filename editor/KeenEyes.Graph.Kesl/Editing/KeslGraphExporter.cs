using System.Text;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Compiler;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Editing;

/// <summary>
/// Exports a shader graph to KESL source code.
/// </summary>
/// <remarks>
/// <para>
/// The exporter first compiles the graph to an AST using <see cref="KeslGraphCompiler"/>,
/// then generates formatted KESL source code from the AST.
/// </para>
/// </remarks>
public sealed class KeslGraphExporter
{
    private readonly KeslGraphCompiler compiler = new();

    /// <summary>
    /// Options for controlling export formatting.
    /// </summary>
    public ExportOptions Options { get; set; } = new();

    /// <summary>
    /// Exports a shader graph to KESL source code.
    /// </summary>
    /// <param name="canvas">The graph canvas entity.</param>
    /// <param name="world">The world containing the graph.</param>
    /// <returns>The export result with source code or errors.</returns>
    public ExportResult Export(Entity canvas, IWorld world)
    {
        // Compile graph to AST
        var compileResult = compiler.Compile(canvas, world);
        if (!compileResult.IsSuccess)
        {
            var errors = compileResult.Errors.Select(e =>
                new ExportError(e.Message, e.Node)).ToList();
            return ExportResult.Failure(errors);
        }

        // Generate source from AST based on shader type
        var source = compileResult.ShaderType switch
        {
            CompiledShaderType.Compute => GenerateComputeSource(compileResult.ComputeDeclaration!),
            CompiledShaderType.Vertex => GenerateVertexSource(compileResult.VertexDeclaration!),
            CompiledShaderType.Fragment => GenerateFragmentSource(compileResult.FragmentDeclaration!),
            _ => throw new InvalidOperationException("Unknown shader type")
        };

        return ExportResult.Success(source);
    }

    private string GenerateComputeSource(ComputeDeclaration declaration)
    {
        var sb = new StringBuilder();
        var indent = Options.UseTabs ? "\t" : new string(' ', Options.IndentSize);

        // compute ShaderName {
        sb.AppendLine($"compute {declaration.Name} {{");

        // query block
        sb.AppendLine($"{indent}query {{");
        foreach (var binding in declaration.Query.Bindings)
        {
            var accessMod = binding.AccessMode switch
            {
                AccessMode.Read => "read",
                AccessMode.Write => "write",
                AccessMode.Optional => "optional",
                AccessMode.Without => "without",
                _ => "read"
            };
            sb.AppendLine($"{indent}{indent}{accessMod} {binding.ComponentName}");
        }
        sb.AppendLine($"{indent}}}");

        // params block (if any)
        if (declaration.Params is not null && declaration.Params.Parameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}params {{");
            foreach (var param in declaration.Params.Parameters)
            {
                var typeName = TypeRefToString(param.Type);
                sb.AppendLine($"{indent}{indent}{param.Name}: {typeName}");
            }
            sb.AppendLine($"{indent}}}");
        }

        // execute block
        sb.AppendLine();
        sb.AppendLine($"{indent}execute() {{");
        foreach (var statement in declaration.Execute.Body)
        {
            GenerateStatement(sb, statement, 2, indent);
        }
        sb.AppendLine($"{indent}}}");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateVertexSource(VertexDeclaration declaration)
    {
        var sb = new StringBuilder();
        var indent = Options.UseTabs ? "\t" : new string(' ', Options.IndentSize);

        // vertex ShaderName {
        sb.AppendLine($"vertex {declaration.Name} {{");

        // in block
        sb.AppendLine($"{indent}in {{");
        foreach (var attr in declaration.Inputs.Attributes)
        {
            var typeName = TypeRefToString(attr.Type);
            var location = attr.LocationIndex.HasValue ? $" @ {attr.LocationIndex.Value}" : "";
            sb.AppendLine($"{indent}{indent}{attr.Name}: {typeName}{location}");
        }
        sb.AppendLine($"{indent}}}");

        // out block
        sb.AppendLine();
        sb.AppendLine($"{indent}out {{");
        foreach (var attr in declaration.Outputs.Attributes)
        {
            var typeName = TypeRefToString(attr.Type);
            var location = attr.LocationIndex.HasValue ? $" @ {attr.LocationIndex.Value}" : "";
            sb.AppendLine($"{indent}{indent}{attr.Name}: {typeName}{location}");
        }
        sb.AppendLine($"{indent}}}");

        // params block (if any)
        if (declaration.Params is not null && declaration.Params.Parameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}params {{");
            foreach (var param in declaration.Params.Parameters)
            {
                var typeName = TypeRefToString(param.Type);
                sb.AppendLine($"{indent}{indent}{param.Name}: {typeName}");
            }
            sb.AppendLine($"{indent}}}");
        }

        // execute block
        sb.AppendLine();
        sb.AppendLine($"{indent}execute() {{");
        foreach (var statement in declaration.Execute.Body)
        {
            GenerateStatement(sb, statement, 2, indent);
        }
        sb.AppendLine($"{indent}}}");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GenerateFragmentSource(FragmentDeclaration declaration)
    {
        var sb = new StringBuilder();
        var indent = Options.UseTabs ? "\t" : new string(' ', Options.IndentSize);

        // fragment ShaderName {
        sb.AppendLine($"fragment {declaration.Name} {{");

        // in block
        sb.AppendLine($"{indent}in {{");
        foreach (var attr in declaration.Inputs.Attributes)
        {
            var typeName = TypeRefToString(attr.Type);
            sb.AppendLine($"{indent}{indent}{attr.Name}: {typeName}");
        }
        sb.AppendLine($"{indent}}}");

        // out block
        sb.AppendLine();
        sb.AppendLine($"{indent}out {{");
        foreach (var attr in declaration.Outputs.Attributes)
        {
            var typeName = TypeRefToString(attr.Type);
            var location = attr.LocationIndex.HasValue ? $" @ {attr.LocationIndex.Value}" : "";
            sb.AppendLine($"{indent}{indent}{attr.Name}: {typeName}{location}");
        }
        sb.AppendLine($"{indent}}}");

        // params block (if any)
        if (declaration.Params is not null && declaration.Params.Parameters.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"{indent}params {{");
            foreach (var param in declaration.Params.Parameters)
            {
                var typeName = TypeRefToString(param.Type);
                sb.AppendLine($"{indent}{indent}{param.Name}: {typeName}");
            }
            sb.AppendLine($"{indent}}}");
        }

        // execute block
        sb.AppendLine();
        sb.AppendLine($"{indent}execute() {{");
        foreach (var statement in declaration.Execute.Body)
        {
            GenerateStatement(sb, statement, 2, indent);
        }
        sb.AppendLine($"{indent}}}");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateStatement(StringBuilder sb, Statement statement, int indentLevel, string indent)
    {
        var prefix = string.Concat(Enumerable.Repeat(indent, indentLevel));

        switch (statement)
        {
            case AssignmentStatement assign:
                sb.AppendLine($"{prefix}{GenerateExpression(assign.Target)} = {GenerateExpression(assign.Value)};");
                break;

            case ExpressionStatement expr:
                sb.AppendLine($"{prefix}{GenerateExpression(expr.Expression)};");
                break;

            case IfStatement ifStmt:
                sb.AppendLine($"{prefix}if ({GenerateExpression(ifStmt.Condition)}) {{");
                foreach (var thenStmt in ifStmt.ThenBranch)
                {
                    GenerateStatement(sb, thenStmt, indentLevel + 1, indent);
                }
                if (ifStmt.ElseBranch is not null && ifStmt.ElseBranch.Count > 0)
                {
                    sb.AppendLine($"{prefix}}} else {{");
                    foreach (var elseStmt in ifStmt.ElseBranch)
                    {
                        GenerateStatement(sb, elseStmt, indentLevel + 1, indent);
                    }
                }
                sb.AppendLine($"{prefix}}}");
                break;

            case ForStatement forStmt:
                sb.AppendLine($"{prefix}for {forStmt.VariableName} in {GenerateExpression(forStmt.Start)}..{GenerateExpression(forStmt.End)} {{");
                foreach (var bodyStmt in forStmt.Body)
                {
                    GenerateStatement(sb, bodyStmt, indentLevel + 1, indent);
                }
                sb.AppendLine($"{prefix}}}");
                break;
        }
    }

    private string GenerateExpression(Expression expression)
    {
        return expression switch
        {
            BinaryExpression binary => $"({GenerateExpression(binary.Left)} {BinaryOpToString(binary.Operator)} {GenerateExpression(binary.Right)})",
            UnaryExpression unary => UnaryOpToString(unary.Operator) + GenerateExpression(unary.Operand),
            CallExpression call => $"{call.FunctionName}({string.Join(", ", call.Arguments.Select(GenerateExpression))})",
            IdentifierExpression ident => ident.Name,
            FloatLiteralExpression floatLit => FormatFloat(floatLit.Value),
            IntLiteralExpression intLit => intLit.Value.ToString(),
            BoolLiteralExpression boolLit => boolLit.Value ? "true" : "false",
            MemberAccessExpression member => $"{GenerateExpression(member.Object)}.{member.MemberName}",
            ParenthesizedExpression paren => $"({GenerateExpression(paren.Inner)})",
            HasExpression has => $"has({has.ComponentName})",
            _ => "/* unknown */"
        };
    }

    private static string FormatFloat(float value)
    {
        // Ensure we always have a decimal point
        var str = value.ToString("G");
        if (!str.Contains('.') && !str.Contains('E') && !str.Contains('e'))
        {
            str += ".0";
        }
        return str;
    }

    private static string BinaryOpToString(BinaryOperator op) => op switch
    {
        BinaryOperator.Add => "+",
        BinaryOperator.Subtract => "-",
        BinaryOperator.Multiply => "*",
        BinaryOperator.Divide => "/",
        BinaryOperator.Equal => "==",
        BinaryOperator.NotEqual => "!=",
        BinaryOperator.Less => "<",
        BinaryOperator.LessEqual => "<=",
        BinaryOperator.Greater => ">",
        BinaryOperator.GreaterEqual => ">=",
        BinaryOperator.And => "&&",
        BinaryOperator.Or => "||",
        _ => "?"
    };

    private static string UnaryOpToString(UnaryOperator op) => op switch
    {
        UnaryOperator.Negate => "-",
        UnaryOperator.Not => "!",
        _ => ""
    };

    private static string TypeRefToString(TypeRef typeRef)
    {
        if (typeRef is PrimitiveType primitive)
        {
            return primitive.Kind switch
            {
                PrimitiveTypeKind.Float => "float",
                PrimitiveTypeKind.Float2 => "vec2",
                PrimitiveTypeKind.Float3 => "vec3",
                PrimitiveTypeKind.Float4 => "vec4",
                PrimitiveTypeKind.Int => "int",
                PrimitiveTypeKind.Int2 => "ivec2",
                PrimitiveTypeKind.Int3 => "ivec3",
                PrimitiveTypeKind.Int4 => "ivec4",
                PrimitiveTypeKind.Bool => "bool",
                PrimitiveTypeKind.Mat4 => "mat4",
                _ => "unknown"
            };
        }
        return "unknown";
    }
}

/// <summary>
/// Options for KESL source export formatting.
/// </summary>
public sealed class ExportOptions
{
    /// <summary>
    /// Gets or sets whether to use tabs for indentation.
    /// </summary>
    public bool UseTabs { get; set; } = false;

    /// <summary>
    /// Gets or sets the number of spaces per indent level (when not using tabs).
    /// </summary>
    public int IndentSize { get; set; } = 4;
}

/// <summary>
/// Result of exporting a graph to KESL source.
/// </summary>
public sealed class ExportResult
{
    private ExportResult(bool isSuccess, string? source, IReadOnlyList<ExportError> errors)
    {
        IsSuccess = isSuccess;
        Source = source;
        Errors = errors;
    }

    /// <summary>
    /// Gets whether export succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the generated source code (null if export failed).
    /// </summary>
    public string? Source { get; }

    /// <summary>
    /// Gets export errors.
    /// </summary>
    public IReadOnlyList<ExportError> Errors { get; }

    /// <summary>
    /// Creates a successful export result.
    /// </summary>
    public static ExportResult Success(string source)
        => new(true, source, []);

    /// <summary>
    /// Creates a failed export result.
    /// </summary>
    public static ExportResult Failure(IReadOnlyList<ExportError> errors)
        => new(false, null, errors);
}

/// <summary>
/// An export error.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Node">The node that caused the error (if applicable).</param>
public readonly record struct ExportError(string Message, Entity Node);
