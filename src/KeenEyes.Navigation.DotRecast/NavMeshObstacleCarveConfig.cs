namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Configuration for the per-tick budget applied when routing
/// <c>NavMeshObstacle</c> components through a <see cref="NavMeshTileCache"/>.
/// </summary>
/// <remarks>
/// A <see cref="NavMeshTileCache"/> rebuilds at most one affected tile per
/// <see cref="NavMeshTileCache.Update"/> call, so the obstacle carve system pumps
/// <see cref="NavMeshTileCache.Update"/> up to
/// <see cref="MaxTileRebuildsPerUpdate"/> times each frame. This bounds the
/// per-frame carving cost, spreading a large re-contour across multiple frames.
/// </remarks>
public sealed class NavMeshObstacleCarveConfig
{
    /// <summary>
    /// Gets the default obstacle carve configuration.
    /// </summary>
    public static NavMeshObstacleCarveConfig Default => new();

    /// <summary>
    /// Gets or sets the maximum number of <see cref="NavMeshTileCache.Update"/>
    /// calls (each rebuilding at most one tile) applied per world update. Bounds
    /// the per-frame cost of obstacle-driven tile rebuilds.
    /// </summary>
    public int MaxTileRebuildsPerUpdate { get; set; } = 4;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (MaxTileRebuildsPerUpdate < 1)
        {
            return "MaxTileRebuildsPerUpdate must be at least 1";
        }

        return null;
    }
}
