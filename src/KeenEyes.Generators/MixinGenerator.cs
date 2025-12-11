using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates component field mixins for types marked with [Mixin(typeof(T))].
/// Mixins copy all fields from one or more source structs into the target component at compile-time.
/// </summary>
[Generator]
public sealed class MixinGenerator : IIncrementalGenerator
{
    private const string MixinAttribute = "KeenEyes.MixinAttribute";
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string TagComponentAttribute = "KeenEyes.TagComponentAttribute";
    private const int MaxCircularReferenceDepth = 10;

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [Component] or [TagComponent] that also have [Mixin] attributes
        var componentWithMixinProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetMixinInfo(ctx))
            .Where(static info => info is not null && (info.MixinTypes.Length > 0 || info.Diagnostics.Length > 0));

        var tagComponentWithMixinProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TagComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetMixinInfo(ctx))
            .Where(static info => info is not null && (info.MixinTypes.Length > 0 || info.Diagnostics.Length > 0));

        // Combine both providers
        var allMixins = componentWithMixinProvider.Collect()
            .Combine(tagComponentWithMixinProvider.Collect());

        // Generate the code
        context.RegisterSourceOutput(allMixins, static (ctx, source) =>
        {
            var (components, tagComponents) = source;
            var allInfos = components.Concat(tagComponents).ToImmutableArray();

            if (allInfos.Length == 0)
            {
                return;
            }

            foreach (var info in allInfos)
            {
                if (info is null)
                {
                    continue;
                }

                // Report diagnostics
                foreach (var diag in info.Diagnostics)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        diag.Descriptor,
                        diag.Location,
                        diag.MessageArgs));
                }

                // Only generate code for valid mixins
                if (!info.IsValid)
                {
                    continue;
                }

                var mixinSource = GenerateMixinPartial(info);
                ctx.AddSource($"{info.FullName}.Mixin.g.cs", SourceText.From(mixinSource, Encoding.UTF8));
            }
        });
    }

    private static MixinInfo? GetMixinInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var diagnostics = new List<DiagnosticInfo>();
        var mixinTypes = new List<MixinTypeInfo>();

        // Get all [Mixin(typeof(T))] attributes
        var mixinAttributes = typeSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MixinAttribute)
            .ToArray();

        if (mixinAttributes.Length == 0)
        {
            return null;
        }

        var processedMixins = new HashSet<string>();

        foreach (var mixinAttr in mixinAttributes)
        {
            if (mixinAttr.ConstructorArguments.Length == 0 ||
                mixinAttr.ConstructorArguments[0].Value is not INamedTypeSymbol mixinType)
            {
                continue;
            }

            var location = mixinAttr.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                           ?? typeSymbol.Locations.FirstOrDefault();

            // KEEN026: Validate mixin type is a struct
            if (mixinType.TypeKind != TypeKind.Struct)
            {
                if (location is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.MixinMustBeStruct,
                        location,
                        [mixinType.ToDisplayString(), typeSymbol.Name]));
                }
                continue;
            }

            // KEEN028: Validate mixin type is accessible
            if (mixinType.DeclaredAccessibility == Accessibility.Private ||
                mixinType.DeclaredAccessibility == Accessibility.Protected)
            {
                if (location is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.MixinTypeNotAccessible,
                        location,
                        [mixinType.ToDisplayString(), typeSymbol.Name]));
                }
                continue;
            }

            // Extract fields from mixin, checking for circular references
            var fields = ExtractMixinFields(
                mixinType,
                typeSymbol,
                context.SemanticModel.Compilation,
                [],
                depth: 0,
                diagnostics,
                location ?? typeSymbol.Locations.FirstOrDefault()!);

            if (fields.Count > 0)
            {
                var mixinTypeName = mixinType.ToDisplayString();

                // Check for duplicate mixins
                if (processedMixins.Contains(mixinTypeName))
                {
                    continue; // Skip duplicate
                }

                processedMixins.Add(mixinTypeName);

                mixinTypes.Add(new MixinTypeInfo(
                    mixinType.Name,
                    mixinTypeName,
                    fields.ToImmutableArray()));
            }
        }

        // Check for field name conflicts across all mixins and the component itself
        var allMixinFields = mixinTypes.SelectMany(m => m.Fields).ToArray();
        var fieldGroups = allMixinFields.GroupBy(f => f.Name).Where(g => g.Count() > 1).ToArray();

        if (fieldGroups.Length > 0)
        {
            foreach (var group in fieldGroups)
            {
                var location = typeSymbol.Locations.FirstOrDefault();
                if (location is not null)
                {
                    var sources = string.Join(", ", group.Select(f => f.SourceMixin).Distinct());
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.MixinFieldConflict,
                        location,
                        [group.Key, typeSymbol.Name, sources]));
                }
            }
        }

        // Check for conflicts between mixin fields and component's own fields
        var componentFields = new HashSet<string>(
            typeSymbol.GetMembers()
                .OfType<IFieldSymbol>()
                .Where(f => !f.IsStatic && !f.IsConst)
                .Select(f => f.Name));

        var conflictingFields = allMixinFields.Where(f => componentFields.Contains(f.Name)).ToArray();
        if (conflictingFields.Length > 0)
        {
            foreach (var field in conflictingFields)
            {
                var location = typeSymbol.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.MixinFieldConflict,
                        location,
                        [field.Name, typeSymbol.Name, $"{field.SourceMixin} and {typeSymbol.Name}"]));
                }
            }
        }

        var isValid = diagnostics.All(d => d.Descriptor.DefaultSeverity != DiagnosticSeverity.Error);

        return new MixinInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            mixinTypes.ToImmutableArray(),
            diagnostics.ToImmutableArray(),
            isValid);
    }

    private static List<MixinFieldInfo> ExtractMixinFields(
        INamedTypeSymbol mixinType,
        INamedTypeSymbol targetType,
        Compilation compilation,
        HashSet<string> visited,
        int depth,
        List<DiagnosticInfo> diagnostics,
        Location errorLocation)
    {
        var fields = new List<MixinFieldInfo>();

        // KEEN027: Check for circular reference depth
        if (depth > MaxCircularReferenceDepth)
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.MixinCircularReference,
                errorLocation,
                [targetType.Name, string.Join(" -> ", visited)]));
            return fields;
        }

        var mixinTypeName = mixinType.ToDisplayString();

        // KEEN027: Check for circular reference
        if (visited.Contains(mixinTypeName))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.MixinCircularReference,
                errorLocation,
                [targetType.Name, string.Join(" -> ", visited.Append(mixinTypeName))]));
            return fields;
        }

        visited.Add(mixinTypeName);

        // Extract all instance fields from the mixin
        foreach (var member in mixinType.GetMembers())
        {
            if (member is not IFieldSymbol field)
            {
                continue;
            }

            if (field.IsStatic || field.IsConst)
            {
                continue;
            }

            fields.Add(new MixinFieldInfo(
                field.Name,
                field.Type.ToDisplayString(),
                mixinType.Name));
        }

        // Check if the mixin itself has [Mixin] attributes (transitive mixins)
        var nestedMixinAttributes = mixinType.GetAttributes()
            .Where(a => a.AttributeClass?.ToDisplayString() == MixinAttribute)
            .ToArray();

        foreach (var nestedMixinAttr in nestedMixinAttributes)
        {
            if (nestedMixinAttr.ConstructorArguments.Length == 0 ||
                nestedMixinAttr.ConstructorArguments[0].Value is not INamedTypeSymbol nestedMixinType)
            {
                continue;
            }

            // Recursively extract fields from nested mixin
            var nestedFields = ExtractMixinFields(
                nestedMixinType,
                targetType,
                compilation,
                visited,
                depth + 1,
                diagnostics,
                errorLocation);

            fields.AddRange(nestedFields);
        }

        visited.Remove(mixinTypeName);

        return fields;
    }

    private static string GenerateMixinPartial(MixinInfo info)
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

        sb.AppendLine($"// Mixin fields from: {string.Join(", ", info.MixinTypes.Select(m => m.Name))}");
        sb.AppendLine($"partial struct {info.Name}");
        sb.AppendLine("{");

        // Generate fields from all mixins
        foreach (var mixin in info.MixinTypes)
        {
            if (mixin.Fields.Length > 0)
            {
                sb.AppendLine($"    // Fields from {mixin.Name} mixin");
            }

            foreach (var field in mixin.Fields)
            {
                sb.AppendLine($"    public {field.Type} {field.Name};");
            }

            if (mixin.Fields.Length > 0)
            {
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private sealed record MixinInfo(
        string Name,
        string Namespace,
        string FullName,
        ImmutableArray<MixinTypeInfo> MixinTypes,
        ImmutableArray<DiagnosticInfo> Diagnostics,
        bool IsValid);

    private sealed record MixinTypeInfo(
        string Name,
        string FullName,
        ImmutableArray<MixinFieldInfo> Fields);

    private sealed record MixinFieldInfo(
        string Name,
        string Type,
        string SourceMixin);

    private sealed record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location Location,
        object[] MessageArgs);
}

/// <summary>
/// Diagnostic descriptors for mixin generation errors.
/// </summary>
internal static partial class Diagnostics
{
    /// <summary>
    /// KEEN026: Mixin type must be a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor MixinMustBeStruct = new(
        id: "KEEN026",
        title: "Mixin type must be a struct",
        messageFormat: "Mixin type '{0}' referenced in component '{1}' must be a struct, not a class or other type",
        category: "KeenEyes.Mixin",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mixin types must be value types (structs) to maintain consistency with component semantics.");

    /// <summary>
    /// KEEN027: Circular mixin reference detected.
    /// </summary>
    public static readonly DiagnosticDescriptor MixinCircularReference = new(
        id: "KEEN027",
        title: "Circular mixin reference detected",
        messageFormat: "Component '{0}' has a circular mixin reference: {1}",
        category: "KeenEyes.Mixin",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A mixin cannot directly or indirectly reference itself. This would create an infinite recursion during field generation.");

    /// <summary>
    /// KEEN028: Mixin type not found or inaccessible.
    /// </summary>
    public static readonly DiagnosticDescriptor MixinTypeNotAccessible = new(
        id: "KEEN028",
        title: "Mixin type not accessible",
        messageFormat: "Mixin type '{0}' referenced in component '{1}' is not accessible (it may be private or protected)",
        category: "KeenEyes.Mixin",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Mixin types must be accessible (public or internal) to be used in component mixins.");

    /// <summary>
    /// KEEN029: Multiple mixins contain conflicting field names.
    /// </summary>
    public static readonly DiagnosticDescriptor MixinFieldConflict = new(
        id: "KEEN029",
        title: "Mixin field name conflict",
        messageFormat: "Field '{0}' in component '{1}' has conflicting definitions from: {2}",
        category: "KeenEyes.Mixin",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Multiple mixins or a mixin and the component cannot define fields with the same name. Rename one of the conflicting fields.");
}
