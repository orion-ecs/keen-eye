using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for the SpatialQueryApi public API.
/// </summary>
public class SpatialQueryApiTests : IDisposable
{
    private readonly World world;
    private readonly SpatialQueryApi spatial;

    public SpatialQueryApiTests()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin(new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = 100f,
                WorldMin = new Vector3(-1000, -1000, -1000),
                WorldMax = new Vector3(1000, 1000, 1000)
            }
        }));

        spatial = world.GetExtension<SpatialQueryApi>();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    #region QueryRadius Tests

    [Fact]
    public void QueryRadius_WithNoEntities_ReturnsEmpty()
    {
        var results = spatial.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryRadius_WithSpatiallyIndexedEntity_ReturnsEntity()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Trigger spatial index update
        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void QueryRadius_WithoutSpatialIndexedTag_NotReturned()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .Build(); // No SpatialIndexed tag

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 100f).ToList();

        Assert.DoesNotContain(entity, results);
    }

    [Fact]
    public void QueryRadius_Generic_FiltersByComponent()
    {
        // Entity with Velocity3D
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(1, 0, 0))
            .WithTag<SpatialIndexed>()
            .Build();

        // Entity without Velocity3D
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(60, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius<Velocity3D>(Vector3.Zero, 100f).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryRadius_AfterEntityMoves_UpdatesIndex()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(50, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Initially in range
        var results1 = spatial.QueryRadius(Vector3.Zero, 100f).ToList();
        Assert.Contains(entity, results1);

        // Move entity far away
        world.Set(entity, new Transform3D(new Vector3(500, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Now out of range
        var results2 = spatial.QueryRadius(Vector3.Zero, 100f).ToList();
        Assert.DoesNotContain(entity, results2);
    }

    #endregion

    #region QueryBounds Tests

    [Fact]
    public void QueryBounds_WithNoEntities_ReturnsEmpty()
    {
        var results = spatial.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryBounds_WithEntityInBounds_ReturnsEntity()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void QueryBounds_WithEntityOutOfBounds_NotReturned()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(500, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryBounds(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.DoesNotContain(entity, results);
    }

    [Fact]
    public void QueryBounds_Generic_FiltersByComponent()
    {
        // Entity with Velocity3D
        var entity1 = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(1, 0, 0))
            .WithTag<SpatialIndexed>()
            .Build();

        // Entity without Velocity3D
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryBounds<Velocity3D>(
            new Vector3(-50, -50, -50),
            new Vector3(50, 50, 50)).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryBounds_WithSpatialBoundsComponent_IndexesAABB()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new SpatialBounds
            {
                Min = new Vector3(-50, -50, -50),
                Max = new Vector3(50, 50, 50)
            })
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Query should find entity at edges of its bounds
        var results1 = spatial.QueryPoint(new Vector3(-40, 0, 0)).ToList();
        var results2 = spatial.QueryPoint(new Vector3(40, 0, 0)).ToList();

        Assert.Contains(entity, results1);
        Assert.Contains(entity, results2);
    }

    #endregion

    #region QueryPoint Tests

    [Fact]
    public void QueryPoint_WithNoEntities_ReturnsEmpty()
    {
        var results = spatial.QueryPoint(Vector3.Zero).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryPoint_WithEntityNearby_ReturnsEntity()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(25, 25, 25), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Same grid cell (0-100 range)
        var results = spatial.QueryPoint(Vector3.Zero).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void QueryPoint_Generic_FiltersByComponent()
    {
        // Entity with Velocity3D
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(25, 0, 0), Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(1, 0, 0))
            .WithTag<SpatialIndexed>()
            .Build();

        // Entity without Velocity3D
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(30, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryPoint<Velocity3D>(Vector3.Zero).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    #endregion

    #region EntityCount Tests

    [Fact]
    public void EntityCount_InitiallyZero()
    {
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void EntityCount_AfterAddingEntity_Increments()
    {
        world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        Assert.Equal(1, spatial.EntityCount);
    }

    [Fact]
    public void EntityCount_AfterDespawn_Decrements()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        world.Despawn(entity);
        world.Update(0.016f);

        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void EntityCount_WithMultipleEntities_Accurate()
    {
        world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Spawn()
            .With(new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Spawn()
            .With(new Transform3D(new Vector3(200, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        Assert.Equal(3, spatial.EntityCount);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SpatialIndex_OnlyTracksEntitiesWithTag()
    {
        // Create 5 entities, only 3 with SpatialIndexed tag
        world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .Build(); // No tag

        world.Spawn()
            .With(new Transform3D(new Vector3(20, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Spawn()
            .With(new Transform3D(new Vector3(30, 0, 0), Quaternion.Identity, Vector3.One))
            .Build(); // No tag

        world.Spawn()
            .With(new Transform3D(new Vector3(40, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Only 3 entities should be indexed
        Assert.Equal(3, spatial.EntityCount);
    }

    [Fact]
    public void SpatialIndex_AddingTagDynamically_IndexesEntity()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build(); // No tag initially

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Add tag dynamically
        world.Add(entity, new SpatialIndexed());
        world.Update(0.016f);

        Assert.Equal(1, spatial.EntityCount);
    }

    [Fact]
    public void SpatialIndex_RemovingTag_RemovesFromIndex()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        // Remove tag
        world.Remove<SpatialIndexed>(entity);
        world.Update(0.016f);

        // Entity should be removed from index
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void SpatialIndex_MultipleUpdatesPerFrame_HandlesCorrectly()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Move entity multiple times
        world.Set(entity, new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One));
        world.Set(entity, new Transform3D(new Vector3(200, 0, 0), Quaternion.Identity, Vector3.One));
        world.Set(entity, new Transform3D(new Vector3(300, 0, 0), Quaternion.Identity, Vector3.One));

        world.Update(0.016f);

        // Should find entity at final position
        var results = spatial.QueryPoint(new Vector3(300, 0, 0)).ToList();
        Assert.Contains(entity, results);

        // Should not find at old positions
        var oldResults = spatial.QueryPoint(Vector3.Zero).ToList();
        Assert.DoesNotContain(entity, oldResults);
    }

    #endregion
}
