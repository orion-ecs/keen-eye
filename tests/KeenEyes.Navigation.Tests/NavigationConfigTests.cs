using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Tests;

/// <summary>
/// Tests for NavigationConfig validation.
/// </summary>
public class NavigationConfigTests
{
    #region Default Configuration

    [Fact]
    public void Default_ReturnsValidConfig()
    {
        var config = NavigationConfig.Default;

        var error = config.Validate();

        error.ShouldBeNull();
    }

    [Fact]
    public void Default_HasExpectedDefaults()
    {
        var config = NavigationConfig.Default;

        config.Strategy.ShouldBe(NavigationStrategy.Grid);
        config.MaxPathRequestsPerFrame.ShouldBe(10);
        config.MaxPendingRequests.ShouldBe(100);
        config.AgentSteeringEnabled.ShouldBeTrue();
        config.DynamicObstaclesEnabled.ShouldBeTrue();
    }

    #endregion

    #region Strategy Validation

    [Fact]
    public void Validate_CustomStrategyWithoutProvider_ReturnsError()
    {
        var config = new NavigationConfig
        {
            Strategy = NavigationStrategy.Custom,
            CustomProvider = null
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("CustomProvider");
    }

    #endregion

    #region MaxPathRequestsPerFrame Validation

    [Fact]
    public void Validate_ZeroMaxPathRequestsPerFrame_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxPathRequestsPerFrame = 0
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxPathRequestsPerFrame");
    }

    [Fact]
    public void Validate_NegativeMaxPathRequestsPerFrame_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxPathRequestsPerFrame = -5
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxPathRequestsPerFrame");
    }

    [Fact]
    public void Validate_PositiveMaxPathRequestsPerFrame_Succeeds()
    {
        var config = new NavigationConfig
        {
            MaxPathRequestsPerFrame = 50
        };

        var error = config.Validate();

        error.ShouldBeNull();
    }

    #endregion

    #region MaxPendingRequests Validation

    [Fact]
    public void Validate_ZeroMaxPendingRequests_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxPendingRequests = 0
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxPendingRequests");
    }

    [Fact]
    public void Validate_NegativeMaxPendingRequests_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxPendingRequests = -10
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxPendingRequests");
    }

    #endregion

    #region WaypointReachDistance Validation

    [Fact]
    public void Validate_ZeroWaypointReachDistance_ReturnsError()
    {
        var config = new NavigationConfig
        {
            WaypointReachDistance = 0f
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("WaypointReachDistance");
    }

    [Fact]
    public void Validate_NegativeWaypointReachDistance_ReturnsError()
    {
        var config = new NavigationConfig
        {
            WaypointReachDistance = -0.5f
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("WaypointReachDistance");
    }

    #endregion

    #region ObstacleUpdateInterval Validation

    [Fact]
    public void Validate_ZeroObstacleUpdateInterval_Succeeds()
    {
        var config = new NavigationConfig
        {
            ObstacleUpdateInterval = 0f
        };

        var error = config.Validate();

        error.ShouldBeNull();
    }

    [Fact]
    public void Validate_NegativeObstacleUpdateInterval_ReturnsError()
    {
        var config = new NavigationConfig
        {
            ObstacleUpdateInterval = -0.1f
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("ObstacleUpdateInterval");
    }

    #endregion

    #region MaxProjectionDistance Validation

    [Fact]
    public void Validate_ZeroMaxProjectionDistance_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxProjectionDistance = 0f
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxProjectionDistance");
    }

    [Fact]
    public void Validate_NegativeMaxProjectionDistance_ReturnsError()
    {
        var config = new NavigationConfig
        {
            MaxProjectionDistance = -5f
        };

        var error = config.Validate();

        error.ShouldNotBeNull();
        error.ShouldContain("MaxProjectionDistance");
    }

    #endregion
}
