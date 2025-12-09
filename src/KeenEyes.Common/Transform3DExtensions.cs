using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Extension properties for <see cref="Transform3D"/> component.
/// </summary>
/// <remarks>
/// These extension properties provide computed values based on the transform's data
/// without violating ECS principles by keeping components as pure data.
/// </remarks>
public static class Transform3DExtensions
{
    /// <summary>
    /// Computes the 4x4 transformation matrix (Scale * Rotation * Translation).
    /// </summary>
    /// <param name="transform">The transform to compute the matrix for.</param>
    /// <returns>The world transformation matrix.</returns>
    public static Matrix4x4 Matrix(this in Transform3D transform)
    {
        return Matrix4x4.CreateScale(transform.Scale) *
               Matrix4x4.CreateFromQuaternion(transform.Rotation) *
               Matrix4x4.CreateTranslation(transform.Position);
    }

    /// <summary>
    /// Gets the forward direction vector (negative Z-axis after rotation).
    /// </summary>
    /// <param name="transform">The transform to get the forward direction from.</param>
    /// <returns>The forward direction vector.</returns>
    public static Vector3 Forward(this in Transform3D transform)
    {
        return Vector3.Transform(-Vector3.UnitZ, transform.Rotation);
    }

    /// <summary>
    /// Gets the right direction vector (positive X-axis after rotation).
    /// </summary>
    /// <param name="transform">The transform to get the right direction from.</param>
    /// <returns>The right direction vector.</returns>
    public static Vector3 Right(this in Transform3D transform)
    {
        return Vector3.Transform(Vector3.UnitX, transform.Rotation);
    }

    /// <summary>
    /// Gets the up direction vector (positive Y-axis after rotation).
    /// </summary>
    /// <param name="transform">The transform to get the up direction from.</param>
    /// <returns>The up direction vector.</returns>
    public static Vector3 Up(this in Transform3D transform)
    {
        return Vector3.Transform(Vector3.UnitY, transform.Rotation);
    }
}
