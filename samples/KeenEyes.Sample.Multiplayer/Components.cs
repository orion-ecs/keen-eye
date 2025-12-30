using KeenEyes;
using KeenEyes.Network.Prediction;
using KeenEyes.Network.Serialization;

namespace KeenEyes.Sample.Multiplayer;

// =============================================================================
// Shared Components for Networked Entities
// =============================================================================

/// <summary>
/// Position component for networked entities.
/// Supports delta serialization for bandwidth efficiency.
/// </summary>
[Component]
public partial struct Position : INetworkSerializable, INetworkDeltaSerializable<Position>
{
    public float X;
    public float Y;

    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteFloat(X);
        writer.WriteFloat(Y);
    }

    public void NetworkDeserialize(ref BitReader reader)
    {
        X = reader.ReadFloat();
        Y = reader.ReadFloat();
    }

    public readonly uint GetDirtyMask(in Position baseline)
    {
        uint mask = 0;
        if (MathF.Abs(X - baseline.X) > 0.001f)
        {
            mask |= 1;
        }

        if (MathF.Abs(Y - baseline.Y) > 0.001f)
        {
            mask |= 2;
        }

        return mask;
    }

    public readonly void NetworkSerializeDelta(ref BitWriter writer, in Position baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            writer.WriteFloat(X);
        }

        if ((dirtyMask & 2) != 0)
        {
            writer.WriteFloat(Y);
        }
    }

    public readonly void NetworkDeserializeDelta(ref BitReader reader, ref Position baseline, uint dirtyMask)
    {
        if ((dirtyMask & 1) != 0)
        {
            baseline.X = reader.ReadFloat();
        }

        if ((dirtyMask & 2) != 0)
        {
            baseline.Y = reader.ReadFloat();
        }
    }

    public override readonly string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// Velocity component for networked entities.
/// </summary>
[Component]
public partial struct Velocity : INetworkSerializable
{
    public float X;
    public float Y;

    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteFloat(X);
        writer.WriteFloat(Y);
    }

    public void NetworkDeserialize(ref BitReader reader)
    {
        X = reader.ReadFloat();
        Y = reader.ReadFloat();
    }

    public override readonly string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// Player input for client-side prediction.
/// </summary>
public struct PlayerInput : INetworkInput
{
    public uint Tick { get; set; }
    public float MoveX;
    public float MoveY;

    public readonly void NetworkSerialize(ref BitWriter writer)
    {
        writer.WriteUInt32(Tick);
        writer.WriteFloat(MoveX);
        writer.WriteFloat(MoveY);
    }

    public void NetworkDeserialize(ref BitReader reader)
    {
        Tick = reader.ReadUInt32();
        MoveX = reader.ReadFloat();
        MoveY = reader.ReadFloat();
    }
}

/// <summary>
/// Tag for locally controlled player.
/// </summary>
[TagComponent]
public partial struct LocalPlayer;

/// <summary>
/// Tag for remote players.
/// </summary>
[TagComponent]
public partial struct RemotePlayer;

/// <summary>
/// Player display name.
/// </summary>
[Component]
public partial struct PlayerName
{
    public string Name;
}
