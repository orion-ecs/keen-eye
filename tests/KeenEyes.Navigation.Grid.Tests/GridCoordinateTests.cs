using System.Numerics;
using KeenEyes.Navigation.Grid;

namespace KeenEyes.Navigation.Grid.Tests;

/// <summary>
/// Tests for <see cref="GridCoordinate"/> struct.
/// </summary>
public class GridCoordinateTests
{
    #region Construction Tests

    [Fact]
    public void Constructor_CreatesCoordinate()
    {
        var coord = new GridCoordinate(5, 10);

        Assert.Equal(5, coord.X);
        Assert.Equal(10, coord.Y);
    }

    [Fact]
    public void Origin_ReturnsZeroCoordinate()
    {
        var origin = GridCoordinate.Origin;

        Assert.Equal(0, origin.X);
        Assert.Equal(0, origin.Y);
    }

    [Fact]
    public void DirectionalConstants_HaveCorrectValues()
    {
        Assert.Equal(new GridCoordinate(0, -1), GridCoordinate.Up);
        Assert.Equal(new GridCoordinate(0, 1), GridCoordinate.Down);
        Assert.Equal(new GridCoordinate(-1, 0), GridCoordinate.Left);
        Assert.Equal(new GridCoordinate(1, 0), GridCoordinate.Right);
    }

    #endregion

    #region Direction Collections Tests

    [Fact]
    public void CardinalDirections_HasFourElements()
    {
        var cardinals = GridCoordinate.CardinalDirections;

        Assert.Equal(4, cardinals.Length);
    }

    [Fact]
    public void DiagonalDirections_HasFourElements()
    {
        var diagonals = GridCoordinate.DiagonalDirections;

        Assert.Equal(4, diagonals.Length);
    }

    [Fact]
    public void AllDirections_HasEightElements()
    {
        var all = GridCoordinate.AllDirections;

        Assert.Equal(8, all.Length);
    }

    [Fact]
    public void CardinalDirections_ContainsCorrectDirections()
    {
        var cardinals = GridCoordinate.CardinalDirections.ToArray();

        Assert.Contains(new GridCoordinate(0, -1), cardinals); // Up
        Assert.Contains(new GridCoordinate(0, 1), cardinals);  // Down
        Assert.Contains(new GridCoordinate(-1, 0), cardinals); // Left
        Assert.Contains(new GridCoordinate(1, 0), cardinals);  // Right
    }

    [Fact]
    public void DiagonalDirections_ContainsCorrectDirections()
    {
        var diagonals = GridCoordinate.DiagonalDirections.ToArray();

        Assert.Contains(new GridCoordinate(-1, -1), diagonals); // Up-Left
        Assert.Contains(new GridCoordinate(1, -1), diagonals);  // Up-Right
        Assert.Contains(new GridCoordinate(-1, 1), diagonals);  // Down-Left
        Assert.Contains(new GridCoordinate(1, 1), diagonals);   // Down-Right
    }

    #endregion

    #region Distance Calculation Tests

    [Fact]
    public void ManhattanDistance_SamePoint_ReturnsZero()
    {
        var a = new GridCoordinate(5, 5);
        var b = new GridCoordinate(5, 5);

        Assert.Equal(0, a.ManhattanDistance(b));
    }

    [Fact]
    public void ManhattanDistance_HorizontalOnly_ReturnsCorrectDistance()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(5, 0);

        Assert.Equal(5, a.ManhattanDistance(b));
    }

    [Fact]
    public void ManhattanDistance_VerticalOnly_ReturnsCorrectDistance()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(0, 7);

        Assert.Equal(7, a.ManhattanDistance(b));
    }

    [Fact]
    public void ManhattanDistance_Diagonal_ReturnsSumOfDistances()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(3, 4);

        Assert.Equal(7, a.ManhattanDistance(b));
    }

    [Fact]
    public void ChebyshevDistance_SamePoint_ReturnsZero()
    {
        var a = new GridCoordinate(5, 5);
        var b = new GridCoordinate(5, 5);

        Assert.Equal(0, a.ChebyshevDistance(b));
    }

    [Fact]
    public void ChebyshevDistance_Diagonal_ReturnsMaxDimension()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(3, 5);

        Assert.Equal(5, a.ChebyshevDistance(b));
    }

    [Fact]
    public void OctileDistance_CardinalMove_ReturnsOne()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(1, 0);

        Assert.Equal(1f, a.OctileDistance(b), 0.0001f);
    }

    [Fact]
    public void OctileDistance_DiagonalMove_ReturnsSqrt2()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(1, 1);

        Assert.Equal(1.41421356f, a.OctileDistance(b), 0.0001f);
    }

    [Fact]
    public void EuclideanDistance_SamePoint_ReturnsZero()
    {
        var a = new GridCoordinate(5, 5);
        var b = new GridCoordinate(5, 5);

        Assert.Equal(0f, a.EuclideanDistance(b), 0.0001f);
    }

    [Fact]
    public void EuclideanDistance_3_4_5Triangle_Returns5()
    {
        var a = new GridCoordinate(0, 0);
        var b = new GridCoordinate(3, 4);

        Assert.Equal(5f, a.EuclideanDistance(b), 0.0001f);
    }

    #endregion

    #region Adjacency Tests

    [Fact]
    public void IsAdjacentTo_CardinalNeighbor_ReturnsTrue()
    {
        var center = new GridCoordinate(5, 5);

        Assert.True(center.IsAdjacentTo(new GridCoordinate(5, 4)));  // Up
        Assert.True(center.IsAdjacentTo(new GridCoordinate(5, 6)));  // Down
        Assert.True(center.IsAdjacentTo(new GridCoordinate(4, 5)));  // Left
        Assert.True(center.IsAdjacentTo(new GridCoordinate(6, 5)));  // Right
    }

    [Fact]
    public void IsAdjacentTo_DiagonalNeighbor_ReturnsTrue()
    {
        var center = new GridCoordinate(5, 5);

        Assert.True(center.IsAdjacentTo(new GridCoordinate(4, 4)));  // Up-Left
        Assert.True(center.IsAdjacentTo(new GridCoordinate(6, 4)));  // Up-Right
        Assert.True(center.IsAdjacentTo(new GridCoordinate(4, 6)));  // Down-Left
        Assert.True(center.IsAdjacentTo(new GridCoordinate(6, 6)));  // Down-Right
    }

    [Fact]
    public void IsAdjacentTo_SameCoordinate_ReturnsFalse()
    {
        var coord = new GridCoordinate(5, 5);

        Assert.False(coord.IsAdjacentTo(coord));
    }

    [Fact]
    public void IsAdjacentTo_TwoAway_ReturnsFalse()
    {
        var center = new GridCoordinate(5, 5);

        Assert.False(center.IsAdjacentTo(new GridCoordinate(5, 3)));  // Two up
        Assert.False(center.IsAdjacentTo(new GridCoordinate(7, 5)));  // Two right
    }

    [Fact]
    public void IsCardinallyAdjacentTo_CardinalNeighbor_ReturnsTrue()
    {
        var center = new GridCoordinate(5, 5);

        Assert.True(center.IsCardinallyAdjacentTo(new GridCoordinate(5, 4)));  // Up
        Assert.True(center.IsCardinallyAdjacentTo(new GridCoordinate(5, 6)));  // Down
        Assert.True(center.IsCardinallyAdjacentTo(new GridCoordinate(4, 5)));  // Left
        Assert.True(center.IsCardinallyAdjacentTo(new GridCoordinate(6, 5)));  // Right
    }

    [Fact]
    public void IsCardinallyAdjacentTo_DiagonalNeighbor_ReturnsFalse()
    {
        var center = new GridCoordinate(5, 5);

        Assert.False(center.IsCardinallyAdjacentTo(new GridCoordinate(4, 4)));  // Up-Left
        Assert.False(center.IsCardinallyAdjacentTo(new GridCoordinate(6, 6)));  // Down-Right
    }

    #endregion

    #region World Position Conversion Tests

    [Fact]
    public void ToWorldPosition_OriginWithCellSizeOne_ReturnsHalfCellOffset()
    {
        var coord = new GridCoordinate(0, 0);

        var world = coord.ToWorldPosition(1f);

        Assert.Equal(0.5f, world.X, 0.0001f);
        Assert.Equal(0f, world.Y, 0.0001f);
        Assert.Equal(0.5f, world.Z, 0.0001f);
    }

    [Fact]
    public void ToWorldPosition_WithHeight_SetsYComponent()
    {
        var coord = new GridCoordinate(0, 0);

        var world = coord.ToWorldPosition(1f, 5f);

        Assert.Equal(5f, world.Y, 0.0001f);
    }

    [Fact]
    public void ToWorldPosition_LargerCellSize_ScalesCorrectly()
    {
        var coord = new GridCoordinate(2, 3);

        var world = coord.ToWorldPosition(2f);

        Assert.Equal(5f, world.X, 0.0001f);  // (2 + 0.5) * 2 = 5
        Assert.Equal(7f, world.Z, 0.0001f);  // (3 + 0.5) * 2 = 7
    }

    [Fact]
    public void FromWorldPosition_ReturnsCorrectCell()
    {
        var world = new Vector3(2.5f, 0f, 3.5f);

        var coord = GridCoordinate.FromWorldPosition(world, 1f);

        Assert.Equal(2, coord.X);
        Assert.Equal(3, coord.Y);
    }

    [Fact]
    public void FromWorldPosition_NegativeCoordinates_WorksCorrectly()
    {
        var world = new Vector3(-0.5f, 0f, -0.5f);

        var coord = GridCoordinate.FromWorldPosition(world, 1f);

        Assert.Equal(-1, coord.X);
        Assert.Equal(-1, coord.Y);
    }

    [Fact]
    public void FromWorldPosition_LargerCellSize_ScalesCorrectly()
    {
        var world = new Vector3(5f, 0f, 7f);

        var coord = GridCoordinate.FromWorldPosition(world, 2f);

        Assert.Equal(2, coord.X);  // floor(5/2) = 2
        Assert.Equal(3, coord.Y);  // floor(7/2) = 3
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void Addition_CombinesCoordinates()
    {
        var a = new GridCoordinate(3, 5);
        var b = new GridCoordinate(2, 4);

        var result = a + b;

        Assert.Equal(5, result.X);
        Assert.Equal(9, result.Y);
    }

    [Fact]
    public void Subtraction_SubtractsCoordinates()
    {
        var a = new GridCoordinate(5, 8);
        var b = new GridCoordinate(2, 3);

        var result = a - b;

        Assert.Equal(3, result.X);
        Assert.Equal(5, result.Y);
    }

    [Fact]
    public void Multiplication_ScalesCoordinate()
    {
        var coord = new GridCoordinate(3, 5);

        var result = coord * 2;

        Assert.Equal(6, result.X);
        Assert.Equal(10, result.Y);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_SameValues_ReturnsTrue()
    {
        var a = new GridCoordinate(5, 10);
        var b = new GridCoordinate(5, 10);

        Assert.Equal(a, b);
        Assert.True(a == b);
    }

    [Fact]
    public void Equality_DifferentValues_ReturnsFalse()
    {
        var a = new GridCoordinate(5, 10);
        var b = new GridCoordinate(10, 5);

        Assert.NotEqual(a, b);
        Assert.True(a != b);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        var coord = new GridCoordinate(5, 10);

        var str = coord.ToString();

        Assert.Equal("(5, 10)", str);
    }

    #endregion
}
