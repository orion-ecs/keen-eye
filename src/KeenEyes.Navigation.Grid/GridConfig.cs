using System.Numerics;

namespace KeenEyes.Navigation.Grid;

/// <summary>
/// Configuration options for grid-based navigation.
/// </summary>
public sealed class GridConfig
{
    /// <summary>
    /// Gets or sets the width of the grid in cells.
    /// </summary>
    /// <remarks>Must be positive.</remarks>
    public int Width { get; set; } = 100;

    /// <summary>
    /// Gets or sets the height of the grid in cells.
    /// </summary>
    /// <remarks>Must be positive.</remarks>
    public int Height { get; set; } = 100;

    /// <summary>
    /// Gets or sets the size of each cell in world units.
    /// </summary>
    /// <remarks>Must be positive.</remarks>
    public float CellSize { get; set; } = 1f;

    /// <summary>
    /// Gets or sets the world-space origin of the grid.
    /// </summary>
    public Vector3 WorldOrigin { get; set; } = Vector3.Zero;

    /// <summary>
    /// Gets or sets whether diagonal movement is allowed.
    /// </summary>
    public bool AllowDiagonal { get; set; } = true;

    /// <summary>
    /// Gets or sets the heuristic to use for A* pathfinding.
    /// </summary>
    public GridHeuristic Heuristic { get; set; } = GridHeuristic.Octile;

    /// <summary>
    /// Gets or sets the maximum number of nodes to expand before giving up.
    /// </summary>
    /// <remarks>
    /// A value of 0 means no limit. This prevents pathfinding from running
    /// too long on impossible or very long paths.
    /// </remarks>
    public int MaxIterations { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the maximum number of pending path requests.
    /// </summary>
    public int MaxPendingRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of path requests to process per update.
    /// </summary>
    public int RequestsPerUpdate { get; set; } = 10;

    /// <summary>
    /// Gets or sets whether to cache frequently used paths.
    /// </summary>
    public bool EnablePathCaching { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of cached paths.
    /// </summary>
    public int MaxCachedPaths { get; set; } = 100;

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static GridConfig Default => new();

    /// <summary>
    /// Creates a configuration for a specific grid size.
    /// </summary>
    /// <param name="width">The width in cells.</param>
    /// <param name="height">The height in cells.</param>
    /// <param name="cellSize">The cell size in world units.</param>
    /// <returns>A new configuration.</returns>
    public static GridConfig WithSize(int width, int height, float cellSize = 1f) => new()
    {
        Width = width,
        Height = height,
        CellSize = cellSize
    };

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>Null if valid, otherwise an error message.</returns>
    public string? Validate()
    {
        if (Width <= 0)
        {
            return $"Width must be positive, got {Width}.";
        }

        if (Height <= 0)
        {
            return $"Height must be positive, got {Height}.";
        }

        if (CellSize <= 0f)
        {
            return $"CellSize must be positive, got {CellSize}.";
        }

        if (MaxIterations < 0)
        {
            return $"MaxIterations must be non-negative, got {MaxIterations}.";
        }

        if (MaxPendingRequests <= 0)
        {
            return $"MaxPendingRequests must be positive, got {MaxPendingRequests}.";
        }

        if (RequestsPerUpdate <= 0)
        {
            return $"RequestsPerUpdate must be positive, got {RequestsPerUpdate}.";
        }

        if (EnablePathCaching && MaxCachedPaths <= 0)
        {
            return $"MaxCachedPaths must be positive when caching is enabled, got {MaxCachedPaths}.";
        }

        return null;
    }
}

/// <summary>
/// Heuristic functions for A* pathfinding on a grid.
/// </summary>
public enum GridHeuristic
{
    /// <summary>
    /// Manhattan distance (sum of absolute differences).
    /// Best for 4-directional movement.
    /// </summary>
    Manhattan,

    /// <summary>
    /// Chebyshev distance (maximum of absolute differences).
    /// Assumes diagonal and cardinal moves have equal cost.
    /// </summary>
    Chebyshev,

    /// <summary>
    /// Octile distance (accounts for sqrt(2) diagonal cost).
    /// Best for 8-directional movement with realistic diagonal costs.
    /// </summary>
    Octile,

    /// <summary>
    /// Euclidean distance (straight-line).
    /// Most accurate but slightly slower to compute.
    /// </summary>
    Euclidean
}
