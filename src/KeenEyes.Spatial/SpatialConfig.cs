namespace KeenEyes.Spatial;

/// <summary>
/// Configuration for the spatial partitioning plugin.
/// </summary>
public sealed class SpatialConfig
{
    /// <summary>
    /// The spatial partitioning strategy to use.
    /// </summary>
    public SpatialStrategy Strategy { get; init; } = SpatialStrategy.Grid;

    /// <summary>
    /// Configuration for grid-based partitioning (used when Strategy is Grid).
    /// </summary>
    public GridConfig Grid { get; init; } = new();

    // Future: QuadtreeConfig and OctreeConfig will be added in Phase 2

    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        return Strategy switch
        {
            SpatialStrategy.Grid => Grid.Validate(),
            SpatialStrategy.Quadtree => "Quadtree strategy not yet implemented (coming in Phase 2)",
            SpatialStrategy.Octree => "Octree strategy not yet implemented (coming in Phase 2)",
            _ => $"Unknown spatial strategy: {Strategy}"
        };
    }
}
