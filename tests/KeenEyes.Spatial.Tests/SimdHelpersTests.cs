using System.Numerics;
using KeenEyes.Spatial;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for SIMD-optimized spatial operations.
/// </summary>
public class SimdHelpersTests
{
    [Fact]
    public void IsHardwareAccelerated_ReturnsBoolean()
    {
        // Simply verify the property is accessible
        // On modern x64 hardware, this should be true
        _ = SimdHelpers.IsHardwareAccelerated;
        Assert.True(true); // Property accessed successfully
    }

    [Fact]
    public void FilterByDistanceSIMD_WithNoMatches_ReturnsEmpty()
    {
        var positions = new Vector3[]
        {
            new(100, 0, 100),
            new(200, 0, 200),
            new(300, 0, 300)
        };

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, 10f * 10f, results);

        Assert.Equal(0, count);
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

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, 100f * 100f, results);

        Assert.Equal(4, count);
        Assert.Contains(0, results[..count].ToArray());
        Assert.Contains(1, results[..count].ToArray());
        Assert.Contains(2, results[..count].ToArray());
        Assert.Contains(3, results[..count].ToArray());
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

        Span<int> results = stackalloc int[10];
        var radiusSquared = 10f * 10f; // radius = 10
        int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

        Assert.Equal(2, count);
        Assert.Contains(0, results[..count].ToArray()); // (5,0,0) is within range
        Assert.Contains(2, results[..count].ToArray()); // (3,0,4) is within range
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

        Span<int> results = stackalloc int[20];
        var radiusSquared = 15f * 15f;
        int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

        // Should match indices 0-7 (positions 0, 2, 4, 6, 8, 10, 12, 14 are within radius 15)
        Assert.Equal(8, count);
        for (int i = 0; i < 8; i++)
        {
            Assert.Contains(i, results[..count].ToArray());
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

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(-10, -10, -10),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(0, count);
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

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(4, count);
        Assert.Contains(0, results[..count].ToArray());
        Assert.Contains(1, results[..count].ToArray());
        Assert.Contains(2, results[..count].ToArray());
        Assert.Contains(3, results[..count].ToArray());
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

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(3, count);
        Assert.Contains(0, results[..count].ToArray()); // (5,5,5) inside
        Assert.Contains(2, results[..count].ToArray()); // (0,0,0) on min boundary
        Assert.Contains(3, results[..count].ToArray()); // (10,10,10) on max boundary
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

        Span<int> results = stackalloc int[20];
        int count = SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(5, 5, 5),
            new Vector3(15, 15, 15),
            results);

        // Should match indices 5-15 (positions where all coords are between 5 and 15)
        Assert.Equal(11, count);
        for (int i = 5; i <= 15; i++)
        {
            Assert.Contains(i, results[..count].ToArray());
        }
    }

    [Fact]
    public void FilterByDistanceSIMD_WithNonMultipleOfFourPositions_HandlesRemainder()
    {
        // Test with 5, 6, 7 positions to ensure remainder handling works
        Span<int> results = stackalloc int[10];
        for (int posCount = 5; posCount <= 7; posCount++)
        {
            var positions = new Vector3[posCount];
            for (int i = 0; i < posCount; i++)
            {
                positions[i] = new Vector3(i, 0, 0);
            }

            var radiusSquared = 4f * 4f; // Matches indices 0-4
            int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

            Assert.Equal(Math.Min(5, posCount), count);
        }
    }

    [Fact]
    public void FilterByAABBSIMD_WithNonMultipleOfFourPositions_HandlesRemainder()
    {
        // Test with 5, 6, 7 positions to ensure remainder handling works
        Span<int> results = stackalloc int[10];
        for (int posCount = 5; posCount <= 7; posCount++)
        {
            var positions = new Vector3[posCount];
            for (int i = 0; i < posCount; i++)
            {
                positions[i] = new Vector3(i, i, i);
            }

            var min = new Vector3(0, 0, 0);
            var max = new Vector3(10, 10, 10);
            int count = SimdHelpers.FilterByAABBSIMD(positions, min, max, results);

            Assert.Equal(posCount, count);  // All positions should match
        }
    }

    [Fact]
    public void FilterByDistanceSIMD_WithSmallArrayCount_UsesScalarPath()
    {
        // Test with < 4 positions to ensure scalar fallback works
        var positions = new Vector3[]
        {
            new(1, 0, 0),
            new(2, 0, 0),
            new(100, 0, 0)  // This one outside range
        };

        Span<int> results = stackalloc int[10];
        var radiusSquared = 3f * 3f;
        int count = SimdHelpers.FilterByDistanceSIMD(positions, Vector3.Zero, radiusSquared, results);

        Assert.Equal(2, count);
        Assert.Contains(0, results[..count].ToArray());
        Assert.Contains(1, results[..count].ToArray());
    }

    [Fact]
    public void FilterByAABBSIMD_WithSmallArrayCount_UsesScalarPath()
    {
        // Test with < 4 positions to ensure scalar fallback works
        var positions = new Vector3[]
        {
            new(5, 5, 5),
            new(15, 15, 15),  // Outside
            new(7, 7, 7)
        };

        Span<int> results = stackalloc int[10];
        int count = SimdHelpers.FilterByAABBSIMD(
            positions,
            new Vector3(0, 0, 0),
            new Vector3(10, 10, 10),
            results);

        Assert.Equal(2, count);
        Assert.Contains(0, results[..count].ToArray());
        Assert.Contains(2, results[..count].ToArray());
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
        Span<int> simdResults = stackalloc int[100];
        int simdCount = SimdHelpers.FilterByDistanceSIMD(positions, center, radiusSquared, simdResults);

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
        Assert.Equal(scalarResults.Count, simdCount);
        foreach (var index in scalarResults)
        {
            Assert.Contains(index, simdResults[..simdCount].ToArray());
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
        Span<int> simdResults = stackalloc int[100];
        int simdCount = SimdHelpers.FilterByAABBSIMD(positions, min, max, simdResults);

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
        Assert.Equal(scalarResults.Count, simdCount);
        foreach (var index in scalarResults)
        {
            Assert.Contains(index, simdResults[..simdCount].ToArray());
        }
    }
}
