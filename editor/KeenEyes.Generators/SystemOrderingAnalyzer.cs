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

    /// <summary>
    /// KEEN006: Circular dependency detected.
    /// </summary>
    public static readonly DiagnosticDescriptor CircularDependency = new(
        id: "KEEN006",
        title: "Circular ordering dependency detected",
        messageFormat: "Circular dependency: '{0}' and '{1}' reference each other via {2} attributes",
        category: "KeenEyes.SystemOrdering",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Systems have circular ordering dependencies. If A runs before B and B runs before A, the execution order cannot be determined. Remove one of the constraints to break the cycle.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            SelfReferentialConstraint,
            TargetNotAClass,
            MissingSystemAttribute,
            TargetNotASystem,
            CircularDependency);

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
        // Validate that we have a valid type argument
        if (attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol targetType)
        {
            return; // Skip invalid attribute - Roslyn will report separate error
        }

        if (attribute.ApplicationSyntaxReference is null)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference.GetSyntax().GetLocation();

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

        // KEEN006: Check for circular dependencies
        // If A has [RunBefore(B)], check if B has [RunBefore(A)] or [RunAfter(A)]
        // If A has [RunAfter(B)], check if B has [RunAfter(A)] or [RunBefore(A)]
        if (targetType.TypeKind == TypeKind.Class)
        {
            CheckCircularDependency(context, declaringType, targetType, location, attrShortName);
        }
    }

    private static void CheckCircularDependency(
        SymbolAnalysisContext context,
        INamedTypeSymbol declaringType,
        INamedTypeSymbol targetType,
        Location location,
        string attrShortName)
    {
        // Look for reciprocal ordering constraints on the target type
        foreach (var targetAttr in targetType.GetAttributes())
        {
            var targetAttrName = targetAttr.AttributeClass?.ToDisplayString();
            if (targetAttrName != RunBeforeAttribute && targetAttrName != RunAfterAttribute)
            {
                continue;
            }

            if (targetAttr.ConstructorArguments.Length == 0 ||
                targetAttr.ConstructorArguments[0].Value is not INamedTypeSymbol referencedType)
            {
                continue;
            }

            // Check if target references back to declaring type
            if (!SymbolEqualityComparer.Default.Equals(referencedType, declaringType))
            {
                continue;
            }

            // Found a reference back - determine if it creates a cycle
            var targetAttrShortName = targetAttrName.EndsWith("RunBeforeAttribute")
                ? "RunBefore"
                : "RunAfter";

            // A [RunBefore(B)] + B [RunBefore(A)] = cycle (A before B, B before A)
            // A [RunBefore(B)] + B [RunAfter(A)] = no cycle (A before B, A before B - same direction)
            // A [RunAfter(B)] + B [RunAfter(A)] = cycle (B before A, A before B)
            // A [RunAfter(B)] + B [RunBefore(A)] = no cycle (B before A, B before A - same direction)

            var isCycle = (attrShortName == "RunBefore" && targetAttrShortName == "RunBefore") ||
                          (attrShortName == "RunAfter" && targetAttrShortName == "RunAfter");

            if (isCycle)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CircularDependency,
                    location,
                    declaringType.Name,
                    targetType.Name,
                    attrShortName));
            }
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
