using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that validates [Component], [TagComponent], [System], and [Query] attribute usage.
/// Reports compile-time errors when these attributes are applied to incorrect type kinds or non-partial types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class AttributeUsageAnalyzer : DiagnosticAnalyzer
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEyes.TagComponentAttribute";
    private const string SystemAttribute = "KeenEyes.SystemAttribute";
    private const string QueryAttribute = "KeenEyes.QueryAttribute";

    #region System Diagnostics (KEEN007-008)

    /// <summary>
    /// KEEN007: [System] must be applied to a class.
    /// </summary>
    public static readonly DiagnosticDescriptor SystemMustBeClass = new(
        id: "KEEN007",
        title: "System must be a class",
        messageFormat: "Type '{0}' is marked with [System] but is not a class",
        category: "KeenEyes.System",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [System] attribute can only be applied to class types. Systems must be classes that inherit from SystemBase or implement ISystem.");

    /// <summary>
    /// KEEN008: [System] class must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor SystemMustBePartial = new(
        id: "KEEN008",
        title: "System must be partial",
        messageFormat: "System class '{0}' must be declared with 'partial' modifier",
        category: "KeenEyes.System",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "System classes must be declared as 'partial' to allow source generators to augment them with metadata properties.");

    #endregion

    #region Component Diagnostics (KEEN015-016)

    /// <summary>
    /// KEEN015: [Component] or [TagComponent] must be applied to a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor ComponentMustBeStruct = new(
        id: "KEEN015",
        title: "Component must be a struct",
        messageFormat: "Type '{0}' is marked with [{1}] but is not a struct",
        category: "KeenEyes.Component",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [Component] and [TagComponent] attributes can only be applied to struct types. Components must be value types for cache efficiency.");

    /// <summary>
    /// KEEN016: Component struct must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor ComponentMustBePartial = new(
        id: "KEEN016",
        title: "Component must be partial",
        messageFormat: "Component struct '{0}' must be declared with 'partial' modifier",
        category: "KeenEyes.Component",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Component structs must be declared as 'partial' to allow source generators to implement the IComponent interface.");

    #endregion

    #region Query Diagnostics (KEEN052-054)

    /// <summary>
    /// KEEN052: [Query] must be applied to a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor QueryMustBeStruct = new(
        id: "KEEN052",
        title: "Query must be a struct",
        messageFormat: "Type '{0}' is marked with [Query] but is not a struct",
        category: "KeenEyes.Query",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The [Query] attribute can only be applied to struct types. Queries must be value types to avoid heap allocations.");

    /// <summary>
    /// KEEN053: Query struct must be partial.
    /// </summary>
    public static readonly DiagnosticDescriptor QueryMustBePartial = new(
        id: "KEEN053",
        title: "Query must be partial",
        messageFormat: "Query struct '{0}' must be declared with 'partial' modifier",
        category: "KeenEyes.Query",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Query structs must be declared as 'partial' to allow source generators to implement the query iterator.");

    #endregion

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            SystemMustBeClass,
            SystemMustBePartial,
            ComponentMustBeStruct,
            ComponentMustBePartial,
            QueryMustBeStruct,
            QueryMustBePartial);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == ComponentAttribute || attributeName == TagComponentAttribute)
            {
                AnalyzeComponentAttribute(context, typeSymbol, attributeName);
            }
            else if (attributeName == SystemAttribute)
            {
                AnalyzeSystemAttribute(context, typeSymbol);
            }
            else if (attributeName == QueryAttribute)
            {
                AnalyzeQueryAttribute(context, typeSymbol);
            }
        }
    }

    private static void AnalyzeComponentAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol,
        string attributeName)
    {
        var location = typeSymbol.Locations.FirstOrDefault();
        if (location == null)
        {
            return;
        }

        // Extract short attribute name for message
        var shortName = attributeName.EndsWith("Attribute")
            ? attributeName.Substring(attributeName.LastIndexOf('.') + 1).Replace("Attribute", "")
            : attributeName;

        // KEEN015: Check if it's a struct
        if (typeSymbol.TypeKind != TypeKind.Struct)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ComponentMustBeStruct,
                location,
                typeSymbol.Name,
                shortName));
            return;
        }

        // KEEN016: Check if it's partial
        if (!IsPartialType(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ComponentMustBePartial,
                location,
                typeSymbol.Name));
        }
    }

    private static void AnalyzeSystemAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol)
    {
        var location = typeSymbol.Locations.FirstOrDefault();
        if (location == null)
        {
            return;
        }

        // KEEN007: Check if it's a class
        if (typeSymbol.TypeKind != TypeKind.Class)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SystemMustBeClass,
                location,
                typeSymbol.Name));
            return;
        }

        // KEEN008: Check if it's partial
        if (!IsPartialType(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SystemMustBePartial,
                location,
                typeSymbol.Name));
        }
    }

    private static void AnalyzeQueryAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol typeSymbol)
    {
        var location = typeSymbol.Locations.FirstOrDefault();
        if (location == null)
        {
            return;
        }

        // KEEN052: Check if it's a struct
        if (typeSymbol.TypeKind != TypeKind.Struct)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                QueryMustBeStruct,
                location,
                typeSymbol.Name));
            return;
        }

        // KEEN053: Check if it's partial
        if (!IsPartialType(typeSymbol))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                QueryMustBePartial,
                location,
                typeSymbol.Name));
        }
    }

    private static bool IsPartialType(INamedTypeSymbol typeSymbol)
    {
        // Check if any declaration has the partial modifier
        foreach (var syntaxRef in typeSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is TypeDeclarationSyntax typeDecl &&
                typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return true;
            }
        }

        return false;
    }
}
