using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KeenEyes.Generators;

/// <summary>
/// Analyzer that validates component migration methods marked with [MigrateFrom]
/// and [DefaultValue] attributes on component fields.
/// </summary>
/// <remarks>
/// <para>
/// Validates that migration methods:
/// <list type="bullet">
/// <item><description>Are static (KEEN110)</description></item>
/// <item><description>Return the containing component type (KEEN111)</description></item>
/// <item><description>Take a single JsonElement parameter (KEEN112)</description></item>
/// <item><description>Have a FromVersion less than the component's current Version (KEEN113)</description></item>
/// <item><description>Don't have gaps in the migration chain (KEEN114 - warning)</description></item>
/// <item><description>Don't have duplicate migration versions (KEEN115)</description></item>
/// <item><description>Are defined in a [Component] type (KEEN116)</description></item>
/// <item><description>Component is serializable (KEEN117)</description></item>
/// </list>
/// </para>
/// <para>
/// Also validates [DefaultValue] attributes:
/// <list type="bullet">
/// <item><description>Value type is compatible with the field type (KEEN118)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Cycle Detection:</strong> Migration cycles are structurally impossible due to the
/// version constraint (KEEN113). Since each [MigrateFrom(v)] migrates from version v to v+1,
/// and v must be less than the component's current version, all migration edges point forward
/// in version space. Combined with the no-duplicates rule (KEEN115), this guarantees a
/// directed acyclic graph (DAG) structure.
/// </para>
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ComponentMigrationAnalyzer : DiagnosticAnalyzer
{
    private const string MigrateFromAttribute = "KeenEyes.MigrateFromAttribute";
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string DefaultValueAttribute = "KeenEyes.DefaultValueAttribute";
    private const string JsonElementType = "System.Text.Json.JsonElement";

    /// <summary>
    /// KEEN110: Migration method must be static.
    /// </summary>
    public static readonly DiagnosticDescriptor MigrationMethodMustBeStatic = new(
        id: "KEEN110",
        title: "Migration method must be static",
        messageFormat: "Migration method '{0}' must be static",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN111: Migration method must return the component type.
    /// </summary>
    public static readonly DiagnosticDescriptor MigrationMethodMustReturnComponentType = new(
        id: "KEEN111",
        title: "Migration method must return component type",
        messageFormat: "Migration method '{0}' must return '{1}', not '{2}'",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN112: Migration method must take a JsonElement parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor MigrationMethodMustTakeJsonElement = new(
        id: "KEEN112",
        title: "Migration method must take JsonElement parameter",
        messageFormat: "Migration method '{0}' must have a single parameter of type 'System.Text.Json.JsonElement'",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN113: Migration version must be less than current component version.
    /// </summary>
    public static readonly DiagnosticDescriptor MigrationVersionTooHigh = new(
        id: "KEEN113",
        title: "Migration version must be less than component version",
        messageFormat: "Migration version {0} must be less than component version {1}",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN114: Missing migration for version.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingMigrationVersion = new(
        id: "KEEN114",
        title: "Missing migration for version",
        messageFormat: "Component '{0}' is missing migration from version {1}; migrations defined: {2}",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN115: Duplicate migration for version.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateMigrationVersion = new(
        id: "KEEN115",
        title: "Duplicate migration for version",
        messageFormat: "Multiple migrations defined for version {0} in component '{1}'",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN116: Migration method not in component type.
    /// </summary>
    public static readonly DiagnosticDescriptor MigrationMethodNotInComponent = new(
        id: "KEEN116",
        title: "Migration method must be in a component type",
        messageFormat: "Migration method '{0}' must be defined in a type marked with [Component]",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN117: Component must be serializable to use migrations.
    /// </summary>
    public static readonly DiagnosticDescriptor ComponentMustBeSerializable = new(
        id: "KEEN117",
        title: "Component must be serializable to use migrations",
        messageFormat: "Component '{0}' must have [Component(Serializable = true)] to use [MigrateFrom]",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN118: DefaultValue type mismatch.
    /// </summary>
    public static readonly DiagnosticDescriptor DefaultValueTypeMismatch = new(
        id: "KEEN118",
        title: "DefaultValue type mismatch",
        messageFormat: "[DefaultValue] value of type '{0}' is not compatible with field type '{1}'",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// KEEN119: DefaultValue on non-serializable component field.
    /// </summary>
    public static readonly DiagnosticDescriptor DefaultValueOnNonSerializableComponent = new(
        id: "KEEN119",
        title: "DefaultValue on non-serializable component",
        messageFormat: "[DefaultValue] on field '{0}' has no effect because component '{1}' is not serializable",
        category: "KeenEyes.Migration",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            MigrationMethodMustBeStatic,
            MigrationMethodMustReturnComponentType,
            MigrationMethodMustTakeJsonElement,
            MigrationVersionTooHigh,
            MissingMigrationVersion,
            DuplicateMigrationVersion,
            MigrationMethodNotInComponent,
            ComponentMustBeSerializable,
            DefaultValueTypeMismatch,
            DefaultValueOnNonSerializableComponent);

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        // Find [MigrateFrom] attributes on this method
        var migrateFromAttrs = methodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MigrateFromAttribute)
            .ToList();

        if (migrateFromAttrs.Count == 0)
        {
            return;
        }

        // Get the containing type
        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
        {
            return;
        }

        // Check if containing type has [Component] attribute
        var componentAttr = containingType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ComponentAttribute);

        if (componentAttr is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MigrationMethodNotInComponent,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
            return;
        }

        // Check if component is serializable
        var isSerializable = componentAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "Serializable")
            .Value.Value is true;

        if (!isSerializable)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                ComponentMustBeSerializable,
                methodSymbol.Locations.FirstOrDefault(),
                containingType.Name));
            return;
        }

        // Get component version
        var componentVersion = 1;
        var versionArg = componentAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "Version");
        if (versionArg.Value.Value is int v)
        {
            componentVersion = v;
        }

        // Validate method is static
        if (!methodSymbol.IsStatic)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MigrationMethodMustBeStatic,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        // Validate return type matches containing type
        if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ReturnType, containingType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MigrationMethodMustReturnComponentType,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name,
                containingType.Name,
                methodSymbol.ReturnType.ToDisplayString()));
        }

        // Validate parameter is JsonElement
        if (methodSymbol.Parameters.Length != 1 ||
            methodSymbol.Parameters[0].Type.ToDisplayString() != JsonElementType)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                MigrationMethodMustTakeJsonElement,
                methodSymbol.Locations.FirstOrDefault(),
                methodSymbol.Name));
        }

        // Validate each [MigrateFrom] version
        foreach (var attr in migrateFromAttrs)
        {
            if (attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is int fromVersion &&
                fromVersion >= componentVersion)
            {
                var location = attr.ApplicationSyntaxReference?.GetSyntax()?.GetLocation()
                    ?? methodSymbol.Locations.FirstOrDefault();

                context.ReportDiagnostic(Diagnostic.Create(
                    MigrationVersionTooHigh,
                    location,
                    fromVersion,
                    componentVersion));
            }
        }
    }

    private static void AnalyzeType(SymbolAnalysisContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.Symbol;

        // Check if this is a component
        var componentAttr = typeSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ComponentAttribute);

        if (componentAttr is null)
        {
            return;
        }

        // Get component version
        var componentVersion = 1;
        var versionArg = componentAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "Version");
        if (versionArg.Value.Value is int v)
        {
            componentVersion = v;
        }

        // Only check for migration gaps if version > 1
        if (componentVersion <= 1)
        {
            return;
        }

        // Check if component is serializable (only then do we need migrations)
        var isSerializable = componentAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "Serializable")
            .Value.Value is true;

        if (!isSerializable)
        {
            return;
        }

        // Check if any field has [DefaultValue] - these can auto-generate migrations
        var hasFieldsWithDefaultValue = typeSymbol.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(f => !f.IsStatic && !f.IsConst)
            .Any(f => f.GetAttributes()
                .Any(a => a.AttributeClass?.ToDisplayString() == DefaultValueAttribute));

        // Collect all migration versions defined
        var migrationVersions = new HashSet<int>();
        var duplicateVersions = new HashSet<int>();

        foreach (var member in typeSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            foreach (var attr in member.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() != MigrateFromAttribute)
                {
                    continue;
                }

                if (attr.ConstructorArguments.Length > 0 &&
                    attr.ConstructorArguments[0].Value is int fromVersion &&
                    !migrationVersions.Add(fromVersion))
                {
                    duplicateVersions.Add(fromVersion);
                }
            }
        }

        // Report duplicate versions
        foreach (var duplicateVersion in duplicateVersions)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DuplicateMigrationVersion,
                typeSymbol.Locations.FirstOrDefault(),
                duplicateVersion,
                typeSymbol.Name));
        }

        // Check for gaps in migration chain
        // Only report if at least one migration is defined (user is opting into migrations)
        // AND the component doesn't have fields with [DefaultValue] (which auto-generate migrations)
        if (migrationVersions.Count > 0 && !hasFieldsWithDefaultValue)
        {
            var missingVersions = new List<int>();
            for (var ver = 1; ver < componentVersion; ver++)
            {
                if (!migrationVersions.Contains(ver))
                {
                    missingVersions.Add(ver);
                }
            }

            if (missingVersions.Count > 0)
            {
                var definedVersionsStr = migrationVersions.Count > 0
                    ? string.Join(", ", migrationVersions.OrderBy(x => x))
                    : "none";

                context.ReportDiagnostic(Diagnostic.Create(
                    MissingMigrationVersion,
                    typeSymbol.Locations.FirstOrDefault(),
                    typeSymbol.Name,
                    string.Join(", ", missingVersions),
                    definedVersionsStr));
            }
        }
    }

    private static void AnalyzeField(SymbolAnalysisContext context)
    {
        var fieldSymbol = (IFieldSymbol)context.Symbol;

        // Find [DefaultValue] attribute on this field
        var defaultValueAttr = fieldSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == DefaultValueAttribute);

        if (defaultValueAttr is null)
        {
            return;
        }

        // Get the containing type
        var containingType = fieldSymbol.ContainingType;
        if (containingType is null)
        {
            return;
        }

        // Check if containing type has [Component] attribute
        var componentAttr = containingType.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ComponentAttribute);

        if (componentAttr is null)
        {
            // Not a component - DefaultValue has no migration effect, but may be used for builder
            return;
        }

        // Check if component is serializable
        var isSerializable = componentAttr.NamedArguments
            .FirstOrDefault(a => a.Key == "Serializable")
            .Value.Value is true;

        if (!isSerializable)
        {
            // [DefaultValue] is also used for builder defaults, so we don't warn here.
            // It's valid to use [DefaultValue] on non-serializable components for builder generation.
            return;
        }

        // Validate type compatibility
        if (defaultValueAttr.ConstructorArguments.Length == 0)
        {
            return;
        }

        var defaultValue = defaultValueAttr.ConstructorArguments[0];
        var valueType = defaultValue.Type;
        var fieldType = fieldSymbol.Type;

        // Check if the value is null
        if (defaultValue.Value is null)
        {
            // null is valid for nullable types
            if (fieldType.NullableAnnotation != NullableAnnotation.Annotated &&
                fieldType.IsValueType &&
                fieldType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DefaultValueTypeMismatch,
                    fieldSymbol.Locations.FirstOrDefault(),
                    "null",
                    fieldType.ToDisplayString()));
            }
            return;
        }

        // Check type compatibility
        if (!IsTypeCompatible(valueType, fieldType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DefaultValueTypeMismatch,
                fieldSymbol.Locations.FirstOrDefault(),
                valueType?.ToDisplayString() ?? "unknown",
                fieldType.ToDisplayString()));
        }
    }

    private static bool IsTypeCompatible(ITypeSymbol? valueType, ITypeSymbol fieldType)
    {
        if (valueType is null)
        {
            return false;
        }

        // Exact type match
        if (SymbolEqualityComparer.Default.Equals(valueType, fieldType))
        {
            return true;
        }

        // Handle special cases for numeric types (constants can be implicitly narrowed)
        // e.g., [DefaultValue(0)] on a float field is valid because 0 is implicitly convertible
        if (IsNumericType(valueType) && IsNumericType(fieldType))
        {
            return true;
        }

        // Handle enum types - value is stored as underlying type
        if (fieldType.TypeKind == TypeKind.Enum)
        {
            var underlyingType = ((INamedTypeSymbol)fieldType).EnumUnderlyingType;
            if (underlyingType is not null && SymbolEqualityComparer.Default.Equals(valueType, underlyingType))
            {
                return true;
            }
        }

        // Handle string type - string literals are always compatible with string fields
        if (valueType.SpecialType == SpecialType.System_String &&
            fieldType.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        // Handle bool type
        if (valueType.SpecialType == SpecialType.System_Boolean &&
            fieldType.SpecialType == SpecialType.System_Boolean)
        {
            return true;
        }

        return false;
    }

    private static bool IsNumericType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Byte => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,
            SpecialType.System_Decimal => true,
            _ => false
        };
    }
}
