namespace KeenEyes.Spatial;

/// <summary>
/// Defines the spatial partitioning strategy to use for organizing entities.
/// </summary>
public enum SpatialStrategy
{
    /// <summary>
    /// Grid-based spatial partitioning with fixed cell sizes.
    /// </summary>
    /// <remarks>
    /// Best for uniformly distributed entities. Provides O(1) cell lookup
    /// and consistent performance regardless of entity clustering.
    /// </remarks>
    Grid = 0,

    /// <summary>
    /// Quadtree-based spatial partitioning for 2D space.
    /// </summary>
    /// <remarks>
    /// Best for clustered 2D entities. Adaptively subdivides space based on
    /// entity density. More efficient than grids for non-uniform distributions.
    /// </remarks>
    Quadtree = 1,

    /// <summary>
    /// Octree-based spatial partitioning for 3D space.
    /// </summary>
    /// <remarks>
    /// Best for clustered 3D entities. Adaptively subdivides space based on
    /// entity density. More efficient than grids for non-uniform distributions.
    /// </remarks>
    Octree = 2
}
