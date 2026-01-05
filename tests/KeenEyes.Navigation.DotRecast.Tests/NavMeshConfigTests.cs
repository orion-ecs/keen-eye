using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for <see cref="NavMeshConfig"/> class.
/// </summary>
public class NavMeshConfigTests
{
    #region Default Config Tests

    [Fact]
    public void Default_ReturnsValidConfig()
    {
        var config = NavMeshConfig.Default;

        Assert.NotNull(config);
        Assert.Null(config.Validate());
    }

    [Fact]
    public void Default_HasReasonableDefaults()
    {
        var config = NavMeshConfig.Default;

        Assert.Equal(0.3f, config.CellSize);
        Assert.Equal(0.2f, config.CellHeight);
        Assert.True(config.AgentHeight > 0);
        Assert.True(config.AgentRadius > 0);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_ValidConfig_ReturnsNull()
    {
        var config = NavMeshConfig.Default;

        Assert.Null(config.Validate());
    }

    [Fact]
    public void Validate_ZeroCellSize_ReturnsError()
    {
        var config = new NavMeshConfig { CellSize = 0 };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_NegativeCellSize_ReturnsError()
    {
        var config = new NavMeshConfig { CellSize = -1f };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_ZeroCellHeight_ReturnsError()
    {
        var config = new NavMeshConfig { CellHeight = 0 };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_ZeroAgentRadius_ReturnsError()
    {
        var config = new NavMeshConfig { AgentRadius = 0 };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_ZeroAgentHeight_ReturnsError()
    {
        var config = new NavMeshConfig { AgentHeight = 0 };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_NegativeMaxSlopeAngle_ReturnsError()
    {
        var config = new NavMeshConfig { MaxSlopeAngle = -10f };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_TooLargeMaxSlopeAngle_ReturnsError()
    {
        var config = new NavMeshConfig { MaxSlopeAngle = 100f };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_ZeroMaxVertsPerPoly_ReturnsError()
    {
        var config = new NavMeshConfig { MaxVertsPerPoly = 0 };

        Assert.NotNull(config.Validate());
    }

    [Fact]
    public void Validate_TooLargeMaxVertsPerPoly_ReturnsError()
    {
        var config = new NavMeshConfig { MaxVertsPerPoly = 7 };

        Assert.NotNull(config.Validate());
    }

    #endregion

    #region ToAgentSettings Tests

    [Fact]
    public void ToAgentSettings_ReturnsCorrectSettings()
    {
        var config = new NavMeshConfig
        {
            AgentRadius = 0.5f,
            AgentHeight = 2.0f,
            MaxSlopeAngle = 45f,
            MaxClimbHeight = 0.5f
        };

        var agentSettings = config.ToAgentSettings();

        Assert.Equal(0.5f, agentSettings.Radius);
        Assert.Equal(2.0f, agentSettings.Height);
        Assert.Equal(45f, agentSettings.MaxSlopeAngle);
        Assert.Equal(0.5f, agentSettings.StepHeight);
    }

    #endregion
}
