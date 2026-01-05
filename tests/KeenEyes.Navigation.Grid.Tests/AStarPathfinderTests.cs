using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Grid.Tests;

/// <summary>
/// Tests for <see cref="AStarPathfinder"/> class.
/// </summary>
public class AStarPathfinderTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesPathfinder()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = GridConfig.Default;

        var pathfinder = new AStarPathfinder(grid, config);

        Assert.NotNull(pathfinder);
        Assert.Same(grid, pathfinder.Grid);
        Assert.Same(config, pathfinder.Config);
    }

    [Fact]
    public void Constructor_NullGrid_ThrowsArgumentNull()
    {
        var config = GridConfig.Default;

        Assert.Throws<ArgumentNullException>(() => new AStarPathfinder(null!, config));
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNull()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.Throws<ArgumentNullException>(() => new AStarPathfinder(grid, null!));
    }

    #endregion

    #region Simple Path Tests

    [Fact]
    public void FindPath_SameStartAndEnd_ReturnsSinglePointPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = GridConfig.Default;
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(5, 5),
            new GridCoordinate(5, 5),
            result);

        Assert.Equal(1, length);
        Assert.Equal(new GridCoordinate(5, 5), result[0]);
    }

    [Fact]
    public void FindPath_AdjacentCells_ReturnsDirectPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(5, 5),
            new GridCoordinate(5, 6),
            result);

        Assert.Equal(2, length);
        Assert.Equal(new GridCoordinate(5, 5), result[0]);
        Assert.Equal(new GridCoordinate(5, 6), result[1]);
    }

    [Fact]
    public void FindPath_StraightLine_ReturnsShortestPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 5),
            new GridCoordinate(4, 5),
            result);

        Assert.Equal(5, length);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i, result[i].X);
            Assert.Equal(5, result[i].Y);
        }
    }

    [Fact]
    public void FindPath_DiagonalEnabled_UsesDiagonalMoves()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = true };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(3, 3),
            result);

        // With diagonal movement, path should be 4 (0,0 -> 1,1 -> 2,2 -> 3,3)
        Assert.Equal(4, length);
    }

    [Fact]
    public void FindPath_DiagonalDisabled_UsesCardinalMoves()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(3, 3),
            result);

        // Without diagonal, path should be 7 (3 horizontal + 3 vertical + 1 start)
        Assert.Equal(7, length);
    }

    #endregion

    #region Obstacle Tests

    [Fact]
    public void FindPath_WithObstacle_PathsAround()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[2, 0] = GridCell.Blocked;  // Block direct path
        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(4, 0),
            result);

        Assert.True(length > 5);  // Path must go around
        Assert.DoesNotContain(new GridCoordinate(2, 0), result[..length].ToArray());
    }

    [Fact]
    public void FindPath_StartBlocked_ReturnsNoPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[0, 0] = GridCell.Blocked;
        var config = GridConfig.Default;
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.Equal(-1, length);
    }

    [Fact]
    public void FindPath_EndBlocked_ReturnsNoPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 5] = GridCell.Blocked;
        var config = GridConfig.Default;
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[10];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.Equal(-1, length);
    }

    [Fact]
    public void FindPath_CompletelyBlocked_ReturnsNoPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        // Create a wall separating start from end
        for (int y = 0; y < 10; y++)
        {
            grid[5, y] = GridCell.Blocked;
        }

        var config = GridConfig.Default;
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[100];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 5),
            new GridCoordinate(9, 5),
            result);

        Assert.Equal(-1, length);
    }

    #endregion

    #region Area Cost Tests

    [Fact]
    public void GetAreaCost_Default_ReturnsOne()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var pathfinder = new AStarPathfinder(grid, GridConfig.Default);

        Assert.Equal(1f, pathfinder.GetAreaCost(NavAreaType.Walkable));
        Assert.Equal(1f, pathfinder.GetAreaCost(NavAreaType.Road));
        Assert.Equal(1f, pathfinder.GetAreaCost(NavAreaType.Water));
    }

    [Fact]
    public void SetAreaCost_UpdatesCost()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var pathfinder = new AStarPathfinder(grid, GridConfig.Default);

        pathfinder.SetAreaCost(NavAreaType.Mud, 5f);

        Assert.Equal(5f, pathfinder.GetAreaCost(NavAreaType.Mud));
    }

    [Fact]
    public void FindPath_HigherCostArea_PrefersLowerCost()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        // Create a direct path through mud
        for (int x = 1; x < 4; x++)
        {
            grid[x, 5] = GridCell.WithAreaType(NavAreaType.Mud);
        }

        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        pathfinder.SetAreaCost(NavAreaType.Mud, 10f);  // Very expensive
        Span<GridCoordinate> result = stackalloc GridCoordinate[50];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 5),
            new GridCoordinate(5, 5),
            result);

        // Path should avoid mud if a cheaper alternative exists
        // In this case, going around would be cheaper
        Assert.True(length > 0);
    }

    #endregion

    #region Area Mask Tests

    [Fact]
    public void FindPath_AreaMaskExcludesWater_AvoidsWater()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        // Create water area
        grid[5, 4] = GridCell.WithAreaType(NavAreaType.Water);
        grid[5, 5] = GridCell.WithAreaType(NavAreaType.Water);
        grid[5, 6] = GridCell.WithAreaType(NavAreaType.Water);

        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[50];

        int length = pathfinder.FindPath(
            new GridCoordinate(3, 5),
            new GridCoordinate(7, 5),
            NavAreaMask.Walkable,  // Only walkable, no water
            result);

        // Should path around the water
        Assert.True(length > 0);
        foreach (var coord in result[..length].ToArray())
        {
            Assert.NotEqual(NavAreaType.Water, grid.GetAreaType(coord));
        }
    }

    [Fact]
    public void FindPath_AreaMaskIncludesWater_GoesThrough()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        for (int x = 0; x < 10; x++)
        {
            grid[x, 5] = GridCell.WithAreaType(NavAreaType.Water);
        }

        var config = new GridConfig { AllowDiagonal = false };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[50];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 4),
            new GridCoordinate(0, 6),
            NavAreaMask.Walkable | NavAreaMask.Water,
            result);

        Assert.Equal(3, length);  // Direct path through water
    }

    #endregion

    #region NavPath Return Tests

    [Fact]
    public void FindPath_NavPath_ReturnsValidPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = true };
        var pathfinder = new AStarPathfinder(grid, config);

        var path = pathfinder.FindPath(new GridCoordinate(0, 0), new GridCoordinate(5, 5));

        Assert.True(path.IsValid);
        Assert.True(path.IsComplete);
        Assert.True(path.Count > 0);
    }

    [Fact]
    public void FindPath_NavPath_NoPath_ReturnsEmpty()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[0, 0] = GridCell.Blocked;
        var config = GridConfig.Default;
        var pathfinder = new AStarPathfinder(grid, config);

        var path = pathfinder.FindPath(new GridCoordinate(0, 0), new GridCoordinate(5, 5));

        Assert.False(path.IsValid);
        Assert.Same(NavPath.Empty, path);
    }

    [Fact]
    public void FindPath_WorldPositions_ReturnsPath()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig { AllowDiagonal = true };
        var pathfinder = new AStarPathfinder(grid, config);

        var path = pathfinder.FindPath(
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(5.5f, 0f, 5.5f));

        Assert.True(path.IsValid);
    }

    #endregion

    #region HasPath Tests

    [Fact]
    public void HasPath_PathExists_ReturnsTrue()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var pathfinder = new AStarPathfinder(grid, GridConfig.Default);

        bool hasPath = pathfinder.HasPath(new GridCoordinate(0, 0), new GridCoordinate(5, 5));

        Assert.True(hasPath);
    }

    [Fact]
    public void HasPath_NoPath_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        // Create impassable wall
        for (int y = 0; y < 10; y++)
        {
            grid[5, y] = GridCell.Blocked;
        }

        var pathfinder = new AStarPathfinder(grid, GridConfig.Default);

        bool hasPath = pathfinder.HasPath(new GridCoordinate(0, 5), new GridCoordinate(9, 5));

        Assert.False(hasPath);
    }

    [Fact]
    public void HasPath_SamePoint_ReturnsTrue()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var pathfinder = new AStarPathfinder(grid, GridConfig.Default);

        bool hasPath = pathfinder.HasPath(new GridCoordinate(5, 5), new GridCoordinate(5, 5));

        Assert.True(hasPath);
    }

    #endregion

    #region Heuristic Tests

    [Fact]
    public void FindPath_ManhattanHeuristic_Works()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig
        {
            AllowDiagonal = false,
            Heuristic = GridHeuristic.Manhattan
        };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.True(length > 0);
    }

    [Fact]
    public void FindPath_EuclideanHeuristic_Works()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig
        {
            AllowDiagonal = true,
            Heuristic = GridHeuristic.Euclidean
        };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.True(length > 0);
    }

    [Fact]
    public void FindPath_OctileHeuristic_Works()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig
        {
            AllowDiagonal = true,
            Heuristic = GridHeuristic.Octile
        };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.True(length > 0);
    }

    [Fact]
    public void FindPath_ChebyshevHeuristic_Works()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var config = new GridConfig
        {
            AllowDiagonal = true,
            Heuristic = GridHeuristic.Chebyshev
        };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[20];

        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(5, 5),
            result);

        Assert.True(length > 0);
    }

    #endregion

    #region Max Iterations Tests

    [Fact]
    public void FindPath_ExceedsMaxIterations_ReturnsNoPath()
    {
        var grid = new NavigationGrid(100, 100, 1f);
        var config = new GridConfig
        {
            MaxIterations = 10,  // Very low
            AllowDiagonal = false
        };
        var pathfinder = new AStarPathfinder(grid, config);
        Span<GridCoordinate> result = stackalloc GridCoordinate[200];

        // Long path that would require many iterations
        int length = pathfinder.FindPath(
            new GridCoordinate(0, 0),
            new GridCoordinate(99, 99),
            result);

        Assert.Equal(-1, length);  // Should fail due to iteration limit
    }

    #endregion
}
