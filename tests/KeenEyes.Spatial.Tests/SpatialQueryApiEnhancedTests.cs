using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Additional tests for SpatialQueryApi covering QueryFrustum variants and additional edge cases.
/// </summary>
public class SpatialQueryApiEnhancedTests : IDisposable
{
    private readonly World world;
    private readonly SpatialQueryApi spatial;

    public SpatialQueryApiEnhancedTests()
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

    #region QueryFrustum Enumerable Tests

    [Fact]
    public void QueryFrustum_WithNoEntities_ReturnsEmpty()
    {
        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryFrustum_WithEntityInFrustum_ReturnsEntity()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum(frustum).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void QueryFrustum_WithEntityBehindCamera_DoesNotReturn()
    {
        // Entity behind camera
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 100), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum(frustum).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryFrustum_WithMultipleEntities_ReturnsOnlyVisible()
    {
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, -5), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 100), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var entity3 = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, -10), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum(frustum).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
        Assert.DoesNotContain(entity2, results);
    }

    #endregion

    #region QueryFrustum Generic Tests

    [Fact]
    public void QueryFrustum_Generic_FiltersByComponent()
    {
        // Entity with Velocity3D in frustum
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, -5), Quaternion.Identity, Vector3.One))
            .With(new Velocity3D(1, 0, 0))
            .WithTag<SpatialIndexed>()
            .Build();

        // Entity without Velocity3D in frustum
        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(1, 0, -5), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum<Velocity3D>(frustum).ToList();

        Assert.Contains(entity1, results);
        Assert.DoesNotContain(entity2, results);
    }

    [Fact]
    public void QueryFrustum_Generic_WithNoMatchingComponents_ReturnsEmpty()
    {
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var frustum = CreateSimpleFrustum();
        var results = spatial.QueryFrustum<Velocity3D>(frustum).ToList();

        Assert.Empty(results);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void QueryRadius_WithZeroRadius_FindsEntitiesInSameCell()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(5, 5, 5), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Zero radius still queries the spatial cell
        var results = spatial.QueryRadius(Vector3.Zero, 0f).ToList();

        Assert.Contains(entity, results);
    }

    [Fact]
    public void QueryBounds_WithVeryLargeBounds_FindsAllEntities()
    {
        var entity1 = world.Spawn()
            .With(new Transform3D(new Vector3(-500, -500, -500), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        var entity2 = world.Spawn()
            .With(new Transform3D(new Vector3(500, 500, 500), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryBounds(
            new Vector3(-1000, -1000, -1000),
            new Vector3(1000, 1000, 1000)).ToList();

        Assert.Contains(entity1, results);
        Assert.Contains(entity2, results);
    }

    [Fact]
    public void QueryPoint_AtWorldBoundary_FindsEntities()
    {
        var entity = world.Spawn()
            .With(new Transform3D(new Vector3(999, 999, 999), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryPoint(new Vector3(999, 999, 999)).ToList();

        Assert.Contains(entity, results);
    }

    #endregion

    #region Multiple Update Tests

    [Fact]
    public void Spatial_HandlesRapidPositionChanges()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Rapidly move entity
        for (int i = 0; i < 10; i++)
        {
            world.Set(entity, new Transform3D(new Vector3(i * 50f, 0, 0), Quaternion.Identity, Vector3.One));
            world.Update(0.016f);
        }

        // Entity should be at final position
        var results = spatial.QueryRadius(new Vector3(450, 0, 0), 100f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void Spatial_HandlesTagRemovalAndReaddition()
    {
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        // Remove tag
        world.Remove<SpatialIndexed>(entity);
        Assert.Equal(0, spatial.EntityCount);

        // Re-add tag
        world.Add(entity, new SpatialIndexed());
        Assert.Equal(1, spatial.EntityCount);

        var results = spatial.QueryRadius(Vector3.Zero, 100f).ToList();
        Assert.Contains(entity, results);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesPartitioner()
    {
        using var tempWorld = new World();
        tempWorld.InstallPlugin(new SpatialPlugin());
        var tempSpatial = tempWorld.GetExtension<SpatialQueryApi>();

        var entity = tempWorld.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        tempWorld.Update(0.016f);
        Assert.Equal(1, tempSpatial.EntityCount);

        // Dispose should clean up
        tempSpatial.Dispose();

        // No further validation needed - just ensure no exceptions
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
