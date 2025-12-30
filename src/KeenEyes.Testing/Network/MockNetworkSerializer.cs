using KeenEyes.Network;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Testing.Network;

/// <summary>
/// A mock implementation of <see cref="INetworkSerializer"/> for testing network serialization
/// without real network infrastructure.
/// </summary>
/// <remarks>
/// <para>
/// MockNetworkSerializer allows registering component types with their serialization handlers,
/// making it easy to test network code in isolation. Each registered type gets a unique
/// network ID and can optionally support delta serialization.
/// </para>
/// <para>
/// Use <see cref="RegisterComponent{T}"/> to add component types for serialization.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var serializer = new MockNetworkSerializer();
/// serializer.RegisterComponent&lt;Position&gt;(
///     serialize: (ref BitWriter w, Position p) => { w.WriteFloat(p.X); w.WriteFloat(p.Y); },
///     deserialize: (ref BitReader r) => new Position { X = r.ReadFloat(), Y = r.ReadFloat() }
/// );
///
/// // Use in tests
/// var buffer = new byte[64];
/// var writer = new BitWriter(buffer);
/// serializer.Serialize(typeof(Position), new Position { X = 1, Y = 2 }, ref writer);
/// </code>
/// </example>
public sealed class MockNetworkSerializer : INetworkSerializer
{
    private readonly Dictionary<Type, ushort> typeToId = [];
    private readonly Dictionary<ushort, Type> idToType = [];
    private readonly Dictionary<Type, ComponentHandler> handlers = [];
    private readonly List<NetworkComponentInfo> componentInfos = [];
    private ushort nextId = 1;

    /// <summary>
    /// Registers a component type for serialization.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <param name="serialize">Action to serialize the component.</param>
    /// <param name="deserialize">Function to deserialize the component.</param>
    /// <param name="info">Optional component info. If null, defaults are used.</param>
    /// <returns>The assigned network type ID.</returns>
    public ushort RegisterComponent<T>(
        SerializeDelegate<T> serialize,
        DeserializeDelegate<T> deserialize,
        NetworkComponentInfo? info = null) where T : struct
    {
        var type = typeof(T);
        if (typeToId.ContainsKey(type))
        {
            throw new InvalidOperationException($"Type {type.Name} is already registered.");
        }

        var id = nextId++;
        typeToId[type] = id;
        idToType[id] = type;

        handlers[type] = new ComponentHandler(
            (object value, ref BitWriter writer) => serialize(ref writer, (T)value),
            (ref BitReader reader) => deserialize(ref reader)!,
            null,
            null,
            null
        );

        componentInfos.Add(info ?? new NetworkComponentInfo
        {
            Type = type,
            NetworkTypeId = id,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = false,
        });

        return id;
    }

    /// <summary>
    /// Registers a component type with delta serialization support.
    /// </summary>
    /// <typeparam name="T">The component type to register.</typeparam>
    /// <param name="serialize">Action to serialize the component.</param>
    /// <param name="deserialize">Function to deserialize the component.</param>
    /// <param name="getDirtyMask">Function to compute the dirty mask between current and baseline.</param>
    /// <param name="serializeDelta">Action to serialize only changed fields.</param>
    /// <param name="deserializeDelta">Function to apply delta to baseline.</param>
    /// <param name="info">Optional component info. If null, defaults with delta support are used.</param>
    /// <returns>The assigned network type ID.</returns>
    public ushort RegisterComponentWithDelta<T>(
        SerializeDelegate<T> serialize,
        DeserializeDelegate<T> deserialize,
        GetDirtyMaskDelegate<T> getDirtyMask,
        SerializeDeltaDelegate<T> serializeDelta,
        DeserializeDeltaDelegate<T> deserializeDelta,
        NetworkComponentInfo? info = null) where T : struct
    {
        var type = typeof(T);
        if (typeToId.ContainsKey(type))
        {
            throw new InvalidOperationException($"Type {type.Name} is already registered.");
        }

        var id = nextId++;
        typeToId[type] = id;
        idToType[id] = type;

        handlers[type] = new ComponentHandler(
            (object value, ref BitWriter writer) => serialize(ref writer, (T)value),
            (ref BitReader reader) => deserialize(ref reader)!,
            (object current, object baseline) => getDirtyMask((T)current, (T)baseline),
            (object current, object baseline, ref BitWriter writer) =>
            {
                var c = (T)current;
                var b = (T)baseline;
                var mask = getDirtyMask(c, b);
                writer.WriteUInt32(mask);
                if (mask != 0)
                {
                    serializeDelta(ref writer, c, b, mask);
                }
            },
            (ref BitReader reader, object baseline) =>
            {
                var mask = reader.ReadUInt32();
                if (mask == 0)
                {
                    return baseline;
                }

                var b = (T)baseline;
                return deserializeDelta(ref reader, b, mask);
            }
        );

        componentInfos.Add(info ?? new NetworkComponentInfo
        {
            Type = type,
            NetworkTypeId = id,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = true,
        });

        return id;
    }

    /// <inheritdoc />
    public bool IsNetworkSerializable(Type type) => typeToId.ContainsKey(type);

    /// <inheritdoc />
    public ushort? GetNetworkTypeId(Type type) =>
        typeToId.TryGetValue(type, out var id) ? id : null;

    /// <inheritdoc />
    public Type? GetTypeFromNetworkId(ushort networkTypeId) =>
        idToType.TryGetValue(networkTypeId, out var type) ? type : null;

    /// <inheritdoc />
    public bool Serialize(Type type, object value, ref BitWriter writer)
    {
        if (!handlers.TryGetValue(type, out var handler))
        {
            return false;
        }

        handler.Serialize(value, ref writer);
        return true;
    }

    /// <inheritdoc />
    public object? Deserialize(ushort networkTypeId, ref BitReader reader)
    {
        if (!idToType.TryGetValue(networkTypeId, out var type))
        {
            return null;
        }

        if (!handlers.TryGetValue(type, out var handler))
        {
            return null;
        }

        return handler.Deserialize(ref reader);
    }

    /// <inheritdoc />
    public IEnumerable<Type> GetRegisteredTypes() => typeToId.Keys;

    /// <inheritdoc />
    public IEnumerable<NetworkComponentInfo> GetRegisteredComponentInfo() => componentInfos;

    /// <inheritdoc />
    public bool SupportsDelta(Type type) =>
        handlers.TryGetValue(type, out var handler) && handler.GetDirtyMask is not null;

    /// <inheritdoc />
    public uint GetDirtyMask(Type type, object current, object baseline)
    {
        if (!handlers.TryGetValue(type, out var handler) || handler.GetDirtyMask is null)
        {
            return 0;
        }

        return handler.GetDirtyMask(current, baseline);
    }

    /// <inheritdoc />
    public bool SerializeDelta(Type type, object current, object baseline, ref BitWriter writer)
    {
        if (!handlers.TryGetValue(type, out var handler) || handler.SerializeDelta is null)
        {
            return false;
        }

        handler.SerializeDelta(current, baseline, ref writer);
        return true;
    }

    /// <inheritdoc />
    public object? DeserializeDelta(ushort networkTypeId, ref BitReader reader, object baseline)
    {
        if (!idToType.TryGetValue(networkTypeId, out var type))
        {
            return baseline;
        }

        if (!handlers.TryGetValue(type, out var handler) || handler.DeserializeDelta is null)
        {
            return baseline;
        }

        return handler.DeserializeDelta(ref reader, baseline);
    }

    /// <summary>
    /// Clears all registered components.
    /// </summary>
    public void Clear()
    {
        typeToId.Clear();
        idToType.Clear();
        handlers.Clear();
        componentInfos.Clear();
        nextId = 1;
    }

    private sealed record ComponentHandler(
        SerializeHandler Serialize,
        DeserializeHandler Deserialize,
        GetDirtyMaskHandler? GetDirtyMask,
        SerializeDeltaHandler? SerializeDelta,
        DeserializeDeltaHandler? DeserializeDelta
    );

    private delegate void SerializeHandler(object value, ref BitWriter writer);
    private delegate object DeserializeHandler(ref BitReader reader);
    private delegate uint GetDirtyMaskHandler(object current, object baseline);
    private delegate void SerializeDeltaHandler(object current, object baseline, ref BitWriter writer);
    private delegate object DeserializeDeltaHandler(ref BitReader reader, object baseline);
}

/// <summary>
/// Delegate for serializing a component.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="writer">The bit writer.</param>
/// <param name="value">The value to serialize.</param>
public delegate void SerializeDelegate<T>(ref BitWriter writer, T value) where T : struct;

/// <summary>
/// Delegate for deserializing a component.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="reader">The bit reader.</param>
/// <returns>The deserialized value.</returns>
public delegate T DeserializeDelegate<T>(ref BitReader reader) where T : struct;

/// <summary>
/// Delegate for computing the dirty mask between two component states.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="current">The current state.</param>
/// <param name="baseline">The baseline state.</param>
/// <returns>A bitmask indicating which fields changed.</returns>
public delegate uint GetDirtyMaskDelegate<T>(T current, T baseline) where T : struct;

/// <summary>
/// Delegate for serializing only changed fields.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="writer">The bit writer.</param>
/// <param name="current">The current state.</param>
/// <param name="baseline">The baseline state.</param>
/// <param name="dirtyMask">The dirty mask from GetDirtyMask.</param>
public delegate void SerializeDeltaDelegate<T>(ref BitWriter writer, T current, T baseline, uint dirtyMask) where T : struct;

/// <summary>
/// Delegate for applying delta to baseline.
/// </summary>
/// <typeparam name="T">The component type.</typeparam>
/// <param name="reader">The bit reader.</param>
/// <param name="baseline">The baseline to apply changes to.</param>
/// <param name="dirtyMask">The dirty mask indicating which fields to read.</param>
/// <returns>The updated value.</returns>
public delegate T DeserializeDeltaDelegate<T>(ref BitReader reader, T baseline, uint dirtyMask) where T : struct;
