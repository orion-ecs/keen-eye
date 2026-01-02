// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Immutable;
using System.Linq;
using System.Text;
using KeenEyes.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates C# 13 extension members on IEditorContext for classes marked with [EditorExtension].
/// This allows typed property access like <c>context.SceneAnalyzer</c> instead of
/// <c>context.GetExtension&lt;SceneAnalyzerExtension&gt;()</c>.
/// </summary>
[Generator]
public sealed class EditorExtensionGenerator : IIncrementalGenerator
{
    private const string EditorExtensionAttribute = "KeenEyes.Editor.Abstractions.EditorExtensionAttribute";

    /// <summary>
    /// KEEN090: EditorExtension property name cannot be null.
    /// </summary>
    public static readonly DiagnosticDescriptor NullPropertyName = new(
        id: "KEEN090",
        title: "EditorExtension property name cannot be null",
        messageFormat: "The property name for [EditorExtension] on '{0}' cannot be null",
        category: "KeenEyes.Editor.Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [EditorExtension] attribute requires a non-null property name to generate the IEditorContext extension property.");

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all classes with [EditorExtension] attribute
        var extensionProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                EditorExtensionAttribute,
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
                        diag.Location,
                        diag.Args));
                }
            }

            // Generate code only for valid extensions
            var validExtensions = extensions.Where(static e => e.IsValid).ToImmutableArray();
            if (validExtensions.Length == 0)
            {
                return;
            }

            var source = GenerateEditorContextExtensions(validExtensions);
            ctx.AddSource("EditorContext.Extensions.g.cs", SourceText.From(source, Encoding.UTF8));
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
            a => a.AttributeClass?.ToDisplayString() == EditorExtensionAttribute);

        if (attributeData is null)
        {
            return CreateInvalidExtensionInfo();
        }

        // Get property name from constructor argument
        if (attributeData.ConstructorArguments.Length == 0 ||
            attributeData.ConstructorArguments[0].Value is not string propertyName)
        {
            // Report error for null property name
            var diagnostics = ImmutableArray.Create(new DiagnosticInfo(
                NullPropertyName,
                context.TargetNode.GetLocation(),
                [typeSymbol.Name]));

            return new ExtensionInfo(
                typeSymbol.Name,
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                string.Empty,
                false,
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
            ImmutableArray<DiagnosticInfo>.Empty,
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
            ImmutableArray<DiagnosticInfo>.Empty,
            IsValid: false);
    }

    private static string GenerateEditorContextExtensions(ImmutableArray<ExtensionInfo> extensions)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes.Editor.Abstractions;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated extension members providing typed access to editor extensions on IEditorContext.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("/// <para>");
        sb.AppendLine("/// These extension members are generated from classes marked with <see cref=\"EditorExtensionAttribute\"/>.");
        sb.AppendLine("/// They provide convenient typed property access instead of using <see cref=\"IEditorContext.GetExtension{T}\"/>.");
        sb.AppendLine("/// </para>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine("public static class EditorContextExtensions");
        sb.AppendLine("{");
        sb.AppendLine("    extension(global::KeenEyes.Editor.Abstractions.IEditorContext context)");
        sb.AppendLine("    {");

        foreach (var ext in extensions)
        {
            var fullTypeName = StringHelpers.IsValidNamespace(ext.Namespace)
                ? $"global::{ext.FullName}"
                : ext.Name;

            sb.AppendLine($"        /// <summary>");
            sb.AppendLine($"        /// Gets the <see cref=\"{ext.FullName}\"/> extension from this editor context.");
            sb.AppendLine($"        /// </summary>");

            if (ext.IsNullable)
            {
                sb.AppendLine($"        /// <returns>The extension instance if registered; otherwise, null.</returns>");
                sb.AppendLine($"        public {fullTypeName}? {ext.PropertyName} => context.TryGetExtension<{fullTypeName}>(out var ext) ? ext : null;");
            }
            else
            {
                sb.AppendLine($"        /// <exception cref=\"global::System.InvalidOperationException\">Thrown when the extension is not registered.</exception>");
                sb.AppendLine($"        public {fullTypeName} {ext.PropertyName} => context.GetExtension<{fullTypeName}>();");
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
        Location Location,
        object[] Args);

    /// <summary>
    /// Information about an editor extension class.
    /// </summary>
    private sealed record ExtensionInfo(
        string Name,
        string Namespace,
        string FullName,
        string PropertyName,
        bool IsNullable,
        ImmutableArray<DiagnosticInfo> Diagnostics,
        bool IsValid);
}
