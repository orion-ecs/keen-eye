using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Represents a point on a navigation mesh or grid.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NavPoint"/> combines a 3D position with metadata about
/// the navigation surface at that location. Use this to represent
/// waypoints in computed paths.
/// </para>
/// </remarks>
/// <param name="Position">The 3D world-space position of the point.</param>
/// <param name="AreaType">The navigation area type at this point.</param>
/// <param name="PolygonId">
/// Optional identifier of the navmesh polygon containing this point.
/// Zero if not applicable (e.g., for grid-based navigation).
/// </param>
public readonly record struct NavPoint(
    Vector3 Position,
    NavAreaType AreaType = NavAreaType.Walkable,
    uint PolygonId = 0)
{
    /// <summary>
    /// Creates a NavPoint at the specified position with default area type.
    /// </summary>
    /// <param name="position">The 3D position.</param>
    /// <returns>A new NavPoint at the specified position.</returns>
    public static NavPoint At(Vector3 position) => new(position);

    /// <summary>
    /// Creates a NavPoint at the specified coordinates with default area type.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <returns>A new NavPoint at the specified coordinates.</returns>
    public static NavPoint At(float x, float y, float z) => new(new Vector3(x, y, z));

    /// <summary>
    /// Calculates the distance to another NavPoint.
    /// </summary>
    /// <param name="other">The other NavPoint.</param>
    /// <returns>The Euclidean distance between the two points.</returns>
    public float DistanceTo(NavPoint other) => Vector3.Distance(Position, other.Position);

    /// <summary>
    /// Calculates the squared distance to another NavPoint.
    /// </summary>
    /// <param name="other">The other NavPoint.</param>
    /// <returns>The squared distance (avoids square root calculation).</returns>
    public float DistanceSquaredTo(NavPoint other) => Vector3.DistanceSquared(Position, other.Position);
}
