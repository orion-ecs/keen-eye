namespace KeenEyes.AI.Utility;

/// <summary>
/// Consideration input that reads health percentage from the blackboard.
/// </summary>
/// <remarks>
/// <para>
/// This input reads current and max health from the blackboard and returns
/// the health percentage (0-1).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var healthInput = new HealthInput
/// {
///     CurrentHealthKey = BBKeys.Health,
///     MaxHealthKey = BBKeys.MaxHealth
/// };
/// </code>
/// </example>
public sealed class HealthInput : IConsiderationInput
{
    /// <summary>
    /// Gets or sets the blackboard key for current health.
    /// </summary>
    public string CurrentHealthKey { get; set; } = BBKeys.Health;

    /// <summary>
    /// Gets or sets the blackboard key for maximum health.
    /// </summary>
    public string MaxHealthKey { get; set; } = BBKeys.MaxHealth;

    /// <summary>
    /// Gets or sets the default value when health keys are not found.
    /// </summary>
    public float DefaultValue { get; set; } = 1f;

    /// <inheritdoc/>
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        var current = blackboard.Get(CurrentHealthKey, 0f);
        var max = blackboard.Get(MaxHealthKey, 0f);

        if (max <= 0f)
        {
            return DefaultValue;
        }

        return Math.Clamp(current / max, 0f, 1f);
    }
}
