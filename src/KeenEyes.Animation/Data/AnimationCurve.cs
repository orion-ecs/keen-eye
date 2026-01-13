using System.Numerics;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Data;

/// <summary>
/// An animation curve that stores keyframes and provides interpolated values over time.
/// </summary>
/// <remarks>
/// Keyframes must be added in ascending time order for correct interpolation.
/// </remarks>
public sealed class FloatCurve
{
    private readonly List<FloatKeyframe> keyframes = [];

    /// <summary>
    /// Gets the keyframes in this curve.
    /// </summary>
    public IReadOnlyList<FloatKeyframe> Keyframes => keyframes;

    /// <summary>
    /// Gets the duration of this curve (time of the last keyframe).
    /// </summary>
    public float Duration => keyframes.Count > 0 ? keyframes[^1].Time : 0f;

    /// <summary>
    /// Adds a keyframe to the curve.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The value at the keyframe.</param>
    /// <param name="inTangent">The incoming tangent.</param>
    /// <param name="outTangent">The outgoing tangent.</param>
    public void AddKeyframe(float time, float value, float inTangent = 0f, float outTangent = 0f)
    {
        keyframes.Add(new FloatKeyframe(time, value, inTangent, outTangent));
    }

    /// <summary>
    /// Evaluates the curve at the specified time.
    /// </summary>
    /// <param name="time">The time to evaluate at.</param>
    /// <returns>The interpolated value.</returns>
    public float Evaluate(float time)
    {
        if (keyframes.Count == 0)
        {
            return 0f;
        }

        if (keyframes.Count == 1 || time <= keyframes[0].Time)
        {
            return keyframes[0].Value;
        }

        if (time >= keyframes[^1].Time)
        {
            return keyframes[^1].Value;
        }

        // Find the two keyframes to interpolate between
        for (var i = 0; i < keyframes.Count - 1; i++)
        {
            var k0 = keyframes[i];
            var k1 = keyframes[i + 1];

            if (time >= k0.Time && time <= k1.Time)
            {
                var t = (time - k0.Time) / (k1.Time - k0.Time);
                return HermiteInterpolate(k0.Value, k0.OutTangent, k1.Value, k1.InTangent, t);
            }
        }

        return keyframes[^1].Value;
    }

    private static float HermiteInterpolate(float v0, float t0, float v1, float t1, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;

        var h00 = 2 * t3 - 3 * t2 + 1;
        var h10 = t3 - 2 * t2 + t;
        var h01 = -2 * t3 + 3 * t2;
        var h11 = t3 - t2;

        return h00 * v0 + h10 * t0 + h01 * v1 + h11 * t1;
    }
}

/// <summary>
/// An animation curve for Vector3 values with configurable interpolation.
/// </summary>
public sealed class Vector3Curve
{
    private readonly List<Vector3Keyframe> keyframes = [];
    private readonly List<CubicSplineVector3Keyframe> cubicKeyframes = [];

    /// <summary>
    /// Gets the keyframes in this curve (for Linear/Step interpolation).
    /// </summary>
    public IReadOnlyList<Vector3Keyframe> Keyframes => keyframes;

    /// <summary>
    /// Gets the cubic spline keyframes (for CubicSpline interpolation).
    /// </summary>
    public IReadOnlyList<CubicSplineVector3Keyframe> CubicKeyframes => cubicKeyframes;

    /// <summary>
    /// Gets or sets the interpolation type for this curve.
    /// </summary>
    public InterpolationType Interpolation { get; set; } = InterpolationType.Linear;

    /// <summary>
    /// Gets the duration of this curve.
    /// </summary>
    public float Duration
    {
        get
        {
            if (Interpolation == InterpolationType.CubicSpline && cubicKeyframes.Count > 0)
            {
                return cubicKeyframes[^1].Time;
            }

            return keyframes.Count > 0 ? keyframes[^1].Time : 0f;
        }
    }

    /// <summary>
    /// Adds a keyframe to the curve (for Linear/Step interpolation).
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The value at the keyframe.</param>
    public void AddKeyframe(float time, Vector3 value)
    {
        keyframes.Add(new Vector3Keyframe(time, value));
    }

    /// <summary>
    /// Adds a cubic spline keyframe with tangents.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The value at the keyframe.</param>
    /// <param name="inTangent">The incoming tangent.</param>
    /// <param name="outTangent">The outgoing tangent.</param>
    public void AddCubicKeyframe(float time, Vector3 value, Vector3 inTangent, Vector3 outTangent)
    {
        cubicKeyframes.Add(new CubicSplineVector3Keyframe(time, value, inTangent, outTangent));
    }

    /// <summary>
    /// Evaluates the curve at the specified time.
    /// </summary>
    /// <param name="time">The time to evaluate at.</param>
    /// <returns>The interpolated value.</returns>
    public Vector3 Evaluate(float time)
    {
        return Interpolation switch
        {
            InterpolationType.Step => EvaluateStep(time),
            InterpolationType.CubicSpline => EvaluateCubicSpline(time),
            _ => EvaluateLinear(time)
        };
    }

    private Vector3 EvaluateLinear(float time)
    {
        if (keyframes.Count == 0)
        {
            return Vector3.Zero;
        }

        if (keyframes.Count == 1 || time <= keyframes[0].Time)
        {
            return keyframes[0].Value;
        }

        if (time >= keyframes[^1].Time)
        {
            return keyframes[^1].Value;
        }

        for (var i = 0; i < keyframes.Count - 1; i++)
        {
            var k0 = keyframes[i];
            var k1 = keyframes[i + 1];

            if (time >= k0.Time && time <= k1.Time)
            {
                var t = (time - k0.Time) / (k1.Time - k0.Time);
                return Vector3.Lerp(k0.Value, k1.Value, t);
            }
        }

        return keyframes[^1].Value;
    }

    private Vector3 EvaluateStep(float time)
    {
        if (keyframes.Count == 0)
        {
            return Vector3.Zero;
        }

        if (time <= keyframes[0].Time)
        {
            return keyframes[0].Value;
        }

        // Find the last keyframe at or before the given time
        for (var i = keyframes.Count - 1; i >= 0; i--)
        {
            if (keyframes[i].Time <= time)
            {
                return keyframes[i].Value;
            }
        }

        return keyframes[0].Value;
    }

    private Vector3 EvaluateCubicSpline(float time)
    {
        if (cubicKeyframes.Count == 0)
        {
            // Fall back to linear keyframes if no cubic keyframes
            return EvaluateLinear(time);
        }

        if (cubicKeyframes.Count == 1 || time <= cubicKeyframes[0].Time)
        {
            return cubicKeyframes[0].Value;
        }

        if (time >= cubicKeyframes[^1].Time)
        {
            return cubicKeyframes[^1].Value;
        }

        for (var i = 0; i < cubicKeyframes.Count - 1; i++)
        {
            var k0 = cubicKeyframes[i];
            var k1 = cubicKeyframes[i + 1];

            if (time >= k0.Time && time <= k1.Time)
            {
                var duration = k1.Time - k0.Time;
                var t = (time - k0.Time) / duration;
                return HermiteInterpolateVector3(k0.Value, k0.OutTangent * duration, k1.Value, k1.InTangent * duration, t);
            }
        }

        return cubicKeyframes[^1].Value;
    }

    private static Vector3 HermiteInterpolateVector3(Vector3 v0, Vector3 t0, Vector3 v1, Vector3 t1, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;

        var h00 = 2 * t3 - 3 * t2 + 1;
        var h10 = t3 - 2 * t2 + t;
        var h01 = -2 * t3 + 3 * t2;
        var h11 = t3 - t2;

        return h00 * v0 + h10 * t0 + h01 * v1 + h11 * t1;
    }
}

/// <summary>
/// An animation curve for Quaternion rotation values with configurable interpolation.
/// </summary>
public sealed class QuaternionCurve
{
    private readonly List<QuaternionKeyframe> keyframes = [];
    private readonly List<CubicSplineQuaternionKeyframe> cubicKeyframes = [];

    /// <summary>
    /// Gets the keyframes in this curve (for Linear/Step interpolation).
    /// </summary>
    public IReadOnlyList<QuaternionKeyframe> Keyframes => keyframes;

    /// <summary>
    /// Gets the cubic spline keyframes (for CubicSpline interpolation).
    /// </summary>
    public IReadOnlyList<CubicSplineQuaternionKeyframe> CubicKeyframes => cubicKeyframes;

    /// <summary>
    /// Gets or sets the interpolation type for this curve.
    /// </summary>
    public InterpolationType Interpolation { get; set; } = InterpolationType.Linear;

    /// <summary>
    /// Gets the duration of this curve.
    /// </summary>
    public float Duration
    {
        get
        {
            if (Interpolation == InterpolationType.CubicSpline && cubicKeyframes.Count > 0)
            {
                return cubicKeyframes[^1].Time;
            }

            return keyframes.Count > 0 ? keyframes[^1].Time : 0f;
        }
    }

    /// <summary>
    /// Adds a keyframe to the curve (for Linear/Step interpolation).
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The rotation at the keyframe.</param>
    public void AddKeyframe(float time, Quaternion value)
    {
        keyframes.Add(new QuaternionKeyframe(time, value));
    }

    /// <summary>
    /// Adds a cubic spline keyframe with tangents.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The rotation at the keyframe.</param>
    /// <param name="inTangent">The incoming tangent.</param>
    /// <param name="outTangent">The outgoing tangent.</param>
    public void AddCubicKeyframe(float time, Quaternion value, Quaternion inTangent, Quaternion outTangent)
    {
        cubicKeyframes.Add(new CubicSplineQuaternionKeyframe(time, value, inTangent, outTangent));
    }

    /// <summary>
    /// Evaluates the curve at the specified time.
    /// </summary>
    /// <param name="time">The time to evaluate at.</param>
    /// <returns>The interpolated rotation.</returns>
    public Quaternion Evaluate(float time)
    {
        return Interpolation switch
        {
            InterpolationType.Step => EvaluateStep(time),
            InterpolationType.CubicSpline => EvaluateCubicSpline(time),
            _ => EvaluateLinear(time)
        };
    }

    private Quaternion EvaluateLinear(float time)
    {
        if (keyframes.Count == 0)
        {
            return Quaternion.Identity;
        }

        if (keyframes.Count == 1 || time <= keyframes[0].Time)
        {
            return keyframes[0].Value;
        }

        if (time >= keyframes[^1].Time)
        {
            return keyframes[^1].Value;
        }

        for (var i = 0; i < keyframes.Count - 1; i++)
        {
            var k0 = keyframes[i];
            var k1 = keyframes[i + 1];

            if (time >= k0.Time && time <= k1.Time)
            {
                var t = (time - k0.Time) / (k1.Time - k0.Time);
                return Quaternion.Slerp(k0.Value, k1.Value, t);
            }
        }

        return keyframes[^1].Value;
    }

    private Quaternion EvaluateStep(float time)
    {
        if (keyframes.Count == 0)
        {
            return Quaternion.Identity;
        }

        if (time <= keyframes[0].Time)
        {
            return keyframes[0].Value;
        }

        // Find the last keyframe at or before the given time
        for (var i = keyframes.Count - 1; i >= 0; i--)
        {
            if (keyframes[i].Time <= time)
            {
                return keyframes[i].Value;
            }
        }

        return keyframes[0].Value;
    }

    private Quaternion EvaluateCubicSpline(float time)
    {
        if (cubicKeyframes.Count == 0)
        {
            // Fall back to linear keyframes if no cubic keyframes
            return EvaluateLinear(time);
        }

        if (cubicKeyframes.Count == 1 || time <= cubicKeyframes[0].Time)
        {
            return cubicKeyframes[0].Value;
        }

        if (time >= cubicKeyframes[^1].Time)
        {
            return cubicKeyframes[^1].Value;
        }

        for (var i = 0; i < cubicKeyframes.Count - 1; i++)
        {
            var k0 = cubicKeyframes[i];
            var k1 = cubicKeyframes[i + 1];

            if (time >= k0.Time && time <= k1.Time)
            {
                var duration = k1.Time - k0.Time;
                var t = (time - k0.Time) / duration;

                // For quaternions, use normalized quaternion lerp (nlerp) with cubic hermite scalar
                // This is an approximation since true cubic spline quaternion interpolation is complex
                var result = HermiteInterpolateQuaternion(k0.Value, k0.OutTangent, k1.Value, k1.InTangent, t, duration);
                return Quaternion.Normalize(result);
            }
        }

        return cubicKeyframes[^1].Value;
    }

    private static Quaternion HermiteInterpolateQuaternion(
        Quaternion v0, Quaternion t0, Quaternion v1, Quaternion t1, float t, float duration)
    {
        // glTF cubic spline for quaternions uses component-wise Hermite interpolation
        // followed by normalization. Tangents are scaled by the time delta.
        var t2 = t * t;
        var t3 = t2 * t;

        var h00 = 2 * t3 - 3 * t2 + 1;
        var h10 = t3 - 2 * t2 + t;
        var h01 = -2 * t3 + 3 * t2;
        var h11 = t3 - t2;

        // Scale tangents by duration as per glTF spec
        var scaledT0 = new Quaternion(t0.X * duration, t0.Y * duration, t0.Z * duration, t0.W * duration);
        var scaledT1 = new Quaternion(t1.X * duration, t1.Y * duration, t1.Z * duration, t1.W * duration);

        return new Quaternion(
            h00 * v0.X + h10 * scaledT0.X + h01 * v1.X + h11 * scaledT1.X,
            h00 * v0.Y + h10 * scaledT0.Y + h01 * v1.Y + h11 * scaledT1.Y,
            h00 * v0.Z + h10 * scaledT0.Z + h01 * v1.Z + h11 * scaledT1.Z,
            h00 * v0.W + h10 * scaledT0.W + h01 * v1.W + h11 * scaledT1.W);
    }
}
