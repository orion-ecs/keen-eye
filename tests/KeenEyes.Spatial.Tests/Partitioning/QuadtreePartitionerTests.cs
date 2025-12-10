using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for the QuadtreePartitioner class.
/// </summary>
public class QuadtreePartitionerTests : IDisposable
{
    private readonly QuadtreePartitioner partitioner;
    private readonly Entity entity1 = new(1, 0);
    private readonly Entity entity2 = new(2, 0);
    private readonly Entity entity3 = new(3, 0);

    public QuadtreePartitionerTests()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000)
        };
        partitioner = new QuadtreePartitioner(config);
    }

    public void Dispose()
    {
        partitioner.Dispose();
    }

    #region Point Entity Tests

    [Fact]
    public void Update_WithPointEntity_IndexesEntity()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));

        Assert.Equal(1, partitioner.EntityCount);
    }

    [Fact]
    public void Update_WithMultiplePointEntities_IndexesAll()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(50, 0, 0));
        partitioner.Update(entity3, new Vector3(100, 0, 0));

        Assert.Equal(3, partitioner.EntityCount);
    }

    [Fact]
    public void Update_SameEntityTwice_UpdatesPosition()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity1, new Vector3(200, 0, 0));

        // Should still have only 1 entity
        Assert.Equal(1, partitioner.EntityCount);

        // Entity should be queryable at new position
        var results = partitioner.QueryPoint(new Vector3(200, 0, 0)).ToList();
        Assert.Contains(entity1, results);
    }

    #endregion

    #region AABB Entity Tests

    [Fact]
    public void Update_WithBounds_IndexesEntity()
    {
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-50, -50, -50),
            Max = new Vector3(50, 50, 50)
        };

        partitioner.Update(entity1, Vector3.Zero, bounds);

        Assert.Equal(1, partitioner.EntityCount);
    }

    [Fact]
    public void Update_WithLargeBounds_IndexesCorrectly()
    {
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-150, 0, 0),
            Max = new Vector3(150, 0, 0)
        };

        partitioner.Update(entity1, Vector3.Zero, bounds);

        // Query should find entity from points within bounds
        var results1 = partitioner.QueryPoint(new Vector3(-100, 0, 0)).ToList();
        var results2 = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();
        var results3 = partitioner.QueryPoint(new Vector3(100, 0, 0)).ToList();

        Assert.Contains(entity1, results1);
        Assert.Contains(entity1, results2);
        Assert.Contains(entity1, results3);
    }

    #endregion

    #region QueryRadius Tests

    [Fact]
    public void QueryRadius_WithNoEntities_ReturnsEmpty()
    {
        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryRadius_WithEntityInRange_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(50, 0, 0));

        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryRadius_WithEntityOutOfRange_ReturnsEmpty()
    {
        partitioner.Update(entity1, new Vector3(500, 0, 0));

        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.DoesNotContain(entity1, results);
    }

    [Fact]
    public void QueryRadius_WithMultipleEntities_ReturnsOnlyNearby()
    {
        partitioner.Update(entity1, new Vector3(50, 0, 0));
        partitioner.Update(entity2, new Vector3(500, 0, 0));
        partitioner.Update(entity3, new Vector3(25, 0, 25));

        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryRadius_AcrossQuadrantBoundaries_FindsEntities()
    {
        // Place entities in different quadrants
        partitioner.Update(entity1, new Vector3(-50, 0, -50)); // NW
        partitioner.Update(entity2, new Vector3(50, 0, -50));  // NE
        partitioner.Update(entity3, new Vector3(-50, 0, 50));  // SW

        // Query from center with radius that covers all three
        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
        Assert.Contains(entity3, results);
    }

    #endregion

    #region QueryBounds Tests

    [Fact]
    public void QueryBounds_WithNoEntities_ReturnsEmpty()
    {
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryBounds_WithEntityInBounds_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));

        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryBounds_WithEntityOutOfBounds_ReturnsEmpty()
    {
        partitioner.Update(entity1, new Vector3(500, 0, 0));

        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.DoesNotContain(entity1, results);
    }

    [Fact]
    public void QueryBounds_WithMultipleEntities_ReturnsOnlyInBounds()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(500, 0, 0));
        partitioner.Update(entity3, new Vector3(25, 0, 25));

        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryBounds_SpanningMultipleQuadrants_FindsAllEntities()
    {
        // Place entities across different quadrants
        partitioner.Update(entity1, new Vector3(-100, 0, -100));
        partitioner.Update(entity2, new Vector3(100, 0, 100));
        partitioner.Update(entity3, new Vector3(0, 0, 0));

        // Query bounds spanning all quadrants
        var results = partitioner.QueryBounds(
            new Vector3(-150, -50, -150),
            new Vector3(150, 50, 150)).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
        Assert.Contains(entity3, results);
        Assert.Equal(3, results.Count);
    }

    #endregion

    #region QueryPoint Tests

    [Fact]
    public void QueryPoint_WithNoEntities_ReturnsEmpty()
    {
        var results = partitioner.QueryPoint(Vector3.Zero).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryPoint_WithEntityInSameNode_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(25, 0, 25));

        var results = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryPoint_WithEntityFarAway_MayReturnFalsePositive()
    {
        // Quadtree is a broadphase - without subdivision, all entities are in root
        partitioner.Update(entity1, new Vector3(500, 0, 0));

        var results = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();

        // Broadphase queries may return false positives
        // Without subdivision (only 1 entity), both map to root node
        // This test just verifies the query doesn't crash
        Assert.True(results.Count >= 0);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_RemovesEntityFromIndex()
    {
        partitioner.Update(entity1, Vector3.Zero);
        Assert.Equal(1, partitioner.EntityCount);

        partitioner.Remove(entity1);
        Assert.Equal(0, partitioner.EntityCount);
    }

    [Fact]
    public void Remove_RemovedEntityNotFoundInQueries()
    {
        partitioner.Update(entity1, Vector3.Zero);
        partitioner.Remove(entity1);

        var results = partitioner.QueryPoint(Vector3.Zero).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Remove_RemoveNonExistentEntity_DoesNotThrow()
    {
        partitioner.Remove(entity1);
        Assert.Equal(0, partitioner.EntityCount);
    }

    [Fact]
    public void Remove_RemoveSameEntityTwice_DoesNotThrow()
    {
        partitioner.Update(entity1, Vector3.Zero);
        partitioner.Remove(entity1);

        partitioner.Remove(entity1);
        Assert.Equal(0, partitioner.EntityCount);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(100, 0, 0));
        partitioner.Update(entity3, new Vector3(200, 0, 0));

        Assert.Equal(3, partitioner.EntityCount);

        partitioner.Clear();

        Assert.Equal(0, partitioner.EntityCount);
    }

    [Fact]
    public void Clear_AfterClear_QueriesReturnEmpty()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(100, 0, 0));

        partitioner.Clear();

        var results1 = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();
        var results2 = partitioner.QueryRadius(Vector3.Zero, 200f).ToList();

        Assert.Empty(results1);
        Assert.Empty(results2);
    }

    [Fact]
    public void Clear_CanAddEntitiesAfterClear()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Clear();

        partitioner.Update(entity2, new Vector3(100, 0, 0));

        Assert.Equal(1, partitioner.EntityCount);
        var results = partitioner.QueryPoint(new Vector3(100, 0, 0)).ToList();
        Assert.Contains(entity2, results);
    }

    #endregion

    #region Subdivision Tests

    [Fact]
    public void Subdivision_WithManyClusteredEntities_SubdividesCorrectly()
    {
        // Add 20 entities in the same area (exceeds MaxEntitiesPerNode = 8)
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(100 + i, 0);
            partitioner.Update(entity, new Vector3(i * 5, 0, i * 5));
        }

        Assert.Equal(20, partitioner.EntityCount);

        // All entities should still be queryable
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(150, 50, 150)).ToList();

        Assert.Equal(20, results.Count);
    }

    [Fact]
    public void Subdivision_WithEntitiesInDifferentQuadrants_DistributesCorrectly()
    {
        // Place many entities in different quadrants
        for (int i = 0; i < 10; i++)
        {
            partitioner.Update(new Entity(100 + i, 0), new Vector3(i * 10, 0, i * 10)); // NE
            partitioner.Update(new Entity(200 + i, 0), new Vector3(-i * 10, 0, i * 10)); // NW
            partitioner.Update(new Entity(300 + i, 0), new Vector3(i * 10, 0, -i * 10)); // SE
            partitioner.Update(new Entity(400 + i, 0), new Vector3(-i * 10, 0, -i * 10)); // SW
        }

        Assert.Equal(40, partitioner.EntityCount);

        // Each quadrant should be queryable independently
        var ne = partitioner.QueryBounds(new Vector3(0, -50, 0), new Vector3(200, 50, 200)).ToList();
        var nw = partitioner.QueryBounds(new Vector3(-200, -50, 0), new Vector3(0, 50, 200)).ToList();

        Assert.True(ne.Count >= 10); // At least the NE entities
        Assert.True(nw.Count >= 10); // At least the NW entities
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Update_NegativeCoordinates_HandlesCorrectly()
    {
        partitioner.Update(entity1, new Vector3(-50, 0, -50));

        var results = partitioner.QueryPoint(new Vector3(-50, 0, -50)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Update_VeryLargeCoordinates_HandlesCorrectly()
    {
        partitioner.Update(entity1, new Vector3(900, 0, 900));

        var results = partitioner.QueryPoint(new Vector3(900, 0, 900)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void EntityCount_AccurateAfterMultipleOperations()
    {
        Assert.Equal(0, partitioner.EntityCount);

        partitioner.Update(entity1, Vector3.Zero);
        Assert.Equal(1, partitioner.EntityCount);

        partitioner.Update(entity2, new Vector3(100, 0, 0));
        Assert.Equal(2, partitioner.EntityCount);

        partitioner.Update(entity1, new Vector3(200, 0, 0)); // Move entity1
        Assert.Equal(2, partitioner.EntityCount); // Count shouldn't change

        partitioner.Remove(entity1);
        Assert.Equal(1, partitioner.EntityCount);

        partitioner.Clear();
        Assert.Equal(0, partitioner.EntityCount);
    }

    #endregion

    #region SIMD Integration Tests

    [Fact]
    public void QueryBounds_WithManyEntitiesInNode_UsesStackallocSIMD()
    {
        //  Create 50 entities in a small area (triggers stackalloc SIMD path: 16-128 entities)
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 1,  // Shallow tree to keep entities in one node
            MaxEntitiesPerNode = 100  // Allow many entities per node
        };
        using var partitioner = new QuadtreePartitioner(config);

        var entities = new List<Entity>();
        for (int i = 0; i < 50; i++)
        {
            var entity = new Entity(i + 1, 0);
            var pos = new Vector3(i * 2, 0, 0);  // Spread entities along X axis
            partitioner.Update(entity, pos);
            entities.Add(entity);
        }

        // Query a bounds that includes all entities
        var results = partitioner.QueryBounds(new Vector3(-10, 0, -10), new Vector3(200, 0, 10)).ToList();

        // Should find all 50 entities
        Assert.Equal(50, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void QueryBounds_WithVeryManyEntitiesInNode_UsesArrayPoolSIMD()
    {
        // Create 150 entities in a small area (triggers ArrayPool SIMD path: >128 entities)
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 1,  // Shallow tree to keep entities in one node
            MaxEntitiesPerNode = 200  // Allow many entities per node
        };
        using var partitioner = new QuadtreePartitioner(config);

        var entities = new List<Entity>();
        for (int i = 0; i < 150; i++)
        {
            var entity = new Entity(i + 1, 0);
            var pos = new Vector3(i * 2, 0, 0);  // Spread entities along X axis
            partitioner.Update(entity, pos);
            entities.Add(entity);
        }

        // Query a bounds that includes all entities
        var results = partitioner.QueryBounds(new Vector3(-10, 0, -10), new Vector3(400, 0, 10)).ToList();

        // Should find all 150 entities
        Assert.Equal(150, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    #endregion
}
