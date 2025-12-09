namespace KeenEyes.Common;

/// <summary>
/// Extension properties for <see cref="Velocity3D"/> component.
/// </summary>
/// <remarks>
/// These extension properties provide computed values based on the velocity's data
/// without violating ECS principles by keeping components as pure data.
/// </remarks>
public static class Velocity3DExtensions
{
    /// <summary>
    /// Gets the magnitude (speed) of the velocity vector.
    /// </summary>
    /// <param name="velocity">The velocity to get the magnitude from.</param>
    /// <returns>The magnitude (speed).</returns>
    public static float Magnitude(this in Velocity3D velocity)
    {
        return velocity.Value.Length();
    }

    /// <summary>
    /// Gets the squared magnitude (avoids square root calculation).
    /// </summary>
    /// <param name="velocity">The velocity to get the squared magnitude from.</param>
    /// <returns>The squared magnitude.</returns>
    /// <remarks>
    /// Useful for performance-sensitive comparisons where relative magnitude is sufficient.
    /// </remarks>
    public static float MagnitudeSquared(this in Velocity3D velocity)
    {
        return velocity.Value.LengthSquared();
    }
}
