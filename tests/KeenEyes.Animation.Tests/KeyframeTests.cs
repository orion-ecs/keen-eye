using System.Numerics;
using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Tests;

/// <summary>
/// Tests for keyframe types.
/// </summary>
public class KeyframeTests
{
    #region Keyframe<T> Tests

    [Fact]
    public void Keyframe_Float_ConstructorSetsProperties()
    {
        var keyframe = new Keyframe<float>(1.5f, 42f);

        Assert.Equal(1.5f, keyframe.Time);
        Assert.Equal(42f, keyframe.Value);
    }

    [Fact]
    public void Keyframe_Int_ConstructorSetsProperties()
    {
        var keyframe = new Keyframe<int>(2.0f, 100);

        Assert.Equal(2.0f, keyframe.Time);
        Assert.Equal(100, keyframe.Value);
    }

    [Fact]
    public void Keyframe_Vector3_ConstructorSetsProperties()
    {
        var value = new Vector3(1f, 2f, 3f);
        var keyframe = new Keyframe<Vector3>(0.5f, value);

        Assert.Equal(0.5f, keyframe.Time);
        Assert.Equal(value, keyframe.Value);
    }

    [Fact]
    public void Keyframe_Equality_SameValues_AreEqual()
    {
        var kf1 = new Keyframe<float>(1.0f, 5f);
        var kf2 = new Keyframe<float>(1.0f, 5f);

        Assert.Equal(kf1, kf2);
        Assert.True(kf1 == kf2);
    }

    [Fact]
    public void Keyframe_Equality_DifferentTime_AreNotEqual()
    {
        var kf1 = new Keyframe<float>(1.0f, 5f);
        var kf2 = new Keyframe<float>(2.0f, 5f);

        Assert.NotEqual(kf1, kf2);
        Assert.True(kf1 != kf2);
    }

    [Fact]
    public void Keyframe_Equality_DifferentValue_AreNotEqual()
    {
        var kf1 = new Keyframe<float>(1.0f, 5f);
        var kf2 = new Keyframe<float>(1.0f, 10f);

        Assert.NotEqual(kf1, kf2);
    }

    #endregion

    #region FloatKeyframe Tests

    [Fact]
    public void FloatKeyframe_ConstructorWithAllParams_SetsProperties()
    {
        var keyframe = new FloatKeyframe(1.0f, 5f, 0.5f, 0.8f);

        Assert.Equal(1.0f, keyframe.Time);
        Assert.Equal(5f, keyframe.Value);
        Assert.Equal(0.5f, keyframe.InTangent);
        Assert.Equal(0.8f, keyframe.OutTangent);
    }

    [Fact]
    public void FloatKeyframe_DefaultTangents_AreZero()
    {
        var keyframe = new FloatKeyframe(1.0f, 5f);

        Assert.Equal(0f, keyframe.InTangent);
        Assert.Equal(0f, keyframe.OutTangent);
    }

    [Fact]
    public void FloatKeyframe_Equality_SameValues_AreEqual()
    {
        var kf1 = new FloatKeyframe(1.0f, 5f, 0.5f, 0.8f);
        var kf2 = new FloatKeyframe(1.0f, 5f, 0.5f, 0.8f);

        Assert.Equal(kf1, kf2);
        Assert.True(kf1 == kf2);
    }

    [Fact]
    public void FloatKeyframe_Equality_DifferentTangent_AreNotEqual()
    {
        var kf1 = new FloatKeyframe(1.0f, 5f, 0.5f, 0.8f);
        var kf2 = new FloatKeyframe(1.0f, 5f, 0.5f, 1.0f);

        Assert.NotEqual(kf1, kf2);
    }

    #endregion

    #region Vector3Keyframe Tests

    [Fact]
    public void Vector3Keyframe_Constructor_SetsProperties()
    {
        var value = new Vector3(10f, 20f, 30f);
        var keyframe = new Vector3Keyframe(2.5f, value);

        Assert.Equal(2.5f, keyframe.Time);
        Assert.Equal(value, keyframe.Value);
    }

    [Fact]
    public void Vector3Keyframe_Equality_SameValues_AreEqual()
    {
        var value = new Vector3(1f, 2f, 3f);
        var kf1 = new Vector3Keyframe(1.0f, value);
        var kf2 = new Vector3Keyframe(1.0f, value);

        Assert.Equal(kf1, kf2);
    }

    [Fact]
    public void Vector3Keyframe_Equality_DifferentValue_AreNotEqual()
    {
        var kf1 = new Vector3Keyframe(1.0f, new Vector3(1f, 2f, 3f));
        var kf2 = new Vector3Keyframe(1.0f, new Vector3(4f, 5f, 6f));

        Assert.NotEqual(kf1, kf2);
    }

    #endregion

    #region QuaternionKeyframe Tests

    [Fact]
    public void QuaternionKeyframe_Constructor_SetsProperties()
    {
        var value = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.2f);
        var keyframe = new QuaternionKeyframe(3.0f, value);

        Assert.Equal(3.0f, keyframe.Time);
        Assert.Equal(value, keyframe.Value);
    }

    [Fact]
    public void QuaternionKeyframe_Identity_CanBeStored()
    {
        var keyframe = new QuaternionKeyframe(0f, Quaternion.Identity);

        Assert.Equal(Quaternion.Identity, keyframe.Value);
    }

    [Fact]
    public void QuaternionKeyframe_Equality_SameValues_AreEqual()
    {
        var value = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.2f);
        var kf1 = new QuaternionKeyframe(1.0f, value);
        var kf2 = new QuaternionKeyframe(1.0f, value);

        Assert.Equal(kf1, kf2);
    }

    #endregion
}
