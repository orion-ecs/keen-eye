namespace KeenEyes.AI.Utility;

/// <summary>
/// Exponential response curve: y = x^exponent
/// </summary>
/// <remarks>
/// <para>
/// Exponential curves produce accelerating or decelerating change:
/// </para>
/// <list type="bullet">
/// <item><description>Exponent &gt; 1: Slow start, fast finish (favor high values)</description></item>
/// <item><description>Exponent = 1: Linear (same as identity)</description></item>
/// <item><description>Exponent &lt; 1: Fast start, slow finish (favor low values)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Quadratic: heavily favor high input values
/// var favorHigh = new ExponentialCurve { Exponent = 2f };
///
/// // Square root: heavily favor low input values
/// var favorLow = new ExponentialCurve { Exponent = 0.5f };
/// </code>
/// </example>
public sealed class ExponentialCurve : ResponseCurve
{
    /// <summary>
    /// Gets or sets the exponent.
    /// </summary>
    public float Exponent { get; set; } = 2f;

    /// <summary>
    /// Gets or sets whether to clamp output to [0, 1].
    /// </summary>
    public bool ClampOutput { get; set; } = true;

    /// <inheritdoc/>
    public override float Evaluate(float input)
    {
        // Clamp input to avoid negative base issues
        var clampedInput = Math.Clamp(input, 0f, 1f);
        var result = MathF.Pow(clampedInput, Exponent);
        return ClampOutput ? Clamp01(result) : result;
    }
}
