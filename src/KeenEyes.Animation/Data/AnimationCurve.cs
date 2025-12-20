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
/// An animation curve for Vector3 values.
/// </summary>
public sealed class Vector3Curve
{
    private readonly List<Vector3Keyframe> keyframes = [];

    /// <summary>
    /// Gets the keyframes in this curve.
    /// </summary>
    public IReadOnlyList<Vector3Keyframe> Keyframes => keyframes;

    /// <summary>
    /// Gets the duration of this curve.
    /// </summary>
    public float Duration => keyframes.Count > 0 ? keyframes[^1].Time : 0f;

    /// <summary>
    /// Adds a keyframe to the curve.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The value at the keyframe.</param>
    public void AddKeyframe(float time, Vector3 value)
    {
        keyframes.Add(new Vector3Keyframe(time, value));
    }

    /// <summary>
    /// Evaluates the curve at the specified time using linear interpolation.
    /// </summary>
    /// <param name="time">The time to evaluate at.</param>
    /// <returns>The interpolated value.</returns>
    public Vector3 Evaluate(float time)
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
}

/// <summary>
/// An animation curve for Quaternion rotation values.
/// </summary>
public sealed class QuaternionCurve
{
    private readonly List<QuaternionKeyframe> keyframes = [];

    /// <summary>
    /// Gets the keyframes in this curve.
    /// </summary>
    public IReadOnlyList<QuaternionKeyframe> Keyframes => keyframes;

    /// <summary>
    /// Gets the duration of this curve.
    /// </summary>
    public float Duration => keyframes.Count > 0 ? keyframes[^1].Time : 0f;

    /// <summary>
    /// Adds a keyframe to the curve.
    /// </summary>
    /// <param name="time">The time of the keyframe.</param>
    /// <param name="value">The rotation at the keyframe.</param>
    public void AddKeyframe(float time, Quaternion value)
    {
        keyframes.Add(new QuaternionKeyframe(time, value));
    }

    /// <summary>
    /// Evaluates the curve at the specified time using spherical interpolation.
    /// </summary>
    /// <param name="time">The time to evaluate at.</param>
    /// <returns>The interpolated rotation.</returns>
    public Quaternion Evaluate(float time)
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
}
