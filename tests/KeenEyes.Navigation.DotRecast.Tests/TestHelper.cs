using System.Collections.Generic;
using System.Numerics;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Helper methods and configurations for testing.
/// </summary>
internal static class TestHelper
{
    /// <summary>
    /// Creates a NavMeshConfig suitable for test geometry.
    /// </summary>
    /// <remarks>
    /// The default NavMeshConfig is designed for typical game worlds.
    /// For test geometry, we use settings that work reliably with small boxes.
    /// </remarks>
    public static NavMeshConfig CreateTestConfig()
    {
        return new NavMeshConfig
        {
            // Cell sizes appropriate for test geometry
            CellSize = 0.3f,
            CellHeight = 0.2f,

            // Agent dimensions
            AgentHeight = 2.0f,
            AgentRadius = 0.6f,
            MaxClimbHeight = 0.9f,
            MaxSlopeAngle = 45.0f,

            // Region thresholds
            MinRegionArea = 8,
            MergeRegionArea = 20,

            // Standard values
            MaxEdgeLength = 12,
            MaxSimplificationError = 1.3f,
            MaxVertsPerPoly = 6,
            DetailSampleDistance = 6.0f,
            DetailSampleMaxError = 1.0f,

            // Disable tiles for simple test cases
            UseTiles = false,
            TileSize = 48,

            // Filtering
            FilterLowHangingObstacles = true,
            FilterLedgeSpans = true,
            FilterWalkableLowHeightSpans = true,

            // Async settings
            MaxPendingRequests = 10,
            RequestsPerUpdate = 5
        };
    }

    /// <summary>
    /// Creates a DotRecastMeshBuilder configured for testing.
    /// </summary>
    public static DotRecastMeshBuilder CreateTestBuilder()
    {
        return new DotRecastMeshBuilder(CreateTestConfig());
    }

    /// <summary>
    /// Builds a test navmesh suitable for most tests.
    /// Creates a large flat terrain mesh for reliable navmesh generation.
    /// </summary>
    public static NavMeshData BuildTestNavMesh()
    {
        var builder = CreateTestBuilder();
        // Build a large terrain mesh at ground level
        return BuildLargeFlatMesh(builder, 200f, 200f);
    }

    /// <summary>
    /// Creates a large flat mesh with proper triangle density for Recast.
    /// </summary>
    private static NavMeshData BuildLargeFlatMesh(DotRecastMeshBuilder builder, float width, float depth)
    {
        // Create a grid of triangles - this gives Recast proper geometry to work with
        int gridSize = 10;
        float cellWidth = width / gridSize;
        float cellDepth = depth / gridSize;

        // Generate vertices for grid
        var vertices = new List<float>();
        for (int z = 0; z <= gridSize; z++)
        {
            for (int x = 0; x <= gridSize; x++)
            {
                vertices.Add(x * cellWidth);
                vertices.Add(0f);  // Y = 0 (ground level)
                vertices.Add(z * cellDepth);
            }
        }

        // Generate indices for triangles
        var indices = new List<int>();
        for (int z = 0; z < gridSize; z++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                int topLeft = z * (gridSize + 1) + x;
                int topRight = topLeft + 1;
                int bottomLeft = (z + 1) * (gridSize + 1) + x;
                int bottomRight = bottomLeft + 1;

                // First triangle
                indices.Add(topLeft);
                indices.Add(bottomLeft);
                indices.Add(topRight);

                // Second triangle
                indices.Add(topRight);
                indices.Add(bottomLeft);
                indices.Add(bottomRight);
            }
        }

        return builder.Build(vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Creates a NavMeshConfig with tiled building enabled for tiled-build tests.
    /// </summary>
    /// <param name="tileSize">The tile size in cells.</param>
    public static NavMeshConfig CreateTiledTestConfig(int tileSize = 48)
    {
        var config = CreateTestConfig();
        config.UseTiles = true;
        config.TileSize = tileSize;
        return config;
    }

    /// <summary>
    /// Builds a flat square slab (a thick floor) centered on the origin plane at Y=0.
    /// </summary>
    /// <remarks>
    /// Unlike a zero-thickness plane, the slab spans a small vertical extent so Recast
    /// can voxelize it into walkable spans. The top face (Y=0) is wound so its normal
    /// points up, making it the walkable surface. A large slab spans multiple tiles.
    /// </remarks>
    /// <param name="size">The side length of the slab in world units.</param>
    /// <returns>The vertices (XYZ triplets) and triangle indices of the slab.</returns>
    public static (float[] Vertices, int[] Indices) BuildSlabGeometry(float size)
    {
        const float top = 0f;
        const float bottom = -1f;

        var vertices = new[]
        {
            // Bottom face
            0f, bottom, 0f,
            size, bottom, 0f,
            size, bottom, size,
            0f, bottom, size,

            // Top face (walkable surface)
            0f, top, 0f,
            size, top, 0f,
            size, top, size,
            0f, top, size
        };

        var indices = new[]
        {
            // Bottom (normal down)
            0, 2, 1,
            0, 3, 2,

            // Top (normal up - walkable)
            4, 6, 5,
            4, 7, 6,

            // Sides
            0, 1, 5,
            0, 5, 4,
            2, 3, 7,
            2, 7, 6,
            0, 4, 7,
            0, 7, 3,
            1, 2, 6,
            1, 6, 5
        };

        return (vertices, indices);
    }
}
