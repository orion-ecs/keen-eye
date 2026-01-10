using System.Numerics;

namespace KeenEyes.Sample.PBR;

/// <summary>
/// Component for an orbit camera that rotates around a target point.
/// </summary>
[Component]
public partial struct OrbitCamera
{
    /// <summary>
    /// The point to orbit around.
    /// </summary>
    public Vector3 Target;

    /// <summary>
    /// Distance from the target.
    /// </summary>
    public float Distance;

    /// <summary>
    /// Horizontal angle in radians (yaw).
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Vertical angle in radians (pitch).
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Minimum distance from target.
    /// </summary>
    public float MinDistance;

    /// <summary>
    /// Maximum distance from target.
    /// </summary>
    public float MaxDistance;

    /// <summary>
    /// Minimum pitch angle in radians.
    /// </summary>
    public float MinPitch;

    /// <summary>
    /// Maximum pitch angle in radians.
    /// </summary>
    public float MaxPitch;

    /// <summary>
    /// Rotation sensitivity for mouse movement.
    /// </summary>
    public float RotationSensitivity;

    /// <summary>
    /// Zoom sensitivity for mouse scroll.
    /// </summary>
    public float ZoomSensitivity;

    /// <summary>
    /// Creates a default orbit camera configuration.
    /// </summary>
    public static OrbitCamera Default => new()
    {
        Target = Vector3.Zero,
        Distance = 5f,
        Yaw = 0f,
        Pitch = 0.3f,
        MinDistance = 1f,
        MaxDistance = 20f,
        MinPitch = -MathF.PI / 2f + 0.1f,
        MaxPitch = MathF.PI / 2f - 0.1f,
        RotationSensitivity = 0.005f,
        ZoomSensitivity = 0.5f
    };
}

/// <summary>
/// Component that makes an entity slowly rotate for demonstration.
/// </summary>
[Component]
public partial struct AutoRotate
{
    /// <summary>
    /// Rotation speed in radians per second around the Y axis.
    /// </summary>
    public float Speed;
}

/// <summary>
/// Tag component to identify the main model entity.
/// </summary>
[TagComponent]
public partial struct MainModelTag;

/// <summary>
/// Tag component to identify the ground plane.
/// </summary>
[TagComponent]
public partial struct GroundTag;

/// <summary>
/// Component that makes a point light orbit around a center point.
/// </summary>
[Component]
public partial struct OrbitingLight
{
    /// <summary>
    /// The center point to orbit around.
    /// </summary>
    public Vector3 Center;

    /// <summary>
    /// Radius of the orbit.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Height above the center point.
    /// </summary>
    public float Height;

    /// <summary>
    /// Current angle in radians.
    /// </summary>
    public float Angle;

    /// <summary>
    /// Rotation speed in radians per second.
    /// </summary>
    public float Speed;
}
