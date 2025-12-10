using System.Numerics;
using KeenEyes.Spatial;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for SIMD-optimized spatial operations.
/// </summary>
public class SimdHelpersTests
{
    [Fact]
    public void FilterByDistanceSIMD_WithNoMatches_ReturnsEmpty()
    {
        var positions = new Vector3[]
        {
            new(100, 0, 100),
            new(200, 0, 200),
            new(300, 0, 300)
        };

        var results = new List<int>();
        SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, 10f * 10f, results);

        Assert.Empty(results);
    }

    [Fact]
    public void FilterByDistanceSIMD_WithAllMatches_ReturnsAll()
    {
        var positions = new Vector3[]
        {
            new(1, 0, 1),
            new(2, 0, 2),
            new(3, 0, 3),
            new(4, 0, 4)
        };

        var results = new List<int>();
        SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, 100f * 100f, results);

        Assert.Equal(4, results.Count);
        Assert.Contains(0, results);
        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
    }

    [Fact]
    public void FilterByDistanceSIMD_WithSomeMatches_ReturnsCorrectIndices()
    {
        var positions = new Vector3[]
        {
            new(5, 0, 0),    // Within range
            new(50, 0, 0),   // Outside range
            new(3, 0, 4),    // Within range (distance = 5)
            new(100, 0, 100) // Outside range
        };

        var results = new List<int>();
        var radiusSquared = 10f * 10f; // radius = 10
        SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

        Assert.Equal(2, results.Count);
        Assert.Contains(0, results); // (5,0,0) is within range
        Assert.Contains(2, results); // (3,0,4) is within range
    }

    [Fact]
    public void FilterByDistanceSIMD_WithManyPositions_ProcessesCorrectly()
    {
        // Test with more than 4 positions to ensure SIMD batch processing works
        var positions = new Vector3[20];
        for (int i = 0; i < 20; i++)
        {
            positions[i] = new Vector3(i * 2, 0, 0);
        }

        var results = new List<int>();
        var radiusSquared = 15f * 15f;
        SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

        // Should match indices 0-7 (positions 0, 2, 4, 6, 8, 10, 12, 14 are within radius 15)
        Assert.Equal(8, results.Count);
        for (int i = 0; i < 8; i++)
        {
            Assert.Contains(i, results);
        }
    }

    [Fact]
    public void FilterByAABBSIMD_WithNoMatches_ReturnsEmpty()
    {
        var positions = new Vector3[]
        {
            new(100, 100, 100),
            new(200, 200, 200),
            new(300, 300, 300)
        };

        var results = new List<int>();
        SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(-10, -10, -10),
            new Vector3(10, 10, 10),
            results);

        Assert.Empty(results);
    }

    [Fact]
    public void FilterByAABBSIMD_WithAllMatches_ReturnsAll()
    {
        var positions = new Vector3[]
        {
            new(1, 2, 3),
            new(4, 5, 6),
            new(7, 8, 9),
            new(2, 3, 4)
        };

        var results = new List<int>();
        SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(4, results.Count);
        Assert.Contains(0, results);
        Assert.Contains(1, results);
        Assert.Contains(2, results);
        Assert.Contains(3, results);
    }

    [Fact]
    public void FilterByAABBSIMD_WithSomeMatches_ReturnsCorrectIndices()
    {
        var positions = new Vector3[]
        {
            new(5, 5, 5),      // Inside
            new(15, 15, 15),   // Outside
            new(0, 0, 0),      // On boundary (min)
            new(10, 10, 10),   // On boundary (max)
            new(-5, 5, 5),     // Outside (X too small)
            new(5, 5, 15)      // Outside (Z too large)
        };

        var results = new List<int>();
        SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(3, results.Count);
        Assert.Contains(0, results); // (5,5,5) inside
        Assert.Contains(2, results); // (0,0,0) on min boundary
        Assert.Contains(3, results); // (10,10,10) on max boundary
    }

    [Fact]
    public void FilterByAABBSIMD_WithManyPositions_ProcessesCorrectly()
    {
        // Test with more than 4 positions
        var positions = new Vector3[20];
        for (int i = 0; i < 20; i++)
        {
            positions[i] = new Vector3(i, i, i);
        }

        var results = new List<int>();
        SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(5, 5, 5),
            new Vector3(15, 15, 15),
            results);

        // Should match indices 5-15 (positions where all coords are between 5 and 15)
        Assert.Equal(11, results.Count);
        for (int i = 5; i <= 15; i++)
        {
            Assert.Contains(i, results);
        }
    }

    [Fact]
    public void FilterByDistanceSIMD_MatchesScalarImplementation()
    {
        var random = new Random(42);
        var positions = new Vector3[100];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = new Vector3(
                random.Next(-500, 500),
                random.Next(-500, 500),
                random.Next(-500, 500));
        }

        var center = new Vector3(50, 50, 50);
        var radiusSquared = 100f * 100f;

        // Get SIMD results
        var simdResults = new List<int>();
        SimdHelpers.FilterByDistanceSIMD(positions, center, radiusSquared, simdResults);

        // Get scalar results (reference implementation)
        var scalarResults = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            if (Vector3.DistanceSquared(positions[i], center) <= radiusSquared)
            {
                scalarResults.Add(i);
            }
        }

        // Results should match
        Assert.Equal(scalarResults.Count, simdResults.Count);
        foreach (var index in scalarResults)
        {
            Assert.Contains(index, simdResults);
        }
    }

    [Fact]
    public void FilterByAABBSIMD_MatchesScalarImplementation()
    {
        var random = new Random(42);
        var positions = new Vector3[100];
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i] = new Vector3(
                random.Next(-500, 500),
                random.Next(-500, 500),
                random.Next(-500, 500));
        }

        var min = new Vector3(-100, -100, -100);
        var max = new Vector3(100, 100, 100);

        // Get SIMD results
        var simdResults = new List<int>();
        SimdHelpers.FilterByAABBSIMD(positions, min, max, simdResults);

        // Get scalar results (reference implementation)
        var scalarResults = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            var pos = positions[i];
            if (pos.X >= min.X && pos.X <= max.X &&
                pos.Y >= min.Y && pos.Y <= max.Y &&
                pos.Z >= min.Z && pos.Z <= max.Z)
            {
                scalarResults.Add(i);
            }
        }

        // Results should match
        Assert.Equal(scalarResults.Count, simdResults.Count);
        foreach (var index in scalarResults)
        {
            Assert.Contains(index, simdResults);
        }
    }
}
