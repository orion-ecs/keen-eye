using System.Numerics;

namespace KeenEyes.Navigation.Abstractions;

/// <summary>
/// Provides navigation and pathfinding services.
/// </summary>
/// <remarks>
/// <para>
/// The navigation provider is the main entry point for pathfinding operations.
/// It supports both synchronous (blocking) and asynchronous path computation,
/// as well as raycast queries for line-of-sight checks.
/// </para>
/// <para>
/// Implementations may use various strategies (navmesh, grid, hierarchical)
/// depending on the game's requirements.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Synchronous pathfinding
/// var path = navigator.FindPath(start, destination, AgentSettings.Default);
/// if (path.IsValid)
/// {
///     foreach (var waypoint in path)
///     {
///         // Follow waypoints
///     }
/// }
///
/// // Asynchronous pathfinding
/// using var request = navigator.RequestPath(start, destination, AgentSettings.Default);
/// var asyncPath = await request.AsTask();
/// </code>
/// </example>
public interface INavigationProvider : IDisposable
{
    /// <summary>
    /// Gets the navigation strategy used by this provider.
    /// </summary>
    NavigationStrategy Strategy { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is initialized and ready for queries.
    /// </summary>
    bool IsReady { get; }

    /// <summary>
    /// Gets the currently active navigation mesh, if any.
    /// </summary>
    INavigationMesh? ActiveMesh { get; }

    #region Pathfinding

    /// <summary>
    /// Computes a path synchronously (blocking).
    /// </summary>
    /// <param name="start">The starting position.</param>
    /// <param name="end">The destination position.</param>
    /// <param name="agent">The agent settings for path computation.</param>
    /// <param name="areaMask">Optional mask to filter traversable areas.</param>
    /// <returns>
    /// The computed path, or <see cref="NavPath.Empty"/> if no path exists.
    /// </returns>
    /// <remarks>
    /// This method blocks until path computation is complete.
    /// For long paths or complex environments, consider using
    /// <see cref="RequestPath"/> instead.
    /// </remarks>
    NavPath FindPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All);

    /// <summary>
    /// Requests a path computation asynchronously.
    /// </summary>
    /// <param name="start">The starting position.</param>
    /// <param name="end">The destination position.</param>
    /// <param name="agent">The agent settings for path computation.</param>
    /// <param name="areaMask">Optional mask to filter traversable areas.</param>
    /// <returns>A path request object to track computation progress.</returns>
    /// <remarks>
    /// The returned request should be disposed when no longer needed.
    /// </remarks>
    IPathRequest RequestPath(Vector3 start, Vector3 end, AgentSettings agent, NavAreaMask areaMask = NavAreaMask.All);

    /// <summary>
    /// Cancels all pending path requests.
    /// </summary>
    void CancelAllRequests();

    #endregion

    #region Raycasting

    /// <summary>
    /// Performs a raycast on the navigation mesh.
    /// </summary>
    /// <param name="start">The start position of the ray.</param>
    /// <param name="end">The end position of the ray.</param>
    /// <param name="hitPosition">
    /// When this method returns true, contains the hit position.
    /// When false, contains the end position.
    /// </param>
    /// <returns>True if the ray hit an obstacle or edge, false if clear.</returns>
    /// <remarks>
    /// Use this for line-of-sight checks and steering behavior.
    /// The raycast is performed on the navmesh surface, not in 3D space.
    /// </remarks>
    bool Raycast(Vector3 start, Vector3 end, out Vector3 hitPosition);

    /// <summary>
    /// Performs a raycast with area filtering.
    /// </summary>
    /// <param name="start">The start position of the ray.</param>
    /// <param name="end">The end position of the ray.</param>
    /// <param name="areaMask">Mask specifying traversable areas.</param>
    /// <param name="hitPosition">The hit or end position.</param>
    /// <param name="hitAreaType">The area type at the hit position.</param>
    /// <returns>True if the ray hit an obstacle, edge, or forbidden area.</returns>
    bool Raycast(
        Vector3 start,
        Vector3 end,
        NavAreaMask areaMask,
        out Vector3 hitPosition,
        out NavAreaType hitAreaType);

    #endregion

    #region Point Queries

    /// <summary>
    /// Finds the nearest point on the navmesh to the given position.
    /// </summary>
    /// <param name="position">The query position.</param>
    /// <param name="searchRadius">Maximum distance to search.</param>
    /// <returns>The nearest navmesh point, or null if none found.</returns>
    NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f);

    /// <summary>
    /// Checks if a position is navigable for the given agent.
    /// </summary>
    /// <param name="position">The position to check.</param>
    /// <param name="agent">The agent settings.</param>
    /// <returns>True if the position is navigable.</returns>
    bool IsNavigable(Vector3 position, AgentSettings agent);

    /// <summary>
    /// Projects a position onto the navmesh surface.
    /// </summary>
    /// <param name="position">The position to project.</param>
    /// <param name="maxDistance">Maximum projection distance.</param>
    /// <returns>The projected position, or null if projection failed.</returns>
    Vector3? ProjectToNavMesh(Vector3 position, float maxDistance = 5f);

    #endregion

    #region Area Costs

    /// <summary>
    /// Gets the traversal cost multiplier for an area type.
    /// </summary>
    /// <param name="areaType">The area type to query.</param>
    /// <returns>The cost multiplier (default is 1.0).</returns>
    float GetAreaCost(NavAreaType areaType);

    /// <summary>
    /// Sets the traversal cost multiplier for an area type.
    /// </summary>
    /// <param name="areaType">The area type to configure.</param>
    /// <param name="cost">
    /// The cost multiplier. Higher values make the area less preferred.
    /// Use 0 to make the area unwalkable.
    /// </param>
    void SetAreaCost(NavAreaType areaType, float cost);

    #endregion

    #region Updates

    /// <summary>
    /// Updates the navigation provider (processes pending requests, etc.).
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last update.</param>
    /// <remarks>
    /// Call this once per frame to process asynchronous path requests.
    /// </remarks>
    void Update(float deltaTime);

    #endregion
}
