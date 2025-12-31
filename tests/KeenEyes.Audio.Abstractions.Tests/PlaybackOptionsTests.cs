using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="PlaybackOptions"/>.
/// </summary>
public class PlaybackOptionsTests
{
    [Fact]
    public void Default_HasStandardValues()
    {
        var options = PlaybackOptions.Default;

        Assert.Equal(1f, options.Volume);
        Assert.Equal(1f, options.Pitch);
        Assert.False(options.Loop);
        Assert.Equal(AudioChannel.SFX, options.Channel);
    }

    [Fact]
    public void Constructor_SetsProperties()
    {
        var options = new PlaybackOptions
        {
            Volume = 0.5f,
            Pitch = 1.5f,
            Loop = true,
            Channel = AudioChannel.Music
        };

        Assert.Equal(0.5f, options.Volume);
        Assert.Equal(1.5f, options.Pitch);
        Assert.True(options.Loop);
        Assert.Equal(AudioChannel.Music, options.Channel);
    }

    [Fact]
    public void WithExpression_ModifiesSingleProperty()
    {
        var original = PlaybackOptions.Default;
        var modified = original with { Volume = 0.8f };

        Assert.Equal(0.8f, modified.Volume);
        Assert.Equal(1f, modified.Pitch);
        Assert.False(modified.Loop);
        Assert.Equal(AudioChannel.SFX, modified.Channel);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var options1 = new PlaybackOptions { Volume = 0.5f, Pitch = 1.5f, Loop = true, Channel = AudioChannel.Music };
        var options2 = new PlaybackOptions { Volume = 0.5f, Pitch = 1.5f, Loop = true, Channel = AudioChannel.Music };

        Assert.Equal(options1, options2);
        Assert.True(options1 == options2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var options1 = new PlaybackOptions { Volume = 0.5f };
        var options2 = new PlaybackOptions { Volume = 0.8f };

        Assert.NotEqual(options1, options2);
        Assert.True(options1 != options2);
    }

    [Fact]
    public void GetHashCode_SameForEqualOptions()
    {
        var options1 = PlaybackOptions.Default;
        var options2 = PlaybackOptions.Default;

        Assert.Equal(options1.GetHashCode(), options2.GetHashCode());
    }

    [Fact]
    public void ToString_ContainsPropertyValues()
    {
        var options = new PlaybackOptions
        {
            Volume = 0.75f,
            Pitch = 1.25f,
            Loop = true,
            Channel = AudioChannel.Voice
        };

        var str = options.ToString();

        Assert.Contains("Volume", str);
        Assert.Contains("Pitch", str);
        Assert.Contains("Loop", str);
        Assert.Contains("Channel", str);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void Volume_AcceptsVariousValues(float volume)
    {
        var options = new PlaybackOptions { Volume = volume };

        Assert.Equal(volume, options.Volume);
    }

    [Theory]
    [InlineData(0.5f)]
    [InlineData(1f)]
    [InlineData(2f)]
    public void Pitch_AcceptsVariousValues(float pitch)
    {
        var options = new PlaybackOptions { Pitch = pitch };

        Assert.Equal(pitch, options.Pitch);
    }

    [Theory]
    [InlineData(AudioChannel.Master)]
    [InlineData(AudioChannel.Music)]
    [InlineData(AudioChannel.SFX)]
    [InlineData(AudioChannel.Voice)]
    [InlineData(AudioChannel.Ambient)]
    public void Channel_AcceptsAllValues(AudioChannel channel)
    {
        var options = new PlaybackOptions { Channel = channel };

        Assert.Equal(channel, options.Channel);
    }
}
