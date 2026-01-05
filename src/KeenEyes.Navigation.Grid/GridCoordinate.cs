using System.Numerics;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Represents a 2D coordinate on a navigation grid.
/// </summary>
/// <remarks>
/// Grid coordinates use integer X and Y values to identify cells.
/// The Y coordinate typically maps to the Z axis in 3D space for top-down games.
/// </remarks>
/// <param name="X">The X coordinate (column) on the grid.</param>
/// <param name="Y">The Y coordinate (row) on the grid.</param>
public readonly record struct GridCoordinate(int X, int Y)
{
    /// <summary>
    /// Gets the origin coordinate (0, 0).
    /// </summary>
    public static GridCoordinate Origin => new(0, 0);

    /// <summary>
    /// Gets the coordinate offset for moving up (negative Y).
    /// </summary>
    public static GridCoordinate Up => new(0, -1);

    /// <summary>
    /// Gets the coordinate offset for moving down (positive Y).
    /// </summary>
    public static GridCoordinate Down => new(0, 1);

    /// <summary>
    /// Gets the coordinate offset for moving left (negative X).
    /// </summary>
    public static GridCoordinate Left => new(-1, 0);

    /// <summary>
    /// Gets the coordinate offset for moving right (positive X).
    /// </summary>
    public static GridCoordinate Right => new(1, 0);

    private static readonly GridCoordinate[] cardinalDirections =
    [
        new(0, -1),  // Up
        new(0, 1),   // Down
        new(-1, 0),  // Left
        new(1, 0)    // Right
    ];

    private static readonly GridCoordinate[] diagonalDirections =
    [
        new(-1, -1), // Up-Left
        new(1, -1),  // Up-Right
        new(-1, 1),  // Down-Left
        new(1, 1)    // Down-Right
    ];

    private static readonly GridCoordinate[] allDirections =
    [
        new(0, -1),  // Up
        new(0, 1),   // Down
        new(-1, 0),  // Left
        new(1, 0),   // Right
        new(-1, -1), // Up-Left
        new(1, -1),  // Up-Right
        new(-1, 1),  // Down-Left
        new(1, 1)    // Down-Right
    ];

    /// <summary>
    /// Gets the four cardinal direction offsets (up, down, left, right).
    /// </summary>
    public static ReadOnlySpan<GridCoordinate> CardinalDirections => cardinalDirections;

    /// <summary>
    /// Gets the four diagonal direction offsets.
    /// </summary>
    public static ReadOnlySpan<GridCoordinate> DiagonalDirections => diagonalDirections;

    /// <summary>
    /// Gets all eight direction offsets (cardinal + diagonal).
    /// </summary>
    public static ReadOnlySpan<GridCoordinate> AllDirections => allDirections;

    /// <summary>
    /// Calculates the Manhattan distance to another coordinate.
    /// </summary>
    /// <param name="other">The target coordinate.</param>
    /// <returns>The Manhattan distance (sum of absolute differences).</returns>
    public int ManhattanDistance(GridCoordinate other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>
    /// Calculates the Chebyshev distance (chessboard distance) to another coordinate.
    /// </summary>
    /// <param name="other">The target coordinate.</param>
    /// <returns>The maximum of the absolute differences.</returns>
    public int ChebyshevDistance(GridCoordinate other)
        => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    /// <summary>
    /// Calculates the octile distance to another coordinate.
    /// </summary>
    /// <remarks>
    /// Octile distance is used for grids with 8-directional movement where
    /// diagonal moves cost sqrt(2) times the cardinal move cost.
    /// </remarks>
    /// <param name="other">The target coordinate.</param>
    /// <returns>The octile distance as a float.</returns>
    public float OctileDistance(GridCoordinate other)
    {
        int dx = Math.Abs(X - other.X);
        int dy = Math.Abs(Y - other.Y);
        const float Sqrt2 = 1.41421356f;
        return dx > dy
            ? dx + (Sqrt2 - 1f) * dy
            : dy + (Sqrt2 - 1f) * dx;
    }

    /// <summary>
    /// Calculates the Euclidean distance to another coordinate.
    /// </summary>
    /// <param name="other">The target coordinate.</param>
    /// <returns>The straight-line distance.</returns>
    public float EuclideanDistance(GridCoordinate other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Checks if this coordinate is adjacent to another (including diagonals).
    /// </summary>
    /// <param name="other">The coordinate to check.</param>
    /// <returns>True if the coordinates are adjacent.</returns>
    public bool IsAdjacentTo(GridCoordinate other)
        => ChebyshevDistance(other) == 1;

    /// <summary>
    /// Checks if this coordinate is cardinally adjacent to another (not diagonal).
    /// </summary>
    /// <param name="other">The coordinate to check.</param>
    /// <returns>True if the coordinates are cardinally adjacent.</returns>
    public bool IsCardinallyAdjacentTo(GridCoordinate other)
        => ManhattanDistance(other) == 1;

    /// <summary>
    /// Converts to a 3D world position using the specified cell size and height.
    /// </summary>
    /// <param name="cellSize">The size of each grid cell in world units.</param>
    /// <param name="height">The Y height in world space (default 0).</param>
    /// <returns>The world position at the center of this cell.</returns>
    public Vector3 ToWorldPosition(float cellSize, float height = 0f)
        => new((X + 0.5f) * cellSize, height, (Y + 0.5f) * cellSize);

    /// <summary>
    /// Creates a coordinate from a world position.
    /// </summary>
    /// <param name="position">The world position (uses X and Z).</param>
    /// <param name="cellSize">The size of each grid cell.</param>
    /// <returns>The grid coordinate containing this position.</returns>
    public static GridCoordinate FromWorldPosition(Vector3 position, float cellSize)
        => new(
            (int)MathF.Floor(position.X / cellSize),
            (int)MathF.Floor(position.Z / cellSize));

    /// <summary>
    /// Adds two coordinates together.
    /// </summary>
    public static GridCoordinate operator +(GridCoordinate a, GridCoordinate b)
        => new(a.X + b.X, a.Y + b.Y);

    /// <summary>
    /// Subtracts one coordinate from another.
    /// </summary>
    public static GridCoordinate operator -(GridCoordinate a, GridCoordinate b)
        => new(a.X - b.X, a.Y - b.Y);

    /// <summary>
    /// Multiplies a coordinate by a scalar.
    /// </summary>
    public static GridCoordinate operator *(GridCoordinate coord, int scalar)
        => new(coord.X * scalar, coord.Y * scalar);

    /// <inheritdoc/>
    public override string ToString() => $"({X}, {Y})";
}
