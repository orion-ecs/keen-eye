namespace KeenEyes.AI.Utility;

/// <summary>
/// Logistic (sigmoid) response curve: y = 1 / (1 + e^(-steepness * (x - midpoint)))
/// </summary>
/// <remarks>
/// <para>
/// Logistic curves produce an S-shaped transition between low and high values.
/// The curve has three distinct regions:
/// </para>
/// <list type="bullet">
/// <item><description>Below midpoint: Low output (approaching 0)</description></item>
/// <item><description>At midpoint: Output = 0.5</description></item>
/// <item><description>Above midpoint: High output (approaching 1)</description></item>
/// </list>
/// <para>
/// The steepness controls how sharp the transition is. Higher values create
/// a more step-like behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Sharp transition at 30% health
/// var lowHealthTrigger = new LogisticCurve
/// {
///     Midpoint = 0.3f,
///     Steepness = 10f
/// };
/// </code>
/// </example>
public sealed class LogisticCurve : ResponseCurve
{
    /// <summary>
    /// Gets or sets the steepness of the transition.
    /// </summary>
    /// <remarks>
    /// Higher values create a sharper transition (more step-like).
    /// Values around 5-15 give smooth S-curves.
    /// Values above 20 are essentially step functions.
    /// </remarks>
    public float Steepness { get; set; } = 10f;

    /// <summary>
    /// Gets or sets the midpoint of the transition (where output = 0.5).
    /// </summary>
    public float Midpoint { get; set; } = 0.5f;

    /// <inheritdoc/>
    public override float Evaluate(float input)
    {
        var exponent = -Steepness * (input - Midpoint);
        return 1f / (1f + MathF.Exp(exponent));
    }
}
