namespace KeenEyes.AI.BehaviorTree.Decorators;

/// <summary>
/// Decorator that always returns Success (except when Running).
/// </summary>
/// <remarks>
/// <para>
/// Succeeder ensures its child's result is always Success:
/// </para>
/// <list type="bullet">
/// <item><description>Child returns Success → Succeeder returns Success</description></item>
/// <item><description>Child returns Failure → Succeeder returns Success</description></item>
/// <item><description>Child returns Running → Succeeder returns Running</description></item>
/// </list>
/// <para>
/// Use for optional actions that shouldn't affect the parent's result:
/// "Try to loot, but continue even if there's nothing to loot."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Attempt to loot (always "succeeds" for the sequence)
/// var sequence = new Sequence
/// {
///     Children = [
///         new ActionNode { Action = new KillEnemyAction() },
///         new Succeeder { Child = new ActionNode { Action = new LootAction() } },
///         new ActionNode { Action = new MoveOnAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class Succeeder : DecoratorNode
{
    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(BTNodeState.Success);
        }

        var state = Child.Execute(entity, blackboard, world);

        return SetState(state == BTNodeState.Running
            ? BTNodeState.Running
            : BTNodeState.Success);
    }
}
