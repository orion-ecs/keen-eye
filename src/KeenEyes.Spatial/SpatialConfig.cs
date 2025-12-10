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

    /// <summary>
    /// Configuration for quadtree-based partitioning (used when Strategy is Quadtree).
    /// </summary>
    public QuadtreeConfig Quadtree { get; init; } = new();

    /// <summary>
    /// Configuration for octree-based partitioning (used when Strategy is Octree).
    /// </summary>
    public OctreeConfig Octree { get; init; } = new();

    /// <summary>
    /// Validates the configuration and returns any errors.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        return Strategy switch
        {
            SpatialStrategy.Grid => Grid.Validate(),
            SpatialStrategy.Quadtree => Quadtree.Validate(),
            SpatialStrategy.Octree => Octree.Validate(),
            _ => $"Unknown spatial strategy: {Strategy}"
        };
    }
}
