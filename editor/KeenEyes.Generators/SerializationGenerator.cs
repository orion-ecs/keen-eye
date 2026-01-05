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
/// </summary>
[Generator]
public sealed class SerializationGenerator : IIncrementalGenerator
{
    private const string ComponentAttribute = "KeenEyes.ComponentAttribute";

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

            var isNullable = field.Type.NullableAnnotation == NullableAnnotation.Annotated;
            fields.Add(new SerializableFieldInfo(
                field.Name,
                field.Type.ToDisplayString(),
                GetJsonTypeName(field.Type),
                isNullable));
        }

        return new SerializableComponentInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            fields.ToImmutableArray(),
            version);
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

    private static string GenerateSerializationRegistry(ImmutableArray<SerializableComponentInfo> components)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
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
        sb.AppendLine("public sealed class ComponentSerializer : IComponentSerializer, IBinaryComponentSerializer");
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

        // Static constructor to initialize dictionaries
        sb.AppendLine("    static ComponentSerializer()");
        sb.AppendLine("    {");
        sb.AppendLine("        ComponentsByName = new Dictionary<string, ComponentSerializationInfo>();");
        sb.AppendLine("        ComponentsByType = new Dictionary<Type, ComponentSerializationInfo>();");
        sb.AppendLine();

        foreach (var component in components)
        {
            sb.AppendLine($"        var info_{component.Name} = new ComponentSerializationInfo(");
            sb.AppendLine($"            typeof({component.FullName}),");
            sb.AppendLine($"            Deserialize_{component.Name},");
            sb.AppendLine($"            value => Serialize_{component.Name}(({component.FullName})value),");
            sb.AppendLine($"            DeserializeBinary_{component.Name},");
            sb.AppendLine($"            (value, writer) => SerializeBinary_{component.Name}(({component.FullName})value, writer),");
            sb.AppendLine($"            (serialization, isTag) => serialization.Components.Register<{component.FullName}>(isTag),");
            sb.AppendLine($"            (serialization, value) => serialization.SetSingleton(({component.FullName})value),");
            sb.AppendLine($"            static () => new {component.FullName}(),");
            sb.AppendLine($"            {component.Version});");
            sb.AppendLine();
            sb.AppendLine($"        ComponentsByName[typeof({component.FullName}).AssemblyQualifiedName!] = info_{component.Name};");
            sb.AppendLine($"        ComponentsByName[\"{component.FullName}\"] = info_{component.Name};");
            sb.AppendLine($"        ComponentsByType[typeof({component.FullName})] = info_{component.Name};");
            sb.AppendLine();
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
            var binaryWrite = GetBinaryWriteCode(field.JsonTypeName, field.Type, $"value.{field.Name}");
            sb.AppendLine($"        {binaryWrite}");
        }

        sb.AppendLine("    }");
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

    private static string GetBinaryWriteCode(string jsonTypeName, string type, string valueExpr)
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
        int Version);

    private sealed record SerializableFieldInfo(
        string Name,
        string Type,
        string JsonTypeName,
        bool IsNullable);
}
