namespace KeenEyes.AI.BehaviorTree.Decorators;

/// <summary>
/// Decorator that repeats its child a specified number of times.
/// </summary>
/// <remarks>
/// <para>
/// Repeater executes its child repeatedly until:
/// </para>
/// <list type="bullet">
/// <item><description>The count is reached (returns Success)</description></item>
/// <item><description>The child fails (returns Failure, unless <see cref="IgnoreFailure"/> is true)</description></item>
/// </list>
/// <para>
/// Set <see cref="Count"/> to -1 for infinite repetition.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Patrol 5 times
/// var repeater = new Repeater
/// {
///     Count = 5,
///     Child = new ActionNode { Action = new PatrolAction() }
/// };
///
/// // Infinite loop (must be interrupted externally)
/// var infiniteRepeater = new Repeater
/// {
///     Count = -1,
///     Child = new ActionNode { Action = new IdleAction() }
/// };
/// </code>
/// </example>
public sealed class Repeater : DecoratorNode
{
    /// <summary>
    /// Gets or sets the number of times to repeat. Use -1 for infinite.
    /// </summary>
    public int Count { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether to continue repeating when the child fails.
    /// </summary>
    /// <remarks>
    /// If true, child failure is treated as success for counting purposes.
    /// If false (default), child failure stops repetition and returns Failure.
    /// </remarks>
    public bool IgnoreFailure { get; set; }

    /// <inheritdoc/>
    public override void Reset(Blackboard blackboard)
    {
        base.Reset(blackboard);
        ((RepeaterMemory)GetMemory(blackboard)).CurrentCount = 0;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(blackboard, BTNodeState.Failure);
        }

        var memory = (RepeaterMemory)GetMemory(blackboard);

        // Check if we've reached the count (for non-infinite)
        if (Count >= 0 && memory.CurrentCount >= Count)
        {
            memory.CurrentCount = 0;
            return SetState(blackboard, BTNodeState.Success);
        }

        var state = Child.Execute(entity, blackboard, world);

        switch (state)
        {
            case BTNodeState.Running:
                return SetState(blackboard, BTNodeState.Running);

            case BTNodeState.Failure when !IgnoreFailure:
                memory.CurrentCount = 0;
                return SetState(blackboard, BTNodeState.Failure);

            case BTNodeState.Success:
            case BTNodeState.Failure: // when IgnoreFailure is true
                memory.CurrentCount++;
                Child.Reset(blackboard);

                // Check if we just hit the count
                if (Count >= 0 && memory.CurrentCount >= Count)
                {
                    memory.CurrentCount = 0;
                    return SetState(blackboard, BTNodeState.Success);
                }

                // More iterations needed - keep running
                return SetState(blackboard, BTNodeState.Running);

            default:
                return SetState(blackboard, state);
        }
    }

    /// <inheritdoc/>
    protected internal override BTNodeMemory GetMemory(Blackboard blackboard)
        => blackboard.GetMemory<RepeaterMemory>(this);

    /// <summary>
    /// Per-entity execution memory for <see cref="Repeater"/>.
    /// </summary>
    internal sealed class RepeaterMemory : BTNodeMemory
    {
        public int CurrentCount { get; set; }
    }
}
