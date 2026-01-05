using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation;

/// <summary>
/// Extension API for navigation operations in a world.
/// </summary>
/// <remarks>
/// <para>
/// This class provides the primary API for navigation operations. It manages
/// path requests, agent destinations, and provides access to underlying
/// navigation data.
/// </para>
/// <para>
/// Access this API through the world extension system:
/// <code>
/// var nav = world.GetExtension&lt;NavigationContext&gt;();
/// nav.SetDestination(entity, targetPosition);
/// </code>
/// </para>
/// </remarks>
[PluginExtension("Navigation")]
public sealed class NavigationContext : IDisposable
{
    private readonly IWorld world;
    private readonly NavigationConfig config;
    private readonly Dictionary<Entity, AgentNavigationState> agentStates;
    private readonly Dictionary<Entity, IPathRequest> pendingRequests;
    private readonly Lock stateLock = new();
    private INavigationProvider? provider;
    private bool disposed;

    /// <summary>
    /// Gets the navigation configuration.
    /// </summary>
    public NavigationConfig Config => config;

    /// <summary>
    /// Gets the underlying navigation provider.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no provider is configured.</exception>
    public INavigationProvider Provider => provider
        ?? throw new InvalidOperationException("Navigation provider is not initialized.");

    /// <summary>
    /// Gets a value indicating whether the navigation system is ready.
    /// </summary>
    public bool IsReady => provider?.IsReady ?? false;

    /// <summary>
    /// Gets the current navigation strategy.
    /// </summary>
    public NavigationStrategy Strategy => provider?.Strategy ?? config.Strategy;

    /// <summary>
    /// Gets the number of agents currently navigating.
    /// </summary>
    public int ActiveAgentCount
    {
        get
        {
            lock (stateLock)
            {
                return agentStates.Count;
            }
        }
    }

    /// <summary>
    /// Gets the number of pending path requests.
    /// </summary>
    public int PendingRequestCount
    {
        get
        {
            lock (stateLock)
            {
                return pendingRequests.Count;
            }
        }
    }

    internal NavigationContext(IWorld world, NavigationConfig config)
    {
        this.world = world;
        this.config = config;
        agentStates = [];
        pendingRequests = [];
    }

    internal void SetProvider(INavigationProvider provider)
    {
        this.provider = provider;
    }

    #region Destination API

    /// <summary>
    /// Sets the destination for an agent entity.
    /// </summary>
    /// <param name="entity">The entity with a <see cref="NavMeshAgent"/> component.</param>
    /// <param name="destination">The target position to navigate to.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the entity doesn't have a <see cref="NavMeshAgent"/> component.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method initiates pathfinding to the destination. The path will be
    /// computed asynchronously and the agent's <see cref="NavMeshAgent.HasPath"/>
    /// property will be set when complete.
    /// </para>
    /// </remarks>
    public void SetDestination(Entity entity, Vector3 destination)
    {
        ThrowIfDisposed();

        if (!world.Has<NavMeshAgent>(entity))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a NavMeshAgent component.");
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.SetDestination(destination);

        // Request a new path
        RequestPathForAgent(entity, destination);
    }

    /// <summary>
    /// Stops an agent and clears its current path.
    /// </summary>
    /// <param name="entity">The entity to stop.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the entity doesn't have a <see cref="NavMeshAgent"/> component.
    /// </exception>
    public void Stop(Entity entity)
    {
        ThrowIfDisposed();

        if (!world.Has<NavMeshAgent>(entity))
        {
            throw new InvalidOperationException($"Entity {entity} does not have a NavMeshAgent component.");
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Stop();

        // Cancel any pending request
        CancelRequestForAgent(entity);

        // Clear navigation state
        lock (stateLock)
        {
            agentStates.Remove(entity);
        }
    }

    /// <summary>
    /// Resumes navigation for a stopped agent.
    /// </summary>
    /// <param name="entity">The entity to resume.</param>
    public void Resume(Entity entity)
    {
        ThrowIfDisposed();

        if (!world.Has<NavMeshAgent>(entity))
        {
            return;
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        agent.Resume();
    }

    /// <summary>
    /// Warps an agent to a position without pathfinding.
    /// </summary>
    /// <param name="entity">The entity to warp.</param>
    /// <param name="position">The position to warp to.</param>
    /// <param name="clearPath">Whether to clear the current path.</param>
    /// <returns>True if the position is on the navmesh, false otherwise.</returns>
    public bool Warp(Entity entity, Vector3 position, bool clearPath = true)
    {
        ThrowIfDisposed();

        if (provider == null || !world.Has<NavMeshAgent>(entity))
        {
            return false;
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);

        var projectedPos = provider.ProjectToNavMesh(position, config.MaxProjectionDistance);
        if (!projectedPos.HasValue)
        {
            return false;
        }

        // Update agent's position (if the entity has Transform3D, it should be updated separately)
        agent.IsOnNavMesh = true;

        if (clearPath)
        {
            agent.Stop();
            CancelRequestForAgent(entity);
            lock (stateLock)
            {
                agentStates.Remove(entity);
            }
        }

        return true;
    }

    #endregion

    #region Query API

    /// <summary>
    /// Finds a path synchronously.
    /// </summary>
    /// <param name="start">The starting position.</param>
    /// <param name="end">The destination position.</param>
    /// <param name="agent">The agent settings for path computation.</param>
    /// <param name="areaMask">Optional mask to filter traversable areas.</param>
    /// <returns>The computed path.</returns>
    public NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        if (provider == null)
        {
            return NavPath.Empty;
        }

        return provider.FindPath(start, end, agent, areaMask);
    }

    /// <summary>
    /// Requests a path asynchronously.
    /// </summary>
    /// <param name="start">The starting position.</param>
    /// <param name="end">The destination position.</param>
    /// <param name="agent">The agent settings for path computation.</param>
    /// <param name="areaMask">Optional mask to filter traversable areas.</param>
    /// <returns>A path request object to track computation progress.</returns>
    public IPathRequest? RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All)
    {
        ThrowIfDisposed();

        if (provider == null)
        {
            return null;
        }

        return provider.RequestPath(start, end, agent, areaMask);
    }

    /// <summary>
    /// Performs a raycast on the navigation data.
    /// </summary>
    /// <param name="start">The start position.</param>
    /// <param name="end">The end position.</param>
    /// <param name="hitPosition">The hit position, or end if no hit.</param>
    /// <returns>True if the ray hit an obstacle.</returns>
    public bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition)
    {
        ThrowIfDisposed();

        if (provider == null)
        {
            hitPosition = end;
            return false;
        }

        return provider.Raycast(start, end, out hitPosition);
    }

    /// <summary>
    /// Finds the nearest navigable point to a position.
    /// </summary>
    /// <param name="position">The query position.</param>
    /// <param name="searchRadius">Maximum distance to search.</param>
    /// <returns>The nearest navigable point, or null if none found.</returns>
    public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f)
    {
        ThrowIfDisposed();
        return provider?.FindNearestPoint(position, searchRadius);
    }

    /// <summary>
    /// Checks if a position is navigable.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="agent">The agent settings.</param>
    /// <returns>True if the position is navigable.</returns>
    public bool IsNavigable(Vector3 position, AgentSettings agent)
    {
        ThrowIfDisposed();
        return provider?.IsNavigable(position, agent) ?? false;
    }

    #endregion

    #region Agent State Management

    /// <summary>
    /// Gets the navigation state for an agent.
    /// </summary>
    /// <param name="entity">The agent entity.</param>
    /// <param name="state">The navigation state if found.</param>
    /// <returns>True if the agent has navigation state.</returns>
    public bool TryGetAgentState(Entity entity, out AgentNavigationState state)
    {
        lock (stateLock)
        {
            return agentStates.TryGetValue(entity, out state);
        }
    }

    internal void SetAgentState(Entity entity, AgentNavigationState state)
    {
        lock (stateLock)
        {
            agentStates[entity] = state;
        }
    }

    internal void RemoveAgent(Entity entity)
    {
        CancelRequestForAgent(entity);
        lock (stateLock)
        {
            agentStates.Remove(entity);
        }
    }

    internal IReadOnlyDictionary<Entity, AgentNavigationState> GetAllAgentStates()
    {
        lock (stateLock)
        {
            return new Dictionary<Entity, AgentNavigationState>(agentStates);
        }
    }

    #endregion

    #region Path Request Management

    private void RequestPathForAgent(Entity entity, Vector3 destination)
    {
        if (provider == null || !world.Has<NavMeshAgent>(entity))
        {
            return;
        }

        // Cancel any existing request
        CancelRequestForAgent(entity);

        // Get agent's current position from Transform3D
        if (!world.Has<Common.Transform3D>(entity))
        {
            return;
        }

        ref readonly var transform = ref world.Get<Common.Transform3D>(entity);
        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        var request = provider.RequestPath(transform.Position, destination, agent.Settings, agent.AreaMask);

        lock (stateLock)
        {
            pendingRequests[entity] = request;
        }
    }

    private void CancelRequestForAgent(Entity entity)
    {
        IPathRequest? request;
        lock (stateLock)
        {
            if (!pendingRequests.TryGetValue(entity, out request))
            {
                return;
            }

            pendingRequests.Remove(entity);
        }

        request.Cancel();
        request.Dispose();
    }

    internal void ProcessPendingRequests()
    {
        if (provider == null)
        {
            return;
        }

        List<Entity>? completedRequests = null;

        lock (stateLock)
        {
            foreach (var (entity, request) in pendingRequests)
            {
                if (request.Status == PathRequestStatus.Completed ||
                    request.Status == PathRequestStatus.Failed ||
                    request.Status == PathRequestStatus.Cancelled)
                {
                    completedRequests ??= [];
                    completedRequests.Add(entity);

                    if (request.Status == PathRequestStatus.Completed && world.Has<NavMeshAgent>(entity))
                    {
                        ref var agent = ref world.Get<NavMeshAgent>(entity);
                        var path = request.Result;

                        if (path.IsValid)
                        {
                            agent.HasPath = true;
                            agent.PathPending = false;
                            agent.RemainingDistance = path.Length;

                            // Initialize navigation state
                            agentStates[entity] = new AgentNavigationState
                            {
                                Path = path,
                                CurrentWaypointIndex = 0,
                                DistanceTraveled = 0f
                            };
                        }
                        else
                        {
                            agent.HasPath = false;
                            agent.PathPending = false;
                        }
                    }
                    else if (world.Has<NavMeshAgent>(entity))
                    {
                        ref var agent = ref world.Get<NavMeshAgent>(entity);
                        agent.HasPath = false;
                        agent.PathPending = false;
                    }
                }
            }

            if (completedRequests != null)
            {
                foreach (var entity in completedRequests)
                {
                    if (pendingRequests.TryGetValue(entity, out var request))
                    {
                        pendingRequests.Remove(entity);
                        request.Dispose();
                    }
                }
            }
        }
    }

    #endregion

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        // Cancel all pending requests
        lock (stateLock)
        {
            foreach (var request in pendingRequests.Values)
            {
                request.Cancel();
                request.Dispose();
            }

            pendingRequests.Clear();
            agentStates.Clear();
        }

        // Don't dispose provider - it's managed by the plugin
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}

/// <summary>
/// Tracks the navigation state for an agent following a path.
/// </summary>
public struct AgentNavigationState
{
    /// <summary>
    /// The current path being followed.
    /// </summary>
    public NavPath Path;

    /// <summary>
    /// The index of the current waypoint being navigated to.
    /// </summary>
    public int CurrentWaypointIndex;

    /// <summary>
    /// The distance traveled along the path so far.
    /// </summary>
    public float DistanceTraveled;
}
