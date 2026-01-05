// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Navigation;

namespace KeenEyes.Editor.Tests.Navigation;

public class NavMeshBakeConfigTests
{
    #region Preset Tests

    [Fact]
    public void Humanoid_ReturnsDefaultSettings()
    {
        var config = NavMeshBakeConfig.Humanoid;

        Assert.Equal(0.5f, config.AgentRadius);
        Assert.Equal(2.0f, config.AgentHeight);
        Assert.Equal(45.0f, config.MaxSlopeAngle);
        Assert.Equal(0.4f, config.MaxClimbHeight);
    }

    [Fact]
    public void Small_ReturnsSmallAgentSettings()
    {
        var config = NavMeshBakeConfig.Small;

        Assert.Equal(0.25f, config.AgentRadius);
        Assert.Equal(1.0f, config.AgentHeight);
        Assert.Equal(0.2f, config.MaxClimbHeight);
    }

    [Fact]
    public void Large_ReturnsLargeAgentSettings()
    {
        var config = NavMeshBakeConfig.Large;

        Assert.Equal(1.0f, config.AgentRadius);
        Assert.Equal(3.0f, config.AgentHeight);
        Assert.Equal(0.8f, config.MaxClimbHeight);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void Validate_WithDefaultConfig_ReturnsNull()
    {
        var config = NavMeshBakeConfig.Humanoid;

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void Validate_WithZeroCellSize_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.CellSize = 0;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void Validate_WithNegativeCellHeight_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.CellHeight = -1;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellHeight", error);
    }

    [Fact]
    public void Validate_WithZeroAgentHeight_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.AgentHeight = 0;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("AgentHeight", error);
    }

    [Fact]
    public void Validate_WithZeroAgentRadius_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.AgentRadius = 0;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("AgentRadius", error);
    }

    [Fact]
    public void Validate_WithNegativeSlopeAngle_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.MaxSlopeAngle = -5;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxSlopeAngle", error);
    }

    [Fact]
    public void Validate_WithSlopeAngleOver90_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.MaxSlopeAngle = 95;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxSlopeAngle", error);
    }

    [Fact]
    public void Validate_WithNegativeClimbHeight_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.MaxClimbHeight = -0.5f;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxClimbHeight", error);
    }

    [Fact]
    public void Validate_WithMaxVertsPerPolyOutOfRange_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.MaxVertsPerPoly = 10;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxVertsPerPoly", error);
    }

    [Fact]
    public void Validate_WithSmallTileSize_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.UseTiles = true;
        config.TileSize = 8;

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("TileSize", error);
    }

    [Fact]
    public void Validate_WithEmptyAgentTypeName_ReturnsError()
    {
        var config = NavMeshBakeConfig.Humanoid;
        config.AgentTypeName = "";

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("AgentTypeName", error);
    }

    #endregion

    #region Clone Tests

    [Fact]
    public void Clone_CreatesIndependentCopy()
    {
        var original = NavMeshBakeConfig.Humanoid;
        original.AgentTypeName = "Original";

        var clone = original.Clone();
        clone.AgentTypeName = "Clone";

        Assert.Equal("Original", original.AgentTypeName);
        Assert.Equal("Clone", clone.AgentTypeName);
    }

    [Fact]
    public void Clone_CopiesAllProperties()
    {
        var original = new NavMeshBakeConfig
        {
            AgentRadius = 1.5f,
            AgentHeight = 3.0f,
            MaxSlopeAngle = 50.0f,
            MaxClimbHeight = 0.6f,
            CellSize = 0.4f,
            CellHeight = 0.3f,
            MinRegionArea = 10,
            MergeRegionArea = 25,
            MaxEdgeLength = 15,
            MaxSimplificationError = 1.5f,
            MaxVertsPerPoly = 5,
            BuildDetailMesh = false,
            UseTiles = false,
            TileSize = 32,
            StaticOnly = false,
            LayerMask = 123,
            AgentTypeName = "Custom"
        };

        var clone = original.Clone();

        Assert.Equal(original.AgentRadius, clone.AgentRadius);
        Assert.Equal(original.AgentHeight, clone.AgentHeight);
        Assert.Equal(original.MaxSlopeAngle, clone.MaxSlopeAngle);
        Assert.Equal(original.MaxClimbHeight, clone.MaxClimbHeight);
        Assert.Equal(original.CellSize, clone.CellSize);
        Assert.Equal(original.CellHeight, clone.CellHeight);
        Assert.Equal(original.MinRegionArea, clone.MinRegionArea);
        Assert.Equal(original.MergeRegionArea, clone.MergeRegionArea);
        Assert.Equal(original.MaxEdgeLength, clone.MaxEdgeLength);
        Assert.Equal(original.MaxSimplificationError, clone.MaxSimplificationError);
        Assert.Equal(original.MaxVertsPerPoly, clone.MaxVertsPerPoly);
        Assert.Equal(original.BuildDetailMesh, clone.BuildDetailMesh);
        Assert.Equal(original.UseTiles, clone.UseTiles);
        Assert.Equal(original.TileSize, clone.TileSize);
        Assert.Equal(original.StaticOnly, clone.StaticOnly);
        Assert.Equal(original.LayerMask, clone.LayerMask);
        Assert.Equal(original.AgentTypeName, clone.AgentTypeName);
    }

    #endregion

    #region ToRuntimeConfig Tests

    [Fact]
    public void ToRuntimeConfig_ConvertsSettings()
    {
        var config = new NavMeshBakeConfig
        {
            CellSize = 0.4f,
            CellHeight = 0.3f,
            AgentHeight = 2.5f,
            AgentRadius = 0.6f,
            MaxSlopeAngle = 50.0f,
            MaxClimbHeight = 0.5f,
            UseTiles = false,
            TileSize = 32
        };

        var runtime = config.ToRuntimeConfig();

        Assert.Equal(config.CellSize, runtime.CellSize);
        Assert.Equal(config.CellHeight, runtime.CellHeight);
        Assert.Equal(config.AgentHeight, runtime.AgentHeight);
        Assert.Equal(config.AgentRadius, runtime.AgentRadius);
        Assert.Equal(config.MaxSlopeAngle, runtime.MaxSlopeAngle);
        Assert.Equal(config.MaxClimbHeight, runtime.MaxClimbHeight);
        Assert.Equal(config.UseTiles, runtime.UseTiles);
        Assert.Equal(config.TileSize, runtime.TileSize);
    }

    #endregion
}
