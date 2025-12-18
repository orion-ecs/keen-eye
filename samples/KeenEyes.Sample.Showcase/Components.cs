using System.Numerics;

namespace KeenEyes.Sample.Showcase;

/// <summary>
/// Component for bouncing ball physics.
/// </summary>
[Component]
public partial struct BallPhysics
{
    /// <summary>
    /// Current velocity of the ball in world space.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Radius of the ball for collision detection.
    /// </summary>
    public float Radius;

    /// <summary>
    /// Bounciness factor (0-1, where 1 is perfectly elastic).
    /// </summary>
    public float Bounciness;
}

/// <summary>
/// Tag component to identify bouncing balls.
/// </summary>
[TagComponent]
public partial struct BallTag;

/// <summary>
/// Tag component to identify the ground plane.
/// </summary>
[TagComponent]
public partial struct GroundTag;

/// <summary>
/// Tag component to identify wall entities.
/// </summary>
[TagComponent]
public partial struct WallTag;

/// <summary>
/// Component for first-person camera control state.
/// </summary>
[Component]
public partial struct CameraController
{
    /// <summary>
    /// Camera yaw angle in radians (horizontal rotation).
    /// </summary>
    public float Yaw;

    /// <summary>
    /// Camera pitch angle in radians (vertical rotation).
    /// </summary>
    public float Pitch;

    /// <summary>
    /// Mouse sensitivity for look controls.
    /// </summary>
    public float Sensitivity;

    /// <summary>
    /// Movement speed in units per second.
    /// </summary>
    public float MoveSpeed;
}

/// <summary>
/// Singleton component for tracking ball spawn state.
/// </summary>
[Component]
public partial struct BallSpawner
{
    /// <summary>
    /// Total number of balls spawned.
    /// </summary>
    public int BallCount;

    /// <summary>
    /// Maximum balls allowed at once.
    /// </summary>
    public int MaxBalls;

    /// <summary>
    /// Reference to the mesh for balls.
    /// </summary>
    public int SphereMeshId;

    /// <summary>
    /// Reference to the shader for balls.
    /// </summary>
    public int ShaderId;

    /// <summary>
    /// Reference to the white texture.
    /// </summary>
    public int TextureId;
}

/// <summary>
/// Component to track spinning animation.
/// </summary>
[Component]
public partial struct Spin
{
    /// <summary>
    /// Rotation speed around each axis in radians per second.
    /// </summary>
    public Vector3 Speed;
}
