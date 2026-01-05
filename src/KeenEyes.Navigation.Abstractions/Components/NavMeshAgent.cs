using System.Numerics;

namespace KeenEyes.Navigation.Abstractions.Components;

/// <summary>
/// Component for entities that use navigation mesh pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// Attach this component to entities that should navigate using the
/// navigation system. The navigation system will compute paths and
/// provide movement targets for these entities.
/// </para>
/// <para>
/// Use <see cref="SetDestination"/> to request a new path, and check
/// <see cref="HasPath"/> to determine if a valid path exists.
/// </para>
/// </remarks>
public struct NavMeshAgent : IComponent
{
    /// <summary>
    /// The agent's physical settings for pathfinding.
    /// </summary>
    public AgentSettings Settings;

    /// <summary>
    /// The area mask determining which areas this agent can traverse.
    /// </summary>
    public NavAreaMask AreaMask;

    /// <summary>
    /// The maximum movement speed of the agent in units per second.
    /// </summary>
    public float Speed;

    /// <summary>
    /// The acceleration rate when changing velocity.
    /// </summary>
    public float Acceleration;

    /// <summary>
    /// The angular speed for turning in degrees per second.
    /// </summary>
    public float AngularSpeed;

    /// <summary>
    /// The distance from the destination at which the agent is considered to have arrived.
    /// </summary>
    public float StoppingDistance;

    /// <summary>
    /// Whether the agent should automatically brake when approaching the destination.
    /// </summary>
    public bool AutoBraking;

    /// <summary>
    /// The current destination the agent is navigating to.
    /// </summary>
    public Vector3 Destination;

    /// <summary>
    /// The next position the agent should move toward.
    /// </summary>
    /// <remarks>
    /// Updated by the navigation system based on the current path.
    /// </remarks>
    public Vector3 SteeringTarget;

    /// <summary>
    /// The desired velocity computed by the navigation system.
    /// </summary>
    public Vector3 DesiredVelocity;

    /// <summary>
    /// The remaining distance to the destination along the current path.
    /// </summary>
    public float RemainingDistance;

    /// <summary>
    /// Whether this agent currently has a valid path.
    /// </summary>
    public bool HasPath;

    /// <summary>
    /// Whether a path is currently being computed for this agent.
    /// </summary>
    public bool PathPending;

    /// <summary>
    /// Whether the agent is currently on the navigation mesh.
    /// </summary>
    public bool IsOnNavMesh;

    /// <summary>
    /// Whether the agent is stopped (not actively navigating).
    /// </summary>
    public bool IsStopped;

    /// <summary>
    /// Creates a new NavMeshAgent with default settings.
    /// </summary>
    /// <returns>A new NavMeshAgent component.</returns>
    public static NavMeshAgent Create()
        => new()
        {
            Settings = AgentSettings.Default,
            AreaMask = NavAreaMask.All,
            Speed = 3.5f,
            Acceleration = 8f,
            AngularSpeed = 120f,
            StoppingDistance = 0.1f,
            AutoBraking = true,
            IsStopped = true
        };

    /// <summary>
    /// Creates a new NavMeshAgent with the specified settings.
    /// </summary>
    /// <param name="settings">The agent's physical settings.</param>
    /// <param name="speed">The movement speed.</param>
    /// <returns>A new NavMeshAgent component.</returns>
    public static NavMeshAgent Create(AgentSettings settings, float speed = 3.5f)
        => Create() with { Settings = settings, Speed = speed };

    /// <summary>
    /// Sets the agent's destination, triggering path computation.
    /// </summary>
    /// <param name="destination">The target position to navigate to.</param>
    public void SetDestination(Vector3 destination)
    {
        Destination = destination;
        PathPending = true;
        IsStopped = false;
    }

    /// <summary>
    /// Stops the agent and clears its current path.
    /// </summary>
    public void Stop()
    {
        HasPath = false;
        PathPending = false;
        IsStopped = true;
        DesiredVelocity = Vector3.Zero;
        RemainingDistance = 0f;
    }

    /// <summary>
    /// Resumes navigation if the agent was stopped.
    /// </summary>
    public void Resume()
    {
        if (HasPath)
        {
            IsStopped = false;
        }
    }
}
