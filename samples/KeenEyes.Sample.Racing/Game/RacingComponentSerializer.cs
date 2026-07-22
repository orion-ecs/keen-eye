using System;
using System.IO;
using System.Numerics;
using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Common;
using KeenEyes.Generated;
using KeenEyes.Serialization;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// A component serializer that adds <see cref="Transform3D"/> support on top of the
/// generated <see cref="ComponentSerializer"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the key integration piece for the ghost pipeline. The source-generated
/// <see cref="ComponentSerializer"/> only knows about components declared with
/// <c>[Component(Serializable = true)]</c> <em>inside this project</em>. The ghost
/// extractor, however, reads <see cref="Transform3D"/> - an engine type from
/// <c>KeenEyes.Common</c> that the generator never sees. Without a serializer that
/// can turn <see cref="Transform3D"/> into JSON, it would never appear in replay
/// snapshots and no ghost could be extracted.
/// </para>
/// <para>
/// The wrapper delegates every call to the generated serializer, intercepting only
/// <see cref="Transform3D"/>. The JSON layout it emits (<c>Position</c>,
/// <c>Rotation</c>, <c>Scale</c> objects with <c>X/Y/Z/W</c> members) is exactly what
/// <see cref="KeenEyes.Replay.Ghost.GhostExtractor"/> looks for. Serialization is done
/// with an explicit <see cref="Utf8JsonWriter"/> rather than reflection so the sample
/// stays consistent with the engine's Native-AOT-friendly conventions.
/// </para>
/// </remarks>
public sealed class RacingComponentSerializer : IComponentSerializer
{
    private const string Transform3DName = "KeenEyes.Common.Transform3D";

    private readonly IComponentSerializer inner = ComponentSerializer.Instance;

    private static bool IsTransform(Type type) => type == typeof(Transform3D);

    private static bool IsTransform(string typeName) =>
        typeName.StartsWith(Transform3DName, StringComparison.Ordinal);

    /// <inheritdoc />
    public bool IsSerializable(Type type) => IsTransform(type) || inner.IsSerializable(type);

    /// <inheritdoc />
    public bool IsSerializable(string typeName) => IsTransform(typeName) || inner.IsSerializable(typeName);

    /// <inheritdoc />
    public JsonElement? Serialize(Type type, object value)
    {
        if (!IsTransform(type))
        {
            return inner.Serialize(type, value);
        }

        var transform = (Transform3D)value;

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            WriteVector3(writer, "Position", transform.Position);
            WriteQuaternion(writer, "Rotation", transform.Rotation);
            WriteVector3(writer, "Scale", transform.Scale);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    /// <inheritdoc />
    public object? Deserialize(string typeName, JsonElement json)
    {
        if (!IsTransform(typeName))
        {
            return inner.Deserialize(typeName, json);
        }

        var position = ReadVector3(json, "Position");
        var rotation = ReadQuaternion(json, "Rotation");
        var scale = ReadVector3(json, "Scale");
        return new Transform3D(position, rotation, scale);
    }

    /// <inheritdoc />
    public Type? GetType(string typeName) =>
        IsTransform(typeName) ? typeof(Transform3D) : inner.GetType(typeName);

    /// <inheritdoc />
    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
    {
        ArgumentNullException.ThrowIfNull(serialization);
        return IsTransform(typeName)
            ? (ComponentInfo)serialization.Components.Register<Transform3D>(isTag)
            : inner.RegisterComponent(serialization, typeName, isTag);
    }

    /// <inheritdoc />
    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value) =>
        inner.SetSingleton(serialization, typeName, value);

    /// <inheritdoc />
    public object? CreateDefault(string typeName) =>
        IsTransform(typeName) ? Transform3D.Identity : inner.CreateDefault(typeName);

    /// <inheritdoc />
    public int GetVersion(string typeName) => IsTransform(typeName) ? 1 : inner.GetVersion(typeName);

    /// <inheritdoc />
    public int GetVersion(Type type) => IsTransform(type) ? 1 : inner.GetVersion(type);

    private static void WriteVector3(Utf8JsonWriter writer, string name, Vector3 value)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteEndObject();
    }

    private static void WriteQuaternion(Utf8JsonWriter writer, string name, Quaternion value)
    {
        writer.WriteStartObject(name);
        writer.WriteNumber("X", value.X);
        writer.WriteNumber("Y", value.Y);
        writer.WriteNumber("Z", value.Z);
        writer.WriteNumber("W", value.W);
        writer.WriteEndObject();
    }

    private static Vector3 ReadVector3(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return Vector3.Zero;
        }

        return new Vector3(
            value.GetProperty("X").GetSingle(),
            value.GetProperty("Y").GetSingle(),
            value.GetProperty("Z").GetSingle());
    }

    private static Quaternion ReadQuaternion(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return Quaternion.Identity;
        }

        return new Quaternion(
            value.GetProperty("X").GetSingle(),
            value.GetProperty("Y").GetSingle(),
            value.GetProperty("Z").GetSingle(),
            value.GetProperty("W").GetSingle());
    }
}
