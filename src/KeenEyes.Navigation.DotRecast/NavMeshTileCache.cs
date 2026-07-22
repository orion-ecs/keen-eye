using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Detour.TileCache;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// A navigation mesh backed by a Detour tile cache, supporting obstacle-driven
/// partial rebuilds of individual tiles at runtime.
/// </summary>
/// <remarks>
/// <para>
/// Built via <see cref="DotRecastMeshBuilder.BuildTileCache"/>. Unlike
/// <see cref="NavMeshObstacleManager"/>, which only filters polygons during
/// queries, obstacles added here carve the navmesh: affected tiles are
/// re-contoured from their compressed heightfield layers so the walkable
/// surface itself changes.
/// </para>
/// <para>
/// Obstacle additions and removals are deferred requests. Call
/// <see cref="Update"/> once per frame; each call rebuilds at most one affected
/// tile, spreading the cost over frames to avoid hitches. All members must be
/// called from the thread that owns the navigation mesh (typically the main
/// update thread).
/// </para>
/// </remarks>
public sealed class NavMeshTileCache
{
    private readonly DtTileCache tileCache;
    private readonly NavMeshData mesh;

    internal NavMeshTileCache(DtTileCache tileCache, NavMeshData mesh)
    {
        this.tileCache = tileCache;
        this.mesh = mesh;
    }

    /// <summary>
    /// Gets the navigation mesh maintained by the tile cache. Install it on a
    /// provider via <see cref="DotRecastProvider.SetNavMesh"/>; tile rebuilds
    /// mutate it in place, so it never needs to be re-set.
    /// </summary>
    public NavMeshData Mesh => mesh;

    /// <summary>
    /// Requests a cylindrical obstacle to be carved out of the navmesh.
    /// </summary>
    /// <param name="position">The center of the cylinder's base.</param>
    /// <param name="radius">The cylinder radius.</param>
    /// <param name="height">The cylinder height.</param>
    /// <returns>An obstacle reference for later removal.</returns>
    /// <remarks>The carve is applied by subsequent <see cref="Update"/> calls.</remarks>
    public long AddCylinderObstacle(Vector3 position, float radius, float height)
    {
        return tileCache.AddObstacle(new RcVec3f(position.X, position.Y, position.Z), radius, height);
    }

    /// <summary>
    /// Requests a box obstacle to be carved out of the navmesh.
    /// </summary>
    /// <param name="center">The center of the box.</param>
    /// <param name="halfExtents">The half-extents of the box.</param>
    /// <param name="yRotation">The rotation around the Y axis in radians.</param>
    /// <returns>An obstacle reference for later removal.</returns>
    /// <remarks>The carve is applied by subsequent <see cref="Update"/> calls.</remarks>
    public long AddBoxObstacle(Vector3 center, Vector3 halfExtents, float yRotation = 0f)
    {
        return tileCache.AddBoxObstacle(
            new RcVec3f(center.X, center.Y, center.Z),
            new RcVec3f(halfExtents.X, halfExtents.Y, halfExtents.Z),
            yRotation);
    }

    /// <summary>
    /// Requests removal of a previously added obstacle, restoring the walkable
    /// surface beneath it.
    /// </summary>
    /// <param name="obstacleRef">The obstacle reference returned by an add method.</param>
    /// <remarks>The restore is applied by subsequent <see cref="Update"/> calls.</remarks>
    public void RemoveObstacle(long obstacleRef)
    {
        tileCache.RemoveObstacle(obstacleRef);
    }

    /// <summary>
    /// Processes pending obstacle requests, rebuilding at most one affected
    /// tile per call.
    /// </summary>
    /// <returns>
    /// True when the tile cache is up to date (no pending requests or dirty
    /// tiles remain); false when more calls are needed.
    /// </returns>
    public bool Update()
    {
        return tileCache.Update();
    }
}
