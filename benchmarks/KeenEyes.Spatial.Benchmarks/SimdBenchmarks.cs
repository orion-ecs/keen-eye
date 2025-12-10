using System.Numerics;
using BenchmarkDotNet.Attributes;
using KeenEyes.Spatial;

namespace KeenEyes.Spatial.Benchmarks;

/// <summary>
/// Benchmarks comparing SIMD vs scalar implementations of spatial operations.
/// Validates Phase 3 requirement: SIMD provides 2-4x speedup for bulk operations.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class SimdBenchmarks
{
    private Vector3[] positions = [];
    private Vector3 queryCenter;
    private Vector3 aabbMin;
    private Vector3 aabbMax;
    private float radiusSquared;

    [Params(100, 1000, 10000)]
    public int EntityCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        positions = new Vector3[EntityCount];

        // Create random positions in clustered distribution
        var clusterCount = 10;
        var entitiesPerCluster = EntityCount / clusterCount;

        int entityIndex = 0;
        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            var centerX = random.Next(-4000, 4000);
            var centerY = random.Next(-4000, 4000);
            var centerZ = random.Next(-4000, 4000);
            var clusterRadius = 200f;

            for (int i = 0; i < entitiesPerCluster && entityIndex < EntityCount; i++)
            {
                var offsetX = (float)(random.NextDouble() * 2 - 1) * clusterRadius;
                var offsetY = (float)(random.NextDouble() * 2 - 1) * clusterRadius;
                var offsetZ = (float)(random.NextDouble() * 2 - 1) * clusterRadius;

                positions[entityIndex++] = new Vector3(
                    centerX + offsetX,
                    centerY + offsetY,
                    centerZ + offsetZ);
            }
        }

        queryCenter = new Vector3(500, 500, 500);
        radiusSquared = 500f * 500f;
        aabbMin = new Vector3(400, 400, 400);
        aabbMax = new Vector3(800, 800, 800);
    }

    #region Distance Filtering Benchmarks

    [Benchmark(Baseline = true)]
    public int DistanceFilter_Scalar()
    {
        var results = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            var distSq = Vector3.DistanceSquared(positions[i], queryCenter);
            if (distSq <= radiusSquared)
            {
                results.Add(i);
            }
        }
        return results.Count;
    }

    [Benchmark]
    public int DistanceFilter_SIMD()
    {
        Span<int> results = stackalloc int[1000];
        int count = SimdHelpers.FilterByDistanceSIMD(positions, queryCenter, radiusSquared, results);
        return count;
    }

    #endregion

    #region AABB Filtering Benchmarks

    [Benchmark]
    public int AABBFilter_Scalar()
    {
        var results = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (pos.X >= aabbMin.X && pos.X <= aabbMax.X &&
                pos.Y >= aabbMin.Y && pos.Y <= aabbMax.Y &&
                pos.Z >= aabbMin.Z && pos.Z <= aabbMax.Z)
            {
                results.Add(i);
            }
        }
        return results.Count;
    }

    [Benchmark]
    public int AABBFilter_SIMD()
    {
        Span<int> results = stackalloc int[1000];
        int count = SimdHelpers.FilterByAABBSIMD(positions, aabbMin, aabbMax, results);
        return count;
    }

    #endregion

    #region Multiple Query Benchmarks

    [Benchmark]
    public int MultipleDistanceQueries_Scalar()
    {
        int totalCount = 0;
        var results = new List<int>();

        // Perform 10 queries at different positions
        for (int q = 0; q < 10; q++)
        {
            var center = new Vector3(q * 200f, q * 200f, q * 200f);
            results.Clear();

            for (int i = 0; i < positions.Length; i++)
            {
                var distSq = Vector3.DistanceSquared(positions[i], center);
                if (distSq <= radiusSquared)
                {
                    results.Add(i);
                }
            }

            totalCount += results.Count;
        }

        return totalCount;
    }

    [Benchmark]
    public int MultipleDistanceQueries_SIMD()
    {
        int totalCount = 0;
        Span<int> results = stackalloc int[1000];

        // Perform 10 queries at different positions
        for (int q = 0; q < 10; q++)
        {
            var center = new Vector3(q * 200f, q * 200f, q * 200f);

            int count = SimdHelpers.FilterByDistanceSIMD(positions, center, radiusSquared, results);
            totalCount += count;
        }

        return totalCount;
    }

    #endregion
}
