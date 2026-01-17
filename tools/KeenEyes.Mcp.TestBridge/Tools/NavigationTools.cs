using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Navigation;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for navigation debugging: agents, paths, and navmesh queries.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the NavigationPlugin debugging infrastructure via MCP, allowing inspection
/// and manipulation of navigation agents and pathfinding in running games.
/// </para>
/// <para>
/// Note: These tools require the NavigationPlugin to be installed in the target world.
/// Entities must have the NavMeshAgent component for agent operations to work.
/// </para>
/// </remarks>
[McpServerToolType]
public sealed class NavigationTools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "navigation_get_statistics")]
    [Description("Get overall navigation statistics including agent count and navigation readiness.")]
    public async Task<NavigationStatisticsResult> GetStatistics()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Navigation.GetStatisticsAsync();
        return NavigationStatisticsResult.FromSnapshot(stats);
    }

    [McpServerTool(Name = "navigation_is_ready")]
    [Description("Check if the navigation system is ready and initialized.")]
    public async Task<OperationResult> IsReady()
    {
        var bridge = connection.GetBridge();
        var isReady = await bridge.Navigation.IsReadyAsync();
        return new OperationResult
        {
            Success = isReady,
            Error = isReady ? null : "Navigation system is not ready"
        };
    }

    #endregion

    #region Agent Operations

    [McpServerTool(Name = "navigation_agent_list")]
    [Description("List all entities that have a NavMeshAgent component.")]
    public async Task<EntityListResult> GetNavigationEntities()
    {
        var bridge = connection.GetBridge();
        var entities = await bridge.Navigation.GetNavigationEntitiesAsync();
        return new EntityListResult
        {
            Success = true,
            EntityIds = entities,
            Count = entities.Count
        };
    }

    [McpServerTool(Name = "navigation_agent_get")]
    [Description("Get the current state of a navigation agent, including path status and destination.")]
    public async Task<NavAgentResult> GetAgentState(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Navigation.GetAgentStateAsync(entityId);

        if (snapshot == null)
        {
            return new NavAgentResult
            {
                Success = false,
                Error = $"No navigation agent found on entity {entityId}"
            };
        }

        return NavAgentResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "navigation_agent_get_path")]
    [Description("Get the current path for a navigation agent, including all waypoints.")]
    public async Task<NavPathResult> GetPath(
        [Description("The entity ID to query")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Navigation.GetPathAsync(entityId);

        if (snapshot == null)
        {
            return new NavPathResult
            {
                Success = false,
                Error = $"No path found for entity {entityId}"
            };
        }

        return NavPathResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "navigation_agent_set_destination")]
    [Description("Set a destination for a navigation agent, triggering pathfinding.")]
    public async Task<OperationResult> SetDestination(
        [Description("The entity ID")]
        int entityId,
        [Description("The destination X coordinate")]
        float x,
        [Description("The destination Y coordinate")]
        float y,
        [Description("The destination Z coordinate")]
        float z)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Navigation.SetDestinationAsync(entityId, x, y, z);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to set destination for entity {entityId}"
        };
    }

    [McpServerTool(Name = "navigation_agent_stop")]
    [Description("Stop a navigation agent and clear its current path.")]
    public async Task<OperationResult> StopAgent(
        [Description("The entity ID to stop")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Navigation.StopAgentAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to stop agent on entity {entityId}"
        };
    }

    [McpServerTool(Name = "navigation_agent_resume")]
    [Description("Resume navigation for a stopped agent.")]
    public async Task<OperationResult> ResumeAgent(
        [Description("The entity ID to resume")]
        int entityId)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Navigation.ResumeAgentAsync(entityId);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to resume agent on entity {entityId}"
        };
    }

    [McpServerTool(Name = "navigation_agent_warp")]
    [Description("Warp an agent to a position without pathfinding. Returns false if position is not on navmesh.")]
    public async Task<OperationResult> WarpAgent(
        [Description("The entity ID to warp")]
        int entityId,
        [Description("The destination X coordinate")]
        float x,
        [Description("The destination Y coordinate")]
        float y,
        [Description("The destination Z coordinate")]
        float z)
    {
        var bridge = connection.GetBridge();
        var success = await bridge.Navigation.WarpAgentAsync(entityId, x, y, z);
        return new OperationResult
        {
            Success = success,
            Error = success ? null : $"Failed to warp agent on entity {entityId} (position may not be on navmesh)"
        };
    }

    #endregion

    #region Path Queries

    [McpServerTool(Name = "navigation_find_path")]
    [Description("Find a path between two positions using the navigation system.")]
    public async Task<NavPathResult> FindPath(
        [Description("The start X coordinate")]
        float startX,
        [Description("The start Y coordinate")]
        float startY,
        [Description("The start Z coordinate")]
        float startZ,
        [Description("The end X coordinate")]
        float endX,
        [Description("The end Y coordinate")]
        float endY,
        [Description("The end Z coordinate")]
        float endZ)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Navigation.FindPathAsync(startX, startY, startZ, endX, endY, endZ);

        if (snapshot == null)
        {
            return new NavPathResult
            {
                Success = false,
                Error = "Failed to find path (navigation not ready or no path exists)"
            };
        }

        return NavPathResult.FromSnapshot(snapshot);
    }

    [McpServerTool(Name = "navigation_is_navigable")]
    [Description("Check if a position is on the navigation mesh and navigable.")]
    public async Task<OperationResult> IsNavigable(
        [Description("The X coordinate")]
        float x,
        [Description("The Y coordinate")]
        float y,
        [Description("The Z coordinate")]
        float z)
    {
        var bridge = connection.GetBridge();
        var isNavigable = await bridge.Navigation.IsNavigableAsync(x, y, z);
        return new OperationResult
        {
            Success = isNavigable,
            Error = isNavigable ? null : "Position is not navigable"
        };
    }

    [McpServerTool(Name = "navigation_find_nearest_point")]
    [Description("Find the nearest navigable point to a given position within a search radius.")]
    public async Task<NavPointResult> FindNearestPoint(
        [Description("The X coordinate")]
        float x,
        [Description("The Y coordinate")]
        float y,
        [Description("The Z coordinate")]
        float z,
        [Description("Maximum search radius in world units")]
        float searchRadius = 10f)
    {
        var bridge = connection.GetBridge();
        var snapshot = await bridge.Navigation.FindNearestPointAsync(x, y, z, searchRadius);

        if (snapshot == null)
        {
            return new NavPointResult
            {
                Success = false,
                Error = $"No navigable point found within {searchRadius} units"
            };
        }

        return NavPointResult.FromSnapshot(snapshot);
    }

    #endregion
}

#region Result Types

/// <summary>
/// Result for navigation statistics.
/// </summary>
public sealed record NavigationStatisticsResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets whether the navigation system is ready and initialized.
    /// </summary>
    public bool IsReady { get; init; }

    /// <summary>
    /// Gets the current navigation strategy.
    /// </summary>
    public string Strategy { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of active navigation agents.
    /// </summary>
    public int ActiveAgentCount { get; init; }

    /// <summary>
    /// Gets the number of pending path requests.
    /// </summary>
    public int PendingRequestCount { get; init; }

    internal static NavigationStatisticsResult FromSnapshot(NavigationStatisticsSnapshot snapshot)
    {
        return new NavigationStatisticsResult
        {
            IsReady = snapshot.IsReady,
            Strategy = snapshot.Strategy,
            ActiveAgentCount = snapshot.ActiveAgentCount,
            PendingRequestCount = snapshot.PendingRequestCount
        };
    }
}

/// <summary>
/// Result for basic operations.
/// </summary>
public record OperationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result for navigation agent queries.
/// </summary>
public sealed record NavAgentResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public int EntityId { get; init; }

    /// <summary>
    /// Gets whether the agent has a valid path.
    /// </summary>
    public bool HasPath { get; init; }

    /// <summary>
    /// Gets whether the agent is stopped.
    /// </summary>
    public bool IsStopped { get; init; }

    /// <summary>
    /// Gets whether a path request is pending.
    /// </summary>
    public bool PathPending { get; init; }

    /// <summary>
    /// Gets the current waypoint index in the path.
    /// </summary>
    public int CurrentWaypointIndex { get; init; }

    /// <summary>
    /// Gets the distance traveled along the current path.
    /// </summary>
    public float DistanceTraveled { get; init; }

    /// <summary>
    /// Gets the agent's movement speed.
    /// </summary>
    public float Speed { get; init; }

    /// <summary>
    /// Gets the agent's destination, if set.
    /// </summary>
    public NavPointSnapshot? Destination { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static NavAgentResult FromSnapshot(NavAgentSnapshot snapshot)
    {
        return new NavAgentResult
        {
            Success = true,
            EntityId = snapshot.EntityId,
            HasPath = snapshot.HasPath,
            IsStopped = snapshot.IsStopped,
            PathPending = snapshot.PathPending,
            CurrentWaypointIndex = snapshot.CurrentWaypointIndex,
            DistanceTraveled = snapshot.DistanceTraveled,
            Speed = snapshot.Speed,
            Destination = snapshot.Destination
        };
    }
}

/// <summary>
/// Result for navigation path queries.
/// </summary>
public sealed record NavPathResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets whether this path is valid (has waypoints).
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Gets whether this path reaches the intended destination.
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Gets the total traversal cost of the path.
    /// </summary>
    public float TotalCost { get; init; }

    /// <summary>
    /// Gets the total length of the path in world units.
    /// </summary>
    public float Length { get; init; }

    /// <summary>
    /// Gets the waypoints comprising the path.
    /// </summary>
    public IReadOnlyList<NavPointSnapshot>? Waypoints { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static NavPathResult FromSnapshot(NavPathSnapshot snapshot)
    {
        return new NavPathResult
        {
            Success = true,
            IsValid = snapshot.IsValid,
            IsComplete = snapshot.IsComplete,
            TotalCost = snapshot.TotalCost,
            Length = snapshot.Length,
            Waypoints = snapshot.Waypoints
        };
    }
}

/// <summary>
/// Result for navigation point queries.
/// </summary>
public sealed record NavPointResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Gets the X coordinate.
    /// </summary>
    public float X { get; init; }

    /// <summary>
    /// Gets the Y coordinate.
    /// </summary>
    public float Y { get; init; }

    /// <summary>
    /// Gets the Z coordinate.
    /// </summary>
    public float Z { get; init; }

    /// <summary>
    /// Gets the navigation area type at this point.
    /// </summary>
    public string AreaType { get; init; } = string.Empty;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? Error { get; init; }

    internal static NavPointResult FromSnapshot(NavPointSnapshot snapshot)
    {
        return new NavPointResult
        {
            Success = true,
            X = snapshot.X,
            Y = snapshot.Y,
            Z = snapshot.Z,
            AreaType = snapshot.AreaType
        };
    }
}

#endregion
