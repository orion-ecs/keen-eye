using System.Numerics;
using BenchmarkDotNet.Attributes;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Benchmarks;

/// <summary>
/// Compares performance of Grid vs Quadtree vs Octree strategies across different entity distributions.
/// Validates Phase 2 requirement: Quadtree/Octree perform better for clustered distributions.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class StrategyComparisonBenchmarks
{
    private World? world;
    private SpatialQueryApi? spatial;

    [Params(SpatialStrategy.Grid, SpatialStrategy.Quadtree, SpatialStrategy.Octree)]
    public SpatialStrategy Strategy { get; set; }

    [Params(10000)]
    public int EntityCount { get; set; }

    [Params("Uniform", "Clustered", "Sparse")]
    public string Distribution { get; set; } = "Uniform";

    [GlobalSetup]
    public void Setup()
    {
        world = new World();

        // Configure strategy
        var config = new SpatialConfig
        {
            Strategy = Strategy,
            Grid = new GridConfig
            {
                CellSize = 100f,
                WorldMin = new Vector3(-5000, -5000, -5000),
                WorldMax = new Vector3(5000, 5000, 5000)
            },
            Quadtree = new QuadtreeConfig
            {
                MaxDepth = 8,
                MaxEntitiesPerNode = 8,
                WorldMin = new Vector3(-5000, 0, -5000),
                WorldMax = new Vector3(5000, 0, 5000)
            },
            Octree = new OctreeConfig
            {
                MaxDepth = 6,
                MaxEntitiesPerNode = 8,
                WorldMin = new Vector3(-5000, -5000, -5000),
                WorldMax = new Vector3(5000, 5000, 5000)
            }
        };

        world.InstallPlugin(new SpatialPlugin(config));
        spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities based on distribution pattern
        switch (Distribution)
        {
            case "Uniform":
                CreateUniformDistribution();
                break;
            case "Clustered":
                CreateClusteredDistribution();
                break;
            case "Sparse":
                CreateSparseDistribution();
                break;
        }

        // Run one update to ensure all entities are indexed
        world.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world?.Dispose();
    }

    /// <summary>
    /// Creates a uniform grid distribution - Grid strategy should excel.
    /// </summary>
    private void CreateUniformDistribution()
    {
        var gridSize = (int)Math.Ceiling(Math.Pow(EntityCount, 1.0 / 3.0)); // Cube root for 3D grid
        var spacing = 50f;

        for (int i = 0; i < EntityCount; i++)
        {
            int x = i % gridSize;
            int y = (i / gridSize) % gridSize;
            int z = i / (gridSize * gridSize);

            world!.Spawn()
                .With(new Transform3D(
                    new Vector3(x * spacing, y * spacing, z * spacing),
                    Quaternion.Identity,
                    Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }
    }

    /// <summary>
    /// Creates clustered distribution - Quadtree/Octree should excel.
    /// </summary>
    private void CreateClusteredDistribution()
    {
        var random = new Random(42); // Fixed seed for consistency
        var clusterCount = 10;
        var entitiesPerCluster = EntityCount / clusterCount;

        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            // Random cluster center
            var centerX = random.Next(-4000, 4000);
            var centerY = random.Next(-4000, 4000);
            var centerZ = random.Next(-4000, 4000);
            var clusterRadius = 200f;

            for (int i = 0; i < entitiesPerCluster; i++)
            {
                // Random offset within cluster
                var offsetX = (float)(random.NextDouble() * 2 - 1) * clusterRadius;
                var offsetY = (float)(random.NextDouble() * 2 - 1) * clusterRadius;
                var offsetZ = (float)(random.NextDouble() * 2 - 1) * clusterRadius;

                world!.Spawn()
                    .With(new Transform3D(
                        new Vector3(centerX + offsetX, centerY + offsetY, centerZ + offsetZ),
                        Quaternion.Identity,
                        Vector3.One))
                    .WithTag<SpatialIndexed>()
                    .Build();
            }
        }
    }

    /// <summary>
    /// Creates sparse distribution with entities far apart.
    /// </summary>
    private void CreateSparseDistribution()
    {
        var random = new Random(42);

        for (int i = 0; i < EntityCount; i++)
        {
            var x = random.Next(-4500, 4500);
            var y = random.Next(-4500, 4500);
            var z = random.Next(-4500, 4500);

            world!.Spawn()
                .With(new Transform3D(
                    new Vector3(x, y, z),
                    Quaternion.Identity,
                    Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }
    }

    /// <summary>
    /// Small radius query - should be fast for all strategies.
    /// </summary>
    [Benchmark]
    public int QueryRadius_Small()
    {
        var radius = 150f;
        var center = new Vector3(500, 500, 500);

        var count = 0;
        foreach (var _ in spatial!.QueryRadius(center, radius))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Medium radius query - tests typical usage.
    /// </summary>
    [Benchmark]
    public int QueryRadius_Medium()
    {
        var radius = 500f;
        var center = new Vector3(500, 500, 500);

        var count = 0;
        foreach (var _ in spatial!.QueryRadius(center, radius))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Large radius query - stress test.
    /// </summary>
    [Benchmark]
    public int QueryRadius_Large()
    {
        var radius = 2000f;
        var center = new Vector3(0, 0, 0);

        var count = 0;
        foreach (var _ in spatial!.QueryRadius(center, radius))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Multiple small queries - simulates collision detection.
    /// Critical for game performance.
    /// </summary>
    [Benchmark]
    public int MultipleSmallQueries()
    {
        var totalCount = 0;
        var radius = 100f;

        // Perform 50 queries at different positions
        for (int i = 0; i < 50; i++)
        {
            var x = (i % 5) * 500f;
            var y = (i / 5 % 5) * 500f;
            var z = (i / 25f) * 500f;
            var position = new Vector3(x, y, z);

            foreach (var _ in spatial!.QueryRadius(position, radius))
            {
                totalCount++;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// Bounds query - tests rectangular region queries.
    /// </summary>
    [Benchmark]
    public int QueryBounds_MediumBox()
    {
        var min = new Vector3(400, 400, 400);
        var max = new Vector3(800, 800, 800);

        var count = 0;
        foreach (var _ in spatial!.QueryBounds(min, max))
        {
            count++;
        }
        return count;
    }
}
