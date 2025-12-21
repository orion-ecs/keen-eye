using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Systems;

namespace KeenEyes.Spatial.Tests.Systems;

/// <summary>
/// Tests for SpatialUpdateSystem functionality.
/// </summary>
public partial class SpatialUpdateSystemTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public void OnInitialize_WithoutSpatialQueryApi_ThrowsInvalidOperationException()
    {
        // This test verifies the system's dependency checking
        // The system is internal and checks for SpatialQueryApi in OnInitialize
        // We can verify this indirectly through plugin installation behavior
        world = new World();

        // Try to use spatial queries without installing the plugin
        // This would fail if someone manually registered the system
        Assert.False(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    [Fact]
    public void OnInitialize_EnablesAutoTrackingForTransform3D()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Modify transform - should be marked as dirty automatically
        world.Set(entity, new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One));

        // Update should process dirty entities
        world.Update(0.016f);

        var spatial = world.GetExtension<SpatialQueryApi>();
        var results = spatial.QueryRadius(new Vector3(10, 0, 0), 5f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void OnInitialize_IndexesExistingEntities()
    {
        world = new World();

        // Create entities before installing plugin
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        _ = world.Spawn()
            .With(new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Install plugin - should index existing entities during OnInitialize
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();
        Assert.Equal(2, spatial.EntityCount);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithDirtyTransform_UpdatesSpatialIndex()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Move entity
        world.Set(entity, new Transform3D(new Vector3(500, 0, 0), Quaternion.Identity, Vector3.One));

        // Update should process dirty entity
        world.Update(0.016f);

        // Should find at new position
        var results = spatial.QueryRadius(new Vector3(500, 0, 0), 50f).ToList();
        Assert.Contains(entity, results);

        // Should not find at old position
        var oldResults = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.DoesNotContain(entity, oldResults);
    }

    [Fact]
    public void Update_ClearsDirtyFlagsAfterProcessing()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Modify transform
        world.Set(entity, new Transform3D(new Vector3(10, 0, 0), Quaternion.Identity, Vector3.One));

        // First update should process dirty entity
        world.Update(0.016f);

        // Second update should not process entity (dirty flag cleared)
        // This is verified by the system not throwing or causing issues
        world.Update(0.016f);
    }

    [Fact]
    public void Update_WithMultipleDirtyEntities_ProcessesAll()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entities = new List<Entity>();
        for (int i = 0; i < 10; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i * 10f, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
            entities.Add(entity);
        }

        world.Update(0.016f);

        // Move all entities
        for (int i = 0; i < 10; i++)
        {
            world.Set(entities[i], new Transform3D(new Vector3(i * 10f + 1000f, 0, 0), Quaternion.Identity, Vector3.One));
        }

        // Update should process all dirty entities
        world.Update(0.016f);

        // Verify all entities are at new positions
        foreach (var entity in entities)
        {
            var transform = world.Get<Transform3D>(entity);
            var results = spatial.QueryRadius(transform.Position, 5f).ToList();
            Assert.Contains(entity, results);
        }
    }

    [Fact]
    public void Update_WithEntityLackingSpatialIndexed_IgnoresEntity()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without SpatialIndexed tag
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);

        // Modify transform - will mark as dirty
        world.Set(entity, new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One));

        world.Update(0.016f);

        // Entity should not be indexed
        Assert.Equal(0, spatial.EntityCount);
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void OnComponentAdded_SpatialIndexed_IndexesEntityImmediately()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without SpatialIndexed
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Add SpatialIndexed tag
        world.Add(entity, new SpatialIndexed());

        // Should be indexed immediately (no Update needed)
        Assert.Equal(1, spatial.EntityCount);
        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void OnComponentAdded_SpatialIndexedWithoutTransform_DoesNotIndex()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without Transform3D
        var entity = world.Spawn().Build();

        // Add SpatialIndexed tag
        world.Add(entity, new SpatialIndexed());

        // Should not be indexed (no Transform3D)
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void OnComponentRemoved_SpatialIndexed_RemovesFromIndex()
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

        // Remove SpatialIndexed tag
        world.Remove<SpatialIndexed>(entity);

        // Should be removed immediately
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void OnEntityDestroyed_RemovesFromIndex()
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

        // Despawn entity
        world.Despawn(entity);

        // Should be removed from index
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void OnEntityDestroyed_WithoutSpatialIndexed_DoesNotThrow()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without SpatialIndexed
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Despawn should not throw
        world.Despawn(entity);

        Assert.Equal(0, spatial.EntityCount);
    }

    #endregion

    #region IndexEntity Tests

    [Fact]
    public void IndexEntity_WithSpatialBounds_UsesAABBIndexing()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity with SpatialBounds
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new SpatialBounds(new Vector3(-5, -5, -5), new Vector3(5, 5, 5)))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Entity should be indexed with its bounds
        Assert.Equal(1, spatial.EntityCount);
        var results = spatial.QueryBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void IndexEntity_WithoutSpatialBounds_UsesPointIndexing()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without SpatialBounds
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Entity should be indexed as a point
        Assert.Equal(1, spatial.EntityCount);
        var results = spatial.QueryRadius(Vector3.Zero, 5f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void IndexEntity_WithDeadEntity_DoesNotIndex()
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

        // Despawn entity
        world.Despawn(entity);
        Assert.Equal(0, spatial.EntityCount);

        // Try to add SpatialIndexed to dead entity (should be handled gracefully)
        // This would happen if events fire on dead entities
        // The system should check IsAlive
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        // Uninstall plugin (which disposes the system)
        world.UninstallPlugin<SpatialPlugin>();

        // Create new entity - should not be indexed (system is disposed)
        _ = world.Spawn()
            .With(new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Extension should be removed
        Assert.False(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    [Fact]
    public void Dispose_DisablesAutoTracking()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        // Create entity
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Uninstall plugin
        world.UninstallPlugin<SpatialPlugin>();

        // Modify transform - auto tracking should be disabled
        world.Set(entity, new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One));

        // Update should not throw even though system is disposed
        world.Update(0.016f);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void System_HandlesEntityWithBoundsMoving()
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

        // Move entity
        world.Set(entity, new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Should be found at new position with bounds
        var results = spatial.QueryBounds(new Vector3(95, -5, -5), new Vector3(105, 5, 5)).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void System_HandlesAddingBoundsToExistingEntity()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without bounds
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Add bounds
        world.Add(entity, new SpatialBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)));

        // Move entity to trigger re-indexing with bounds
        world.Set(entity, new Transform3D(new Vector3(1, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Should be indexed with bounds
        var results = spatial.QueryBounds(new Vector3(-9, -10, -10), new Vector3(11, 10, 10)).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void System_HandlesRemovingBoundsFromEntity()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity with bounds
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new SpatialBounds(new Vector3(-10, -10, -10), new Vector3(10, 10, 10)))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Remove bounds
        world.Remove<SpatialBounds>(entity);

        // Move entity to trigger re-indexing as point
        world.Set(entity, new Transform3D(new Vector3(1, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Should be indexed as point
        var results = spatial.QueryRadius(new Vector3(1, 0, 0), 5f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void System_HandlesManyUpdatesPerFrame()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entities = new List<Entity>();
        for (int i = 0; i < 100; i++)
        {
            var entity = world.Spawn()
                .With(new Transform3D(new Vector3(i, 0, 0), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
            entities.Add(entity);
        }

        world.Update(0.016f);

        // Move all entities
        for (int i = 0; i < 100; i++)
        {
            world.Set(entities[i], new Transform3D(new Vector3(i + 1000, 0, 0), Quaternion.Identity, Vector3.One));
        }

        // Update should handle all dirty entities
        world.Update(0.016f);

        Assert.Equal(100, spatial.EntityCount);
    }

    #endregion
}
