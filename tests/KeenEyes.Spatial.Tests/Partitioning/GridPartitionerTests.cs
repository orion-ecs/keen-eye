using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for the GridPartitioner class.
/// </summary>
public class GridPartitionerTests : IDisposable
{
    private readonly GridPartitioner partitioner;
    private readonly Entity entity1 = new(1, 0);
    private readonly Entity entity2 = new(2, 0);
    private readonly Entity entity3 = new(3, 0);

    public GridPartitionerTests()
    {
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000)
        };
        partitioner = new GridPartitioner(config);
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
    public void Update_WithBounds_IndexesEntityInMultipleCells()
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
    public void Update_WithLargeBounds_IndexesInMultipleCells()
    {
        // Bounds spanning multiple grid cells (100 units each)
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-150, 0, 0),
            Max = new Vector3(150, 0, 0) // 300 units = 3 cells
        };

        partitioner.Update(entity1, Vector3.Zero, bounds);

        // Query should find entity from any point within bounds
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

        // Broadphase may return false positives but should not miss entities
        // If empty, entity is definitely out of range at grid level
        Assert.DoesNotContain(entity1, results);
    }

    [Fact]
    public void QueryRadius_WithMultipleEntities_ReturnsOnlyNearby()
    {
        partitioner.Update(entity1, new Vector3(50, 0, 0));
        partitioner.Update(entity2, new Vector3(500, 0, 0));
        partitioner.Update(entity3, new Vector3(25, 25, 0));

        var results = partitioner.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryRadius_OnCellBoundary_FindsNearbyEntities()
    {
        // Place entities at cell boundaries (100 unit cells)
        partitioner.Update(entity1, new Vector3(99, 0, 0));
        partitioner.Update(entity2, new Vector3(101, 0, 0));

        var results = partitioner.QueryRadius(new Vector3(100, 0, 0), 50f).ToList();

        // Both should be found (broadphase includes neighboring cells)
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
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
        partitioner.Update(entity3, new Vector3(25, 25, 25));

        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryBounds_SpanningMultipleCells_FindsAllEntities()
    {
        // Place entities across multiple cells
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(150, 0, 0));
        partitioner.Update(entity3, new Vector3(300, 0, 0));

        // Query bounds spanning all three cells
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(350, 50, 50)).ToList();

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
    public void QueryPoint_WithEntityInSameCell_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(25, 25, 25));

        var results = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();

        // Both points in same cell (0-100 range)
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryPoint_WithEntityInDifferentCell_ReturnsEmpty()
    {
        partitioner.Update(entity1, new Vector3(500, 0, 0));

        var results = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();

        Assert.DoesNotContain(entity1, results);
    }

    [Fact]
    public void QueryPoint_AtCellBoundary_ConsistentResults()
    {
        // Test behavior at cell boundary (100 units)
        partitioner.Update(entity1, new Vector3(100, 0, 0));
        partitioner.Update(entity2, new Vector3(99, 0, 0));

        var resultsAt99 = partitioner.QueryPoint(new Vector3(99, 0, 0)).ToList();
        var resultsAt100 = partitioner.QueryPoint(new Vector3(100, 0, 0)).ToList();

        // Entity at 99 should be in same cell as query at 99
        Assert.Contains(entity2, resultsAt99);

        // Entity at 100 should be in same cell as query at 100
        Assert.Contains(entity1, resultsAt100);
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
        // Should not throw even if entity wasn't indexed
        partitioner.Remove(entity1);
        Assert.Equal(0, partitioner.EntityCount);
    }

    [Fact]
    public void Remove_RemoveSameEntityTwice_DoesNotThrow()
    {
        partitioner.Update(entity1, Vector3.Zero);
        partitioner.Remove(entity1);

        // Removing again should not throw
        partitioner.Remove(entity1);
        Assert.Equal(0, partitioner.EntityCount);
    }

    [Fact]
    public void Remove_EntityWithBounds_RemovesFromAllCells()
    {
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-150, 0, 0),
            Max = new Vector3(150, 0, 0)
        };

        partitioner.Update(entity1, Vector3.Zero, bounds);
        partitioner.Remove(entity1);

        // Query multiple points that should have contained the entity
        var results1 = partitioner.QueryPoint(new Vector3(-100, 0, 0)).ToList();
        var results2 = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();
        var results3 = partitioner.QueryPoint(new Vector3(100, 0, 0)).ToList();

        Assert.DoesNotContain(entity1, results1);
        Assert.DoesNotContain(entity1, results2);
        Assert.DoesNotContain(entity1, results3);
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

    #region Edge Cases and Performance

    [Fact]
    public void Update_NegativeCoordinates_HandlesCorrectly()
    {
        partitioner.Update(entity1, new Vector3(-50, -50, -50));

        var results = partitioner.QueryPoint(new Vector3(-50, -50, -50)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Update_VeryLargeCoordinates_HandlesCorrectly()
    {
        // Within world bounds
        partitioner.Update(entity1, new Vector3(900, 900, 900));

        var results = partitioner.QueryPoint(new Vector3(900, 900, 900)).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryRadius_ZeroRadius_FindsOnlyExactCell()
    {
        partitioner.Update(entity1, new Vector3(50, 0, 0));
        partitioner.Update(entity2, new Vector3(150, 0, 0));

        var results = partitioner.QueryRadius(new Vector3(50, 0, 0), 0f).ToList();

        // Should find entities in same cell
        Assert.Contains(entity1, results);
        // Should not find entities in other cells (0 radius = single cell check)
        Assert.DoesNotContain(entity2, results);
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
}
