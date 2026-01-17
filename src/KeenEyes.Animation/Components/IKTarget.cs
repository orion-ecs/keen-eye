using System.Numerics;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Target position and rotation for an IK chain end effector.
/// </summary>
[Component]
public partial struct IKTarget
{
    /// <summary>World-space target position.</summary>
    public Vector3 Position;

    /// <summary>Target rotation for end effector orientation.</summary>
    public Quaternion Rotation;

    /// <summary>Whether to apply rotation constraint to end effector.</summary>
    public bool UseRotation;

    /// <summary>Blend weight for this target (0-1).</summary>
    public float Weight;

    /// <summary>Entity ID for pole vector control (-1 if none).</summary>
    public int PoleTargetEntityId;

    /// <summary>Default IKTarget configuration.</summary>
    public static IKTarget Default => new()
    {
        Position = Vector3.Zero,
        Rotation = Quaternion.Identity,
        UseRotation = false,
        Weight = 1f,
        PoleTargetEntityId = -1
    };

    /// <summary>Creates a position-only target.</summary>
    public static IKTarget AtPosition(Vector3 position) => Default with { Position = position };

    /// <summary>Creates a target with position and rotation.</summary>
    public static IKTarget WithRotation(Vector3 position, Quaternion rotation) => Default with
    {
        Position = position,
        Rotation = rotation,
        UseRotation = true
    };

    /// <summary>Creates a target with pole vector control.</summary>
    public static IKTarget WithPole(Vector3 position, int poleEntityId) => Default with
    {
        Position = position,
        PoleTargetEntityId = poleEntityId
    };
}
