using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Extended tests for QuadtreePartitioner covering Span queries, deterministic mode, and additional edge cases.
/// </summary>
public class QuadtreePartitionerExtendedTests : IDisposable
{
    private readonly QuadtreePartitioner partitioner;
    private readonly Entity entity1 = new(1, 0);
    private readonly Entity entity2 = new(2, 0);
    private readonly Entity entity3 = new(3, 0);
    private readonly Entity entity4 = new(4, 0);

    public QuadtreePartitionerExtendedTests()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 6,
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

    #region Span QueryRadius Tests

    [Fact]
    public void QueryRadius_Span_WithNoEntities_ReturnsZero()
    {
        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryRadius(Vector3.Zero, 100f, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryRadius_Span_WithEntityInRange_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(50, 0, 50));

        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryRadius(Vector3.Zero, 100f, buffer);

        Assert.Equal(1, count);
        Assert.Equal(entity1, buffer[0]);
    }

    [Fact]
    public void QueryRadius_Span_WithMultipleEntities_ReturnsAll()
    {
        partitioner.Update(entity1, new Vector3(10, 0, 10));
        partitioner.Update(entity2, new Vector3(20, 0, 20));
        partitioner.Update(entity3, new Vector3(30, 0, 30));

        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryRadius(Vector3.Zero, 100f, buffer);

        Assert.Equal(3, count);
    }

    [Fact]
    public void QueryRadius_Span_WithBufferTooSmall_ReturnsNegativeOne()
    {
        partitioner.Update(entity1, new Vector3(10, 0, 10));
        partitioner.Update(entity2, new Vector3(20, 0, 20));
        partitioner.Update(entity3, new Vector3(30, 0, 30));

        Span<Entity> buffer = stackalloc Entity[2]; // Too small
        int count = partitioner.QueryRadius(Vector3.Zero, 100f, buffer);

        Assert.Equal(-1, count);
    }

    #endregion

    #region Span QueryBounds Tests

    [Fact]
    public void QueryBounds_Span_WithNoEntities_ReturnsZero()
    {
        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50),
            buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryBounds_Span_WithEntityInBounds_ReturnsEntity()
    {
        partitioner.Update(entity1, Vector3.Zero);

        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50),
            buffer);

        Assert.Equal(1, count);
        Assert.Equal(entity1, buffer[0]);
    }

    [Fact]
    public void QueryBounds_Span_WithMultipleEntities_ReturnsAll()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(10, 0, 10));
        partitioner.Update(entity3, new Vector3(20, 0, 20));

        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50),
            buffer);

        Assert.Equal(3, count);
    }

    [Fact]
    public void QueryBounds_Span_WithBufferTooSmall_ReturnsNegativeOne()
    {
        partitioner.Update(entity1, new Vector3(0, 0, 0));
        partitioner.Update(entity2, new Vector3(10, 0, 10));
        partitioner.Update(entity3, new Vector3(20, 0, 20));

        Span<Entity> buffer = stackalloc Entity[2]; // Too small
        int count = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50),
            buffer);

        Assert.Equal(-1, count);
    }

    #endregion

    #region Span QueryPoint Tests

    [Fact]
    public void QueryPoint_Span_WithNoEntities_ReturnsZero()
    {
        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryPoint(Vector3.Zero, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryPoint_Span_WithEntityInNode_ReturnsEntity()
    {
        partitioner.Update(entity1, new Vector3(10, 0, 10));

        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryPoint(Vector3.Zero, buffer);

        Assert.True(count >= 1);
    }

    [Fact]
    public void QueryPoint_Span_WithBufferTooSmall_ReturnsNegativeOne()
    {
        // Add multiple entities in the same node
        partitioner.Update(entity1, new Vector3(10, 0, 10));
        partitioner.Update(entity2, new Vector3(15, 0, 15));
        partitioner.Update(entity3, new Vector3(20, 0, 20));

        Span<Entity> buffer = stackalloc Entity[2]; // Too small
        int count = partitioner.QueryPoint(Vector3.Zero, buffer);

        Assert.Equal(-1, count);
    }

    #endregion

    #region Span QueryFrustum Tests

    [Fact]
    public void QueryFrustum_Span_WithNoEntities_ReturnsZero()
    {
        var frustum = CreateSimpleFrustum();
        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryFrustum(frustum, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void QueryFrustum_Span_WithEntityInFrustum_ReturnsEntity()
    {
        partitioner.Update(entity1, Vector3.Zero);

        var frustum = CreateSimpleFrustum();
        Span<Entity> buffer = stackalloc Entity[10];
        int count = partitioner.QueryFrustum(frustum, buffer);

        Assert.Equal(1, count);
        Assert.Equal(entity1, buffer[0]);
    }

    [Fact]
    public void QueryFrustum_Span_WithBufferTooSmall_ReturnsNegativeOne()
    {
        partitioner.Update(entity1, new Vector3(0, 0, -5));
        partitioner.Update(entity2, new Vector3(1, 0, -5));
        partitioner.Update(entity3, new Vector3(2, 0, -5));

        var frustum = CreateSimpleFrustum();
        Span<Entity> buffer = stackalloc Entity[2]; // Too small
        int count = partitioner.QueryFrustum(frustum, buffer);

        Assert.Equal(-1, count);
    }

    #endregion

    #region Update with Bounds Tests

    [Fact]
    public void Update_WithBoundsMoving_UpdatesCorrectly()
    {
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-10, -10, -10),
            Max = new Vector3(10, 10, 10)
        };

        partitioner.Update(entity1, Vector3.Zero, bounds);
        partitioner.Update(entity1, new Vector3(100, 0, 100), bounds);

        Assert.Equal(1, partitioner.EntityCount);
        var results = partitioner.QueryBounds(new Vector3(90, 0, 90), new Vector3(110, 0, 110)).ToList();
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void Update_SwitchingBetweenPointAndBounds_HandlesCorrectly()
    {
        // Start as point
        partitioner.Update(entity1, Vector3.Zero);
        Assert.Equal(1, partitioner.EntityCount);

        // Update to bounds
        var bounds = new SpatialBounds
        {
            Min = new Vector3(-50, -50, -50),
            Max = new Vector3(50, 50, 50)
        };
        partitioner.Update(entity1, Vector3.Zero, bounds);
        Assert.Equal(1, partitioner.EntityCount);

        // Back to point
        partitioner.Update(entity1, new Vector3(100, 0, 100));
        Assert.Equal(1, partitioner.EntityCount);
    }

    #endregion

    #region QueryFrustum Enumerable Tests

    [Fact]
    public void QueryFrustum_WithNoEntities_ReturnsEmpty()
    {
        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryFrustum_WithEntityInFrustum_ReturnsEntity()
    {
        partitioner.Update(entity1, Vector3.Zero);

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Contains(entity1, results);
    }

    [Fact]
    public void QueryFrustum_WithEntityBehindFrustum_DoesNotReturn()
    {
        // Entity behind camera
        partitioner.Update(entity1, new Vector3(0, 0, 100));

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryFrustum_WithMultipleEntities_ReturnsOnlyVisible()
    {
        partitioner.Update(entity1, new Vector3(0, 0, -5));   // In frustum
        partitioner.Update(entity2, new Vector3(0, 0, 100));  // Behind camera
        partitioner.Update(entity3, new Vector3(1, 0, -5));   // In frustum
        partitioner.Update(entity4, new Vector3(2, 0, -5));   // In frustum

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.Contains(entity4, results);
        Assert.DoesNotContain(entity2, results);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Constructor_WithInvalidConfig_ThrowsArgumentException()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = -1 // Invalid
        };

        Assert.Throws<ArgumentException>(() => new QuadtreePartitioner(config));
    }

    [Fact]
    public void Constructor_WithNodePoolingEnabled_CreatesSuccessfully()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 6,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            UseNodePooling = true
        };

        using var pooledPartitioner = new QuadtreePartitioner(config);
        Assert.Equal(0, pooledPartitioner.EntityCount);
    }

    [Fact]
    public void Constructor_WithNodePoolingDisabled_CreatesSuccessfully()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 6,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            UseNodePooling = false
        };

        using var nonPooledPartitioner = new QuadtreePartitioner(config);
        Assert.Equal(0, nonPooledPartitioner.EntityCount);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void Update_WithManyEntitiesAtSamePosition_HandlesCorrectly()
    {
        var position = new Vector3(100, 0, 100);

        for (int i = 0; i < 50; i++)
        {
            partitioner.Update(new Entity(100 + i, 0), position);
        }

        Assert.Equal(50, partitioner.EntityCount);
        var results = partitioner.QueryPoint(position).ToList();
        Assert.Equal(50, results.Count);
    }

    [Fact]
    public void Update_WithDeeplyNestedSubdivisions_HandlesCorrectly()
    {
        // Create entities at progressively smaller scales to trigger deep subdivision
        for (int i = 0; i < 100; i++)
        {
            var scale = 1000f / (float)Math.Pow(2, i / 10);
            var position = new Vector3(scale, 0, scale);
            partitioner.Update(new Entity(i + 1, 0), position);
        }

        Assert.Equal(100, partitioner.EntityCount);
    }

    #endregion

    #region Y-Coordinate Handling Tests

    [Fact]
    public void Update_IgnoresYCoordinate()
    {
        // Quadtree uses only X and Z, Y should be ignored
        partitioner.Update(entity1, new Vector3(50, 100, 50));
        partitioner.Update(entity2, new Vector3(50, -100, 50));

        // Both entities should map to the same 2D position
        var results = partitioner.QueryPoint(new Vector3(50, 0, 50)).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void Update_WithDifferentYValues_BothFoundInSameQuadrant()
    {
        partitioner.Update(entity1, new Vector3(10, 50, 10));
        partitioner.Update(entity2, new Vector3(10, -50, 10));

        var results = partitioner.QueryBounds(
            new Vector3(0, -100, 0),
            new Vector3(20, 100, 20)).ToList();

        Assert.Equal(2, results.Count);
    }

    #endregion

    #region Helper Methods

    private static Frustum CreateSimpleFrustum()
    {
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,  // 45 degree FOV
            1.0f,          // Aspect ratio
            0.1f,          // Near plane
            100.0f);       // Far plane

        var view = Matrix4x4.CreateLookAt(
            new Vector3(0, 0, -10),  // Camera position
            new Vector3(0, 0, 0),    // Look at origin
            new Vector3(0, 1, 0));   // Up vector

        return Frustum.FromMatrix(view * projection);
    }

    #endregion
}
