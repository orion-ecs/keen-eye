namespace KeenEyes.AI.BehaviorTree.Leaves;

/// <summary>
/// Leaf node that waits for a specified duration.
/// </summary>
/// <remarks>
/// <para>
/// WaitNode returns Running until the duration has elapsed, then returns Success.
/// Uses <see cref="BBKeys.DeltaTime"/> from the blackboard for timing.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Wait 2 seconds between attacks
/// var sequence = new Sequence
/// {
///     Children = [
///         new ActionNode { Action = new AttackAction() },
///         new WaitNode { Duration = 2f },
///         new ActionNode { Action = new AttackAction() }
///     ]
/// };
/// </code>
/// </example>
public sealed class WaitNode : BTNode
{
    /// <summary>
    /// Gets or sets the duration to wait in seconds.
    /// </summary>
    public float Duration { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether to use random duration within a range.
    /// </summary>
    /// <remarks>
    /// If true, the actual duration is random between <see cref="MinDuration"/>
    /// and <see cref="MaxDuration"/>.
    /// </remarks>
    public bool UseRandomDuration { get; set; }

    /// <summary>
    /// Gets or sets the minimum duration when using random.
    /// </summary>
    public float MinDuration { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the maximum duration when using random.
    /// </summary>
    public float MaxDuration { get; set; } = 2f;

    /// <inheritdoc/>
    public override void Reset(Blackboard blackboard)
    {
        base.Reset(blackboard);
        var memory = (WaitMemory)GetMemory(blackboard);
        memory.Elapsed = 0f;
        memory.RandomizedDuration = null;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        var memory = (WaitMemory)GetMemory(blackboard);

        // Determine target duration
        var targetDuration = GetTargetDuration(memory);

        // Get delta time from blackboard
        var deltaTime = blackboard.Get(BBKeys.DeltaTime, 0f);
        memory.Elapsed += deltaTime;

        if (memory.Elapsed >= targetDuration)
        {
            memory.Elapsed = 0f;
            memory.RandomizedDuration = null;
            return SetState(blackboard, BTNodeState.Success);
        }

        return SetState(blackboard, BTNodeState.Running);
    }

    private float GetTargetDuration(WaitMemory memory)
    {
        if (!UseRandomDuration)
        {
            return Duration;
        }

        // Calculate randomized duration once per execution
        memory.RandomizedDuration ??= MinDuration + (Random.Shared.NextSingle() * (MaxDuration - MinDuration));
        return memory.RandomizedDuration.Value;
    }

    /// <inheritdoc/>
    protected internal override BTNodeMemory GetMemory(Blackboard blackboard)
        => blackboard.GetMemory<WaitMemory>(this);

    /// <summary>
    /// Per-entity execution memory for <see cref="WaitNode"/>.
    /// </summary>
    internal sealed class WaitMemory : BTNodeMemory
    {
        public float Elapsed { get; set; }
        public float? RandomizedDuration { get; set; }
    }
}
