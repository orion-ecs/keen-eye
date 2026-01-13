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
/// Generates AOT-compatible serialization code for components marked with [Component(Serializable = true)].
/// Eliminates runtime reflection by generating strongly-typed serialization methods.
/// Also generates migration code for components with [MigrateFrom] methods.
/// </summary>
[Generator]
public sealed class SerializationGenerator : IIncrementalGenerator
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";
    private const string MigrateFromAttribute = "KeenEyes.MigrateFromAttribute";
    private const string DefaultValueAttribute = "KeenEyes.DefaultValueAttribute";

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [Component(Serializable = true)]
        var serializableComponents = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ComponentAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetSerializableComponentInfo(ctx))
            .Where(static info => info is not null);

        var collected = serializableComponents.Collect();

        // Generate the serialization registry only if there are serializable components
        context.RegisterSourceOutput(collected, static (ctx, components) =>
        {
            var validComponents = components
                .Where(c => c is not null)
                .Select(c => c!)
                .ToImmutableArray();

            // Don't generate anything if there are no serializable components
            // This avoids conflicts when multiple projects reference KeenEyes.Core
            if (validComponents.Length == 0)
            {
                return;
            }

            var source = GenerateSerializationRegistry(validComponents);
            ctx.AddSource("ComponentSerializer.g.cs", SourceText.From(source, Encoding.UTF8));
        });
    }

    private static SerializableComponentInfo? GetSerializableComponentInfo(GeneratorAttributeSyntaxContext context)
    {
        // TargetSymbol is guaranteed to be INamedTypeSymbol because we filter for StructDeclarationSyntax
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;

        // Check for Serializable = true in attribute
        // Attributes is guaranteed non-empty because we use ForAttributeWithMetadataName
        var attr = context.Attributes.First();

        var serializableArg = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "Serializable");

        if (serializableArg.Value.Value is not true)
        {
            return null;
        }

        // Extract Version property (defaults to 1)
        var version = 1;
        var versionArg = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "Version");

        if (versionArg.Value.Value is int v)
        {
            version = v;
        }

        // Skip components with invalid versions
        if (version < 1)
        {
            return null;
        }

        // Collect fields for serialization
        var fields = new List<SerializableFieldInfo>();
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

            var isNullable = IsNullableType(field.Type);

            // Extract [DefaultValue] attribute if present
            string? defaultValueLiteral = null;
            var defaultValueAttr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == DefaultValueAttribute);

            if (defaultValueAttr is not null &&
                defaultValueAttr.ConstructorArguments.Length > 0)
            {
                var value = defaultValueAttr.ConstructorArguments[0].Value;
                defaultValueLiteral = GetCSharpLiteral(value, field.Type);
            }

            fields.Add(new SerializableFieldInfo(
                field.Name,
                field.Type.ToDisplayString(),
                GetJsonTypeName(field.Type),
                isNullable,
                defaultValueLiteral));
        }

        // Collect migration methods
        var migrations = new List<MigrationMethodInfo>();
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }

            foreach (var migrateAttr in method.GetAttributes())
            {
                if (migrateAttr.AttributeClass?.ToDisplayString() != MigrateFromAttribute)
                {
                    continue;
                }

                // Validate migration method signature and extract version
                if (migrateAttr.ConstructorArguments.Length > 0 &&
                    migrateAttr.ConstructorArguments[0].Value is int fromVersion &&
                    method.IsStatic &&
                    SymbolEqualityComparer.Default.Equals(method.ReturnType, typeSymbol) &&
                    method.Parameters.Length == 1 &&
                    method.Parameters[0].Type.ToDisplayString() == "System.Text.Json.JsonElement")
                {
                    migrations.Add(new MigrationMethodInfo(fromVersion, method.Name));
                }
            }
        }

        return new SerializableComponentInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            fields.ToImmutableArray(),
            version,
            migrations.ToImmutableArray());
    }

    private static string GetJsonTypeName(ITypeSymbol type)
    {
        // Map CLR types to JSON reader method names
        var displayString = type.ToDisplayString();
        return displayString switch
        {
            "int" or "System.Int32" => "Int32",
            "long" or "System.Int64" => "Int64",
            "short" or "System.Int16" => "Int16",
            "byte" or "System.Byte" => "Byte",
            "uint" or "System.UInt32" => "UInt32",
            "ulong" or "System.UInt64" => "UInt64",
            "ushort" or "System.UInt16" => "UInt16",
            "sbyte" or "System.SByte" => "SByte",
            "float" or "System.Single" => "Single",
            "double" or "System.Double" => "Double",
            "decimal" or "System.Decimal" => "Decimal",
            "bool" or "System.Boolean" => "Boolean",
            "string" or "System.String" => "String",
            _ => "Object" // For complex types, use generic deserialization
        };
    }

    /// <summary>
    /// Checks if a type is nullable (either nullable reference type or Nullable&lt;T&gt; value type).
    /// </summary>
    private static bool IsNullableType(ITypeSymbol type)
    {
        // Check for nullable reference types (string?)
        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        // Check for Nullable<T> value types (int?)
        if (type is INamedTypeSymbol namedType &&
            namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }

    private static string? GetCSharpLiteral(object? value, ITypeSymbol targetType)
    {
        if (value is null)
        {
            return "null";
        }

        var targetTypeName = targetType.ToDisplayString();

        return value switch
        {
            // Numeric types
            int i => i.ToString(),
            long l => $"{l}L",
            short s => $"(short){s}",
            byte b => $"(byte){b}",
            uint ui => $"{ui}U",
            ulong ul => $"{ul}UL",
            ushort us => $"(ushort){us}",
            sbyte sb => $"(sbyte){sb}",
            float f => FormatFloat(f),
            double d => FormatDouble(d),
            decimal m => $"{m}m",

            // Boolean
            bool bo => bo ? "true" : "false",

            // String
            string str => $"\"{EscapeString(str)}\"",

            // Char
            char c => $"'{EscapeChar(c)}'",

            // Handle boxed enums - the value comes as the underlying type
            _ when targetType.TypeKind == TypeKind.Enum =>
                $"({targetTypeName}){value}",

            _ => null
        };
    }

    private static string FormatFloat(float f)
    {
        if (float.IsNaN(f))
        {
            return "float.NaN";
        }
        if (float.IsPositiveInfinity(f))
        {
            return "float.PositiveInfinity";
        }
        if (float.IsNegativeInfinity(f))
        {
            return "float.NegativeInfinity";
        }
        // Use 'G9' format for round-trip precision, append 'f' suffix
        return $"{f:G9}f";
    }

    private static string FormatDouble(double d)
    {
        if (double.IsNaN(d))
        {
            return "double.NaN";
        }
        if (double.IsPositiveInfinity(d))
        {
            return "double.PositiveInfinity";
        }
        if (double.IsNegativeInfinity(d))
        {
            return "double.NegativeInfinity";
        }
        // Use 'G17' format for round-trip precision
        var result = d.ToString("G17");
        // Ensure there's a decimal point or 'd' suffix for disambiguation
        if (!result.Contains('.') && !result.Contains('E'))
        {
            result += ".0";
        }
        return result;
    }

    private static string EscapeString(string s)
    {
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    private static string EscapeChar(char c)
    {
        return c switch
        {
            '\\' => "\\\\",
            '\'' => "\\'",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            _ => c.ToString()
        };
    }

    private static string GenerateSerializationRegistry(ImmutableArray<SerializableComponentInfo> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using KeenEyes.Capabilities;");
        sb.AppendLine("using KeenEyes.Serialization;");
        sb.AppendLine();
        sb.AppendLine("namespace KeenEyes.Generated;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Generated registry for AOT-compatible component serialization.");
        sb.AppendLine("/// Contains serialization methods for components marked with [Component(Serializable = true)].");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("/// <remarks>");
        sb.AppendLine("/// <para>");
        sb.AppendLine("/// <strong>Performance Note:</strong> When using custom <see cref=\"JsonSerializerOptions\"/>,");
        sb.AppendLine("/// cache the options instance rather than creating new ones per call. Creating options is");
        sb.AppendLine("/// expensive as it involves reflection and internal caching on first use.");
        sb.AppendLine("/// </para>");
        sb.AppendLine("/// <code>");
        sb.AppendLine("/// // ❌ BAD: Creating options per call");
        sb.AppendLine("/// JsonSerializer.Serialize(value, new JsonSerializerOptions { ... });");
        sb.AppendLine("///");
        sb.AppendLine("/// // ✅ GOOD: Cached options");
        sb.AppendLine("/// private static readonly JsonSerializerOptions Options = new() { ... };");
        sb.AppendLine("/// JsonSerializer.Serialize(value, Options);");
        sb.AppendLine("/// </code>");
        sb.AppendLine("/// </remarks>");
        sb.AppendLine("public sealed class ComponentSerializer : IComponentSerializer, IBinaryComponentSerializer, IComponentMigrator, IMigrationDiagnostics");
        sb.AppendLine("{");
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Shared instance for convenience.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public static readonly ComponentSerializer Instance = new();");
        sb.AppendLine();
        sb.AppendLine("    private sealed record ComponentSerializationInfo(");
        sb.AppendLine("        Type Type,");
        sb.AppendLine("        Func<JsonElement, object> JsonDeserializer,");
        sb.AppendLine("        Func<object, JsonElement> JsonSerializer,");
        sb.AppendLine("        Func<BinaryReader, object> BinaryDeserializer,");
        sb.AppendLine("        Action<object, BinaryWriter> BinarySerializer,");
        sb.AppendLine("        Func<ISerializationCapability, bool, ComponentInfo> Registrar,");
        sb.AppendLine("        Action<ISerializationCapability, object> SingletonSetter,");
        sb.AppendLine("        Func<object> Factory,");
        sb.AppendLine("        int Version);");
        sb.AppendLine();
        sb.AppendLine("    private static readonly Dictionary<string, ComponentSerializationInfo> ComponentsByName;");
        sb.AppendLine("    private static readonly Dictionary<Type, ComponentSerializationInfo> ComponentsByType;");
        sb.AppendLine();
        sb.AppendLine("    // Migration lookup: Type -> (FromVersion -> MigrationDelegate)");
        sb.AppendLine("    private static readonly Dictionary<Type, Dictionary<int, Func<JsonElement, object>>> MigrationsByType;");
        sb.AppendLine("    private static readonly Dictionary<string, Dictionary<int, Func<JsonElement, object>>> MigrationsByName;");
        sb.AppendLine();
        sb.AppendLine("    // Cached migration graphs for diagnostics");
        sb.AppendLine("    private static readonly Dictionary<string, MigrationGraph> MigrationGraphsByName;");
        sb.AppendLine("    private static readonly Dictionary<Type, MigrationGraph> MigrationGraphsByType;");
        sb.AppendLine();

        // Static constructor to initialize dictionaries
        sb.AppendLine("    static ComponentSerializer()");
        sb.AppendLine("    {");
        sb.AppendLine("        ComponentsByName = new Dictionary<string, ComponentSerializationInfo>();");
        sb.AppendLine("        ComponentsByType = new Dictionary<Type, ComponentSerializationInfo>();");
        sb.AppendLine("        MigrationsByType = new Dictionary<Type, Dictionary<int, Func<JsonElement, object>>>();");
        sb.AppendLine("        MigrationsByName = new Dictionary<string, Dictionary<int, Func<JsonElement, object>>>();");
        sb.AppendLine("        MigrationGraphsByName = new Dictionary<string, MigrationGraph>();");
        sb.AppendLine("        MigrationGraphsByType = new Dictionary<Type, MigrationGraph>();");
        sb.AppendLine();

        foreach (var component in components)
        {
            sb.AppendLine($"        var info_{component.Name} = new ComponentSerializationInfo(");
            sb.AppendLine($"            typeof({component.FullName}),");
            sb.AppendLine($"            Deserialize_{component.Name},");
            sb.AppendLine($"            value => Serialize_{component.Name}(({component.FullName})value),");
            sb.AppendLine($"            DeserializeBinary_{component.Name},");
            sb.AppendLine($"            (value, writer) => SerializeBinary_{component.Name}(({component.FullName})value, writer),");
            sb.AppendLine($"            (serialization, isTag) => (ComponentInfo)serialization.Components.Register<{component.FullName}>(isTag),");
            sb.AppendLine($"            (serialization, value) => serialization.SetSingleton(({component.FullName})value),");
            sb.AppendLine($"            static () => new {component.FullName}(),");
            sb.AppendLine($"            {component.Version});");
            sb.AppendLine();
            sb.AppendLine($"        ComponentsByName[typeof({component.FullName}).AssemblyQualifiedName!] = info_{component.Name};");
            sb.AppendLine($"        ComponentsByName[\"{component.FullName}\"] = info_{component.Name};");
            sb.AppendLine($"        ComponentsByType[typeof({component.FullName})] = info_{component.Name};");
            sb.AppendLine();

            // Register migrations for this component (explicit + auto-generated)
            var hasFieldsWithDefaults = component.Fields.Any(f => f.HasDefaultValue);
            var explicitVersions = new HashSet<int>(component.Migrations.Select(m => m.FromVersion));

            // Determine if we need to generate any migrations (explicit or auto)
            var needsMigrations = component.Migrations.Length > 0 ||
                                  (component.Version > 1 && hasFieldsWithDefaults);

            if (needsMigrations)
            {
                sb.AppendLine($"        var migrations_{component.Name} = new Dictionary<int, Func<JsonElement, object>>");
                sb.AppendLine("        {");

                // Add explicit migrations
                foreach (var migration in component.Migrations.OrderBy(m => m.FromVersion))
                {
                    sb.AppendLine($"            [{migration.FromVersion}] = json => {component.FullName}.{migration.MethodName}(json),");
                }

                // Add auto-generated migrations for missing versions (only if we have default values)
                if (hasFieldsWithDefaults)
                {
                    for (var v = 1; v < component.Version; v++)
                    {
                        if (!explicitVersions.Contains(v))
                        {
                            sb.AppendLine($"            [{v}] = AutoMigrate_{component.Name},");
                        }
                    }
                }

                sb.AppendLine("        };");
                sb.AppendLine($"        MigrationsByType[typeof({component.FullName})] = migrations_{component.Name};");
                sb.AppendLine($"        MigrationsByName[typeof({component.FullName}).AssemblyQualifiedName!] = migrations_{component.Name};");
                sb.AppendLine($"        MigrationsByName[\"{component.FullName}\"] = migrations_{component.Name};");
                sb.AppendLine();

                // Create migration graph for diagnostics
                sb.AppendLine($"        var graph_{component.Name} = new MigrationGraph(\"{component.FullName}\", {component.Version});");
                foreach (var migration in component.Migrations.OrderBy(m => m.FromVersion))
                {
                    sb.AppendLine($"        graph_{component.Name}.AddEdge({migration.FromVersion}, {migration.FromVersion + 1});");
                }
                sb.AppendLine($"        MigrationGraphsByType[typeof({component.FullName})] = graph_{component.Name};");
                sb.AppendLine($"        MigrationGraphsByName[typeof({component.FullName}).AssemblyQualifiedName!] = graph_{component.Name};");
                sb.AppendLine($"        MigrationGraphsByName[\"{component.FullName}\"] = graph_{component.Name};");
                sb.AppendLine();
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Public API methods implementing IComponentSerializer
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool IsSerializable(Type type) => ComponentsByType.ContainsKey(type);");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool IsSerializable(string typeName) => ComponentsByName.ContainsKey(typeName);");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? Deserialize(string typeName, JsonElement json)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info)");
        sb.AppendLine("            ? info.JsonDeserializer(json)");
        sb.AppendLine("            : null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public JsonElement? Serialize(Type type, object value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!ComponentsByType.TryGetValue(type, out var info))");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine("        return info.JsonSerializer(value);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    Type? IComponentSerializer.GetType(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info) ? info.Type : null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info) ? info.Registrar(serialization, isTag) : null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (ComponentsByName.TryGetValue(typeName, out var info))");
        sb.AppendLine("        {");
        sb.AppendLine("            info.SingletonSetter(serialization, value);");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        return false;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? CreateDefault(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info) ? info.Factory() : null;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all registered serializable type names.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public IEnumerable<string> GetSerializableTypeNames() => ComponentsByName.Keys;");
        sb.AppendLine();
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets all registered serializable types.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public IEnumerable<Type> GetSerializableTypes() => ComponentsByType.Keys;");
        sb.AppendLine();

        // IBinaryComponentSerializer implementation
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool WriteTo(Type type, object value, BinaryWriter writer)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!ComponentsByType.TryGetValue(type, out var info))");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("        info.BinarySerializer(value, writer);");
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? ReadFrom(string typeName, BinaryReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info)");
        sb.AppendLine("            ? info.BinaryDeserializer(reader)");
        sb.AppendLine("            : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetVersion methods for schema versioning
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public int GetVersion(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByName.TryGetValue(typeName, out var info) ? info.Version : 1;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public int GetVersion(Type type)");
        sb.AppendLine("    {");
        sb.AppendLine("        return ComponentsByType.TryGetValue(type, out var info) ? info.Version : 1;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // IComponentMigrator implementation
        GenerateMigratorMethods(sb);

        // IMigrationDiagnostics implementation
        GenerateDiagnosticMethods(sb);

        // Generate individual serialization/deserialization methods
        foreach (var component in components)
        {
            GenerateComponentMethods(sb, component);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateComponentMethods(StringBuilder sb, SerializableComponentInfo component)
    {
        // Deserialize method
        sb.AppendLine($"    private static object Deserialize_{component.Name}(JsonElement json)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = new {component.FullName}();");

        foreach (var field in component.Fields)
        {
            var camelFieldName = StringHelpers.ToCamelCase(field.Name);
            sb.AppendLine($"        if (json.TryGetProperty(\"{camelFieldName}\", out var {camelFieldName}Elem))");
            sb.AppendLine("        {");

            if (field.JsonTypeName == "Object")
            {
                // Complex type - use generic deserialization
                if (field.IsNullable)
                {
                    sb.AppendLine($"            result.{field.Name} = JsonSerializer.Deserialize<{field.Type}>({camelFieldName}Elem.GetRawText());");
                }
                else
                {
                    sb.AppendLine($"            result.{field.Name} = JsonSerializer.Deserialize<{field.Type}>({camelFieldName}Elem.GetRawText()) ?? throw new JsonException(\"Non-nullable field '{field.Name}' was null in JSON\");");
                }
            }
            else if (field.JsonTypeName == "String")
            {
                if (field.IsNullable)
                {
                    sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.GetString();");
                }
                else
                {
                    sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.GetString() ?? string.Empty;");
                }
            }
            else
            {
                // Value types like int, bool, etc. - GetInt32(), GetBoolean(), etc. never return null
                sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.Get{field.JsonTypeName}();");
            }

            sb.AppendLine("        }");
        }

        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Serialize method
        sb.AppendLine($"    private static JsonElement Serialize_{component.Name}({component.FullName} value)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var stream = new System.IO.MemoryStream();");
        sb.AppendLine("        using var writer = new Utf8JsonWriter(stream);");
        sb.AppendLine("        writer.WriteStartObject();");

        foreach (var field in component.Fields)
        {
            var camelFieldName = StringHelpers.ToCamelCase(field.Name);

            if (field.JsonTypeName == "Object")
            {
                sb.AppendLine($"        writer.WritePropertyName(\"{camelFieldName}\");");
                sb.AppendLine($"        JsonSerializer.Serialize(writer, value.{field.Name});");
            }
            else if (field.JsonTypeName == "String")
            {
                sb.AppendLine($"        writer.WriteString(\"{camelFieldName}\", value.{field.Name});");
            }
            else if (field.JsonTypeName == "Boolean")
            {
                sb.AppendLine($"        writer.WriteBoolean(\"{camelFieldName}\", value.{field.Name});");
            }
            else
            {
                sb.AppendLine($"        writer.WriteNumber(\"{camelFieldName}\", value.{field.Name});");
            }
        }

        sb.AppendLine("        writer.WriteEndObject();");
        sb.AppendLine("        writer.Flush();");
        sb.AppendLine("        stream.Position = 0;");
        sb.AppendLine("        return JsonDocument.Parse(stream).RootElement.Clone();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Binary deserialize method
        sb.AppendLine($"    private static object DeserializeBinary_{component.Name}(BinaryReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = new {component.FullName}();");

        foreach (var field in component.Fields)
        {
            var binaryRead = GetBinaryReadMethod(field.JsonTypeName, field.Type, field.IsNullable);
            sb.AppendLine($"        result.{field.Name} = {binaryRead};");
        }

        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Binary serialize method
        sb.AppendLine($"    private static void SerializeBinary_{component.Name}({component.FullName} value, BinaryWriter writer)");
        sb.AppendLine("    {");

        foreach (var field in component.Fields)
        {
            var binaryWrite = GetBinaryWriteCode(field.JsonTypeName, $"value.{field.Name}");
            sb.AppendLine($"        {binaryWrite}");
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate auto-migration method if any field has [DefaultValue]
        var fieldsWithDefaults = component.Fields.Where(f => f.HasDefaultValue).ToList();
        if (fieldsWithDefaults.Count > 0 && component.Version > 1)
        {
            GenerateAutoMigrationMethod(sb, component);
        }
    }

    private static void GenerateAutoMigrationMethod(
        StringBuilder sb,
        SerializableComponentInfo component)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine($"    /// Auto-generated migration for {component.Name}.");
        sb.AppendLine("    /// Reads existing fields from JSON and applies default values for new fields.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    private static object AutoMigrate_{component.Name}(JsonElement json)");
        sb.AppendLine("    {");
        sb.AppendLine($"        var result = new {component.FullName}();");

        foreach (var field in component.Fields)
        {
            var camelFieldName = StringHelpers.ToCamelCase(field.Name);

            // Check if JSON has this property
            sb.AppendLine($"        if (json.TryGetProperty(\"{camelFieldName}\", out var {camelFieldName}Elem))");
            sb.AppendLine("        {");

            // Generate the field reading code (same as deserialize)
            if (field.JsonTypeName == "Object")
            {
                if (field.IsNullable)
                {
                    sb.AppendLine($"            result.{field.Name} = JsonSerializer.Deserialize<{field.Type}>({camelFieldName}Elem.GetRawText());");
                }
                else
                {
                    sb.AppendLine($"            result.{field.Name} = JsonSerializer.Deserialize<{field.Type}>({camelFieldName}Elem.GetRawText()) ?? throw new JsonException(\"Non-nullable field '{field.Name}' was null in JSON\");");
                }
            }
            else if (field.JsonTypeName == "String")
            {
                if (field.IsNullable)
                {
                    sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.GetString();");
                }
                else
                {
                    sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.GetString() ?? string.Empty;");
                }
            }
            else
            {
                sb.AppendLine($"            result.{field.Name} = {camelFieldName}Elem.Get{field.JsonTypeName}();");
            }

            sb.AppendLine("        }");

            // If field has a default value, apply it when not present in JSON
            if (field.HasDefaultValue)
            {
                sb.AppendLine("        else");
                sb.AppendLine("        {");
                sb.AppendLine($"            result.{field.Name} = {field.DefaultValueLiteral};");
                sb.AppendLine("        }");
            }
        }

        sb.AppendLine("        return result;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private static void GenerateMigratorMethods(StringBuilder sb)
    {
        // CanMigrate(string, int, int)
        sb.AppendLine("    #region IComponentMigrator Implementation");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool CanMigrate(string typeName, int fromVersion, int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (fromVersion >= toVersion)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!MigrationsByName.TryGetValue(typeName, out var migrations))");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Check all versions in the chain have migrations");
        sb.AppendLine("        for (var v = fromVersion; v < toVersion; v++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!migrations.ContainsKey(v))");
        sb.AppendLine("            {");
        sb.AppendLine("                return false;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // CanMigrate(Type, int, int)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool CanMigrate(Type type, int fromVersion, int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (fromVersion >= toVersion)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!MigrationsByType.TryGetValue(type, out var migrations))");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Check all versions in the chain have migrations");
        sb.AppendLine("        for (var v = fromVersion; v < toVersion; v++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!migrations.ContainsKey(v))");
        sb.AppendLine("            {");
        sb.AppendLine("                return false;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return true;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Migrate(string, JsonElement, int, int)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public JsonElement? Migrate(string typeName, JsonElement data, int fromVersion, int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (fromVersion >= toVersion)");
        sb.AppendLine("        {");
        sb.AppendLine("            return data; // No migration needed");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!MigrationsByName.TryGetValue(typeName, out var migrations))");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!ComponentsByName.TryGetValue(typeName, out var info))");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var currentData = data;");
        sb.AppendLine("        object? currentValue = null;");
        sb.AppendLine();
        sb.AppendLine("        // Chain migrations from fromVersion to toVersion");
        sb.AppendLine("        for (var v = fromVersion; v < toVersion; v++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!migrations.TryGetValue(v, out var migrate))");
        sb.AppendLine("            {");
        sb.AppendLine("                throw new ComponentVersionException(");
        sb.AppendLine("                    typeName.Split('.').Last(),");
        sb.AppendLine("                    v,");
        sb.AppendLine("                    toVersion,");
        sb.AppendLine("                    $\"No migration defined from version {v} to {v + 1}\");");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Apply migration");
        sb.AppendLine("            currentValue = migrate(currentData);");
        sb.AppendLine();
        sb.AppendLine("            // If not at final version, serialize back to JSON for next migration");
        sb.AppendLine("            if (v < toVersion - 1)");
        sb.AppendLine("            {");
        sb.AppendLine("                var jsonElement = info.JsonSerializer(currentValue);");
        sb.AppendLine("                currentData = jsonElement;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Serialize final result to JsonElement");
        sb.AppendLine("        if (currentValue is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            return info.JsonSerializer(currentValue);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Migrate(Type, JsonElement, int, int)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public JsonElement? Migrate(Type type, JsonElement data, int fromVersion, int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;");
        sb.AppendLine("        return Migrate(typeName, data, fromVersion, toVersion);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetMigrationVersions(string)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IEnumerable<int> GetMigrationVersions(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return MigrationsByName.TryGetValue(typeName, out var migrations)");
        sb.AppendLine("            ? migrations.Keys.OrderBy(v => v)");
        sb.AppendLine("            : Enumerable.Empty<int>();");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetMigrationVersions(Type)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IEnumerable<int> GetMigrationVersions(Type type)");
        sb.AppendLine("    {");
        sb.AppendLine("        return MigrationsByType.TryGetValue(type, out var migrations)");
        sb.AppendLine("            ? migrations.Keys.OrderBy(v => v)");
        sb.AppendLine("            : Enumerable.Empty<int>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    #endregion");
        sb.AppendLine();
    }

    private static void GenerateDiagnosticMethods(StringBuilder sb)
    {
        sb.AppendLine("    #region IMigrationDiagnostics Implementation");
        sb.AppendLine();

        // MigrateWithDiagnostics(string, ...)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public MigrationResult MigrateWithDiagnostics(");
        sb.AppendLine("        string typeName,");
        sb.AppendLine("        JsonElement data,");
        sb.AppendLine("        int fromVersion,");
        sb.AppendLine("        int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();");
        sb.AppendLine();
        sb.AppendLine("        if (fromVersion >= toVersion)");
        sb.AppendLine("        {");
        sb.AppendLine("            totalStopwatch.Stop();");
        sb.AppendLine("            return MigrationResult.NoMigrationNeeded(data, typeName, fromVersion);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!MigrationsByName.TryGetValue(typeName, out var migrations))");
        sb.AppendLine("        {");
        sb.AppendLine("            totalStopwatch.Stop();");
        sb.AppendLine("            return MigrationResult.Failed(");
        sb.AppendLine("                typeName, fromVersion, toVersion, totalStopwatch.Elapsed,");
        sb.AppendLine("                $\"No migrations registered for type '{typeName}'\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        if (!ComponentsByName.TryGetValue(typeName, out var info))");
        sb.AppendLine("        {");
        sb.AppendLine("            totalStopwatch.Stop();");
        sb.AppendLine("            return MigrationResult.Failed(");
        sb.AppendLine("                typeName, fromVersion, toVersion, totalStopwatch.Elapsed,");
        sb.AppendLine("                $\"Component type '{typeName}' not found\");");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var stepTimings = new List<MigrationStepTiming>();");
        sb.AppendLine("        var currentData = data;");
        sb.AppendLine("        object? currentValue = null;");
        sb.AppendLine();
        sb.AppendLine("        // Chain migrations from fromVersion to toVersion");
        sb.AppendLine("        for (var v = fromVersion; v < toVersion; v++)");
        sb.AppendLine("        {");
        sb.AppendLine("            if (!migrations.TryGetValue(v, out var migrate))");
        sb.AppendLine("            {");
        sb.AppendLine("                totalStopwatch.Stop();");
        sb.AppendLine("                return MigrationResult.Failed(");
        sb.AppendLine("                    typeName, fromVersion, toVersion, totalStopwatch.Elapsed,");
        sb.AppendLine("                    $\"No migration defined from version {v} to {v + 1}\",");
        sb.AppendLine("                    failedAtVersion: v,");
        sb.AppendLine("                    stepTimings: stepTimings);");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            var stepStopwatch = System.Diagnostics.Stopwatch.StartNew();");
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                // Apply migration");
        sb.AppendLine("                currentValue = migrate(currentData);");
        sb.AppendLine();
        sb.AppendLine("                // If not at final version, serialize back to JSON for next migration");
        sb.AppendLine("                if (v < toVersion - 1)");
        sb.AppendLine("                {");
        sb.AppendLine("                    currentData = info.JsonSerializer(currentValue);");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                stepStopwatch.Stop();");
        sb.AppendLine("                stepTimings.Add(new MigrationStepTiming(new MigrationStep(v, v + 1), stepStopwatch.Elapsed));");
        sb.AppendLine("            }");
        sb.AppendLine("            catch (Exception ex)");
        sb.AppendLine("            {");
        sb.AppendLine("                stepStopwatch.Stop();");
        sb.AppendLine("                stepTimings.Add(new MigrationStepTiming(new MigrationStep(v, v + 1), stepStopwatch.Elapsed));");
        sb.AppendLine("                totalStopwatch.Stop();");
        sb.AppendLine("                return MigrationResult.Failed(");
        sb.AppendLine("                    typeName, fromVersion, toVersion, totalStopwatch.Elapsed,");
        sb.AppendLine("                    $\"Migration from v{v} to v{v + 1} failed: {ex.Message}\",");
        sb.AppendLine("                    failedAtVersion: v,");
        sb.AppendLine("                    stepTimings: stepTimings);");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        // Serialize final result to JsonElement");
        sb.AppendLine("        if (currentValue is not null)");
        sb.AppendLine("        {");
        sb.AppendLine("            var finalData = info.JsonSerializer(currentValue);");
        sb.AppendLine("            totalStopwatch.Stop();");
        sb.AppendLine("            return MigrationResult.Succeeded(");
        sb.AppendLine("                finalData, typeName, fromVersion, toVersion,");
        sb.AppendLine("                totalStopwatch.Elapsed, stepTimings);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        totalStopwatch.Stop();");
        sb.AppendLine("        return MigrationResult.Failed(");
        sb.AppendLine("            typeName, fromVersion, toVersion, totalStopwatch.Elapsed,");
        sb.AppendLine("            \"Migration produced null result\",");
        sb.AppendLine("            stepTimings: stepTimings);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // MigrateWithDiagnostics(Type, ...)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public MigrationResult MigrateWithDiagnostics(");
        sb.AppendLine("        Type type,");
        sb.AppendLine("        JsonElement data,");
        sb.AppendLine("        int fromVersion,");
        sb.AppendLine("        int toVersion)");
        sb.AppendLine("    {");
        sb.AppendLine("        var typeName = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;");
        sb.AppendLine("        return MigrateWithDiagnostics(typeName, data, fromVersion, toVersion);");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetMigrationGraph(string)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public MigrationGraph? GetMigrationGraph(string typeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        return MigrationGraphsByName.TryGetValue(typeName, out var graph) ? graph : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetMigrationGraph(Type)
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public MigrationGraph? GetMigrationGraph(Type type)");
        sb.AppendLine("    {");
        sb.AppendLine("        return MigrationGraphsByType.TryGetValue(type, out var graph) ? graph : null;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetComponentsWithMigrations
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IEnumerable<string> GetComponentsWithMigrations()");
        sb.AppendLine("    {");
        sb.AppendLine("        return MigrationGraphsByName.Keys;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // FindAllMigrationGaps
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IReadOnlyDictionary<string, IReadOnlyList<int>> FindAllMigrationGaps()");
        sb.AppendLine("    {");
        sb.AppendLine("        var gaps = new Dictionary<string, IReadOnlyList<int>>();");
        sb.AppendLine("        foreach (var (typeName, graph) in MigrationGraphsByName)");
        sb.AppendLine("        {");
        sb.AppendLine("            var componentGaps = graph.FindGaps();");
        sb.AppendLine("            if (componentGaps.Count > 0)");
        sb.AppendLine("            {");
        sb.AppendLine("                gaps[typeName] = componentGaps;");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        return gaps;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GenerateDiagnosticReport
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public string GenerateDiagnosticReport()");
        sb.AppendLine("    {");
        sb.AppendLine("        var sb = new System.Text.StringBuilder();");
        sb.AppendLine("        sb.AppendLine(\"=== Migration Diagnostic Report ===\");");
        sb.AppendLine("        sb.AppendLine();");
        sb.AppendLine();
        sb.AppendLine("        if (MigrationGraphsByName.Count == 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            sb.AppendLine(\"No components with migrations registered.\");");
        sb.AppendLine("            return sb.ToString();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        sb.AppendLine($\"Components with migrations: {MigrationGraphsByName.Count}\");");
        sb.AppendLine("        sb.AppendLine();");
        sb.AppendLine();
        sb.AppendLine("        foreach (var (typeName, graph) in MigrationGraphsByName.OrderBy(kvp => kvp.Key))");
        sb.AppendLine("        {");
        sb.AppendLine("            sb.AppendLine(graph.ToDiagnosticString());");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        var allGaps = FindAllMigrationGaps();");
        sb.AppendLine("        if (allGaps.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine("            sb.AppendLine(\"=== WARNINGS ===\");");
        sb.AppendLine("            sb.AppendLine($\"Found {allGaps.Count} component(s) with migration gaps:\");");
        sb.AppendLine("            foreach (var (typeName, gaps) in allGaps)");
        sb.AppendLine("            {");
        sb.AppendLine("                sb.AppendLine($\"  - {typeName}: missing migrations from versions {string.Join(\", \", gaps)}\");");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        return sb.ToString();");
        sb.AppendLine("    }");
        sb.AppendLine();

        sb.AppendLine("    #endregion");
        sb.AppendLine();
    }

    private static string GetBinaryReadMethod(string jsonTypeName, string type, bool isNullable)
    {
        return jsonTypeName switch
        {
            "Int32" => "reader.ReadInt32()",
            "Int64" => "reader.ReadInt64()",
            "Int16" => "reader.ReadInt16()",
            "Byte" => "reader.ReadByte()",
            "UInt32" => "reader.ReadUInt32()",
            "UInt64" => "reader.ReadUInt64()",
            "UInt16" => "reader.ReadUInt16()",
            "SByte" => "reader.ReadSByte()",
            "Single" => "reader.ReadSingle()",
            "Double" => "reader.ReadDouble()",
            "Decimal" => "reader.ReadDecimal()",
            "Boolean" => "reader.ReadBoolean()",
            "String" => "reader.ReadString()", // BinaryReader.ReadString() never returns null
            _ => isNullable
                ? $"JsonSerializer.Deserialize<{type}>(reader.ReadString())"
                : $"JsonSerializer.Deserialize<{type}>(reader.ReadString()) ?? throw new InvalidDataException(\"Non-nullable field was null in binary data\")"
        };
    }

    private static string GetBinaryWriteCode(string jsonTypeName, string valueExpr)
    {
        return jsonTypeName switch
        {
            "Int32" or "Int64" or "Int16" or "Byte" or
            "UInt32" or "UInt64" or "UInt16" or "SByte" or
            "Single" or "Double" or "Decimal" or "Boolean" or "String"
                => $"writer.Write({valueExpr});",
            _ => $"writer.Write(JsonSerializer.Serialize({valueExpr}));" // Complex types as JSON
        };
    }

    private sealed record SerializableComponentInfo(
        string Name,
        string Namespace,
        string FullName,
        ImmutableArray<SerializableFieldInfo> Fields,
        int Version,
        ImmutableArray<MigrationMethodInfo> Migrations);

    private sealed record SerializableFieldInfo(
        string Name,
        string Type,
        string JsonTypeName,
        bool IsNullable,
        string? DefaultValueLiteral = null)
    {
        /// <summary>
        /// Gets whether this field has a default value specified via [DefaultValue].
        /// </summary>
        public bool HasDefaultValue => DefaultValueLiteral is not null;
    };

    private sealed record MigrationMethodInfo(
        int FromVersion,
        string MethodName);
}
