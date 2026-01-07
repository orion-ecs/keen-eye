using KeenEyes.Network;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Sample.Multiplayer;

/// <summary>
/// Network serializer for the multiplayer sample.
/// Registers all game components for network replication.
/// </summary>
/// <remarks>
/// Uses static serialization helpers from <see cref="PositionSerializer"/>,
/// <see cref="VelocitySerializer"/>, etc. to maintain ECS principle that
/// components are pure data.
/// </remarks>
public sealed class GameSerializer : INetworkSerializer
{
    private readonly Dictionary<Type, ushort> typeToId = new()
    {
        [typeof(Position)] = 1,
        [typeof(Velocity)] = 2,
    };

    private readonly Dictionary<ushort, Type> idToType = new()
    {
        [1] = typeof(Position),
        [2] = typeof(Velocity),
    };

    public bool IsNetworkSerializable(Type type) => typeToId.ContainsKey(type);

    public ushort? GetNetworkTypeId(Type type) =>
        typeToId.TryGetValue(type, out var id) ? id : null;

    public Type? GetTypeFromNetworkId(ushort networkTypeId) =>
        idToType.TryGetValue(networkTypeId, out var type) ? type : null;

    public bool Serialize(Type type, object value, ref BitWriter writer)
    {
        if (!typeToId.ContainsKey(type))
        {
            return false;
        }

        if (type == typeof(Position))
        {
            PositionSerializer.Serialize(ref writer, (Position)value);
            return true;
        }

        if (type == typeof(Velocity))
        {
            VelocitySerializer.Serialize(ref writer, (Velocity)value);
            return true;
        }

        return false;
    }

    public object? Deserialize(ushort networkTypeId, ref BitReader reader)
    {
        switch (networkTypeId)
        {
            case 1:
                return PositionSerializer.Deserialize(ref reader);
            case 2:
                return VelocitySerializer.Deserialize(ref reader);
            default:
                return null;
        }
    }

    public IEnumerable<Type> GetRegisteredTypes() => typeToId.Keys;

    public IEnumerable<NetworkComponentInfo> GetRegisteredComponentInfo()
    {
        yield return new NetworkComponentInfo
        {
            Type = typeof(Position),
            NetworkTypeId = 1,
            Strategy = SyncStrategy.Predicted,
            Frequency = 0,
            Priority = 255,
            SupportsInterpolation = true,
            SupportsPrediction = true,
            SupportsDelta = true,
        };
        yield return new NetworkComponentInfo
        {
            Type = typeof(Velocity),
            NetworkTypeId = 2,
            Strategy = SyncStrategy.Authoritative,
            Frequency = 0,
            Priority = 128,
            SupportsInterpolation = false,
            SupportsPrediction = false,
            SupportsDelta = false,
        };
    }

    public bool SupportsDelta(Type type) => type == typeof(Position);

    public uint GetDirtyMask(Type type, object current, object baseline)
    {
        if (type == typeof(Position))
        {
            return PositionSerializer.GetDirtyMask((Position)current, (Position)baseline);
        }

        return 0;
    }

    public bool SerializeDelta(Type type, object current, object baseline, ref BitWriter writer)
    {
        if (type == typeof(Position))
        {
            var c = (Position)current;
            var b = (Position)baseline;
            var mask = PositionSerializer.GetDirtyMask(c, b);
            writer.WriteUInt32(mask);
            if (mask != 0)
            {
                PositionSerializer.SerializeDelta(ref writer, c, b, mask);
            }

            return true;
        }

        return false;
    }

    public object? DeserializeDelta(ushort networkTypeId, ref BitReader reader, object baseline)
    {
        var mask = reader.ReadUInt32();
        if (mask == 0)
        {
            return baseline;
        }

        if (networkTypeId == 1)
        {
            var b = (Position)baseline;
            PositionSerializer.DeserializeDelta(ref reader, ref b, mask);
            return b;
        }

        return baseline;
    }
}
