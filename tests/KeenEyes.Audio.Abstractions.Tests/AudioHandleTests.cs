using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for audio handle types.
/// </summary>
public class AudioHandleTests
{
    #region AudioClipHandle Tests

    [Fact]
    public void AudioClipHandle_WithPositiveId_IsValid()
    {
        var handle = new AudioClipHandle(0);
        Assert.True(handle.IsValid);

        handle = new AudioClipHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void AudioClipHandle_WithNegativeId_IsInvalid()
    {
        var handle = new AudioClipHandle(-1);
        Assert.False(handle.IsValid);

        handle = new AudioClipHandle(-100);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void AudioClipHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, AudioClipHandle.Invalid.Id);
        Assert.False(AudioClipHandle.Invalid.IsValid);
    }

    [Fact]
    public void AudioClipHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new AudioClipHandle(42);
        Assert.Equal("AudioClip(42)", handle.ToString());
    }

    [Fact]
    public void AudioClipHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("AudioClip(Invalid)", AudioClipHandle.Invalid.ToString());
    }

    [Fact]
    public void AudioClipHandle_Equality_SameId_AreEqual()
    {
        var handle1 = new AudioClipHandle(5);
        var handle2 = new AudioClipHandle(5);

        Assert.Equal(handle1, handle2);
        Assert.True(handle1 == handle2);
    }

    [Fact]
    public void AudioClipHandle_Equality_DifferentId_AreNotEqual()
    {
        var handle1 = new AudioClipHandle(5);
        var handle2 = new AudioClipHandle(6);

        Assert.NotEqual(handle1, handle2);
        Assert.True(handle1 != handle2);
    }

    [Fact]
    public void AudioClipHandle_GetHashCode_SameForEqualHandles()
    {
        var handle1 = new AudioClipHandle(42);
        var handle2 = new AudioClipHandle(42);

        Assert.Equal(handle1.GetHashCode(), handle2.GetHashCode());
    }

    #endregion

    #region SoundHandle Tests

    [Fact]
    public void SoundHandle_WithPositiveId_IsValid()
    {
        var handle = new SoundHandle(0);
        Assert.True(handle.IsValid);

        handle = new SoundHandle(42);
        Assert.True(handle.IsValid);
    }

    [Fact]
    public void SoundHandle_WithNegativeId_IsInvalid()
    {
        var handle = new SoundHandle(-1);
        Assert.False(handle.IsValid);

        handle = new SoundHandle(-100);
        Assert.False(handle.IsValid);
    }

    [Fact]
    public void SoundHandle_Invalid_HasNegativeOneId()
    {
        Assert.Equal(-1, SoundHandle.Invalid.Id);
        Assert.False(SoundHandle.Invalid.IsValid);
    }

    [Fact]
    public void SoundHandle_ToString_ValidHandle_ShowsId()
    {
        var handle = new SoundHandle(42);
        Assert.Equal("Sound(42)", handle.ToString());
    }

    [Fact]
    public void SoundHandle_ToString_InvalidHandle_ShowsInvalid()
    {
        Assert.Equal("Sound(Invalid)", SoundHandle.Invalid.ToString());
    }

    [Fact]
    public void SoundHandle_Equality_SameId_AreEqual()
    {
        var handle1 = new SoundHandle(5);
        var handle2 = new SoundHandle(5);

        Assert.Equal(handle1, handle2);
        Assert.True(handle1 == handle2);
    }

    [Fact]
    public void SoundHandle_Equality_DifferentId_AreNotEqual()
    {
        var handle1 = new SoundHandle(5);
        var handle2 = new SoundHandle(6);

        Assert.NotEqual(handle1, handle2);
        Assert.True(handle1 != handle2);
    }

    [Fact]
    public void SoundHandle_GetHashCode_SameForEqualHandles()
    {
        var handle1 = new SoundHandle(42);
        var handle2 = new SoundHandle(42);

        Assert.Equal(handle1.GetHashCode(), handle2.GetHashCode());
    }

    #endregion
}
