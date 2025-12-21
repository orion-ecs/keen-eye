using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for SpatialIndexed tag component behavior.
/// </summary>
public partial class SpatialIndexedTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Tag Component Behavior

    [Fact]
    public void SpatialIndexed_IsTagComponent_CanBeAddedWithoutValue()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        Assert.True(world.Has<SpatialIndexed>(entity));
    }

    [Fact]
    public void SpatialIndexed_CanBeAddedDynamically()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        Assert.False(world.Has<SpatialIndexed>(entity));

        world.Add(entity, new SpatialIndexed());

        Assert.True(world.Has<SpatialIndexed>(entity));
    }

    [Fact]
    public void SpatialIndexed_CanBeRemoved()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        Assert.True(world.Has<SpatialIndexed>(entity));

        world.Remove<SpatialIndexed>(entity);

        Assert.False(world.Has<SpatialIndexed>(entity));
    }

    [Fact]
    public void SpatialIndexed_MultipleEntities_CanHaveTag()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var entity3 = world.Spawn()
            .With(new Transform3D(new Vector3(200, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        Assert.True(world.Has<SpatialIndexed>(entity1));
        Assert.True(world.Has<SpatialIndexed>(entity2));
        Assert.True(world.Has<SpatialIndexed>(entity3));

        // Use all entities to avoid IDE0059 warning
        Assert.NotEqual(entity1, entity2);
        Assert.NotEqual(entity2, entity3);
    }

    #endregion

    #region Query Integration

    [Fact]
    public void SpatialIndexed_WithTransform_EnablesSpatialQueries()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void SpatialIndexed_WithoutTransform_DoesNotEnableSpatialQueries()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void SpatialIndexed_RemovedFromEntity_RemovesFromSpatialQueries()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        world.Remove<SpatialIndexed>(entity);

        Assert.Equal(0, spatial.EntityCount);
    }

    #endregion

    #region Multiple Component Combinations

    [Fact]
    public void SpatialIndexed_WithTransformAndBounds_IndexesCorrectly()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new SpatialBounds(new Vector3(-5, -5, -5), new Vector3(5, 5, 5)))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void SpatialIndexed_CanBeQueriedWithOtherComponents()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var results = world.Query<Transform3D, SpatialIndexed>().ToList();

        Assert.Single(results);
        Assert.Contains(entity, results);
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void SpatialIndexed_SurvivesWorldUpdate()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        for (int i = 0; i < 10; i++)
        {
            world.Update(0.016f);
        }

        Assert.True(world.Has<SpatialIndexed>(entity));
    }

    [Fact]
    public void SpatialIndexed_RemovedWhenEntityDespawned()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.True(world.Has<SpatialIndexed>(entity));

        world.Despawn(entity);

        Assert.False(world.IsAlive(entity));
    }

    #endregion

    #region Multiple Strategies

    [Fact]
    public void SpatialIndexed_WorksWithGridStrategy()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Grid };
        world.InstallPlugin(new SpatialPlugin(config));

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void SpatialIndexed_WorksWithQuadtreeStrategy()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Quadtree };
        world.InstallPlugin(new SpatialPlugin(config));

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void SpatialIndexed_WorksWithOctreeStrategy()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Octree };
        world.InstallPlugin(new SpatialPlugin(config));

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void SpatialIndexed_ManyEntities_AllIndexedCorrectly()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entities = new List<Entity>();
        for (int i = 0; i < 500; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
            entities.Add(entity);
        }

        world.Update(0.016f);

        Assert.Equal(500, spatial.EntityCount);

        // Verify all entities can be queried
        foreach (var entity in entities)
        {
            Assert.True(world.Has<SpatialIndexed>(entity));
        }
    }

    [Fact]
    public void SpatialIndexed_AddRemoveCycle_WorksCorrectly()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        for (int i = 0; i < 10; i++)
        {
            world.Add(entity, new SpatialIndexed());
            world.Update(0.016f);
            Assert.Equal(1, spatial.EntityCount);

            world.Remove<SpatialIndexed>(entity);
            Assert.Equal(0, spatial.EntityCount);
        }
    }

    #endregion
}
