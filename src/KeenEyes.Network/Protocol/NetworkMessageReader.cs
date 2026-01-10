using KeenEyes;
using KeenEyes.Capabilities;
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
    /// Reads a component using delta deserialization, applying changes to a baseline.
    /// </summary>
    /// <param name="serializer">The network serializer.</param>
    /// <param name="baseline">The baseline component value to apply changes to.</param>
    /// <param name="componentType">Output: The type of the deserialized component.</param>
    /// <returns>The updated component, or null if type not registered.</returns>
    public object? ReadComponentDelta(INetworkSerializer serializer, object baseline, out Type? componentType)
    {
        var typeId = reader.ReadUInt16();
        componentType = serializer.GetTypeFromNetworkId(typeId);
        if (componentType is null)
        {
            return null;
        }

        return serializer.DeserializeDelta(typeId, ref reader, baseline);
    }

    /// <summary>
    /// Reads a delta-encoded component, using the entity's current component value as baseline.
    /// </summary>
    /// <param name="serializer">The network serializer.</param>
    /// <param name="entity">The entity to get the baseline from.</param>
    /// <param name="world">The world containing the entity.</param>
    /// <param name="componentType">Output: The type of the deserialized component.</param>
    /// <returns>The updated component, or null if type not registered.</returns>
    public object? ReadComponentDeltaWithBaseline(INetworkSerializer serializer, Entity entity, IWorld world, out Type? componentType)
    {
        var typeId = reader.ReadUInt16();
        componentType = serializer.GetTypeFromNetworkId(typeId);
        if (componentType is null)
        {
            return null;
        }

        // Look up current component value as baseline
        object? baseline = null;
        if (world is ISnapshotCapability snapshot)
        {
            foreach (var (type, value) in snapshot.GetComponents(entity))
            {
                if (type == componentType)
                {
                    baseline = value;
                    break;
                }
            }
        }

        // Use delta deserialization if we have a baseline and the type supports it
        if (baseline is not null && serializer.SupportsDelta(componentType))
        {
            return serializer.DeserializeDelta(typeId, ref reader, baseline);
        }

        // Fall back to full deserialization
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

    /// <summary>
    /// Reads a hierarchy change message (parent-child relationship).
    /// </summary>
    /// <param name="childNetworkId">The network ID of the child entity.</param>
    /// <param name="parentNetworkId">The network ID of the parent entity (0 for no parent).</param>
    public void ReadHierarchyChange(out uint childNetworkId, out uint parentNetworkId)
    {
        childNetworkId = reader.ReadUInt32();
        parentNetworkId = reader.ReadUInt32();
    }
}
