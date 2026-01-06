namespace KeenEyes.AI.BehaviorTree.Composites;

/// <summary>
/// Composite node that implements OR logic.
/// </summary>
/// <remarks>
/// <para>
/// Selector executes children in order until one succeeds:
/// </para>
/// <list type="bullet">
/// <item><description>Returns <see cref="BTNodeState.Success"/> when any child succeeds</description></item>
/// <item><description>Returns <see cref="BTNodeState.Failure"/> when all children fail</description></item>
/// <item><description>Returns <see cref="BTNodeState.Running"/> when a child is still running</description></item>
/// </list>
/// <para>
/// Use selectors for fallback behavior: "Try A, if that fails try B, if that fails try C."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Try to attack, then chase, then patrol
/// var selector = new Selector
/// {
///     Children = [
///         new ActionNode { Action = new AttackAction() },
///         new ActionNode { Action = new ChaseAction() },
///         new ActionNode { Action = new PatrolAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class Selector : CompositeNode
{
    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        while (currentChildIndex < Children.Count)
        {
            var child = Children[currentChildIndex];
            var state = child.Execute(entity, blackboard, world);

            switch (state)
            {
                case BTNodeState.Success:
                    // Found a successful child - selector succeeds
                    currentChildIndex = 0;
                    return SetState(BTNodeState.Success);

                case BTNodeState.Running:
                    // Child still running - wait for completion
                    return SetState(BTNodeState.Running);

                case BTNodeState.Failure:
                    // Child failed - try next child
                    currentChildIndex++;
                    break;
            }
        }

        // All children failed - selector fails
        currentChildIndex = 0;
        return SetState(BTNodeState.Failure);
    }
}
