using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace KeenEyes.Generators;

/// <summary>
/// Generates bundle implementations and fluent builder methods for types marked with [Bundle].
/// Bundles are compositions of multiple components commonly used together.
/// </summary>
[Generator]
public sealed class BundleGenerator : IIncrementalGenerator
{
    private const string BundleAttribute = "KeenEyes.BundleAttribute";
    private const string IComponentInterface = "KeenEyes.IComponent";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all types with [Bundle] attribute (allow both structs and classes for validation)
        var bundleProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BundleAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax or ClassDeclarationSyntax,
                transform: static (ctx, _) => GetBundleInfo(ctx))
            .Where(static info => info is not null);

        // Generate the code
        context.RegisterSourceOutput(bundleProvider, static (ctx, bundleInfo) =>
        {
            if (bundleInfo is null)
            {
                return;
            }

            // Report diagnostics
            foreach (var diag in bundleInfo.Diagnostics)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    diag.Descriptor,
                    diag.Location,
                    diag.MessageArgs));
            }

            // Only generate code for valid bundles
            if (!bundleInfo.IsValid)
            {
                return;
            }

            // Generate bundle partial struct with IBundle implementation and constructor
            var bundleSource = GenerateBundlePartial(bundleInfo);
            ctx.AddSource($"{bundleInfo.FullName}.g.cs", SourceText.From(bundleSource, Encoding.UTF8));
        });

        // Generate EntityBuilder extensions for all bundles
        var allBundles = bundleProvider.Collect();
        context.RegisterSourceOutput(allBundles, static (ctx, bundles) =>
        {
            // Filter to only valid bundles
            var validBundles = bundles.Where(b => b is not null && b.IsValid).ToImmutableArray();

            if (validBundles.Length == 0)
            {
                return;
            }

            var builderSource = GenerateEntityBuilderExtensions(validBundles!);
            ctx.AddSource("EntityBuilder.Bundles.g.cs", SourceText.From(builderSource, Encoding.UTF8));

            var getBundleSource = GenerateGetBundleExtensions(validBundles!);
            ctx.AddSource("World.GetBundle.g.cs", SourceText.From(getBundleSource, Encoding.UTF8));

            var queryBundleSource = GenerateQueryBundleExtensions(validBundles!);
            ctx.AddSource("World.QueryBundle.g.cs", SourceText.From(queryBundleSource, Encoding.UTF8));
        });

        // Generate ref structs for each bundle
        context.RegisterSourceOutput(bundleProvider, static (ctx, bundleInfo) =>
        {
            if (bundleInfo is null || !bundleInfo.IsValid)
            {
                return;
            }

            var refStructSource = GenerateBundleRefStruct(bundleInfo);
            ctx.AddSource($"{bundleInfo.FullName}Ref.g.cs", SourceText.From(refStructSource, Encoding.UTF8));
        });
    }

    private static BundleInfo? GetBundleInfo(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol typeSymbol)
        {
            return null;
        }

        var diagnostics = new List<DiagnosticInfo>();

        // Validate: must be a struct
        if (typeSymbol.TypeKind != TypeKind.Struct)
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location is not null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.BundleMustBeStruct,
                    location,
                    [typeSymbol.Name]));
            }
            return new BundleInfo(
                typeSymbol.Name,
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                ImmutableArray<ComponentFieldInfo>.Empty,
                diagnostics.ToImmutableArray(),
                IsValid: false);
        }

        var fields = new List<ComponentFieldInfo>();
        var compilation = context.SemanticModel.Compilation;

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

            // Check for circular reference first (bundle containing itself)
            if (field.Type.ToDisplayString() == typeSymbol.ToDisplayString())
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.BundleCircularReference,
                        location,
                        [typeSymbol.Name, field.Name]));
                }
                return new BundleInfo(
                    typeSymbol.Name,
                    typeSymbol.ContainingNamespace.ToDisplayString(),
                    typeSymbol.ToDisplayString(),
                    ImmutableArray<ComponentFieldInfo>.Empty,
                    diagnostics.ToImmutableArray(),
                    IsValid: false);
            }

            // Validate: field must be a component type
            if (!IsComponentType(field.Type, compilation))
            {
                var location = field.Locations.FirstOrDefault();
                if (location is not null)
                {
                    diagnostics.Add(new DiagnosticInfo(
                        Diagnostics.BundleFieldMustBeComponent,
                        location,
                        [field.Name, typeSymbol.Name, field.Type.ToDisplayString()]));
                }
                continue; // Skip invalid field but continue processing others
            }

            fields.Add(new ComponentFieldInfo(
                field.Name,
                field.Type.ToDisplayString()));
        }

        // Validate: must have at least one field
        if (fields.Count == 0)
        {
            var location = typeSymbol.Locations.FirstOrDefault();
            if (location is not null)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.BundleMustHaveFields,
                    location,
                    [typeSymbol.Name]));
            }
            return new BundleInfo(
                typeSymbol.Name,
                typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.ToDisplayString(),
                ImmutableArray<ComponentFieldInfo>.Empty,
                diagnostics.ToImmutableArray(),
                IsValid: false);
        }

        return new BundleInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            fields.ToImmutableArray(),
            diagnostics.ToImmutableArray(),
            IsValid: true);
    }

    private static bool IsComponentType(ITypeSymbol typeSymbol, Compilation compilation)
    {
        // Must be a struct
        if (typeSymbol.TypeKind != TypeKind.Struct)
        {
            return false;
        }

        // Check if has [Component] or [TagComponent] attribute (will implement IComponent after generation)
        const string componentAttr = "KeenEyes.ComponentAttribute";
        const string tagComponentAttr = "KeenEyes.TagComponentAttribute";

        var hasComponentAttribute = typeSymbol.GetAttributes()
            .Any(a => a.AttributeClass?.ToDisplayString() is componentAttr or tagComponentAttr);

        if (hasComponentAttribute)
        {
            return true;
        }

        // Check if implements IComponent interface directly
        var iComponentType = compilation.GetTypeByMetadataName(IComponentInterface);
        if (iComponentType is not null &&
            typeSymbol.AllInterfaces.Any(iface =>
                SymbolEqualityComparer.Default.Equals(iface, iComponentType)))
        {
            return true;
        }

        return false;
    }

    private static string GenerateBundlePartial(BundleInfo info)
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

        // Generate partial struct with IBundle implementation and constructor
        sb.AppendLine($"partial struct {info.Name} : global::KeenEyes.IBundle");
        sb.AppendLine("{");

        // Generate constructor
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{info.Name}\"/> bundle.");
        sb.AppendLine("    /// </summary>");

        foreach (var field in info.Fields)
        {
            var paramName = ToCamelCase(field.Name);
            sb.AppendLine($"    /// <param name=\"{paramName}\">The {field.Name} component.</param>");
        }

        var constructorParams = string.Join(", ", info.Fields.Select(f =>
            $"{f.Type} {ToCamelCase(f.Name)}"));

        sb.AppendLine($"    public {info.Name}({constructorParams})");
        sb.AppendLine("    {");

        foreach (var field in info.Fields)
        {
            var paramName = ToCamelCase(field.Name);
            sb.AppendLine($"        {field.Name} = {paramName};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateEntityBuilderExtensions(ImmutableArray<BundleInfo?> bundles)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated fluent builder methods for bundles.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class EntityBuilderExtensions");
        sb.AppendLine("{");

        foreach (var info in bundles)
        {
            if (info is null)
            {
                continue;
            }

            // Generate parameters from bundle fields
            var parameters = new List<string>();

            foreach (var field in info.Fields)
            {
                var paramName = ToCamelCase(field.Name);
                parameters.Add($"{field.Type} {paramName}");
            }

            var paramList = string.Join(", ", parameters);
            var argList = string.Join(", ", info.Fields.Select(f => ToCamelCase(f.Name)));

            sb.AppendLine($"    /// <summary>Adds a <see cref=\"{info.FullName}\"/> bundle to the entity.</summary>");

            // Generate generic version for fluent chaining
            sb.AppendLine($"    public static TSelf With{info.Name}<TSelf>(this TSelf builder, {paramList})");
            sb.AppendLine($"        where TSelf : global::KeenEyes.IEntityBuilder<TSelf>");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var bundle = new {info.FullName}({argList});");

            // Add each component from the bundle
            foreach (var field in info.Fields)
            {
                sb.AppendLine($"        builder = builder.With(bundle.{field.Name});");
            }

            sb.AppendLine($"        return builder;");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Generate non-generic version for interface usage
            sb.AppendLine($"    /// <summary>Adds a <see cref=\"{info.FullName}\"/> bundle to the entity.</summary>");
            sb.AppendLine($"    public static global::KeenEyes.IEntityBuilder With{info.Name}(this global::KeenEyes.IEntityBuilder builder, {paramList})");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var bundle = new {info.FullName}({argList});");

            foreach (var field in info.Fields)
            {
                sb.AppendLine($"        builder = builder.With(bundle.{field.Name});");
            }

            sb.AppendLine($"        return builder;");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateBundleRefStruct(BundleInfo info)
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

        // Generate ref struct with refs to all components
        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Ref struct providing zero-copy access to all components in <see cref=\"{info.Name}\"/>.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"public ref struct {info.Name}Ref");
        sb.AppendLine("{");

        // Generate ref fields for each component
        foreach (var field in info.Fields)
        {
            sb.AppendLine($"    /// <summary>Reference to the {field.Name} component.</summary>");
            sb.AppendLine($"    public ref {field.Type} {field.Name};");
            sb.AppendLine();
        }

        // Generate constructor
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Initializes a new instance of the <see cref=\"{info.Name}Ref\"/> ref struct.");
        sb.AppendLine("    /// </summary>");
        foreach (var field in info.Fields)
        {
            var paramName = ToCamelCase(field.Name);
            sb.AppendLine($"    /// <param name=\"{paramName}\">Reference to the {field.Name} component.</param>");
        }

        var constructorParams = string.Join(", ", info.Fields.Select(f =>
            $"ref {f.Type} {ToCamelCase(f.Name)}"));

        sb.AppendLine($"    public {info.Name}Ref({constructorParams})");
        sb.AppendLine("    {");

        foreach (var field in info.Fields)
        {
            var paramName = ToCamelCase(field.Name);
            sb.AppendLine($"        {field.Name} = ref {paramName};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateGetBundleExtensions(ImmutableArray<BundleInfo?> bundles)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated GetBundle extension methods for accessing bundle components.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class WorldBundleExtensions");
        sb.AppendLine("{");

        foreach (var info in bundles)
        {
            if (info is null)
            {
                continue;
            }

            // Generate GetBundle extension method as a generic method with type inference
            // Note: The type parameter isn't actually used at runtime, it's for type safety
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Gets a ref struct with references to all components in the bundle.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <param name=\"world\">The world instance.</param>");
            sb.AppendLine($"    /// <param name=\"entity\">The entity to get components from.</param>");
            sb.AppendLine($"    /// <param name=\"bundle\">The bundle type (for type inference, use default).</param>");
            sb.AppendLine($"    /// <returns>A ref struct containing references to all bundle components.</returns>");
            sb.AppendLine($"    /// <exception cref=\"System.InvalidOperationException\">Thrown when the entity is not alive or does not have all required components.</exception>");
            sb.AppendLine($"    /// <example>");
            sb.AppendLine($"    /// <code>");
            sb.AppendLine($"    /// ref var bundle = ref world.GetBundle(entity, default({info.FullName}));");
            sb.AppendLine($"    /// bundle.{info.Fields[0].Name}./* modify component */;");
            sb.AppendLine($"    /// </code>");
            sb.AppendLine($"    /// </example>");
            sb.AppendLine($"    [global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
            sb.AppendLine($"    public static {info.FullName}Ref GetBundle(this global::KeenEyes.World world, global::KeenEyes.Entity entity, {info.FullName} bundle = default)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return new {info.FullName}Ref(");

            var refParams = info.Fields.Select(f => $"            ref world.Get<{f.Type}>(entity)");
            sb.AppendLine(string.Join($",{Environment.NewLine}", refParams));

            sb.AppendLine($"        );");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateQueryBundleExtensions(ImmutableArray<BundleInfo?> bundles)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes;");
        sb.AppendLine();
        sb.AppendLine("public sealed partial class World");
        sb.AppendLine("{");

        // Generate Query<TBundle>() methods for 1-4 bundle parameters
        foreach (var info in bundles)
        {
            if (info is null)
            {
                continue;
            }

            // Generate Query<TBundle>() that expands to Query<C1, C2, ...>()
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Creates a query for entities with all components in <see cref=\"{info.FullName}\"/>.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    /// <typeparam name=\"T\">The bundle type.</typeparam>");
            sb.AppendLine($"    /// <example>");
            sb.AppendLine($"    /// <code>");
            sb.AppendLine($"    /// foreach (var entity in world.Query&lt;{info.Name}&gt;())");
            sb.AppendLine($"    /// {{");
            sb.AppendLine($"    ///     // Process entities with all bundle components");
            sb.AppendLine($"    /// }}");
            sb.AppendLine($"    /// </code>");
            sb.AppendLine($"    /// </example>");

            var componentTypeParams = string.Join(", ", info.Fields.Select(f => f.Type));
            var queryBuilderType = info.Fields.Length switch
            {
                1 => $"QueryBuilder<{componentTypeParams}>",
                2 => $"QueryBuilder<{componentTypeParams}>",
                3 => $"QueryBuilder<{componentTypeParams}>",
                4 => $"QueryBuilder<{componentTypeParams}>",
                _ => null
            };

            if (queryBuilderType is null)
            {
                // Skip bundles with more than 4 components (not supported by current QueryBuilder)
                continue;
            }

            sb.AppendLine($"    public {queryBuilderType} Query<T>() where T : struct, global::KeenEyes.IBundle");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return Query<{componentTypeParams}>();");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        sb.AppendLine("}");
        sb.AppendLine();

        // Generate With<TBundle>() and Without<TBundle>() extension methods for QueryBuilder
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Bundle filter extensions for QueryBuilder.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public static partial class QueryBuilderBundleExtensions");
        sb.AppendLine("{");

        foreach (var info in bundles)
        {
            if (info is null)
            {
                continue;
            }

            // Generate With<TBundle>() for each QueryBuilder arity (1-4)
            for (int arity = 1; arity <= 4; arity++)
            {
                var typeParams = string.Join(", ", Enumerable.Range(1, arity).Select(i => $"T{i}"));
                var whereConstraints = string.Join($"{Environment.NewLine}        ",
                    Enumerable.Range(1, arity).Select(i => $"where T{i} : struct, global::KeenEyes.IComponent"));

                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Requires the entity to have all components in the bundle.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    /// <typeparam name=\"TBundle\">The bundle type.</typeparam>");
                sb.AppendLine($"    public static QueryBuilder<{typeParams}> With{info.Name}<TBundle, {typeParams}>(this QueryBuilder<{typeParams}> builder)");
                sb.AppendLine($"        where TBundle : struct, global::KeenEyes.IBundle");
                sb.AppendLine($"        {whereConstraints}");
                sb.AppendLine($"    {{");

                foreach (var field in info.Fields)
                {
                    sb.AppendLine($"        builder = builder.With<{field.Type}>();");
                }

                sb.AppendLine($"        return builder;");
                sb.AppendLine($"    }}");
                sb.AppendLine();

                sb.AppendLine($"    /// <summary>");
                sb.AppendLine($"    /// Excludes entities that have all components in the bundle.");
                sb.AppendLine($"    /// </summary>");
                sb.AppendLine($"    /// <typeparam name=\"TBundle\">The bundle type.</typeparam>");
                sb.AppendLine($"    public static QueryBuilder<{typeParams}> Without{info.Name}<TBundle, {typeParams}>(this QueryBuilder<{typeParams}> builder)");
                sb.AppendLine($"        where TBundle : struct, global::KeenEyes.IBundle");
                sb.AppendLine($"        {whereConstraints}");
                sb.AppendLine($"    {{");

                foreach (var field in info.Fields)
                {
                    sb.AppendLine($"        builder = builder.Without<{field.Type}>();");
                }

                sb.AppendLine($"        return builder;");
                sb.AppendLine($"    }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        if (name.Length == 1)
        {
            return name.ToLowerInvariant();
        }

        return char.ToLowerInvariant(name[0]) + name.Substring(1);
    }

    private sealed record BundleInfo(
        string Name,
        string Namespace,
        string FullName,
        ImmutableArray<ComponentFieldInfo> Fields,
        ImmutableArray<DiagnosticInfo> Diagnostics,
        bool IsValid);

    private sealed record ComponentFieldInfo(
        string Name,
        string Type);

    private sealed record DiagnosticInfo(
        DiagnosticDescriptor Descriptor,
        Location Location,
        object[] MessageArgs);
}

/// <summary>
/// Diagnostic descriptors for bundle generation errors.
/// </summary>
internal static class Diagnostics
{
    /// <summary>
    /// KEEN020: Bundle must be a struct.
    /// </summary>
    public static readonly DiagnosticDescriptor BundleMustBeStruct = new(
        id: "KEEN020",
        title: "Bundle must be a struct",
        messageFormat: "Bundle '{0}' must be a struct, not a class or other type",
        category: "KeenEyes.Bundle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Bundles must be value types (structs) to maintain consistency with component semantics.");

    /// <summary>
    /// KEEN021: Bundle field must be a component type.
    /// </summary>
    public static readonly DiagnosticDescriptor BundleFieldMustBeComponent = new(
        id: "KEEN021",
        title: "Bundle field must be a component type",
        messageFormat: "Field '{0}' in bundle '{1}' must be a component type (struct implementing IComponent), but is '{2}'",
        category: "KeenEyes.Bundle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All fields in a bundle must be valid component types. Components must be structs implementing IComponent.");

    /// <summary>
    /// KEEN022: Bundle must have at least one field.
    /// </summary>
    public static readonly DiagnosticDescriptor BundleMustHaveFields = new(
        id: "KEEN022",
        title: "Bundle must have at least one component field",
        messageFormat: "Bundle '{0}' must contain at least one component field",
        category: "KeenEyes.Bundle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A bundle without fields serves no purpose. Define at least one component field in the bundle.");

    /// <summary>
    /// KEEN023: Circular reference in bundle.
    /// </summary>
    public static readonly DiagnosticDescriptor BundleCircularReference = new(
        id: "KEEN023",
        title: "Circular reference in bundle",
        messageFormat: "Bundle '{0}' cannot contain a field '{1}' of its own type (circular reference)",
        category: "KeenEyes.Bundle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A bundle cannot contain itself as a field. This would create an infinite recursion.");
}
