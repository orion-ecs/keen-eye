using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Conditions;

/// <summary>
/// Condition that checks if an entity has reached its navigation destination.
/// </summary>
/// <remarks>
/// <para>
/// This condition returns true when the entity is within the specified tolerance
/// of its destination, or when the agent reports it has arrived (remaining distance
/// is less than or equal to stopping distance).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var behavior = new Sequence {
///     Children = [
///         new ActionNode { Action = new MoveToAction { Destination = target } },
///         new ConditionNode { Condition = new AtDestinationCondition() },
///         new ActionNode { Action = new InteractAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class AtDestinationCondition : ICondition
{
    /// <summary>
    /// The tolerance for considering the destination reached.
    /// </summary>
    /// <remarks>
    /// If not set (null), uses the agent's <see cref="NavMeshAgent.StoppingDistance"/>.
    /// </remarks>
    public float? Tolerance { get; set; }

    /// <summary>
    /// Whether to check against the destination stored in the blackboard.
    /// </summary>
    /// <remarks>
    /// When true, compares position against <see cref="BBKeys.Destination"/>.
    /// When false, uses the agent's current destination.
    /// </remarks>
    public bool UseBlackboardDestination { get; set; }

    /// <inheritdoc/>
    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (!world.Has<NavMeshAgent>(entity) || !world.Has<Transform3D>(entity))
        {
            return false;
        }

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);
        ref readonly var transform = ref world.Get<Transform3D>(entity);

        // Get the destination to check against
        var destination = UseBlackboardDestination
            ? blackboard.Get<Vector3?>(BBKeys.Destination) ?? agent.Destination
            : agent.Destination;

        // Get the tolerance to use
        var tolerance = Tolerance ?? agent.StoppingDistance;

        // Check distance to destination
        var distance = Vector3.Distance(transform.Position, destination);
        if (distance <= tolerance)
        {
            return true;
        }

        // Also check agent's remaining distance if it has a path
        if (agent.HasPath && agent.RemainingDistance <= tolerance)
        {
            return true;
        }

        return false;
    }
}
