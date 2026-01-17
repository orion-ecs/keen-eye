using System.Numerics;

using KeenEyes.Animation.IK;

namespace KeenEyes.Animation.Components;

/// <summary>
/// Joint rotation constraints for a bone in an IK chain.
/// </summary>
[Component]
public partial struct IKConstraint
{
    /// <summary>Type of joint constraint.</summary>
    public IKConstraintType ConstraintType;

    /// <summary>Primary rotation axis for hinge constraints.</summary>
    public Vector3 Axis;

    /// <summary>Minimum euler angles in degrees.</summary>
    public Vector3 MinAngles;

    /// <summary>Maximum euler angles in degrees.</summary>
    public Vector3 MaxAngles;

    /// <summary>Cone angle for ball-socket constraints (degrees).</summary>
    public float ConeAngle;

    /// <summary>Maximum twist around primary axis (degrees).</summary>
    public float TwistLimit;

    /// <summary>Default unconstrained configuration.</summary>
    public static IKConstraint Default => new()
    {
        ConstraintType = IKConstraintType.None,
        Axis = Vector3.UnitX,
        MinAngles = new Vector3(-180f, -180f, -180f),
        MaxAngles = new Vector3(180f, 180f, 180f),
        ConeAngle = 180f,
        TwistLimit = 180f
    };

    /// <summary>Creates a hinge constraint (single-axis rotation like knee/elbow).</summary>
    public static IKConstraint Hinge(Vector3 axis, float minAngle, float maxAngle) => Default with
    {
        ConstraintType = IKConstraintType.Hinge,
        Axis = Vector3.Normalize(axis),
        MinAngles = new Vector3(minAngle, 0f, 0f),
        MaxAngles = new Vector3(maxAngle, 0f, 0f)
    };

    /// <summary>Creates a ball-socket constraint (cone + twist like shoulder/hip).</summary>
    public static IKConstraint BallSocket(float coneAngle, float twistLimit = 180f) => Default with
    {
        ConstraintType = IKConstraintType.BallSocket,
        ConeAngle = coneAngle,
        TwistLimit = twistLimit
    };

    /// <summary>Creates euler angle constraints (independent per-axis limits).</summary>
    public static IKConstraint Euler(Vector3 minAngles, Vector3 maxAngles) => Default with
    {
        ConstraintType = IKConstraintType.Euler,
        MinAngles = minAngles,
        MaxAngles = maxAngles
    };
}
