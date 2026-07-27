namespace KeenEyes.AI.BehaviorTree.Decorators;

/// <summary>
/// Decorator that rate-limits child execution with a time-based cooldown.
/// </summary>
/// <remarks>
/// <para>
/// Cooldown prevents its child from executing too frequently:
/// </para>
/// <list type="bullet">
/// <item><description>If cooldown is active, returns Failure immediately</description></item>
/// <item><description>If cooldown is inactive, executes child normally</description></item>
/// <item><description>On child Success, starts the cooldown timer</description></item>
/// </list>
/// <para>
/// Use for abilities with cooldowns: "Attack only every 2 seconds."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Special attack with 5 second cooldown
/// var cooldown = new Cooldown
/// {
///     Duration = 5f,
///     Child = new ActionNode { Action = new SpecialAttackAction() }
/// };
/// </code>
/// </example>
public sealed class Cooldown : DecoratorNode
{

    /// <summary>
    /// Gets or sets the cooldown duration in seconds.
    /// </summary>
    public float Duration { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether to start the cooldown on failure as well as success.
    /// </summary>
    /// <remarks>
    /// If true, the cooldown starts when the child completes (Success or Failure).
    /// If false (default), the cooldown only starts on Success.
    /// </remarks>
    public bool CooldownOnFailure { get; set; }

    /// <inheritdoc/>
    public override void Reset(Blackboard blackboard)
    {
        base.Reset(blackboard);
        // Note: We don't reset LastExecutionTime here because
        // cooldowns should persist across tree resets
    }

    /// <summary>
    /// Resets the cooldown timer for one entity, allowing immediate execution.
    /// </summary>
    /// <param name="blackboard">The blackboard of the entity whose cooldown to clear.</param>
    public void ResetCooldown(Blackboard blackboard)
    {
        ((CooldownMemory)GetMemory(blackboard)).LastExecutionTime = float.MinValue;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(blackboard, BTNodeState.Failure);
        }

        var memory = (CooldownMemory)GetMemory(blackboard);

        // Get current time from blackboard
        var currentTime = blackboard.Get(BBKeys.Time, 0f);

        // Check if still on cooldown
        if (currentTime - memory.LastExecutionTime < Duration)
        {
            return SetState(blackboard, BTNodeState.Failure);
        }

        var state = Child.Execute(entity, blackboard, world);

        // Start cooldown on completion (based on settings)
        if (state == BTNodeState.Success || (CooldownOnFailure && state == BTNodeState.Failure))
        {
            memory.LastExecutionTime = currentTime;
        }

        return SetState(blackboard, state);
    }

    /// <inheritdoc/>
    protected internal override BTNodeMemory GetMemory(Blackboard blackboard)
        => blackboard.GetMemory<CooldownMemory>(this);

    /// <summary>
    /// Per-entity execution memory for <see cref="Cooldown"/>.
    /// </summary>
    internal sealed class CooldownMemory : BTNodeMemory
    {
        public float LastExecutionTime { get; set; } = float.MinValue;
    }
}
