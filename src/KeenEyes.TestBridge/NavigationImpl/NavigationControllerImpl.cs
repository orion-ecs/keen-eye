using System.Numerics;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;
using KeenEyes.TestBridge.Navigation;

namespace KeenEyes.TestBridge.NavigationImpl;

/// <summary>
/// In-process implementation of <see cref="INavigationController"/>.
/// </summary>
internal sealed class NavigationControllerImpl(World world) : INavigationController
{
    #region Statistics

    /// <inheritdoc />
    public Task<NavigationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(new NavigationStatisticsSnapshot
            {
                IsReady = false,
                Strategy = "None",
                ActiveAgentCount = 0,
                PendingRequestCount = 0
            });
        }

        return Task.FromResult(new NavigationStatisticsSnapshot
        {
            IsReady = nav.IsReady,
            Strategy = nav.Strategy.ToString(),
            ActiveAgentCount = nav.ActiveAgentCount,
            PendingRequestCount = nav.PendingRequestCount
        });
    }

    /// <inheritdoc />
    public Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(nav.IsReady);
    }

    #endregion

    #region Agent Operations

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> GetNavigationEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var entities = new List<int>();
        foreach (var entity in world.Query<NavMeshAgent>())
        {
            entities.Add(entity.Id);
        }
        return Task.FromResult<IReadOnlyList<int>>(entities);
    }

    /// <inheritdoc />
    public Task<NavAgentSnapshot?> GetAgentStateAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult<NavAgentSnapshot?>(null);
        }

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);

        NavPointSnapshot? destination = null;
        if (agent.HasPath || agent.PathPending)
        {
            destination = new NavPointSnapshot
            {
                X = agent.Destination.X,
                Y = agent.Destination.Y,
                Z = agent.Destination.Z,
                AreaType = "Unknown"
            };
        }

        int currentWaypointIndex = 0;
        float distanceTraveled = 0f;

        if (world.TryGetExtension<NavigationContext>(out var nav) && nav.TryGetAgentState(entity, out var state))
        {
            currentWaypointIndex = state.CurrentWaypointIndex;
            distanceTraveled = state.DistanceTraveled;
        }

        return Task.FromResult<NavAgentSnapshot?>(new NavAgentSnapshot
        {
            EntityId = entityId,
            HasPath = agent.HasPath,
            IsStopped = agent.IsStopped,
            PathPending = agent.PathPending,
            CurrentWaypointIndex = currentWaypointIndex,
            DistanceTraveled = distanceTraveled,
            Speed = agent.Speed,
            Destination = destination
        });
    }

    /// <inheritdoc />
    public Task<NavPathSnapshot?> GetPathAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        if (!nav.TryGetAgentState(entity, out var state))
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        var path = state.Path;
        if (!path.IsValid)
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        var waypoints = new List<NavPointSnapshot>();
        foreach (var point in path)
        {
            waypoints.Add(new NavPointSnapshot
            {
                X = point.Position.X,
                Y = point.Position.Y,
                Z = point.Position.Z,
                AreaType = point.AreaType.ToString()
            });
        }

        return Task.FromResult<NavPathSnapshot?>(new NavPathSnapshot
        {
            IsValid = path.IsValid,
            IsComplete = path.IsComplete,
            TotalCost = path.TotalCost,
            Length = path.Length,
            Waypoints = waypoints
        });
    }

    /// <inheritdoc />
    public Task<bool> SetDestinationAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult(false);
        }

        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        try
        {
            var destination = new Vector3(x, y, z);
            nav.SetDestination(entity, destination);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> StopAgentAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult(false);
        }

        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        try
        {
            nav.Stop(entity);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> ResumeAgentAsync(int entityId, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult(false);
        }

        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        try
        {
            nav.Resume(entity);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<bool> WarpAgentAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default)
    {
        var entity = new Entity(entityId, 0);
        if (!world.IsAlive(entity) || !world.Has<NavMeshAgent>(entity))
        {
            return Task.FromResult(false);
        }

        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        try
        {
            var position = new Vector3(x, y, z);
            return Task.FromResult(nav.Warp(entity, position));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    #endregion

    #region Path Queries

    /// <inheritdoc />
    public Task<NavPathSnapshot?> FindPathAsync(float startX, float startY, float startZ, float endX, float endY, float endZ, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        if (!nav.IsReady)
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }

        try
        {
            var start = new Vector3(startX, startY, startZ);
            var end = new Vector3(endX, endY, endZ);
            var agent = AgentSettings.Default;

            var path = nav.FindPath(start, end, agent);
            if (!path.IsValid)
            {
                return Task.FromResult<NavPathSnapshot?>(null);
            }

            var waypoints = new List<NavPointSnapshot>();
            foreach (var point in path)
            {
                waypoints.Add(new NavPointSnapshot
                {
                    X = point.Position.X,
                    Y = point.Position.Y,
                    Z = point.Position.Z,
                    AreaType = point.AreaType.ToString()
                });
            }

            return Task.FromResult<NavPathSnapshot?>(new NavPathSnapshot
            {
                IsValid = path.IsValid,
                IsComplete = path.IsComplete,
                TotalCost = path.TotalCost,
                Length = path.Length,
                Waypoints = waypoints
            });
        }
        catch
        {
            return Task.FromResult<NavPathSnapshot?>(null);
        }
    }

    /// <inheritdoc />
    public Task<bool> IsNavigableAsync(float x, float y, float z, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult(false);
        }

        if (!nav.IsReady)
        {
            return Task.FromResult(false);
        }

        try
        {
            var position = new Vector3(x, y, z);
            var agent = AgentSettings.Default;
            return Task.FromResult(nav.IsNavigable(position, agent));
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<NavPointSnapshot?> FindNearestPointAsync(float x, float y, float z, float searchRadius, CancellationToken cancellationToken = default)
    {
        if (!world.TryGetExtension<NavigationContext>(out var nav))
        {
            return Task.FromResult<NavPointSnapshot?>(null);
        }

        if (!nav.IsReady)
        {
            return Task.FromResult<NavPointSnapshot?>(null);
        }

        try
        {
            var position = new Vector3(x, y, z);
            var nearestPoint = nav.FindNearestPoint(position, searchRadius);

            if (!nearestPoint.HasValue)
            {
                return Task.FromResult<NavPointSnapshot?>(null);
            }

            return Task.FromResult<NavPointSnapshot?>(new NavPointSnapshot
            {
                X = nearestPoint.Value.Position.X,
                Y = nearestPoint.Value.Position.Y,
                Z = nearestPoint.Value.Position.Z,
                AreaType = nearestPoint.Value.AreaType.ToString()
            });
        }
        catch
        {
            return Task.FromResult<NavPointSnapshot?>(null);
        }
    }

    #endregion
}
