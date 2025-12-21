namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for SpatialStrategy enum and related functionality.
/// </summary>
public class SpatialStrategyTests
{
    #region Enum Values

    [Fact]
    public void SpatialStrategy_Grid_HasExpectedValue()
    {
        Assert.Equal(0, (int)SpatialStrategy.Grid);
    }

    [Fact]
    public void SpatialStrategy_Quadtree_HasExpectedValue()
    {
        Assert.Equal(1, (int)SpatialStrategy.Quadtree);
    }

    [Fact]
    public void SpatialStrategy_Octree_HasExpectedValue()
    {
        Assert.Equal(2, (int)SpatialStrategy.Octree);
    }

    [Fact]
    public void SpatialStrategy_AllValues_AreUnique()
    {
        var values = Enum.GetValues<SpatialStrategy>();
        var distinct = values.Distinct().ToArray();

        Assert.Equal(values.Length, distinct.Length);
    }

    [Fact]
    public void SpatialStrategy_HasThreeValues()
    {
        var values = Enum.GetValues<SpatialStrategy>();

        Assert.Equal(3, values.Length);
    }

    #endregion

    #region String Conversion

    [Fact]
    public void SpatialStrategy_Grid_ToString_ReturnsGridString()
    {
        var strategy = SpatialStrategy.Grid;

        Assert.Equal("Grid", strategy.ToString());
    }

    [Fact]
    public void SpatialStrategy_Quadtree_ToString_ReturnsQuadtreeString()
    {
        var strategy = SpatialStrategy.Quadtree;

        Assert.Equal("Quadtree", strategy.ToString());
    }

    [Fact]
    public void SpatialStrategy_Octree_ToString_ReturnsOctreeString()
    {
        var strategy = SpatialStrategy.Octree;

        Assert.Equal("Octree", strategy.ToString());
    }

    #endregion

    #region Parsing

    [Fact]
    public void SpatialStrategy_ParseGrid_ReturnsGridValue()
    {
        var success = Enum.TryParse<SpatialStrategy>("Grid", out var result);

        Assert.True(success);
        Assert.Equal(SpatialStrategy.Grid, result);
    }

    [Fact]
    public void SpatialStrategy_ParseQuadtree_ReturnsQuadtreeValue()
    {
        var success = Enum.TryParse<SpatialStrategy>("Quadtree", out var result);

        Assert.True(success);
        Assert.Equal(SpatialStrategy.Quadtree, result);
    }

    [Fact]
    public void SpatialStrategy_ParseOctree_ReturnsOctreeValue()
    {
        var success = Enum.TryParse<SpatialStrategy>("Octree", out var result);

        Assert.True(success);
        Assert.Equal(SpatialStrategy.Octree, result);
    }

    [Fact]
    public void SpatialStrategy_ParseInvalidValue_ReturnsFalse()
    {
        var success = Enum.TryParse<SpatialStrategy>("InvalidStrategy", out _);

        Assert.False(success);
    }

    #endregion

    #region Config Integration

    [Fact]
    public void SpatialConfig_DefaultStrategy_IsGrid()
    {
        var config = new SpatialConfig();

        Assert.Equal(SpatialStrategy.Grid, config.Strategy);
    }

    [Fact]
    public void SpatialConfig_CanSetGridStrategy()
    {
        var config = new SpatialConfig { Strategy = SpatialStrategy.Grid };

        Assert.Equal(SpatialStrategy.Grid, config.Strategy);
    }

    [Fact]
    public void SpatialConfig_CanSetQuadtreeStrategy()
    {
        var config = new SpatialConfig { Strategy = SpatialStrategy.Quadtree };

        Assert.Equal(SpatialStrategy.Quadtree, config.Strategy);
    }

    [Fact]
    public void SpatialConfig_CanSetOctreeStrategy()
    {
        var config = new SpatialConfig { Strategy = SpatialStrategy.Octree };

        Assert.Equal(SpatialStrategy.Octree, config.Strategy);
    }

    #endregion

    #region Validation with Different Strategies

    [Fact]
    public void SpatialConfig_GridStrategy_ValidatesGridConfig()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig { CellSize = -10f } // Invalid
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void SpatialConfig_QuadtreeStrategy_ValidatesQuadtreeConfig()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Quadtree,
            Quadtree = new QuadtreeConfig { MaxDepth = 0 } // Invalid
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth", error);
    }

    [Fact]
    public void SpatialConfig_OctreeStrategy_ValidatesOctreeConfig()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Octree,
            Octree = new OctreeConfig { MaxDepth = 0 } // Invalid
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("MaxDepth", error);
    }

    [Fact]
    public void SpatialConfig_GridStrategy_IgnoresQuadtreeAndOctreeConfigs()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig { CellSize = 100f },
            Quadtree = new QuadtreeConfig { MaxDepth = 0 }, // Invalid but should be ignored
            Octree = new OctreeConfig { MaxDepth = 0 } // Invalid but should be ignored
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_QuadtreeStrategy_IgnoresGridAndOctreeConfigs()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Quadtree,
            Quadtree = new QuadtreeConfig { MaxDepth = 8 },
            Grid = new GridConfig { CellSize = -10f }, // Invalid but should be ignored
            Octree = new OctreeConfig { MaxDepth = 0 } // Invalid but should be ignored
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_OctreeStrategy_IgnoresGridAndQuadtreeConfigs()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Octree,
            Octree = new OctreeConfig { MaxDepth = 6 },
            Grid = new GridConfig { CellSize = -10f }, // Invalid but should be ignored
            Quadtree = new QuadtreeConfig { MaxDepth = 0 } // Invalid but should be ignored
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    #endregion

    #region Comparison

    [Fact]
    public void SpatialStrategy_EqualityComparison_WorksCorrectly()
    {
        var strategy1 = SpatialStrategy.Grid;
        var strategy2 = SpatialStrategy.Grid;
        var strategy3 = SpatialStrategy.Quadtree;

        Assert.Equal(strategy1, strategy2);
        Assert.NotEqual(strategy1, strategy3);
    }

    [Fact]
    public void SpatialStrategy_CanBeUsedInSwitch()
    {
        var strategies = new[] { SpatialStrategy.Grid, SpatialStrategy.Quadtree, SpatialStrategy.Octree };
        var results = new List<string>();

        foreach (var strategy in strategies)
        {
            var result = strategy switch
            {
                SpatialStrategy.Grid => "Grid",
                SpatialStrategy.Quadtree => "Quadtree",
                SpatialStrategy.Octree => "Octree",
                _ => "Unknown"
            };
            results.Add(result);
        }

        Assert.Equal(new[] { "Grid", "Quadtree", "Octree" }, results);
    }

    #endregion

    #region Iteration

    [Fact]
    public void SpatialStrategy_CanIterateAllValues()
    {
        var values = Enum.GetValues<SpatialStrategy>();
        var list = new List<SpatialStrategy>();

        foreach (var value in values)
        {
            list.Add(value);
        }

        Assert.Contains(SpatialStrategy.Grid, list);
        Assert.Contains(SpatialStrategy.Quadtree, list);
        Assert.Contains(SpatialStrategy.Octree, list);
    }

    #endregion
}
