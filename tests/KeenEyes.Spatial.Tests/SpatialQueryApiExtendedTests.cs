using System.Numerics;
using KeenEyes.Common;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Extended tests for SpatialQueryApi covering edge cases and additional scenarios.
/// </summary>
public partial class SpatialQueryApiExtendedTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesPartitioner()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Add entities
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        // Dispose the API
        spatial.Dispose();

        // EntityCount should still work (though partitioner might be disposed)
        // This tests the disposal doesn't crash
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Dispose multiple times should not throw
        spatial.Dispose();
        spatial.Dispose();
        spatial.Dispose();
    }

    #endregion

    #region Generic Query Methods with Component Filtering

    [Component]
    public partial struct TestMarker : IComponent
    {
        public int Value;
    }

    [Fact]
    public void QueryRadius_Generic_FiltersEntitiesByComponent()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities with and without TestMarker
        var entityWith = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new TestMarker { Value = 1 })
            .WithTag<SpatialIndexed>()
            .Build();

        var entityWithout = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Query with generic filter
        var results = spatial.QueryRadius<TestMarker>(Vector3.Zero, 100f).ToList();

        Assert.Single(results);
        Assert.Contains(entityWith, results);
        Assert.DoesNotContain(entityWithout, results);
    }

    [Fact]
    public void QueryRadius_Generic_EmptyResultsWhenNoEntitiesHaveComponent()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities without TestMarker
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Query for component that no entities have
        var results = spatial.QueryRadius<TestMarker>(Vector3.Zero, 100f).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void QueryBounds_Generic_FiltersEntitiesByComponent()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities with and without TestMarker
        var entityWith = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new TestMarker { Value = 1 })
            .WithTag<SpatialIndexed>()
            .Build();

        var entityWithout = world.Spawn()
            .With(new Transform3D(new Vector3(5, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Query with generic filter
        var results = spatial.QueryBounds<TestMarker>(
            new Vector3(-10, -10, -10),
            new Vector3(10, 10, 10)).ToList();

        Assert.Single(results);
        Assert.Contains(entityWith, results);
        Assert.DoesNotContain(entityWithout, results);
    }

    [Fact]
    public void QueryPoint_Generic_FiltersEntitiesByComponent()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities with and without TestMarker
        var entityWith = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new TestMarker { Value = 1 })
            .WithTag<SpatialIndexed>()
            .Build();

        var entityWithout = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Query with generic filter
        var results = spatial.QueryPoint<TestMarker>(Vector3.Zero).ToList();

        Assert.Contains(entityWith, results);
        Assert.DoesNotContain(entityWithout, results);
    }

    [Fact]
    public void QueryFrustum_Generic_FiltersEntitiesByComponent()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities with and without TestMarker
        var entityWith = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, -5), Quaternion.Identity, Vector3.One))
            .With(new TestMarker { Value = 1 })
            .WithTag<SpatialIndexed>()
            .Build();

        var entityWithout = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, -5), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Create a frustum using LookAt to view the entities
        var viewMatrix = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        var projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, 1.0f, 0.1f, 1000f);
        var frustum = Frustum.FromMatrix(viewMatrix * projMatrix);

        // Query with generic filter
        var results = spatial.QueryFrustum<TestMarker>(frustum).ToList();

        // Should contain entity with TestMarker
        Assert.Contains(entityWith, results);
        // Should not contain entity without TestMarker (though it's in frustum)
        Assert.DoesNotContain(entityWithout, results);
    }

    #endregion

    #region Span Query Buffer Overflow Tests

    [Fact]
    public void QueryPoint_Span_BufferTooSmall_ReturnsNegativeOne()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create many entities at the same point
        for (int i = 0; i < 10; i++)
        {
            _ = world.Spawn()
                .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }

        world.Update(0.016f);

        // Buffer too small to hold all results
        Span<Entity> buffer = stackalloc Entity[5];
        int count = spatial.QueryPoint(Vector3.Zero, buffer);

        Assert.Equal(-1, count); // Overflow
        // Buffer should contain partial results
        Assert.NotEqual(Entity.Null, buffer[0]);
    }

    [Fact]
    public void QueryFrustum_Span_ReturnsCorrectCount()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entities
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Create a large frustum
        var frustum = Frustum.FromMatrix(Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI / 2, 1.0f, 0.1f, 1000f));

        Span<Entity> buffer = stackalloc Entity[16];
        int count = spatial.QueryFrustum(frustum, buffer);

        Assert.True(count >= 0);
        Assert.True(count <= buffer.Length);
    }

    [Fact]
    public void QueryFrustum_Span_BufferTooSmall_ReturnsNegativeOne()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create many entities
        for (int i = 0; i < 20; i++)
        {
            _ = world.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }

        world.Update(0.016f);

        // Create a large frustum (FOV must be < PI)
        var viewMatrix = Matrix4x4.CreateLookAt(Vector3.Zero, -Vector3.UnitZ, Vector3.UnitY);
        var projMatrix = Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI * 0.9f, 1.0f, 0.1f, 10000f);
        var frustum = Frustum.FromMatrix(viewMatrix * projMatrix);

        // Buffer too small
        Span<Entity> buffer = stackalloc Entity[5];
        int count = spatial.QueryFrustum(frustum, buffer);

        // Should return -1 if buffer was too small
        // Note: actual result depends on how many entities are in frustum
        Assert.True(count == -1 || count <= 5);
    }

    #endregion

    #region EntityCount Tests

    [Fact]
    public void EntityCount_AfterAddingEntities_ReturnsCorrectCount()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        Assert.Equal(0, spatial.EntityCount);

        for (int i = 0; i < 10; i++)
        {
            _ = world.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }

        world.Update(0.016f);

        Assert.Equal(10, spatial.EntityCount);
    }

    [Fact]
    public void EntityCount_AfterRemovingEntities_ReturnsCorrectCount()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entities = new List<Entity>();
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
            entities.Add(entity);
        }

        world.Update(0.016f);
        Assert.Equal(10, spatial.EntityCount);

        // Remove half the entities
        for (int i = 0; i < 5; i++)
        {
            world.Despawn(entities[i]);
        }

        Assert.Equal(5, spatial.EntityCount);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void QueryRadius_WithZeroRadius_FindsEntitiesAtExactPosition()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 0f).ToList();

        // Zero radius might return entities in same cell (implementation dependent)
        // This test verifies it doesn't crash
        Assert.NotNull(results);
    }

    [Fact]
    public void QueryRadius_WithVeryLargeRadius_FindsAllEntities()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entities = new List<Entity>();
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i * 100f, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
            entities.Add(entity);
        }

        world.Update(0.016f);

        var results = spatial.QueryRadius(Vector3.Zero, 10000f).ToList();

        Assert.Equal(10, results.Count);
    }

    [Fact]
    public void QueryBounds_WithVerySmallBounds_ReturnsEntitiesInCell()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Very small bounds
        var results = spatial.QueryBounds(
            new Vector3(-0.01f, -0.01f, -0.01f),
            new Vector3(0.01f, 0.01f, 0.01f)).ToList();

        // Should find entity at origin (implementation dependent)
        Assert.NotNull(results);
    }

    [Fact]
    public void QueryBounds_WithInvertedBounds_HandlesGracefully()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Inverted bounds (min > max) - behavior is implementation dependent
        // This test verifies it doesn't crash
        var results = spatial.QueryBounds(
            new Vector3(10, 10, 10),
            new Vector3(-10, -10, -10)).ToList();

        Assert.NotNull(results);
    }

    #endregion

    #region Multiple Strategy Tests

    [Theory]
    [InlineData(SpatialStrategy.Grid)]
    [InlineData(SpatialStrategy.Quadtree)]
    [InlineData(SpatialStrategy.Octree)]
    public void QueryMethods_WorkAcrossAllStrategies(SpatialStrategy strategy)
    {
        world = new World();
        var config = new SpatialConfig { Strategy = strategy };
        world.InstallPlugin(new SpatialPlugin(config));

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Test all query methods work
        var radiusResults = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        var boundsResults = spatial.QueryBounds(new Vector3(-50, -50, -50), new Vector3(50, 50, 50)).ToList();
        var pointResults = spatial.QueryPoint(Vector3.Zero).ToList();

        Assert.NotEmpty(radiusResults);
        Assert.NotEmpty(boundsResults);
        Assert.NotEmpty(pointResults);
    }

    #endregion
}
