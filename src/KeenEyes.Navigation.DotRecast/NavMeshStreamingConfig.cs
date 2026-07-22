namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Configuration for runtime navigation mesh tile streaming.
/// </summary>
/// <remarks>
/// Tiles whose footprint lies within <see cref="LoadRadius"/> of any anchor are
/// loaded; tiles farther than <see cref="LoadRadius"/> plus
/// <see cref="UnloadHysteresis"/> from every anchor are unloaded. Tiles in the
/// hysteresis band keep their current state, preventing load/unload thrash when
/// an anchor hovers near a tile boundary.
/// </remarks>
public sealed class NavMeshStreamingConfig
{
    /// <summary>
    /// Gets the default streaming configuration.
    /// </summary>
    public static NavMeshStreamingConfig Default => new();

    /// <summary>
    /// Gets or sets the radius (in world units) around each anchor within
    /// which tiles are loaded.
    /// </summary>
    public float LoadRadius { get; set; } = 64f;

    /// <summary>
    /// Gets or sets the extra distance (in world units) beyond
    /// <see cref="LoadRadius"/> a loaded tile must be from every anchor before
    /// it is unloaded.
    /// </summary>
    public float UnloadHysteresis { get; set; } = 16f;

    /// <summary>
    /// Gets or sets the maximum number of tile install/remove operations
    /// applied to the navigation mesh per <see cref="NavMeshStreamingManager.Update"/>
    /// call. Bounds the per-frame cost of streaming.
    /// </summary>
    public int MaxTileOperationsPerUpdate { get; set; } = 4;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (LoadRadius <= 0f)
        {
            return "LoadRadius must be greater than 0";
        }

        if (UnloadHysteresis < 0f)
        {
            return "UnloadHysteresis must be non-negative";
        }

        if (MaxTileOperationsPerUpdate < 1)
        {
            return "MaxTileOperationsPerUpdate must be at least 1";
        }

        return null;
    }
}
