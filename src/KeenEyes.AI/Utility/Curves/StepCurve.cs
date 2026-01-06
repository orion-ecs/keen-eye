namespace KeenEyes.AI.Utility;

/// <summary>
/// Step response curve: y = (x >= threshold) ? highValue : lowValue
/// </summary>
/// <remarks>
/// <para>
/// Step curves provide binary output based on a threshold.
/// Useful for conditions that should be "all or nothing."
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Only consider if health is above 50%
/// var healthGate = new StepCurve
/// {
///     Threshold = 0.5f,
///     LowValue = 0f,
///     HighValue = 1f
/// };
///
/// // Invert: Only consider if health is BELOW 30%
/// var lowHealthOnly = new StepCurve
/// {
///     Threshold = 0.3f,
///     LowValue = 1f,
///     HighValue = 0f
/// };
/// </code>
/// </example>
public sealed class StepCurve : ResponseCurve
{
    /// <summary>
    /// Gets or sets the threshold value.
    /// </summary>
    public float Threshold { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the output value when input is below threshold.
    /// </summary>
    public float LowValue { get; set; }

    /// <summary>
    /// Gets or sets the output value when input is at or above threshold.
    /// </summary>
    public float HighValue { get; set; } = 1f;

    /// <inheritdoc/>
    public override float Evaluate(float input)
    {
        return input >= Threshold ? HighValue : LowValue;
    }
}
