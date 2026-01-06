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
    private float lastExecutionTime = float.MinValue;

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
    public override void Reset()
    {
        base.Reset();
        // Note: We don't reset lastExecutionTime here because
        // cooldowns should persist across tree resets
    }

    /// <summary>
    /// Resets the cooldown timer, allowing immediate execution.
    /// </summary>
    public void ResetCooldown()
    {
        lastExecutionTime = float.MinValue;
    }

    /// <inheritdoc/>
    public override BTNodeState Execute(Entity entity, Blackboard blackboard, IWorld world)
    {
        if (Child == null)
        {
            return SetState(BTNodeState.Failure);
        }

        // Get current time from blackboard
        var currentTime = blackboard.Get(BBKeys.Time, 0f);

        // Check if still on cooldown
        if (currentTime - lastExecutionTime < Duration)
        {
            return SetState(BTNodeState.Failure);
        }

        var state = Child.Execute(entity, blackboard, world);

        // Start cooldown on completion (based on settings)
        if (state == BTNodeState.Success || (CooldownOnFailure && state == BTNodeState.Failure))
        {
            lastExecutionTime = currentTime;
        }

        return SetState(state);
    }
}
