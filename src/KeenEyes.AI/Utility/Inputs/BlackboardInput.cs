namespace KeenEyes.AI.Utility;

/// <summary>
/// Consideration input that reads a float value from the blackboard.
/// </summary>
/// <remarks>
/// <para>
/// This input reads a float value from the blackboard and optionally normalizes it.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var ammoInput = new BlackboardInput
/// {
///     Key = "Ammo",
///     MaxValue = 30, // Normalize to 0-1 based on max ammo
///     DefaultValue = 0
/// };
/// </code>
/// </example>
public sealed class BlackboardInput : IConsiderationInput
{
    /// <summary>
    /// Gets or sets the blackboard key to read.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum value for normalization.
    /// </summary>
    /// <remarks>
    /// If greater than 0, the raw value is divided by this to produce a 0-1 range.
    /// If 0 or negative, the raw value is returned directly.
    /// </remarks>
    public float MaxValue { get; set; }

    /// <summary>
    /// Gets or sets the default value when the key is not found.
    /// </summary>
    public float DefaultValue { get; set; }

    /// <inheritdoc/>
    public float GetValue(Entity entity, Blackboard blackboard, IWorld world)
    {
        var value = blackboard.Get(Key, DefaultValue);

        if (MaxValue > 0)
        {
            return Math.Clamp(value / MaxValue, 0f, 1f);
        }

        return value;
    }
}
