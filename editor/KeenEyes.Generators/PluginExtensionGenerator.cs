using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using KeenEyes.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates C# 13 extension members on World for classes marked with [PluginExtension].
/// This allows typed property access like <c>world.Physics</c> instead of <c>world.GetExtension&lt;PhysicsWorld&gt;()</c>.
/// </summary>
[Generator]
public sealed class PluginExtensionGenerator : IIncrementalGenerator
{
    private const string PluginExtensionAttribute = "KeenEyes.PluginExtensionAttribute";

    /// <summary>
    /// KEEN005: PluginExtension property name cannot be null.
    /// </summary>
    public static readonly DiagnosticDescriptor NullPropertyName = new(
        id: "KEEN005",
        title: "PluginExtension property name cannot be null",
        messageFormat: "The property name for [PluginExtension] on '{0}' cannot be null",
        category: "KeenEyes.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [PluginExtension] attribute requires a non-null property name to generate the World extension property.");

    /// <summary>
    /// KEEN009: Duplicate PluginExtension property name.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicatePropertyName = new(
        id: "KEEN009",
        title: "Duplicate PluginExtension property name",
        messageFormat: "The property name '{0}' is declared by [PluginExtension] on both '{1}' and '{2}'; property names must be unique, so no extension property is generated for '{2}'",
        category: "KeenEyes.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each [PluginExtension] property name must be unique across the compilation. Duplicate names would generate conflicting extension members on IWorld.");

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with [PluginExtension] attribute
        var extensionProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                PluginExtensionAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, _) => GetExtensionInfo(ctx));

        // Collect all extensions
        var allExtensions = extensionProvider.Collect();

        // Single output registration handles both diagnostics and code generation
        context.RegisterSourceOutput(allExtensions, static (ctx, extensions) =>
        {
            // Report all diagnostics first
            foreach (var ext in extensions)
            {
                foreach (var diag in ext.Diagnostics)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        diag.Descriptor,
                        diag.Location?.ToLocation() ?? Location.None,
                        diag.Args.ToArray()));
                }
            }

            // Generate code only for valid extensions with unique property names.
            // Duplicates would emit two same-named extension members (an opaque CS0111
            // in generated code), so report KEEN009 and skip everything after the first.
            var validExtensions = ImmutableArray.CreateBuilder<ExtensionInfo>();
            var firstByPropertyName = new Dictionary<string, ExtensionInfo>();

            foreach (var ext in extensions.Where(static e => e.IsValid))
            {
                if (firstByPropertyName.TryGetValue(ext.PropertyName, out var first))
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        DuplicatePropertyName,
                        ext.Location?.ToLocation() ?? Location.None,
                        ext.PropertyName,
                        first.FullName,
                        ext.FullName));
                    continue;
                }

                firstByPropertyName.Add(ext.PropertyName, ext);
                validExtensions.Add(ext);
            }

            if (validExtensions.Count == 0)
            {
                return;
            }

            var source = GenerateWorldExtensions(validExtensions.ToImmutable());
            ctx.AddSource("World.PluginExtensions.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static ExtensionInfo GetExtensionInfo(GeneratorAttributeSyntaxContext context)
    {
        // Predicate guarantees ClassDeclarationSyntax, which always has INamedTypeSymbol
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return CreateInvalidExtensionInfo();
        }

        // ForAttributeWithMetadataName guarantees the attribute exists, but use FirstOrDefault for safety
        var attributeData = context.Attributes.FirstOrDefault(
            a => a.AttributeClass?.ToDisplayString() == PluginExtensionAttribute);

        if (attributeData is null)
        {
            return CreateInvalidExtensionInfo();
        }

        var location = LocationInfo.From(context.TargetNode.GetLocation());

        // Get property name from constructor argument
        if (attributeData.ConstructorArguments.Length == 0 ||
            attributeData.ConstructorArguments[0].Value is not string propertyName)
        {
            // Report error for null property name
            var diagnostics = ImmutableArray.Create(new DiagnosticInfo(
                NullPropertyName,
                location,
                ImmutableArray.Create(typeSymbol.Name)));

            return new ExtensionInfo(
                typeSymbol.Name,
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                string.Empty,
                false,
                location,
                diagnostics,
                IsValid: false);
        }

        // Get Nullable property from named arguments
        var isNullable = false;
        foreach (var namedArg in attributeData.NamedArguments)
        {
            if (namedArg.Key == "Nullable" && namedArg.Value.Value is bool nullable)
            {
                isNullable = nullable;
            }
        }

        return new ExtensionInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            propertyName,
            isNullable,
            location,
            EquatableArray<DiagnosticInfo>.Empty,
            IsValid: true);
    }

    private static ExtensionInfo CreateInvalidExtensionInfo()
    {
        return new ExtensionInfo(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            false,
            null,
            EquatableArray<DiagnosticInfo>.Empty,
            IsValid: false);
    }

    private static string GenerateWorldExtensions(ImmutableArray<ExtensionInfo> extensions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated extension members providing typed access to plugin extensions on World.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("/// <para>");
        sb.AppendLine("/// These extension members are generated from classes marked with <see cref=\"PluginExtensionAttribute\"/>.");
        sb.AppendLine("/// They provide convenient typed property access instead of using <see cref=\"IWorld.GetExtension{T}\"/>.");
        sb.AppendLine("/// </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine("public static class WorldPluginExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    extension(global::KeenEyes.IWorld world)");
        sb.AppendLine("    {");

        foreach (var ext in extensions)
        {
            var fullTypeName = StringHelpers.IsValidNamespace(ext.Namespace)
                ? $"global::{ext.FullName}"
                : ext.Name;

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets the <see cref=\"{ext.FullName}\"/> extension from this world.");
            sb.AppendLine($"        /// </summary>");

            if (ext.IsNullable)
            {
                sb.AppendLine($"        /// <returns>The extension instance if registered; otherwise, null.</returns>");
                sb.AppendLine($"        public {fullTypeName}? {ext.PropertyName} => world.TryGetExtension<{fullTypeName}>(out var ext) ? ext : null;");
            }
            else
            {
                sb.AppendLine($"        /// <exception cref=\"global::System.InvalidOperationException\">Thrown when the extension is not registered.</exception>");
                sb.AppendLine($"        public {fullTypeName} {ext.PropertyName} => world.GetExtension<{fullTypeName}>();");
            }

            sb.AppendLine();
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    /// <summary>
    /// Information about a diagnostic to report, captured for incremental pipeline.
    /// </summary>
    private sealed record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        LocationInfo? Location,
        EquatableArray<string> Args);

    /// <summary>
    /// Information about a plugin extension class.
    /// </summary>
    private sealed record ExtensionInfo(
        string Name,
        string Namespace,
        string FullName,
        string PropertyName,
        bool IsNullable,
        LocationInfo? Location,
        EquatableArray<DiagnosticInfo> Diagnostics,
        bool IsValid);
}
