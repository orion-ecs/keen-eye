using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for <see cref="AgentSettings"/>.
/// </summary>
public class AgentSettingsTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var settings = new AgentSettings(0.6f, 1.8f, 40f, 0.3f);

        Assert.Equal(0.6f, settings.Radius);
        Assert.Equal(1.8f, settings.Height);
        Assert.Equal(40f, settings.MaxSlopeAngle);
        Assert.Equal(0.3f, settings.StepHeight);
    }

    [Fact]
    public void Default_ReturnsReasonableValues()
    {
        var settings = AgentSettings.Default;

        Assert.Equal(0.5f, settings.Radius);
        Assert.Equal(2.0f, settings.Height);
        Assert.Equal(45f, settings.MaxSlopeAngle);
        Assert.Equal(0.4f, settings.StepHeight);
    }

    [Fact]
    public void Small_ReturnsSmallerValues()
    {
        var settings = AgentSettings.Small;

        Assert.Equal(0.3f, settings.Radius);
        Assert.Equal(1.0f, settings.Height);
        Assert.Equal(50f, settings.MaxSlopeAngle);
        Assert.Equal(0.25f, settings.StepHeight);
    }

    [Fact]
    public void Large_ReturnsLargerValues()
    {
        var settings = AgentSettings.Large;

        Assert.Equal(1.5f, settings.Radius);
        Assert.Equal(3.0f, settings.Height);
        Assert.Equal(30f, settings.MaxSlopeAngle);
        Assert.Equal(0.6f, settings.StepHeight);
    }

    [Fact]
    public void WithRadius_CreatesSettingsWithSpecifiedRadius()
    {
        var settings = AgentSettings.WithRadius(1.0f);

        Assert.Equal(1.0f, settings.Radius);
        Assert.Equal(AgentSettings.Default.Height, settings.Height);
        Assert.Equal(AgentSettings.Default.MaxSlopeAngle, settings.MaxSlopeAngle);
        Assert.Equal(AgentSettings.Default.StepHeight, settings.StepHeight);
    }

    [Fact]
    public void Diameter_ReturnsDoubleRadius()
    {
        var settings = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);

        Assert.Equal(1.0f, settings.Diameter);
    }

    [Fact]
    public void IsValid_WithValidSettings_ReturnsTrue()
    {
        var settings = AgentSettings.Default;

        Assert.True(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithZeroRadius_ReturnsFalse()
    {
        var settings = new AgentSettings(0f, 2.0f, 45f, 0.4f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithNegativeRadius_ReturnsFalse()
    {
        var settings = new AgentSettings(-0.5f, 2.0f, 45f, 0.4f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithZeroHeight_ReturnsFalse()
    {
        var settings = new AgentSettings(0.5f, 0f, 45f, 0.4f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithZeroSlope_ReturnsFalse()
    {
        var settings = new AgentSettings(0.5f, 2.0f, 0f, 0.4f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithSlopeAt90Degrees_ReturnsFalse()
    {
        var settings = new AgentSettings(0.5f, 2.0f, 90f, 0.4f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithStepHeightEqualToHeight_ReturnsFalse()
    {
        var settings = new AgentSettings(0.5f, 2.0f, 45f, 2.0f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void IsValid_WithNegativeStepHeight_ReturnsFalse()
    {
        var settings = new AgentSettings(0.5f, 2.0f, 45f, -0.1f);

        Assert.False(settings.IsValid());
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var settings1 = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);
        var settings2 = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);

        Assert.Equal(settings1, settings2);
        Assert.True(settings1 == settings2);
    }

    [Fact]
    public void Equality_DifferentRadius_AreNotEqual()
    {
        var settings1 = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);
        var settings2 = new AgentSettings(0.6f, 2.0f, 45f, 0.4f);

        Assert.NotEqual(settings1, settings2);
        Assert.True(settings1 != settings2);
    }

    [Fact]
    public void GetHashCode_SameForEqualSettings()
    {
        var settings1 = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);
        var settings2 = new AgentSettings(0.5f, 2.0f, 45f, 0.4f);

        Assert.Equal(settings1.GetHashCode(), settings2.GetHashCode());
    }

    [Fact]
    public void WithExpression_ModifiesSingleProperty()
    {
        var original = AgentSettings.Default;
        var modified = original with { Radius = 1.0f };

        Assert.Equal(0.5f, original.Radius);
        Assert.Equal(1.0f, modified.Radius);
        Assert.Equal(original.Height, modified.Height);
    }
}
