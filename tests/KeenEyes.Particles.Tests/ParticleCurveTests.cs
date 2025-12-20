using KeenEyes.Particles.Data;

namespace KeenEyes.Particles.Tests;

/// <summary>
/// Tests for the ParticleCurve struct.
/// </summary>
public class ParticleCurveTests
{
    #region Constant Tests

    [Fact]
    public void Constant_ReturnsConstantValue()
    {
        var curve = ParticleCurve.Constant(0.5f);

        Assert.Equal(0.5f, curve.Evaluate(0f));
        Assert.Equal(0.5f, curve.Evaluate(0.5f));
        Assert.Equal(0.5f, curve.Evaluate(1f));
    }

    [Fact]
    public void Constant_DifferentValues()
    {
        Assert.Equal(0f, ParticleCurve.Constant(0f).Evaluate(0.5f));
        Assert.Equal(1f, ParticleCurve.Constant(1f).Evaluate(0.5f));
        Assert.Equal(2.5f, ParticleCurve.Constant(2.5f).Evaluate(0.5f));
    }

    #endregion

    #region LinearFadeOut Tests

    [Fact]
    public void LinearFadeOut_StartsAtOne()
    {
        var curve = ParticleCurve.LinearFadeOut();

        Assert.Equal(1f, curve.Evaluate(0f), 4);
    }

    [Fact]
    public void LinearFadeOut_EndsAtZero()
    {
        var curve = ParticleCurve.LinearFadeOut();

        Assert.Equal(0f, curve.Evaluate(1f), 4);
    }

    [Fact]
    public void LinearFadeOut_MidpointIsHalf()
    {
        var curve = ParticleCurve.LinearFadeOut();

        Assert.Equal(0.5f, curve.Evaluate(0.5f), 2);
    }

    #endregion

    #region LinearFadeIn Tests

    [Fact]
    public void LinearFadeIn_StartsAtZero()
    {
        var curve = ParticleCurve.LinearFadeIn();

        Assert.Equal(0f, curve.Evaluate(0f), 4);
    }

    [Fact]
    public void LinearFadeIn_EndsAtOne()
    {
        var curve = ParticleCurve.LinearFadeIn();

        Assert.Equal(1f, curve.Evaluate(1f), 4);
    }

    [Fact]
    public void LinearFadeIn_MidpointIsHalf()
    {
        var curve = ParticleCurve.LinearFadeIn();

        Assert.Equal(0.5f, curve.Evaluate(0.5f), 2);
    }

    #endregion

    #region EaseOut Tests

    [Fact]
    public void EaseOut_StartsAtOne()
    {
        var curve = ParticleCurve.EaseOut();

        Assert.Equal(1f, curve.Evaluate(0f), 4);
    }

    [Fact]
    public void EaseOut_EndsAtZero()
    {
        var curve = ParticleCurve.EaseOut();

        Assert.Equal(0f, curve.Evaluate(1f), 4);
    }

    [Fact]
    public void EaseOut_MidpointIsHigherThanLinear()
    {
        var easeOut = ParticleCurve.EaseOut();
        var linear = ParticleCurve.LinearFadeOut();

        // Ease out should have higher values in the middle (starts fast)
        Assert.True(easeOut.Evaluate(0.5f) > linear.Evaluate(0.5f));
    }

    #endregion

    #region EaseIn Tests

    [Fact]
    public void EaseIn_StartsAtZero()
    {
        var curve = ParticleCurve.EaseIn();

        Assert.Equal(0f, curve.Evaluate(0f), 4);
    }

    [Fact]
    public void EaseIn_EndsAtOne()
    {
        var curve = ParticleCurve.EaseIn();

        Assert.Equal(1f, curve.Evaluate(1f), 4);
    }

    [Fact]
    public void EaseIn_MidpointIsLowerThanLinear()
    {
        var easeIn = ParticleCurve.EaseIn();
        var linear = ParticleCurve.LinearFadeIn();

        // Ease in should have lower values in the middle (starts slow)
        Assert.True(easeIn.Evaluate(0.5f) < linear.Evaluate(0.5f));
    }

    #endregion

    #region FromPoints Tests

    [Fact]
    public void FromPoints_EmptyPoints_ReturnsConstantOne()
    {
        var curve = ParticleCurve.FromPoints([]);

        Assert.Equal(1f, curve.Evaluate(0.5f));
    }

    [Fact]
    public void FromPoints_SinglePoint_ReturnsConstant()
    {
        var curve = ParticleCurve.FromPoints([(0.5f, 0.75f)]);

        Assert.Equal(0.75f, curve.Evaluate(0f), 4);
        Assert.Equal(0.75f, curve.Evaluate(0.5f), 4);
        Assert.Equal(0.75f, curve.Evaluate(1f), 4);
    }

    [Fact]
    public void FromPoints_TwoPoints_Interpolates()
    {
        var curve = ParticleCurve.FromPoints([(0f, 0f), (1f, 1f)]);

        Assert.Equal(0f, curve.Evaluate(0f), 2);
        Assert.Equal(0.5f, curve.Evaluate(0.5f), 2);
        Assert.Equal(1f, curve.Evaluate(1f), 2);
    }

    [Fact]
    public void FromPoints_ThreePoints_InterpolatesCorrectly()
    {
        var curve = ParticleCurve.FromPoints([(0f, 0f), (0.5f, 1f), (1f, 0f)]);

        Assert.Equal(0f, curve.Evaluate(0f), 2);
        // Due to 64-sample LUT, midpoint may not be exactly at 0.5
        // Check that it's close to the peak (1.0)
        Assert.InRange(curve.Evaluate(0.5f), 0.95f, 1.0f);
        Assert.Equal(0f, curve.Evaluate(1f), 2);
    }

    #endregion

    #region Evaluate Edge Cases

    [Fact]
    public void Evaluate_BelowZero_ClampsToStart()
    {
        var curve = ParticleCurve.LinearFadeOut();

        Assert.Equal(1f, curve.Evaluate(-0.5f), 4);
    }

    [Fact]
    public void Evaluate_AboveOne_ClampsToEnd()
    {
        var curve = ParticleCurve.LinearFadeOut();

        Assert.Equal(0f, curve.Evaluate(1.5f), 4);
    }

    [Fact]
    public void Evaluate_DefaultCurve_ReturnsOne()
    {
        ParticleCurve curve = default;

        Assert.Equal(1f, curve.Evaluate(0.5f));
    }

    #endregion

    #region FromPoints Edge Cases

    [Fact]
    public void FromPoints_TimeBeforeFirstPoint_ReturnsFirstValue()
    {
        // Points that don't start at 0
        var curve = ParticleCurve.FromPoints([(0.2f, 0.5f), (0.8f, 1f)]);

        // Value at t=0 should clamp to first point value
        Assert.Equal(0.5f, curve.Evaluate(0f), 2);
    }

    [Fact]
    public void FromPoints_TimeAfterLastPoint_ReturnsLastValue()
    {
        // Points that don't end at 1
        var curve = ParticleCurve.FromPoints([(0.2f, 0.5f), (0.8f, 0.75f)]);

        // Value at t=1 should clamp to last point value
        Assert.Equal(0.75f, curve.Evaluate(1f), 2);
    }

    [Fact]
    public void FromPoints_PointsAtSameTime_HandlesZeroSegmentLength()
    {
        // Two points at the same time (degenerate case)
        var curve = ParticleCurve.FromPoints([(0.5f, 0.3f), (0.5f, 0.7f), (1f, 1f)]);

        // Should still evaluate without crashing
        var value = curve.Evaluate(0.5f);
        Assert.InRange(value, 0f, 1f);
    }

    [Fact]
    public void FromPoints_ManyPoints_InterpolatesCorrectly()
    {
        var curve = ParticleCurve.FromPoints([
            (0f, 0f),
            (0.25f, 0.5f),
            (0.5f, 1f),
            (0.75f, 0.5f),
            (1f, 0f)
        ]);

        // Should peak at 0.5
        Assert.InRange(curve.Evaluate(0.5f), 0.95f, 1.0f);
        // Should be at half at 0.25 and 0.75
        Assert.InRange(curve.Evaluate(0.25f), 0.45f, 0.55f);
        Assert.InRange(curve.Evaluate(0.75f), 0.45f, 0.55f);
    }

    #endregion
}
