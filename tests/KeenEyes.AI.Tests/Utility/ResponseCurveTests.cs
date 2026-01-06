using KeenEyes.AI.Utility;
using KeenEyes.Testing;

namespace KeenEyes.AI.Tests.Utility;

/// <summary>
/// Tests for response curve implementations.
/// </summary>
public class ResponseCurveTests
{
    #region LinearCurve Tests

    [Fact]
    public void LinearCurve_Identity_ReturnsInput()
    {
        var curve = new LinearCurve
        {
            Slope = 1f,
            XShift = 0f,
            YShift = 0f
        };

        curve.Evaluate(0.5f).ShouldBe(0.5f);
        curve.Evaluate(0f).ShouldBe(0f);
        curve.Evaluate(1f).ShouldBe(1f);
    }

    [Fact]
    public void LinearCurve_Inverted_ReturnsOneMinusInput()
    {
        var curve = new LinearCurve
        {
            Slope = -1f,
            XShift = 0f,
            YShift = 1f
        };

        curve.Evaluate(0f).ShouldBe(1f);
        curve.Evaluate(1f).ShouldBe(0f);
        curve.Evaluate(0.5f).ShouldBe(0.5f);
    }

    [Fact]
    public void LinearCurve_WithXShift_ShiftsHorizontally()
    {
        var curve = new LinearCurve
        {
            Slope = 1f,
            XShift = 0.5f,
            YShift = 0.5f
        };

        // At x=0.5, the shifted x is 0, so output is 0.5 (YShift)
        curve.Evaluate(0.5f).ShouldBe(0.5f);
    }

    [Fact]
    public void LinearCurve_WithClampOutput_ClampsToZeroOne()
    {
        var curve = new LinearCurve
        {
            Slope = 2f,
            XShift = 0f,
            YShift = 0f,
            ClampOutput = true
        };

        // 2 * 1 = 2, but clamped to 1
        curve.Evaluate(1f).ShouldBe(1f);
        // 2 * -0.5 = -1, but clamped to 0
        curve.Evaluate(-0.5f).ShouldBe(0f);
    }

    [Fact]
    public void LinearCurve_WithoutClamp_AllowsValuesOutsideRange()
    {
        var curve = new LinearCurve
        {
            Slope = 2f,
            XShift = 0f,
            YShift = 0f,
            ClampOutput = false
        };

        curve.Evaluate(1f).ShouldBe(2f);
    }

    #endregion

    #region ExponentialCurve Tests

    [Fact]
    public void ExponentialCurve_WithExponentTwo_ReturnsSquare()
    {
        var curve = new ExponentialCurve { Exponent = 2f };

        curve.Evaluate(0f).ShouldBe(0f);
        curve.Evaluate(0.5f).ShouldBe(0.25f);
        curve.Evaluate(1f).ShouldBe(1f);
    }

    [Fact]
    public void ExponentialCurve_WithExponentHalf_ReturnsSquareRoot()
    {
        var curve = new ExponentialCurve { Exponent = 0.5f };

        curve.Evaluate(0f).ShouldBe(0f);
        curve.Evaluate(0.25f).ShouldBe(0.5f);
        curve.Evaluate(1f).ShouldBe(1f);
    }

    [Fact]
    public void ExponentialCurve_WithExponentOne_ReturnsIdentity()
    {
        var curve = new ExponentialCurve { Exponent = 1f };

        curve.Evaluate(0.5f).ShouldBe(0.5f);
    }

    [Fact]
    public void ExponentialCurve_ClampsInputToZeroOne()
    {
        var curve = new ExponentialCurve { Exponent = 2f };

        // Input is clamped, so negative input becomes 0
        curve.Evaluate(-0.5f).ShouldBe(0f);
        // Input > 1 is clamped to 1
        curve.Evaluate(1.5f).ShouldBe(1f);
    }

    #endregion

    #region LogisticCurve Tests

    [Fact]
    public void LogisticCurve_AtMidpoint_ReturnsHalf()
    {
        var curve = new LogisticCurve
        {
            Midpoint = 0.5f,
            Steepness = 10f
        };

        curve.Evaluate(0.5f).ShouldBe(0.5f, 0.001);
    }

    [Fact]
    public void LogisticCurve_BelowMidpoint_ReturnsLowValue()
    {
        var curve = new LogisticCurve
        {
            Midpoint = 0.5f,
            Steepness = 10f
        };

        // Well below midpoint should be close to 0
        curve.Evaluate(0f).ShouldBeLessThan(0.1f);
    }

    [Fact]
    public void LogisticCurve_AboveMidpoint_ReturnsHighValue()
    {
        var curve = new LogisticCurve
        {
            Midpoint = 0.5f,
            Steepness = 10f
        };

        // Well above midpoint should be close to 1
        curve.Evaluate(1f).ShouldBeGreaterThan(0.9f);
    }

    [Fact]
    public void LogisticCurve_WithHighSteepness_CreatesSharpTransition()
    {
        var curve = new LogisticCurve
        {
            Midpoint = 0.5f,
            Steepness = 50f // Very steep
        };

        // Very close to step function
        curve.Evaluate(0.45f).ShouldBeLessThan(0.1f);
        curve.Evaluate(0.55f).ShouldBeGreaterThan(0.9f);
    }

    [Fact]
    public void LogisticCurve_WithLowSteepness_CreatesSmoothTransition()
    {
        var curve = new LogisticCurve
        {
            Midpoint = 0.5f,
            Steepness = 2f // Very gradual
        };

        // More gradual transition
        var value = curve.Evaluate(0.25f);
        value.ShouldBeGreaterThan(0.2f);
        value.ShouldBeLessThan(0.5f);
    }

    #endregion

    #region StepCurve Tests

    [Fact]
    public void StepCurve_BelowThreshold_ReturnsLowValue()
    {
        var curve = new StepCurve
        {
            Threshold = 0.5f,
            LowValue = 0f,
            HighValue = 1f
        };

        curve.Evaluate(0.49f).ShouldBe(0f);
        curve.Evaluate(0f).ShouldBe(0f);
    }

    [Fact]
    public void StepCurve_AtOrAboveThreshold_ReturnsHighValue()
    {
        var curve = new StepCurve
        {
            Threshold = 0.5f,
            LowValue = 0f,
            HighValue = 1f
        };

        curve.Evaluate(0.5f).ShouldBe(1f);
        curve.Evaluate(0.51f).ShouldBe(1f);
        curve.Evaluate(1f).ShouldBe(1f);
    }

    [Fact]
    public void StepCurve_WithInvertedValues_WorksCorrectly()
    {
        var curve = new StepCurve
        {
            Threshold = 0.3f,
            LowValue = 1f,  // Inverted
            HighValue = 0f  // Inverted
        };

        curve.Evaluate(0.2f).ShouldBe(1f);
        curve.Evaluate(0.3f).ShouldBe(0f);
        curve.Evaluate(0.5f).ShouldBe(0f);
    }

    [Fact]
    public void StepCurve_WithCustomValues_ReturnsConfiguredValues()
    {
        var curve = new StepCurve
        {
            Threshold = 0.5f,
            LowValue = 0.25f,
            HighValue = 0.75f
        };

        curve.Evaluate(0.4f).ShouldBe(0.25f);
        curve.Evaluate(0.6f).ShouldBe(0.75f);
    }

    #endregion
}
