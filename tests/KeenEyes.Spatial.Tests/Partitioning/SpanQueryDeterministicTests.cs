using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for deterministic mode with Span-based query APIs.
/// </summary>
public class SpanQueryDeterministicTests
{
    #region Octree Span Query Tests

    [Fact]
    public void OctreeQueryBounds_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new OctreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, -100, -100),
            WorldMax = new Vector3(100, 100, 100),
            DeterministicMode = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add entities in non-sorted order
        var entity1 = new Entity(5, 0);
        var entity2 = new Entity(2, 0);
        var entity3 = new Entity(8, 0);
        var entity4 = new Entity(1, 0);

        partitioner.Update(entity1, new Vector3(10, 10, 10));
        partitioner.Update(entity2, new Vector3(15, 15, 15));
        partitioner.Update(entity3, new Vector3(20, 20, 20));
        partitioner.Update(entity4, new Vector3(25, 25, 25));

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryBounds(new Vector3(5, 5, 5), new Vector3(30, 30, 30), results);

        // Assert
        Assert.Equal(4, count);
        // Results should be sorted by entity ID in deterministic mode
        Assert.Equal(entity4, results[0]); // ID 1
        Assert.Equal(entity2, results[1]); // ID 2
        Assert.Equal(entity1, results[2]); // ID 5
        Assert.Equal(entity3, results[3]); // ID 8
    }

    [Fact]
    public void OctreeQueryPoint_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new OctreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, -100, -100),
            WorldMax = new Vector3(100, 100, 100),
            DeterministicMode = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add multiple entities at nearby points in non-sorted order
        var entity1 = new Entity(7, 0);
        var entity2 = new Entity(3, 0);
        var entity3 = new Entity(9, 0);

        var point = new Vector3(50, 50, 50);
        partitioner.Update(entity1, point);
        partitioner.Update(entity2, new Vector3(50.1f, 50.1f, 50.1f));
        partitioner.Update(entity3, new Vector3(50.2f, 50.2f, 50.2f));

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryPoint(point, results);

        // Assert - At least some entities returned
        Assert.True(count > 0);
        // If deterministic mode is working, results should be in ID order
        if (count > 1)
        {
            for (int i = 0; i < count - 1; i++)
            {
                Assert.True(results[i].Id <= results[i + 1].Id);
            }
        }
    }

    [Fact]
    public void OctreeQueryPoint_OutsideBounds_ReturnsZero()
    {
        // Arrange
        var config = new OctreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, -100, -100),
            WorldMax = new Vector3(100, 100, 100),
            DeterministicMode = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Act - Query point outside world bounds
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryPoint(new Vector3(200, 200, 200), results);

        // Assert - FindLeafNode returns null, so count is 0
        Assert.Equal(0, count);
    }

    [Fact]
    public void OctreeQueryFrustum_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new OctreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, -100, -100),
            WorldMax = new Vector3(100, 100, 100),
            DeterministicMode = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add entities in non-sorted order
        var entity1 = new Entity(6, 0);
        var entity2 = new Entity(1, 0);
        var entity3 = new Entity(4, 0);

        partitioner.Update(entity1, new Vector3(0, 0, 10));
        partitioner.Update(entity2, new Vector3(0, 0, 20));
        partitioner.Update(entity3, new Vector3(0, 0, 30));

        // Create a frustum that contains all entities
        var viewMatrix = Matrix4x4.CreateLookAt(
            new Vector3(0, 0, -50),
            new Vector3(0, 0, 50),
            Vector3.UnitY
        );
        var projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2,
            1.0f,
            1.0f,
            200.0f
        );
        var frustum = Frustum.FromMatrix(viewMatrix * projMatrix);

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryFrustum(frustum, results);

        // Assert
        Assert.Equal(3, count);
        // Results should be sorted by entity ID
        Assert.Equal(entity2, results[0]); // ID 1
        Assert.Equal(entity3, results[1]); // ID 4
        Assert.Equal(entity1, results[2]); // ID 6
    }

    #endregion

    #region Quadtree Span Query Tests

    [Fact]
    public void QuadtreeQueryBounds_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new QuadtreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            DeterministicMode = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add entities in non-sorted order
        var entity1 = new Entity(5, 0);
        var entity2 = new Entity(2, 0);
        var entity3 = new Entity(8, 0);

        partitioner.Update(entity1, new Vector3(10, 0, 10));
        partitioner.Update(entity2, new Vector3(15, 0, 15));
        partitioner.Update(entity3, new Vector3(20, 0, 20));

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryBounds(new Vector3(5, 0, 5), new Vector3(30, 0, 30), results);

        // Assert
        Assert.Equal(3, count);
        // Results should be sorted by entity ID
        Assert.Equal(entity2, results[0]); // ID 2
        Assert.Equal(entity1, results[1]); // ID 5
        Assert.Equal(entity3, results[2]); // ID 8
    }

    [Fact]
    public void QuadtreeQueryPoint_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new QuadtreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            DeterministicMode = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add multiple entities in same cell in non-sorted order
        var entity1 = new Entity(7, 0);
        var entity2 = new Entity(3, 0);
        var entity3 = new Entity(9, 0);

        var point = new Vector3(50, 0, 50);
        partitioner.Update(entity1, point);
        partitioner.Update(entity2, new Vector3(50.1f, 0, 50.1f));
        partitioner.Update(entity3, new Vector3(50.2f, 0, 50.2f));

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryPoint(point, results);

        // Assert - At least some entities returned
        Assert.True(count > 0);
        // If deterministic mode is working, results should be in ID order
        if (count > 1)
        {
            for (int i = 0; i < count - 1; i++)
            {
                Assert.True(results[i].Id <= results[i + 1].Id);
            }
        }
    }

    [Fact]
    public void QuadtreeQueryPoint_OutsideBounds_ReturnsZero()
    {
        // Arrange
        var config = new QuadtreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            DeterministicMode = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Act - Query point outside world bounds
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryPoint(new Vector3(200, 0, 200), results);

        // Assert - FindLeafNode returns null, so count is 0
        Assert.Equal(0, count);
    }

    [Fact]
    public void QuadtreeQueryFrustum_SpanAPI_WithDeterministicMode_SortsResults()
    {
        // Arrange
        var config = new QuadtreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            DeterministicMode = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add entities in non-sorted order
        var entity1 = new Entity(6, 0);
        var entity2 = new Entity(1, 0);
        var entity3 = new Entity(4, 0);

        partitioner.Update(entity1, new Vector3(0, 0, 10));
        partitioner.Update(entity2, new Vector3(0, 0, 20));
        partitioner.Update(entity3, new Vector3(0, 0, 30));

        // Create a frustum that contains all entities
        var viewMatrix = Matrix4x4.CreateLookAt(
            new Vector3(0, 50, -50),
            new Vector3(0, 0, 50),
            Vector3.UnitY
        );
        var projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2,
            1.0f,
            1.0f,
            200.0f
        );
        var frustum = Frustum.FromMatrix(viewMatrix * projMatrix);

        // Act - Use Span overload
        Span<Entity> results = stackalloc Entity[10];
        int count = partitioner.QueryFrustum(frustum, results);

        // Assert
        Assert.Equal(3, count);
        // Results should be sorted by entity ID
        Assert.Equal(entity2, results[0]); // ID 1
        Assert.Equal(entity3, results[1]); // ID 4
        Assert.Equal(entity1, results[2]); // ID 6
    }

    #endregion
}
