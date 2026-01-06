namespace KeenEyes.AI.BehaviorTree.Decorators;

/// <summary>
/// Decorator that repeats its child until it fails.
/// </summary>
/// <remarks>
/// <para>
/// UntilFail keeps executing its child as long as it succeeds:
/// </para>
/// <list type="bullet">
/// <item><description>Child returns Success → Reset and run again (returns Running)</description></item>
/// <item><description>Child returns Failure → Stop and return Success</description></item>
/// <item><description>Child returns Running → Return Running</description></item>
/// </list>
/// <para>
/// Use for continuous actions that should repeat until a condition changes:
/// "Keep attacking until target is dead."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Keep patrolling until something interrupts
/// var untilFail = new UntilFail
/// {
///     Child = new Sequence
///     {
///         Children = [
///             new ConditionNode { Condition = new NoEnemiesNearbyCondition() },
///             new ActionNode { Action = new PatrolAction() }
///         ]
///     }
/// };
/// </code>
/// </example>
public sealed class UntilFail : DecoratorNode
{
    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(BTNodeState.Success);
        }

        var state = Child.Execute(entity, blackboard, world);

        switch (state)
        {
            case BTNodeState.Failure:
                // Child failed - we're done (successfully)
                return SetState(BTNodeState.Success);

            case BTNodeState.Success:
                // Child succeeded - reset and continue
                Child.Reset();
                return SetState(BTNodeState.Running);

            default:
                return SetState(state);
        }
    }
}
