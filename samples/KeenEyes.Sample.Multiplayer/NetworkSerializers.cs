using KeenEyes.Common;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Sample.Multiplayer;

/// <summary>
/// Static serialization helpers for network components.
/// Follows ECS principle: components are pure data, logic lives elsewhere.
/// </summary>
/// <remarks>
/// This pattern separates serialization logic from component data, maintaining
/// the ECS principle that components should be pure data structures.
/// The <see cref="GameSerializer"/> uses these helpers to serialize components
/// for network transmission.
/// </remarks>
public static class PositionSerializer
{
    private const float NetworkDeltaEpsilon = 0.001f;

    /// <summary>
    /// Serializes a position to the network stream.
    /// </summary>
    public static void Serialize(ref BitWriter writer, in Position position)
    {
        writer.WriteFloat(position.X);
        writer.WriteFloat(position.Y);
    }

    /// <summary>
    /// Deserializes a position from the network stream.
    /// </summary>
    public static Position Deserialize(ref BitReader reader)
    {
        return new Position
        {
            X = reader.ReadFloat(),
            Y = reader.ReadFloat()
        };
    }

    /// <summary>
    /// Computes a dirty mask indicating which fields changed between current and baseline.
    /// </summary>
    public static uint GetDirtyMask(in Position current, in Position baseline)
    {
        uint mask = 0;
        if (!current.X.ApproximatelyEquals(baseline.X, NetworkDeltaEpsilon))
        {
            mask |= 1;
        }

        if (!current.Y.ApproximatelyEquals(baseline.Y, NetworkDeltaEpsilon))
        {
            mask |= 2;
        }

        return mask;
    }

    /// <summary>
    /// Serializes only the fields that changed (indicated by dirty mask).
    /// </summary>
    public static void SerializeDelta(ref BitWriter writer, in Position position, in Position baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(position.X);
        }

        if ((dirtyMask & 2) != 0)
        {
            writer.WriteFloat(position.Y);
        }
    }

    /// <summary>
    /// Deserializes only the fields indicated by the dirty mask, updating the position in place.
    /// </summary>
    public static void DeserializeDelta(ref BitReader reader, ref Position position, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            position.X = reader.ReadFloat();
        }

        if ((dirtyMask & 2) != 0)
        {
            position.Y = reader.ReadFloat();
        }
    }
}

/// <summary>
/// Static serialization helpers for the Velocity component.
/// </summary>
public static class VelocitySerializer
{
    /// <summary>
    /// Serializes a velocity to the network stream.
    /// </summary>
    public static void Serialize(ref BitWriter writer, in Velocity velocity)
    {
        writer.WriteFloat(velocity.X);
        writer.WriteFloat(velocity.Y);
    }

    /// <summary>
    /// Deserializes a velocity from the network stream.
    /// </summary>
    public static Velocity Deserialize(ref BitReader reader)
    {
        return new Velocity
        {
            X = reader.ReadFloat(),
            Y = reader.ReadFloat()
        };
    }
}

/// <summary>
/// Static serialization helpers for the PlayerInput struct.
/// </summary>
public static class PlayerInputSerializer
{
    /// <summary>
    /// Serializes a player input to the network stream.
    /// </summary>
    public static void Serialize(ref BitWriter writer, in PlayerInput input)
    {
        writer.WriteUInt32(input.Tick);
        writer.WriteFloat(input.MoveX);
        writer.WriteFloat(input.MoveY);
    }

    /// <summary>
    /// Deserializes a player input from the network stream.
    /// </summary>
    public static PlayerInput Deserialize(ref BitReader reader)
    {
        return new PlayerInput
        {
            Tick = reader.ReadUInt32(),
            MoveX = reader.ReadFloat(),
            MoveY = reader.ReadFloat()
        };
    }
}
