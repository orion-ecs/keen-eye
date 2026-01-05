using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Represents navigation mesh data for pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// A navigation mesh (navmesh) is a data structure that describes the walkable
/// surfaces in a game environment. It consists of connected convex polygons
/// that agents can traverse.
/// </para>
/// <para>
/// Implementations should support serialization for efficient loading and
/// runtime modification for dynamic environments.
/// </para>
/// </remarks>
public interface INavigationMesh
{
    /// <summary>
    /// Gets the unique identifier for this navigation mesh.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the axis-aligned bounding box encompassing all navmesh geometry.
    /// </summary>
    (Vector3 Min, Vector3 Max) Bounds { get; }

    /// <summary>
    /// Gets the number of polygons in the navigation mesh.
    /// </summary>
    int PolygonCount { get; }

    /// <summary>
    /// Gets the total number of vertices in the navigation mesh.
    /// </summary>
    int VertexCount { get; }

    /// <summary>
    /// Gets the agent settings this navmesh was built for.
    /// </summary>
    /// <remarks>
    /// Agents with significantly different settings may not navigate correctly.
    /// </remarks>
    AgentSettings BuiltForAgent { get; }

    /// <summary>
    /// Finds the nearest point on the navmesh to the specified position.
    /// </summary>
    /// <param name="position">The query position.</param>
    /// <param name="searchRadius">Maximum distance to search for a valid point.</param>
    /// <returns>
    /// The nearest point on the navmesh, or null if no point is within the search radius.
    /// </returns>
    NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f);

    /// <summary>
    /// Finds a random point on the navmesh.
    /// </summary>
    /// <param name="areaMask">Optional area mask to filter valid areas.</param>
    /// <returns>A random point on the navmesh, or null if no valid points exist.</returns>
    NavPoint? GetRandomPoint(NavAreaMask areaMask = NavAreaMask.All);

    /// <summary>
    /// Finds a random point on the navmesh within a sphere.
    /// </summary>
    /// <param name="center">The center of the search sphere.</param>
    /// <param name="radius">The radius of the search sphere.</param>
    /// <param name="areaMask">Optional area mask to filter valid areas.</param>
    /// <returns>
    /// A random point within the specified sphere, or null if no valid points exist.
    /// </returns>
    NavPoint? GetRandomPointInRadius(Vector3 center, float radius, NavAreaMask areaMask = NavAreaMask.All);

    /// <summary>
    /// Gets the area type at the specified position.
    /// </summary>
    /// <param name="position">The position to query.</param>
    /// <returns>
    /// The area type at the position, or <see cref="NavAreaType.NotWalkable"/>
    /// if the position is not on the navmesh.
    /// </returns>
    NavAreaType GetAreaType(Vector3 position);

    /// <summary>
    /// Checks whether a position is on the navigation mesh.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="tolerance">Vertical tolerance for the check.</param>
    /// <returns>True if the position is on the navmesh within tolerance.</returns>
    bool IsOnNavMesh(Vector3 position, float tolerance = 0.5f);

    /// <summary>
    /// Serializes the navigation mesh to a byte array.
    /// </summary>
    /// <returns>The serialized navmesh data.</returns>
    byte[] Serialize();

    /// <summary>
    /// Gets the vertices of a specific polygon.
    /// </summary>
    /// <param name="polygonId">The polygon identifier.</param>
    /// <returns>The vertices of the polygon, or an empty span if invalid.</returns>
    ReadOnlySpan<Vector3> GetPolygonVertices(uint polygonId);
}
