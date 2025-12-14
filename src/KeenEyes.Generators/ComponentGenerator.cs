using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates component metadata and fluent builder methods for types marked with [Component] or [TagComponent].
/// All generated code is stateless - component registration happens per-World at runtime.
/// </summary>
[Generator]
public sealed class ComponentGenerator : IIncrementalGenerator
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEyes.TagComponentAttribute";
    private const string DefaultValueAttribute = "KeenEyes.DefaultValueAttribute";
    private const string BuilderIgnoreAttribute = "KeenEyes.BuilderIgnoreAttribute";

    /// <inheritdoc />
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
            {
                return;
            }

            // Generate component interface implementations
            foreach (var info in allInfos)
            {
                if (info is null)
                {
                    continue;
                }
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
        {
            return null;
        }

        var fields = new List<FieldInfo>();

        if (!isTag)
        {
            foreach (var member in typeSymbol.GetMembers())
            {
                if (member is not IFieldSymbol field)
                {
                    continue;
                }

                if (field.IsStatic || field.IsConst)
                {
                    continue;
                }

                // Check for [BuilderIgnore]
                var hasBuilderIgnore = field.GetAttributes()
                    .Any(a => a.AttributeClass?.ToDisplayString() == BuilderIgnoreAttribute);

                if (hasBuilderIgnore)
                {
                    continue;
                }

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

        // Check if this is a nested type and get the containing type name
        var containingTypeName = typeSymbol.ContainingType?.Name;

        // Check if type is public (for extension method generation)
        var isPublic = typeSymbol.DeclaredAccessibility == Accessibility.Public;

        return new ComponentInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            isTag,
            fields.ToImmutableArray(),
            containingTypeName,
            isPublic);
    }

    private static string FormatDefaultValue(TypedConstant constant)
    {
        if (constant.IsNull)
        {
            return "null";
        }

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

        // For nested types, we need to wrap in the containing type's partial declaration
        if (info.ContainingTypeName != null)
        {
            sb.AppendLine($"partial class {info.ContainingTypeName}");
            sb.AppendLine("{");
            sb.AppendLine($"    partial struct {info.Name} : global::KeenEyes.{interfaceType};");
            sb.AppendLine("}");
        }
        else
        {
            // Simple partial that just implements the interface
            // No static state - component registration happens per-World at runtime
            sb.AppendLine($"partial struct {info.Name} : global::KeenEyes.{interfaceType};");
        }

        return sb.ToString();
    }

    private static string GenerateEntityBuilderExtensions(ImmutableArray<ComponentInfo?> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated fluent builder methods for components.");
        sb.AppendLine("/// Component registration happens per-World at runtime via the World's ComponentRegistry.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class EntityBuilderExtensions");
        sb.AppendLine("{");

        // Detect name collisions and build suffix map
        var componentsByName = components
            .Where(c => c is not null)
            .GroupBy(c => c!.Name)
            .ToImmutableArray();

        var suffixMap = new Dictionary<string, string>();

        foreach (var group in componentsByName)
        {
            if (group.Count() > 1)
            {
                // Name collision detected - generate unique suffixes
                foreach (var info in group)
                {
                    // For nested types, use containing type name; otherwise use namespace
                    var suffix = info!.ContainingTypeName ?? GenerateNamespaceSuffix(info.Namespace);
                    suffixMap[info.FullName] = suffix;
                }
            }
        }

        foreach (var info in components)
        {
            if (info is null)
            {
                continue;
            }

            // Skip non-public components - they don't need fluent builder extensions
            // (private/internal test components should use With<T>() directly)
            if (!info.IsPublic)
            {
                continue;
            }

            // Determine method name with optional suffix for collisions
            var methodName = info.Name;
            if (suffixMap.TryGetValue(info.FullName, out var suffix))
            {
                methodName = $"{info.Name}_{suffix}";
            }

            if (info.IsTag)
            {
                GenerateTagComponentExtensions(sb, info, methodName);
            }
            else
            {
                GenerateRegularComponentExtensions(sb, info, methodName);
            }

            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateTagComponentExtensions(StringBuilder sb, ComponentInfo info, string methodName)
    {
        // Generic version for fluent chaining
        AppendXmlSummary(sb, info.FullName);
        sb.AppendLine($"    public static TSelf With{methodName}<TSelf>(this TSelf builder)");
        sb.AppendLine($"        where TSelf : global::KeenEyes.IEntityBuilder<TSelf>");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return builder.WithTag<{info.FullName}>();");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Non-generic version for interface usage
        AppendXmlSummary(sb, info.FullName);
        sb.AppendLine($"    public static global::KeenEyes.IEntityBuilder With{methodName}(this global::KeenEyes.IEntityBuilder builder)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return builder.WithTag<{info.FullName}>();");
        sb.AppendLine($"    }}");
    }

    private static void GenerateRegularComponentExtensions(StringBuilder sb, ComponentInfo info, string methodName)
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

        // Generic version for fluent chaining
        AppendXmlSummary(sb, info.FullName);
        sb.AppendLine($"    public static TSelf With{methodName}<TSelf>(this TSelf builder, {paramList})");
        sb.AppendLine($"        where TSelf : global::KeenEyes.IEntityBuilder<TSelf>");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return builder.With(new {info.FullName} {{ {assignList} }});");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Non-generic version for interface usage
        AppendXmlSummary(sb, info.FullName);
        sb.AppendLine($"    public static global::KeenEyes.IEntityBuilder With{methodName}(this global::KeenEyes.IEntityBuilder builder, {paramList})");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return builder.With(new {info.FullName} {{ {assignList} }});");
        sb.AppendLine($"    }}");
    }

    private static void AppendXmlSummary(StringBuilder sb, string fullTypeName)
    {
        sb.AppendLine($"    /// <summary>Adds a <see cref=\"{fullTypeName}\"/> component to the entity.</summary>");
    }

    private static string GenerateNamespaceSuffix(string namespaceName)
    {
        // Handle empty or global namespace
        if (string.IsNullOrEmpty(namespaceName) || namespaceName == "<global namespace>")
        {
            return "Global";
        }

        // Extract the last meaningful part of the namespace
        // For "KeenEyes.Tests.MultiWorldIsolationTests", we want "MultiWorldIsolationTests"
        var parts = namespaceName.Split('.');

        // Take the last part, or last two parts if the last part is very short
        var lastPart = parts[parts.Length - 1];
        if (parts.Length > 1 && lastPart.Length <= 3)
        {
            // Last part is very short (like "V2"), include the part before it
            return parts[parts.Length - 2] + lastPart;
        }

        return lastPart;
    }

    private static string? GetDefaultExpression(FieldInfo field)
    {
        if (field.DefaultValue is not null)
        {
            return field.DefaultValue;
        }

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
        {
            return name;
        }

        // Handle names like "X", "Y", "Z" - keep them lowercase
        if (name.Length == 1)
        {
            return name.ToLowerInvariant();
        }

        // Handle PascalCase -> camelCase
        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private sealed record ComponentInfo(
        string Name,
        string Namespace,
        string FullName,
        bool IsTag,
        ImmutableArray<FieldInfo> Fields,
        string? ContainingTypeName,
        bool IsPublic);

    private sealed record FieldInfo(
        string Name,
        string Type,
        bool IsRequired,
        string? DefaultValue);
}
