using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for audio enum types.
/// </summary>
public class AudioEnumTests
{
    #region AudioFormat Tests

    [Fact]
    public void AudioFormat_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Mono8));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Mono16));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Stereo8));
        Assert.True(Enum.IsDefined(typeof(AudioFormat), AudioFormat.Stereo16));
    }

    [Fact]
    public void AudioFormat_HasFourValues()
    {
        var values = Enum.GetValues<AudioFormat>();

        Assert.Equal(4, values.Length);
    }

    #endregion

    #region AudioPlayState Tests

    [Fact]
    public void AudioPlayState_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(AudioPlayState), AudioPlayState.Stopped));
        Assert.True(Enum.IsDefined(typeof(AudioPlayState), AudioPlayState.Playing));
        Assert.True(Enum.IsDefined(typeof(AudioPlayState), AudioPlayState.Paused));
    }

    [Fact]
    public void AudioPlayState_HasThreeValues()
    {
        var values = Enum.GetValues<AudioPlayState>();

        Assert.Equal(3, values.Length);
    }

    #endregion

    #region AudioRolloffMode Tests

    [Fact]
    public void AudioRolloffMode_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(AudioRolloffMode), AudioRolloffMode.Linear));
        Assert.True(Enum.IsDefined(typeof(AudioRolloffMode), AudioRolloffMode.Logarithmic));
        Assert.True(Enum.IsDefined(typeof(AudioRolloffMode), AudioRolloffMode.Exponential));
        Assert.True(Enum.IsDefined(typeof(AudioRolloffMode), AudioRolloffMode.Custom));
    }

    [Fact]
    public void AudioRolloffMode_HasFourValues()
    {
        var values = Enum.GetValues<AudioRolloffMode>();

        Assert.Equal(4, values.Length);
    }

    #endregion

    #region AudioChannel Tests

    [Fact]
    public void AudioChannel_HasExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(AudioChannel), AudioChannel.Master));
        Assert.True(Enum.IsDefined(typeof(AudioChannel), AudioChannel.Music));
        Assert.True(Enum.IsDefined(typeof(AudioChannel), AudioChannel.SFX));
        Assert.True(Enum.IsDefined(typeof(AudioChannel), AudioChannel.Voice));
        Assert.True(Enum.IsDefined(typeof(AudioChannel), AudioChannel.Ambient));
    }

    [Fact]
    public void AudioChannel_HasFiveValues()
    {
        var values = Enum.GetValues<AudioChannel>();

        Assert.Equal(5, values.Length);
    }

    #endregion
}
