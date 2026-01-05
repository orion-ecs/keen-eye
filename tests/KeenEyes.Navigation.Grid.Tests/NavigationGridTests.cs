using System.Numerics;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Grid.Tests;

/// <summary>
/// Tests for <see cref="NavigationGrid"/> class.
/// </summary>
public class NavigationGridTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_ValidParameters_CreatesGrid()
    {
        var grid = new NavigationGrid(10, 20, 1f);

        Assert.Equal(10, grid.Width);
        Assert.Equal(20, grid.Height);
        Assert.Equal(1f, grid.CellSize);
        Assert.Equal(Vector3.Zero, grid.WorldOrigin);
    }

    [Fact]
    public void Constructor_WithWorldOrigin_SetsOrigin()
    {
        var origin = new Vector3(100, 0, 50);

        var grid = new NavigationGrid(10, 10, 1f, origin);

        Assert.Equal(origin, grid.WorldOrigin);
    }

    [Fact]
    public void Constructor_ZeroWidth_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationGrid(0, 10, 1f));
    }

    [Fact]
    public void Constructor_NegativeWidth_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationGrid(-1, 10, 1f));
    }

    [Fact]
    public void Constructor_ZeroHeight_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationGrid(10, 0, 1f));
    }

    [Fact]
    public void Constructor_ZeroCellSize_ThrowsArgumentOutOfRange()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NavigationGrid(10, 10, 0f));
    }

    [Fact]
    public void CellCount_ReturnsWidthTimesHeight()
    {
        var grid = new NavigationGrid(10, 20, 1f);

        Assert.Equal(200, grid.CellCount);
    }

    #endregion

    #region Cell Access Tests

    [Fact]
    public void Indexer_ValidCoordinate_ReturnsWalkableCell()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        ref var cell = ref grid[new GridCoordinate(5, 5)];

        Assert.True(cell.IsWalkable);
        Assert.Equal(NavAreaType.Walkable, cell.AreaType);
    }

    [Fact]
    public void Indexer_OutOfBounds_ThrowsArgumentOutOfRange()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = grid[new GridCoordinate(10, 5)]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = grid[new GridCoordinate(5, 10)]; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { var _ = grid[new GridCoordinate(-1, 5)]; });
    }

    [Fact]
    public void Indexer_IntParameters_Works()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        ref var cell = ref grid[5, 5];

        Assert.True(cell.IsWalkable);
    }

    [Fact]
    public void TryGetCell_ValidCoordinate_ReturnsTrueWithCell()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        bool success = grid.TryGetCell(new GridCoordinate(5, 5), out var cell);

        Assert.True(success);
        Assert.True(cell.IsWalkable);
    }

    [Fact]
    public void TryGetCell_OutOfBounds_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        bool success = grid.TryGetCell(new GridCoordinate(15, 5), out _);

        Assert.False(success);
    }

    [Fact]
    public void TrySetCell_ValidCoordinate_SetsCell()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var blockedCell = GridCell.Blocked;

        bool success = grid.TrySetCell(new GridCoordinate(5, 5), blockedCell);

        Assert.True(success);
        Assert.False(grid[5, 5].IsWalkable);
    }

    [Fact]
    public void TrySetCell_OutOfBounds_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        bool success = grid.TrySetCell(new GridCoordinate(15, 5), GridCell.Blocked);

        Assert.False(success);
    }

    #endregion

    #region Bounds Tests

    [Fact]
    public void IsInBounds_ValidCoordinate_ReturnsTrue()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.True(grid.IsInBounds(new GridCoordinate(0, 0)));
        Assert.True(grid.IsInBounds(new GridCoordinate(5, 5)));
        Assert.True(grid.IsInBounds(new GridCoordinate(9, 9)));
    }

    [Fact]
    public void IsInBounds_OutOfBounds_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.False(grid.IsInBounds(new GridCoordinate(-1, 0)));
        Assert.False(grid.IsInBounds(new GridCoordinate(0, -1)));
        Assert.False(grid.IsInBounds(new GridCoordinate(10, 0)));
        Assert.False(grid.IsInBounds(new GridCoordinate(0, 10)));
    }

    [Fact]
    public void GetWorldBounds_ReturnsCorrectBounds()
    {
        var grid = new NavigationGrid(10, 20, 2f, new Vector3(5, 0, 10));

        var (min, max) = grid.GetWorldBounds();

        Assert.Equal(new Vector3(5, 0, 10), min);
        Assert.Equal(new Vector3(25, 0, 50), max);  // 5 + 10*2, 0, 10 + 20*2
    }

    #endregion

    #region Walkability Tests

    [Fact]
    public void IsWalkable_WalkableCell_ReturnsTrue()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.True(grid.IsWalkable(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void IsWalkable_BlockedCell_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 5] = GridCell.Blocked;

        Assert.False(grid.IsWalkable(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void IsWalkable_OutOfBounds_ReturnsFalse()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.False(grid.IsWalkable(new GridCoordinate(15, 5)));
    }

    [Fact]
    public void IsWalkable_WithAreaMask_FiltersCorrectly()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 5] = GridCell.WithAreaType(NavAreaType.Water);

        Assert.True(grid.IsWalkable(new GridCoordinate(5, 5), NavAreaMask.Water));
        Assert.False(grid.IsWalkable(new GridCoordinate(5, 5), NavAreaMask.Walkable));
        Assert.True(grid.IsWalkable(new GridCoordinate(5, 5), NavAreaMask.All));
    }

    [Fact]
    public void GetAreaType_ReturnsCorrectType()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 5] = GridCell.WithAreaType(NavAreaType.Road);

        Assert.Equal(NavAreaType.Road, grid.GetAreaType(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void GetAreaType_OutOfBounds_ReturnsNotWalkable()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.Equal(NavAreaType.NotWalkable, grid.GetAreaType(new GridCoordinate(15, 5)));
    }

    [Fact]
    public void GetCost_WalkableCell_ReturnsCost()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.Equal(1f, grid.GetCost(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void GetCost_BlockedCell_ReturnsMaxValue()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 5] = GridCell.Blocked;

        Assert.Equal(float.MaxValue, grid.GetCost(new GridCoordinate(5, 5)));
    }

    [Fact]
    public void GetCost_OutOfBounds_ReturnsMaxValue()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        Assert.Equal(float.MaxValue, grid.GetCost(new GridCoordinate(15, 5)));
    }

    #endregion

    #region World Position Conversion Tests

    [Fact]
    public void ToWorldPosition_ReturnsCorrectPosition()
    {
        var grid = new NavigationGrid(10, 10, 2f, new Vector3(10, 0, 20));

        var world = grid.ToWorldPosition(new GridCoordinate(5, 3));

        Assert.Equal(21f, world.X, 0.0001f);  // 10 + (5 + 0.5) * 2
        Assert.Equal(0f, world.Y, 0.0001f);
        Assert.Equal(27f, world.Z, 0.0001f);  // 20 + (3 + 0.5) * 2
    }

    [Fact]
    public void FromWorldPosition_ReturnsCorrectCoordinate()
    {
        var grid = new NavigationGrid(10, 10, 2f, new Vector3(10, 0, 20));

        var coord = grid.FromWorldPosition(new Vector3(21, 5, 27));

        Assert.Equal(5, coord.X);  // (21 - 10) / 2 = 5.5 -> floor = 5
        Assert.Equal(3, coord.Y);  // (27 - 20) / 2 = 3.5 -> floor = 3
    }

    #endregion

    #region Fill and Clear Tests

    [Fact]
    public void Fill_SetsAllCellsInRegion()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        grid.Fill(new GridCoordinate(2, 2), new GridCoordinate(4, 4), GridCell.Blocked);

        for (int x = 2; x <= 4; x++)
        {
            for (int y = 2; y <= 4; y++)
            {
                Assert.False(grid[x, y].IsWalkable);
            }
        }
    }

    [Fact]
    public void Fill_ReversedCoordinates_StillWorks()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        grid.Fill(new GridCoordinate(4, 4), new GridCoordinate(2, 2), GridCell.Blocked);

        for (int x = 2; x <= 4; x++)
        {
            for (int y = 2; y <= 4; y++)
            {
                Assert.False(grid[x, y].IsWalkable);
            }
        }
    }

    [Fact]
    public void Clear_SetsAllCellsToWalkable()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid.Fill(new GridCoordinate(0, 0), new GridCoordinate(9, 9), GridCell.Blocked);

        grid.Clear();

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Assert.True(grid[x, y].IsWalkable);
            }
        }
    }

    [Fact]
    public void Block_SetsAllCellsToBlocked()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        grid.Block();

        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                Assert.False(grid[x, y].IsWalkable);
            }
        }
    }

    #endregion

    #region Neighbor Tests

    [Fact]
    public void GetWalkableNeighbors_OpenGrid_ReturnsFourCardinal()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int count = grid.GetWalkableNeighbors(new GridCoordinate(5, 5), allowDiagonal: false, neighbors);

        Assert.Equal(4, count);
    }

    [Fact]
    public void GetWalkableNeighbors_OpenGrid_ReturnsEightWithDiagonal()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int count = grid.GetWalkableNeighbors(new GridCoordinate(5, 5), allowDiagonal: true, neighbors);

        Assert.Equal(8, count);
    }

    [Fact]
    public void GetWalkableNeighbors_Corner_ReturnsLimitedNeighbors()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int count = grid.GetWalkableNeighbors(new GridCoordinate(0, 0), allowDiagonal: false, neighbors);

        Assert.Equal(2, count);  // Only right and down
    }

    [Fact]
    public void GetWalkableNeighbors_BlockedNeighbor_ExcludesBlocked()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 4] = GridCell.Blocked;  // Block the cell above
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int count = grid.GetWalkableNeighbors(new GridCoordinate(5, 5), allowDiagonal: false, neighbors);

        Assert.Equal(3, count);
        Assert.DoesNotContain(new GridCoordinate(5, 4), neighbors[..count].ToArray());
    }

    [Fact]
    public void GetWalkableNeighbors_DiagonalCornerCutting_Prevents()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[4, 5] = GridCell.Blocked;  // Block left
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int count = grid.GetWalkableNeighbors(new GridCoordinate(5, 5), allowDiagonal: true, neighbors);

        // Should not include (4,4) or (4,6) because (4,5) is blocked
        Assert.DoesNotContain(new GridCoordinate(4, 4), neighbors[..count].ToArray());
        Assert.DoesNotContain(new GridCoordinate(4, 6), neighbors[..count].ToArray());
    }

    [Fact]
    public void GetWalkableNeighbors_WithAreaMask_FiltersCorrectly()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        grid[5, 4] = GridCell.WithAreaType(NavAreaType.Water);  // Water above
        Span<GridCoordinate> neighbors = stackalloc GridCoordinate[8];

        int countWalkableOnly = grid.GetWalkableNeighbors(
            new GridCoordinate(5, 5), false, NavAreaMask.Walkable, neighbors);
        int countWithWater = grid.GetWalkableNeighbors(
            new GridCoordinate(5, 5), false, NavAreaMask.Walkable | NavAreaMask.Water, neighbors);

        Assert.Equal(3, countWalkableOnly);  // Excludes water
        Assert.Equal(4, countWithWater);     // Includes water
    }

    #endregion

    #region Span Access Tests

    [Fact]
    public void GetCellsSpan_ReturnsAllCells()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        var span = grid.GetCellsSpan();

        Assert.Equal(100, span.Length);
    }

    [Fact]
    public void GetCellsReadOnlySpan_ReturnsAllCells()
    {
        var grid = new NavigationGrid(10, 10, 1f);

        var span = grid.GetCellsReadOnlySpan();

        Assert.Equal(100, span.Length);
    }

    [Fact]
    public void GetCellsSpan_ModificationsAffectGrid()
    {
        var grid = new NavigationGrid(10, 10, 1f);
        var span = grid.GetCellsSpan();

        span[0].IsWalkable = false;

        Assert.False(grid[0, 0].IsWalkable);
    }

    #endregion
}
