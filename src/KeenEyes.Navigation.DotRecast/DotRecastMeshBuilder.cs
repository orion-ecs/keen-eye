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
/// For large worlds, use tiled building with the <see cref="NavMeshConfig.UseTiles"/>
/// option to enable runtime updates and streaming.
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
    /// <returns>The built navigation mesh data.</returns>
    /// <exception cref="ArgumentException">Thrown when geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when mesh building fails.</exception>
    public NavMeshData Build(ReadOnlySpan<float> vertices, ReadOnlySpan<int> indices, int[]? areaIds = null)
    {
        if (vertices.Length == 0 || vertices.Length % 3 != 0)
        {
            throw new ArgumentException("Vertices must be XYZ triplets", nameof(vertices));
        }

        if (indices.Length == 0 || indices.Length % 3 != 0)
        {
            throw new ArgumentException("Indices must be triangle triplets", nameof(indices));
        }

        // Convert to arrays for DotRecast
        var vertsArray = vertices.ToArray();
        var trisArray = indices.ToArray();

        // Create geometry provider
        var geom = new SimpleInputGeomProvider(vertsArray, trisArray);

        // Compute bounds
        var bmin = geom.GetMeshBoundsMin();
        var bmax = geom.GetMeshBoundsMax();

        // Build the navmesh
        // areaIds reserved for future area marking support
        _ = areaIds;
        return BuildFromGeometry(geom, bmin, bmax);
    }

    /// <summary>
    /// Builds a navigation mesh from Vector3 vertices and triangle indices.
    /// </summary>
    /// <param name="vertices">The mesh vertices.</param>
    /// <param name="indices">The triangle indices.</param>
    /// <param name="areaIds">Optional per-triangle area IDs.</param>
    /// <returns>The built navigation mesh data.</returns>
    public NavMeshData Build(ReadOnlySpan<Vector3> vertices, ReadOnlySpan<int> indices, int[]? areaIds = null)
    {
        // Convert Vector3 to float triplets
        var floatVerts = new float[vertices.Length * 3];
        for (int i = 0; i < vertices.Length; i++)
        {
            floatVerts[i * 3] = vertices[i].X;
            floatVerts[i * 3 + 1] = vertices[i].Y;
            floatVerts[i * 3 + 2] = vertices[i].Z;
        }

        return Build(floatVerts, indices, areaIds);
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
            4, 5, 6,
            4, 6, 7,

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

    private NavMeshData BuildFromGeometry(IInputGeomProvider geom, RcVec3f bmin, RcVec3f bmax)
    {
        // Create Recast config
        var rcConfig = CreateRcConfig();

        // Create builder config
        var bcfg = new RcBuilderConfig(rcConfig, bmin, bmax);

        // Build using RcBuilder
        var result = builder.Build(geom, bcfg, false);

        if (result == null || result.Mesh == null || result.Mesh.nverts == 0)
        {
            throw new InvalidOperationException("Failed to build navigation mesh");
        }

        // Create Detour navmesh data from result
        var dtParams = CreateNavMeshParams(result.Mesh, result.MeshDetail, bmin, bmax);
        var meshData = DtNavMeshBuilder.CreateNavMeshData(dtParams);

        if (meshData == null)
        {
            throw new InvalidOperationException("Failed to create navmesh data");
        }

        // Create the navmesh
        var navMesh = new DtNavMesh();
        navMesh.Init(meshData, config.MaxVertsPerPoly, 0);

        return new NavMeshData(navMesh, config.ToAgentSettings());
    }

    private RcConfig CreateRcConfig()
    {
        // RcConfig constructor takes agent parameters in world units
        return new RcConfig(
            useTiles: config.UseTiles,
            tileSizeX: config.TileSize,
            tileSizeZ: config.TileSize,
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
            walkableAreaMod: new RcAreaModification(0, 0x3f),
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
