using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;

namespace KeenEyes.Tests.Serialization;

/// <summary>
/// JSON-based component serializer for testing purposes.
/// Implements IComponentSerializer using System.Text.Json.
/// </summary>
internal sealed class TestJsonSerializer : IComponentSerializer
{
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly Dictionary<string, Type> typeMap = [];
    private readonly Dictionary<Type, Func<JsonElement, object>> deserializers = [];
    private readonly Dictionary<Type, Func<object, JsonElement>> serializers = [];
    private readonly Dictionary<Type, Func<ISerializationCapability, bool, ComponentInfo>> registrars = [];
    private readonly Dictionary<Type, Action<ISerializationCapability, object>> singletonSetters = [];

    /// <summary>
    /// Gets the JSON serializer options used by this serializer.
    /// </summary>
    public static JsonSerializerOptions Options => jsonOptions;

    /// <summary>
    /// Registers a component type for JSON serialization.
    /// </summary>
    public TestJsonSerializer WithComponent<T>(string? typeName = null) where T : struct, IComponent
    {
        var type = typeof(T);
        RegisterType(type, typeName);

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        registrars[type] = (serialization, isTag) => (ComponentInfo)serialization.Components.Register<T>(isTag);
        singletonSetters[type] = (serialization, value) => serialization.SetSingleton((T)value);

        return this;
    }

    /// <summary>
    /// Registers a struct type for singleton serialization (not a component).
    /// </summary>
    public TestJsonSerializer WithStruct<T>(string? typeName = null) where T : struct
    {
        var type = typeof(T);
        RegisterType(type, typeName);

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        singletonSetters[type] = (serialization, value) => serialization.SetSingleton((T)value);

        return this;
    }

    private void RegisterType(Type type, string? typeName)
    {
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        typeMap[name] = type;

        if (type.FullName is not null && type.FullName != name)
        {
            typeMap[type.FullName] = type;
        }
        if (type.Name != name)
        {
            typeMap[type.Name] = type;
        }
    }

    /// <inheritdoc />
    public bool IsSerializable(Type type) => deserializers.ContainsKey(type);

    /// <inheritdoc />
    public bool IsSerializable(string typeName) => typeMap.ContainsKey(typeName);

    /// <inheritdoc />
    public object? Deserialize(string typeName, JsonElement json)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            deserializers.TryGetValue(type, out var deserializer))
        {
            return deserializer(json);
        }
        return null;
    }

    /// <inheritdoc />
    public JsonElement? Serialize(Type type, object value)
    {
        if (serializers.TryGetValue(type, out var serializer))
        {
            return serializer(value);
        }
        return null;
    }

    /// <inheritdoc />
    public Type? GetType(string typeName)
    {
        return typeMap.TryGetValue(typeName, out var type) ? type : null;
    }

    /// <inheritdoc />
    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            registrars.TryGetValue(type, out var registrar))
        {
            return registrar(serialization, isTag);
        }
        return null;
    }

    /// <inheritdoc />
    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            singletonSetters.TryGetValue(type, out var setter))
        {
            setter(serialization, value);
            return true;
        }
        return false;
    }

    /// <inheritdoc />
    public object? CreateDefault(string typeName)
    {
        if (typeMap.TryGetValue(typeName, out var type))
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    /// <inheritdoc />
    public int GetVersion(string typeName) => 1;

    /// <inheritdoc />
    public int GetVersion(Type type) => 1;
}
