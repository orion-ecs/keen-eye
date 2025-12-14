using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Angular velocity in 3D space, expressed as a rotation axis scaled by rotation speed.
/// </summary>
/// <remarks>
/// <para>
/// This component represents the rate of rotation in radians per second.
/// The direction of the vector is the axis of rotation, and its magnitude
/// is the angular speed.
/// </para>
/// <para>
/// Typically used by physics systems to update <see cref="Transform3D"/> rotations.
/// </para>
/// </remarks>
public struct AngularVelocity3D : IComponent
{
    /// <summary>
    /// The angular velocity vector in radians per second.
    /// </summary>
    /// <remarks>
    /// The direction is the rotation axis, the magnitude is the rotation speed.
    /// </remarks>
    public Vector3 Value;

    /// <summary>
    /// Creates a new 3D angular velocity component.
    /// </summary>
    /// <param name="value">The angular velocity vector in radians per second.</param>
    public AngularVelocity3D(Vector3 value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new 3D angular velocity from X, Y, and Z components.
    /// </summary>
    /// <param name="x">The X component of angular velocity (rotation around X axis).</param>
    /// <param name="y">The Y component of angular velocity (rotation around Y axis).</param>
    /// <param name="z">The Z component of angular velocity (rotation around Z axis).</param>
    public AngularVelocity3D(float x, float y, float z)
    {
        Value = new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets an angular velocity of zero (no rotation).
    /// </summary>
    public static AngularVelocity3D Zero => new(Vector3.Zero);
}
