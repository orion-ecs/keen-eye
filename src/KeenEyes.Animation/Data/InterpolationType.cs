namespace KeenEyes.Animation.Data;

/// <summary>
/// Specifies how values are interpolated between keyframes.
/// </summary>
/// <remarks>
/// <para>
/// These interpolation types correspond to glTF animation sampler interpolation modes.
/// Each type defines how the value changes from one keyframe to the next.
/// </para>
/// </remarks>
public enum InterpolationType
{
    /// <summary>
    /// Linear interpolation between keyframes.
    /// </summary>
    /// <remarks>
    /// The value changes at a constant rate between keyframes.
    /// This is the default and most common interpolation type.
    /// </remarks>
    Linear,

    /// <summary>
    /// Step/constant interpolation - value jumps instantly at keyframes.
    /// </summary>
    /// <remarks>
    /// The value remains constant until the next keyframe, then changes immediately.
    /// Useful for discrete state changes like visibility toggles or frame indices.
    /// </remarks>
    Step,

    /// <summary>
    /// Cubic spline interpolation with in/out tangents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Uses Hermite interpolation with explicit tangent values at each keyframe.
    /// This provides smooth curves with control over the shape of the interpolation.
    /// </para>
    /// <para>
    /// In glTF, cubic spline keyframes store three values: [inTangent, value, outTangent].
    /// The tangents control the curve's slope as it enters and exits the keyframe.
    /// </para>
    /// </remarks>
    CubicSpline
}
