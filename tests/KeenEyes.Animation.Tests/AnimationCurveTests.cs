using System.Numerics;
using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Tests;

public class AnimationCurveTests
{
    #region FloatCurve Tests

    [Fact]
    public void FloatCurve_WithNoKeyframes_ReturnsZero()
    {
        var curve = new FloatCurve();

        curve.Evaluate(0f).ShouldBe(0f);
        curve.Evaluate(1f).ShouldBe(0f);
    }

    [Fact]
    public void FloatCurve_WithSingleKeyframe_ReturnsKeyframeValue()
    {
        var curve = new FloatCurve();
        curve.AddKeyframe(0f, 5f);

        curve.Evaluate(-1f).ShouldBe(5f);
        curve.Evaluate(0f).ShouldBe(5f);
        curve.Evaluate(1f).ShouldBe(5f);
    }

    [Fact]
    public void FloatCurve_InterpolatesBetweenKeyframes()
    {
        var curve = new FloatCurve();
        curve.AddKeyframe(0f, 0f);
        curve.AddKeyframe(1f, 10f);

        curve.Evaluate(0f).ShouldBe(0f);
        curve.Evaluate(1f).ShouldBe(10f);

        // Mid-point should be approximately 5 with linear tangents
        var mid = curve.Evaluate(0.5f);
        mid.ShouldBeInRange(4f, 6f);
    }

    [Fact]
    public void FloatCurve_Duration_ReturnsLastKeyframeTime()
    {
        var curve = new FloatCurve();
        curve.AddKeyframe(0f, 0f);
        curve.AddKeyframe(2.5f, 10f);

        curve.Duration.ShouldBe(2.5f);
    }

    [Fact]
    public void FloatCurve_BeforeFirstKeyframe_ReturnsFirstValue()
    {
        var curve = new FloatCurve();
        curve.AddKeyframe(1f, 5f);
        curve.AddKeyframe(2f, 10f);

        curve.Evaluate(0f).ShouldBe(5f);
        curve.Evaluate(-1f).ShouldBe(5f);
    }

    [Fact]
    public void FloatCurve_AfterLastKeyframe_ReturnsLastValue()
    {
        var curve = new FloatCurve();
        curve.AddKeyframe(0f, 0f);
        curve.AddKeyframe(1f, 10f);

        curve.Evaluate(1.5f).ShouldBe(10f);
        curve.Evaluate(100f).ShouldBe(10f);
    }

    #endregion

    #region Vector3Curve Tests

    [Fact]
    public void Vector3Curve_WithNoKeyframes_ReturnsZero()
    {
        var curve = new Vector3Curve();

        curve.Evaluate(0f).ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void Vector3Curve_InterpolatesLinearly()
    {
        var curve = new Vector3Curve();
        curve.AddKeyframe(0f, new Vector3(0f, 0f, 0f));
        curve.AddKeyframe(1f, new Vector3(10f, 20f, 30f));

        var mid = curve.Evaluate(0.5f);
        mid.X.ShouldBe(5f, 0.001f);
        mid.Y.ShouldBe(10f, 0.001f);
        mid.Z.ShouldBe(15f, 0.001f);
    }

    #endregion

    #region QuaternionCurve Tests

    [Fact]
    public void QuaternionCurve_WithNoKeyframes_ReturnsIdentity()
    {
        var curve = new QuaternionCurve();

        curve.Evaluate(0f).ShouldBe(Quaternion.Identity);
    }

    [Fact]
    public void QuaternionCurve_InterpolatesWithSlerp()
    {
        var curve = new QuaternionCurve();
        var start = Quaternion.Identity;
        var end = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2); // 90 degrees

        curve.AddKeyframe(0f, start);
        curve.AddKeyframe(1f, end);

        var mid = curve.Evaluate(0.5f);

        // Mid-point should be approximately 45 degrees
        mid.ShouldNotBe(start);
        mid.ShouldNotBe(end);
    }

    #endregion
}
