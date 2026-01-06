namespace KeenEyes.AI.Utility;

/// <summary>
/// Consideration input that provides time-based values.
/// </summary>
/// <remarks>
/// <para>
/// This input calculates how long since an event occurred, normalized by a duration.
/// Useful for "time since last attack" or "time in current state" considerations.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Higher score as more time passes since last attack
/// var timeSinceAttack = new TimeInput
/// {
///     EventTimeKey = "LastAttackTime",
///     Duration = 5f // Fully charged after 5 seconds
/// };
/// </code>
/// </example>
public sealed class TimeInput : IConsiderationInput
{
    /// <summary>
    /// Gets or sets the blackboard key for the event timestamp.
    /// </summary>
    /// <remarks>
    /// This should contain the time (from <see cref="BBKeys.Time"/>) when the event occurred.
    /// </remarks>
    public string EventTimeKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration for normalization.
    /// </summary>
    /// <remarks>
    /// The time elapsed is divided by this value. Results greater than 1 are clamped.
    /// </remarks>
    public float Duration { get; set; } = 1f;

    /// <summary>
    /// Gets or sets whether to invert the result.
    /// </summary>
    /// <remarks>
    /// If true, returns (1 - normalizedTime), meaning the value starts at 1
    /// immediately after the event and decreases over time.
    /// </remarks>
    public bool Invert { get; set; }

    /// <summary>
    /// Gets or sets the value to return when no event time exists.
    /// </summary>
    public float NoEventValue { get; set; } = 1f;

    /// <inheritdoc/>
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        // Get current time
        var currentTime = blackboard.Get(BBKeys.Time, 0f);

        // Get event time
        if (!blackboard.TryGet<float>(EventTimeKey, out var eventTime))
        {
            return NoEventValue;
        }

        var elapsed = currentTime - eventTime;
        var normalized = Math.Clamp(elapsed / Duration, 0f, 1f);

        return Invert ? 1f - normalized : normalized;
    }
}
