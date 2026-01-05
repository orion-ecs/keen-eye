// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using System.Numerics;

namespace KeenEyes.Editor.Navigation;

/// <summary>
/// Configuration for baking a navigation mesh from scene geometry.
/// </summary>
/// <remarks>
/// <para>
/// This configuration extends the runtime <see cref="KeenEyes.Navigation.DotRecast.NavMeshConfig"/>
/// with additional editor-specific settings for geometry collection and region filtering.
/// </para>
/// <para>
/// Use presets like <see cref="Humanoid"/> or <see cref="Small"/> for common agent types,
/// or customize individual properties for specific requirements.
/// </para>
/// </remarks>
public sealed class NavMeshBakeConfig
{
    /// <summary>
    /// Gets the default configuration suitable for humanoid agents.
    /// </summary>
    public static NavMeshBakeConfig Humanoid => new();

    /// <summary>
    /// Gets a configuration for small agents (e.g., critters, drones).
    /// </summary>
    public static NavMeshBakeConfig Small => new()
    {
        AgentRadius = 0.25f,
        AgentHeight = 1.0f,
        MaxClimbHeight = 0.2f,
        CellSize = 0.15f,
        CellHeight = 0.1f
    };

    /// <summary>
    /// Gets a configuration for large agents (e.g., vehicles, giants).
    /// </summary>
    public static NavMeshBakeConfig Large => new()
    {
        AgentRadius = 1.0f,
        AgentHeight = 3.0f,
        MaxClimbHeight = 0.8f,
        CellSize = 0.5f,
        CellHeight = 0.3f
    };

    #region Agent Settings

    /// <summary>
    /// Gets or sets the agent radius in world units.
    /// </summary>
    /// <remarks>
    /// The radius defines the minimum clearance around obstacles.
    /// </remarks>
    public float AgentRadius { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the agent height in world units.
    /// </summary>
    /// <remarks>
    /// Used to determine minimum ceiling clearance.
    /// </remarks>
    public float AgentHeight { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the maximum slope angle the agent can walk (in degrees).
    /// </summary>
    public float MaxSlopeAngle { get; set; } = 45.0f;

    /// <summary>
    /// Gets or sets the maximum ledge height the agent can step up.
    /// </summary>
    public float MaxClimbHeight { get; set; } = 0.4f;

    #endregion

    #region Voxelization Settings

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

    #endregion

    #region Region Settings

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

    #endregion

    #region Polygon Settings

    /// <summary>
    /// Gets or sets the maximum edge length for polygon edges (in cells).
    /// </summary>
    /// <remarks>
    /// Edges longer than this are subdivided.
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

    #endregion

    #region Detail Mesh Settings

    /// <summary>
    /// Gets or sets whether to build the detail mesh.
    /// </summary>
    /// <remarks>
    /// The detail mesh provides accurate height queries but increases memory.
    /// </remarks>
    public bool BuildDetailMesh { get; set; } = true;

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

    #endregion

    #region Tiling Settings

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

    #endregion

    #region Filter Settings

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

    #endregion

    #region Editor-Specific Settings

    /// <summary>
    /// Gets or sets the baking bounds. If null, uses all geometry.
    /// </summary>
    /// <remarks>
    /// Allows limiting the navmesh to a specific region of the scene.
    /// </remarks>
    public (Vector3 Min, Vector3 Max)? BakeBounds { get; set; }

    /// <summary>
    /// Gets or sets whether to include static colliders only.
    /// </summary>
    /// <remarks>
    /// When true, only geometry marked as static is included in the bake.
    /// </remarks>
    public bool StaticOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets the layer mask for included geometry.
    /// </summary>
    /// <remarks>
    /// -1 means all layers. Otherwise, only geometry matching the mask is included.
    /// </remarks>
    public int LayerMask { get; set; } = -1;

    /// <summary>
    /// Gets or sets the output file path for the baked navmesh.
    /// </summary>
    /// <remarks>
    /// If null, a default path based on the scene name is used.
    /// </remarks>
    public string? OutputPath { get; set; }

    /// <summary>
    /// Gets or sets the agent type name for this configuration.
    /// </summary>
    /// <remarks>
    /// Used to identify the navmesh when multiple agent types are supported.
    /// </remarks>
    public string AgentTypeName { get; set; } = "Humanoid";

    #endregion

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

        if (string.IsNullOrWhiteSpace(AgentTypeName))
        {
            return "AgentTypeName cannot be empty";
        }

        return null;
    }

    /// <summary>
    /// Converts this bake configuration to a runtime NavMeshConfig.
    /// </summary>
    /// <returns>A NavMeshConfig with matching settings.</returns>
    public KeenEyes.Navigation.DotRecast.NavMeshConfig ToRuntimeConfig()
    {
        return new KeenEyes.Navigation.DotRecast.NavMeshConfig
        {
            CellSize = CellSize,
            CellHeight = CellHeight,
            AgentHeight = AgentHeight,
            AgentRadius = AgentRadius,
            MaxSlopeAngle = MaxSlopeAngle,
            MaxClimbHeight = MaxClimbHeight,
            MinRegionArea = MinRegionArea,
            MergeRegionArea = MergeRegionArea,
            MaxEdgeLength = MaxEdgeLength,
            MaxSimplificationError = MaxSimplificationError,
            MaxVertsPerPoly = MaxVertsPerPoly,
            DetailSampleDistance = DetailSampleDistance,
            DetailSampleMaxError = DetailSampleMaxError,
            UseTiles = UseTiles,
            TileSize = TileSize,
            FilterLowHangingObstacles = FilterLowHangingObstacles,
            FilterLedgeSpans = FilterLedgeSpans,
            FilterWalkableLowHeightSpans = FilterWalkableLowHeightSpans,
            BuildDetailMesh = BuildDetailMesh
        };
    }

    /// <summary>
    /// Creates a copy of this configuration.
    /// </summary>
    /// <returns>A new configuration with the same values.</returns>
    public NavMeshBakeConfig Clone()
    {
        return new NavMeshBakeConfig
        {
            AgentRadius = AgentRadius,
            AgentHeight = AgentHeight,
            MaxSlopeAngle = MaxSlopeAngle,
            MaxClimbHeight = MaxClimbHeight,
            CellSize = CellSize,
            CellHeight = CellHeight,
            MinRegionArea = MinRegionArea,
            MergeRegionArea = MergeRegionArea,
            MaxEdgeLength = MaxEdgeLength,
            MaxSimplificationError = MaxSimplificationError,
            MaxVertsPerPoly = MaxVertsPerPoly,
            BuildDetailMesh = BuildDetailMesh,
            DetailSampleDistance = DetailSampleDistance,
            DetailSampleMaxError = DetailSampleMaxError,
            UseTiles = UseTiles,
            TileSize = TileSize,
            FilterLowHangingObstacles = FilterLowHangingObstacles,
            FilterLedgeSpans = FilterLedgeSpans,
            FilterWalkableLowHeightSpans = FilterWalkableLowHeightSpans,
            BakeBounds = BakeBounds,
            StaticOnly = StaticOnly,
            LayerMask = LayerMask,
            OutputPath = OutputPath,
            AgentTypeName = AgentTypeName
        };
    }
}
