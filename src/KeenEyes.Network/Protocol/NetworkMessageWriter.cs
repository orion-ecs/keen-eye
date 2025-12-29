using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Protocol;

/// <summary>
/// Helper for writing network protocol messages.
/// </summary>
public ref struct NetworkMessageWriter(Span<byte> buffer)
{
    private BitWriter writer = new(buffer);

    /// <summary>
    /// Writes signed bits to the buffer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="bits">The number of bits to use.</param>
    public void WriteSignedBits(int value, int bits) => writer.WriteSignedBits(value, bits);

    /// <summary>
    /// Writes an unsigned 32-bit integer to the buffer.
    /// </summary>
    /// <param name="value">The value to write.</param>
    public void WriteUInt32(uint value) => writer.WriteUInt32(value);

    /// <summary>
    /// Gets the number of bytes written.
    /// </summary>
    public readonly int BytesWritten => writer.BytesRequired;

    /// <summary>
    /// Writes the message header.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="tick">The server tick.</param>
    public void WriteHeader(MessageType type, uint tick)
    {
        writer.WriteByte((byte)type);
        writer.WriteUInt32(tick);
    }

    /// <summary>
    /// Writes an entity spawn message.
    /// </summary>
    /// <param name="networkId">The network ID of the spawned entity.</param>
    /// <param name="ownerId">The owner client ID (0 for server).</param>
    public void WriteEntitySpawn(uint networkId, int ownerId)
    {
        writer.WriteUInt32(networkId);
        writer.WriteSignedBits(ownerId, 16);
    }

    /// <summary>
    /// Writes an entity despawn message.
    /// </summary>
    /// <param name="networkId">The network ID of the despawned entity.</param>
    public void WriteEntityDespawn(uint networkId)
    {
        writer.WriteUInt32(networkId);
    }

    /// <summary>
    /// Writes a component type ID.
    /// </summary>
    /// <param name="componentTypeId">The component type ID.</param>
    public void WriteComponentTypeId(ushort componentTypeId)
    {
        writer.WriteUInt16(componentTypeId);
    }

    /// <summary>
    /// Writes a component using the network serializer.
    /// </summary>
    /// <param name="serializer">The network serializer.</param>
    /// <param name="componentType">The type of the component.</param>
    /// <param name="componentValue">The component value.</param>
    /// <returns>True if the component was serialized; false if not registered.</returns>
    public bool WriteComponent(INetworkSerializer serializer, Type componentType, object componentValue)
    {
        var typeId = serializer.GetNetworkTypeId(componentType);
        if (typeId is null)
        {
            return false;
        }

        writer.WriteUInt16(typeId.Value);
        return serializer.Serialize(componentType, componentValue, ref writer);
    }

    /// <summary>
    /// Writes the number of components that will follow.
    /// </summary>
    /// <param name="count">The component count.</param>
    public void WriteComponentCount(byte count)
    {
        writer.WriteByte(count);
    }

    /// <summary>
    /// Gets the written data as a span.
    /// </summary>
    /// <returns>A span containing all written bytes.</returns>
    public readonly ReadOnlySpan<byte> GetWrittenSpan()
    {
        return writer.GetWrittenSpan();
    }

    /// <summary>
    /// Copies the written data to a new byte array.
    /// </summary>
    /// <returns>A new byte array containing the written data.</returns>
    public readonly byte[] ToArray()
    {
        return writer.ToArray();
    }

    /// <summary>
    /// Writes a network ID.
    /// </summary>
    /// <param name="networkId">The network ID.</param>
    public void WriteNetworkId(uint networkId)
    {
        writer.WriteUInt32(networkId);
    }

    /// <summary>
    /// Writes an input using the input serializer.
    /// </summary>
    /// <param name="serializer">The input serializer.</param>
    /// <param name="input">The input value (boxed).</param>
    public void WriteInput(Prediction.IInputSerializer serializer, object input)
    {
        serializer.Serialize(input, ref writer);
    }

    /// <summary>
    /// Writes the entity count for a snapshot message.
    /// </summary>
    /// <param name="count">The number of entities in the snapshot.</param>
    public void WriteEntityCount(ushort count)
    {
        writer.WriteUInt16(count);
    }

    /// <summary>
    /// Writes a byte value.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public void WriteByte(byte value)
    {
        writer.WriteByte(value);
    }
}
