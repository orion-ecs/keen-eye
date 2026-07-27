namespace KeenEyes.AI.BehaviorTree.Composites;

/// <summary>
/// Composite node that implements AND logic.
/// </summary>
/// <remarks>
/// <para>
/// Sequence executes children in order until one fails:
/// </para>
/// <list type="bullet">
/// <item><description>Returns <see cref="BTNodeState.Failure"/> when any child fails</description></item>
/// <item><description>Returns <see cref="BTNodeState.Success"/> when all children succeed</description></item>
/// <item><description>Returns <see cref="BTNodeState.Running"/> when a child is still running</description></item>
/// </list>
/// <para>
/// Use sequences for step-by-step behavior: "Do A, then B, then C."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Check condition, then perform action
/// var sequence = new Sequence
/// {
///     Children = [
///         new ConditionNode { Condition = new InRangeCondition() },
///         new ActionNode { Action = new AttackAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class Sequence : CompositeNode
{
    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        var memory = GetCompositeMemory(blackboard);
        while (memory.CurrentChildIndex < Children.Count)
        {
            var child = Children[memory.CurrentChildIndex];
            var state = child.Execute(entity, blackboard, world);

            switch (state)
            {
                case BTNodeState.Failure:
                    // Child failed - sequence fails
                    memory.CurrentChildIndex = 0;
                    return SetState(blackboard, BTNodeState.Failure);

                case BTNodeState.Running:
                    // Child still running - wait for completion
                    return SetState(blackboard, BTNodeState.Running);

                case BTNodeState.Success:
                    // Child succeeded - try next child
                    memory.CurrentChildIndex++;
                    break;
            }
        }

        // All children succeeded - sequence succeeds
        memory.CurrentChildIndex = 0;
        return SetState(blackboard, BTNodeState.Success);
    }
}
