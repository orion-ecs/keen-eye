namespace KeenEyes.AI.BehaviorTree.Decorators;

/// <summary>
/// Decorator that inverts the result of its child.
/// </summary>
/// <remarks>
/// <para>
/// Inverter flips Success to Failure and Failure to Success.
/// Running is passed through unchanged.
/// </para>
/// <para>
/// Use for negating conditions: "If NOT at destination, keep moving."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Execute if NOT in range
/// var inverter = new Inverter
/// {
///     Child = new ConditionNode { Condition = new InRangeCondition() }
/// };
/// </code>
/// </example>
public sealed class Inverter : DecoratorNode
{
    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(BTNodeState.Failure);
        }

        var state = Child.Execute(entity, blackboard, world);

        return SetState(state switch
        {
            BTNodeState.Success => BTNodeState.Failure,
            BTNodeState.Failure => BTNodeState.Success,
            _ => state
        });
    }
}
