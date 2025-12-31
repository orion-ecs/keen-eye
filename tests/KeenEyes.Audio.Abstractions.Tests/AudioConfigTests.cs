using KeenEyes.Audio.Abstractions;

namespace KeenEyes.Audio.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="AudioConfig"/>.
/// </summary>
public class AudioConfigTests
{
    [Fact]
    public void Default_ReturnsNewInstance()
    {
        var config = AudioConfig.Default;

        Assert.NotNull(config);
    }

    [Fact]
    public void Default_HasStandardValues()
    {
        var config = AudioConfig.Default;

        Assert.Equal(32, config.MaxSimultaneousSounds);
        Assert.Equal(1f, config.DefaultMinDistance);
        Assert.Equal(100f, config.DefaultMaxDistance);
        Assert.Equal(AudioRolloffMode.Logarithmic, config.DefaultRolloff);
        Assert.Equal(1f, config.DopplerFactor);
        Assert.Equal(343f, config.SpeedOfSound);
    }

    [Fact]
    public void Constructor_DefaultValues_AreApplied()
    {
        var config = new AudioConfig();

        Assert.Equal(32, config.MaxSimultaneousSounds);
        Assert.Equal(1f, config.DefaultMinDistance);
        Assert.Equal(100f, config.DefaultMaxDistance);
        Assert.Equal(AudioRolloffMode.Logarithmic, config.DefaultRolloff);
        Assert.Equal(1f, config.DopplerFactor);
        Assert.Equal(343f, config.SpeedOfSound);
    }

    [Fact]
    public void Constructor_WithCustomValues_SetsProperties()
    {
        var config = new AudioConfig
        {
            MaxSimultaneousSounds = 64,
            DefaultMinDistance = 2f,
            DefaultMaxDistance = 50f,
            DefaultRolloff = AudioRolloffMode.Linear,
            DopplerFactor = 0.5f,
            SpeedOfSound = 400f
        };

        Assert.Equal(64, config.MaxSimultaneousSounds);
        Assert.Equal(2f, config.DefaultMinDistance);
        Assert.Equal(50f, config.DefaultMaxDistance);
        Assert.Equal(AudioRolloffMode.Linear, config.DefaultRolloff);
        Assert.Equal(0.5f, config.DopplerFactor);
        Assert.Equal(400f, config.SpeedOfSound);
    }

    [Theory]
    [InlineData(AudioRolloffMode.Linear)]
    [InlineData(AudioRolloffMode.Logarithmic)]
    [InlineData(AudioRolloffMode.Exponential)]
    [InlineData(AudioRolloffMode.Custom)]
    public void DefaultRolloff_AcceptsAllValues(AudioRolloffMode mode)
    {
        var config = new AudioConfig { DefaultRolloff = mode };

        Assert.Equal(mode, config.DefaultRolloff);
    }

    [Fact]
    public void DopplerFactor_ZeroDisablesDoppler()
    {
        var config = new AudioConfig { DopplerFactor = 0f };

        Assert.Equal(0f, config.DopplerFactor);
    }

    [Fact]
    public void DopplerFactor_GreaterThanOneExaggeratesEffect()
    {
        var config = new AudioConfig { DopplerFactor = 2f };

        Assert.Equal(2f, config.DopplerFactor);
    }
}
