using KeenEyes.Animation.Data;

namespace KeenEyes.Animation.Tests;

public class AnimationClipTests
{
    #region WrapTime Tests

    [Fact]
    public void WrapTime_Once_ClampsToRange()
    {
        var clip = new AnimationClip { Name = "Test", Duration = 2f, WrapMode = WrapMode.Once };

        clip.WrapTime(-1f).ShouldBe(0f);
        clip.WrapTime(0f).ShouldBe(0f);
        clip.WrapTime(1f).ShouldBe(1f);
        clip.WrapTime(2f).ShouldBe(2f);
        clip.WrapTime(3f).ShouldBe(2f);
    }

    [Fact]
    public void WrapTime_Loop_WrapsTime()
    {
        var clip = new AnimationClip { Name = "Test", Duration = 2f, WrapMode = WrapMode.Loop };

        clip.WrapTime(0f).ShouldBe(0f);
        clip.WrapTime(1f).ShouldBe(1f);
        clip.WrapTime(2f).ShouldBe(0f);
        clip.WrapTime(3f).ShouldBe(1f);
        clip.WrapTime(4f).ShouldBe(0f);
    }

    [Fact]
    public void WrapTime_PingPong_ReverseOnOddCycles()
    {
        var clip = new AnimationClip { Name = "Test", Duration = 2f, WrapMode = WrapMode.PingPong };

        // First cycle: forward
        clip.WrapTime(0f).ShouldBe(0f);
        clip.WrapTime(1f).ShouldBe(1f);

        // Second cycle: backward
        var t = clip.WrapTime(2.5f);
        t.ShouldBe(1.5f);

        // Third cycle: forward again
        var t2 = clip.WrapTime(4.5f);
        t2.ShouldBe(0.5f);
    }

    [Fact]
    public void WrapTime_ClampForever_AllowsExceedingDuration()
    {
        var clip = new AnimationClip { Name = "Test", Duration = 2f, WrapMode = WrapMode.ClampForever };

        clip.WrapTime(-1f).ShouldBe(0f);
        clip.WrapTime(0f).ShouldBe(0f);
        clip.WrapTime(5f).ShouldBe(5f);
        clip.WrapTime(100f).ShouldBe(100f);
    }

    [Fact]
    public void WrapTime_WithZeroDuration_ReturnsZero()
    {
        var clip = new AnimationClip { Name = "Test", Duration = 0f };

        clip.WrapTime(1f).ShouldBe(0f);
        clip.WrapTime(5f).ShouldBe(0f);
    }

    #endregion

    #region BoneTrack Tests

    [Fact]
    public void AddBoneTrack_UpdatesDuration()
    {
        var clip = new AnimationClip { Name = "Test" };

        var track1 = new BoneTrack
        {
            BoneName = "Bone1",
            PositionCurve = CreatePositionCurve(3f)
        };

        var track2 = new BoneTrack
        {
            BoneName = "Bone2",
            PositionCurve = CreatePositionCurve(5f)
        };

        clip.AddBoneTrack(track1);
        clip.Duration.ShouldBe(3f);

        clip.AddBoneTrack(track2);
        clip.Duration.ShouldBe(5f);
    }

    [Fact]
    public void TryGetBoneTrack_ReturnsTrueForExisting()
    {
        var clip = new AnimationClip { Name = "Test" };
        var track = new BoneTrack { BoneName = "TestBone" };

        clip.AddBoneTrack(track);
        var found = clip.TryGetBoneTrack("TestBone", out var retrieved);

        found.ShouldBeTrue();
        retrieved.ShouldBe(track);
    }

    [Fact]
    public void TryGetBoneTrack_ReturnsFalseForMissing()
    {
        var clip = new AnimationClip { Name = "Test" };

        var found = clip.TryGetBoneTrack("NonExistent", out var retrieved);

        found.ShouldBeFalse();
        retrieved.ShouldBeNull();
    }

    private static Vector3Curve CreatePositionCurve(float duration)
    {
        var curve = new Vector3Curve();
        curve.AddKeyframe(0f, System.Numerics.Vector3.Zero);
        curve.AddKeyframe(duration, System.Numerics.Vector3.One);
        return curve;
    }

    #endregion
}
