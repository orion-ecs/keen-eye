using System.Numerics;

namespace KeenEyes.Sample.CollisionDetection;

/// <summary>
/// Component representing entity velocity.
/// </summary>
[Component]
public partial struct Velocity
{
    /// <summary>Velocity vector in world units per second.</summary>
    public Vector3 Value;
}

/// <summary>
/// Component representing collision radius.
/// </summary>
[Component]
public partial struct CollisionRadius
{
    /// <summary>Collision radius in world units.</summary>
    public float Value;
}
