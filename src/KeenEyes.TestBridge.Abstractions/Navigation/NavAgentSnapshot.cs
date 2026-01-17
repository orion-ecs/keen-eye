namespace KeenEyes.TestBridge.Navigation;

/// <summary>
/// Snapshot of a navigation agent's current state.
/// </summary>
public sealed record NavAgentSnapshot
{
    /// <summary>
    /// Gets the entity ID.
    /// </summary>
    public required int EntityId { get; init; }

    /// <summary>
    /// Gets whether the agent has a valid path.
    /// </summary>
    public required bool HasPath { get; init; }

    /// <summary>
    /// Gets whether the agent is stopped.
    /// </summary>
    public required bool IsStopped { get; init; }

    /// <summary>
    /// Gets whether a path request is pending.
    /// </summary>
    public required bool PathPending { get; init; }

    /// <summary>
    /// Gets the current waypoint index in the path.
    /// </summary>
    public required int CurrentWaypointIndex { get; init; }

    /// <summary>
    /// Gets the distance traveled along the current path.
    /// </summary>
    public required float DistanceTraveled { get; init; }

    /// <summary>
    /// Gets the agent's movement speed.
    /// </summary>
    public required float Speed { get; init; }

    /// <summary>
    /// Gets the agent's destination, if set.
    /// </summary>
    public NavPointSnapshot? Destination { get; init; }
}
