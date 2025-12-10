using System.Numerics;
using BenchmarkDotNet.Attributes;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Benchmarks;

/// <summary>
/// Benchmarks for node pooling in quadtree and octree partitioners.
/// Measures the performance impact of reusing node arrays vs allocating new ones.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class NodePoolingBenchmarks
{
    private const int EntityCount = 1000;


    #region Quadtree Pooling Benchmarks

    [Benchmark(Baseline = true, Description = "Quadtree with pooling")]
    public void Quadtree_WithPooling()
    {
        // Perform 10 subdivision cycles (create world, add entities, dispose)
        for (int cycle = 0; cycle < 10; cycle++)
        {
            using var world = new World();
            world.InstallPlugin(new SpatialPlugin(new SpatialConfig
            {
                Strategy = SpatialStrategy.Quadtree,
                Quadtree = new QuadtreeConfig
                {
                    WorldMin = new Vector3(-1000, 0, -1000),
                    WorldMax = new Vector3(1000, 0, 1000),
                    MaxDepth = 8,
                    MaxEntitiesPerNode = 4,
                    UseNodePooling = true
                }
            }));

            var spatial = world.GetExtension<SpatialQueryApi>();

            // Add entities to trigger subdivision
            for (int i = 0; i < EntityCount; i++)
            {
                var pos = new Vector3(i * 2, 0, i * 2);
                world.Spawn()
                    .With(new Transform3D(pos, Quaternion.Identity, Vector3.One))
                    .WithTag<SpatialIndexed>()
                    .Build();
            }

            // Update to index entities
            world.Update(0.016f);

            // Query to verify correctness
            _ = spatial.QueryBounds(new Vector3(-100, 0, -100), new Vector3(2100, 0, 2100)).ToList();
        }
    }

    [Benchmark(Description = "Quadtree without pooling")]
    public void Quadtree_WithoutPooling()
    {
        // Perform 10 subdivision cycles (create world, add entities, dispose)
        for (int cycle = 0; cycle < 10; cycle++)
        {
            using var world = new World();
            world.InstallPlugin(new SpatialPlugin(new SpatialConfig
            {
                Strategy = SpatialStrategy.Quadtree,
                Quadtree = new QuadtreeConfig
                {
                    WorldMin = new Vector3(-1000, 0, -1000),
                    WorldMax = new Vector3(1000, 0, 1000),
                    MaxDepth = 8,
                    MaxEntitiesPerNode = 4,
                    UseNodePooling = false
                }
            }));

            var spatial = world.GetExtension<SpatialQueryApi>();

            // Add entities to trigger subdivision
            for (int i = 0; i < EntityCount; i++)
            {
                var pos = new Vector3(i * 2, 0, i * 2);
                world.Spawn()
                    .With(new Transform3D(pos, Quaternion.Identity, Vector3.One))
                    .WithTag<SpatialIndexed>()
                    .Build();
            }

            // Update to index entities
            world.Update(0.016f);

            // Query to verify correctness
            _ = spatial.QueryBounds(new Vector3(-100, 0, -100), new Vector3(2100, 0, 2100)).ToList();
        }
    }

    #endregion

    #region Octree Pooling Benchmarks

    [Benchmark(Description = "Octree with pooling")]
    public void Octree_WithPooling()
    {
        // Perform 10 subdivision cycles (create world, add entities, dispose)
        for (int cycle = 0; cycle < 10; cycle++)
        {
            using var world = new World();
            world.InstallPlugin(new SpatialPlugin(new SpatialConfig
            {
                Strategy = SpatialStrategy.Octree,
                Octree = new OctreeConfig
                {
                    WorldMin = new Vector3(-1000, -1000, -1000),
                    WorldMax = new Vector3(1000, 1000, 1000),
                    MaxDepth = 6,
                    MaxEntitiesPerNode = 4,
                    UseNodePooling = true
                }
            }));

            var spatial = world.GetExtension<SpatialQueryApi>();

            // Add entities to trigger subdivision
            for (int i = 0; i < EntityCount; i++)
            {
                var pos = new Vector3(i * 2, i * 2, i * 2);
                world.Spawn()
                    .With(new Transform3D(pos, Quaternion.Identity, Vector3.One))
                    .WithTag<SpatialIndexed>()
                    .Build();
            }

            // Update to index entities
            world.Update(0.016f);

            // Query to verify correctness
            _ = spatial.QueryBounds(new Vector3(-100, -100, -100), new Vector3(2100, 2100, 2100)).ToList();
        }
    }

    [Benchmark(Description = "Octree without pooling")]
    public void Octree_WithoutPooling()
    {
        // Perform 10 subdivision cycles (create world, add entities, dispose)
        for (int cycle = 0; cycle < 10; cycle++)
        {
            using var world = new World();
            world.InstallPlugin(new SpatialPlugin(new SpatialConfig
            {
                Strategy = SpatialStrategy.Octree,
                Octree = new OctreeConfig
                {
                    WorldMin = new Vector3(-1000, -1000, -1000),
                    WorldMax = new Vector3(1000, 1000, 1000),
                    MaxDepth = 6,
                    MaxEntitiesPerNode = 4,
                    UseNodePooling = false
                }
            }));

            var spatial = world.GetExtension<SpatialQueryApi>();

            // Add entities to trigger subdivision
            for (int i = 0; i < EntityCount; i++)
            {
                var pos = new Vector3(i * 2, i * 2, i * 2);
                world.Spawn()
                    .With(new Transform3D(pos, Quaternion.Identity, Vector3.One))
                    .WithTag<SpatialIndexed>()
                    .Build();
            }

            // Update to index entities
            world.Update(0.016f);

            // Query to verify correctness
            _ = spatial.QueryBounds(new Vector3(-100, -100, -100), new Vector3(2100, 2100, 2100)).ToList();
        }
    }

    #endregion
}
