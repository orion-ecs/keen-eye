using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Linear velocity in 2D space.
/// </summary>
/// <remarks>
/// This component represents the rate of change of position per second.
/// Typically used by movement systems to update <see cref="Transform2D"/> positions.
/// </remarks>
public struct Velocity2D : IComponent
{
    /// <summary>
    /// The velocity vector in units per second.
    /// </summary>
    public Vector2 Value;

    /// <summary>
    /// Creates a new 2D velocity component.
    /// </summary>
    /// <param name="value">The velocity vector in units per second.</param>
    public Velocity2D(Vector2 value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a new 2D velocity from X and Y components.
    /// </summary>
    /// <param name="x">The X component of velocity.</param>
    /// <param name="y">The Y component of velocity.</param>
    public Velocity2D(float x, float y)
    {
        Value = new Vector2(x, y);
    }

    /// <summary>
    /// Gets a velocity of zero (no movement).
    /// </summary>
    public static Velocity2D Zero => new(Vector2.Zero);
}
