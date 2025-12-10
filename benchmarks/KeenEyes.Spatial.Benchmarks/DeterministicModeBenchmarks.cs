using System.Numerics;
using BenchmarkDotNet.Attributes;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Benchmarks;

/// <summary>
/// Benchmarks comparing query performance with and without deterministic mode.
/// Measures the overhead of stable sorting in query results.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 5)]
public class DeterministicModeBenchmarks
{
    private const int EntityCount = 1000;
    private World? worldDeterministic;
    private World? worldNonDeterministic;
    private SpatialQueryApi? spatialDeterministic;
    private SpatialQueryApi? spatialNonDeterministic;

    [Params(SpatialStrategy.Grid, SpatialStrategy.Quadtree, SpatialStrategy.Octree)]
    public SpatialStrategy Strategy { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        worldDeterministic = new World();
        worldNonDeterministic = new World();

        // Configure deterministic world
        var configDeterministic = new SpatialConfig
        {
            Strategy = Strategy,
            Grid = new GridConfig
            {
                CellSize = 50.0f,
                DeterministicMode = true
            },
            Quadtree = new QuadtreeConfig
            {
                WorldMin = new Vector3(-5000, 0, -5000),
                WorldMax = new Vector3(5000, 0, 5000),
                MaxDepth = 8,
                MaxEntitiesPerNode = 8,
                DeterministicMode = true
            },
            Octree = new OctreeConfig
            {
                WorldMin = new Vector3(-5000, -5000, -5000),
                WorldMax = new Vector3(5000, 5000, 5000),
                MaxDepth = 8,
                MaxEntitiesPerNode = 8,
                DeterministicMode = true
            }
        };

        // Configure non-deterministic world
        var configNonDeterministic = new SpatialConfig
        {
            Strategy = Strategy,
            Grid = new GridConfig
            {
                CellSize = 50.0f,
                DeterministicMode = false
            },
            Quadtree = new QuadtreeConfig
            {
                WorldMin = new Vector3(-5000, 0, -5000),
                WorldMax = new Vector3(5000, 0, 5000),
                MaxDepth = 8,
                MaxEntitiesPerNode = 8,
                DeterministicMode = false
            },
            Octree = new OctreeConfig
            {
                WorldMin = new Vector3(-5000, -5000, -5000),
                WorldMax = new Vector3(5000, 5000, 5000),
                MaxDepth = 8,
                MaxEntitiesPerNode = 8,
                DeterministicMode = false
            }
        };

        worldDeterministic.InstallPlugin(new SpatialPlugin(configDeterministic));
        worldNonDeterministic.InstallPlugin(new SpatialPlugin(configNonDeterministic));

        spatialDeterministic = worldDeterministic.GetExtension<SpatialQueryApi>();
        spatialNonDeterministic = worldNonDeterministic.GetExtension<SpatialQueryApi>();

        // Populate both worlds with the same entities
        var random = new Random(42);
        for (int i = 0; i < EntityCount; i++)
        {
            var position = new Vector3(
                random.Next(-1000, 1000),
                random.Next(-1000, 1000),
                random.Next(-1000, 1000));

            worldDeterministic.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .Build();

            worldNonDeterministic.Spawn()
                .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
                .Build();
        }

        // Index all entities
        worldDeterministic.Update(0.016f);
        worldNonDeterministic.Update(0.016f);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        worldDeterministic?.Dispose();
        worldNonDeterministic?.Dispose();
    }

    [Benchmark(Baseline = true, Description = "QueryBounds (Non-Deterministic)")]
    public int QueryBounds_NonDeterministic()
    {
        return spatialNonDeterministic!.QueryBounds(
            new Vector3(-200, -200, -200),
            new Vector3(200, 200, 200)).Count();
    }

    [Benchmark(Description = "QueryBounds (Deterministic)")]
    public int QueryBounds_Deterministic()
    {
        return spatialDeterministic!.QueryBounds(
            new Vector3(-200, -200, -200),
            new Vector3(200, 200, 200)).Count();
    }

    [Benchmark(Description = "QueryRadius (Non-Deterministic)")]
    public int QueryRadius_NonDeterministic()
    {
        return spatialNonDeterministic!.QueryRadius(Vector3.Zero, 200.0f).Count();
    }

    [Benchmark(Description = "QueryRadius (Deterministic)")]
    public int QueryRadius_Deterministic()
    {
        return spatialDeterministic!.QueryRadius(Vector3.Zero, 200.0f).Count();
    }

    [Benchmark(Description = "QueryPoint (Non-Deterministic)")]
    public int QueryPoint_NonDeterministic()
    {
        return spatialNonDeterministic!.QueryPoint(Vector3.Zero).Count();
    }

    [Benchmark(Description = "QueryPoint (Deterministic)")]
    public int QueryPoint_Deterministic()
    {
        return spatialDeterministic!.QueryPoint(Vector3.Zero).Count();
    }
}
