using System.Numerics;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Look-at/aim constraint for head and eye tracking.
/// </summary>
[Component]
public partial struct LookAtTarget
{
    /// <summary>Entity ID to track (-1 to use WorldTarget position).</summary>
    public int TargetEntityId;

    /// <summary>World position to look at (used when TargetEntityId is -1).</summary>
    public Vector3 WorldTarget;

    /// <summary>Local forward axis of the bone.</summary>
    public Vector3 ForwardAxis;

    /// <summary>Local up axis of the bone.</summary>
    public Vector3 UpAxis;

    /// <summary>Maximum rotation angle from rest pose (degrees).</summary>
    public float MaxAngle;

    /// <summary>Blend weight (0-1).</summary>
    public float Weight;

    /// <summary>Smoothing factor (0=instant, higher=slower response).</summary>
    public float Smoothing;

    /// <summary>Current smoothed rotation (system-managed).</summary>
    [BuilderIgnore]
    public Quaternion CurrentRotation;

    /// <summary>Default look-at configuration.</summary>
    public static LookAtTarget Default => new()
    {
        TargetEntityId = -1,
        WorldTarget = Vector3.Zero,
        ForwardAxis = Vector3.UnitZ,
        UpAxis = Vector3.UnitY,
        MaxAngle = 90f,
        Weight = 1f,
        Smoothing = 5f,
        CurrentRotation = Quaternion.Identity
    };

    /// <summary>Creates a look-at targeting an entity.</summary>
    public static LookAtTarget AtEntity(int entityId) => Default with { TargetEntityId = entityId };

    /// <summary>Creates a look-at targeting a world position.</summary>
    public static LookAtTarget AtPosition(Vector3 position) => Default with { WorldTarget = position };
}
