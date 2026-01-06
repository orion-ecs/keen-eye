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
    private int currentCount;

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
    public override void Reset()
    {
        base.Reset();
        currentCount = 0;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(BTNodeState.Failure);
        }

        // Check if we've reached the count (for non-infinite)
        if (Count >= 0 && currentCount >= Count)
        {
            currentCount = 0;
            return SetState(BTNodeState.Success);
        }

        var state = Child.Execute(entity, blackboard, world);

        switch (state)
        {
            case BTNodeState.Running:
                return SetState(BTNodeState.Running);

            case BTNodeState.Failure when !IgnoreFailure:
                currentCount = 0;
                return SetState(BTNodeState.Failure);

            case BTNodeState.Success:
            case BTNodeState.Failure: // when IgnoreFailure is true
                currentCount++;
                Child.Reset();

                // Check if we just hit the count
                if (Count >= 0 && currentCount >= Count)
                {
                    currentCount = 0;
                    return SetState(BTNodeState.Success);
                }

                // More iterations needed - keep running
                return SetState(BTNodeState.Running);

            default:
                return SetState(state);
        }
    }
}
