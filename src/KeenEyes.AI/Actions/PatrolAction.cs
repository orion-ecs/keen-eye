using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Actions;

/// <summary>
/// AI action that cycles through a series of waypoints using pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// This action navigates to each waypoint in sequence. When <see cref="Loop"/> is true,
/// it cycles back to the first waypoint after reaching the last one.
/// </para>
/// <para>
/// Waypoints can be set directly via <see cref="Waypoints"/> or read from the
/// blackboard using <see cref="BBKeys.PatrolWaypoints"/>. The current waypoint index
/// is stored in the blackboard using <see cref="BBKeys.PatrolIndex"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var patrol = new PatrolAction
/// {
///     Waypoints =
///     [
///         new Vector3(0, 0, 0),
///         new Vector3(10, 0, 0),
///         new Vector3(10, 0, 10),
///         new Vector3(0, 0, 10)
///     ],
///     Loop = true,
///     WaitTimeAtWaypoint = 2.0f
/// };
/// </code>
/// </example>
public sealed class PatrolAction : IAIAction
{
    private bool isWaiting;
    private float waitTimer;
    private bool pathRequested;

    /// <summary>
    /// The waypoints to patrol through.
    /// </summary>
    /// <remarks>
    /// If null or empty, waypoints are read from the blackboard using <see cref="BBKeys.PatrolWaypoints"/>.
    /// </remarks>
    public Vector3[]? Waypoints { get; set; }

    /// <summary>
    /// Whether to loop back to the first waypoint after reaching the last.
    /// </summary>
    public bool Loop { get; set; } = true;

    /// <summary>
    /// Whether to patrol waypoints in reverse order after reaching the end.
    /// </summary>
    /// <remarks>
    /// When true, the patrol reverses direction at each end instead of jumping back to the start.
    /// This is often called "ping-pong" mode.
    /// </remarks>
    public bool PingPong { get; set; }

    /// <summary>
    /// The time to wait at each waypoint in seconds.
    /// </summary>
    public float WaitTimeAtWaypoint { get; set; }

    /// <summary>
    /// The tolerance for considering a waypoint reached.
    /// </summary>
    public float ArrivalTolerance { get; set; } = 0.5f;

    // Track direction for ping-pong mode
    private bool reverseDirection;

    /// <inheritdoc/>
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get waypoints
        var waypoints = Waypoints ?? blackboard.Get<Vector3[]?>(BBKeys.PatrolWaypoints);

        if (waypoints is null || waypoints.Length == 0)
        {
            return BTNodeState.Failure;
        }

        // Verify entity has required components
        if (!world.Has<NavMeshAgent>(entity) || !world.Has<Transform3D>(entity))
        {
            return BTNodeState.Failure;
        }

        // Get navigation context
        if (!world.TryGetExtension<NavigationContext>(out var nav) || nav is null)
        {
            return BTNodeState.Failure;
        }

        // Get or initialize current waypoint index
        var currentIndex = blackboard.Get(BBKeys.PatrolIndex, 0);

        // Ensure index is valid
        if (currentIndex < 0 || currentIndex >= waypoints.Length)
        {
            currentIndex = 0;
        }

        // Always store current index for external visibility
        blackboard.Set(BBKeys.PatrolIndex, currentIndex);

        var deltaTime = blackboard.Get(BBKeys.DeltaTime, 0f);

        // Handle waiting at waypoint
        if (isWaiting)
        {
            waitTimer -= deltaTime;
            if (waitTimer > 0)
            {
                return BTNodeState.Running;
            }

            isWaiting = false;
            pathRequested = false;

            // Move to next waypoint
            if (PingPong)
            {
                if (reverseDirection)
                {
                    currentIndex--;
                    if (currentIndex < 0)
                    {
                        currentIndex = 1;
                        reverseDirection = false;
                    }
                }
                else
                {
                    currentIndex++;
                    if (currentIndex >= waypoints.Length)
                    {
                        currentIndex = waypoints.Length - 2;
                        reverseDirection = true;
                    }
                }
            }
            else
            {
                currentIndex++;
                if (currentIndex >= waypoints.Length)
                {
                    if (Loop)
                    {
                        currentIndex = 0;
                    }
                    else
                    {
                        blackboard.Set(BBKeys.PatrolIndex, currentIndex);
                        return BTNodeState.Success;
                    }
                }
            }

            blackboard.Set(BBKeys.PatrolIndex, currentIndex);
        }

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        ref readonly var transform = ref world.Get<Transform3D>(entity);

        var targetWaypoint = waypoints[currentIndex];
        blackboard.Set(BBKeys.Destination, targetWaypoint);

        // Check if we've reached the waypoint
        var distanceToWaypoint = Vector3.Distance(transform.Position, targetWaypoint);
        if (distanceToWaypoint <= ArrivalTolerance ||
            (agent.HasPath && agent.RemainingDistance <= agent.StoppingDistance && pathRequested))
        {
            if (WaitTimeAtWaypoint > 0)
            {
                isWaiting = true;
                waitTimer = WaitTimeAtWaypoint;
                nav.Stop(entity);
                return BTNodeState.Running;
            }

            // No wait, move to next waypoint immediately
            pathRequested = false;

            if (PingPong)
            {
                if (reverseDirection)
                {
                    currentIndex--;
                    if (currentIndex < 0)
                    {
                        currentIndex = Math.Min(1, waypoints.Length - 1);
                        reverseDirection = false;
                    }
                }
                else
                {
                    currentIndex++;
                    if (currentIndex >= waypoints.Length)
                    {
                        currentIndex = Math.Max(0, waypoints.Length - 2);
                        reverseDirection = true;
                    }
                }
            }
            else
            {
                currentIndex++;
                if (currentIndex >= waypoints.Length)
                {
                    if (Loop)
                    {
                        currentIndex = 0;
                    }
                    else
                    {
                        blackboard.Set(BBKeys.PatrolIndex, currentIndex);
                        return BTNodeState.Success;
                    }
                }
            }

            blackboard.Set(BBKeys.PatrolIndex, currentIndex);

            // Request path to new waypoint
            nav.SetDestination(entity, waypoints[currentIndex]);
            pathRequested = true;
            return BTNodeState.Running;
        }

        // Request initial path if not yet done
        if (!pathRequested)
        {
            nav.SetDestination(entity, targetWaypoint);
            pathRequested = true;
        }

        // Check for pathfinding failure
        if (!agent.HasPath && !agent.PathPending && pathRequested)
        {
            // Try next waypoint if current is unreachable
            return BTNodeState.Running; // Keep trying
        }

        // Store current path in blackboard
        if (agent.HasPath && nav.TryGetAgentState(entity, out var state))
        {
            blackboard.Set(BBKeys.CurrentPath, state.Path);
        }

        return BTNodeState.Running;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        isWaiting = false;
        waitTimer = 0f;
        pathRequested = false;
        reverseDirection = false;
    }

    /// <inheritdoc/>
    public void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        Reset();

        // Stop the agent when interrupted
        if (world.Has<NavMeshAgent>(entity) &&
            world.TryGetExtension<NavigationContext>(out var nav) &&
            nav is not null)
        {
            nav.Stop(entity);
        }
    }
}
