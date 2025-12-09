using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Extension properties for <see cref="SpatialBounds"/> component.
/// </summary>
/// <remarks>
/// These extension properties provide computed values and operations based on the bounds' data
/// without violating ECS principles by keeping components as pure data.
/// </remarks>
public static class SpatialBoundsExtensions
{
    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    /// <param name="bounds">The bounds to get the center from.</param>
    /// <returns>The center point.</returns>
    public static Vector3 Center(this in SpatialBounds bounds)
    {
        return (bounds.Min + bounds.Max) * 0.5f;
    }

    /// <summary>
    /// Gets the extents (half-size) of the bounding box in each dimension.
    /// </summary>
    /// <param name="bounds">The bounds to get the extents from.</param>
    /// <returns>The extents (half-size).</returns>
    public static Vector3 Extents(this in SpatialBounds bounds)
    {
        return (bounds.Max - bounds.Min) * 0.5f;
    }

    /// <summary>
    /// Gets the full size of the bounding box in each dimension.
    /// </summary>
    /// <param name="bounds">The bounds to get the size from.</param>
    /// <returns>The full size.</returns>
    public static Vector3 Size(this in SpatialBounds bounds)
    {
        return bounds.Max - bounds.Min;
    }

    /// <summary>
    /// Checks if a point is contained within the bounding box (inclusive).
    /// </summary>
    /// <param name="bounds">The bounds to test containment against.</param>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is inside or on the boundary of the bounding box.</returns>
    public static bool Contains(this in SpatialBounds bounds, Vector3 point)
    {
        return point.X >= bounds.Min.X && point.X <= bounds.Max.X &&
               point.Y >= bounds.Min.Y && point.Y <= bounds.Max.Y &&
               point.Z >= bounds.Min.Z && point.Z <= bounds.Max.Z;
    }

    /// <summary>
    /// Checks if the bounding box intersects with another bounding box.
    /// </summary>
    /// <param name="bounds">The first bounding box.</param>
    /// <param name="other">The other bounding box to test.</param>
    /// <returns>True if the bounding boxes overlap.</returns>
    public static bool Intersects(this in SpatialBounds bounds, in SpatialBounds other)
    {
        return bounds.Min.X <= other.Max.X && bounds.Max.X >= other.Min.X &&
               bounds.Min.Y <= other.Max.Y && bounds.Max.Y >= other.Min.Y &&
               bounds.Min.Z <= other.Max.Z && bounds.Max.Z >= other.Min.Z;
    }

    /// <summary>
    /// Expands the bounding box to include a point.
    /// </summary>
    /// <param name="bounds">The bounds to expand.</param>
    /// <param name="point">The point to include.</param>
    /// <returns>The expanded bounding box.</returns>
    public static SpatialBounds Encapsulate(this SpatialBounds bounds, Vector3 point)
    {
        return new SpatialBounds(
            Vector3.Min(bounds.Min, point),
            Vector3.Max(bounds.Max, point)
        );
    }

    /// <summary>
    /// Expands the bounding box to include another bounding box.
    /// </summary>
    /// <param name="bounds">The bounds to expand.</param>
    /// <param name="other">The bounding box to include.</param>
    /// <returns>The expanded bounding box.</returns>
    public static SpatialBounds Encapsulate(this SpatialBounds bounds, in SpatialBounds other)
    {
        return new SpatialBounds(
            Vector3.Min(bounds.Min, other.Min),
            Vector3.Max(bounds.Max, other.Max)
        );
    }
}
