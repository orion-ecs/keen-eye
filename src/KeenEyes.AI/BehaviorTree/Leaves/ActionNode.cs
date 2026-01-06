namespace KeenEyes.AI.BehaviorTree.Leaves;

/// <summary>
/// Leaf node that wraps an <see cref="IAIAction"/>.
/// </summary>
/// <remarks>
/// <para>
/// ActionNode executes the wrapped action and returns its result.
/// This is the primary way to integrate game-specific behaviors into behavior trees.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var actionNode = new ActionNode
/// {
///     Action = new MoveToAction { Speed = 5f }
/// };
/// </code>
/// </example>
public sealed class ActionNode : BTNode
{
    /// <summary>
    /// Gets or sets the action to execute.
    /// </summary>
    public IAIAction? Action { get; set; }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Action == null)
        {
            return SetState(BTNodeState.Failure);
        }

        var state = Action.Execute(entity, blackboard, world);
        return SetState(state);
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        Action?.Reset();
    }

    /// <inheritdoc/>
    public override void OnInterrupted(Entity entity, Blackboard blackboard, IWorld world)
    {
        base.OnInterrupted(entity, blackboard, world);
        Action?.OnInterrupted(entity, blackboard, world);
    }
}
