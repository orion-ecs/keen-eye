using System.Text.Json;
using KeenEyes.Serialization;

namespace KeenEyes.Tests.Serialization;

/// <summary>
/// Binary component serializer for testing purposes.
/// Implements IBinaryComponentSerializer by storing JSON as strings in binary format.
/// </summary>
/// <remarks>
/// This implementation stores JSON strings as the binary format for simplicity in tests.
/// Production code would use a more efficient binary serialization format.
/// </remarks>
internal sealed class TestBinarySerializer : IBinaryComponentSerializer
{
    private readonly Dictionary<string, Func<BinaryReader, object>> deserializers = [];
    private readonly Dictionary<Type, Action<object, BinaryWriter>> serializers = [];

    /// <summary>
    /// Registers a component type for binary serialization.
    /// </summary>
    public TestBinarySerializer WithComponent<T>(string? typeName = null) where T : struct, IComponent
    {
        var type = typeof(T);
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

        RegisterDeserializer<T>(name);

        if (type.FullName is not null && type.FullName != name)
        {
            RegisterDeserializer<T>(type.FullName);
        }

        serializers[type] = (obj, writer) =>
        {
            var json = JsonSerializer.Serialize((T)obj, TestJsonSerializer.Options);
            writer.Write(json);
        };

        return this;
    }

    /// <summary>
    /// Registers a struct type for binary serialization (not a component).
    /// </summary>
    public TestBinarySerializer WithStruct<T>(string? typeName = null) where T : struct
    {
        var type = typeof(T);
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;

        RegisterDeserializer<T>(name);

        if (type.FullName is not null && type.FullName != name)
        {
            RegisterDeserializer<T>(type.FullName);
        }

        serializers[type] = (obj, writer) =>
        {
            var json = JsonSerializer.Serialize((T)obj, TestJsonSerializer.Options);
            writer.Write(json);
        };

        return this;
    }

    private void RegisterDeserializer<T>(string name) where T : struct
    {
        deserializers[name] = reader =>
        {
            var json = reader.ReadString();
            return JsonSerializer.Deserialize<T>(json, TestJsonSerializer.Options)!;
        };
    }

    /// <inheritdoc />
    public bool WriteTo(Type type, object value, BinaryWriter writer)
    {
        if (serializers.TryGetValue(type, out var serializer))
        {
            serializer(value, writer);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public object? ReadFrom(string typeName, BinaryReader reader)
    {
        if (deserializers.TryGetValue(typeName, out var deserializer))
        {
            return deserializer(reader);
        }
        return null;
    }
}
