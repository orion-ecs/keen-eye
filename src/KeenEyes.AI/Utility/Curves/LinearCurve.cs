namespace KeenEyes.AI.Utility;

/// <summary>
/// Linear response curve: y = slope * (x - xShift) + yShift
/// </summary>
/// <remarks>
/// <para>
/// Linear curves provide a constant rate of change between input and output.
/// Common configurations:
/// </para>
/// <list type="bullet">
/// <item><description>Identity: slope=1, xShift=0, yShift=0 → y = x</description></item>
/// <item><description>Inverted: slope=-1, xShift=0, yShift=1 → y = 1 - x</description></item>
/// <item><description>Threshold: slope=2, xShift=0.5, yShift=0.5 → y = 2*(x-0.5)+0.5</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Higher score when closer (distance input is normalized 0=close, 1=far)
/// var closerIsBetter = new LinearCurve { Slope = -1f, YShift = 1f };
///
/// // Equal weight for all values
/// var identity = new LinearCurve { Slope = 1f };
/// </code>
/// </example>
public sealed class LinearCurve : ResponseCurve
{
    /// <summary>
    /// Gets or sets the slope of the line.
    /// </summary>
    /// <remarks>
    /// Positive slope: increasing input = increasing output.
    /// Negative slope: increasing input = decreasing output.
    /// </remarks>
    public float Slope { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the horizontal shift.
    /// </summary>
    public float XShift { get; set; }

    /// <summary>
    /// Gets or sets the vertical shift.
    /// </summary>
    public float YShift { get; set; }

    /// <summary>
    /// Gets or sets whether to clamp output to [0, 1].
    /// </summary>
    public bool ClampOutput { get; set; } = true;

    /// <inheritdoc/>
    public override float Evaluate(float input)
    {
        var result = (Slope * (input - XShift)) + YShift;
        return ClampOutput ? Clamp01(result) : result;
    }
}
