using System.Numerics;

namespace KeenEyes.Spatial;

/// <summary>
/// Represents a view frustum defined by six planes for culling operations.
/// </summary>
/// <remarks>
/// A frustum is typically used for camera-based visibility culling in 3D graphics.
/// The six planes (near, far, left, right, top, bottom) define the volume visible to a camera.
/// </remarks>
/// <param name="Near">The near plane of the frustum.</param>
/// <param name="Far">The far plane of the frustum.</param>
/// <param name="Left">The left plane of the frustum.</param>
/// <param name="Right">The right plane of the frustum.</param>
/// <param name="Top">The top plane of the frustum.</param>
/// <param name="Bottom">The bottom plane of the frustum.</param>
public readonly struct Frustum(Plane Near, Plane Far, Plane Left, Plane Right, Plane Top, Plane Bottom)
{
    /// <summary>
    /// Gets the near plane of the frustum.
    /// </summary>
    public Plane Near { get; } = Near;

    /// <summary>
    /// Gets the far plane of the frustum.
    /// </summary>
    public Plane Far { get; } = Far;

    /// <summary>
    /// Gets the left plane of the frustum.
    /// </summary>
    public Plane Left { get; } = Left;

    /// <summary>
    /// Gets the right plane of the frustum.
    /// </summary>
    public Plane Right { get; } = Right;

    /// <summary>
    /// Gets the top plane of the frustum.
    /// </summary>
    public Plane Top { get; } = Top;

    /// <summary>
    /// Gets the bottom plane of the frustum.
    /// </summary>
    public Plane Bottom { get; } = Bottom;

    /// <summary>
    /// Creates a frustum from a view-projection matrix.
    /// </summary>
    /// <param name="viewProjection">The combined view-projection matrix.</param>
    /// <returns>A frustum extracted from the matrix.</returns>
    /// <remarks>
    /// Extracts the six frustum planes from the view-projection matrix using
    /// the Gribb-Hartmann method. Each plane is normalized for accurate distance tests.
    /// </remarks>
    public static Frustum FromMatrix(Matrix4x4 viewProjection)
    {
        // Extract planes using Gribb-Hartmann method
        // Left plane: add fourth column to first column
        var left = new Plane(
            viewProjection.M14 + viewProjection.M11,
            viewProjection.M24 + viewProjection.M21,
            viewProjection.M34 + viewProjection.M31,
            viewProjection.M44 + viewProjection.M41);

        // Right plane: subtract first column from fourth column
        var right = new Plane(
            viewProjection.M14 - viewProjection.M11,
            viewProjection.M24 - viewProjection.M21,
            viewProjection.M34 - viewProjection.M31,
            viewProjection.M44 - viewProjection.M41);

        // Bottom plane: add third column to second column
        var bottom = new Plane(
            viewProjection.M14 + viewProjection.M12,
            viewProjection.M24 + viewProjection.M22,
            viewProjection.M34 + viewProjection.M32,
            viewProjection.M44 + viewProjection.M42);

        // Top plane: subtract second column from fourth column
        var top = new Plane(
            viewProjection.M14 - viewProjection.M12,
            viewProjection.M24 - viewProjection.M22,
            viewProjection.M34 - viewProjection.M32,
            viewProjection.M44 - viewProjection.M42);

        // Near plane: add fourth column to third column
        var near = new Plane(
            viewProjection.M14 + viewProjection.M13,
            viewProjection.M24 + viewProjection.M23,
            viewProjection.M34 + viewProjection.M33,
            viewProjection.M44 + viewProjection.M43);

        // Far plane: subtract third column from fourth column
        var far = new Plane(
            viewProjection.M14 - viewProjection.M13,
            viewProjection.M24 - viewProjection.M23,
            viewProjection.M34 - viewProjection.M33,
            viewProjection.M44 - viewProjection.M43);

        // Normalize all planes
        left = Plane.Normalize(left);
        right = Plane.Normalize(right);
        bottom = Plane.Normalize(bottom);
        top = Plane.Normalize(top);
        near = Plane.Normalize(near);
        far = Plane.Normalize(far);

        return new Frustum(near, far, left, right, top, bottom);
    }

    /// <summary>
    /// Tests if a point is inside the frustum.
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns><c>true</c> if the point is inside the frustum; otherwise, <c>false</c>.</returns>
    public bool Contains(Vector3 point)
    {
        // Point must be on the positive side of all six planes
        return Plane.DotCoordinate(Near, point) >= 0 &&
               Plane.DotCoordinate(Far, point) >= 0 &&
               Plane.DotCoordinate(Left, point) >= 0 &&
               Plane.DotCoordinate(Right, point) >= 0 &&
               Plane.DotCoordinate(Top, point) >= 0 &&
               Plane.DotCoordinate(Bottom, point) >= 0;
    }

    /// <summary>
    /// Tests if an axis-aligned bounding box intersects or is contained within the frustum.
    /// </summary>
    /// <param name="min">The minimum corner of the AABB.</param>
    /// <param name="max">The maximum corner of the AABB.</param>
    /// <returns><c>true</c> if the AABB intersects the frustum; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// Uses the "p-vertex/n-vertex" test for efficient AABB-frustum intersection.
    /// For each plane, we test the corner of the box that is furthest along the plane's normal.
    /// If that corner is outside, the entire box is outside.
    /// </remarks>
    public bool Intersects(Vector3 min, Vector3 max)
    {
        return IntersectsPlane(Near, min, max) &&
               IntersectsPlane(Far, min, max) &&
               IntersectsPlane(Left, min, max) &&
               IntersectsPlane(Right, min, max) &&
               IntersectsPlane(Top, min, max) &&
               IntersectsPlane(Bottom, min, max);
    }

    /// <summary>
    /// Tests if a sphere intersects or is contained within the frustum.
    /// </summary>
    /// <param name="center">The center of the sphere.</param>
    /// <param name="radius">The radius of the sphere.</param>
    /// <returns><c>true</c> if the sphere intersects the frustum; otherwise, <c>false</c>.</returns>
    public bool Intersects(Vector3 center, float radius)
    {
        // Sphere must be within radius distance of all six planes
        return Plane.DotCoordinate(Near, center) >= -radius &&
               Plane.DotCoordinate(Far, center) >= -radius &&
               Plane.DotCoordinate(Left, center) >= -radius &&
               Plane.DotCoordinate(Right, center) >= -radius &&
               Plane.DotCoordinate(Top, center) >= -radius &&
               Plane.DotCoordinate(Bottom, center) >= -radius;
    }

    private static bool IntersectsPlane(Plane plane, Vector3 min, Vector3 max)
    {
        // Find the p-vertex (the corner furthest along the plane normal)
        var pVertex = new Vector3(
            plane.Normal.X >= 0 ? max.X : min.X,
            plane.Normal.Y >= 0 ? max.Y : min.Y,
            plane.Normal.Z >= 0 ? max.Z : min.Z);

        // If p-vertex is outside, the entire box is outside
        return Plane.DotCoordinate(plane, pVertex) >= 0;
    }
}
