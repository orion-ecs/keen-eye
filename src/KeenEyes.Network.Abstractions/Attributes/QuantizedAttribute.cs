namespace KeenEyes.Network;

/// <summary>
/// Specifies quantization parameters for a numeric field in a replicated component.
/// </summary>
/// <remarks>
/// <para>
/// Quantization converts floating-point values to integers within a bounded range,
/// significantly reducing bandwidth. The tradeoff is precision loss, which is
/// acceptable for most gameplay values.
/// </para>
/// <para>
/// The number of bits required is calculated as: ceil(log2((Max - Min) / Resolution + 1))
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Component]
/// [Replicated]
/// public partial struct Position
/// {
///     // Range [-1000, 1000] with 0.01 precision = 200,001 values = 18 bits
///     // vs 32 bits for raw float (44% bandwidth savings)
///     [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
///     public float X;
///
///     [Quantized(Min = -1000, Max = 1000, Resolution = 0.01f)]
///     public float Y;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class QuantizedAttribute(float min, float max, float resolution) : Attribute
{
    /// <summary>
    /// Gets the minimum value of the quantization range.
    /// </summary>
    /// <remarks>
    /// Values below this will be clamped to Min.
    /// </remarks>
    public float Min { get; } = min;

    /// <summary>
    /// Gets the maximum value of the quantization range.
    /// </summary>
    /// <remarks>
    /// Values above this will be clamped to Max.
    /// </remarks>
    public float Max { get; } = max;

    /// <summary>
    /// Gets the resolution (precision) of the quantization.
    /// </summary>
    /// <remarks>
    /// Smaller values provide more precision but require more bits.
    /// For example, 0.01f means values are rounded to the nearest 0.01.
    /// </remarks>
    public float Resolution { get; } = resolution;

    /// <summary>
    /// Gets the number of bits required to represent values in this range.
    /// </summary>
    public int BitsRequired
    {
        get
        {
            var range = Max - Min;
            var steps = (int)Math.Ceiling(range / Resolution) + 1;
            return (int)Math.Ceiling(Math.Log2(steps));
        }
    }
}
