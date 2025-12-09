using System.Numerics;

using KeenEyes.Common;

namespace KeenEyes.Graphics;

/// <summary>
/// Extension properties for <see cref="Camera"/> component.
/// </summary>
/// <remarks>
/// These extension properties provide computed values based on the camera's data
/// without violating ECS principles by keeping components as pure data.
/// </remarks>
public static class CameraExtensions
{
    /// <summary>
    /// Computes the projection matrix for this camera.
    /// </summary>
    /// <param name="camera">The camera to compute the projection matrix for.</param>
    /// <returns>The projection matrix.</returns>
    public static Matrix4x4 ProjectionMatrix(this in Camera camera)
    {
        return camera.Projection switch
        {
            ProjectionType.Perspective => Matrix4x4.CreatePerspectiveFieldOfView(
                camera.FieldOfView * (MathF.PI / 180f),
                camera.AspectRatio,
                camera.NearPlane,
                camera.FarPlane),
            ProjectionType.Orthographic => Matrix4x4.CreateOrthographic(
                camera.OrthographicSize * 2f * camera.AspectRatio,
                camera.OrthographicSize * 2f,
                camera.NearPlane,
                camera.FarPlane),
            _ => Matrix4x4.Identity
        };
    }

    /// <summary>
    /// Computes the view matrix from a transform.
    /// </summary>
    /// <param name="camera">The camera (unused, but required for extension method syntax).</param>
    /// <param name="transform">The camera's transform.</param>
    /// <returns>The view matrix.</returns>
    public static Matrix4x4 ViewMatrix(this in Camera camera, in Transform3D transform)
    {
        var forward = transform.Forward();
        var up = transform.Up();
        return Matrix4x4.CreateLookAt(transform.Position, transform.Position + forward, up);
    }
}
