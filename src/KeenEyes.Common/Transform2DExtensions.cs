using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Extension properties for <see cref="Transform2D"/> component.
/// </summary>
/// <remarks>
/// These extension properties provide computed values based on the transform's data
/// without violating ECS principles by keeping components as pure data.
/// </remarks>
public static class Transform2DExtensions
{
    /// <summary>
    /// Computes the 3x2 transformation matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <param name="transform">The transform to compute the matrix for.</param>
    /// <returns>The world transformation matrix.</returns>
    public static Matrix3x2 Matrix(this in Transform2D transform)
    {
        return Matrix3x2.CreateScale(transform.Scale) *
               Matrix3x2.CreateRotation(transform.Rotation) *
               Matrix3x2.CreateTranslation(transform.Position);
    }

    /// <summary>
    /// Gets the forward direction vector (unit vector in the direction of rotation).
    /// </summary>
    /// <param name="transform">The transform to get the forward direction from.</param>
    /// <returns>The forward direction vector.</returns>
    public static Vector2 Forward(this in Transform2D transform)
    {
        return new Vector2((float)Math.Cos(transform.Rotation), (float)Math.Sin(transform.Rotation));
    }

    /// <summary>
    /// Gets the right direction vector (perpendicular to forward, 90 degrees clockwise).
    /// </summary>
    /// <param name="transform">The transform to get the right direction from.</param>
    /// <returns>The right direction vector.</returns>
    public static Vector2 Right(this in Transform2D transform)
    {
        return new Vector2((float)Math.Sin(transform.Rotation), -(float)Math.Cos(transform.Rotation));
    }
}
