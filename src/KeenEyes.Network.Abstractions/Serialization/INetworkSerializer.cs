namespace KeenEyes.Network.Serialization;

/// <summary>
/// Registry for network-serializable component types.
/// </summary>
/// <remarks>
/// <para>
/// This interface is implemented by generated code that registers all components
/// marked with <see cref="ReplicatedAttribute"/>. It provides type-safe serialization
/// without runtime reflection.
/// </para>
/// <para>
/// Unlike the save/load component serializer, this interface is optimized for
/// network transmission with support for bit-packing and quantization.
/// </para>
/// </remarks>
public interface INetworkSerializer
{
    /// <summary>
    /// Checks if a component type is registered for network serialization.
    /// </summary>
    /// <param name="type">The component type to check.</param>
    /// <returns>True if the type can be network serialized; false otherwise.</returns>
    bool IsNetworkSerializable(Type type);

    /// <summary>
    /// Gets the network type ID for a component type.
    /// </summary>
    /// <param name="type">The component type.</param>
    /// <returns>The network type ID, or null if not registered.</returns>
    /// <remarks>
    /// Network type IDs are compact identifiers (typically 8-16 bits) used to
    /// identify component types in network messages, rather than full type names.
    /// </remarks>
    ushort? GetNetworkTypeId(Type type);

    /// <summary>
    /// Gets the component type for a network type ID.
    /// </summary>
    /// <param name="networkTypeId">The network type ID.</param>
    /// <returns>The component type, or null if not registered.</returns>
    Type? GetTypeFromNetworkId(ushort networkTypeId);

    /// <summary>
    /// Serializes a component to a bit writer.
    /// </summary>
    /// <param name="type">The type of the component.</param>
    /// <param name="value">The component value to serialize.</param>
    /// <param name="writer">The bit writer to write to.</param>
    /// <returns>True if serialized successfully; false if type not registered.</returns>
    bool Serialize(Type type, object value, ref BitWriter writer);

    /// <summary>
    /// Deserializes a component from a bit reader.
    /// </summary>
    /// <param name="networkTypeId">The network type ID of the component.</param>
    /// <param name="reader">The bit reader to read from.</param>
    /// <returns>The deserialized component, or null if type not registered.</returns>
    object? Deserialize(ushort networkTypeId, ref BitReader reader);
}

/// <summary>
/// Provides metadata about a network-serializable component type.
/// </summary>
public readonly struct NetworkComponentInfo
{
    /// <summary>
    /// Gets the CLR type of the component.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Gets the network type ID.
    /// </summary>
    public required ushort NetworkTypeId { get; init; }

    /// <summary>
    /// Gets the synchronization strategy.
    /// </summary>
    public required SyncStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the target update frequency (updates per second).
    /// </summary>
    public required int Frequency { get; init; }

    /// <summary>
    /// Gets the network priority.
    /// </summary>
    public required byte Priority { get; init; }

    /// <summary>
    /// Gets whether the component supports interpolation.
    /// </summary>
    public required bool SupportsInterpolation { get; init; }

    /// <summary>
    /// Gets whether the component supports prediction.
    /// </summary>
    public required bool SupportsPrediction { get; init; }

    /// <summary>
    /// Gets whether the component supports delta serialization.
    /// </summary>
    public required bool SupportsDelta { get; init; }
}
