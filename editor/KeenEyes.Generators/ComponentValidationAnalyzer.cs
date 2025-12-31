using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators;

/// <summary>
/// Roslyn analyzer that validates RequiresComponent and ConflictsWith attribute usage.
/// Reports compile-time errors and warnings for invalid component validation constraints.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentValidationAnalyzer : DiagnosticAnalyzer
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEyes.TagComponentAttribute";
    private const string RequiresComponentAttribute = "KeenEyes.RequiresComponentAttribute";
    private const string ConflictsWithAttribute = "KeenEyes.ConflictsWithAttribute";
    private const string IComponentInterface = "KeenEyes.IComponent";
    private const string ITagComponentInterface = "KeenEyes.ITagComponent";

    /// <summary>
    /// KEEN010: Self-referential component constraint.
    /// </summary>
    public static readonly DiagnosticDescriptor SelfReferentialConstraint = new(
        id: "KEEN010",
        title: "Self-referential component constraint",
        messageFormat: "Component '{0}' cannot reference itself in {1} attribute",
        category: "KeenEyes.ComponentValidation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A component cannot specify itself as a dependency or conflict. This would create an unsatisfiable constraint.");

    /// <summary>
    /// KEEN011: Target type is not a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor TargetNotAStruct = new(
        id: "KEEN011",
        title: "Component constraint target must be a struct",
        messageFormat: "Type '{0}' in {1} attribute must be a struct, not a {2}",
        category: "KeenEyes.ComponentValidation",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "RequiresComponent and ConflictsWith attributes must reference struct types. Components must be value types (structs).");

    /// <summary>
    /// KEEN012: Target is not a component.
    /// </summary>
    public static readonly DiagnosticDescriptor TargetNotAComponent = new(
        id: "KEEN012",
        title: "Constraint target should be a component",
        messageFormat: "Type '{0}' referenced in {1} attribute does not appear to implement IComponent or ITagComponent",
        category: "KeenEyes.ComponentValidation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Types referenced in RequiresComponent and ConflictsWith should implement IComponent or ITagComponent to be valid component constraint targets.");

    /// <summary>
    /// KEEN013: Missing [Component] or [TagComponent] attribute.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingComponentAttribute = new(
        id: "KEEN013",
        title: "Missing [Component] or [TagComponent] attribute",
        messageFormat: "Struct '{0}' uses {1} but is missing the [Component] or [TagComponent] attribute",
        category: "KeenEyes.ComponentValidation",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Structs using RequiresComponent or ConflictsWith attributes should also have the [Component] or [TagComponent] attribute to enable source generation of validation metadata.");

    /// <summary>
    /// KEEN014: Mutual conflict detected.
    /// </summary>
    public static readonly DiagnosticDescriptor MutualConflictWarning = new(
        id: "KEEN014",
        title: "Consider adding mutual ConflictsWith",
        messageFormat: "Component '{0}' conflicts with '{1}' - consider adding [ConflictsWith(typeof({0}))] to '{1}' for bidirectional conflict detection",
        category: "KeenEyes.ComponentValidation",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "When component A conflicts with B, it's often desirable for B to also conflict with A. This ensures the conflict is detected regardless of which component is added first.");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            SelfReferentialConstraint,
            TargetNotAStruct,
            TargetNotAComponent,
            MissingComponentAttribute,
            MutualConflictWarning);

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

        // Only analyze structs
        if (typeSymbol.TypeKind != TypeKind.Struct)
        {
            return;
        }

        var hasComponentAttribute = false;
        var hasValidationAttribute = false;

        foreach (var attribute in typeSymbol.GetAttributes())
        {
            var attributeName = attribute.AttributeClass?.ToDisplayString();

            if (attributeName == ComponentAttribute || attributeName == TagComponentAttribute)
            {
                hasComponentAttribute = true;
            }
            else if (attributeName == RequiresComponentAttribute || attributeName == ConflictsWithAttribute)
            {
                hasValidationAttribute = true;
                AnalyzeValidationAttribute(context, typeSymbol, attribute, attributeName!);
            }
        }

        // KEEN013: Check for RequiresComponent/ConflictsWith without [Component]
        if (hasValidationAttribute && !hasComponentAttribute)
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location != null)
            {
                var attrName = typeSymbol.GetAttributes()
                    .FirstOrDefault(a =>
                        a.AttributeClass?.ToDisplayString() == RequiresComponentAttribute ||
                        a.AttributeClass?.ToDisplayString() == ConflictsWithAttribute)?
                    .AttributeClass?.Name ?? "RequiresComponent/ConflictsWith";

                context.ReportDiagnostic(Diagnostic.Create(
                    MissingComponentAttribute,
                    location,
                    typeSymbol.Name,
                    attrName));
            }
        }
    }

    private static void AnalyzeValidationAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol declaringType,
        AttributeData attribute,
        string attributeName)
    {
        if (attribute.ConstructorArguments.Length == 0 ||
            attribute.ConstructorArguments[0].Value is not INamedTypeSymbol targetType)
        {
            return;
        }

        var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        if (location == null)
        {
            return;
        }

        var attrShortName = attributeName.Contains("RequiresComponent")
            ? "RequiresComponent"
            : "ConflictsWith";

        // KEEN010: Check for self-reference
        if (SymbolEqualityComparer.Default.Equals(targetType, declaringType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                SelfReferentialConstraint,
                location,
                declaringType.Name,
                attrShortName));
            return;
        }

        // KEEN011: Check if target is a struct
        if (targetType.TypeKind != TypeKind.Struct)
        {
            var typeKindName = targetType.TypeKind switch
            {
                TypeKind.Class => "class",
                TypeKind.Interface => "interface",
                TypeKind.Enum => "enum",
                TypeKind.Delegate => "delegate",
                _ => targetType.TypeKind.ToString().ToLowerInvariant()
            };

            context.ReportDiagnostic(Diagnostic.Create(
                TargetNotAStruct,
                location,
                targetType.Name,
                attrShortName,
                typeKindName));
            return;
        }

        // KEEN012: Check if target implements IComponent or ITagComponent
        if (!ImplementsComponentInterface(targetType, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                TargetNotAComponent,
                location,
                targetType.Name,
                attrShortName));
        }

        // KEEN014: For ConflictsWith, check if target has mutual conflict
        if (attrShortName == "ConflictsWith")
        {
            CheckMutualConflict(context, declaringType, targetType, location);
        }
    }

    private static bool ImplementsComponentInterface(INamedTypeSymbol typeSymbol, Compilation compilation)
    {
        // Check if type has [Component] or [TagComponent] attribute
        // (it will implement IComponent/ITagComponent after source generation)
        var hasComponentAttribute = typeSymbol.GetAttributes()
            .Any(a =>
                a.AttributeClass?.ToDisplayString() == ComponentAttribute ||
                a.AttributeClass?.ToDisplayString() == TagComponentAttribute);

        if (hasComponentAttribute)
        {
            return true;
        }

        // Check if type directly implements IComponent or ITagComponent
        var iComponentType = compilation.GetTypeByMetadataName(IComponentInterface);
        var iTagComponentType = compilation.GetTypeByMetadataName(ITagComponentInterface);

        if (iComponentType == null && iTagComponentType == null)
        {
            // Interface not found in compilation - assume valid to avoid false positives
            return true;
        }

        return typeSymbol.AllInterfaces.Any(iface =>
            SymbolEqualityComparer.Default.Equals(iface, iComponentType) ||
            SymbolEqualityComparer.Default.Equals(iface, iTagComponentType));
    }

    private static void CheckMutualConflict(
        SymbolAnalysisContext context,
        INamedTypeSymbol declaringType,
        INamedTypeSymbol targetType,
        Location location)
    {
        // Check if target type has a ConflictsWith attribute pointing back to declaring type
        var hasMutualConflict = targetType.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == ConflictsWithAttribute)
            .Any(a =>
                a.ConstructorArguments.Length > 0 &&
                a.ConstructorArguments[0].Value is INamedTypeSymbol conflictTarget &&
                SymbolEqualityComparer.Default.Equals(conflictTarget, declaringType));

        if (!hasMutualConflict)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MutualConflictWarning,
                location,
                declaringType.Name,
                targetType.Name));
        }
    }
}
