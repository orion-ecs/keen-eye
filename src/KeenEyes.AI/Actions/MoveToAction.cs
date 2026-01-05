using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Actions;

/// <summary>
/// AI action that moves an entity to a destination using pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// This action uses the navigation system to compute and follow a path to the destination.
/// The destination can be set directly via <see cref="Destination"/> or read from the
/// blackboard using <see cref="BBKeys.Destination"/>.
/// </para>
/// <para>
/// The action returns <see cref="BTNodeState.Running"/> while navigating,
/// <see cref="BTNodeState.Success"/> when the destination is reached, and
/// <see cref="BTNodeState.Failure"/> if no valid path can be found.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var moveAction = new MoveToAction
/// {
///     Destination = new Vector3(10, 0, 10)
/// };
///
/// // Or use blackboard:
/// blackboard.Set(BBKeys.Destination, targetPosition);
/// var moveAction = new MoveToAction { UseBlackboardDestination = true };
/// </code>
/// </example>
public sealed class MoveToAction : IAIAction
{
    private bool pathRequested;
    private float pathRequestTime;

    /// <summary>
    /// The destination to move to.
    /// </summary>
    /// <remarks>
    /// This is ignored if <see cref="UseBlackboardDestination"/> is true.
    /// </remarks>
    public Vector3 Destination { get; set; }

    /// <summary>
    /// Whether to read the destination from the blackboard using <see cref="BBKeys.Destination"/>.
    /// </summary>
    public bool UseBlackboardDestination { get; set; }

    /// <summary>
    /// The tolerance for considering the destination reached.
    /// </summary>
    /// <remarks>
    /// Defaults to 0.5 units. Set higher for less precise navigation.
    /// </remarks>
    public float ArrivalTolerance { get; set; } = 0.5f;

    /// <summary>
    /// Maximum time to wait for a path before failing in seconds.
    /// </summary>
    public float PathTimeout { get; set; } = 5.0f;

    /// <inheritdoc/>
    public BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get the destination
        var destination = UseBlackboardDestination
            ? blackboard.Get<Vector3?>(BBKeys.Destination) ?? Destination
            : Destination;

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

        ref var agent = ref world.Get<NavMeshAgent>(entity);
        ref readonly var transform = ref world.Get<Transform3D>(entity);

        // Check if we've arrived
        var distanceToDestination = Vector3.Distance(transform.Position, destination);
        if (distanceToDestination <= ArrivalTolerance ||
            (agent.HasPath && agent.RemainingDistance <= agent.StoppingDistance))
        {
            // Store the path in blackboard before completing
            if (nav.TryGetAgentState(entity, out var state))
            {
                blackboard.Set(BBKeys.CurrentPath, state.Path);
            }

            return BTNodeState.Success;
        }

        var currentTime = blackboard.Get(BBKeys.Time, 0f);

        // Request path if we haven't yet
        if (!pathRequested)
        {
            nav.SetDestination(entity, destination);
            pathRequested = true;
            pathRequestTime = currentTime;
            return BTNodeState.Running;
        }

        // Check for path timeout
        if (!agent.HasPath && !agent.PathPending)
        {
            // Path computation failed
            return BTNodeState.Failure;
        }

        if (agent.PathPending && currentTime - pathRequestTime > PathTimeout)
        {
            // Timeout waiting for path
            return BTNodeState.Failure;
        }

        // Store current path in blackboard
        if (agent.HasPath && nav.TryGetAgentState(entity, out var navState))
        {
            blackboard.Set(BBKeys.CurrentPath, navState.Path);
        }

        return BTNodeState.Running;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        pathRequested = false;
        pathRequestTime = 0f;
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
