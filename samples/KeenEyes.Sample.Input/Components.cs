using System.Numerics;

namespace KeenEyes.Sample.Input;

/// <summary>
/// Tag component to identify the player entity.
/// </summary>
[TagComponent]
public partial struct PlayerTag;

/// <summary>
/// Component storing player movement velocity.
/// </summary>
[Component]
public partial struct PlayerVelocity
{
    /// <summary>
    /// Current velocity in world space.
    /// </summary>
    public Vector3 Velocity;

    /// <summary>
    /// Vertical velocity for jumping.
    /// </summary>
    public float VerticalVelocity;
}
