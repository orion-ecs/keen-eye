using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Tests for loose bounds functionality in QuadtreePartitioner.
/// </summary>
public class LooseQuadtreeTests : IDisposable
{
    private readonly QuadtreePartitioner partitioner;
    private readonly QuadtreePartitioner loosePartitioner;
    private readonly Entity entity1 = new(1, 0);
    private readonly Entity entity2 = new(2, 0);

    public LooseQuadtreeTests()
    {
        // Standard tight bounds config
        var tightConfig = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            UseLooseBounds = false
        };
        partitioner = new QuadtreePartitioner(tightConfig);

        // Loose bounds config with 2x factor
        var looseConfig = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000),
            UseLooseBounds = true,
            LoosenessFactor = 2.0f
        };
        loosePartitioner = new QuadtreePartitioner(looseConfig);
    }

    public void Dispose()
    {
        partitioner.Dispose();
        loosePartitioner.Dispose();
    }

    #region Loose Bounds Behavior Tests

    [Fact]
    public void LooseBounds_SmallMovement_DoesNotTriggerReposition()
    {
        // Insert entity at origin
        loosePartitioner.Update(entity1, new Vector3(0, 0, 0));

        // Simulate small movement (should stay within loose bounds)
        loosePartitioner.Update(entity1, new Vector3(10, 0, 10));

        // Entity should still be queryable
        var results = loosePartitioner.QueryRadius(new Vector3(10, 0, 10), 50f).ToList();
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void LooseBounds_LargeMovement_TriggersReposition()
    {
        // Insert entity at origin
        loosePartitioner.Update(entity1, new Vector3(0, 0, 0));

        // Move entity far away (outside loose bounds)
        loosePartitioner.Update(entity1, new Vector3(500, 0, 500));

        // Entity should be queryable at new position
        var results = loosePartitioner.QueryRadius(new Vector3(500, 0, 500), 50f).ToList();
        Assert.Contains(entity1, results);

        // Should not be queryable at old position
        var oldResults = loosePartitioner.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.DoesNotContain(entity1, oldResults);
    }

    [Fact]
    public void LooseBounds_MultipleSmallUpdates_StaysInSameNode()
    {
        // Insert entity
        loosePartitioner.Update(entity1, new Vector3(0, 0, 0));

        // Perform multiple small updates
        for (int i = 1; i <= 10; i++)
        {
            loosePartitioner.Update(entity1, new Vector3(i * 2, 0, i * 2));
        }

        // Entity should still be queryable
        var results = loosePartitioner.QueryRadius(new Vector3(20, 0, 20), 50f).ToList();
        Assert.Contains(entity1, results);
        Assert.Equal(1, loosePartitioner.EntityCount);
    }

    [Fact]
    public void TightBounds_SmallMovement_MayTriggerReposition()
    {
        // With tight bounds, even small movements can trigger repositioning
        // when entities cross node boundaries

        // Insert entity near a quadrant boundary
        partitioner.Update(entity1, new Vector3(-5, 0, -5));

        // Small movement that crosses into another quadrant
        partitioner.Update(entity1, new Vector3(5, 0, 5));

        // Entity should still be queryable at new position
        var results = partitioner.QueryRadius(new Vector3(5, 0, 5), 50f).ToList();
        Assert.Contains(entity1, results);
    }

    #endregion

    #region Query Correctness Tests

    [Fact]
    public void LooseBounds_QueryRadius_ReturnsCorrectEntities()
    {
        loosePartitioner.Update(entity1, new Vector3(0, 0, 0));
        loosePartitioner.Update(entity2, new Vector3(100, 0, 100));

        var results = loosePartitioner.QueryRadius(Vector3.Zero, 50f).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void LooseBounds_QueryBounds_ReturnsCorrectEntities()
    {
        loosePartitioner.Update(entity1, new Vector3(0, 0, 0));
        loosePartitioner.Update(entity2, new Vector3(200, 0, 200));

        var results = loosePartitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void LooseBounds_AfterMultipleUpdates_QueriesStillCorrect()
    {
        // Update entity1 multiple times
        for (int i = 0; i < 5; i++)
        {
            loosePartitioner.Update(entity1, new Vector3(i * 10, 0, i * 10));
        }

        // Final position should be (40, 0, 40)
        var results = loosePartitioner.QueryRadius(new Vector3(40, 0, 40), 20f).ToList();
        Assert.Contains(entity1, results);
    }

    #endregion

    #region Subdivision Tests

    [Fact]
    public void LooseBounds_WithManyEntities_SubdividesCorrectly()
    {
        // Add enough entities to trigger subdivision
        for (int i = 0; i < 20; i++)
        {
            var entity = new Entity(100 + i, 0);
            loosePartitioner.Update(entity, new Vector3(i * 10, 0, i * 10));
        }

        Assert.Equal(20, loosePartitioner.EntityCount);

        // All entities should be queryable
        var results = loosePartitioner.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(250, 50, 250)).ToList();

        Assert.Equal(20, results.Count);
    }

    #endregion

    #region Configuration Validation Tests

    [Fact]
    public void Config_InvalidLoosenessFactor_ThrowsOnConstruction()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 0.5f // Too small
        };

        Assert.Throws<ArgumentException>(() => new QuadtreePartitioner(config));
    }

    [Fact]
    public void Config_ValidLoosenessFactor_ConstructsSuccessfully()
    {
        var config = new QuadtreeConfig
        {
            UseLooseBounds = true,
            LoosenessFactor = 2.5f
        };

        var p = new QuadtreePartitioner(config);
        Assert.NotNull(p);
        p.Dispose();
    }

    #endregion
}
