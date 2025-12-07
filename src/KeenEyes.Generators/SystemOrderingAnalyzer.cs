using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that validates RunBefore and RunAfter attribute usage.
/// Reports compile-time errors for invalid system ordering constraints.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SystemOrderingAnalyzer : DiagnosticAnalyzer
{
    private const string RunBeforeAttribute = "KeenEyes.RunBeforeAttribute";
    private const string RunAfterAttribute = "KeenEyes.RunAfterAttribute";
    private const string SystemAttribute = "KeenEyes.SystemAttribute";
    private const string ISystemInterface = "KeenEyes.ISystem";

    /// <summary>
    /// KEEN001: Self-referential ordering constraint.
    /// </summary>
    public static readonly DiagnosticDescriptor SelfReferentialConstraint = new(
        id: "KEEN001",
        title: "Self-referential ordering constraint",
        messageFormat: "System '{0}' cannot reference itself in {1} attribute",
        category: "KeenEyes.SystemOrdering",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A system cannot specify itself as a dependency in RunBefore or RunAfter attributes. This would create a circular dependency.");

    /// <summary>
    /// KEEN002: Target type is not a class.
    /// </summary>
    public static readonly DiagnosticDescriptor TargetNotAClass = new(
        id: "KEEN002",
        title: "Ordering target must be a class",
        messageFormat: "Type '{0}' in {1} attribute must be a class, not a {2}",
        category: "KeenEyes.SystemOrdering",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RunBefore and RunAfter attributes must reference class types. Systems must be classes, not structs, interfaces, or other types.");

    /// <summary>
    /// KEEN003: Missing [System] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingSystemAttribute = new(
        id: "KEEN003",
        title: "Missing [System] attribute",
        messageFormat: "Class '{0}' uses {1} but is missing the [System] attribute",
        category: "KeenEyes.SystemOrdering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Classes using RunBefore or RunAfter attributes should also have the [System] attribute to enable source generation of ordering metadata.");

    /// <summary>
    /// KEEN004: Target doesn't implement ISystem.
    /// </summary>
    public static readonly DiagnosticDescriptor TargetNotASystem = new(
        id: "KEEN004",
        title: "Ordering target should implement ISystem",
        messageFormat: "Type '{0}' referenced in {1} attribute does not appear to implement ISystem",
        category: "KeenEyes.SystemOrdering",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types referenced in RunBefore and RunAfter should implement ISystem or derive from SystemBase to be valid system ordering targets.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            SelfReferentialConstraint,
            TargetNotAClass,
            MissingSystemAttribute,
            TargetNotASystem);

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

        // Only analyze classes
        if (typeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        var hasSystemAttribute = false;
        var hasOrderingAttribute = false;

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == SystemAttribute)
            {
                hasSystemAttribute = true;
            }
            else if (attributeName == RunBeforeAttribute || attributeName == RunAfterAttribute)
            {
                hasOrderingAttribute = true;
                AnalyzeOrderingAttribute(context, typeSymbol, attribute, attributeName!);
            }
        }

        // KEEN003: Check for RunBefore/RunAfter without [System]
        if (hasOrderingAttribute && !hasSystemAttribute)
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                var attrName = typeSymbol.GetAttributes()
                    .FirstOrDefault(a =>
                        a.AttributeClass?.ToDisplayString() == RunBeforeAttribute ||
                        a.AttributeClass?.ToDisplayString() == RunAfterAttribute)?
                    .AttributeClass?.Name ?? "RunBefore/RunAfter";

                context.ReportDiagnostic(Diagnostic.Create(
                    MissingSystemAttribute,
                    location,
                    typeSymbol.Name,
                    attrName));
            }
        }
    }

    private static void AnalyzeOrderingAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol declaringType,
        AttributeData attribute,
        string attributeName)
    {
        // The attribute constructor requires a Type argument, so this is guaranteed by Roslyn
        var targetType = (INamedTypeSymbol)attribute.ConstructorArguments[0].Value!;
        var location = attribute.ApplicationSyntaxReference!.GetSyntax().GetLocation();

        var attrShortName = attributeName.EndsWith("RunBeforeAttribute")
            ? "RunBefore"
            : "RunAfter";

        // KEEN001: Check for self-reference
        if (SymbolEqualityComparer.Default.Equals(targetType, declaringType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SelfReferentialConstraint,
                location,
                declaringType.Name,
                attrShortName));
        }

        // KEEN002: Check if target is a class
        if (targetType.TypeKind != TypeKind.Class)
        {
            var typeKindName = targetType.TypeKind switch
            {
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
                TypeKind.Enum => "enum",
                TypeKind.Delegate => "delegate",
                _ => targetType.TypeKind.ToString().ToLowerInvariant()
            };

            context.ReportDiagnostic(Diagnostic.Create(
                TargetNotAClass,
                location,
                targetType.Name,
                attrShortName,
                typeKindName));
        }

        // KEEN004: Check if target implements ISystem
        if (targetType.TypeKind == TypeKind.Class && !ImplementsISystem(targetType, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TargetNotASystem,
                location,
                targetType.Name,
                attrShortName));
        }
    }

    private static bool ImplementsISystem(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        var iSystemType = compilation.GetTypeByMetadataName(ISystemInterface);
        if (iSystemType == null)
        {
            // ISystem not found in compilation - assume valid to avoid false positives
            return true;
        }

        return typeSymbol.AllInterfaces.Any(iface =>
            SymbolEqualityComparer.Default.Equals(iface, iSystemType));
    }
}
