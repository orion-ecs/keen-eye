namespace KeenEyes.AI.Utility;

/// <summary>
/// Abstract base class for response curves that map input values to utility scores.
/// </summary>
/// <remarks>
/// <para>
/// Response curves transform raw input values (typically 0-1) into utility scores.
/// Different curve types produce different scoring behaviors:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="LinearCurve"/> - Constant rate of change</description></item>
/// <item><description><see cref="ExponentialCurve"/> - Accelerating/decelerating change</description></item>
/// <item><description><see cref="LogisticCurve"/> - S-shaped transition</description></item>
/// <item><description><see cref="StepCurve"/> - Binary threshold</description></item>
/// </list>
/// </remarks>
public abstract class ResponseCurve
{
    /// <summary>
    /// Evaluates the curve at the given input value.
    /// </summary>
    /// <param name="input">The input value (typically 0-1).</param>
    /// <returns>The utility score (typically 0-1, but may exceed this range).</returns>
    public abstract float Evaluate(float input);

    /// <summary>
    /// Clamps a value to the range [0, 1].
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <returns>The clamped value.</returns>
    protected static float Clamp01(float value) => Math.Clamp(value, 0f, 1f);
}
