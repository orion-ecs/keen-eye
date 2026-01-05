using System.Numerics;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Grid.Tests;

/// <summary>
/// Tests for <see cref="GridConfig"/> validation and configuration.
/// </summary>
public class GridConfigTests
{
    #region Default Values Tests

    [Fact]
    public void Default_HasValidDefaults()
    {
        var config = GridConfig.Default;

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Default_HasExpectedValues()
    {
        var config = GridConfig.Default;

        Assert.Equal(100, config.Width);
        Assert.Equal(100, config.Height);
        Assert.Equal(1f, config.CellSize);
        Assert.Equal(Vector3.Zero, config.WorldOrigin);
        Assert.True(config.AllowDiagonal);
        Assert.Equal(GridHeuristic.Octile, config.Heuristic);
        Assert.Equal(10000, config.MaxIterations);
    }

    [Fact]
    public void WithSize_CreatesConfigWithCorrectSize()
    {
        var config = GridConfig.WithSize(50, 75, 2f);

        Assert.Equal(50, config.Width);
        Assert.Equal(75, config.Height);
        Assert.Equal(2f, config.CellSize);
    }

    #endregion

    #region Width Validation Tests

    [Fact]
    public void Validate_ZeroWidth_ReturnsError()
    {
        var config = new GridConfig { Width = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Width", error);
        Assert.Contains("positive", error);
    }

    [Fact]
    public void Validate_NegativeWidth_ReturnsError()
    {
        var config = new GridConfig { Width = -10 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Width", error);
        Assert.Contains("-10", error);
    }

    [Fact]
    public void Validate_PositiveWidth_ReturnsNull()
    {
        var config = new GridConfig { Width = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Height Validation Tests

    [Fact]
    public void Validate_ZeroHeight_ReturnsError()
    {
        var config = new GridConfig { Height = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Height", error);
        Assert.Contains("positive", error);
    }

    [Fact]
    public void Validate_NegativeHeight_ReturnsError()
    {
        var config = new GridConfig { Height = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Height", error);
        Assert.Contains("-5", error);
    }

    [Fact]
    public void Validate_PositiveHeight_ReturnsNull()
    {
        var config = new GridConfig { Height = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region CellSize Validation Tests

    [Fact]
    public void Validate_ZeroCellSize_ReturnsError()
    {
        var config = new GridConfig { CellSize = 0f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
        Assert.Contains("positive", error);
    }

    [Fact]
    public void Validate_NegativeCellSize_ReturnsError()
    {
        var config = new GridConfig { CellSize = -1f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void Validate_VerySmallCellSize_ReturnsNull()
    {
        var config = new GridConfig { CellSize = 0.001f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_LargeCellSize_ReturnsNull()
    {
        var config = new GridConfig { CellSize = 1000f };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region MaxIterations Validation Tests

    [Fact]
    public void Validate_NegativeMaxIterations_ReturnsError()
    {
        var config = new GridConfig { MaxIterations = -1 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxIterations", error);
    }

    [Fact]
    public void Validate_ZeroMaxIterations_ReturnsNull()
    {
        var config = new GridConfig { MaxIterations = 0 };

        var error = config.Validate();

        Assert.Null(error);  // 0 means no limit
    }

    [Fact]
    public void Validate_PositiveMaxIterations_ReturnsNull()
    {
        var config = new GridConfig { MaxIterations = 50000 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region MaxPendingRequests Validation Tests

    [Fact]
    public void Validate_ZeroMaxPendingRequests_ReturnsError()
    {
        var config = new GridConfig { MaxPendingRequests = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxPendingRequests", error);
        Assert.Contains("positive", error);
    }

    [Fact]
    public void Validate_NegativeMaxPendingRequests_ReturnsError()
    {
        var config = new GridConfig { MaxPendingRequests = -10 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxPendingRequests", error);
    }

    [Fact]
    public void Validate_PositiveMaxPendingRequests_ReturnsNull()
    {
        var config = new GridConfig { MaxPendingRequests = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region RequestsPerUpdate Validation Tests

    [Fact]
    public void Validate_ZeroRequestsPerUpdate_ReturnsError()
    {
        var config = new GridConfig { RequestsPerUpdate = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("RequestsPerUpdate", error);
        Assert.Contains("positive", error);
    }

    [Fact]
    public void Validate_NegativeRequestsPerUpdate_ReturnsError()
    {
        var config = new GridConfig { RequestsPerUpdate = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("RequestsPerUpdate", error);
    }

    [Fact]
    public void Validate_PositiveRequestsPerUpdate_ReturnsNull()
    {
        var config = new GridConfig { RequestsPerUpdate = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Path Caching Validation Tests

    [Fact]
    public void Validate_CachingEnabledWithZeroMaxCached_ReturnsError()
    {
        var config = new GridConfig
        {
            EnablePathCaching = true,
            MaxCachedPaths = 0
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxCachedPaths", error);
    }

    [Fact]
    public void Validate_CachingDisabledWithZeroMaxCached_ReturnsNull()
    {
        var config = new GridConfig
        {
            EnablePathCaching = false,
            MaxCachedPaths = 0
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_CachingEnabledWithPositiveMaxCached_ReturnsNull()
    {
        var config = new GridConfig
        {
            EnablePathCaching = true,
            MaxCachedPaths = 50
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Heuristic Tests

    [Fact]
    public void Heuristic_AllValues_AreValid()
    {
        foreach (GridHeuristic heuristic in Enum.GetValues<GridHeuristic>())
        {
            var config = new GridConfig { Heuristic = heuristic };

            var error = config.Validate();

            Assert.Null(error);
        }
    }

    #endregion

    #region AllowDiagonal Tests

    [Fact]
    public void AllowDiagonal_True_IsValid()
    {
        var config = new GridConfig { AllowDiagonal = true };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void AllowDiagonal_False_IsValid()
    {
        var config = new GridConfig { AllowDiagonal = false };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region WorldOrigin Tests

    [Fact]
    public void WorldOrigin_AnyValue_IsValid()
    {
        var config = new GridConfig
        {
            WorldOrigin = new Vector3(-1000, 500, 2000)
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_VeryLargeGrid_ReturnsNull()
    {
        var config = new GridConfig
        {
            Width = 10000,
            Height = 10000,
            CellSize = 0.1f
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_MinimalValidConfig_ReturnsNull()
    {
        var config = new GridConfig
        {
            Width = 1,
            Height = 1,
            CellSize = 0.0001f,
            MaxPendingRequests = 1,
            RequestsPerUpdate = 1
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion
}
