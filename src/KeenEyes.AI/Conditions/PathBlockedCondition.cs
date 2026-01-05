using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Conditions;

/// <summary>
/// Condition that checks if the entity's current navigation path is blocked.
/// </summary>
/// <remarks>
/// <para>
/// This condition performs a raycast along the agent's intended movement direction
/// to detect if the path ahead is blocked. This is useful for detecting dynamic
/// obstacles that may have appeared after the path was computed.
/// </para>
/// <para>
/// It can also detect if path computation failed, indicating the destination
/// may be unreachable.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var behavior = new Selector {
///     Children = [
///         new Sequence {
///             Children = [
///                 new ConditionNode { Condition = new Inverter {
///                     Child = new PathBlockedCondition()
///                 }},
///                 new ActionNode { Action = new MoveToAction() }
///             ]
///         },
///         new ActionNode { Action = new FindAlternateRouteAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class PathBlockedCondition : ICondition
{
    /// <summary>
    /// The distance ahead to check for obstacles.
    /// </summary>
    public float CheckDistance { get; set; } = 2.0f;

    /// <summary>
    /// Whether to consider a pending path request as "not blocked".
    /// </summary>
    /// <remarks>
    /// When true, returns false while a path is being computed.
    /// When false, returns true if there's no valid path regardless of pending requests.
    /// </remarks>
    public bool AllowPendingPath { get; set; } = true;

    /// <summary>
    /// Whether to use navmesh raycast for more accurate obstacle detection.
    /// </summary>
    /// <remarks>
    /// When true, performs a raycast on the navigation mesh.
    /// When false, only checks agent path state.
    /// </remarks>
    public bool UseNavmeshRaycast { get; set; } = true;

    /// <inheritdoc/>
    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (!world.Has<NavMeshAgent>(entity))
        {
            return true; // No agent means path is "blocked" (unavailable)
        }

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);

        // Check if path computation is in progress
        if (agent.PathPending)
        {
            return !AllowPendingPath;
        }

        // Check if path computation failed
        if (!agent.HasPath && !agent.IsStopped)
        {
            return true; // Path was requested but failed
        }

        // If no path and stopped, we may not have requested one yet
        if (!agent.HasPath && agent.IsStopped)
        {
            return false; // Not blocked, just no path requested
        }

        // Path exists - check if it's still valid using raycast
        if (UseNavmeshRaycast && world.Has<Transform3D>(entity))
        {
            if (!world.TryGetExtension<NavigationContext>(out var nav) || nav is null)
            {
                return false; // Can't check, assume not blocked
            }

            ref readonly var transform = ref world.Get<Transform3D>(entity);

            // Raycast toward the steering target
            var direction = Vector3.Normalize(agent.SteeringTarget - transform.Position);
            var endPoint = transform.Position + direction * CheckDistance;

            if (nav.Raycast(transform.Position, endPoint, out var hitPosition))
            {
                // Check if the hit is between us and the steering target
                var distanceToHit = Vector3.Distance(transform.Position, hitPosition);
                var distanceToTarget = Vector3.Distance(transform.Position, agent.SteeringTarget);

                // Blocked if hit is closer than the steering target
                return distanceToHit < distanceToTarget;
            }
        }

        return false; // Path is valid and not blocked
    }
}
