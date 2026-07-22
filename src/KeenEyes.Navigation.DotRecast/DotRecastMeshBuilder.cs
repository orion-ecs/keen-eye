using System.Numerics;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Recast;
using DotRecast.Recast.Geom;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Builds navigation meshes from input geometry using DotRecast.
/// </summary>
/// <remarks>
/// <para>
/// The mesh builder takes triangulated geometry (vertices and indices) and
/// generates a navigation mesh suitable for 3D pathfinding. The process involves:
/// </para>
/// <list type="number">
/// <item>Voxelizing the input geometry</item>
/// <item>Filtering walkable surfaces</item>
/// <item>Building regions and contours</item>
/// <item>Generating polygon mesh and detail mesh</item>
/// </list>
/// <para>
/// For large worlds, enable tiled building with <see cref="NavMeshConfig.UseTiles"/>.
/// A tiled build partitions the geometry bounds into a grid of square tiles
/// (<see cref="NavMeshConfig.TileSize"/> cells on a side), builds each tile
/// independently with a bordered configuration, and installs them into a
/// multi-tile <see cref="DtNavMesh"/> via <see cref="DtNavMesh.AddTile"/>. This is
/// the prerequisite for runtime tile streaming and bounded partial rebuilds.
/// </para>
/// </remarks>
public sealed class DotRecastMeshBuilder
{
    private readonly NavMeshConfig config;
    private readonly RcBuilder builder;

    /// <summary>
    /// Creates a new mesh builder with the specified configuration.
    /// </summary>
    /// <param name="config">The build configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when config validation fails.</exception>
    public DotRecastMeshBuilder(NavMeshConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException(error, nameof(config));
        }

        this.config = config;
        builder = new RcBuilder();
    }

    /// <summary>
    /// Creates a new mesh builder with default configuration.
    /// </summary>
    public DotRecastMeshBuilder()
        : this(NavMeshConfig.Default)
    {
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public NavMeshConfig Config => config;

    /// <summary>
    /// Builds a navigation mesh from triangulated geometry.
    /// </summary>
    /// <param name="vertices">The mesh vertices (XYZ triplets).</param>
    /// <param name="indices">The triangle indices.</param>
    /// <param name="areaIds">Optional per-triangle area IDs. If null, all triangles are walkable.</param>
    /// <param name="overrideTileSize">
    /// Optional per-surface tile size (in cells) that overrides
    /// <see cref="NavMeshConfig.TileSize"/> when greater than zero. Mirrors
    /// <c>NavMeshSurface.OverrideTileSize</c>. Ignored when tiling is disabled.
    /// </param>
    /// <returns>The built navigation mesh data.</returns>
    /// <exception cref="ArgumentException">Thrown when geometry is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when overrideTileSize is negative.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mesh building fails.</exception>
    public NavMeshData Build(ReadOnlySpan<float> vertices, ReadOnlySpan<int> indices, int[]? areaIds = null, int overrideTileSize = 0)
    {
        if (vertices.Length == 0 || vertices.Length % 3 != 0)
        {
            throw new ArgumentException("Vertices must be XYZ triplets", nameof(vertices));
        }

        if (indices.Length == 0 || indices.Length % 3 != 0)
        {
            throw new ArgumentException("Indices must be triangle triplets", nameof(indices));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(overrideTileSize);

        // Convert to arrays for DotRecast
        var vertsArray = vertices.ToArray();
        var trisArray = indices.ToArray();

        // Create geometry provider
        var geom = new RcSampleInputGeomProvider(vertsArray, trisArray);

        // Compute bounds
        var bmin = geom.GetMeshBoundsMin();
        var bmax = geom.GetMeshBoundsMax();

        // Build the navmesh
        // areaIds reserved for future area marking support
        _ = areaIds;
        return BuildFromGeometry(geom, bmin, bmax, overrideTileSize);
    }

    /// <summary>
    /// Builds a navigation mesh from Vector3 vertices and triangle indices.
    /// </summary>
    /// <param name="vertices">The mesh vertices.</param>
    /// <param name="indices">The triangle indices.</param>
    /// <param name="areaIds">Optional per-triangle area IDs.</param>
    /// <param name="overrideTileSize">
    /// Optional per-surface tile size (in cells) that overrides
    /// <see cref="NavMeshConfig.TileSize"/> when greater than zero.
    /// </param>
    /// <returns>The built navigation mesh data.</returns>
    public NavMeshData Build(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices, int[]? areaIds = null, int overrideTileSize = 0)
    {
        // Convert Vector3 to float triplets
        var floatVerts = new float[vertices.Length * 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            floatVerts[i * 3] = vertices[i].X;
            floatVerts[i * 3 + 1] = vertices[i].Y;
            floatVerts[i * 3 + 2] = vertices[i].Z;
        }

        return Build(floatVerts, indices, areaIds, overrideTileSize);
    }

    /// <summary>
    /// Builds a simple box-shaped navigation mesh for testing.
    /// </summary>
    /// <param name="min">The minimum corner of the box.</param>
    /// <param name="max">The maximum corner of the box.</param>
    /// <returns>The built navigation mesh data.</returns>
    /// <remarks>
    /// Creates a complete box geometry (6 faces) which Recast can properly voxelize.
    /// The top face becomes the walkable surface.
    /// </remarks>
    public NavMeshData BuildBox(Vector3 min, Vector3 max)
    {
        // Ensure the box has some height for proper voxelization
        float height = Math.Max(max.Y - min.Y, 1.0f);
        float bottomY = min.Y;
        float topY = min.Y + height;

        // Create a complete box with 6 faces (8 vertices, 12 triangles)
        var vertices = new float[]
        {
            // Bottom face (4 vertices)
            min.X, bottomY, min.Z,  // 0
            max.X, bottomY, min.Z,  // 1
            max.X, bottomY, max.Z,  // 2
            min.X, bottomY, max.Z,  // 3

            // Top face (4 vertices)
            min.X, topY, min.Z,     // 4
            max.X, topY, min.Z,     // 5
            max.X, topY, max.Z,     // 6
            min.X, topY, max.Z      // 7
        };

        var indices = new int[]
        {
            // Bottom face (facing down, CCW when viewed from below)
            0, 2, 1,
            0, 3, 2,

            // Top face (facing up, CCW when viewed from above) - this is the walkable surface
            4, 6, 5,
            4, 7, 6,

            // Front face (facing -Z)
            0, 1, 5,
            0, 5, 4,

            // Back face (facing +Z)
            2, 3, 7,
            2, 7, 6,

            // Left face (facing -X)
            0, 4, 7,
            0, 7, 3,

            // Right face (facing +X)
            1, 2, 6,
            1, 6, 5
        };

        return Build(vertices, indices, null);
    }

    private NavMeshData BuildFromGeometry(IRcInputGeomProvider geom, RcVec3f bmin, RcVec3f bmax, int overrideTileSize)
    {
        return config.UseTiles
            ? BuildTiled(geom, bmin, bmax, overrideTileSize)
            : BuildSingleTile(geom, bmin, bmax);
    }

    private NavMeshData BuildSingleTile(IRcInputGeomProvider geom, RcVec3f bmin, RcVec3f bmax)
    {
        // Create Recast config (tile size is irrelevant when tiling is disabled)
        var rcConfig = CreateRcConfig(config.TileSize);

        // Create builder config
        var bcfg = new RcBuilderConfig(rcConfig, bmin, bmax);

        // Build using RcBuilder
        var result = builder.Build(geom, bcfg, false);

        if (result == null || result.Mesh == null || result.Mesh.nverts == 0)
        {
            throw new InvalidOperationException("Failed to build navigation mesh");
        }

        MarkWalkablePolys(result.Mesh);

        // Create Detour navmesh data from result
        var dtParams = CreateNavMeshParams(result.Mesh, result.MeshDetail, bmin, bmax);
        var meshData = DtNavMeshBuilder.CreateNavMeshData(dtParams);

        if (meshData == null)
        {
            throw new InvalidOperationException("Failed to create navmesh data");
        }

        // Create the navmesh with the single-tile Init overload.
        var navMesh = new DtNavMesh();
        navMesh.Init(meshData, config.MaxVertsPerPoly, 0);

        return new NavMeshData(navMesh, config.ToAgentSettings());
    }

    private NavMeshData BuildTiled(IRcInputGeomProvider geom, RcVec3f bmin, RcVec3f bmax, int overrideTileSize)
    {
        // Effective tile size honours a per-surface override (NavMeshSurface.OverrideTileSize)
        // when positive, otherwise falls back to the global config value.
        int tileSize = overrideTileSize > 0 ? overrideTileSize : config.TileSize;
        var rcConfig = CreateRcConfig(tileSize);

        // Partition the bounds into a grid of tiles: tw x th tiles of tileSize cells each.
        RcRecast.CalcTileCount(bmin, bmax, config.CellSize, rcConfig.TileSizeX, rcConfig.TileSizeZ, out int tw, out int th);

        // Size the navmesh so tile + poly indices fit in the reference bit budget.
        // DotRecast packs the polygon reference as salt|tile|poly; the tile and poly
        // indices together use 22 bits. Allocate as many bits to the tile index as the
        // grid needs (capped at 14 to leave room for salt), and give the rest to polys.
        int tileColumns = Math.Max(1, tw * th);
        int tileBits = Math.Min(BitOperations.Log2(BitOperations.RoundUpToPowerOf2((uint)tileColumns)), 14);
        int polyBits = 22 - tileBits;
        int maxTiles = 1 << tileBits;
        int maxPolysPerTile = 1 << polyBits;

        // Tile footprint in world units.
        float tileWorldSize = rcConfig.TileSizeX * config.CellSize;

        var navMeshParams = new DtNavMeshParams
        {
            orig = bmin,
            tileWidth = tileWorldSize,
            tileHeight = tileWorldSize,
            maxTiles = maxTiles,
            maxPolys = maxPolysPerTile
        };

        var navMesh = new DtNavMesh();
        navMesh.Init(navMeshParams, config.MaxVertsPerPoly);

        // BuildTiles walks the whole tw x th grid and returns one result per tile.
        var results = builder.BuildTiles(geom, rcConfig, false, true);

        int tilesAdded = 0;
        foreach (var result in results)
        {
            // Skip empty tiles (no walkable geometry landed in this cell).
            if (result?.Mesh == null || result.Mesh.npolys == 0)
            {
                continue;
            }

            MarkWalkablePolys(result.Mesh);

            var dtParams = CreateNavMeshParams(
                result.Mesh,
                result.MeshDetail,
                result.Mesh.bmin,
                result.Mesh.bmax,
                result.TileX,
                result.TileZ);

            var meshData = DtNavMeshBuilder.CreateNavMeshData(dtParams);
            if (meshData == null)
            {
                continue;
            }

            navMesh.AddTile(meshData, 0, 0, out _);
            tilesAdded++;
        }

        if (tilesAdded == 0)
        {
            throw new InvalidOperationException("Failed to build navigation mesh");
        }

        return new NavMeshData(navMesh, config.ToAgentSettings());
    }

    /// <summary>
    /// Assigns a walkable polygon flag to every non-null-area polygon so the default
    /// Detour query filter (which requires at least one include flag bit set) can
    /// traverse them.
    /// </summary>
    private static void MarkWalkablePolys(RcPolyMesh mesh)
    {
        for (int i = 0; i < mesh.npolys; i++)
        {
            if (mesh.areas[i] != RcRecast.RC_NULL_AREA)
            {
                mesh.flags[i] = 1;
            }
        }
    }

    private RcConfig CreateRcConfig(int tileSize)
    {
        // RcConfig constructor takes agent parameters in world units.
        // The walkable area modification must resolve to a non-null area
        // (RC_WALKABLE_AREA); mapping it to RC_NULL_AREA (0) would make Recast treat
        // every span as unwalkable and produce an empty mesh.
        return new RcConfig(
            useTiles: config.UseTiles,
            tileSizeX: tileSize,
            tileSizeZ: tileSize,
            borderSize: config.MaxVertsPerPoly + 3,
            partition: RcPartition.WATERSHED,
            cellSize: config.CellSize,
            cellHeight: config.CellHeight,
            agentMaxSlope: config.MaxSlopeAngle,
            agentHeight: config.AgentHeight,
            agentRadius: config.AgentRadius,
            agentMaxClimb: config.MaxClimbHeight,
            minRegionArea: config.MinRegionArea,
            mergeRegionArea: config.MergeRegionArea,
            edgeMaxLen: config.MaxEdgeLength,
            edgeMaxError: config.MaxSimplificationError,
            vertsPerPoly: config.MaxVertsPerPoly,
            detailSampleDist: config.DetailSampleDistance,
            detailSampleMaxError: config.DetailSampleMaxError,
            filterLowHangingObstacles: config.FilterLowHangingObstacles,
            filterLedgeSpans: config.FilterLedgeSpans,
            filterWalkableLowHeightSpans: config.FilterWalkableLowHeightSpans,
            walkableAreaMod: new RcAreaModification(RcRecast.RC_WALKABLE_AREA),
            buildMeshDetail: true);
    }

    private DtNavMeshCreateParams CreateNavMeshParams(RcPolyMesh pmesh, RcPolyMeshDetail? dmesh, RcVec3f bmin, RcVec3f bmax, int tileX = 0, int tileZ = 0)
    {
        var navMeshParams = new DtNavMeshCreateParams
        {
            verts = pmesh.verts,
            vertCount = pmesh.nverts,
            polys = pmesh.polys,
            polyAreas = pmesh.areas,
            polyFlags = pmesh.flags,
            polyCount = pmesh.npolys,
            nvp = pmesh.nvp,
            bmin = bmin,
            bmax = bmax,
            cs = config.CellSize,
            ch = config.CellHeight,
            walkableHeight = config.AgentHeight,
            walkableRadius = config.AgentRadius,
            walkableClimb = config.MaxClimbHeight,
            tileX = tileX,
            tileZ = tileZ,
            buildBvTree = true
        };

        if (dmesh != null)
        {
            navMeshParams.detailMeshes = dmesh.meshes;
            navMeshParams.detailVerts = dmesh.verts;
            navMeshParams.detailVertsCount = dmesh.nverts;
            navMeshParams.detailTris = dmesh.tris;
            navMeshParams.detailTriCount = dmesh.ntris;
        }

        return navMeshParams;
    }
}
