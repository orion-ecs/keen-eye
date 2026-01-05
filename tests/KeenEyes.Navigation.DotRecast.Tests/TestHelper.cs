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
}
