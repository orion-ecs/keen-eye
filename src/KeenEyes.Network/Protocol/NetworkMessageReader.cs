using KeenEyes.Network.Serialization;

namespace KeenEyes.Network.Protocol;

/// <summary>
/// Helper for reading network protocol messages.
/// </summary>
public ref struct NetworkMessageReader(ReadOnlySpan<byte> data)
{
    private BitReader reader = new(data);

    /// <summary>
    /// Gets whether the reader has reached the end.
    /// </summary>
    public readonly bool IsAtEnd => reader.IsAtEnd;

    /// <summary>
    /// Gets the number of bits remaining.
    /// </summary>
    public readonly int BitsRemaining => reader.BitsRemaining;

    /// <summary>
    /// Reads signed bits from the buffer.
    /// </summary>
    /// <param name="bits">The number of bits to read.</param>
    /// <returns>The signed value.</returns>
    public int ReadSignedBits(int bits) => reader.ReadSignedBits(bits);

    /// <summary>
    /// Reads the message header.
    /// </summary>
    /// <param name="type">The message type.</param>
    /// <param name="tick">The server tick.</param>
    public void ReadHeader(out MessageType type, out uint tick)
    {
        type = (MessageType)reader.ReadByte();
        tick = reader.ReadUInt32();
    }

    /// <summary>
    /// Peeks the message type without advancing the reader.
    /// </summary>
    /// <returns>The message type.</returns>
    /// <remarks>
    /// Note: This is a simplified approach that consumes the byte.
    /// In production you'd want proper peek support.
    /// </remarks>
    public MessageType PeekMessageType()
    {
        return (MessageType)reader.ReadByte();
    }

    /// <summary>
    /// Reads an entity spawn message.
    /// </summary>
    /// <param name="networkId">The network ID of the spawned entity.</param>
    /// <param name="ownerId">The owner client ID (0 for server).</param>
    public void ReadEntitySpawn(out uint networkId, out int ownerId)
    {
        networkId = reader.ReadUInt32();
        ownerId = reader.ReadSignedBits(16);
    }

    /// <summary>
    /// Reads an entity despawn message.
    /// </summary>
    /// <param name="networkId">The network ID of the despawned entity.</param>
    public void ReadEntityDespawn(out uint networkId)
    {
        networkId = reader.ReadUInt32();
    }

    /// <summary>
    /// Reads a component type ID.
    /// </summary>
    /// <returns>The component type ID.</returns>
    public ushort ReadComponentTypeId()
    {
        return reader.ReadUInt16();
    }

    /// <summary>
    /// Reads a component using the network serializer.
    /// </summary>
    /// <param name="serializer">The network serializer.</param>
    /// <param name="componentType">Output: The type of the deserialized component.</param>
    /// <returns>The deserialized component, or null if type not registered.</returns>
    public object? ReadComponent(INetworkSerializer serializer, out Type? componentType)
    {
        var typeId = reader.ReadUInt16();
        componentType = serializer.GetTypeFromNetworkId(typeId);
        if (componentType is null)
        {
            return null;
        }

        return serializer.Deserialize(typeId, ref reader);
    }

    /// <summary>
    /// Reads the component count for an entity update.
    /// </summary>
    /// <returns>The number of components.</returns>
    public byte ReadComponentCount()
    {
        return reader.ReadByte();
    }

    /// <summary>
    /// Reads the network ID from a component update message.
    /// </summary>
    /// <returns>The network entity ID.</returns>
    public uint ReadNetworkId()
    {
        return reader.ReadUInt32();
    }

    /// <summary>
    /// Reads the entity count from a snapshot message.
    /// </summary>
    /// <returns>The number of entities in the snapshot.</returns>
    public ushort ReadEntityCount()
    {
        return reader.ReadUInt16();
    }

    /// <summary>
    /// Reads a byte value.
    /// </summary>
    /// <returns>The byte value.</returns>
    public byte ReadByte()
    {
        return reader.ReadByte();
    }
}
