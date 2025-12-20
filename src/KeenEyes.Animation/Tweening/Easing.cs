using KeenEyes.Common;

namespace KeenEyes.Animation.Tweening;

/// <summary>
/// Provides easing functions for smooth animation interpolation.
/// </summary>
/// <remarks>
/// All easing functions take a normalized time value (0-1) and return
/// an eased value. The output may exceed 0-1 for effects like elastic and back.
/// </remarks>
public static class Easing
{
    private const float Pi = MathF.PI;
    private const float HalfPi = MathF.PI / 2f;

    /// <summary>
    /// Evaluates the specified easing function at time t.
    /// </summary>
    /// <param name="type">The easing type.</param>
    /// <param name="t">The normalized time (0-1).</param>
    /// <returns>The eased value.</returns>
    public static float Evaluate(EaseType type, float t) => type switch
    {
        EaseType.Linear => t,

        EaseType.QuadIn => QuadIn(t),
        EaseType.QuadOut => QuadOut(t),
        EaseType.QuadInOut => QuadInOut(t),

        EaseType.CubicIn => CubicIn(t),
        EaseType.CubicOut => CubicOut(t),
        EaseType.CubicInOut => CubicInOut(t),

        EaseType.QuartIn => QuartIn(t),
        EaseType.QuartOut => QuartOut(t),
        EaseType.QuartInOut => QuartInOut(t),

        EaseType.QuintIn => QuintIn(t),
        EaseType.QuintOut => QuintOut(t),
        EaseType.QuintInOut => QuintInOut(t),

        EaseType.SineIn => SineIn(t),
        EaseType.SineOut => SineOut(t),
        EaseType.SineInOut => SineInOut(t),

        EaseType.ExpoIn => ExpoIn(t),
        EaseType.ExpoOut => ExpoOut(t),
        EaseType.ExpoInOut => ExpoInOut(t),

        EaseType.CircIn => CircIn(t),
        EaseType.CircOut => CircOut(t),
        EaseType.CircInOut => CircInOut(t),

        EaseType.ElasticIn => ElasticIn(t),
        EaseType.ElasticOut => ElasticOut(t),
        EaseType.ElasticInOut => ElasticInOut(t),

        EaseType.BackIn => BackIn(t),
        EaseType.BackOut => BackOut(t),
        EaseType.BackInOut => BackInOut(t),

        EaseType.BounceIn => BounceIn(t),
        EaseType.BounceOut => BounceOut(t),
        EaseType.BounceInOut => BounceInOut(t),

        _ => t
    };

    #region Quadratic

    /// <summary>Quadratic ease in.</summary>
    public static float QuadIn(float t) => t * t;

    /// <summary>Quadratic ease out.</summary>
    public static float QuadOut(float t) => t * (2f - t);

    /// <summary>Quadratic ease in and out.</summary>
    public static float QuadInOut(float t) =>
        t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    #endregion

    #region Cubic

    /// <summary>Cubic ease in.</summary>
    public static float CubicIn(float t) => t * t * t;

    /// <summary>Cubic ease out.</summary>
    public static float CubicOut(float t)
    {
        var f = t - 1f;
        return f * f * f + 1f;
    }

    /// <summary>Cubic ease in and out.</summary>
    public static float CubicInOut(float t) =>
        t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

    #endregion

    #region Quartic

    /// <summary>Quartic ease in.</summary>
    public static float QuartIn(float t) => t * t * t * t;

    /// <summary>Quartic ease out.</summary>
    public static float QuartOut(float t)
    {
        var f = t - 1f;
        return 1f - f * f * f * f;
    }

    /// <summary>Quartic ease in and out.</summary>
    public static float QuartInOut(float t)
    {
        if (t < 0.5f)
        {
            return 8f * t * t * t * t;
        }

        var f = t - 1f;
        return 1f - 8f * f * f * f * f;
    }

    #endregion

    #region Quintic

    /// <summary>Quintic ease in.</summary>
    public static float QuintIn(float t) => t * t * t * t * t;

    /// <summary>Quintic ease out.</summary>
    public static float QuintOut(float t)
    {
        var f = t - 1f;
        return 1f + f * f * f * f * f;
    }

    /// <summary>Quintic ease in and out.</summary>
    public static float QuintInOut(float t)
    {
        if (t < 0.5f)
        {
            return 16f * t * t * t * t * t;
        }

        var f = t - 1f;
        return 1f + 16f * f * f * f * f * f;
    }

    #endregion

    #region Sinusoidal

    /// <summary>Sinusoidal ease in.</summary>
    public static float SineIn(float t) => 1f - MathF.Cos(t * HalfPi);

    /// <summary>Sinusoidal ease out.</summary>
    public static float SineOut(float t) => MathF.Sin(t * HalfPi);

    /// <summary>Sinusoidal ease in and out.</summary>
    public static float SineInOut(float t) => 0.5f * (1f - MathF.Cos(Pi * t));

    #endregion

    #region Exponential

    /// <summary>Exponential ease in.</summary>
    public static float ExpoIn(float t) =>
        t.IsApproximatelyZero() ? 0f : MathF.Pow(2f, 10f * (t - 1f));

    /// <summary>Exponential ease out.</summary>
    public static float ExpoOut(float t) =>
        t.ApproximatelyEquals(1f) ? 1f : 1f - MathF.Pow(2f, -10f * t);

    /// <summary>Exponential ease in and out.</summary>
    public static float ExpoInOut(float t)
    {
        if (t.IsApproximatelyZero())
        {
            return 0f;
        }

        if (t.ApproximatelyEquals(1f))
        {
            return 1f;
        }

        if (t < 0.5f)
        {
            return 0.5f * MathF.Pow(2f, 20f * t - 10f);
        }

        return 1f - 0.5f * MathF.Pow(2f, -20f * t + 10f);
    }

    #endregion

    #region Circular

    /// <summary>Circular ease in.</summary>
    public static float CircIn(float t) => 1f - MathF.Sqrt(1f - t * t);

    /// <summary>Circular ease out.</summary>
    public static float CircOut(float t) => MathF.Sqrt((2f - t) * t);

    /// <summary>Circular ease in and out.</summary>
    public static float CircInOut(float t)
    {
        if (t < 0.5f)
        {
            return 0.5f * (1f - MathF.Sqrt(1f - 4f * t * t));
        }

        return 0.5f * (MathF.Sqrt(-((2f * t) - 3f) * ((2f * t) - 1f)) + 1f);
    }

    #endregion

    #region Elastic

    private const float ElasticAmplitude = 1f;
    private const float ElasticPeriod = 0.3f;

    /// <summary>Elastic ease in.</summary>
    public static float ElasticIn(float t)
    {
        if (t.IsApproximatelyZero() || t.ApproximatelyEquals(1f))
        {
            return t;
        }

        var s = ElasticPeriod / 4f;
        t -= 1f;
        return -(ElasticAmplitude * MathF.Pow(2f, 10f * t) * MathF.Sin((t - s) * (2f * Pi) / ElasticPeriod));
    }

    /// <summary>Elastic ease out.</summary>
    public static float ElasticOut(float t)
    {
        if (t.IsApproximatelyZero() || t.ApproximatelyEquals(1f))
        {
            return t;
        }

        var s = ElasticPeriod / 4f;
        return ElasticAmplitude * MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * Pi) / ElasticPeriod) + 1f;
    }

    /// <summary>Elastic ease in and out.</summary>
    public static float ElasticInOut(float t)
    {
        if (t.IsApproximatelyZero() || t.ApproximatelyEquals(1f))
        {
            return t;
        }

        var s = ElasticPeriod / 4f;
        t = t * 2f - 1f;

        if (t < 0f)
        {
            return -0.5f * (ElasticAmplitude * MathF.Pow(2f, 10f * t) * MathF.Sin((t - s) * (2f * Pi) / ElasticPeriod));
        }

        return ElasticAmplitude * MathF.Pow(2f, -10f * t) * MathF.Sin((t - s) * (2f * Pi) / ElasticPeriod) * 0.5f + 1f;
    }

    #endregion

    #region Back

    private const float BackOvershoot = 1.70158f;

    /// <summary>Back ease in.</summary>
    public static float BackIn(float t) => t * t * ((BackOvershoot + 1f) * t - BackOvershoot);

    /// <summary>Back ease out.</summary>
    public static float BackOut(float t)
    {
        t -= 1f;
        return t * t * ((BackOvershoot + 1f) * t + BackOvershoot) + 1f;
    }

    /// <summary>Back ease in and out.</summary>
    public static float BackInOut(float t)
    {
        var s = BackOvershoot * 1.525f;
        t *= 2f;

        if (t < 1f)
        {
            return 0.5f * (t * t * ((s + 1f) * t - s));
        }

        t -= 2f;
        return 0.5f * (t * t * ((s + 1f) * t + s) + 2f);
    }

    #endregion

    #region Bounce

    /// <summary>Bounce ease in.</summary>
    public static float BounceIn(float t) => 1f - BounceOut(1f - t);

    /// <summary>Bounce ease out.</summary>
    public static float BounceOut(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
        {
            return n1 * t * t;
        }

        if (t < 2f / d1)
        {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }

        if (t < 2.5f / d1)
        {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }

        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }

    /// <summary>Bounce ease in and out.</summary>
    public static float BounceInOut(float t) =>
        t < 0.5f
            ? 0.5f * BounceIn(t * 2f)
            : 0.5f * BounceOut(t * 2f - 1f) + 0.5f;

    #endregion
}
