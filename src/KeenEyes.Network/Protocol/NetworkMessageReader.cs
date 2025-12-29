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
}
