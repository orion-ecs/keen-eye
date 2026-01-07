using KeenEyes;
using KeenEyes.Network.Prediction;

namespace KeenEyes.Sample.Multiplayer;

// =============================================================================
// Shared Components for Networked Entities
// =============================================================================
// Following ECS principles: components are pure data structures.
// Serialization logic is in NetworkSerializers.cs, used by GameSerializer.cs.
// =============================================================================

/// <summary>
/// Position component for networked entities.
/// </summary>
/// <remarks>
/// Supports delta serialization for bandwidth efficiency via
/// <see cref="PositionSerializer"/>.
/// </remarks>
[Component]
public partial struct Position
{
    public float X;
    public float Y;

    public override readonly string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// Velocity component for networked entities.
/// </summary>
/// <remarks>
/// Serialization is handled by <see cref="VelocitySerializer"/>.
/// </remarks>
[Component]
public partial struct Velocity
{
    public float X;
    public float Y;

    public override readonly string ToString() => $"({X:F1}, {Y:F1})";
}

/// <summary>
/// Player input for client-side prediction.
/// </summary>
/// <remarks>
/// Implements <see cref="INetworkInput"/> for the prediction system.
/// Serialization is handled by <see cref="PlayerInputSerializer"/>.
/// </remarks>
public struct PlayerInput : INetworkInput
{
    public uint Tick { get; set; }
    public float MoveX;
    public float MoveY;
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
