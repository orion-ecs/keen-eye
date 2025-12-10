using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;
using KeenEyes.Spatial.Partitioning;
using Xunit;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for deterministic mode across all partitioners.
/// </summary>
public class DeterministicModeTests
{
    [Fact]
    public void GridPartitioner_DeterministicMode_ReturnsSortedResults()
    {
        var config = new GridConfig { CellSize = 10.0f, DeterministicMode = true };
        using var partitioner = new GridPartitioner(config);

        // Add entities in random order
        var e3 = new Entity(3, 0);
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);

        partitioner.Update(e3, new Vector3(0, 0, 0));
        partitioner.Update(e1, new Vector3(1, 0, 0));
        partitioner.Update(e2, new Vector3(2, 0, 0));

        // Query should return entities sorted by ID
        var results = partitioner.QueryBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
    }

    [Fact]
    public void GridPartitioner_NonDeterministicMode_MayReturnUnsorted()
    {
        var config = new GridConfig { CellSize = 10.0f, DeterministicMode = false };
        using var partitioner = new GridPartitioner(config);

        // Add entities
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);
        var e3 = new Entity(3, 0);

        partitioner.Update(e1, new Vector3(0, 0, 0));
        partitioner.Update(e2, new Vector3(1, 0, 0));
        partitioner.Update(e3, new Vector3(2, 0, 0));

        // Non-deterministic mode - order may vary
        var results = partitioner.QueryBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)).ToList();

        Assert.Equal(3, results.Count);
        // All entities present but order not guaranteed
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.Contains(e3, results);
    }

    [Fact]
    public void QuadtreePartitioner_DeterministicMode_ReturnsSortedResults()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            DeterministicMode = true
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Add entities in random order
        var e5 = new Entity(5, 0);
        var e2 = new Entity(2, 0);
        var e4 = new Entity(4, 0);
        var e1 = new Entity(1, 0);
        var e3 = new Entity(3, 0);

        partitioner.Update(e5, new Vector3(0, 0, 0));
        partitioner.Update(e2, new Vector3(1, 0, 1));
        partitioner.Update(e4, new Vector3(2, 0, 2));
        partitioner.Update(e1, new Vector3(3, 0, 3));
        partitioner.Update(e3, new Vector3(4, 0, 4));

        // Query should return entities sorted by ID
        var results = partitioner.QueryBounds(new Vector3(-10, 0, -10), new Vector3(10, 0, 10)).ToList();

        Assert.Equal(5, results.Count);
        for (int i = 0; i < 5; i++)
        {
            Assert.Equal(i + 1, results[i].Id);
        }
    }

    [Fact]
    public void OctreePartitioner_DeterministicMode_ReturnsSortedResults()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            DeterministicMode = true
        };
        using var partitioner = new OctreePartitioner(config);

        // Add entities in random order
        var e4 = new Entity(4, 0);
        var e1 = new Entity(1, 0);
        var e3 = new Entity(3, 0);
        var e2 = new Entity(2, 0);

        partitioner.Update(e4, new Vector3(0, 0, 0));
        partitioner.Update(e1, new Vector3(1, 1, 1));
        partitioner.Update(e3, new Vector3(2, 2, 2));
        partitioner.Update(e2, new Vector3(3, 3, 3));

        // Query should return entities sorted by ID
        var results = partitioner.QueryBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)).ToList();

        Assert.Equal(4, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
        Assert.Equal(4, results[3].Id);
    }

    [Fact]
    public void QuadtreePartitioner_DeterministicMode_QueryRadius_ReturnsSorted()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            DeterministicMode = true
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Add entities
        var e3 = new Entity(3, 0);
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);

        partitioner.Update(e3, new Vector3(0, 0, 0));
        partitioner.Update(e1, new Vector3(1, 0, 1));
        partitioner.Update(e2, new Vector3(2, 0, 2));

        // Query radius should return sorted
        var results = partitioner.QueryRadius(new Vector3(1, 0, 1), 5.0f).ToList();

        Assert.True(results.Count >= 1); // At least e1 should be in range
        // Check that results are sorted
        for (int i = 1; i < results.Count; i++)
        {
            Assert.True(results[i - 1].Id <= results[i].Id);
        }
    }

    [Fact]
    public void OctreePartitioner_DeterministicMode_QueryPoint_ReturnsSorted()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 2, // Force multiple entities in same node
            DeterministicMode = true
        };
        using var partitioner = new OctreePartitioner(config);

        // Add multiple entities at same point
        var e3 = new Entity(3, 0);
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);

        var position = new Vector3(5, 5, 5);
        partitioner.Update(e3, position);
        partitioner.Update(e1, position);
        partitioner.Update(e2, position);

        // QueryPoint should return sorted
        var results = partitioner.QueryPoint(position).ToList();

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].Id);
        Assert.Equal(2, results[1].Id);
        Assert.Equal(3, results[2].Id);
    }

    [Fact]
    public void GridPartitioner_DeterministicMode_ConsistentAcrossQueries()
    {
        var config = new GridConfig { CellSize = 10.0f, DeterministicMode = true };
        using var partitioner = new GridPartitioner(config);

        // Add entities
        for (int i = 10; i >= 1; i--)
        {
            var entity = new Entity(i, 0);
            partitioner.Update(entity, new Vector3(i, 0, 0));
        }

        // Multiple queries should return same order
        var query1 = partitioner.QueryBounds(new Vector3(-100, -100, -100), new Vector3(100, 100, 100)).ToList();
        var query2 = partitioner.QueryBounds(new Vector3(-100, -100, -100), new Vector3(100, 100, 100)).ToList();
        var query3 = partitioner.QueryBounds(new Vector3(-100, -100, -100), new Vector3(100, 100, 100)).ToList();

        Assert.Equal(query1.Count, query2.Count);
        Assert.Equal(query1.Count, query3.Count);

        for (int i = 0; i < query1.Count; i++)
        {
            Assert.Equal(query1[i].Id, query2[i].Id);
            Assert.Equal(query1[i].Id, query3[i].Id);
        }
    }
}
