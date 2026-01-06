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
    private float elapsed;

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

    private float? randomizedDuration;

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        elapsed = 0f;
        randomizedDuration = null;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Determine target duration
        var targetDuration = GetTargetDuration();

        // Get delta time from blackboard
        var deltaTime = blackboard.Get(BBKeys.DeltaTime, 0f);
        elapsed += deltaTime;

        if (elapsed >= targetDuration)
        {
            elapsed = 0f;
            randomizedDuration = null;
            return SetState(BTNodeState.Success);
        }

        return SetState(BTNodeState.Running);
    }

    private float GetTargetDuration()
    {
        if (!UseRandomDuration)
        {
            return Duration;
        }

        // Calculate randomized duration once per execution
        randomizedDuration ??= MinDuration + (Random.Shared.NextSingle() * (MaxDuration - MinDuration));
        return randomizedDuration.Value;
    }
}
