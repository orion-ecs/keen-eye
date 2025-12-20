using System.Numerics;

namespace KeenEyes.Particles.Data;

/// <summary>
/// Pre-sampled color gradient for efficient evaluation.
/// Stored as a fixed-size lookup table for AOT compatibility.
/// </summary>
public readonly struct ParticleGradient
{
    /// <summary>
    /// Number of samples in the lookup table.
    /// </summary>
    public const int SampleCount = 64;

    private readonly Vector4[] samples;

    private ParticleGradient(Vector4[] samples)
    {
        this.samples = samples;
    }

    /// <summary>
    /// Gets the raw samples array. Returns null for uninitialized gradients.
    /// </summary>
    internal Vector4[]? Samples => samples;

    /// <summary>
    /// Evaluates the gradient at normalized time t (0-1).
    /// Uses linear interpolation between samples.
    /// </summary>
    /// <param name="t">The normalized time (0-1).</param>
    /// <returns>The interpolated color at time t (RGBA).</returns>
    public Vector4 Evaluate(float t)
    {
        if (samples == null)
        {
            return Vector4.One;
        }

        t = Math.Clamp(t, 0f, 1f);
        var scaledT = t * (SampleCount - 1);
        var index = (int)scaledT;
        var frac = scaledT - index;

        if (index >= SampleCount - 1)
        {
            return samples[SampleCount - 1];
        }

        return Vector4.Lerp(samples[index], samples[index + 1], frac);
    }

    /// <summary>
    /// Creates a constant color gradient.
    /// </summary>
    /// <param name="color">The constant color (RGBA).</param>
    /// <returns>A constant gradient.</returns>
    public static ParticleGradient Constant(Vector4 color)
    {
        var samples = new Vector4[SampleCount];
        Array.Fill(samples, color);
        return new ParticleGradient(samples);
    }

    /// <summary>
    /// Creates a gradient that fades alpha from 1 to 0.
    /// </summary>
    /// <param name="color">The base color (RGB components, alpha will vary).</param>
    /// <returns>A fade-out gradient.</returns>
    public static ParticleGradient FadeOut(Vector4 color)
    {
        var samples = new Vector4[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = color with { W = color.W * (1f - t) };
        }
        return new ParticleGradient(samples);
    }

    /// <summary>
    /// Creates a gradient that fades alpha from 0 to 1.
    /// </summary>
    /// <param name="color">The base color (RGB components, alpha will vary).</param>
    /// <returns>A fade-in gradient.</returns>
    public static ParticleGradient FadeIn(Vector4 color)
    {
        var samples = new Vector4[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = color with { W = color.W * t };
        }
        return new ParticleGradient(samples);
    }

    /// <summary>
    /// Creates a gradient that interpolates between two colors.
    /// </summary>
    /// <param name="start">The start color (RGBA).</param>
    /// <param name="end">The end color (RGBA).</param>
    /// <returns>A two-color gradient.</returns>
    public static ParticleGradient TwoColor(Vector4 start, Vector4 end)
    {
        var samples = new Vector4[SampleCount];
        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = Vector4.Lerp(start, end, t);
        }
        return new ParticleGradient(samples);
    }

    /// <summary>
    /// Creates a gradient from control points using linear interpolation.
    /// </summary>
    /// <param name="points">Array of (time, color) tuples defining the gradient. Times should be in [0, 1].</param>
    /// <returns>A gradient sampled from the control points.</returns>
    public static ParticleGradient FromPoints(ReadOnlySpan<(float time, Vector4 color)> points)
    {
        var samples = new Vector4[SampleCount];

        if (points.Length == 0)
        {
            Array.Fill(samples, Vector4.One);
            return new ParticleGradient(samples);
        }

        if (points.Length == 1)
        {
            Array.Fill(samples, points[0].color);
            return new ParticleGradient(samples);
        }

        for (int i = 0; i < SampleCount; i++)
        {
            var t = (float)i / (SampleCount - 1);
            samples[i] = EvaluatePoints(t, points);
        }

        return new ParticleGradient(samples);
    }

    private static Vector4 EvaluatePoints(float t, ReadOnlySpan<(float time, Vector4 color)> points)
    {
        // Clamp to first/last values outside range
        if (t <= points[0].time)
        {
            return points[0].color;
        }

        if (t >= points[^1].time)
        {
            return points[^1].color;
        }

        // Find bracketing points
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (t >= points[i].time && t <= points[i + 1].time)
            {
                var segmentLength = points[i + 1].time - points[i].time;
                if (segmentLength <= 0)
                {
                    return points[i].color;
                }

                var localT = (t - points[i].time) / segmentLength;
                return Vector4.Lerp(points[i].color, points[i + 1].color, localT);
            }
        }

        return points[^1].color;
    }
}
