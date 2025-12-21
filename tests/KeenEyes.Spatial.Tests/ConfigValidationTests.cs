using System.Numerics;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Comprehensive tests for configuration validation across all spatial partitioning strategies.
/// </summary>
public class ConfigValidationTests
{
    #region GridConfig Validation Tests

    [Fact]
    public void GridConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            DeterministicMode = false
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void GridConfig_Validate_NegativeCellSize_ReturnsError()
    {
        var config = new GridConfig { CellSize = -10f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize must be positive", error);
        Assert.Contains("-10", error);
    }

    [Fact]
    public void GridConfig_Validate_ZeroCellSize_ReturnsError()
    {
        var config = new GridConfig { CellSize = 0f };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize must be positive", error);
        Assert.Contains("0", error);
    }

    [Fact]
    public void GridConfig_Validate_VerySmallPositiveCellSize_ReturnsNull()
    {
        var config = new GridConfig { CellSize = 0.0001f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void GridConfig_Validate_WorldMinXGreaterThanMaxX_ReturnsError()
    {
        var config = new GridConfig
        {
            WorldMin = new Vector3(1000, 0, 0),
            WorldMax = new Vector3(-1000, 100, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax", error);
    }

    [Fact]
    public void GridConfig_Validate_WorldMinYGreaterThanMaxY_ReturnsError()
    {
        var config = new GridConfig
        {
            WorldMin = new Vector3(0, 1000, 0),
            WorldMax = new Vector3(100, -1000, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax", error);
    }

    [Fact]
    public void GridConfig_Validate_WorldMinZGreaterThanMaxZ_ReturnsError()
    {
        var config = new GridConfig
        {
            WorldMin = new Vector3(0, 0, 1000),
            WorldMax = new Vector3(100, 100, -1000)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax", error);
    }

    [Fact]
    public void GridConfig_Validate_WorldMinEqualsMaxX_ReturnsError()
    {
        var config = new GridConfig
        {
            WorldMin = new Vector3(100, 0, 0),
            WorldMax = new Vector3(100, 100, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax", error);
    }

    [Fact]
    public void GridConfig_DefaultValues_AreValid()
    {
        var config = new GridConfig();

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void GridConfig_DeterministicMode_ValidWithTrue()
    {
        var config = new GridConfig { DeterministicMode = true };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region QuadtreeConfig Validation Tests

    [Fact]
    public void QuadtreeConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            UseLooseBounds = false,
            DeterministicMode = false,
            UseNodePooling = true
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxDepthZero_ReturnsError()
    {
        var config = new QuadtreeConfig { MaxDepth = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 16", error);
        Assert.Contains("0", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxDepthNegative_ReturnsError()
    {
        var config = new QuadtreeConfig { MaxDepth = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 16", error);
        Assert.Contains("-5", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxDepthTooHigh_ReturnsError()
    {
        var config = new QuadtreeConfig { MaxDepth = 17 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 16", error);
        Assert.Contains("17", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxDepthOne_ReturnsNull()
    {
        var config = new QuadtreeConfig { MaxDepth = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxDepthSixteen_ReturnsNull()
    {
        var config = new QuadtreeConfig { MaxDepth = 16 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxEntitiesPerNodeZero_ReturnsError()
    {
        var config = new QuadtreeConfig { MaxEntitiesPerNode = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxEntitiesPerNode must be positive", error);
        Assert.Contains("0", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxEntitiesPerNodeNegative_ReturnsError()
    {
        var config = new QuadtreeConfig { MaxEntitiesPerNode = -10 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxEntitiesPerNode must be positive", error);
        Assert.Contains("-10", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_WorldMinXGreaterThanMaxX_ReturnsError()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(1000, 0, 0),
            WorldMax = new Vector3(-1000, 0, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax in X and Z dimensions", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_WorldMinZGreaterThanMaxZ_ReturnsError()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(0, 0, 1000),
            WorldMax = new Vector3(100, 0, -1000)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax in X and Z dimensions", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_DifferentYValues_ReturnsNull()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-100, 1000, -100),
            WorldMax = new Vector3(100, -1000, 100)
        };

        var error = config.Validate();

        // Y dimension is not checked for quadtree
        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_LoosenessFactor_TooSmall_ReturnsError()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 0.5f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("LoosenessFactor must be between 1.0 and 10.0", error);
        Assert.Contains("0.5", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_LoosenessFactor_TooLarge_ReturnsError()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 15f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("LoosenessFactor must be between 1.0 and 10.0", error);
        Assert.Contains("15", error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_LoosenessFactor_ExactlyOne_ReturnsNull()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 1.0f
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_LoosenessFactor_ExactlyTen_ReturnsNull()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 10.0f
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_LoosenessFactor_InvalidButLooseBoundsDisabled_ReturnsNull()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = false,
            LoosenessFactor = 0.1f // Invalid, but UseLooseBounds is false
        };

        var error = config.Validate();

        // Should be valid because UseLooseBounds is false
        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_DefaultValues_AreValid()
    {
        var config = new QuadtreeConfig();

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region OctreeConfig Validation Tests

    [Fact]
    public void OctreeConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new OctreeConfig
        {
            MaxDepth = 6,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            UseLooseBounds = false,
            DeterministicMode = false,
            UseNodePooling = true
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxDepthZero_ReturnsError()
    {
        var config = new OctreeConfig { MaxDepth = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 12", error);
        Assert.Contains("0", error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxDepthNegative_ReturnsError()
    {
        var config = new OctreeConfig { MaxDepth = -3 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 12", error);
        Assert.Contains("-3", error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxDepthTooHigh_ReturnsError()
    {
        var config = new OctreeConfig { MaxDepth = 13 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth must be between 1 and 12", error);
        Assert.Contains("13", error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxDepthOne_ReturnsNull()
    {
        var config = new OctreeConfig { MaxDepth = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxDepthTwelve_ReturnsNull()
    {
        var config = new OctreeConfig { MaxDepth = 12 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxEntitiesPerNodeZero_ReturnsError()
    {
        var config = new OctreeConfig { MaxEntitiesPerNode = 0 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxEntitiesPerNode must be positive", error);
        Assert.Contains("0", error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxEntitiesPerNodeNegative_ReturnsError()
    {
        var config = new OctreeConfig { MaxEntitiesPerNode = -5 };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxEntitiesPerNode must be positive", error);
        Assert.Contains("-5", error);
    }

    [Fact]
    public void OctreeConfig_Validate_WorldMinXGreaterThanMaxX_ReturnsError()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(1000, 0, 0),
            WorldMax = new Vector3(-1000, 100, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax in all dimensions", error);
    }

    [Fact]
    public void OctreeConfig_Validate_WorldMinYGreaterThanMaxY_ReturnsError()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(0, 1000, 0),
            WorldMax = new Vector3(100, -1000, 100)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax in all dimensions", error);
    }

    [Fact]
    public void OctreeConfig_Validate_WorldMinZGreaterThanMaxZ_ReturnsError()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(0, 0, 1000),
            WorldMax = new Vector3(100, 100, -1000)
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin must be less than WorldMax in all dimensions", error);
    }

    [Fact]
    public void OctreeConfig_Validate_LoosenessFactor_TooSmall_ReturnsError()
    {
        var config = new OctreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 0.9f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("LoosenessFactor must be between 1.0 and 10.0", error);
        Assert.Contains("0.9", error);
    }

    [Fact]
    public void OctreeConfig_Validate_LoosenessFactor_TooLarge_ReturnsError()
    {
        var config = new OctreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 10.1f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("LoosenessFactor must be between 1.0 and 10.0", error);
        Assert.Contains("10.1", error);
    }

    [Fact]
    public void OctreeConfig_Validate_LoosenessFactor_ExactlyOne_ReturnsNull()
    {
        var config = new OctreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 1.0f
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_LoosenessFactor_ExactlyTen_ReturnsNull()
    {
        var config = new OctreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 10.0f
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_LoosenessFactor_InvalidButLooseBoundsDisabled_ReturnsNull()
    {
        var config = new OctreeConfig
        {
            UseLooseBounds = false,
            LoosenessFactor = 0.5f // Invalid, but UseLooseBounds is false
        };

        var error = config.Validate();

        // Should be valid because UseLooseBounds is false
        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_DefaultValues_AreValid()
    {
        var config = new OctreeConfig();

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region SpatialConfig Validation Tests

    [Fact]
    public void SpatialConfig_Validate_GridStrategy_ValidConfig_ReturnsNull()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig { CellSize = 100f }
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_Validate_GridStrategy_InvalidGridConfig_ReturnsError()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig { CellSize = -10f }
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void SpatialConfig_Validate_QuadtreeStrategy_ValidConfig_ReturnsNull()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Quadtree,
            Quadtree = new QuadtreeConfig { MaxDepth = 8 }
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_Validate_QuadtreeStrategy_InvalidConfig_ReturnsError()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Quadtree,
            Quadtree = new QuadtreeConfig { MaxDepth = 0 }
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth", error);
    }

    [Fact]
    public void SpatialConfig_Validate_OctreeStrategy_ValidConfig_ReturnsNull()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Octree,
            Octree = new OctreeConfig { MaxDepth = 6 }
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_Validate_OctreeStrategy_InvalidConfig_ReturnsError()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Octree,
            Octree = new OctreeConfig { MaxDepth = 0 }
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth", error);
    }

    [Fact]
    public void SpatialConfig_Validate_UnknownStrategy_ReturnsError()
    {
        var config = new SpatialConfig
        {
            Strategy = (SpatialStrategy)999
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Unknown spatial strategy", error);
        Assert.Contains("999", error);
    }

    [Fact]
    public void SpatialConfig_DefaultValues_AreValid()
    {
        var config = new SpatialConfig();

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_Validate_AllStrategies_WithDefaultConfigs_AreValid()
    {
        var strategies = new[] { SpatialStrategy.Grid, SpatialStrategy.Quadtree, SpatialStrategy.Octree };

        foreach (var strategy in strategies)
        {
            var config = new SpatialConfig { Strategy = strategy };

            var error = config.Validate();

            Assert.Null(error);
        }
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void GridConfig_Validate_VeryLargeCellSize_ReturnsNull()
    {
        var config = new GridConfig { CellSize = 1000000f };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void GridConfig_Validate_VeryLargeWorldBounds_ReturnsNull()
    {
        var config = new GridConfig
        {
            WorldMin = new Vector3(-1000000, -1000000, -1000000),
            WorldMax = new Vector3(1000000, 1000000, 1000000)
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_MaxEntitiesOne_ReturnsNull()
    {
        var config = new QuadtreeConfig { MaxEntitiesPerNode = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_MaxEntitiesOne_ReturnsNull()
    {
        var config = new OctreeConfig { MaxEntitiesPerNode = 1 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void QuadtreeConfig_Validate_VeryLargeMaxEntities_ReturnsNull()
    {
        var config = new QuadtreeConfig { MaxEntitiesPerNode = 10000 };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void OctreeConfig_Validate_VeryLargeMaxEntities_ReturnsNull()
    {
        var config = new OctreeConfig { MaxEntitiesPerNode = 10000 };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion
}
