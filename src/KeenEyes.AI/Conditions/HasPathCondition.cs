using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.AI.Conditions;

/// <summary>
/// Condition that checks if an entity currently has a valid navigation path.
/// </summary>
/// <remarks>
/// <para>
/// This condition returns true if the entity has a <see cref="NavMeshAgent"/>
/// component with a valid path computed.
/// </para>
/// <para>
/// Use this condition to guard actions that require an existing path,
/// or to switch to fallback behavior when pathfinding fails.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var behavior = new Selector {
///     Children = [
///         new Sequence {
///             Children = [
///                 new ConditionNode { Condition = new HasPathCondition() },
///                 new ActionNode { Action = new FollowPathAction() }
///             ]
///         },
///         new ActionNode { Action = new IdleAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class HasPathCondition : ICondition
{
    /// <summary>
    /// Whether to also return false if a path is pending (being computed).
    /// </summary>
    /// <remarks>
    /// When true, only returns true if a path is fully computed and ready.
    /// When false, returns true if a path exists OR is being computed.
    /// </remarks>
    public bool RequireCompletedPath { get; set; } = true;

    /// <inheritdoc/>
    public bool Evaluate(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (!world.Has<NavMeshAgent>(entity))
        {
            return false;
        }

        ref readonly var agent = ref world.Get<NavMeshAgent>(entity);

        if (RequireCompletedPath)
        {
            return agent.HasPath;
        }

        return agent.HasPath || agent.PathPending;
    }
}
