using System.Numerics;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Configuration for building a navigation mesh.
/// </summary>
/// <remarks>
/// <para>
/// These settings control how the navigation mesh is generated from input geometry.
/// The values should be tuned based on your game's scale and agent characteristics.
/// </para>
/// <para>
/// The cell size determines the resolution of the navmesh. Smaller values produce
/// more accurate results but increase build time and memory usage.
/// </para>
/// </remarks>
public sealed class NavMeshConfig
{
    /// <summary>
    /// Gets the default configuration suitable for typical 3D games.
    /// </summary>
    public static NavMeshConfig Default => new();

    /// <summary>
    /// Gets or sets the xz-plane cell size (voxel width) in world units.
    /// </summary>
    /// <remarks>
    /// Smaller values increase detail but also increase memory and build time.
    /// Typical values: 0.1 to 0.5 for indoor, 0.3 to 1.0 for outdoor scenes.
    /// </remarks>
    public float CellSize { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the y-axis cell size (voxel height) in world units.
    /// </summary>
    /// <remarks>
    /// Should typically be 0.5x to 1x of CellSize.
    /// </remarks>
    public float CellHeight { get; set; } = 0.2f;

    /// <summary>
    /// Gets or sets the agent height in world units.
    /// </summary>
    public float AgentHeight { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the agent radius in world units.
    /// </summary>
    public float AgentRadius { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the maximum slope angle the agent can walk (in degrees).
    /// </summary>
    public float MaxSlopeAngle { get; set; } = 45.0f;

    /// <summary>
    /// Gets or sets the maximum ledge height the agent can climb.
    /// </summary>
    public float MaxClimbHeight { get; set; } = 0.4f;

    /// <summary>
    /// Gets or sets the minimum region area in cells.
    /// </summary>
    /// <remarks>
    /// Regions smaller than this are removed. This helps filter out noise.
    /// </remarks>
    public int MinRegionArea { get; set; } = 8;

    /// <summary>
    /// Gets or sets the merge region area threshold.
    /// </summary>
    /// <remarks>
    /// Regions smaller than this may be merged into larger neighbors.
    /// </remarks>
    public int MergeRegionArea { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum edge length for polygon edges.
    /// </summary>
    /// <remarks>
    /// Edges longer than this are subdivided. Measured in cells.
    /// </remarks>
    public int MaxEdgeLength { get; set; } = 12;

    /// <summary>
    /// Gets or sets the maximum simplification error for contours.
    /// </summary>
    /// <remarks>
    /// Maximum distance a simplified contour's border can deviate from the original.
    /// </remarks>
    public float MaxSimplificationError { get; set; } = 1.3f;

    /// <summary>
    /// Gets or sets the maximum vertices per polygon.
    /// </summary>
    /// <remarks>
    /// Higher values create fewer polygons but increase complexity.
    /// Valid range: 3-6. Default is 6 for best performance.
    /// </remarks>
    public int MaxVertsPerPoly { get; set; } = 6;

    /// <summary>
    /// Gets or sets the detail mesh sample distance.
    /// </summary>
    /// <remarks>
    /// Controls detail mesh generation for accurate height queries.
    /// </remarks>
    public float DetailSampleDistance { get; set; } = 6.0f;

    /// <summary>
    /// Gets or sets the detail mesh maximum sample error.
    /// </summary>
    public float DetailSampleMaxError { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether to use tiled navmesh building.
    /// </summary>
    /// <remarks>
    /// Tiled meshes are better for large worlds and support runtime updates.
    /// </remarks>
    public bool UseTiles { get; set; } = true;

    /// <summary>
    /// Gets or sets the tile size in cells.
    /// </summary>
    /// <remarks>
    /// Only used when <see cref="UseTiles"/> is true.
    /// </remarks>
    public int TileSize { get; set; } = 48;

    /// <summary>
    /// Gets or sets whether to filter low-hanging obstacles.
    /// </summary>
    public bool FilterLowHangingObstacles { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to filter ledge spans.
    /// </summary>
    public bool FilterLedgeSpans { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to filter walkable low height spans.
    /// </summary>
    public bool FilterWalkableLowHeightSpans { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to build the detail mesh.
    /// </summary>
    /// <remarks>
    /// The detail mesh provides accurate height queries but increases memory.
    /// </remarks>
    public bool BuildDetailMesh { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum pending path requests.
    /// </summary>
    public int MaxPendingRequests { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of path requests to process per update.
    /// </summary>
    public int RequestsPerUpdate { get; set; } = 10;

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <returns>An error message if invalid, or null if valid.</returns>
    public string? Validate()
    {
        if (CellSize <= 0)
        {
            return "CellSize must be greater than 0";
        }

        if (CellHeight <= 0)
        {
            return "CellHeight must be greater than 0";
        }

        if (AgentHeight <= 0)
        {
            return "AgentHeight must be greater than 0";
        }

        if (AgentRadius <= 0)
        {
            return "AgentRadius must be greater than 0";
        }

        if (MaxSlopeAngle is < 0 or > 90)
        {
            return "MaxSlopeAngle must be between 0 and 90 degrees";
        }

        if (MaxClimbHeight < 0)
        {
            return "MaxClimbHeight must be non-negative";
        }

        if (MaxVertsPerPoly is < 3 or > 6)
        {
            return "MaxVertsPerPoly must be between 3 and 6";
        }

        if (UseTiles && TileSize < 16)
        {
            return "TileSize must be at least 16 when using tiles";
        }

        if (MaxPendingRequests < 1)
        {
            return "MaxPendingRequests must be at least 1";
        }

        if (RequestsPerUpdate < 1)
        {
            return "RequestsPerUpdate must be at least 1";
        }

        return null;
    }

    /// <summary>
    /// Creates agent settings from this configuration.
    /// </summary>
    /// <returns>Agent settings matching this configuration.</returns>
    public Abstractions.AgentSettings ToAgentSettings()
    {
        return new Abstractions.AgentSettings(
            AgentRadius,
            AgentHeight,
            MaxSlopeAngle,
            MaxClimbHeight);
    }
}
