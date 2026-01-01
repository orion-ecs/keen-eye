using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using KeenEyes.Generators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

#pragma warning disable RS1035 // Roslyn analyzers warn about non-shipping analyzer target frameworks

namespace KeenEyes.Generators;

/// <summary>
/// Generates efficient query iterators for types marked with [Query].
/// </summary>
[Generator]
public sealed class QueryGenerator : IIncrementalGenerator
{
    private const string QueryAttribute = "KeenEyes.QueryAttribute";
    private const string WithAttribute = "KeenEyes.WithAttribute";
    private const string WithoutAttribute = "KeenEyes.WithoutAttribute";
    private const string OptionalAttribute = "KeenEyes.OptionalAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var queryProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                QueryAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetQueryInfo(ctx))
            .Where(static info => info is not null);

        context.RegisterSourceOutput(queryProvider, static (ctx, info) =>
        {
            if (info is null)
            {
                return;
            }

            // Report any diagnostics (e.g., conflicting attributes)
            foreach (var diag in info.Diagnostics)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    diag.Descriptor,
                    diag.Location,
                    diag.MessageArgs));
            }

            // Only generate code if no errors
            if (!info.IsValid)
            {
                return;
            }

            var source = GenerateQueryImplementation(info);
            ctx.AddSource($"{info.FullName}.Query.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static QueryInfo? GetQueryInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var fields = new List<QueryFieldInfo>();
        var diagnostics = new List<QueryDiagnosticInfo>();
        var hasErrors = false;

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

            var fieldType = field.Type;
            var isRef = false;
            var isReadOnly = false;

            // Check for ref/ref readonly field types
            if (fieldType is IPointerTypeSymbol || field.RefKind != RefKind.None)
            {
                isRef = true;
                isReadOnly = field.RefKind == RefKind.RefReadOnly;
            }

            // Get the actual component type
            var componentType = fieldType.ToDisplayString();

            // Check for filter attributes
            var hasWithAttr = field.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == WithAttribute);
            var hasWithoutAttr = field.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == WithoutAttribute);
            var hasOptionalAttr = field.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == OptionalAttribute);

            // Detect conflicting attributes
            if (hasWithAttr && hasWithoutAttr)
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new QueryDiagnosticInfo(
                        QueryDiagnostics.ConflictingQueryAttributes,
                        location,
                        [field.Name, typeSymbol.Name, "[With]", "[Without]"]));
                    hasErrors = true;
                }
            }

            if (hasWithAttr && hasOptionalAttr)
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new QueryDiagnosticInfo(
                        QueryDiagnostics.ConflictingQueryAttributes,
                        location,
                        [field.Name, typeSymbol.Name, "[With]", "[Optional]"]));
                    hasErrors = true;
                }
            }

            if (hasWithoutAttr && hasOptionalAttr)
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new QueryDiagnosticInfo(
                        QueryDiagnostics.ConflictingQueryAttributes,
                        location,
                        [field.Name, typeSymbol.Name, "[Without]", "[Optional]"]));
                    hasErrors = true;
                }
            }

            var accessType = hasWithAttr ? QueryAccessType.With
                : hasWithoutAttr ? QueryAccessType.Without
                : hasOptionalAttr ? QueryAccessType.Optional
                : isReadOnly ? QueryAccessType.Read
                : QueryAccessType.Write;

            fields.Add(new QueryFieldInfo(
                field.Name,
                componentType,
                accessType,
                isRef));
        }

        return new QueryInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            fields.ToImmutableArray(),
            diagnostics.ToImmutableArray(),
            IsValid: !hasErrors);
    }

    private static string GenerateQueryImplementation(QueryInfo info)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        StringHelpers.AppendNamespaceDeclaration(sb, info.Namespace);

        // Generate the query struct partial
        sb.AppendLine($"partial struct {info.Name}");
        sb.AppendLine("{");

        // Generate static description property
        sb.AppendLine("    /// <summary>Gets the query description for matching entities.</summary>");
        sb.AppendLine("    public static global::KeenEyes.QueryDescription CreateDescription()");
        sb.AppendLine("    {");
        sb.AppendLine("        var desc = new global::KeenEyes.QueryDescription();");

        foreach (var field in info.Fields)
        {
            var methodName = field.AccessType switch
            {
                QueryAccessType.Read => "AddRead",
                QueryAccessType.Write => "AddWrite",
                QueryAccessType.With => "AddWith",
                QueryAccessType.Without => "AddWithout",
                QueryAccessType.Optional => null, // Optional doesn't add to description
                _ => null
            };

            if (methodName is not null)
            {
                sb.AppendLine($"        desc.{methodName}<{field.ComponentType}>();");
            }
        }

        sb.AppendLine("        return desc;");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        // Generate extension method for World
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes");
        sb.AppendLine("{");
        sb.AppendLine($"    /// <summary>Query extensions for {info.Name}.</summary>");
        sb.AppendLine("    public static partial class QueryExtensions");
        sb.AppendLine("    {");
        sb.AppendLine($"        /// <summary>Creates a query using the {info.Name} definition.</summary>");
        sb.AppendLine($"        public static global::System.Collections.Generic.IEnumerable<global::KeenEyes.Entity> Query(");
        sb.AppendLine($"            this global::KeenEyes.World world,");
        sb.AppendLine($"            {info.FullName} _)");
        sb.AppendLine("        {");
        sb.AppendLine($"            var description = {info.FullName}.CreateDescription();");
        sb.AppendLine("            return world.GetMatchingEntities(description);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private enum QueryAccessType
    {
        Read,
        Write,
        With,
        Without,
        Optional
    }

    private sealed record QueryInfo(
        string Name,
        string Namespace,
        string FullName,
        ImmutableArray<QueryFieldInfo> Fields,
        ImmutableArray<QueryDiagnosticInfo> Diagnostics,
        bool IsValid);

    private sealed record QueryFieldInfo(
        string Name,
        string ComponentType,
        QueryAccessType AccessType,
        bool IsRef);

    private sealed record QueryDiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location Location,
        object[] MessageArgs);
}

/// <summary>
/// Diagnostic descriptors for query generation errors.
/// </summary>
internal static partial class QueryDiagnostics
{
    /// <summary>
    /// KEEN051: Conflicting query attributes on field.
    /// </summary>
    public static readonly DiagnosticDescriptor ConflictingQueryAttributes = new(
        id: "KEEN051",
        title: "Conflicting query attributes",
        messageFormat: "Field '{0}' in query '{1}' has conflicting attributes {2} and {3}; only one filter attribute is allowed per field",
        category: "KeenEyes.Query",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A query field can only have one filter attribute ([With], [Without], or [Optional]). " +
                     "Having multiple filter attributes on the same field is contradictory and not allowed.");
}
