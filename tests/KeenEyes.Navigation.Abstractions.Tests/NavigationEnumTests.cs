using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Abstractions.Tests;

/// <summary>
/// Tests for navigation-related enums.
/// </summary>
public class NavigationEnumTests
{
    #region NavigationStrategy Tests

    [Theory]
    [InlineData(NavigationStrategy.NavMesh)]
    [InlineData(NavigationStrategy.Grid)]
    [InlineData(NavigationStrategy.Hierarchical)]
    [InlineData(NavigationStrategy.Custom)]
    public void NavigationStrategy_HasAllExpectedValues(NavigationStrategy strategy)
    {
        Assert.True(Enum.IsDefined(strategy));
    }

    [Fact]
    public void NavigationStrategy_HasExpectedCount()
    {
        var values = Enum.GetValues<NavigationStrategy>();
        Assert.Equal(4, values.Length);
    }

    #endregion

    #region PathRequestStatus Tests

    [Theory]
    [InlineData(PathRequestStatus.Pending)]
    [InlineData(PathRequestStatus.Computing)]
    [InlineData(PathRequestStatus.Completed)]
    [InlineData(PathRequestStatus.Failed)]
    [InlineData(PathRequestStatus.Cancelled)]
    public void PathRequestStatus_HasAllExpectedValues(PathRequestStatus status)
    {
        Assert.True(Enum.IsDefined(status));
    }

    [Fact]
    public void PathRequestStatus_HasExpectedCount()
    {
        var values = Enum.GetValues<PathRequestStatus>();
        Assert.Equal(5, values.Length);
    }

    #endregion

    #region ObstacleShape Tests

    [Theory]
    [InlineData(ObstacleShape.Box)]
    [InlineData(ObstacleShape.Cylinder)]
    public void ObstacleShape_HasAllExpectedValues(ObstacleShape shape)
    {
        Assert.True(Enum.IsDefined(shape));
    }

    [Fact]
    public void ObstacleShape_HasExpectedCount()
    {
        var values = Enum.GetValues<ObstacleShape>();
        Assert.Equal(2, values.Length);
    }

    #endregion

    #region NavAreaType Tests

    [Theory]
    [InlineData(NavAreaType.Walkable, 0)]
    [InlineData(NavAreaType.Water, 1)]
    [InlineData(NavAreaType.Road, 2)]
    [InlineData(NavAreaType.Grass, 3)]
    [InlineData(NavAreaType.Door, 4)]
    [InlineData(NavAreaType.NotWalkable, 31)]
    public void NavAreaType_HasCorrectValues(NavAreaType areaType, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)areaType);
    }

    #endregion

    #region NavAreaMask Tests

    [Fact]
    public void NavAreaMask_None_IsZero()
    {
        Assert.Equal(0u, (uint)NavAreaMask.None);
    }

    [Fact]
    public void NavAreaMask_Walkable_MatchesAreaType()
    {
        var expected = 1u << (int)NavAreaType.Walkable;
        Assert.Equal((uint)NavAreaMask.Walkable, expected);
    }

    [Fact]
    public void NavAreaMask_Water_MatchesAreaType()
    {
        var expected = 1u << (int)NavAreaType.Water;
        Assert.Equal((uint)NavAreaMask.Water, expected);
    }

    [Fact]
    public void NavAreaMask_Ground_CombinesExpectedTypes()
    {
        var expected = NavAreaMask.Walkable | NavAreaMask.Road | NavAreaMask.Grass | NavAreaMask.Sand;
        Assert.Equal(NavAreaMask.Ground, expected);
    }

    [Fact]
    public void NavAreaMask_CanCombineWithBitwiseOr()
    {
        var combined = NavAreaMask.Walkable | NavAreaMask.Water;

        Assert.True((combined & NavAreaMask.Walkable) != 0);
        Assert.True((combined & NavAreaMask.Water) != 0);
        Assert.False((combined & NavAreaMask.Road) != 0);
    }

    [Fact]
    public void NavAreaMask_All_ExcludesNotWalkable()
    {
        var notWalkableBit = 1u << (int)NavAreaType.NotWalkable;
        var allValue = (uint)NavAreaMask.All;

        Assert.Equal(0u, allValue & notWalkableBit);
    }

    [Fact]
    public void NavAreaMask_All_IncludesWalkable()
    {
        var walkableBit = 1u << (int)NavAreaType.Walkable;
        var allValue = (uint)NavAreaMask.All;

        Assert.NotEqual(0u, allValue & walkableBit);
    }

    #endregion
}
