namespace KeenEyes.Animation.IK;

/// <summary>
/// Specifies the type of joint constraint applied to a bone.
/// </summary>
public enum IKConstraintType
{
    /// <summary>No constraint - full rotation freedom.</summary>
    None,

    /// <summary>Single-axis rotation (knee, elbow).</summary>
    Hinge,

    /// <summary>Cone with twist limit (shoulder, hip).</summary>
    BallSocket,

    /// <summary>Independent per-axis euler limits.</summary>
    Euler
}
