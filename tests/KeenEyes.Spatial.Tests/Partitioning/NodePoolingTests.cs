using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for memory pooling in quadtree and octree partitioners.
/// </summary>
public class NodePoolingTests
{
    #region Quadtree Pooling Tests

    [Fact]
    public void QuadtreePartitioner_WithPoolingEnabled_WorksCorrectly()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add enough entities to trigger subdivision (>4 entities per node)
        var entities = new List<Entity>();
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i + 1, 0);
            partitioner.Update(entity, new Vector3(i * 10, 0, i * 10));
            entities.Add(entity);
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Verify queries work correctly with pooling
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(250, 50, 250)).ToList();

        Assert.Equal(20, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void QuadtreePartitioner_WithPoolingDisabled_WorksCorrectly()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 4,
            UseNodePooling = false
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add enough entities to trigger subdivision (>4 entities per node)
        var entities = new List<Entity>();
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i + 1, 0);
            partitioner.Update(entity, new Vector3(i * 10, 0, i * 10));
            entities.Add(entity);
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Verify queries work correctly without pooling
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(250, 50, 250)).ToList();

        Assert.Equal(20, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void QuadtreePartitioner_WithPooling_ClearReturnsNodesToPool()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add entities to trigger subdivision
        for (int i = 0; i < 20; i++)
        {
            partitioner.Update(new Entity(i + 1, 0), new Vector3(i * 10, 0, i * 10));
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Clear should return nodes to pool (verified by not throwing)
        partitioner.Clear();

        Assert.Equal(0, partitioner.EntityCount);

        // Add new entities - should reuse pooled nodes
        for (int i = 0; i < 20; i++)
        {
            partitioner.Update(new Entity(i + 100, 0), new Vector3(i * 10, 0, i * 10));
        }

        Assert.Equal(20, partitioner.EntityCount);
    }

    [Fact]
    public void QuadtreePartitioner_WithPooling_SubdivisionCyclesWorkCorrectly()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Perform multiple subdivision cycles
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Add entities to trigger subdivision
            var entities = new List<Entity>();
            for (int i = 0; i < 20; i++)
            {
                var entity = new Entity(cycle * 100 + i + 1, 0);
                partitioner.Update(entity, new Vector3(i * 10, 0, i * 10));
                entities.Add(entity);
            }

            Assert.Equal(20, partitioner.EntityCount);

            // Verify queries
            var results = partitioner.QueryBounds(
                new Vector3(-50, -50, -50),
                new Vector3(250, 50, 250)).ToList();

            Assert.Equal(20, results.Count);

            // Clear for next cycle
            partitioner.Clear();
        }
    }

    [Fact]
    public void QuadtreePartitioner_WithPooling_DeepSubdivisionWorks()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 2,  // Low threshold to force deep subdivision
            UseNodePooling = true
        };

        using var partitioner = new QuadtreePartitioner(config);

        // Add many entities in a small area to force deep subdivision
        var entities = new List<Entity>();
        for (int i = 0; i < 50; i++)
        {
            var entity = new Entity(i + 1, 0);
            // Cluster entities in a small area to force deep tree
            partitioner.Update(entity, new Vector3(i * 2, 0, i * 2));
            entities.Add(entity);
        }

        Assert.Equal(50, partitioner.EntityCount);

        // Verify all entities are queryable
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(200, 50, 200)).ToList();

        Assert.Equal(50, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    #endregion

    #region Octree Pooling Tests

    [Fact]
    public void OctreePartitioner_WithPoolingEnabled_WorksCorrectly()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 6,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add enough entities to trigger subdivision (>4 entities per node)
        var entities = new List<Entity>();
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i + 1, 0);
            partitioner.Update(entity, new Vector3(i * 10, i * 10, i * 10));
            entities.Add(entity);
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Verify queries work correctly with pooling
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(250, 250, 250)).ToList();

        Assert.Equal(20, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void OctreePartitioner_WithPoolingDisabled_WorksCorrectly()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 6,
            MaxEntitiesPerNode = 4,
            UseNodePooling = false
        };

        using var partitioner = new OctreePartitioner(config);

        // Add enough entities to trigger subdivision (>4 entities per node)
        var entities = new List<Entity>();
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i + 1, 0);
            partitioner.Update(entity, new Vector3(i * 10, i * 10, i * 10));
            entities.Add(entity);
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Verify queries work correctly without pooling
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(250, 250, 250)).ToList();

        Assert.Equal(20, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void OctreePartitioner_WithPooling_ClearReturnsNodesToPool()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 6,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add entities to trigger subdivision
        for (int i = 0; i < 20; i++)
        {
            partitioner.Update(new Entity(i + 1, 0), new Vector3(i * 10, i * 10, i * 10));
        }

        Assert.Equal(20, partitioner.EntityCount);

        // Clear should return nodes to pool (verified by not throwing)
        partitioner.Clear();

        Assert.Equal(0, partitioner.EntityCount);

        // Add new entities - should reuse pooled nodes
        for (int i = 0; i < 20; i++)
        {
            partitioner.Update(new Entity(i + 100, 0), new Vector3(i * 10, i * 10, i * 10));
        }

        Assert.Equal(20, partitioner.EntityCount);
    }

    [Fact]
    public void OctreePartitioner_WithPooling_SubdivisionCyclesWorkCorrectly()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 6,
            MaxEntitiesPerNode = 4,
            UseNodePooling = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Perform multiple subdivision cycles
        for (int cycle = 0; cycle < 5; cycle++)
        {
            // Add entities to trigger subdivision
            var entities = new List<Entity>();
            for (int i = 0; i < 20; i++)
            {
                var entity = new Entity(cycle * 100 + i + 1, 0);
                partitioner.Update(entity, new Vector3(i * 10, i * 10, i * 10));
                entities.Add(entity);
            }

            Assert.Equal(20, partitioner.EntityCount);

            // Verify queries
            var results = partitioner.QueryBounds(
                new Vector3(-50, -50, -50),
                new Vector3(250, 250, 250)).ToList();

            Assert.Equal(20, results.Count);

            // Clear for next cycle
            partitioner.Clear();
        }
    }

    [Fact]
    public void OctreePartitioner_WithPooling_DeepSubdivisionWorks()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 6,
            MaxEntitiesPerNode = 2,  // Low threshold to force deep subdivision
            UseNodePooling = true
        };

        using var partitioner = new OctreePartitioner(config);

        // Add many entities in a small area to force deep subdivision
        var entities = new List<Entity>();
        for (int i = 0; i < 50; i++)
        {
            var entity = new Entity(i + 1, 0);
            // Cluster entities in a small area to force deep tree
            partitioner.Update(entity, new Vector3(i * 2, i * 2, i * 2));
            entities.Add(entity);
        }

        Assert.Equal(50, partitioner.EntityCount);

        // Verify all entities are queryable
        var results = partitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(200, 200, 200)).ToList();

        Assert.Equal(50, results.Count);
        foreach (var entity in entities)
        {
            Assert.Contains(entity, results);
        }
    }

    #endregion

    #region Pooling Configuration Tests

    [Fact]
    public void QuadtreeConfig_DefaultUsesPooling()
    {
        var config = new QuadtreeConfig();
        Assert.True(config.UseNodePooling);
    }

    [Fact]
    public void OctreeConfig_DefaultUsesPooling()
    {
        var config = new OctreeConfig();
        Assert.True(config.UseNodePooling);
    }

    #endregion
}
