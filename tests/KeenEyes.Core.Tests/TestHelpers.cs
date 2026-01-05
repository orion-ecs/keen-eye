using System.Runtime.CompilerServices;
using System.Text.Json;
using KeenEyes.Capabilities;
using KeenEyes.Serialization;
using KeenEyes.Tests.Serialization;

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
/// A combined component serializer for testing that supports both JSON and binary formats.
/// Uses composition to delegate to specialized serializers.
/// </summary>
/// <remarks>
/// This class implements both IComponentSerializer and IBinaryComponentSerializer by
/// delegating to <see cref="TestJsonSerializer"/> and <see cref="TestBinarySerializer"/>
/// respectively. This maintains backward compatibility while separating concerns.
/// </remarks>
internal sealed class TestComponentSerializer : IComponentSerializer, IBinaryComponentSerializer
{
    private readonly TestJsonSerializer jsonSerializer = new();
    private readonly TestBinarySerializer binarySerializer = new();

    /// <summary>
    /// Registers a component type for serialization.
    /// </summary>
    public TestComponentSerializer WithComponent<T>(string? typeName = null) where T : struct, IComponent
    {
        jsonSerializer.WithComponent<T>(typeName);
        binarySerializer.WithComponent<T>(typeName);
        return this;
    }

    /// <summary>
    /// Registers a struct type for singleton serialization (not a component).
    /// </summary>
    public TestComponentSerializer WithStruct<T>(string? typeName = null) where T : struct
    {
        jsonSerializer.WithStruct<T>(typeName);
        binarySerializer.WithStruct<T>(typeName);
        return this;
    }

    /// <inheritdoc />
    public bool IsSerializable(Type type) => jsonSerializer.IsSerializable(type);

    /// <inheritdoc />
    public bool IsSerializable(string typeName) => jsonSerializer.IsSerializable(typeName);

    /// <inheritdoc />
    public object? Deserialize(string typeName, JsonElement json) => jsonSerializer.Deserialize(typeName, json);

    /// <inheritdoc />
    public JsonElement? Serialize(Type type, object value) => jsonSerializer.Serialize(type, value);

    /// <inheritdoc />
    public Type? GetType(string typeName) => jsonSerializer.GetType(typeName);

    /// <inheritdoc />
    public ComponentInfo? RegisterComponent(ISerializationCapability serialization, string typeName, bool isTag)
        => jsonSerializer.RegisterComponent(serialization, typeName, isTag);

    /// <inheritdoc />
    public bool SetSingleton(ISerializationCapability serialization, string typeName, object value)
        => jsonSerializer.SetSingleton(serialization, typeName, value);

    /// <inheritdoc />
    public object? CreateDefault(string typeName) => jsonSerializer.CreateDefault(typeName);

    /// <inheritdoc />
    public bool WriteTo(Type type, object value, BinaryWriter writer) => binarySerializer.WriteTo(type, value, writer);

    /// <inheritdoc />
    public object? ReadFrom(string typeName, BinaryReader reader) => binarySerializer.ReadFrom(typeName, reader);

    /// <inheritdoc />
    public int GetVersion(string typeName) => 1;

    /// <inheritdoc />
    public int GetVersion(Type type) => 1;
}
