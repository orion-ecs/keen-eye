using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial;
using KeenEyes.Spatial.Partitioning;
using Xunit;

namespace KeenEyes.Spatial.Tests.Partitioning;

public class FrustumCullingTests
{
    [Fact]
    public void GridPartitioner_QueryFrustum_ReturnsEntitiesInFrustum()
    {
        var config = new GridConfig { CellSize = 10.0f };
        using var partitioner = new GridPartitioner(config);

        // Add entities in different positions
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);
        var e3 = new Entity(3, 0);

        partitioner.Update(e1, new Vector3(0, 0, 0));    // Inside frustum (at origin)
        partitioner.Update(e2, new Vector3(0, 0, -5));   // Inside frustum (closer to camera)
        partitioner.Update(e3, new Vector3(0, 0, 100));  // Outside frustum (way behind)

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        // Should return entities in front of camera
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);
    }

    [Fact]
    public void QuadtreePartitioner_QueryFrustum_ReturnsEntitiesInFrustum()
    {
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Add entities in different positions
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);
        var e3 = new Entity(3, 0);

        partitioner.Update(e1, new Vector3(0, 0, 0));    // Inside frustum (at origin)
        partitioner.Update(e2, new Vector3(0, 0, -5));   // Inside frustum (closer to camera)
        partitioner.Update(e3, new Vector3(0, 0, 100));  // Outside frustum (way behind)

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        // Should return entities visible to camera
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);
    }

    [Fact]
    public void OctreePartitioner_QueryFrustum_ReturnsEntitiesInFrustum()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8
        };
        using var partitioner = new OctreePartitioner(config);

        // Add entities in different positions
        var e1 = new Entity(1, 0);
        var e2 = new Entity(2, 0);
        var e3 = new Entity(3, 0);

        partitioner.Update(e1, new Vector3(0, 0, 0));    // Inside frustum (at origin)
        partitioner.Update(e2, new Vector3(0, 0, -5));   // Inside frustum (closer to camera)
        partitioner.Update(e3, new Vector3(0, 0, 100));  // Outside frustum (way behind)

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        // Should return entities visible to camera
        Assert.Contains(e1, results);
        Assert.Contains(e2, results);
        Assert.DoesNotContain(e3, results);
    }

    [Fact]
    public void OctreePartitioner_QueryFrustum_WithManyEntities_CullsCorrectly()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8
        };
        using var partitioner = new OctreePartitioner(config);

        // Add entities in a grid pattern
        var entitiesInFront = new List<Entity>();
        var entitiesBehind = new List<Entity>();

        int id = 1;
        // Use smaller grid that fits within frustum FOV at this distance
        for (int x = -2; x <= 2; x += 2)
        {
            for (int y = -2; y <= 2; y += 2)
            {
                // Entities in front of camera (between camera and look-at point)
                var e1 = new Entity(id++, 0);
                partitioner.Update(e1, new Vector3(x, y, -5));
                entitiesInFront.Add(e1);

                // Entities behind camera (behind camera position)
                var e2 = new Entity(id++, 0);
                partitioner.Update(e2, new Vector3(x, y, -15));
                entitiesBehind.Add(e2);
            }
        }

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToHashSet();

        // All entities in front should be returned
        foreach (var entity in entitiesInFront)
        {
            Assert.Contains(entity, results);
        }

        // All entities behind should be culled
        foreach (var entity in entitiesBehind)
        {
            Assert.DoesNotContain(entity, results);
        }
    }

    [Fact]
    public void QueryFrustum_EmptyPartitioner_ReturnsEmpty()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8
        };
        using var partitioner = new OctreePartitioner(config);

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryFrustum_AllEntitiesOutside_ReturnsEmpty()
    {
        var config = new OctreeConfig
        {
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000),
            MaxDepth = 8,
            MaxEntitiesPerNode = 8
        };
        using var partitioner = new OctreePartitioner(config);

        // Add entities far from frustum
        for (int i = 0; i < 10; i++)
        {
            var entity = new Entity(i, 0);
            partitioner.Update(entity, new Vector3(1000, 1000, 1000)); // Far away
        }

        var frustum = CreateSimpleFrustum();
        var results = partitioner.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    /// <summary>
    /// Creates a simple frustum for testing purposes.
    /// Camera at (0, 0, -10) looking toward origin.
    /// </summary>
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

    [Fact]
    public void QuadtreePartitioner_QueryFrustum_WithSubdividedTree_QueriesChildren()
    {
        // Create a tree with subdivision to hit recursive QueryFrustum path
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            MaxDepth = 4,
            MaxEntitiesPerNode = 4  // Low threshold to force subdivision
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Add 20 entities spread across the world to trigger subdivision
        var entities = new List<Entity>();
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(i + 1, 0);
            var x = -80 + (i * 8);  // Spread from -80 to 72
            partitioner.Update(entity, new Vector3(x, 0, 0));
            entities.Add(entity);
        }

        // Create a frustum that captures the left half of the world
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,    // 45° FOV
            1.0f,            // Aspect ratio
            1.0f,            // Near plane
            200.0f);         // Far plane

        var view = Matrix4x4.CreateLookAt(
            new Vector3(-50, 0, -50),  // Camera on left side
            new Vector3(-50, 0, 0),    // Looking toward left entities
            new Vector3(0, 1, 0));

        var frustum = Frustum.FromMatrix(view * projection);
        var results = partitioner.QueryFrustum(frustum).ToList();

        // Should find entities on the left side, miss entities on the right
        Assert.NotEmpty(results);
        Assert.True(results.Count < entities.Count);  // Not all entities
    }

    [Fact]
    public void QuadtreePartitioner_QueryFrustum_WithNonOverlappingNode_EarlyExits()
    {
        // Create a tree with entities, then query with frustum that doesn't overlap
        var config = new QuadtreeConfig
        {
            WorldMin = new Vector3(-100, 0, -100),
            WorldMax = new Vector3(100, 0, 100),
            MaxDepth = 4,
            MaxEntitiesPerNode = 4
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Add entities only on the right side (X > 0)
        for (int i = 0; i < 10; i++)
        {
            var entity = new Entity(i + 1, 0);
            partitioner.Update(entity, new Vector3(50, 0, i * 5));
        }

        // Create a frustum pointing to the LEFT side (won't overlap entities on right)
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 4,    // 45° FOV
            1.0f,
            1.0f,
            100.0f);

        var view = Matrix4x4.CreateLookAt(
            new Vector3(-50, 0, -50),  // Camera on left
            new Vector3(-50, 0, 0),    // Looking left
            new Vector3(0, 1, 0));

        var frustum = Frustum.FromMatrix(view * projection);
        var results = partitioner.QueryFrustum(frustum).ToList();

        // Should return empty - frustum doesn't overlap nodes with entities
        Assert.Empty(results);
    }
}
