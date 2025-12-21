using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Additional tests for GridPartitioner to improve coverage.
/// </summary>
public class GridPartitionerAdditionalTests
{
    [Fact]
    public void Constructor_WithInvalidConfig_ThrowsArgumentException()
    {
        // Arrange - Invalid config with negative cell size
        var invalidConfig = new GridConfig
        {
            CellSize = -10f,
            WorldMin = Vector3.Zero,
            WorldMax = new Vector3(100, 100, 100)
        };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new GridPartitioner(invalidConfig));
        Assert.Contains("Invalid GridConfig", exception.Message);
    }

    [Fact]
    public void QueryBounds_Vector3Overload_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            DeterministicMode = true
        };

        using var partitioner = new GridPartitioner(config);

        // Add entities in non-sorted order
        var entity1 = new Entity(7, 0);
        var entity2 = new Entity(3, 0);
        var entity3 = new Entity(9, 0);

        partitioner.Update(entity1, new Vector3(10, 10, 10));
        partitioner.Update(entity2, new Vector3(15, 15, 15));
        partitioner.Update(entity3, new Vector3(20, 20, 20));

        // Act - Use Vector3 overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryBounds(new Vector3(5, 5, 5), new Vector3(30, 30, 30), results);

        // Assert
        Assert.Equal(3, count);
        // Results should be sorted by entity ID
        Assert.Equal(entity2, results[0]); // ID 3
        Assert.Equal(entity1, results[1]); // ID 7
        Assert.Equal(entity3, results[2]); // ID 9
    }

    [Fact]
    public void QueryBounds_WithMultiCellEntity_ReturnsEntityOnce()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 50f,
            WorldMin = new Vector3(-500, -500, -500),
            WorldMax = new Vector3(500, 500, 500),
            DeterministicMode = false
        };

        using var partitioner = new GridPartitioner(config);

        // Add entity with bounds spanning multiple cells
        var entity = new Entity(1, 0);
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-60, -60, -60),
            Max = new Vector3(60, 60, 60)
        };

        partitioner.Update(entity, Vector3.Zero, bounds);

        // Act - Query across the cells the entity spans
        Span<Entity> results = stackalloc Entity[20];
        int count = partitioner.QueryBounds(new Vector3(-100, -100, -100), new Vector3(100, 100, 100), results);

        // Assert - Entity should appear only once, not multiple times
        Assert.Equal(1, count);
        Assert.Equal(entity, results[0]);
    }

    [Fact]
    public void QueryBounds_Vector3Overload_WithOverflow_ReturnsNegativeOne()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 50f,
            WorldMin = new Vector3(-500, -500, -500),
            WorldMax = new Vector3(500, 500, 500)
        };

        using var partitioner = new GridPartitioner(config);

        // Add more entities than result buffer can hold
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i, 0);
            partitioner.Update(entity, new Vector3(i * 5, i * 5, i * 5));
        }

        // Act - Small result buffer to trigger overflow
        Span<Entity> results = stackalloc Entity[5];
        int count = partitioner.QueryBounds(new Vector3(0, 0, 0), new Vector3(100, 100, 100), results);

        // Assert - Should return -1 for overflow
        Assert.Equal(-1, count);
    }

    [Fact]
    public void QueryBounds_EmptyResults_ReturnsZero()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            DeterministicMode = true
        };

        using var partitioner = new GridPartitioner(config);

        // Act - Query empty partitioner
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryBounds(new Vector3(-50, -50, -50), new Vector3(50, 50, 50), results);

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryPoint_WithDeterministicMode_ReturnsConsistentOrder()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            DeterministicMode = true
        };

        using var partitioner = new GridPartitioner(config);

        // Add entities in the same cell
        var entity1 = new Entity(5, 0);
        var entity2 = new Entity(2, 0);
        var entity3 = new Entity(8, 0);

        partitioner.Update(entity1, new Vector3(10, 10, 10));
        partitioner.Update(entity2, new Vector3(15, 15, 15));
        partitioner.Update(entity3, new Vector3(20, 20, 20));

        // Act - Query multiple times
        Span<Entity> results1 = stackalloc Entity[10];
        Span<Entity> results2 = stackalloc Entity[10];

        int count1 = partitioner.QueryPoint(new Vector3(10, 10, 10), results1);
        int count2 = partitioner.QueryPoint(new Vector3(10, 10, 10), results2);

        // Assert - Same results in same order
        Assert.Equal(count1, count2);
        for (int i = 0; i < count1; i++)
        {
            Assert.Equal(results1[i], results2[i]);
        }
    }

    [Fact]
    public void Update_WithLargeBounds_SpansMultipleCells()
    {
        // Arrange
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000)
        };

        using var partitioner = new GridPartitioner(config);

        // Add entity with large bounds
        var entity = new Entity(1, 0);
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-150, -150, -150),
            Max = new Vector3(150, 150, 150)
        };

        // Act
        partitioner.Update(entity, Vector3.Zero, bounds);

        // Assert - Entity should be queryable from multiple cells
        var results1 = partitioner.QueryPoint(new Vector3(-100, -100, -100)).ToList();
        var results2 = partitioner.QueryPoint(new Vector3(0, 0, 0)).ToList();
        var results3 = partitioner.QueryPoint(new Vector3(100, 100, 100)).ToList();

        Assert.Contains(entity, results1);
        Assert.Contains(entity, results2);
        Assert.Contains(entity, results3);
    }
}
