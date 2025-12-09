using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Linear velocity in 3D space.
/// </summary>
/// <remarks>
/// This component represents the rate of change of position per second.
/// Typically used by movement systems to update <see cref="Transform3D"/> positions.
/// </remarks>
public struct Velocity3D : IComponent
{
    /// <summary>
    /// The velocity vector in units per second.
    /// </summary>
    public Vector3 Value;

    /// <summary>
    /// Creates a new 3D velocity component.
    /// </summary>
    /// <param name="value">The velocity vector in units per second.</param>
    public Velocity3D(Vector3 value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new 3D velocity from X, Y, and Z components.
    /// </summary>
    /// <param name="x">The X component of velocity.</param>
    /// <param name="y">The Y component of velocity.</param>
    /// <param name="z">The Z component of velocity.</param>
    public Velocity3D(float x, float y, float z)
    {
        Value = new Vector3(x, y, z);
    }

    /// <summary>
    /// Gets a velocity of zero (no movement).
    /// </summary>
    public static Velocity3D Zero => new(Vector3.Zero);
}
