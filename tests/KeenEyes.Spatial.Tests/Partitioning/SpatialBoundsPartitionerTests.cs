using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Partitioning;

namespace KeenEyes.Spatial.Tests.Partitioning;

/// <summary>
/// Regression tests for spatial partitioner bounds handling.
/// </summary>
/// <remarks>
/// Covers two defects that caused false negatives in tree partitioners relative to
/// <see cref="GridPartitioner"/>:
/// <list type="bullet">
/// <item>#1125 - loose quadtree/octree queries pruned traversal by strict node bounds,
/// so an entity legitimately stored in a node via its loose bounds was missed when the
/// query box only overlapped the loose region.</item>
/// <item>#1129 - bounded <c>Update</c> discarded the supplied bounds and filtered queries
/// by the stored center, so a large entity whose center is outside the query box but whose
/// footprint overlaps it was missed on the quadtree/octree yet returned by the grid.</item>
/// </list>
/// </remarks>
public class SpatialBoundsPartitionerTests
{
    private readonly Entity building = new(1, 0);

    #region #1129 - Bounded Update Filters By Footprint, Not Center

    [Fact]
    public void QueryBounds_QuadtreeEntityFootprintOverlapsButCenterOutside_ReturnsEntity()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000)
        };
        using var partitioner = new QuadtreePartitioner(config);

        // 40x40 building centered at the origin: footprint spans (-20,-20)..(20,20).
        var bounds = SpatialBounds.FromCenterAndExtents(Vector3.Zero, new Vector3(20, 20, 20));
        partitioner.Update(building, Vector3.Zero, bounds);

        // Query box lies wholly inside the footprint but does not contain the center.
        var results = partitioner.QueryBounds(
            new Vector3(5, -100, 5),
            new Vector3(15, 100, 15)).ToList();

        Assert.Contains(building, results);
    }

    [Fact]
    public void QueryBounds_OctreeEntityFootprintOverlapsButCenterOutside_ReturnsEntity()
    {
        var config = new OctreeConfig
        {
            MaxDepth = 6,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000)
        };
        using var partitioner = new OctreePartitioner(config);

        // 40x40x40 building centered at the origin: footprint spans (-20,-20,-20)..(20,20,20).
        var bounds = SpatialBounds.FromCenterAndExtents(Vector3.Zero, new Vector3(20, 20, 20));
        partitioner.Update(building, Vector3.Zero, bounds);

        var results = partitioner.QueryBounds(
            new Vector3(5, 5, 5),
            new Vector3(15, 15, 15)).ToList();

        Assert.Contains(building, results);
    }

    [Fact]
    public void QueryBounds_GridEntityFootprintOverlapsButCenterOutside_ReturnsEntity()
    {
        // Control: the grid partitioner already returns the entity. The tree partitioners
        // must match this behavior so switching SpatialStrategy does not change correctness.
        var config = new GridConfig { CellSize = 10f };
        using var partitioner = new GridPartitioner(config);

        var bounds = SpatialBounds.FromCenterAndExtents(Vector3.Zero, new Vector3(20, 20, 20));
        partitioner.Update(building, Vector3.Zero, bounds);

        var results = partitioner.QueryBounds(
            new Vector3(5, 5, 5),
            new Vector3(15, 15, 15)).ToList();

        Assert.Contains(building, results);
    }

    [Fact]
    public void QueryBoundsSpan_QuadtreeEntityFootprintOverlapsButCenterOutside_ReturnsEntity()
    {
        var config = new QuadtreeConfig
        {
            MaxDepth = 8,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, 0, -1000),
            WorldMax = new Vector3(1000, 0, 1000)
        };
        using var partitioner = new QuadtreePartitioner(config);

        var bounds = SpatialBounds.FromCenterAndExtents(Vector3.Zero, new Vector3(20, 20, 20));
        partitioner.Update(building, Vector3.Zero, bounds);

        Span<Entity> buffer = stackalloc Entity[8];
        int count = partitioner.QueryBounds(
            new Vector3(5, -100, 5),
            new Vector3(15, 100, 15),
            buffer);

        Assert.Equal(1, count);
        Assert.Equal(building, buffer[0]);
    }

    [Fact]
    public void QueryBoundsSpan_OctreeEntityFootprintOverlapsButCenterOutside_ReturnsEntity()
    {
        var config = new OctreeConfig
        {
            MaxDepth = 6,
            MaxEntitiesPerNode = 8,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000)
        };
        using var partitioner = new OctreePartitioner(config);

        var bounds = SpatialBounds.FromCenterAndExtents(Vector3.Zero, new Vector3(20, 20, 20));
        partitioner.Update(building, Vector3.Zero, bounds);

        Span<Entity> buffer = stackalloc Entity[8];
        int count = partitioner.QueryBounds(
            new Vector3(5, 5, 5),
            new Vector3(15, 15, 15),
            buffer);

        Assert.Equal(1, count);
        Assert.Equal(building, buffer[0]);
    }

    #endregion

    #region #1125 - Loose Query Traversal Prunes By Loose Bounds

    [Fact]
    public void QueryBounds_LooseQuadtreeEntityInLooseRegionOfNode_ReturnsEntity()
    {
        // A one-level subdivided loose quadtree whose NW leaf covers (0,0)..(8,8).
        var config = new QuadtreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 4,
            WorldMin = new Vector3(0, 0, 0),
            WorldMax = new Vector3(16, 0, 16),
            UseLooseBounds = true,
            LoosenessFactor = 2.0f
        };
        using var partitioner = new QuadtreePartitioner(config);

        // Five entities force the root to subdivide exactly once (one per quadrant + spare).
        var target = new Entity(1, 0);
        partitioner.Update(target, new Vector3(4, 0, 4));   // NW leaf (0,0)..(8,8)
        partitioner.Update(new Entity(2, 0), new Vector3(12, 0, 4));   // NE
        partitioner.Update(new Entity(3, 0), new Vector3(4, 0, 12));   // SW
        partitioner.Update(new Entity(4, 0), new Vector3(12, 0, 12));  // SE
        partitioner.Update(new Entity(5, 0), new Vector3(13, 0, 5));   // NE

        // Move target to (10,4): outside the NW leaf's strict bounds (x > 8) but inside its
        // loose bounds (x in [-4,12]), so it stays stored in the NW leaf.
        partitioner.Update(target, new Vector3(10, 0, 4));

        // Query box covers the target's actual position but only overlaps the NW leaf's loose
        // region. Strict-bounds pruning would skip the NW leaf and miss the target.
        var results = partitioner.QueryBounds(
            new Vector3(9, -1, 3),
            new Vector3(11, 1, 5)).ToList();

        Assert.Contains(target, results);
    }

    [Fact]
    public void QueryBounds_LooseOctreeEntityInLooseRegionOfNode_ReturnsEntity()
    {
        // A one-level subdivided loose octree whose "---" octant covers (0,0,0)..(8,8,8).
        var config = new OctreeConfig
        {
            MaxDepth = 4,
            MaxEntitiesPerNode = 4,
            WorldMin = new Vector3(0, 0, 0),
            WorldMax = new Vector3(16, 16, 16),
            UseLooseBounds = true,
            LoosenessFactor = 2.0f
        };
        using var partitioner = new OctreePartitioner(config);

        // Five entities force the root to subdivide exactly once.
        var target = new Entity(1, 0);
        partitioner.Update(target, new Vector3(4, 4, 4));       // octant (0,0,0)..(8,8,8)
        partitioner.Update(new Entity(2, 0), new Vector3(12, 4, 4));
        partitioner.Update(new Entity(3, 0), new Vector3(4, 12, 4));
        partitioner.Update(new Entity(4, 0), new Vector3(4, 4, 12));
        partitioner.Update(new Entity(5, 0), new Vector3(12, 12, 12));

        // Move target to (10,4,4): outside the octant's strict bounds (x > 8) but inside its
        // loose bounds (x in [-4,12]), so it stays stored in that octant.
        partitioner.Update(target, new Vector3(10, 4, 4));

        var results = partitioner.QueryBounds(
            new Vector3(9, 3, 3),
            new Vector3(11, 5, 5)).ToList();

        Assert.Contains(target, results);
    }

    #endregion
}
