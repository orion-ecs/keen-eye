using BenchmarkDotNet.Attributes;
using KeenEyes.Common;
using System.Numerics;

namespace KeenEyes.Spatial.Benchmarks;

/// <summary>
/// Benchmarks for spatial partitioning queries and updates.
/// Verifies Phase 1 requirement: 10,000 entities with &lt;1ms query time.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SpatialBenchmarks
{
    private World world = null!;
    private SpatialQueryApi spatial = null!;

    [Params(1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin(new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = 100f,
                WorldMin = new Vector3(-5000, -500, -5000),
                WorldMax = new Vector3(5000, 500, 5000)
            }
        }));

        spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities in a grid pattern spanning the world
        // This creates a realistic spatial distribution
        var gridSize = (int)Math.Sqrt(EntityCount);
        var spacing = 50f; // 50 units between entities

        for (int i = 0; i < EntityCount; i++)
        {
            int x = (i % gridSize);
            int z = (i / gridSize);

            world.Spawn()
                .With(new Transform3D(
                    new Vector3(x * spacing, 0, z * spacing),
                    Quaternion.Identity,
                    Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }

        // Run one update to ensure all entities are indexed
        world.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        world.Dispose();
    }

    /// <summary>
    /// Measures QueryRadius performance - should be &lt;1ms for 10,000 entities.
    /// This is the primary Phase 1 success criterion.
    /// </summary>
    [Benchmark]
    public int QueryRadius_NearbyEntities()
    {
        // Query center of the world with radius that captures ~1-5% of entities
        var radius = 150f;
        var center = new Vector3(500, 0, 500);

        var count = 0;
        foreach (var entity in spatial.QueryRadius(center, radius))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures QueryBounds performance with a medium-sized bounding box.
    /// </summary>
    [Benchmark]
    public int QueryBounds_MediumBox()
    {
        // Bounds that captures ~5-10% of entities
        var min = new Vector3(400, -100, 400);
        var max = new Vector3(600, 100, 600);

        var count = 0;
        foreach (var entity in spatial.QueryBounds(min, max))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures QueryPoint performance (finds entities in the cell containing the point).
    /// </summary>
    [Benchmark]
    public int QueryPoint_SingleCell()
    {
        var point = new Vector3(500, 0, 500);

        var count = 0;
        foreach (var entity in spatial.QueryPoint(point))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures update performance when entities move.
    /// Tests ChangeTracker integration - only moved entities should be updated.
    /// </summary>
    [Benchmark]
    public void Update_MovingEntities()
    {
        // Move 10% of entities
        var movedCount = EntityCount / 10;
        var entities = world.Query<Transform3D, SpatialIndexed>().ToList();

        for (int i = 0; i < movedCount && i < entities.Count; i++)
        {
            var entity = entities[i];
            ref var transform = ref world.Get<Transform3D>(entity);
            transform.Position += new Vector3(5, 0, 0);
        }

        // Update should only process the moved entities
        world.Update(0.016f);
    }

    /// <summary>
    /// Measures large radius query that returns many results.
    /// Stress test for query performance.
    /// </summary>
    [Benchmark]
    public int QueryRadius_LargeRadius()
    {
        // Large radius that captures ~25-50% of entities
        var radius = 500f;
        var center = new Vector3(500, 0, 500);

        var count = 0;
        foreach (var entity in spatial.QueryRadius(center, radius))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Measures performance of multiple small queries in succession.
    /// Simulates typical usage pattern (e.g., collision detection for many entities).
    /// </summary>
    [Benchmark]
    public int MultipleSmallQueries()
    {
        var totalCount = 0;
        var radius = 100f;

        // Perform 100 small queries at different positions
        for (int i = 0; i < 100; i++)
        {
            var x = (i % 10) * 100f;
            var z = (i / 10) * 100f;
            var position = new Vector3(x, 0, z);

            foreach (var entity in spatial.QueryRadius(position, radius))
            {
                totalCount++;
            }
        }

        return totalCount;
    }
}
