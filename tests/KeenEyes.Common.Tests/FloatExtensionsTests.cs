namespace KeenEyes.Common.Tests;

/// <summary>
/// Tests for the <see cref="FloatExtensions"/> class.
/// </summary>
public class FloatExtensionsTests
{
    #region IsApproximatelyZero Tests

    [Fact]
    public void IsApproximatelyZero_WithExactZero_ReturnsTrue()
    {
        float value = 0f;

        Assert.True(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithVerySmallPositive_ReturnsTrue()
    {
        float value = 1e-7f;

        Assert.True(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithVerySmallNegative_ReturnsTrue()
    {
        float value = -1e-7f;

        Assert.True(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithValueAtEpsilon_ReturnsFalse()
    {
        float value = FloatExtensions.DefaultEpsilon;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithLargeValue_ReturnsFalse()
    {
        float value = 1.0f;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithLargeNegativeValue_ReturnsFalse()
    {
        float value = -1.0f;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithCustomEpsilon_UsesCustomTolerance()
    {
        float value = 0.005f;
        float customEpsilon = 0.01f;

        Assert.True(value.IsApproximatelyZero(customEpsilon));
    }

    [Fact]
    public void IsApproximatelyZero_WithCustomEpsilon_RejectsLargerValues()
    {
        float value = 0.02f;
        float customEpsilon = 0.01f;

        Assert.False(value.IsApproximatelyZero(customEpsilon));
    }

    #endregion

    #region ApproximatelyEquals Tests

    [Fact]
    public void ApproximatelyEquals_WithIdenticalValues_ReturnsTrue()
    {
        float a = 1.0f;
        float b = 1.0f;

        Assert.True(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_WithVeryCloseValues_ReturnsTrue()
    {
        float a = 1.0f;
        float b = 1.0000001f;

        Assert.True(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_WithDifferentValues_ReturnsFalse()
    {
        float a = 1.0f;
        float b = 2.0f;

        Assert.False(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_WithNegativeValues_ReturnsTrue()
    {
        float a = -1.0f;
        float b = -1.0000001f;

        Assert.True(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_WithOppositeSignsNearZero_ReturnsFalse()
    {
        float a = FloatExtensions.DefaultEpsilon;
        float b = -FloatExtensions.DefaultEpsilon;

        Assert.False(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_IsSymmetric()
    {
        float a = 1.0f;
        float b = 1.0000001f;

        Assert.Equal(a.ApproximatelyEquals(b), b.ApproximatelyEquals(a));
    }

    [Fact]
    public void ApproximatelyEquals_WithCustomEpsilon_UsesCustomTolerance()
    {
        float a = 1.0f;
        float b = 1.005f;
        float customEpsilon = 0.01f;

        Assert.True(a.ApproximatelyEquals(b, customEpsilon));
    }

    [Fact]
    public void ApproximatelyEquals_WithCustomEpsilon_RejectsLargerDifferences()
    {
        float a = 1.0f;
        float b = 1.02f;
        float customEpsilon = 0.01f;

        Assert.False(a.ApproximatelyEquals(b, customEpsilon));
    }

    [Fact]
    public void ApproximatelyEquals_WithZeroValues_ReturnsTrue()
    {
        float a = 0f;
        float b = 0f;

        Assert.True(a.ApproximatelyEquals(b));
    }

    #endregion

    #region DefaultEpsilon Tests

    [Fact]
    public void DefaultEpsilon_HasExpectedValue()
    {
        Assert.Equal(1e-6f, FloatExtensions.DefaultEpsilon);
    }

    [Fact]
    public void DefaultEpsilon_IsPositive()
    {
        Assert.True(FloatExtensions.DefaultEpsilon > 0);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void IsApproximatelyZero_WithPositiveInfinity_ReturnsFalse()
    {
        float value = float.PositiveInfinity;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithNegativeInfinity_ReturnsFalse()
    {
        float value = float.NegativeInfinity;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void IsApproximatelyZero_WithNaN_ReturnsFalse()
    {
        float value = float.NaN;

        Assert.False(value.IsApproximatelyZero());
    }

    [Fact]
    public void ApproximatelyEquals_WithInfinities_ReturnsFalse()
    {
        float a = float.PositiveInfinity;
        float b = float.PositiveInfinity;

        // Infinity - Infinity = NaN, which is not less than epsilon
        Assert.False(a.ApproximatelyEquals(b));
    }

    [Fact]
    public void ApproximatelyEquals_WithNaN_ReturnsFalse()
    {
        float a = float.NaN;
        float b = 0f;

        Assert.False(a.ApproximatelyEquals(b));
    }

    #endregion
}
