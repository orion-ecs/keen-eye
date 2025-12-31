using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="AudioClipInfo"/>.
/// </summary>
public class AudioClipInfoTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var handle = new AudioClipHandle(42);
        var info = new AudioClipInfo(
            Handle: handle,
            Format: AudioFormat.Stereo16,
            SampleRate: 44100,
            Channels: 2,
            BitsPerSample: 16,
            Duration: 5.5f);

        Assert.Equal(handle, info.Handle);
        Assert.Equal(AudioFormat.Stereo16, info.Format);
        Assert.Equal(44100, info.SampleRate);
        Assert.Equal(2, info.Channels);
        Assert.Equal(16, info.BitsPerSample);
        Assert.Equal(5.5f, info.Duration);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var handle = new AudioClipHandle(1);
        var info1 = new AudioClipInfo(handle, AudioFormat.Mono8, 22050, 1, 8, 2.0f);
        var info2 = new AudioClipInfo(handle, AudioFormat.Mono8, 22050, 1, 8, 2.0f);

        Assert.Equal(info1, info2);
        Assert.True(info1 == info2);
    }

    [Fact]
    public void Equality_DifferentHandle_AreNotEqual()
    {
        var info1 = new AudioClipInfo(new AudioClipHandle(1), AudioFormat.Mono8, 22050, 1, 8, 2.0f);
        var info2 = new AudioClipInfo(new AudioClipHandle(2), AudioFormat.Mono8, 22050, 1, 8, 2.0f);

        Assert.NotEqual(info1, info2);
        Assert.True(info1 != info2);
    }

    [Fact]
    public void Equality_DifferentFormat_AreNotEqual()
    {
        var handle = new AudioClipHandle(1);
        var info1 = new AudioClipInfo(handle, AudioFormat.Mono8, 22050, 1, 8, 2.0f);
        var info2 = new AudioClipInfo(handle, AudioFormat.Stereo16, 22050, 1, 8, 2.0f);

        Assert.NotEqual(info1, info2);
    }

    [Fact]
    public void Equality_DifferentSampleRate_AreNotEqual()
    {
        var handle = new AudioClipHandle(1);
        var info1 = new AudioClipInfo(handle, AudioFormat.Mono8, 22050, 1, 8, 2.0f);
        var info2 = new AudioClipInfo(handle, AudioFormat.Mono8, 44100, 1, 8, 2.0f);

        Assert.NotEqual(info1, info2);
    }

    [Fact]
    public void GetHashCode_SameForEqualInfos()
    {
        var handle = new AudioClipHandle(42);
        var info1 = new AudioClipInfo(handle, AudioFormat.Stereo16, 48000, 2, 16, 3.0f);
        var info2 = new AudioClipInfo(handle, AudioFormat.Stereo16, 48000, 2, 16, 3.0f);

        Assert.Equal(info1.GetHashCode(), info2.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsRelevantInfo()
    {
        var handle = new AudioClipHandle(10);
        var info = new AudioClipInfo(handle, AudioFormat.Stereo16, 44100, 2, 16, 2.5f);

        var str = info.ToString();

        Assert.Contains("Handle", str);
        Assert.Contains("Format", str);
        Assert.Contains("SampleRate", str);
        Assert.Contains("Channels", str);
    }

    [Theory]
    [InlineData(AudioFormat.Mono8)]
    [InlineData(AudioFormat.Mono16)]
    [InlineData(AudioFormat.Stereo8)]
    [InlineData(AudioFormat.Stereo16)]
    public void Format_AcceptsAllValues(AudioFormat format)
    {
        var handle = new AudioClipHandle(1);
        var info = new AudioClipInfo(handle, format, 44100, 2, 16, 1.0f);

        Assert.Equal(format, info.Format);
    }

    [Theory]
    [InlineData(8000)]
    [InlineData(22050)]
    [InlineData(44100)]
    [InlineData(48000)]
    [InlineData(96000)]
    public void SampleRate_AcceptsCommonValues(int sampleRate)
    {
        var handle = new AudioClipHandle(1);
        var info = new AudioClipInfo(handle, AudioFormat.Stereo16, sampleRate, 2, 16, 1.0f);

        Assert.Equal(sampleRate, info.SampleRate);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Channels_AcceptsMonoAndStereo(int channels)
    {
        var handle = new AudioClipHandle(1);
        var info = new AudioClipInfo(handle, AudioFormat.Stereo16, 44100, channels, 16, 1.0f);

        Assert.Equal(channels, info.Channels);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void BitsPerSample_Accepts8And16Bit(int bits)
    {
        var handle = new AudioClipHandle(1);
        var info = new AudioClipInfo(handle, AudioFormat.Stereo16, 44100, 2, bits, 1.0f);

        Assert.Equal(bits, info.BitsPerSample);
    }

    [Fact]
    public void WithExpression_ModifiesSingleProperty()
    {
        var handle = new AudioClipHandle(1);
        var original = new AudioClipInfo(handle, AudioFormat.Mono8, 22050, 1, 8, 1.0f);
        var modified = original with { Duration = 5.0f };

        Assert.Equal(1.0f, original.Duration);
        Assert.Equal(5.0f, modified.Duration);
        Assert.Equal(original.Handle, modified.Handle);
        Assert.Equal(original.Format, modified.Format);
    }
}
