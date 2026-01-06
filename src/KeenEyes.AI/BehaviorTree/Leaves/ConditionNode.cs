namespace KeenEyes.AI.BehaviorTree.Leaves;

/// <summary>
/// Leaf node that wraps an <see cref="ICondition"/>.
/// </summary>
/// <remarks>
/// <para>
/// ConditionNode evaluates the wrapped condition and returns:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="BTNodeState.Success"/> if the condition is true</description></item>
/// <item><description><see cref="BTNodeState.Failure"/> if the condition is false</description></item>
/// </list>
/// <para>
/// Conditions are typically used as guards in sequences to check prerequisites
/// before executing actions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check if target is in range before attacking
/// var sequence = new Sequence
/// {
///     Children = [
///         new ConditionNode { Condition = new InRangeCondition { Range = 2f } },
///         new ActionNode { Action = new AttackAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class ConditionNode : BTNode
{
    /// <summary>
    /// Gets or sets the condition to evaluate.
    /// </summary>
    public ICondition? Condition { get; set; }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Condition == null)
        {
            return SetState(BTNodeState.Failure);
        }

        var result = Condition.Evaluate(entity, blackboard, world);
        return SetState(result ? BTNodeState.Success : BTNodeState.Failure);
    }
}
