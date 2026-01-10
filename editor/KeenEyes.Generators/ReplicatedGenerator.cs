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
/// Generates network serialization code for components marked with [Replicated].
/// Produces INetworkSerializable implementations with bit-packed serialization,
/// delta encoding, and optional interpolation helpers.
/// </summary>
[Generator]
public sealed class ReplicatedGenerator : IIncrementalGenerator
{
    private const string ReplicatedAttribute = "KeenEyes.Network.ReplicatedAttribute";
    private const string QuantizedAttribute = "KeenEyes.Network.QuantizedAttribute";
    private const int MaxDeltaTrackableFields = 32;

    /// <summary>
    /// Diagnostic reported when a replicated component has more than 32 fields.
    /// </summary>
    private static readonly DiagnosticDescriptor TooManyFieldsForReplication = new(
        id: "KEEN100",
        title: "Too many fields for delta replication",
        messageFormat: "Component '{0}' has {1} fields but delta mask only supports 32. Fields after index 31 will not be tracked for delta updates.",
        category: "KeenEyes.Network",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all structs with [Replicated] attribute
        var replicatedComponents = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ReplicatedAttribute,
                predicate: static (node, _) => node is StructDeclarationSyntax,
                transform: static (ctx, _) => GetReplicatedComponentInfo(ctx))
            .Where(static info => info is not null);

        var collected = replicatedComponents.Collect();

        // Generate network serialization code
        context.RegisterSourceOutput(collected, static (ctx, components) =>
        {
            var validComponents = components
                .Where(c => c is not null)
                .Select(c => c!)
                .ToImmutableArray();

            if (validComponents.Length == 0)
            {
                return;
            }

            // Generate partial class for each component with INetworkSerializable implementation
            foreach (var component in validComponents)
            {
                // Warn if component has too many fields for delta replication
                if (component.Fields.Length > MaxDeltaTrackableFields && component.Location is not null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        TooManyFieldsForReplication,
                        component.Location,
                        component.Name,
                        component.Fields.Length));
                }

                var partialSource = GenerateNetworkSerializable(component);
                ctx.AddSource($"{component.FullName}.Network.g.cs", SourceText.From(partialSource, Encoding.UTF8));
            }

            // Generate the network serializer registry
            var registrySource = GenerateNetworkSerializerRegistry(validComponents);
            ctx.AddSource("NetworkSerializer.g.cs", SourceText.From(registrySource, Encoding.UTF8));

            // Generate the network interpolator registry
            var interpolatableComponents = validComponents.Where(c => c.GenerateInterpolation).ToImmutableArray();
            if (interpolatableComponents.Length > 0)
            {
                var interpolatorSource = GenerateNetworkInterpolatorRegistry(interpolatableComponents);
                ctx.AddSource("NetworkInterpolator.g.cs", SourceText.From(interpolatorSource, Encoding.UTF8));
            }
        });
    }

    private static ReplicatedComponentInfo? GetReplicatedComponentInfo(GeneratorAttributeSyntaxContext context)
    {
        var typeSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var attr = context.Attributes.FirstOrDefault();
        if (attr is null)
        {
            return null;
        }

        // Parse attribute properties
        var strategy = 0; // Default: Authoritative
        var generateInterpolation = false;
        var generatePrediction = false;
        var priority = (byte)128;
        var frequency = 0;

        foreach (var arg in attr.NamedArguments)
        {
            switch (arg.Key)
            {
                case "Strategy":
                    strategy = (int)(arg.Value.Value ?? 0);
                    break;
                case "GenerateInterpolation":
                    generateInterpolation = arg.Value.Value is true;
                    break;
                case "GeneratePrediction":
                    generatePrediction = arg.Value.Value is true;
                    break;
                case "Priority":
                    priority = (byte)(arg.Value.Value ?? 128);
                    break;
                case "Frequency":
                    frequency = (int)(arg.Value.Value ?? 0);
                    break;
            }
        }

        // Collect fields
        var fields = new List<ReplicatedFieldInfo>();
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

            // Check for [Quantized] attribute
            QuantizedInfo? quantized = null;
            var quantizedAttr = field.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == QuantizedAttribute);

            if (quantizedAttr is not null)
            {
                var args = quantizedAttr.ConstructorArguments;
                if (args.Length >= 3 &&
                    args[0].Value is float min &&
                    args[1].Value is float max &&
                    args[2].Value is float resolution)
                {
                    quantized = new QuantizedInfo(min, max, resolution);
                }
                // If types don't match, quantized remains null (skip quantization)
            }

            fields.Add(new ReplicatedFieldInfo(
                field.Name,
                field.Type.ToDisplayString(),
                GetFieldSerializationType(field.Type),
                quantized,
                IsInterpolatable(field.Type)));
        }

        return new ReplicatedComponentInfo(
            typeSymbol.Name,
            typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.ToDisplayString(),
            strategy,
            generateInterpolation,
            generatePrediction,
            priority,
            frequency,
            fields.ToImmutableArray(),
            typeSymbol.Locations.FirstOrDefault());
    }

    private static FieldSerializationType GetFieldSerializationType(ITypeSymbol type)
    {
        // Check special types first (primitives)
        var result = type.SpecialType switch
        {
            SpecialType.System_Boolean => FieldSerializationType.Bool,
            SpecialType.System_Byte => FieldSerializationType.Byte,
            SpecialType.System_SByte => FieldSerializationType.SByte,
            SpecialType.System_Int16 => FieldSerializationType.Int16,
            SpecialType.System_UInt16 => FieldSerializationType.UInt16,
            SpecialType.System_Int32 => FieldSerializationType.Int32,
            SpecialType.System_UInt32 => FieldSerializationType.UInt32,
            SpecialType.System_Int64 => FieldSerializationType.Int64,
            SpecialType.System_UInt64 => FieldSerializationType.UInt64,
            SpecialType.System_Single => FieldSerializationType.Float,
            SpecialType.System_Double => FieldSerializationType.Double,
            _ => (FieldSerializationType?)null
        };

        if (result.HasValue)
        {
            return result.Value;
        }

        // Check enums
        if (type.TypeKind == TypeKind.Enum)
        {
            return FieldSerializationType.Enum;
        }

        // Check System.Numerics types by full name
        var fullName = type.ToDisplayString();
        return fullName switch
        {
            "System.Numerics.Vector2" => FieldSerializationType.Vector2,
            "System.Numerics.Vector3" => FieldSerializationType.Vector3,
            "System.Numerics.Vector4" => FieldSerializationType.Vector4,
            "System.Numerics.Quaternion" => FieldSerializationType.Quaternion,
            _ => FieldSerializationType.Unsupported
        };
    }

    private static bool IsInterpolatable(ITypeSymbol type)
    {
        // Common interpolatable types (primitives)
        if (type.SpecialType is
            SpecialType.System_Single or
            SpecialType.System_Double or
            SpecialType.System_Int32 or
            SpecialType.System_Int64)
        {
            return true;
        }

        // System.Numerics types are interpolatable
        var fullName = type.ToDisplayString();
        return fullName is
            "System.Numerics.Vector2" or
            "System.Numerics.Vector3" or
            "System.Numerics.Vector4" or
            "System.Numerics.Quaternion";
    }

    private static string GenerateNetworkSerializable(ReplicatedComponentInfo info)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using KeenEyes.Common;");
        sb.AppendLine("using KeenEyes.Network.Serialization;");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(info.Namespace))
        {
            sb.AppendLine($"namespace {info.Namespace};");
            sb.AppendLine();
        }

        // Generate partial struct implementing interfaces
        var interfaces = new List<string> { "INetworkSerializable" };
        if (info.Fields.Length > 0)
        {
            interfaces.Add($"INetworkDeltaSerializable<{info.Name}>");
        }
        if (info.GenerateInterpolation && info.Fields.Any(f => f.IsInterpolatable))
        {
            interfaces.Add($"INetworkInterpolatable<{info.Name}>");
        }

        sb.AppendLine($"public partial struct {info.Name} : {string.Join(", ", interfaces)}");
        sb.AppendLine("{");

        // Generate NetworkSerialize
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Serializes all fields to the network writer.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public void NetworkSerialize(ref BitWriter writer)");
        sb.AppendLine("    {");
        foreach (var field in info.Fields)
        {
            GenerateFieldWrite(sb, field);
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate NetworkDeserialize
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Deserializes all fields from the network reader.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine("    public void NetworkDeserialize(ref BitReader reader)");
        sb.AppendLine("    {");
        foreach (var field in info.Fields)
        {
            GenerateFieldRead(sb, field);
        }
        sb.AppendLine("    }");

        // Generate delta serialization if we have fields
        if (info.Fields.Length > 0)
        {
            sb.AppendLine();
            GenerateDeltaMethods(sb, info);
        }

        // Generate interpolation if requested
        if (info.GenerateInterpolation && info.Fields.Any(f => f.IsInterpolatable))
        {
            sb.AppendLine();
            GenerateInterpolateMethod(sb, info);
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateFieldWrite(StringBuilder sb, ReplicatedFieldInfo field)
    {
        if (field.Quantized is not null)
        {
            var q = field.Quantized;
            sb.AppendLine($"        writer.WriteQuantized({field.Name}, {q.Min}f, {q.Max}f, {q.Resolution}f);");
        }
        else
        {
            switch (field.SerializationType)
            {
                case FieldSerializationType.Bool:
                    sb.AppendLine($"        writer.WriteBool({field.Name});");
                    break;
                case FieldSerializationType.Byte:
                    sb.AppendLine($"        writer.WriteByte({field.Name});");
                    break;
                case FieldSerializationType.SByte:
                    sb.AppendLine($"        writer.WriteSignedBits({field.Name}, 8);");
                    break;
                case FieldSerializationType.Int16:
                    sb.AppendLine($"        writer.WriteSignedBits({field.Name}, 16);");
                    break;
                case FieldSerializationType.UInt16:
                    sb.AppendLine($"        writer.WriteUInt16({field.Name});");
                    break;
                case FieldSerializationType.Int32:
                    sb.AppendLine($"        writer.WriteSignedBits({field.Name}, 32);");
                    break;
                case FieldSerializationType.UInt32:
                    sb.AppendLine($"        writer.WriteUInt32({field.Name});");
                    break;
                case FieldSerializationType.Float:
                    sb.AppendLine($"        writer.WriteFloat({field.Name});");
                    break;
                case FieldSerializationType.Double:
                    // Lossy for network - converts to float
                    sb.AppendLine($"        writer.WriteFloat((float){field.Name});");
                    break;
                case FieldSerializationType.Enum:
                    // Assume 8-bit enum
                    sb.AppendLine($"        writer.WriteBits((uint){field.Name}, 8);");
                    break;
                case FieldSerializationType.Vector2:
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.X);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.Y);");
                    break;
                case FieldSerializationType.Vector3:
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.X);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.Y);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.Z);");
                    break;
                case FieldSerializationType.Vector4:
                case FieldSerializationType.Quaternion:
                    // Both Vector4 and Quaternion have X, Y, Z, W components
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.X);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.Y);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.Z);");
                    sb.AppendLine($"        writer.WriteFloat({field.Name}.W);");
                    break;
                default:
                    // Generate compile error for unsupported types
                    sb.AppendLine($"#error Unsupported network serialization type '{field.TypeName}' for field '{field.Name}'. Add a custom serializer or use a supported type.");
                    break;
            }
        }
    }

    private static void GenerateFieldRead(StringBuilder sb, ReplicatedFieldInfo field)
    {
        if (field.Quantized is not null)
        {
            var q = field.Quantized;
            sb.AppendLine($"        {field.Name} = reader.ReadQuantized({q.Min}f, {q.Max}f, {q.Resolution}f);");
        }
        else
        {
            switch (field.SerializationType)
            {
                case FieldSerializationType.Bool:
                    sb.AppendLine($"        {field.Name} = reader.ReadBool();");
                    break;
                case FieldSerializationType.Byte:
                    sb.AppendLine($"        {field.Name} = reader.ReadByte();");
                    break;
                case FieldSerializationType.SByte:
                    sb.AppendLine($"        {field.Name} = (sbyte)reader.ReadSignedBits(8);");
                    break;
                case FieldSerializationType.Int16:
                    sb.AppendLine($"        {field.Name} = (short)reader.ReadSignedBits(16);");
                    break;
                case FieldSerializationType.UInt16:
                    sb.AppendLine($"        {field.Name} = reader.ReadUInt16();");
                    break;
                case FieldSerializationType.Int32:
                    sb.AppendLine($"        {field.Name} = reader.ReadSignedBits(32);");
                    break;
                case FieldSerializationType.UInt32:
                    sb.AppendLine($"        {field.Name} = reader.ReadUInt32();");
                    break;
                case FieldSerializationType.Float:
                    sb.AppendLine($"        {field.Name} = reader.ReadFloat();");
                    break;
                case FieldSerializationType.Double:
                    sb.AppendLine($"        {field.Name} = reader.ReadFloat();");
                    break;
                case FieldSerializationType.Enum:
                    sb.AppendLine($"        {field.Name} = ({field.TypeName})reader.ReadBits(8);");
                    break;
                case FieldSerializationType.Vector2:
                    sb.AppendLine($"        {field.Name} = new System.Numerics.Vector2(reader.ReadFloat(), reader.ReadFloat());");
                    break;
                case FieldSerializationType.Vector3:
                    sb.AppendLine($"        {field.Name} = new System.Numerics.Vector3(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());");
                    break;
                case FieldSerializationType.Vector4:
                    sb.AppendLine($"        {field.Name} = new System.Numerics.Vector4(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());");
                    break;
                case FieldSerializationType.Quaternion:
                    sb.AppendLine($"        {field.Name} = new System.Numerics.Quaternion(reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat(), reader.ReadFloat());");
                    break;
                default:
                    // Generate compile error for unsupported types
                    sb.AppendLine($"#error Unsupported network deserialization type '{field.TypeName}' for field '{field.Name}'. Add a custom serializer or use a supported type.");
                    break;
            }
        }
    }

    private static void GenerateDeltaMethods(StringBuilder sb, ReplicatedComponentInfo info)
    {
        // GetDirtyMask
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Gets a bitmask of fields that differ from baseline.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public uint GetDirtyMask(in {info.Name} baseline)");
        sb.AppendLine("    {");
        sb.AppendLine("        uint mask = 0;");
        for (int i = 0; i < info.Fields.Length && i < 32; i++)
        {
            var field = info.Fields[i];
            var comparison = field.SerializationType switch
            {
                // Use approximate equality for floating-point types
                FieldSerializationType.Float => $"!{field.Name}.ApproximatelyEquals(baseline.{field.Name}, 0.0001f)",
                FieldSerializationType.Double => $"!((float){field.Name}).ApproximatelyEquals((float)baseline.{field.Name}, 0.0001f)",
                // Vectors and Quaternions use != operator (exact comparison)
                _ => $"{field.Name} != baseline.{field.Name}"
            };
            sb.AppendLine($"        if ({comparison}) mask |= {1u << i};");
        }
        sb.AppendLine("        return mask;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // NetworkSerializeDelta
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Serializes only changed fields.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public void NetworkSerializeDelta(ref BitWriter writer, in {info.Name} baseline, uint dirtyMask)");
        sb.AppendLine("    {");
        for (int i = 0; i < info.Fields.Length && i < 32; i++)
        {
            var field = info.Fields[i];
            sb.AppendLine($"        if ((dirtyMask & {1u << i}) != 0)");
            sb.AppendLine("        {");
            GenerateFieldWrite(sb, field);
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // NetworkDeserializeDelta
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Deserializes changed fields onto baseline.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public void NetworkDeserializeDelta(ref BitReader reader, ref {info.Name} baseline, uint dirtyMask)");
        sb.AppendLine("    {");
        for (int i = 0; i < info.Fields.Length && i < 32; i++)
        {
            var field = info.Fields[i];
            sb.AppendLine($"        if ((dirtyMask & {1u << i}) != 0)");
            sb.AppendLine("        {");
            GenerateFieldRead(sb, field);
            sb.AppendLine("        }");
        }
        sb.AppendLine("    }");
    }

    private static void GenerateInterpolateMethod(StringBuilder sb, ReplicatedComponentInfo info)
    {
        sb.AppendLine("    /// <summary>");
        sb.AppendLine("    /// Interpolates between two component states.");
        sb.AppendLine("    /// </summary>");
        sb.AppendLine($"    public static {info.Name} Interpolate(in {info.Name} from, in {info.Name} to, float t)");
        sb.AppendLine("    {");
        sb.AppendLine($"        return new {info.Name}");
        sb.AppendLine("        {");
        foreach (var field in info.Fields)
        {
            if (field.IsInterpolatable)
            {
                var interpolation = field.SerializationType switch
                {
                    FieldSerializationType.Float or FieldSerializationType.Double =>
                        $"from.{field.Name} + (to.{field.Name} - from.{field.Name}) * t",
                    FieldSerializationType.Int32 =>
                        $"(int)(from.{field.Name} + (to.{field.Name} - from.{field.Name}) * t)",
                    FieldSerializationType.Int64 =>
                        $"(long)(from.{field.Name} + (to.{field.Name} - from.{field.Name}) * t)",
                    FieldSerializationType.Vector2 =>
                        $"System.Numerics.Vector2.Lerp(from.{field.Name}, to.{field.Name}, t)",
                    FieldSerializationType.Vector3 =>
                        $"System.Numerics.Vector3.Lerp(from.{field.Name}, to.{field.Name}, t)",
                    FieldSerializationType.Vector4 =>
                        $"System.Numerics.Vector4.Lerp(from.{field.Name}, to.{field.Name}, t)",
                    FieldSerializationType.Quaternion =>
                        $"System.Numerics.Quaternion.Slerp(from.{field.Name}, to.{field.Name}, t)",
                    _ => $"t >= 0.5f ? to.{field.Name} : from.{field.Name}"
                };
                sb.AppendLine($"            {field.Name} = {interpolation},");
            }
            else
            {
                // Non-interpolatable fields snap to 'to' at t >= 0.5
                sb.AppendLine($"            {field.Name} = t >= 0.5f ? to.{field.Name} : from.{field.Name},");
            }
        }
        sb.AppendLine("        };");
        sb.AppendLine("    }");
    }

    private static string GenerateNetworkSerializerRegistry(ImmutableArray<ReplicatedComponentInfo> components)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using KeenEyes.Network;");
        sb.AppendLine("using KeenEyes.Network.Serialization;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated network serializer registry for replicated components.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class NetworkSerializer : INetworkSerializer");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly Dictionary<Type, ushort> typeToId = new()");
        sb.AppendLine("    {");
        for (ushort i = 0; i < components.Length; i++)
        {
            sb.AppendLine($"        [typeof({components[i].FullName})] = {i + 1},");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    private static readonly Dictionary<ushort, Type> idToType = new()");
        sb.AppendLine("    {");
        for (ushort i = 0; i < components.Length; i++)
        {
            sb.AppendLine($"        [{i + 1}] = typeof({components[i].FullName}),");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        // IsNetworkSerializable
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool IsNetworkSerializable(Type type) => typeToId.ContainsKey(type);");
        sb.AppendLine();

        // GetNetworkTypeId
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public ushort? GetNetworkTypeId(Type type) => typeToId.TryGetValue(type, out var id) ? id : null;");
        sb.AppendLine();

        // GetTypeFromNetworkId
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public Type? GetTypeFromNetworkId(ushort networkTypeId) => idToType.TryGetValue(networkTypeId, out var type) ? type : null;");
        sb.AppendLine();

        // Serialize
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool Serialize(Type type, object value, ref BitWriter writer)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!typeToId.TryGetValue(type, out var id)) return false;");
        sb.AppendLine("        switch (id)");
        sb.AppendLine("        {");
        for (ushort i = 0; i < components.Length; i++)
        {
            sb.AppendLine($"            case {i + 1}:");
            sb.AppendLine($"                (({components[i].FullName})value).NetworkSerialize(ref writer);");
            sb.AppendLine("                return true;");
        }
        sb.AppendLine("            default: return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Deserialize
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? Deserialize(ushort networkTypeId, ref BitReader reader)");
        sb.AppendLine("    {");
        sb.AppendLine("        switch (networkTypeId)");
        sb.AppendLine("        {");
        for (ushort i = 0; i < components.Length; i++)
        {
            sb.AppendLine($"            case {i + 1}:");
            sb.AppendLine($"                var v{i} = new {components[i].FullName}();");
            sb.AppendLine($"                v{i}.NetworkDeserialize(ref reader);");
            sb.AppendLine($"                return v{i};");
        }
        sb.AppendLine("            default: return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // GetRegisteredTypes
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IEnumerable<Type> GetRegisteredTypes() => typeToId.Keys;");
        sb.AppendLine();

        // GetRegisteredComponentInfo
        sb.AppendLine("    private static readonly NetworkComponentInfo[] componentInfos =");
        sb.AppendLine("    [");
        for (ushort i = 0; i < components.Length; i++)
        {
            var comp = components[i];
            var strategy = comp.Strategy switch
            {
                0 => "SyncStrategy.Authoritative",
                1 => "SyncStrategy.OwnerPredicted",
                2 => "SyncStrategy.InterpolatedOnly",
                _ => "SyncStrategy.Authoritative"
            };
            sb.AppendLine($"        new NetworkComponentInfo");
            sb.AppendLine("        {");
            sb.AppendLine($"            Type = typeof({comp.FullName}),");
            sb.AppendLine($"            NetworkTypeId = {i + 1},");
            sb.AppendLine($"            Strategy = {strategy},");
            sb.AppendLine($"            Frequency = {comp.Frequency},");
            sb.AppendLine($"            Priority = {comp.Priority},");
            sb.AppendLine($"            SupportsInterpolation = {(comp.GenerateInterpolation ? "true" : "false")},");
            sb.AppendLine($"            SupportsPrediction = {(comp.GeneratePrediction ? "true" : "false")},");
            sb.AppendLine($"            SupportsDelta = {(comp.Fields.Length > 0 ? "true" : "false")},");
            sb.AppendLine("        },");
        }
        sb.AppendLine("    ];");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public IEnumerable<NetworkComponentInfo> GetRegisteredComponentInfo() => componentInfos;");
        sb.AppendLine();

        // SupportsDelta
        sb.AppendLine("    private static readonly HashSet<Type> deltaTypes = new()");
        sb.AppendLine("    {");
        foreach (var comp in components.Where(c => c.Fields.Length > 0))
        {
            sb.AppendLine($"        typeof({comp.FullName}),");
        }
        sb.AppendLine("    };");
        sb.AppendLine();
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool SupportsDelta(Type type) => deltaTypes.Contains(type);");
        sb.AppendLine();

        // GetDirtyMask
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public uint GetDirtyMask(Type type, object current, object baseline)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!typeToId.TryGetValue(type, out var id)) return 0;");
        sb.AppendLine("        switch (id)");
        sb.AppendLine("        {");
        for (ushort i = 0; i < components.Length; i++)
        {
            if (components[i].Fields.Length > 0)
            {
                sb.AppendLine($"            case {i + 1}:");
                sb.AppendLine($"                return (({components[i].FullName})current).GetDirtyMask(({components[i].FullName})baseline);");
            }
        }
        sb.AppendLine("            default: return 0;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // SerializeDelta
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool SerializeDelta(Type type, object current, object baseline, ref BitWriter writer)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (!typeToId.TryGetValue(type, out var id)) return false;");
        sb.AppendLine("        switch (id)");
        sb.AppendLine("        {");
        for (ushort i = 0; i < components.Length; i++)
        {
            if (components[i].Fields.Length > 0)
            {
                sb.AppendLine($"            case {i + 1}:");
                sb.AppendLine("            {");
                sb.AppendLine($"                var c = ({components[i].FullName})current;");
                sb.AppendLine($"                var b = ({components[i].FullName})baseline;");
                sb.AppendLine("                var mask = c.GetDirtyMask(b);");
                sb.AppendLine("                writer.WriteUInt32(mask);");
                sb.AppendLine("                if (mask != 0) c.NetworkSerializeDelta(ref writer, b, mask);");
                sb.AppendLine("                return true;");
                sb.AppendLine("            }");
            }
        }
        sb.AppendLine("            default: return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // DeserializeDelta
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? DeserializeDelta(ushort networkTypeId, ref BitReader reader, object baseline)");
        sb.AppendLine("    {");
        sb.AppendLine("        var mask = reader.ReadUInt32();");
        sb.AppendLine("        if (mask == 0) return baseline;");
        sb.AppendLine("        switch (networkTypeId)");
        sb.AppendLine("        {");
        for (ushort i = 0; i < components.Length; i++)
        {
            if (components[i].Fields.Length > 0)
            {
                sb.AppendLine($"            case {i + 1}:");
                sb.AppendLine("            {");
                sb.AppendLine($"                var b = ({components[i].FullName})baseline;");
                sb.AppendLine($"                new {components[i].FullName}().NetworkDeserializeDelta(ref reader, ref b, mask);");
                sb.AppendLine("                return b;");
                sb.AppendLine("            }");
            }
        }
        sb.AppendLine("            default: return baseline;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateNetworkInterpolatorRegistry(ImmutableArray<ReplicatedComponentInfo> components)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using KeenEyes.Network.Serialization;");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine("/// Auto-generated network interpolator registry for interpolatable components.");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public sealed class NetworkInterpolator : INetworkInterpolator");
        sb.AppendLine("{");
        sb.AppendLine("    private static readonly HashSet<Type> interpolatableTypes = new()");
        sb.AppendLine("    {");
        foreach (var component in components)
        {
            sb.AppendLine($"        typeof({component.FullName}),");
        }
        sb.AppendLine("    };");
        sb.AppendLine();

        // IsInterpolatable
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public bool IsInterpolatable(Type type) => interpolatableTypes.Contains(type);");
        sb.AppendLine();

        // Interpolate
        sb.AppendLine("    /// <inheritdoc />");
        sb.AppendLine("    public object? Interpolate(Type type, object from, object to, float factor)");
        sb.AppendLine("    {");
        if (components.Length > 0)
        {
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                var condition = i == 0 ? "if" : "else if";
                sb.AppendLine($"        {condition} (type == typeof({component.FullName}))");
                sb.AppendLine("        {");
                sb.AppendLine($"            return {component.FullName}.Interpolate(({component.FullName})from, ({component.FullName})to, factor);");
                sb.AppendLine("        }");
            }
        }
        sb.AppendLine("        return null;");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}

// Data classes for generator
internal sealed record ReplicatedComponentInfo(
    string Name,
    string Namespace,
    string FullName,
    int Strategy,
    bool GenerateInterpolation,
    bool GeneratePrediction,
    byte Priority,
    int Frequency,
    ImmutableArray<ReplicatedFieldInfo> Fields,
    Location? Location);

internal sealed record ReplicatedFieldInfo(
    string Name,
    string TypeName,
    FieldSerializationType SerializationType,
    QuantizedInfo? Quantized,
    bool IsInterpolatable);

internal sealed record QuantizedInfo(float Min, float Max, float Resolution);

internal enum FieldSerializationType
{
    Bool,
    Byte,
    SByte,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float,
    Double,
    Enum,
    Vector2,
    Vector3,
    Vector4,
    Quaternion,
    Unsupported
}
