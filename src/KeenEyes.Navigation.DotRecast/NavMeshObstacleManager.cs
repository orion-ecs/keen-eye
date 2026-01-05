using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Manages dynamic obstacles for navmesh carving.
/// </summary>
/// <remarks>
/// <para>
/// This manager tracks obstacles and marks affected polygons as unwalkable
/// during pathfinding queries. Unlike true tile carving which modifies the
/// navmesh data, this approach uses query filters to exclude polygons.
/// </para>
/// <para>
/// For true dynamic navmesh updates, consider using DotRecast.Detour.TileCache
/// which supports streaming and runtime navmesh modification.
/// </para>
/// </remarks>
public sealed class NavMeshObstacleManager
{
    private readonly Dictionary<int, ObstacleData> obstacles = [];
    private readonly Lock obstaclesLock = new();
    private int nextObstacleId;
    private bool isDirty;

    /// <summary>
    /// Gets the number of active obstacles.
    /// </summary>
    public int ObstacleCount
    {
        get
        {
            lock (obstaclesLock)
            {
                return obstacles.Count;
            }
        }
    }

    /// <summary>
    /// Adds a box-shaped obstacle.
    /// </summary>
    /// <param name="position">The center position of the obstacle.</param>
    /// <param name="halfExtents">The half-extents of the box.</param>
    /// <param name="rotation">The Y-axis rotation in radians.</param>
    /// <returns>An obstacle handle for later removal.</returns>
    public int AddBoxObstacle(Vector3 position, Vector3 halfExtents, float rotation = 0f)
    {
        lock (obstaclesLock)
        {
            int id = ++nextObstacleId;

            obstacles[id] = new ObstacleData
            {
                Id = id,
                Position = position,
                Shape = ObstacleShape.Box,
                HalfExtents = halfExtents,
                Rotation = rotation
            };

            isDirty = true;
            return id;
        }
    }

    /// <summary>
    /// Adds a cylindrical obstacle.
    /// </summary>
    /// <param name="position">The center position at the base of the cylinder.</param>
    /// <param name="radius">The radius of the cylinder.</param>
    /// <param name="height">The height of the cylinder.</param>
    /// <returns>An obstacle handle for later removal.</returns>
    public int AddCylinderObstacle(Vector3 position, float radius, float height)
    {
        lock (obstaclesLock)
        {
            int id = ++nextObstacleId;

            obstacles[id] = new ObstacleData
            {
                Id = id,
                Position = position,
                Shape = ObstacleShape.Cylinder,
                Radius = radius,
                Height = height
            };

            isDirty = true;
            return id;
        }
    }

    /// <summary>
    /// Updates an obstacle's position.
    /// </summary>
    /// <param name="obstacleId">The obstacle handle.</param>
    /// <param name="position">The new position.</param>
    /// <returns>True if the obstacle was found and updated.</returns>
    public bool UpdateObstacle(int obstacleId, Vector3 position)
    {
        lock (obstaclesLock)
        {
            if (obstacles.TryGetValue(obstacleId, out var obstacle))
            {
                obstacle.Position = position;
                obstacles[obstacleId] = obstacle;
                isDirty = true;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Removes an obstacle.
    /// </summary>
    /// <param name="obstacleId">The obstacle handle.</param>
    /// <returns>True if the obstacle was found and removed.</returns>
    public bool RemoveObstacle(int obstacleId)
    {
        lock (obstaclesLock)
        {
            if (obstacles.Remove(obstacleId))
            {
                isDirty = true;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Removes all obstacles.
    /// </summary>
    public void Clear()
    {
        lock (obstaclesLock)
        {
            obstacles.Clear();
            isDirty = true;
        }
    }

    /// <summary>
    /// Checks if a position is blocked by any obstacle.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <returns>True if the position is inside an obstacle.</returns>
    public bool IsBlocked(Vector3 position)
    {
        lock (obstaclesLock)
        {
            foreach (var obstacle in obstacles.Values)
            {
                if (IsInsideObstacle(position, obstacle))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// Gets polygons that should be excluded due to obstacles.
    /// </summary>
    /// <param name="navMesh">The navigation mesh.</param>
    /// <param name="excludedPolys">Output list of excluded polygon references.</param>
    internal void GetExcludedPolygons(DtNavMesh navMesh, List<long> excludedPolys)
    {
        excludedPolys.Clear();

        lock (obstaclesLock)
        {
            if (!isDirty && obstacles.Count == 0)
            {
                return;
            }

            foreach (var obstacle in obstacles.Values)
            {
                GetPolygonsInObstacle(navMesh, obstacle, excludedPolys);
            }

            isDirty = false;
        }
    }

    private static void GetPolygonsInObstacle(DtNavMesh navMesh, ObstacleData obstacle, List<long> polys)
    {
        // Calculate bounding box for the obstacle
        Vector3 bmin, bmax;

        if (obstacle.Shape == ObstacleShape.Cylinder)
        {
            bmin = new Vector3(
                obstacle.Position.X - obstacle.Radius,
                obstacle.Position.Y,
                obstacle.Position.Z - obstacle.Radius);
            bmax = new Vector3(
                obstacle.Position.X + obstacle.Radius,
                obstacle.Position.Y + obstacle.Height,
                obstacle.Position.Z + obstacle.Radius);
        }
        else // Box
        {
            // For rotated boxes, use the enclosing AABB
            float maxExtent = MathF.Max(obstacle.HalfExtents.X, obstacle.HalfExtents.Z);
            bmin = new Vector3(
                obstacle.Position.X - maxExtent,
                obstacle.Position.Y - obstacle.HalfExtents.Y,
                obstacle.Position.Z - maxExtent);
            bmax = new Vector3(
                obstacle.Position.X + maxExtent,
                obstacle.Position.Y + obstacle.HalfExtents.Y,
                obstacle.Position.Z + maxExtent);
        }

        // Query polygons in the bounding box
        var query = new DtNavMeshQuery(navMesh);
        var filter = new DtQueryDefaultFilter();

        var center = new RcVec3f(
            (bmin.X + bmax.X) * 0.5f,
            (bmin.Y + bmax.Y) * 0.5f,
            (bmin.Z + bmax.Z) * 0.5f);

        var halfExtents = new RcVec3f(
            (bmax.X - bmin.X) * 0.5f,
            (bmax.Y - bmin.Y) * 0.5f,
            (bmax.Z - bmin.Z) * 0.5f);

        long[] polyBuffer = new long[64];
        var status = query.QueryPolygons(center, halfExtents, filter, polyBuffer, out var polyCount, 64);

        if (status.Succeeded())
        {
            for (int i = 0; i < polyCount; i++)
            {
                // Check if polygon center is inside obstacle
                var polyCenter = navMesh.GetPolyCenter(polyBuffer[i]);
                var pos = new Vector3(polyCenter.X, polyCenter.Y, polyCenter.Z);

                if (IsInsideObstacle(pos, obstacle))
                {
                    polys.Add(polyBuffer[i]);
                }
            }
        }
    }

    private static bool IsInsideObstacle(Vector3 position, ObstacleData obstacle)
    {
        if (obstacle.Shape == ObstacleShape.Cylinder)
        {
            // Check height
            if (position.Y < obstacle.Position.Y || position.Y > obstacle.Position.Y + obstacle.Height)
            {
                return false;
            }

            // Check XZ distance
            float dx = position.X - obstacle.Position.X;
            float dz = position.Z - obstacle.Position.Z;
            float distSq = dx * dx + dz * dz;

            return distSq <= obstacle.Radius * obstacle.Radius;
        }
        else // Box
        {
            // Transform point to obstacle local space
            Vector3 local = position - obstacle.Position;

            // Apply inverse rotation
            if (!obstacle.Rotation.Equals(0f))
            {
                float cos = MathF.Cos(-obstacle.Rotation);
                float sin = MathF.Sin(-obstacle.Rotation);
                float newX = local.X * cos - local.Z * sin;
                float newZ = local.X * sin + local.Z * cos;
                local = new Vector3(newX, local.Y, newZ);
            }

            // Check if inside box
            return MathF.Abs(local.X) <= obstacle.HalfExtents.X &&
                   MathF.Abs(local.Y) <= obstacle.HalfExtents.Y &&
                   MathF.Abs(local.Z) <= obstacle.HalfExtents.Z;
        }
    }

    private struct ObstacleData
    {
        public int Id;
        public Vector3 Position;
        public ObstacleShape Shape;

        // Box
        public Vector3 HalfExtents;
        public float Rotation;

        // Cylinder
        public float Radius;
        public float Height;
    }
}
