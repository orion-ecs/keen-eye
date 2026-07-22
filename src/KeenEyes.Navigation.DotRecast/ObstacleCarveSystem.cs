using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// System that routes carving <see cref="NavMeshObstacle"/> components through a
/// <see cref="NavMeshTileCache"/>, carving the walkable surface itself instead of
/// only modifying query costs.
/// </summary>
/// <remarks>
/// <para>
/// Registered by <see cref="DotRecastNavigationPlugin"/> when the plugin is
/// constructed with a <see cref="NavMeshTileCache"/>. Each update, entities
/// carrying a carving <see cref="NavMeshObstacle"/> and a <see cref="Transform3D"/>
/// are mirrored into the tile cache as cylinder or box obstacles: newly seen
/// obstacles are added, obstacles that moved past their
/// <see cref="NavMeshObstacle.CarvingMoveThreshold"/> are re-carved (removed and
/// re-added, since the tile cache has no in-place move), and obstacles whose
/// entity despawned or dropped the component are removed.
/// </para>
/// <para>
/// After reconciling requests the system pumps <see cref="NavMeshTileCache.Update"/>
/// up to <see cref="NavMeshObstacleCarveConfig.MaxTileRebuildsPerUpdate"/> times,
/// spreading tile re-contouring across frames. Runs early in the update phase so
/// carves land before path requests execute. Non-carving obstacles are ignored
/// here and remain the responsibility of the provider-agnostic soft-cost path.
/// </para>
/// </remarks>
/// <param name="tileCache">The tile cache whose navmesh is carved.</param>
/// <param name="config">The per-tick rebuild budget.</param>
internal sealed class ObstacleCarveSystem(NavMeshTileCache tileCache, NavMeshObstacleCarveConfig config) : SystemBase
{
    private readonly Dictionary<int, CarvedObstacle> carved = [];
    private readonly HashSet<int> seen = [];
    private readonly List<int> removalScratch = [];

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        seen.Clear();

        foreach (var entity in World.Query<NavMeshObstacle, Transform3D>())
        {
            ref readonly var obstacle = ref World.Get<NavMeshObstacle>(entity);

            // Non-carving obstacles are local-avoidance only; leave them to the
            // provider-agnostic soft-cost path.
            if (!obstacle.Carving)
            {
                continue;
            }

            ref readonly var transform = ref World.Get<Transform3D>(entity);
            var worldPosition = transform.Position + obstacle.Center;
            seen.Add(entity.Id);

            if (!carved.TryGetValue(entity.Id, out var state))
            {
                long addedRef = AddObstacle(in obstacle, worldPosition, transform.Rotation);
                carved[entity.Id] = new CarvedObstacle { ObstacleRef = addedRef, LastPosition = worldPosition };
                continue;
            }

            // The tile cache exposes no in-place move, so a significant move is a
            // remove followed by a re-add at the new position.
            float movedDistance = Vector3.Distance(worldPosition, state.LastPosition);
            if (movedDistance >= obstacle.CarvingMoveThreshold)
            {
                tileCache.RemoveObstacle(state.ObstacleRef);
                long movedRef = AddObstacle(in obstacle, worldPosition, transform.Rotation);
                carved[entity.Id] = new CarvedObstacle { ObstacleRef = movedRef, LastPosition = worldPosition };
            }
        }

        // Remove carves whose entity despawned or dropped the carving obstacle.
        removalScratch.Clear();
        foreach (var (entityId, _) in carved)
        {
            if (!seen.Contains(entityId))
            {
                removalScratch.Add(entityId);
            }
        }

        foreach (int entityId in removalScratch)
        {
            tileCache.RemoveObstacle(carved[entityId].ObstacleRef);
            carved.Remove(entityId);
        }

        // Pump the incremental rebuild under the per-tick budget; stop early once
        // the tile cache reports it is up to date.
        int budget = config.MaxTileRebuildsPerUpdate;
        for (int i = 0; i < budget; i++)
        {
            if (tileCache.Update())
            {
                break;
            }
        }
    }

    private long AddObstacle(in NavMeshObstacle obstacle, Vector3 worldPosition, Quaternion rotation)
    {
        if (obstacle.Shape == ObstacleShape.Box)
        {
            // The component's world position is the box center, matching the tile
            // cache box convention directly.
            return tileCache.AddBoxObstacle(worldPosition, obstacle.Size * 0.5f, ExtractYaw(rotation));
        }

        // Cylinder: the tile cache wants the center of the base, but the component
        // treats its world position as the volumetric center, so lower the base by
        // half the height to keep the carve straddling the walkable surface.
        var basePosition = worldPosition - new Vector3(0f, obstacle.Height * 0.5f, 0f);
        return tileCache.AddCylinderObstacle(basePosition, obstacle.Radius, obstacle.Height);
    }

    /// <summary>
    /// Extracts the heading (rotation about the Y axis) from a quaternion so a box
    /// obstacle is carved with the entity's yaw.
    /// </summary>
    private static float ExtractYaw(Quaternion q)
    {
        return MathF.Atan2(2f * (q.X * q.Z + q.W * q.Y), 1f - 2f * (q.X * q.X + q.Y * q.Y));
    }

    private struct CarvedObstacle
    {
        public long ObstacleRef;
        public Vector3 LastPosition;
    }
}
