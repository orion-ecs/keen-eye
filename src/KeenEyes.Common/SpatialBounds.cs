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
}
