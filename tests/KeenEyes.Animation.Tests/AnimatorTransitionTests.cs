using KeenEyes.Animation.Data;
using KeenEyes.Animation.Tweening;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Tests for AnimatorTransition struct.
/// </summary>
public class AnimatorTransitionTests
{
    [Fact]
    public void Constructor_AllParams_SetsProperties()
    {
        var transition = new AnimatorTransition(
            TargetStateHash: 12345,
            Duration: 0.5f,
            ExitTime: 0.8f,
            EaseType: EaseType.SineInOut);

        Assert.Equal(12345, transition.TargetStateHash);
        Assert.Equal(0.5f, transition.Duration);
        Assert.Equal(0.8f, transition.ExitTime);
        Assert.Equal(EaseType.SineInOut, transition.EaseType);
    }

    [Fact]
    public void Constructor_DefaultExitTime_IsNull()
    {
        var transition = new AnimatorTransition(
            TargetStateHash: 123,
            Duration: 0.3f);

        Assert.Null(transition.ExitTime);
    }

    [Fact]
    public void Constructor_DefaultEaseType_IsLinear()
    {
        var transition = new AnimatorTransition(
            TargetStateHash: 123,
            Duration: 0.3f);

        Assert.Equal(EaseType.Linear, transition.EaseType);
    }

    [Fact]
    public void Constructor_ZeroDuration_IsValid()
    {
        var transition = new AnimatorTransition(
            TargetStateHash: 123,
            Duration: 0f);

        Assert.Equal(0f, transition.Duration);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var t1 = new AnimatorTransition(123, 0.5f, 0.8f, EaseType.QuadIn);
        var t2 = new AnimatorTransition(123, 0.5f, 0.8f, EaseType.QuadIn);

        Assert.Equal(t1, t2);
        Assert.True(t1 == t2);
    }

    [Fact]
    public void Equality_DifferentTargetState_AreNotEqual()
    {
        var t1 = new AnimatorTransition(123, 0.5f, 0.8f, EaseType.QuadIn);
        var t2 = new AnimatorTransition(456, 0.5f, 0.8f, EaseType.QuadIn);

        Assert.NotEqual(t1, t2);
        Assert.True(t1 != t2);
    }

    [Fact]
    public void Equality_DifferentDuration_AreNotEqual()
    {
        var t1 = new AnimatorTransition(123, 0.5f);
        var t2 = new AnimatorTransition(123, 1.0f);

        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void Equality_DifferentEaseType_AreNotEqual()
    {
        var t1 = new AnimatorTransition(123, 0.5f, null, EaseType.Linear);
        var t2 = new AnimatorTransition(123, 0.5f, null, EaseType.QuadIn);

        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void Equality_NullVsValueExitTime_AreNotEqual()
    {
        var t1 = new AnimatorTransition(123, 0.5f, null, EaseType.Linear);
        var t2 = new AnimatorTransition(123, 0.5f, 0.5f, EaseType.Linear);

        Assert.NotEqual(t1, t2);
    }

    [Fact]
    public void GetHashCode_SameValues_SameHashCode()
    {
        var t1 = new AnimatorTransition(123, 0.5f, 0.8f, EaseType.QuadIn);
        var t2 = new AnimatorTransition(123, 0.5f, 0.8f, EaseType.QuadIn);

        Assert.Equal(t1.GetHashCode(), t2.GetHashCode());
    }
}
