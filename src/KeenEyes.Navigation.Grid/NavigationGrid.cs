using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Represents a 2D navigation grid for pathfinding.
/// </summary>
/// <remarks>
/// <para>
/// The navigation grid stores walkability and area type information for a rectangular
/// region. It supports both cardinal (4-direction) and diagonal (8-direction) movement.
/// </para>
/// <para>
/// Grid coordinates map to world space using the configured cell size. The grid Y axis
/// corresponds to the world Z axis (top-down perspective).
/// </para>
/// </remarks>
public sealed class NavigationGrid
{
    private readonly GridCell[] cells;

    /// <summary>
    /// Gets the width of the grid in cells.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Gets the height of the grid in cells.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Gets the size of each cell in world units.
    /// </summary>
    public float CellSize { get; }

    /// <summary>
    /// Gets the world-space origin of the grid (minimum corner).
    /// </summary>
    public Vector3 WorldOrigin { get; }

    /// <summary>
    /// Gets the total number of cells in the grid.
    /// </summary>
    public int CellCount => Width * Height;

    /// <summary>
    /// Creates a new navigation grid.
    /// </summary>
    /// <param name="width">Width in cells.</param>
    /// <param name="height">Height in cells.</param>
    /// <param name="cellSize">Size of each cell in world units.</param>
    /// <param name="worldOrigin">World-space origin (default: Vector3.Zero).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when width, height, or cellSize is not positive.
    /// </exception>
    public NavigationGrid(int width, int height, float cellSize, Vector3? worldOrigin = null)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "Width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), height, "Height must be positive.");
        }

        if (cellSize <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be positive.");
        }

        Width = width;
        Height = height;
        CellSize = cellSize;
        WorldOrigin = worldOrigin ?? Vector3.Zero;
        cells = new GridCell[width * height];

        // Initialize all cells as walkable by default
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = GridCell.Walkable;
        }
    }

    /// <summary>
    /// Gets the cell data at the specified coordinate.
    /// </summary>
    /// <param name="coord">The grid coordinate.</param>
    /// <returns>A reference to the cell data.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the coordinate is outside the grid bounds.
    /// </exception>
    public ref GridCell this[GridCoordinate coord]
    {
        get
        {
            if (!IsInBounds(coord))
            {
                throw new ArgumentOutOfRangeException(nameof(coord), coord, "Coordinate is outside grid bounds.");
            }

            return ref cells[GetIndex(coord)];
        }
    }

    /// <summary>
    /// Gets the cell data at the specified coordinate.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <returns>A reference to the cell data.</returns>
    public ref GridCell this[int x, int y] => ref this[new GridCoordinate(x, y)];

    /// <summary>
    /// Tries to get the cell data at the specified coordinate.
    /// </summary>
    /// <param name="coord">The grid coordinate.</param>
    /// <param name="cell">The cell data if the coordinate is valid.</param>
    /// <returns>True if the coordinate is within bounds.</returns>
    public bool TryGetCell(GridCoordinate coord, out GridCell cell)
    {
        if (!IsInBounds(coord))
        {
            cell = default;
            return false;
        }

        cell = cells[GetIndex(coord)];
        return true;
    }

    /// <summary>
    /// Sets the cell data at the specified coordinate.
    /// </summary>
    /// <param name="coord">The grid coordinate.</param>
    /// <param name="cell">The cell data to set.</param>
    /// <returns>True if the coordinate was valid and the cell was set.</returns>
    public bool TrySetCell(GridCoordinate coord, GridCell cell)
    {
        if (!IsInBounds(coord))
        {
            return false;
        }

        cells[GetIndex(coord)] = cell;
        return true;
    }

    /// <summary>
    /// Checks if a coordinate is within the grid bounds.
    /// </summary>
    /// <param name="coord">The coordinate to check.</param>
    /// <returns>True if the coordinate is valid.</returns>
    public bool IsInBounds(GridCoordinate coord)
        => coord.X >= 0 && coord.X < Width && coord.Y >= 0 && coord.Y < Height;

    /// <summary>
    /// Checks if a coordinate is walkable.
    /// </summary>
    /// <param name="coord">The coordinate to check.</param>
    /// <returns>True if the coordinate is in bounds and walkable.</returns>
    public bool IsWalkable(GridCoordinate coord)
        => IsInBounds(coord) && cells[GetIndex(coord)].CanTraverse();

    /// <summary>
    /// Checks if a coordinate is walkable considering the area mask.
    /// </summary>
    /// <param name="coord">The coordinate to check.</param>
    /// <param name="areaMask">The area mask to filter traversable cells.</param>
    /// <returns>True if the coordinate is walkable and its area type is in the mask.</returns>
    public bool IsWalkable(GridCoordinate coord, NavAreaMask areaMask)
    {
        if (!IsInBounds(coord))
        {
            return false;
        }

        ref readonly var cell = ref cells[GetIndex(coord)];
        if (!cell.CanTraverse())
        {
            return false;
        }

        var cellMask = (NavAreaMask)(1u << (int)cell.AreaType);
        return (areaMask & cellMask) != NavAreaMask.None;
    }

    /// <summary>
    /// Gets the area type at the specified coordinate.
    /// </summary>
    /// <param name="coord">The coordinate to query.</param>
    /// <returns>The area type, or <see cref="NavAreaType.NotWalkable"/> if out of bounds.</returns>
    public NavAreaType GetAreaType(GridCoordinate coord)
        => IsInBounds(coord) ? cells[GetIndex(coord)].AreaType : NavAreaType.NotWalkable;

    /// <summary>
    /// Gets the movement cost at the specified coordinate.
    /// </summary>
    /// <param name="coord">The coordinate to query.</param>
    /// <returns>The movement cost, or float.MaxValue if out of bounds or not walkable.</returns>
    public float GetCost(GridCoordinate coord)
    {
        if (!IsInBounds(coord))
        {
            return float.MaxValue;
        }

        ref readonly var cell = ref cells[GetIndex(coord)];
        return cell.CanTraverse() ? cell.Cost : float.MaxValue;
    }

    /// <summary>
    /// Converts a grid coordinate to a world position.
    /// </summary>
    /// <param name="coord">The grid coordinate.</param>
    /// <param name="height">The Y height in world space (default 0).</param>
    /// <returns>The world position at the center of the cell.</returns>
    public Vector3 ToWorldPosition(GridCoordinate coord, float height = 0f)
        => WorldOrigin + new Vector3(
            (coord.X + 0.5f) * CellSize,
            height,
            (coord.Y + 0.5f) * CellSize);

    /// <summary>
    /// Converts a world position to a grid coordinate.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <returns>The grid coordinate containing this position.</returns>
    public GridCoordinate FromWorldPosition(Vector3 position)
    {
        var relative = position - WorldOrigin;
        return new GridCoordinate(
            (int)MathF.Floor(relative.X / CellSize),
            (int)MathF.Floor(relative.Z / CellSize));
    }

    /// <summary>
    /// Gets the world-space bounds of the grid.
    /// </summary>
    /// <returns>A tuple containing the minimum and maximum corners.</returns>
    public (Vector3 Min, Vector3 Max) GetWorldBounds()
    {
        var max = WorldOrigin + new Vector3(Width * CellSize, 0f, Height * CellSize);
        return (WorldOrigin, max);
    }

    /// <summary>
    /// Fills a rectangular region with the specified cell data.
    /// </summary>
    /// <param name="start">The start corner of the region.</param>
    /// <param name="end">The end corner of the region (inclusive).</param>
    /// <param name="cell">The cell data to fill with.</param>
    public void Fill(GridCoordinate start, GridCoordinate end, GridCell cell)
    {
        int minX = Math.Max(0, Math.Min(start.X, end.X));
        int maxX = Math.Min(Width - 1, Math.Max(start.X, end.X));
        int minY = Math.Max(0, Math.Min(start.Y, end.Y));
        int maxY = Math.Min(Height - 1, Math.Max(start.Y, end.Y));

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                cells[y * Width + x] = cell;
            }
        }
    }

    /// <summary>
    /// Sets all cells to walkable.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = GridCell.Walkable;
        }
    }

    /// <summary>
    /// Sets all cells to blocked (unwalkable).
    /// </summary>
    public void Block()
    {
        for (int i = 0; i < cells.Length; i++)
        {
            cells[i] = GridCell.Blocked;
        }
    }

    /// <summary>
    /// Gets the neighbors of a cell that are walkable.
    /// </summary>
    /// <param name="coord">The center coordinate.</param>
    /// <param name="allowDiagonal">Whether to include diagonal neighbors.</param>
    /// <param name="results">Buffer to store the walkable neighbor coordinates.</param>
    /// <returns>The number of walkable neighbors found.</returns>
    public int GetWalkableNeighbors(GridCoordinate coord, bool allowDiagonal, Span<GridCoordinate> results)
    {
        return GetWalkableNeighbors(coord, allowDiagonal, NavAreaMask.All, results);
    }

    /// <summary>
    /// Gets the neighbors of a cell that are walkable with area filtering.
    /// </summary>
    /// <param name="coord">The center coordinate.</param>
    /// <param name="allowDiagonal">Whether to include diagonal neighbors.</param>
    /// <param name="areaMask">The area mask to filter traversable cells.</param>
    /// <param name="results">Buffer to store the walkable neighbor coordinates.</param>
    /// <returns>The number of walkable neighbors found.</returns>
    public int GetWalkableNeighbors(GridCoordinate coord, bool allowDiagonal, NavAreaMask areaMask, Span<GridCoordinate> results)
    {
        int count = 0;
        int maxCount = results.Length;

        // Cardinal directions first
        ReadOnlySpan<GridCoordinate> cardinals = GridCoordinate.CardinalDirections;
        for (int i = 0; i < cardinals.Length && count < maxCount; i++)
        {
            var neighbor = coord + cardinals[i];
            if (IsWalkable(neighbor, areaMask))
            {
                results[count++] = neighbor;
            }
        }

        if (!allowDiagonal)
        {
            return count;
        }

        // Diagonal directions - check corner cutting
        ReadOnlySpan<GridCoordinate> diagonals = GridCoordinate.DiagonalDirections;
        for (int i = 0; i < diagonals.Length && count < maxCount; i++)
        {
            var diagonal = diagonals[i];
            var neighbor = coord + diagonal;

            if (!IsWalkable(neighbor, areaMask))
            {
                continue;
            }

            // Check corner cutting: both adjacent cardinal cells must be walkable
            var horizontal = coord + new GridCoordinate(diagonal.X, 0);
            var vertical = coord + new GridCoordinate(0, diagonal.Y);

            if (IsWalkable(horizontal, areaMask) && IsWalkable(vertical, areaMask))
            {
                results[count++] = neighbor;
            }
        }

        return count;
    }

    /// <summary>
    /// Gets the raw cell array for advanced operations.
    /// </summary>
    /// <returns>A span over the cell data.</returns>
    public Span<GridCell> GetCellsSpan() => cells.AsSpan();

    /// <summary>
    /// Gets a read-only span over the cell data.
    /// </summary>
    /// <returns>A read-only span over the cell data.</returns>
    public ReadOnlySpan<GridCell> GetCellsReadOnlySpan() => cells.AsSpan();

    private int GetIndex(GridCoordinate coord) => coord.Y * Width + coord.X;
}
