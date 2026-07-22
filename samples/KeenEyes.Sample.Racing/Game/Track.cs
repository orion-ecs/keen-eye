using System;
using System.Numerics;

namespace KeenEyes.Sample.Racing;

/// <summary>
/// A simple circular racing line in the XZ plane.
/// </summary>
/// <remarks>
/// <para>
/// The track is fully parametric: given a distance travelled from the
/// start/finish line, it returns the world-space position on the racing line and
/// the heading (yaw) tangent to the line at that point. This keeps the sample
/// deterministic and headless-friendly - there is no physics or collision, just
/// a car sliding along a known curve.
/// </para>
/// <para>
/// Because arc length along a circle is simply <c>radius * angle</c>, distance
/// maps linearly to angle, which makes distance-synced ghost comparison exact.
/// </para>
/// </remarks>
public sealed class Track
{
    /// <summary>
    /// Initializes a new circular track with the given radius, centered at the origin.
    /// </summary>
    /// <param name="radius">Radius of the circular racing line, in world units.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when radius is not positive.</exception>
    public Track(float radius)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);
        Radius = radius;
        Length = 2f * MathF.PI * radius;
    }

    /// <summary>
    /// Gets the radius of the circular racing line, in world units.
    /// </summary>
    public float Radius { get; }

    /// <summary>
    /// Gets the total length of one lap, in world units.
    /// </summary>
    public float Length { get; }

    /// <summary>
    /// Gets the world-space position on the racing line at the given distance.
    /// </summary>
    /// <param name="distance">Distance travelled from the start/finish line, in world units.</param>
    /// <returns>The position on the racing line.</returns>
    public Vector3 PositionAt(float distance)
    {
        var angle = distance / Radius;
        return new Vector3(Radius * MathF.Cos(angle), 0f, Radius * MathF.Sin(angle));
    }

    /// <summary>
    /// Gets the heading (yaw, in radians) tangent to the racing line at the given distance.
    /// </summary>
    /// <param name="distance">Distance travelled from the start/finish line, in world units.</param>
    /// <returns>The heading angle in radians.</returns>
    public float HeadingAt(float distance)
    {
        // The tangent of a circle leads the radial angle by 90 degrees.
        var angle = distance / Radius;
        return angle + MathF.PI / 2f;
    }
}
