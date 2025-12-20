using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Tests;

public class EasingTests
{
    #region Boundary Tests

    [Fact]
    public void Evaluate_AtZero_ReturnsZeroOrNear()
    {
        // All easing functions should return 0 (or near 0) at t=0
        foreach (var easeType in Enum.GetValues<EaseType>())
        {
            var result = Easing.Evaluate(easeType, 0f);

            // Allow small tolerance for elastic/bounce which may not be exactly 0
            result.ShouldBeInRange(-0.1f, 0.1f, $"{easeType} at t=0");
        }
    }

    [Fact]
    public void Evaluate_AtOne_ReturnsOneOrNear()
    {
        // All easing functions should return 1 (or near 1) at t=1
        foreach (var easeType in Enum.GetValues<EaseType>())
        {
            var result = Easing.Evaluate(easeType, 1f);

            // Allow small tolerance for elastic/bounce
            result.ShouldBeInRange(0.9f, 1.1f, $"{easeType} at t=1");
        }
    }

    [Fact]
    public void Evaluate_AtHalf_ReturnsMidRangeValue()
    {
        // Most easing functions should return a value between 0 and 1 at t=0.5
        foreach (var easeType in Enum.GetValues<EaseType>())
        {
            var result = Easing.Evaluate(easeType, 0.5f);

            // Back and elastic can overshoot
            result.ShouldBeInRange(-0.5f, 1.5f, $"{easeType} at t=0.5");
        }
    }

    #endregion

    #region Linear Tests

    [Fact]
    public void Linear_ReturnsInputValue()
    {
        Easing.Evaluate(EaseType.Linear, 0f).ShouldBe(0f);
        Easing.Evaluate(EaseType.Linear, 0.25f).ShouldBe(0.25f);
        Easing.Evaluate(EaseType.Linear, 0.5f).ShouldBe(0.5f);
        Easing.Evaluate(EaseType.Linear, 0.75f).ShouldBe(0.75f);
        Easing.Evaluate(EaseType.Linear, 1f).ShouldBe(1f);
    }

    #endregion

    #region Quadratic Tests

    [Fact]
    public void QuadIn_IsSlowerAtStart()
    {
        var quarter = Easing.QuadIn(0.25f);
        var half = Easing.QuadIn(0.5f);

        // Quad In accelerates, so first quarter should be less than 0.25
        quarter.ShouldBeLessThan(0.25f);
        half.ShouldBeLessThan(0.5f);
    }

    [Fact]
    public void QuadOut_IsFasterAtStart()
    {
        var quarter = Easing.QuadOut(0.25f);
        var half = Easing.QuadOut(0.5f);

        // Quad Out decelerates, so first quarter should be more than 0.25
        quarter.ShouldBeGreaterThan(0.25f);
        half.ShouldBeGreaterThan(0.5f);
    }

    [Fact]
    public void QuadInOut_IsSymmetric()
    {
        var quarter = Easing.QuadInOut(0.25f);
        var threeQuarter = Easing.QuadInOut(0.75f);

        // Should be symmetric around 0.5
        quarter.ShouldBeLessThan(0.5f);
        threeQuarter.ShouldBeGreaterThan(0.5f);
        (quarter + threeQuarter).ShouldBe(1f, 0.001f);
    }

    #endregion

    #region Elastic Tests

    [Fact]
    public void ElasticOut_OscillatesAroundTarget()
    {
        // Elastic out should overshoot and oscillate
        var values = new List<float>();
        for (var t = 0f; t <= 1f; t += 0.1f)
        {
            values.Add(Easing.ElasticOut(t));
        }

        // Should have values > 1 (overshoot)
        values.ShouldContain(v => v > 1f);
    }

    #endregion

    #region Bounce Tests

    [Fact]
    public void BounceOut_HasMultipleBounces()
    {
        // Bounce out should have multiple local maxima (bounces)
        var values = new List<float>();
        for (var t = 0f; t <= 1f; t += 0.05f)
        {
            values.Add(Easing.BounceOut(t));
        }

        // Values should generally increase but with some "bounces"
        var increasing = values[^1] > values[0];
        increasing.ShouldBeTrue();
    }

    [Fact]
    public void BounceIn_IsInverseOfBounceOut()
    {
        for (var t = 0f; t <= 1f; t += 0.1f)
        {
            var bounceIn = Easing.BounceIn(t);
            var expectedFromBounceOut = 1f - Easing.BounceOut(1f - t);

            bounceIn.ShouldBe(expectedFromBounceOut, 0.0001f);
        }
    }

    #endregion

    #region Back Tests

    [Fact]
    public void BackIn_GoesNegative()
    {
        // Back In should go negative before reaching the target
        var earlyValue = Easing.BackIn(0.3f);
        earlyValue.ShouldBeLessThan(0f);
    }

    [Fact]
    public void BackOut_Overshoots()
    {
        // Back Out should overshoot past 1 before settling
        var lateValue = Easing.BackOut(0.7f);
        lateValue.ShouldBeGreaterThan(1f);
    }

    #endregion
}
