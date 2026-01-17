namespace KeenEyes.TestBridge.Navigation;

/// <summary>
/// Controller interface for navigation debugging and inspection operations.
/// </summary>
/// <remarks>
/// <para>
/// This controller provides access to navigation state including navmesh agents,
/// pathfinding, and navigation mesh queries. It enables inspection and manipulation
/// of navigation components for debugging and testing.
/// </para>
/// <para>
/// <strong>Note:</strong> Requires the NavigationPlugin to be installed on the world
/// for full functionality.
/// </para>
/// </remarks>
public interface INavigationController
{
    #region Statistics

    /// <summary>
    /// Gets statistics about navigation system usage in the world.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Statistics about navigation components and state.</returns>
    Task<NavigationStatisticsSnapshot> GetStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets whether the navigation system is ready and initialized.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the navigation provider is ready.</returns>
    Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Agent Operations

    /// <summary>
    /// Gets all entities with NavMeshAgent components.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of entity IDs that have navigation agent components.</returns>
    Task<IReadOnlyList<int>> GetNavigationEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the state of a navigation agent for an entity.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent state, or null if the entity has no agent component.</returns>
    Task<NavAgentSnapshot?> GetAgentStateAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current path for a navigation agent.
    /// </summary>
    /// <param name="entityId">The entity ID to query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The agent's path, or null if the entity has no active path.</returns>
    Task<NavPathSnapshot?> GetPathAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a destination for a navigation agent.
    /// </summary>
    /// <param name="entityId">The entity ID to command.</param>
    /// <param name="x">The destination X coordinate.</param>
    /// <param name="y">The destination Y coordinate.</param>
    /// <param name="z">The destination Z coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the destination was set successfully.</returns>
    Task<bool> SetDestinationAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops a navigation agent and clears its path.
    /// </summary>
    /// <param name="entityId">The entity ID to stop.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the agent was stopped successfully.</returns>
    Task<bool> StopAgentAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes navigation for a stopped agent.
    /// </summary>
    /// <param name="entityId">The entity ID to resume.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the agent was resumed successfully.</returns>
    Task<bool> ResumeAgentAsync(int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Warps an agent to a position without pathfinding.
    /// </summary>
    /// <param name="entityId">The entity ID to warp.</param>
    /// <param name="x">The destination X coordinate.</param>
    /// <param name="y">The destination Y coordinate.</param>
    /// <param name="z">The destination Z coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the position is on the navmesh.</returns>
    Task<bool> WarpAgentAsync(int entityId, float x, float y, float z, CancellationToken cancellationToken = default);

    #endregion

    #region Path Queries

    /// <summary>
    /// Finds a path between two positions.
    /// </summary>
    /// <param name="startX">The start X coordinate.</param>
    /// <param name="startY">The start Y coordinate.</param>
    /// <param name="startZ">The start Z coordinate.</param>
    /// <param name="endX">The end X coordinate.</param>
    /// <param name="endY">The end Y coordinate.</param>
    /// <param name="endZ">The end Z coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The computed path, or null if no path exists.</returns>
    Task<NavPathSnapshot?> FindPathAsync(float startX, float startY, float startZ, float endX, float endY, float endZ, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a position is navigable.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="z">The Z coordinate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the position is on the navigation mesh.</returns>
    Task<bool> IsNavigableAsync(float x, float y, float z, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the nearest navigable point to a position.
    /// </summary>
    /// <param name="x">The query X coordinate.</param>
    /// <param name="y">The query Y coordinate.</param>
    /// <param name="z">The query Z coordinate.</param>
    /// <param name="searchRadius">Maximum distance to search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The nearest navigable point, or null if none found.</returns>
    Task<NavPointSnapshot?> FindNearestPointAsync(float x, float y, float z, float searchRadius, CancellationToken cancellationToken = default);

    #endregion
}
