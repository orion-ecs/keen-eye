using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation;

/// <summary>
/// Configuration for the navigation plugin.
/// </summary>
/// <remarks>
/// <para>
/// This configuration determines how the navigation plugin operates, including
/// the pathfinding strategy, request throttling, and agent behavior settings.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = new NavigationConfig
/// {
///     Strategy = NavigationStrategy.Grid,
///     MaxPathRequestsPerFrame = 10,
///     AgentSteeringEnabled = true
/// };
///
/// world.InstallPlugin(new NavigationPlugin(config));
/// </code>
/// </example>
public sealed class NavigationConfig
{
    /// <summary>
    /// Gets or sets the navigation strategy to use.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set to <see cref="NavigationStrategy.Custom"/>, a custom
    /// <see cref="INavigationProvider"/> must be supplied via
    /// <see cref="CustomProvider"/>.
    /// </para>
    /// </remarks>
    public NavigationStrategy Strategy { get; set; } = NavigationStrategy.Grid;

    /// <summary>
    /// Gets or sets the custom navigation provider.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="Strategy"/> is <see cref="NavigationStrategy.Custom"/>.
    /// </remarks>
    public INavigationProvider? CustomProvider { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of path requests to process per frame.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Higher values allow faster path computation but may cause frame rate issues.
    /// Consider the complexity of your navmesh and typical path lengths.
    /// </para>
    /// </remarks>
    public int MaxPathRequestsPerFrame { get; set; } = 10;

    /// <summary>
    /// Gets or sets the maximum number of pending path requests.
    /// </summary>
    /// <remarks>
    /// When this limit is reached, new path requests will fail immediately.
    /// </remarks>
    public int MaxPendingRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether agent steering is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the <see cref="Systems.NavMeshAgentSystem"/> will automatically
    /// compute and apply steering velocities to move agents along their paths.
    /// </para>
    /// <para>
    /// When disabled, paths are computed but agents must be moved manually.
    /// </para>
    /// </remarks>
    public bool AgentSteeringEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the distance at which path waypoints are considered reached.
    /// </summary>
    /// <remarks>
    /// Agents will advance to the next waypoint when within this distance.
    /// </remarks>
    public float WaypointReachDistance { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets whether dynamic obstacle carving is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled, the <see cref="Systems.ObstacleUpdateSystem"/> will update
    /// the navigation data when <see cref="KeenEyes.Navigation.Abstractions.Components.NavMeshObstacle"/>
    /// components are added, moved, or removed.
    /// </para>
    /// </remarks>
    public bool DynamicObstaclesEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum time between obstacle recalculations in seconds.
    /// </summary>
    /// <remarks>
    /// Prevents excessive recalculation when obstacles move frequently.
    /// </remarks>
    public float ObstacleUpdateInterval { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets whether to automatically project agents onto the navmesh.
    /// </summary>
    /// <remarks>
    /// When enabled, agents that move off the navmesh will be projected back onto it.
    /// </remarks>
    public bool AutoProjectToNavMesh { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum distance to project agents back onto the navmesh.
    /// </summary>
    public float MaxProjectionDistance { get; set; } = 5f;

    /// <summary>
    /// Gets a default configuration.
    /// </summary>
    public static NavigationConfig Default => new();

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>Null if valid, otherwise an error message.</returns>
    public string? Validate()
    {
        if (Strategy == NavigationStrategy.Custom && CustomProvider == null)
        {
            return "Custom strategy requires a CustomProvider to be set.";
        }

        if (MaxPathRequestsPerFrame <= 0)
        {
            return $"MaxPathRequestsPerFrame must be positive, got {MaxPathRequestsPerFrame}.";
        }

        if (MaxPendingRequests <= 0)
        {
            return $"MaxPendingRequests must be positive, got {MaxPendingRequests}.";
        }

        if (WaypointReachDistance <= 0f)
        {
            return $"WaypointReachDistance must be positive, got {WaypointReachDistance}.";
        }

        if (ObstacleUpdateInterval < 0f)
        {
            return $"ObstacleUpdateInterval must be non-negative, got {ObstacleUpdateInterval}.";
        }

        if (MaxProjectionDistance <= 0f)
        {
            return $"MaxProjectionDistance must be positive, got {MaxProjectionDistance}.";
        }

        return null;
    }
}
