using System.Numerics;

namespace KeenEyes.Common;

/// <summary>
/// Axis-aligned bounding box (AABB) component for spatial queries and collision detection.
/// </summary>
/// <remarks>
/// <para>
/// This component defines a 3D rectangular volume aligned with the world axes.
/// It's commonly used for broadphase collision detection and spatial partitioning.
/// </para>
/// <para>
/// The bounds are represented as Min (lowest corner) and Max (highest corner) points.
/// All coordinates in Max should be greater than or equal to those in Min.
/// </para>
/// </remarks>
public struct SpatialBounds : IComponent
{
    /// <summary>
    /// The minimum corner of the bounding box (lowest X, Y, Z coordinates).
    /// </summary>
    public Vector3 Min;

    /// <summary>
    /// The maximum corner of the bounding box (highest X, Y, Z coordinates).
    /// </summary>
    public Vector3 Max;

    /// <summary>
    /// Creates a new bounding box with the specified min and max corners.
    /// </summary>
    /// <param name="min">The minimum corner.</param>
    /// <param name="max">The maximum corner.</param>
    public SpatialBounds(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Creates a bounding box centered at a position with the specified extents.
    /// </summary>
    /// <param name="center">The center point of the bounding box.</param>
    /// <param name="extents">Half the size in each dimension (distance from center to face).</param>
    /// <returns>A new bounding box centered at the specified position.</returns>
    public static SpatialBounds FromCenterAndExtents(Vector3 center, Vector3 extents)
    {
        return new SpatialBounds(center - extents, center + extents);
    }

    /// <summary>
    /// Gets the center point of the bounding box.
    /// </summary>
    public readonly Vector3 Center => (Min + Max) * 0.5f;

    /// <summary>
    /// Gets the extents (half-size) of the bounding box in each dimension.
    /// </summary>
    public readonly Vector3 Extents => (Max - Min) * 0.5f;

    /// <summary>
    /// Gets the full size of the bounding box in each dimension.
    /// </summary>
    public readonly Vector3 Size => Max - Min;

    /// <summary>
    /// Checks if a point is contained within this bounding box (inclusive).
    /// </summary>
    /// <param name="point">The point to test.</param>
    /// <returns>True if the point is inside or on the boundary of the bounding box.</returns>
    public readonly bool Contains(Vector3 point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y &&
               point.Z >= Min.Z && point.Z <= Max.Z;
    }

    /// <summary>
    /// Checks if this bounding box intersects with another bounding box.
    /// </summary>
    /// <param name="other">The other bounding box to test.</param>
    /// <returns>True if the bounding boxes overlap.</returns>
    public readonly bool Intersects(SpatialBounds other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
               Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    }

    /// <summary>
    /// Expands the bounding box to include a point.
    /// </summary>
    /// <param name="point">The point to include.</param>
    public void Encapsulate(Vector3 point)
    {
        Min = Vector3.Min(Min, point);
        Max = Vector3.Max(Max, point);
    }

    /// <summary>
    /// Expands the bounding box to include another bounding box.
    /// </summary>
    /// <param name="other">The bounding box to include.</param>
    public void Encapsulate(SpatialBounds other)
    {
        Min = Vector3.Min(Min, other.Min);
        Max = Vector3.Max(Max, other.Max);
    }
}
