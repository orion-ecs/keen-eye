namespace KeenEyes.Particles.Data;

/// <summary>
/// Pre-sampled curve for efficient evaluation.
/// Stored as a fixed-size lookup table for AOT compatibility.
/// </summary>
public readonly struct ParticleCurve
{
    /// <summary>
    /// Number of samples in the lookup table.
    /// </summary>
    public const int SampleCount = 64;

    private readonly float[] samples;

    private ParticleCurve(float[] samples)
    {
        this.samples = samples;
    }

    /// <summary>
    /// Gets the raw samples array. Returns null for uninitialized curves.
    /// </summary>
    internal float[]? Samples => samples;

    /// <summary>
    /// Evaluates the curve at normalized time t (0-1).
    /// Uses linear interpolation between samples.
    /// </summary>
    /// <param name="t">The normalized time (0-1).</param>
    /// <returns>The interpolated value at time t.</returns>
    public float Evaluate(float t)
    {
        if (samples == null)
        {
            return 1f;
        }

        t = Math.Clamp(t, 0f, 1f);
        var scaledT = t * (SampleCount - 1);
        var index = (int)scaledT;
        var frac = scaledT - index;

        if (index >= SampleCount - 1)
        {
            return samples[SampleCount - 1];
        }

        return samples[index] + (samples[index + 1] - samples[index]) * frac;
    }

    /// <summary>
    /// Creates a constant curve that returns the same value for all t.
    /// </summary>
    /// <param name="value">The constant value.</param>
    /// <returns>A constant curve.</returns>
    public static ParticleCurve Constant(float value)
    {
        var samples = new float[SampleCount];
        Array.Fill(samples, value);
        return new ParticleCurve(samples);
    }

    /// <summary>
    /// Creates a linear fade-out curve (1 at t=0, 0 at t=1).
    /// </summary>
    /// <returns>A linear fade-out curve.</returns>
    public static ParticleCurve LinearFadeOut()
    {
        var samples = new float[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            samples[i] = 1f - (float)i / (SampleCount - 1);
        }
        return new ParticleCurve(samples);
    }

    /// <summary>
    /// Creates a linear fade-in curve (0 at t=0, 1 at t=1).
    /// </summary>
    /// <returns>A linear fade-in curve.</returns>
    public static ParticleCurve LinearFadeIn()
    {
        var samples = new float[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            samples[i] = (float)i / (SampleCount - 1);
        }
        return new ParticleCurve(samples);
    }

    /// <summary>
    /// Creates a quadratic ease-out curve (starts fast, slows down).
    /// </summary>
    /// <returns>An ease-out curve.</returns>
    public static ParticleCurve EaseOut()
    {
        var samples = new float[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = 1f - t * t;
        }
        return new ParticleCurve(samples);
    }

    /// <summary>
    /// Creates a quadratic ease-in curve (starts slow, speeds up).
    /// </summary>
    /// <returns>An ease-in curve.</returns>
    public static ParticleCurve EaseIn()
    {
        var samples = new float[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = t * t;
        }
        return new ParticleCurve(samples);
    }

    /// <summary>
    /// Creates a curve from control points using linear interpolation.
    /// </summary>
    /// <param name="points">Array of (time, value) tuples defining the curve. Times should be in [0, 1].</param>
    /// <returns>A curve sampled from the control points.</returns>
    public static ParticleCurve FromPoints(ReadOnlySpan<(float time, float value)> points)
    {
        var samples = new float[SampleCount];

        if (points.Length == 0)
        {
            Array.Fill(samples, 1f);
            return new ParticleCurve(samples);
        }

        if (points.Length == 1)
        {
            Array.Fill(samples, points[0].value);
            return new ParticleCurve(samples);
        }

        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = EvaluatePoints(t, points);
        }

        return new ParticleCurve(samples);
    }

    private static float EvaluatePoints(float t, ReadOnlySpan<(float time, float value)> points)
    {
        // Clamp to first/last values outside range
        if (t <= points[0].time)
        {
            return points[0].value;
        }

        if (t >= points[^1].time)
        {
            return points[^1].value;
        }

        // Find bracketing points
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (t >= points[i].time && t <= points[i + 1].time)
            {
                var segmentLength = points[i + 1].time - points[i].time;
                if (segmentLength <= 0)
                {
                    return points[i].value;
                }

                var localT = (t - points[i].time) / segmentLength;
                return points[i].value + (points[i + 1].value - points[i].value) * localT;
            }
        }

        return points[^1].value;
    }
}
