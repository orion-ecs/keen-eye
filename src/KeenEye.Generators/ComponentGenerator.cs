using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEye.Generators;

/// <summary>
/// Generates component metadata and fluent builder methods for types marked with [Component] or [TagComponent].
/// All generated code is stateless - component registration happens per-World at runtime.
/// </summary>
[Generator]
public sealed class ComponentGenerator : IIncrementalGenerator
{
    private const string ComponentAttribute = "KeenEye.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEye.TagComponentAttribute";
    private const string DefaultValueAttribute = "KeenEye.DefaultValueAttribute";
    private const string BuilderIgnoreAttribute = "KeenEye.BuilderIgnoreAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [Component] attribute
        var componentProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetComponentInfo(ctx, isTag: false))
            .Where(static info => info is not null);

        // Find all structs with [TagComponent] attribute
        var tagComponentProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TagComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetComponentInfo(ctx, isTag: true))
            .Where(static info => info is not null);

        // Combine both providers
        var allComponents = componentProvider.Collect()
            .Combine(tagComponentProvider.Collect());

        // Generate the code
        context.RegisterSourceOutput(allComponents, static (ctx, source) =>
        {
            var (components, tagComponents) = source;
            var allInfos = components.Concat(tagComponents).ToImmutableArray();

            if (allInfos.Length == 0)
                return;

            // Generate component interface implementations
            foreach (var info in allInfos)
            {
                if (info is null) continue;
                var componentSource = GenerateComponentPartial(info);
                ctx.AddSource($"{info.FullName}.g.cs", SourceText.From(componentSource, Encoding.UTF8));
            }

            // Generate EntityBuilder extension methods
            var builderSource = GenerateEntityBuilderExtensions(allInfos!);
            ctx.AddSource("EntityBuilder.Components.g.cs", SourceText.From(builderSource, Encoding.UTF8));
        });
    }

    private static ComponentInfo? GetComponentInfo(GeneratorAttributeSyntaxContext context, bool isTag)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return null;

        var fields = new List<FieldInfo>();

        if (!isTag)
        {
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IFieldSymbol field)
                    continue;

                if (field.IsStatic || field.IsConst)
                    continue;

                // Check for [BuilderIgnore]
                var hasBuilderIgnore = field.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == BuilderIgnoreAttribute);

                if (hasBuilderIgnore)
                    continue;

                // Check for [DefaultValue]
                string? defaultValue = null;
                var defaultAttr = field.GetAttributes()
                    .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == DefaultValueAttribute);

                if (defaultAttr is not null && defaultAttr.ConstructorArguments.Length > 0)
                {
                    var arg = defaultAttr.ConstructorArguments[0];
                    defaultValue = FormatDefaultValue(arg);
                }

                // Check if field has 'required' modifier
                var isRequired = field.IsRequired;

                // Get default from field initializer if present
                var fieldSyntax = field.DeclaringSyntaxReferences
                    .FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;

                if (fieldSyntax?.Initializer is not null && defaultValue is null)
                {
                    defaultValue = fieldSyntax.Initializer.Value.ToString();
                }

                fields.Add(new FieldInfo(
                    field.Name,
                    field.Type.ToDisplayString(),
                    isRequired,
                    defaultValue));
            }
        }

        return new ComponentInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            isTag,
            fields.ToImmutableArray());
    }

    private static string FormatDefaultValue(TypedConstant constant)
    {
        if (constant.IsNull)
            return "null";

        return constant.Kind switch
        {
            TypedConstantKind.Primitive when constant.Value is string s => $"\"{s}\"",
            TypedConstantKind.Primitive when constant.Value is char c => $"'{c}'",
            TypedConstantKind.Primitive when constant.Value is bool b => b ? "true" : "false",
            TypedConstantKind.Primitive when constant.Value is float f => $"{f}f",
            TypedConstantKind.Primitive when constant.Value is double d => $"{d}d",
            TypedConstantKind.Primitive when constant.Value is decimal m => $"{m}m",
            TypedConstantKind.Primitive => constant.Value?.ToString() ?? "default",
            TypedConstantKind.Enum => $"({constant.Type!.ToDisplayString()}){constant.Value}",
            _ => "default"
        };
    }

    private static string GenerateComponentPartial(ComponentInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace) && info.Namespace != "<global namespace>")
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        var interfaceType = info.IsTag ? "ITagComponent" : "IComponent";

        // Simple partial that just implements the interface
        // No static state - component registration happens per-World at runtime
        sb.AppendLine($"partial struct {info.Name} : global::KeenEye.{interfaceType};");

        return sb.ToString();
    }

    private static string GenerateEntityBuilderExtensions(ImmutableArray<ComponentInfo?> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEye;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated fluent builder methods for components.");
        sb.AppendLine("/// Component registration happens per-World at runtime via the World's ComponentRegistry.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class EntityBuilderExtensions");
        sb.AppendLine("{");

        foreach (var info in components)
        {
            if (info is null) continue;

            sb.AppendLine($"    /// <summary>Adds a <see cref=\"{info.FullName}\"/> component to the entity.</summary>");

            if (info.IsTag)
            {
                // Tag component - no parameters
                sb.AppendLine($"    public static EntityBuilder With{info.Name}(this EntityBuilder builder)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        return builder.WithTag<{info.FullName}>();");
                sb.AppendLine($"    }}");
            }
            else
            {
                // Regular component - generate parameters from fields
                var parameters = new List<string>();
                var assignments = new List<string>();

                foreach (var field in info.Fields)
                {
                    var paramName = ToCamelCase(field.Name);
                    var defaultExpr = GetDefaultExpression(field);

                    if (field.IsRequired || defaultExpr is null)
                    {
                        // Required parameter (no default)
                        parameters.Add($"{field.Type} {paramName}");
                    }
                    else
                    {
                        // Optional parameter with default
                        parameters.Add($"{field.Type} {paramName} = {defaultExpr}");
                    }

                    assignments.Add($"{field.Name} = {paramName}");
                }

                var paramList = string.Join(", ", parameters);
                var assignList = string.Join(", ", assignments);

                sb.AppendLine($"    public static EntityBuilder With{info.Name}(this EntityBuilder builder, {paramList})");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        return builder.With(new {info.FullName} {{ {assignList} }});");
                sb.AppendLine($"    }}");
            }

            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string? GetDefaultExpression(FieldInfo field)
    {
        if (field.DefaultValue is not null)
            return field.DefaultValue;

        // Provide sensible defaults based on type
        return field.Type switch
        {
            "int" or "System.Int32" => "0",
            "float" or "System.Single" => "0f",
            "double" or "System.Double" => "0d",
            "bool" or "System.Boolean" => "false",
            "string" or "System.String" => "\"\"",
            "long" or "System.Int64" => "0L",
            "byte" or "System.Byte" => "0",
            "short" or "System.Int16" => "0",
            "uint" or "System.UInt32" => "0u",
            "ulong" or "System.UInt64" => "0ul",
            "decimal" or "System.Decimal" => "0m",
            "char" or "System.Char" => "'\\0'",
            _ => "default"
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // Handle names like "X", "Y", "Z" - keep them lowercase
        if (name.Length == 1)
            return name.ToLowerInvariant();

        // Handle PascalCase -> camelCase
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private sealed record ComponentInfo(
        string Name,
        string Namespace,
        string FullName,
        bool IsTag,
        ImmutableArray<FieldInfo> Fields);

    private sealed record FieldInfo(
        string Name,
        string Type,
        bool IsRequired,
        string? DefaultValue);
}
