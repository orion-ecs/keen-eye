using System.Numerics;

namespace KeenEyes.Animation.Data;

/// <summary>
/// Animation data for a single bone in a skeleton, containing position, rotation, and scale curves.
/// </summary>
/// <remarks>
/// Each curve is optional - if null, the bone uses its bind pose for that property.
/// </remarks>
public sealed class BoneTrack
{
    /// <summary>
    /// Gets or sets the name of the bone this track animates.
    /// </summary>
    public required string BoneName { get; init; }

    /// <summary>
    /// Gets the position animation curve, or null if position is not animated.
    /// </summary>
    public Vector3Curve? PositionCurve { get; init; }

    /// <summary>
    /// Gets the rotation animation curve, or null if rotation is not animated.
    /// </summary>
    public QuaternionCurve? RotationCurve { get; init; }

    /// <summary>
    /// Gets the scale animation curve, or null if scale is not animated.
    /// </summary>
    public Vector3Curve? ScaleCurve { get; init; }

    /// <summary>
    /// Gets the duration of this track (maximum duration of all curves).
    /// </summary>
    public float Duration
    {
        get
        {
            var duration = 0f;
            if (PositionCurve != null)
            {
                duration = Math.Max(duration, PositionCurve.Duration);
            }

            if (RotationCurve != null)
            {
                duration = Math.Max(duration, RotationCurve.Duration);
            }

            if (ScaleCurve != null)
            {
                duration = Math.Max(duration, ScaleCurve.Duration);
            }

            return duration;
        }
    }

    /// <summary>
    /// Samples the bone transform at the specified time.
    /// </summary>
    /// <param name="time">The time to sample at.</param>
    /// <param name="position">The sampled position.</param>
    /// <param name="rotation">The sampled rotation.</param>
    /// <param name="scale">The sampled scale.</param>
    public void Sample(float time, out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = PositionCurve?.Evaluate(time) ?? Vector3.Zero;
        rotation = RotationCurve?.Evaluate(time) ?? Quaternion.Identity;
        scale = ScaleCurve?.Evaluate(time) ?? Vector3.One;
    }
}
