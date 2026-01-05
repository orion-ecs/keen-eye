using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.Systems;

/// <summary>
/// System that synchronizes dynamic obstacles with navigation data.
/// </summary>
/// <remarks>
/// <para>
/// This system tracks <see cref="NavMeshObstacle"/> components and updates
/// navigation data when obstacles are added, moved, or removed. It runs
/// in the LateUpdate phase after all movement is complete.
/// </para>
/// <para>
/// The system uses a dirty tracking mechanism to avoid unnecessary updates:
/// </para>
/// <list type="bullet">
/// <item><description>Tracks obstacle positions from the previous frame</description></item>
/// <item><description>Only triggers updates when obstacles move significantly</description></item>
/// <item><description>Respects the configured update interval to prevent excessive recalculation</description></item>
/// </list>
/// </remarks>
internal sealed class ObstacleUpdateSystem : SystemBase
{
    private NavigationContext? context;
    private NavigationConfig? config;
    private readonly Dictionary<Entity, ObstacleState> obstacleStates;
    private float timeSinceLastUpdate;
    private bool navigationDataDirty;

    /// <summary>
    /// Creates a new obstacle update system.
    /// </summary>
    public ObstacleUpdateSystem()
    {
        obstacleStates = [];
    }

    /// <inheritdoc/>
    protected override void OnInitialize()
    {
        if (!World.TryGetExtension(out NavigationContext? ctx) || ctx is null)
        {
            throw new InvalidOperationException("ObstacleUpdateSystem requires NavigationContext extension.");
        }

        context = ctx;
        config = ctx.Config;
    }

    /// <inheritdoc/>
    public override void Update(float deltaTime)
    {
        if (context == null || config == null)
        {
            return;
        }

        timeSinceLastUpdate += deltaTime;

        // Check for obstacle changes
        CheckForObstacleChanges();

        // Process updates if dirty and enough time has passed
        if (navigationDataDirty && timeSinceLastUpdate >= config.ObstacleUpdateInterval)
        {
            ProcessObstacleUpdates();
            timeSinceLastUpdate = 0f;
            navigationDataDirty = false;
        }
    }

    private void CheckForObstacleChanges()
    {
        // Track which obstacles are still active
        var activeObstacles = new HashSet<Entity>();

        foreach (var entity in World.Query<NavMeshObstacle, Transform3D>())
        {
            activeObstacles.Add(entity);

            ref readonly var obstacle = ref World.Get<NavMeshObstacle>(entity);
            ref readonly var transform = ref World.Get<Transform3D>(entity);

            // Calculate obstacle world position
            var worldPosition = transform.Position + obstacle.Center;

            // Check if this is a new obstacle
            if (!obstacleStates.TryGetValue(entity, out var state))
            {
                // New obstacle - add to tracking
                obstacleStates[entity] = new ObstacleState
                {
                    LastPosition = worldPosition,
                    LastRotation = transform.Rotation,
                    IsCarving = obstacle.Carving
                };

                if (obstacle.Carving)
                {
                    navigationDataDirty = true;
                }

                continue;
            }

            // Check if the obstacle has moved significantly
            if (obstacle.Carving)
            {
                float distanceMoved = Vector3.Distance(worldPosition, state.LastPosition);

                if (distanceMoved >= obstacle.CarvingMoveThreshold)
                {
                    navigationDataDirty = true;
                    state.LastPosition = worldPosition;
                    state.LastRotation = transform.Rotation;
                    obstacleStates[entity] = state;
                }
            }
        }

        // Check for removed obstacles
        var removedObstacles = obstacleStates.Keys
            .Where(e => !activeObstacles.Contains(e))
            .ToList();

        foreach (var entity in removedObstacles)
        {
            var state = obstacleStates[entity];
            if (state.IsCarving)
            {
                navigationDataDirty = true;
            }

            obstacleStates.Remove(entity);
        }
    }

    private void ProcessObstacleUpdates()
    {
        // In a full implementation, this would:
        // 1. Collect all carving obstacles with their current transforms
        // 2. Send them to the navigation provider for mesh updates
        // 3. Invalidate affected paths

        // For now, this is a stub that logs the update
        // The actual navmesh carving would depend on the navigation provider implementation

        // Note: Grid-based navigation doesn't need carving updates in the same way
        // as navmesh navigation does. For grids, we could mark affected cells as blocked.
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        obstacleStates.Clear();
        base.Dispose();
    }

    private struct ObstacleState
    {
        public Vector3 LastPosition;
        public Quaternion LastRotation;
        public bool IsCarving;
    }
}
