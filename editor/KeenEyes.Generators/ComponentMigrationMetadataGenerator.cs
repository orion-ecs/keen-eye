using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates AOT-compatible version metadata for components.
/// Produces the ComponentMigrationMetadata class with GetVersion methods.
/// </summary>
[Generator]
public sealed class ComponentMigrationMetadataGenerator : IIncrementalGenerator
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEyes.TagComponentAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [Component] attribute
        var componentProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetVersionedComponentInfo(ctx))
            .Where(static info => info is not null);

        // Find all structs with [TagComponent] attribute (always version 1)
        var tagComponentProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TagComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetTagComponentInfo(ctx))
            .Where(static info => info is not null);

        // Combine both providers
        var allComponents = componentProvider.Collect()
            .Combine(tagComponentProvider.Collect());

        // Generate the code
        context.RegisterSourceOutput(allComponents, static (ctx, source) =>
        {
            var (components, tagComponents) = source;

            var allInfos = components
                .Concat(tagComponents)
                .Where(static info => info is not null)
                .Select(static info => info!)
                .ToImmutableArray();

            if (allInfos.Length == 0)
            {
                return;
            }

            var metadataSource = GenerateMigrationMetadata(allInfos);
            ctx.AddSource("ComponentMigrationMetadata.g.cs", SourceText.From(metadataSource, Encoding.UTF8));
        });
    }

    private static VersionedComponentInfo? GetVersionedComponentInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        // Skip non-public components to avoid accessibility issues in generated code
        if (!IsTypeAccessible(typeSymbol))
        {
            return null;
        }

        // Get the [Component] attribute
        var attr = context.Attributes.First();

        // Extract Version property (defaults to 1)
        var version = 1;
        var versionArg = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "Version");

        if (versionArg.Value.Value is int v)
        {
            version = v;
        }

        // Skip components with invalid versions (will be reported by analyzer)
        if (version < 1)
        {
            return null;
        }

        return new VersionedComponentInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            version,
            IsTag: false);
    }

    private static VersionedComponentInfo? GetTagComponentInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        // Skip non-public components to avoid accessibility issues in generated code
        if (!IsTypeAccessible(typeSymbol))
        {
            return null;
        }

        // Tag components always have version 1 (they have no data to migrate)
        return new VersionedComponentInfo(
            typeSymbol.Name,
            typeSymbol.ToDisplayString(),
            Version: 1,
            IsTag: true);
    }

    private static bool IsTypeAccessible(INamedTypeSymbol typeSymbol)
    {
        // Check if the type itself is public or internal
        if (typeSymbol.DeclaredAccessibility != Accessibility.Public &&
            typeSymbol.DeclaredAccessibility != Accessibility.Internal)
        {
            return false;
        }

        // Check containing types for accessibility
        var containingType = typeSymbol.ContainingType;
        while (containingType is not null)
        {
            if (containingType.DeclaredAccessibility != Accessibility.Public &&
                containingType.DeclaredAccessibility != Accessibility.Internal)
            {
                return false;
            }
            containingType = containingType.ContainingType;
        }

        return true;
    }

    private static string GenerateMigrationMetadata(ImmutableArray<VersionedComponentInfo> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated metadata for component schema versioning.");
        sb.AppendLine("/// Provides AOT-compatible version lookup for migration support.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class ComponentMigrationMetadata");
        sb.AppendLine("{");

        // Generate version lookup dictionary
        sb.AppendLine("    private static readonly Dictionary<Type, int> VersionsByType = new()");
        sb.AppendLine("    {");

        foreach (var component in components)
        {
            sb.AppendLine($"        [typeof({component.FullName})] = {component.Version},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();

        // Generate type name lookup dictionary
        sb.AppendLine("    private static readonly Dictionary<string, int> VersionsByTypeName = new()");
        sb.AppendLine("    {");

        foreach (var component in components)
        {
            // Store both the full name and assembly-qualified name for lookup flexibility
            sb.AppendLine($"        [\"{component.FullName}\"] = {component.Version},");
            sb.AppendLine($"        [typeof({component.FullName}).AssemblyQualifiedName!] = {component.Version},");
        }

        sb.AppendLine("    };");
        sb.AppendLine();

        // Generate generic GetVersion<T>() method using type checks for AOT compatibility
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the schema version of the specified component type.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <typeparam name=\"T\">The component type.</typeparam>");
        sb.AppendLine("    /// <returns>The schema version of the component, or 1 if not found.</returns>");
        sb.AppendLine("    public static int GetVersion<T>() where T : struct, IComponent");
        sb.AppendLine("    {");

        // Use type checks for compile-time optimization
        var first = true;
        foreach (var component in components)
        {
            var keyword = first ? "if" : "else if";
            first = false;
            sb.AppendLine($"        {keyword} (typeof(T) == typeof({component.FullName}))");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return {component.Version};");
            sb.AppendLine($"        }}");
        }

        sb.AppendLine();
        sb.AppendLine("        return 1; // Default version for unknown components");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate GetVersion(Type) method for runtime lookup
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the schema version of the specified component type.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"componentType\">The component type.</param>");
        sb.AppendLine("    /// <returns>The schema version of the component, or 1 if not found.</returns>");
        sb.AppendLine("    public static int GetVersion(Type componentType)");
        sb.AppendLine("    {");
        sb.AppendLine("        return VersionsByType.TryGetValue(componentType, out var version) ? version : 1;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate GetVersion(string) method for deserialization by type name
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets the schema version of the specified component type by name.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"typeName\">The fully-qualified type name or assembly-qualified type name.</param>");
        sb.AppendLine("    /// <returns>The schema version of the component, or 1 if not found.</returns>");
        sb.AppendLine("    public static int GetVersion(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return VersionsByTypeName.TryGetValue(typeName, out var version) ? version : 1;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate GetAllVersions() method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all known component types and their versions.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <returns>A dictionary mapping component types to their schema versions.</returns>");
        sb.AppendLine("    public static IReadOnlyDictionary<Type, int> GetAllVersions()");
        sb.AppendLine("    {");
        sb.AppendLine("        return VersionsByType;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate HasVersion method for checking if a type is registered
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Checks if a component type has version metadata registered.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"componentType\">The component type to check.</param>");
        sb.AppendLine("    /// <returns><c>true</c> if the type has version metadata; otherwise, <c>false</c>.</returns>");
        sb.AppendLine("    public static bool HasVersion(Type componentType)");
        sb.AppendLine("    {");
        sb.AppendLine("        return VersionsByType.ContainsKey(componentType);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate HasVersion(string) method
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Checks if a component type name has version metadata registered.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    /// <param name=\"typeName\">The fully-qualified type name to check.</param>");
        sb.AppendLine("    /// <returns><c>true</c> if the type has version metadata; otherwise, <c>false</c>.</returns>");
        sb.AppendLine("    public static bool HasVersion(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return VersionsByTypeName.ContainsKey(typeName);");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private sealed record VersionedComponentInfo(
        string Name,
        string FullName,
        int Version,
        bool IsTag);
}
