using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeenEyes.Serialization;

namespace KeenEyes.Tests;

/// <summary>
/// Helper methods for creating test fixtures without requiring a full World instance.
/// These helpers create properly configured ComponentInfo instances for AOT-compatible testing.
/// </summary>
internal static class TestComponentInfo
{
    private static int nextId;

    /// <summary>
    /// Creates a ComponentInfo for testing purposes with all required delegates configured.
    /// </summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="isTag">Whether this is a tag component.</param>
    /// <returns>A fully configured ComponentInfo instance.</returns>
    public static ComponentInfo Create<T>(bool isTag = false) where T : struct, IComponent
    {
        var id = new ComponentId(Interlocked.Increment(ref nextId) - 1);
        var size = isTag ? 0 : Unsafe.SizeOf<T>();

        var info = new ComponentInfo(id, typeof(T), size, isTag)
        {
            CreateComponentArray = capacity => new FixedComponentArray<T>(capacity),
            ApplyToBuilder = (builder, boxedValue) => builder.With((T)boxedValue),
        };

        if (isTag)
        {
            object defaultValue = default(T)!;
            info.ApplyTagToBuilder = builder => builder.WithBoxed(info, defaultValue);
        }

        return info;
    }
}

/// <summary>
/// Creates a fully-configured serializer for serialization tests.
/// </summary>
internal static class TestSerializerFactory
{
    /// <summary>
    /// Creates a serializer with all common test component types registered.
    /// </summary>
    public static TestComponentSerializer CreateForSerializationTests()
    {
        return new TestComponentSerializer()
            .WithComponent<SerializablePosition>()
            .WithComponent<SerializableVelocity>()
            .WithComponent<SerializableHealth>()
            .WithComponent<SerializableTag>()
            .WithStruct<SerializableGameTime>()
            .WithStruct<SerializableConfig>();
    }
}

/// <summary>
/// A mock IComponentSerializer for testing that supports common test component types.
/// </summary>
internal sealed class TestComponentSerializer : IComponentSerializer
{
    // JSON options matching SnapshotManager's options
    private static readonly JsonSerializerOptions jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        IncludeFields = true,
        PropertyNameCaseInsensitive = true  // Handle both camelCase and PascalCase
    };

    private readonly Dictionary<string, Type> typeMap = [];
    private readonly Dictionary<Type, Func<JsonElement, object>> deserializers = [];
    private readonly Dictionary<Type, Func<object, JsonElement>> serializers = [];
    private readonly Dictionary<Type, Func<World, bool, ComponentInfo>> registrars = [];
    private readonly Dictionary<Type, Action<World, object>> singletonSetters = [];

    /// <summary>
    /// Registers a component type for serialization.
    /// </summary>
    public TestComponentSerializer WithComponent<T>(string? typeName = null) where T : struct, IComponent
    {
        var type = typeof(T);
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        typeMap[name] = type;

        // Also add short name and full name variants
        if (type.FullName is not null && type.FullName != name)
        {
            typeMap[type.FullName] = type;
        }
        if (type.Name != name)
        {
            typeMap[type.Name] = type;
        }

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        registrars[type] = (world, isTag) => world.Components.Register<T>(isTag);
        singletonSetters[type] = (world, value) => world.SetSingleton((T)value);

        return this;
    }

    /// <summary>
    /// Registers a struct type for singleton serialization (not a component).
    /// </summary>
    public TestComponentSerializer WithStruct<T>(string? typeName = null) where T : struct
    {
        var type = typeof(T);
        var name = typeName ?? type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
        typeMap[name] = type;

        // Also add short name and full name variants
        if (type.FullName is not null && type.FullName != name)
        {
            typeMap[type.FullName] = type;
        }
        if (type.Name != name)
        {
            typeMap[type.Name] = type;
        }

        deserializers[type] = json => JsonSerializer.Deserialize<T>(json.GetRawText(), jsonOptions)!;
        serializers[type] = obj =>
        {
            var jsonStr = JsonSerializer.Serialize((T)obj, jsonOptions);
            using var doc = JsonDocument.Parse(jsonStr);
            return doc.RootElement.Clone();
        };
        singletonSetters[type] = (world, value) => world.SetSingleton((T)value);
        // Note: No registrar for non-component types

        return this;
    }

    public bool IsSerializable(Type type) => deserializers.ContainsKey(type);

    public bool IsSerializable(string typeName) => typeMap.ContainsKey(typeName);

    public object? Deserialize(string typeName, JsonElement json)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            deserializers.TryGetValue(type, out var deserializer))
        {
            return deserializer(json);
        }
        return null;
    }

    public JsonElement? Serialize(Type type, object value)
    {
        if (serializers.TryGetValue(type, out var serializer))
        {
            return serializer(value);
        }
        return null;
    }

    public Type? GetType(string typeName)
    {
        return typeMap.TryGetValue(typeName, out var type) ? type : null;
    }

    public ComponentInfo? RegisterComponent(World world, string typeName, bool isTag)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            registrars.TryGetValue(type, out var registrar))
        {
            return registrar(world, isTag);
        }
        return null;
    }

    public bool SetSingleton(World world, string typeName, object value)
    {
        if (typeMap.TryGetValue(typeName, out var type) &&
            singletonSetters.TryGetValue(type, out var setter))
        {
            setter(world, value);
            return true;
        }
        return false;
    }
}
