using System.Numerics;
using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticleGradient struct.
/// </summary>
public class ParticleGradientTests
{
    #region Constant Tests

    [Fact]
    public void Constant_ReturnsConstantColor()
    {
        var color = new Vector4(1f, 0.5f, 0.25f, 1f);
        var gradient = ParticleGradient.Constant(color);

        Assert.Equal(color, gradient.Evaluate(0f));
        Assert.Equal(color, gradient.Evaluate(0.5f));
        Assert.Equal(color, gradient.Evaluate(1f));
    }

    #endregion

    #region FadeOut Tests

    [Fact]
    public void FadeOut_StartsWithFullAlpha()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var gradient = ParticleGradient.FadeOut(color);

        var result = gradient.Evaluate(0f);
        Assert.Equal(1f, result.W, 4);
    }

    [Fact]
    public void FadeOut_EndsWithZeroAlpha()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var gradient = ParticleGradient.FadeOut(color);

        var result = gradient.Evaluate(1f);
        Assert.Equal(0f, result.W, 4);
    }

    [Fact]
    public void FadeOut_PreservesRgb()
    {
        var color = new Vector4(1f, 0.5f, 0.25f, 1f);
        var gradient = ParticleGradient.FadeOut(color);

        var result = gradient.Evaluate(0.5f);
        Assert.Equal(1f, result.X, 4);
        Assert.Equal(0.5f, result.Y, 4);
        Assert.Equal(0.25f, result.Z, 4);
    }

    #endregion

    #region FadeIn Tests

    [Fact]
    public void FadeIn_StartsWithZeroAlpha()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var gradient = ParticleGradient.FadeIn(color);

        var result = gradient.Evaluate(0f);
        Assert.Equal(0f, result.W, 4);
    }

    [Fact]
    public void FadeIn_EndsWithFullAlpha()
    {
        var color = new Vector4(1f, 0f, 0f, 1f);
        var gradient = ParticleGradient.FadeIn(color);

        var result = gradient.Evaluate(1f);
        Assert.Equal(1f, result.W, 4);
    }

    #endregion

    #region TwoColor Tests

    [Fact]
    public void TwoColor_StartsWithFirstColor()
    {
        var start = new Vector4(1f, 0f, 0f, 1f);
        var end = new Vector4(0f, 0f, 1f, 1f);
        var gradient = ParticleGradient.TwoColor(start, end);

        var result = gradient.Evaluate(0f);
        Assert.Equal(1f, result.X, 4);
        Assert.Equal(0f, result.Y, 4);
        Assert.Equal(0f, result.Z, 4);
    }

    [Fact]
    public void TwoColor_EndsWithSecondColor()
    {
        var start = new Vector4(1f, 0f, 0f, 1f);
        var end = new Vector4(0f, 0f, 1f, 1f);
        var gradient = ParticleGradient.TwoColor(start, end);

        var result = gradient.Evaluate(1f);
        Assert.Equal(0f, result.X, 4);
        Assert.Equal(0f, result.Y, 4);
        Assert.Equal(1f, result.Z, 4);
    }

    [Fact]
    public void TwoColor_InterpolatesAtMidpoint()
    {
        var start = new Vector4(1f, 0f, 0f, 1f);
        var end = new Vector4(0f, 0f, 1f, 1f);
        var gradient = ParticleGradient.TwoColor(start, end);

        var result = gradient.Evaluate(0.5f);
        Assert.Equal(0.5f, result.X, 2);
        Assert.Equal(0f, result.Y, 4);
        Assert.Equal(0.5f, result.Z, 2);
    }

    #endregion

    #region FromPoints Tests

    [Fact]
    public void FromPoints_EmptyPoints_ReturnsWhite()
    {
        var gradient = ParticleGradient.FromPoints([]);

        Assert.Equal(Vector4.One, gradient.Evaluate(0.5f));
    }

    [Fact]
    public void FromPoints_SinglePoint_ReturnsConstant()
    {
        var color = new Vector4(0.5f, 0.5f, 0.5f, 1f);
        var gradient = ParticleGradient.FromPoints([(0.5f, color)]);

        Assert.Equal(color, gradient.Evaluate(0f));
        Assert.Equal(color, gradient.Evaluate(0.5f));
        Assert.Equal(color, gradient.Evaluate(1f));
    }

    [Fact]
    public void FromPoints_TwoPoints_Interpolates()
    {
        var red = new Vector4(1f, 0f, 0f, 1f);
        var blue = new Vector4(0f, 0f, 1f, 1f);
        var gradient = ParticleGradient.FromPoints([(0f, red), (1f, blue)]);

        var result = gradient.Evaluate(0.5f);
        Assert.Equal(0.5f, result.X, 2);
        Assert.Equal(0.5f, result.Z, 2);
    }

    [Fact]
    public void FromPoints_FireGradient()
    {
        var yellow = new Vector4(1f, 1f, 0f, 1f);
        var orange = new Vector4(1f, 0.5f, 0f, 1f);
        var red = new Vector4(1f, 0f, 0f, 0f);

        var gradient = ParticleGradient.FromPoints([
            (0f, yellow),
            (0.5f, orange),
            (1f, red)
        ]);

        // Should start yellow
        var start = gradient.Evaluate(0f);
        Assert.Equal(1f, start.X, 2);
        Assert.Equal(1f, start.Y, 2);

        // Should be orange in middle
        var mid = gradient.Evaluate(0.5f);
        Assert.Equal(1f, mid.X, 2);
        Assert.Equal(0.5f, mid.Y, 2);

        // Should end transparent red
        var end = gradient.Evaluate(1f);
        Assert.Equal(0f, end.W, 2);
    }

    #endregion

    #region Evaluate Edge Cases

    [Fact]
    public void Evaluate_BelowZero_ClampsToStart()
    {
        var gradient = ParticleGradient.TwoColor(
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(0f, 0f, 1f, 1f));

        var result = gradient.Evaluate(-0.5f);
        Assert.Equal(1f, result.X, 4);
        Assert.Equal(0f, result.Z, 4);
    }

    [Fact]
    public void Evaluate_AboveOne_ClampsToEnd()
    {
        var gradient = ParticleGradient.TwoColor(
            new Vector4(1f, 0f, 0f, 1f),
            new Vector4(0f, 0f, 1f, 1f));

        var result = gradient.Evaluate(1.5f);
        Assert.Equal(0f, result.X, 4);
        Assert.Equal(1f, result.Z, 4);
    }

    [Fact]
    public void Evaluate_DefaultGradient_ReturnsWhite()
    {
        ParticleGradient gradient = default;

        Assert.Equal(Vector4.One, gradient.Evaluate(0.5f));
    }

    #endregion

    #region FromPoints Edge Cases

    [Fact]
    public void FromPoints_TimeBeforeFirstPoint_ReturnsFirstColor()
    {
        var red = new Vector4(1f, 0f, 0f, 1f);
        var blue = new Vector4(0f, 0f, 1f, 1f);

        // Points that don't start at 0
        var gradient = ParticleGradient.FromPoints([(0.2f, red), (0.8f, blue)]);

        // Value at t=0 should clamp to first point color
        var result = gradient.Evaluate(0f);
        Assert.Equal(1f, result.X, 2); // Red
        Assert.Equal(0f, result.Z, 2); // Not blue
    }

    [Fact]
    public void FromPoints_TimeAfterLastPoint_ReturnsLastColor()
    {
        var red = new Vector4(1f, 0f, 0f, 1f);
        var blue = new Vector4(0f, 0f, 1f, 1f);

        // Points that don't end at 1
        var gradient = ParticleGradient.FromPoints([(0.2f, red), (0.8f, blue)]);

        // Value at t=1 should clamp to last point color
        var result = gradient.Evaluate(1f);
        Assert.Equal(0f, result.X, 2); // Not red
        Assert.Equal(1f, result.Z, 2); // Blue
    }

    [Fact]
    public void FromPoints_PointsAtSameTime_HandlesZeroSegmentLength()
    {
        var red = new Vector4(1f, 0f, 0f, 1f);
        var green = new Vector4(0f, 1f, 0f, 1f);
        var blue = new Vector4(0f, 0f, 1f, 1f);

        // Two points at the same time (degenerate case)
        var gradient = ParticleGradient.FromPoints([(0.5f, red), (0.5f, green), (1f, blue)]);

        // Should still evaluate without crashing
        var value = gradient.Evaluate(0.5f);
        // Value should be valid (doesn't matter which one due to overlap)
        Assert.InRange(value.X, 0f, 1f);
        Assert.InRange(value.Y, 0f, 1f);
        Assert.InRange(value.Z, 0f, 1f);
    }

    [Fact]
    public void FromPoints_ManyPoints_InterpolatesCorrectly()
    {
        var white = new Vector4(1f, 1f, 1f, 1f);
        var red = new Vector4(1f, 0f, 0f, 1f);
        var blue = new Vector4(0f, 0f, 1f, 1f);
        var green = new Vector4(0f, 1f, 0f, 1f);
        var black = new Vector4(0f, 0f, 0f, 1f);

        var gradient = ParticleGradient.FromPoints([
            (0f, white),
            (0.25f, red),
            (0.5f, blue),
            (0.75f, green),
            (1f, black)
        ]);

        // Should be white at start
        var start = gradient.Evaluate(0f);
        Assert.Equal(1f, start.X, 2);
        Assert.Equal(1f, start.Y, 2);
        Assert.Equal(1f, start.Z, 2);

        // Should be blue at midpoint
        var mid = gradient.Evaluate(0.5f);
        Assert.InRange(mid.Z, 0.95f, 1.0f); // Blue component high

        // Should be black at end
        var end = gradient.Evaluate(1f);
        Assert.Equal(0f, end.X, 2);
        Assert.Equal(0f, end.Y, 2);
        Assert.Equal(0f, end.Z, 2);
    }

    #endregion
}
