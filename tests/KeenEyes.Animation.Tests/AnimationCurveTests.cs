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

    #region Vector3Curve STEP Interpolation Tests

    [Fact]
    public void Vector3Curve_StepInterpolation_ReturnsValueAtKeyframe()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.Step };
        curve.AddKeyframe(0f, new Vector3(0f, 0f, 0f));
        curve.AddKeyframe(1f, new Vector3(10f, 10f, 10f));
        curve.AddKeyframe(2f, new Vector3(20f, 20f, 20f));

        // At keyframe should return that keyframe's value
        curve.Evaluate(0f).ShouldBe(new Vector3(0f, 0f, 0f));
        curve.Evaluate(1f).ShouldBe(new Vector3(10f, 10f, 10f));
        curve.Evaluate(2f).ShouldBe(new Vector3(20f, 20f, 20f));
    }

    [Fact]
    public void Vector3Curve_StepInterpolation_HoldsPreviousValue()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.Step };
        curve.AddKeyframe(0f, new Vector3(0f, 0f, 0f));
        curve.AddKeyframe(1f, new Vector3(10f, 10f, 10f));

        // Between keyframes should hold the previous value
        curve.Evaluate(0.1f).ShouldBe(new Vector3(0f, 0f, 0f));
        curve.Evaluate(0.5f).ShouldBe(new Vector3(0f, 0f, 0f));
        curve.Evaluate(0.99f).ShouldBe(new Vector3(0f, 0f, 0f));
    }

    [Fact]
    public void Vector3Curve_StepInterpolation_WithNoKeyframes_ReturnsZero()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.Step };

        curve.Evaluate(0f).ShouldBe(Vector3.Zero);
        curve.Evaluate(1f).ShouldBe(Vector3.Zero);
    }

    #endregion

    #region Vector3Curve CubicSpline Interpolation Tests

    [Fact]
    public void Vector3Curve_CubicSpline_WithNoKeyframes_FallsBackToLinear()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.CubicSpline };
        // No cubic keyframes added, no linear keyframes either
        curve.Evaluate(0f).ShouldBe(Vector3.Zero);
    }

    [Fact]
    public void Vector3Curve_CubicSpline_WithSingleKeyframe_ReturnsValue()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.CubicSpline };
        curve.AddCubicKeyframe(0f, new Vector3(5f, 5f, 5f), Vector3.Zero, Vector3.Zero);

        curve.Evaluate(0f).ShouldBe(new Vector3(5f, 5f, 5f));
        curve.Evaluate(1f).ShouldBe(new Vector3(5f, 5f, 5f));
    }

    [Fact]
    public void Vector3Curve_CubicSpline_InterpolatesSmoothly()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.CubicSpline };
        curve.AddCubicKeyframe(0f, new Vector3(0f, 0f, 0f), Vector3.Zero, Vector3.Zero);
        curve.AddCubicKeyframe(1f, new Vector3(10f, 10f, 10f), Vector3.Zero, Vector3.Zero);

        var mid = curve.Evaluate(0.5f);

        // With zero tangents, should interpolate through midpoint
        mid.X.ShouldBeInRange(4f, 6f);
        mid.Y.ShouldBeInRange(4f, 6f);
        mid.Z.ShouldBeInRange(4f, 6f);
    }

    [Fact]
    public void Vector3Curve_CubicSpline_Duration_ReturnsLastCubicKeyframeTime()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.CubicSpline };
        curve.AddCubicKeyframe(0f, Vector3.Zero, Vector3.Zero, Vector3.Zero);
        curve.AddCubicKeyframe(2.5f, Vector3.One, Vector3.Zero, Vector3.Zero);

        curve.Duration.ShouldBe(2.5f);
    }

    #endregion

    #region QuaternionCurve STEP Interpolation Tests

    [Fact]
    public void QuaternionCurve_StepInterpolation_ReturnsValueAtKeyframe()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.Step };
        var q0 = Quaternion.Identity;
        var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        curve.AddKeyframe(0f, q0);
        curve.AddKeyframe(1f, q1);

        curve.Evaluate(0f).ShouldBe(q0);
        curve.Evaluate(1f).ShouldBe(q1);
    }

    [Fact]
    public void QuaternionCurve_StepInterpolation_HoldsPreviousValue()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.Step };
        var q0 = Quaternion.Identity;
        var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        curve.AddKeyframe(0f, q0);
        curve.AddKeyframe(1f, q1);

        // Between keyframes should hold the previous value
        curve.Evaluate(0.1f).ShouldBe(q0);
        curve.Evaluate(0.5f).ShouldBe(q0);
        curve.Evaluate(0.99f).ShouldBe(q0);
    }

    [Fact]
    public void QuaternionCurve_StepInterpolation_WithNoKeyframes_ReturnsIdentity()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.Step };

        curve.Evaluate(0f).ShouldBe(Quaternion.Identity);
    }

    #endregion

    #region QuaternionCurve CubicSpline Interpolation Tests

    [Fact]
    public void QuaternionCurve_CubicSpline_WithNoKeyframes_FallsBackToLinear()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.CubicSpline };

        curve.Evaluate(0f).ShouldBe(Quaternion.Identity);
    }

    [Fact]
    public void QuaternionCurve_CubicSpline_WithSingleKeyframe_ReturnsValue()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.CubicSpline };
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 4);
        curve.AddCubicKeyframe(0f, q, Quaternion.Identity, Quaternion.Identity);

        var result = curve.Evaluate(0f);

        // Check quaternion is approximately equal (accounting for floating point)
        var dot = MathF.Abs(Quaternion.Dot(result, q));
        dot.ShouldBeGreaterThan(0.999f);
    }

    [Fact]
    public void QuaternionCurve_CubicSpline_InterpolatesAndNormalizes()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.CubicSpline };
        var q0 = Quaternion.Identity;
        var q1 = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

        curve.AddCubicKeyframe(0f, q0, Quaternion.Identity, Quaternion.Identity);
        curve.AddCubicKeyframe(1f, q1, Quaternion.Identity, Quaternion.Identity);

        var mid = curve.Evaluate(0.5f);

        // Result should be normalized
        var length = MathF.Sqrt(mid.X * mid.X + mid.Y * mid.Y + mid.Z * mid.Z + mid.W * mid.W);
        length.ShouldBe(1f, 0.001f);

        // Should be between start and end
        mid.ShouldNotBe(q0);
        mid.ShouldNotBe(q1);
    }

    [Fact]
    public void QuaternionCurve_CubicSpline_Duration_ReturnsLastCubicKeyframeTime()
    {
        var curve = new QuaternionCurve { Interpolation = InterpolationType.CubicSpline };
        curve.AddCubicKeyframe(0f, Quaternion.Identity, Quaternion.Identity, Quaternion.Identity);
        curve.AddCubicKeyframe(3.0f, Quaternion.Identity, Quaternion.Identity, Quaternion.Identity);

        curve.Duration.ShouldBe(3.0f);
    }

    #endregion

    #region InterpolationType Tests

    [Fact]
    public void Vector3Curve_DefaultInterpolation_IsLinear()
    {
        var curve = new Vector3Curve();

        curve.Interpolation.ShouldBe(InterpolationType.Linear);
    }

    [Fact]
    public void QuaternionCurve_DefaultInterpolation_IsLinear()
    {
        var curve = new QuaternionCurve();

        curve.Interpolation.ShouldBe(InterpolationType.Linear);
    }

    [Fact]
    public void Vector3Curve_InterpolationType_CanBeChanged()
    {
        var curve = new Vector3Curve { Interpolation = InterpolationType.Step };
        curve.Interpolation.ShouldBe(InterpolationType.Step);

        curve.Interpolation = InterpolationType.CubicSpline;
        curve.Interpolation.ShouldBe(InterpolationType.CubicSpline);

        curve.Interpolation = InterpolationType.Linear;
        curve.Interpolation.ShouldBe(InterpolationType.Linear);
    }

    #endregion
}
