using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Spatial.Systems;
using KeenEyes.Testing.Plugins;

namespace KeenEyes.Spatial.Tests;

/// <summary>
/// Tests for the SpatialPlugin integration with the World.
/// </summary>
public class SpatialPluginTests : IDisposable
{
    private World? world;

    public void Dispose()
    {
        world?.Dispose();
    }

    #region Installation Tests

    [Fact]
    public void Install_WithDefaultConfig_Succeeds()
    {
        world = new World();

        // Should not throw
        world.InstallPlugin(new SpatialPlugin());

        // Should have the extension
        Assert.True(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    [Fact]
    public void Install_WithCustomConfig_Succeeds()
    {
        world = new World();

        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = 50f,
                WorldMin = new Vector3(-500, -500, -500),
                WorldMax = new Vector3(500, 500, 500)
            }
        };

        world.InstallPlugin(new SpatialPlugin(config));

        Assert.True(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    [Fact]
    public void Install_WithInvalidConfig_ThrowsArgumentException()
    {
        world = new World();

        var config = new SpatialConfig
        {
            Grid = new GridConfig
            {
                CellSize = -10f // Invalid
            }
        };

        var ex = Assert.Throws<ArgumentException>(() => world.InstallPlugin(new SpatialPlugin(config)));
        Assert.Contains("Invalid SpatialConfig", ex.Message);
    }

    [Fact]
    public void Install_GridStrategy_CreatesGridPartitioner()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Grid };

        world.InstallPlugin(new SpatialPlugin(config));

        var spatial = world.GetExtension<SpatialQueryApi>();
        Assert.NotNull(spatial);
    }

    [Fact]
    public void Install_QuadtreeStrategy_Succeeds()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Quadtree };

        world.InstallPlugin(new SpatialPlugin(config));

        Assert.True(world.HasExtension<SpatialQueryApi>());
    }

    [Fact]
    public void Install_OctreeStrategy_Succeeds()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Octree };

        world.InstallPlugin(new SpatialPlugin(config));

        Assert.True(world.HasExtension<SpatialQueryApi>());
    }

    #endregion

    #region Uninstallation Tests

    [Fact]
    public void Uninstall_RemovesExtension()
    {
        world = new World();
        var plugin = new SpatialPlugin();

        world.InstallPlugin(plugin);
        Assert.True(world.TryGetExtension<SpatialQueryApi>(out _));

        world.UninstallPlugin<SpatialPlugin>();
        Assert.False(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    [Fact]
    public void Uninstall_DisposesResources()
    {
        world = new World();
        var plugin = new SpatialPlugin();

        world.InstallPlugin(plugin);
        var spatial = world.GetExtension<SpatialQueryApi>();

        // Add entity to index
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);

        // Uninstall should dispose the partitioner
        world.UninstallPlugin<SpatialPlugin>();

        // Extension should be gone
        Assert.False(world.TryGetExtension<SpatialQueryApi>(out _));
    }

    #endregion

    #region System Integration Tests

    [Fact]
    public void Plugin_RegistersSpatialUpdateSystem()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        // Create entity with transform and spatial tag
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Update should trigger spatial indexing
        world.Update(0.016f);

        var spatial = world.GetExtension<SpatialQueryApi>();
        Assert.Equal(1, spatial.EntityCount);
    }

    [Fact]
    public void Plugin_UpdatesIndexOnTransformChange()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Entity should be found near origin
        var results1 = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results1);

        // Move entity far away
        world.Set(entity, new Transform3D(new Vector3(1000, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Should no longer be found near origin
        var results2 = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.DoesNotContain(entity, results2);

        // Should be found at new position
        var results3 = spatial.QueryRadius(new Vector3(1000, 0, 0), 50f).ToList();
        Assert.Contains(entity, results3);
    }

    [Fact]
    public void Plugin_HandlesEntityDespawn()
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

        world.Despawn(entity);
        world.Update(0.016f);

        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void Plugin_RunsInLateUpdatePhase()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity
        _ = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Entity is indexed immediately when created (component events fire during creation)
        Assert.Equal(1, spatial.EntityCount);

        // After update (which runs LateUpdate systems), entity should be indexed
        world.Update(0.016f);
        Assert.Equal(1, spatial.EntityCount);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void GridConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(-1000, -1000, -1000),
            WorldMax = new Vector3(1000, 1000, 1000)
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void GridConfig_Validate_NegativeCellSize_ReturnsError()
    {
        var config = new GridConfig
        {
            CellSize = -10f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void GridConfig_Validate_ZeroCellSize_ReturnsError()
    {
        var config = new GridConfig
        {
            CellSize = 0f
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    [Fact]
    public void GridConfig_Validate_InvalidWorldBounds_ReturnsError()
    {
        var config = new GridConfig
        {
            CellSize = 100f,
            WorldMin = new Vector3(1000, 1000, 1000),
            WorldMax = new Vector3(-1000, -1000, -1000) // Min > Max
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("WorldMin", error);
        Assert.Contains("WorldMax", error);
    }

    [Fact]
    public void SpatialConfig_Validate_ValidConfig_ReturnsNull()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = 100f,
                WorldMin = new Vector3(-1000, -1000, -1000),
                WorldMax = new Vector3(1000, 1000, 1000)
            }
        };

        var error = config.Validate();

        Assert.Null(error);
    }

    [Fact]
    public void SpatialConfig_Validate_InvalidGridConfig_ReturnsError()
    {
        var config = new SpatialConfig
        {
            Strategy = SpatialStrategy.Grid,
            Grid = new GridConfig
            {
                CellSize = -10f // Invalid
            }
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("CellSize", error);
    }

    #endregion

    #region Stress Tests

    [Fact]
    public void Plugin_HandlesLargeNumberOfEntities()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create 1000 entities in a grid pattern
        for (int i = 0; i < 1000; i++)
        {
            int x = (i % 32) * 50;
            int z = (i / 32) * 50;

            world.Spawn()
                .With(new Transform3D(new Vector3(x, 0, z), Quaternion.Identity, Vector3.One))
                .WithTag<SpatialIndexed>()
                .Build();
        }

        world.Update(0.016f);

        Assert.Equal(1000, spatial.EntityCount);

        // Query should find nearby entities efficiently
        var results = spatial.QueryRadius(new Vector3(500, 0, 500), 200f).ToList();
        Assert.NotEmpty(results);
        Assert.True(results.Count < 1000); // Should not return all entities
    }

    [Fact]
    public void Plugin_HandlesFrequentUpdates()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .WithTag<SpatialIndexed>()
            .Build();

        // Update entity position many times
        for (int i = 0; i < 100; i++)
        {
            world.Set(entity, new Transform3D(new Vector3(i * 10f, 0, 0), Quaternion.Identity, Vector3.One));
            world.Update(0.016f);
        }

        // Entity should be at final position
        var results = spatial.QueryRadius(new Vector3(990, 0, 0), 50f).ToList();
        Assert.Contains(entity, results);
    }

    #endregion

    #region Event Handler Tests

    [Fact]
    public void Plugin_IndexesEntityWithSpatialBounds()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity with Transform3D and SpatialBounds
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .With(new SpatialBounds(new Vector3(-1, -1, -1), new Vector3(1, 1, 1)))
            .WithTag<SpatialIndexed>()
            .Build();

        world.Update(0.016f);

        // Entity should be indexed
        Assert.Equal(1, spatial.EntityCount);
        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void Plugin_AddingSpatialIndexedTag_IndexesEntity()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity with Transform3D but without SpatialIndexed tag
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Add SpatialIndexed tag - should trigger OnComponentAdded handler
        world.Add(entity, new SpatialIndexed());

        // Entity should be indexed immediately (no need for Update)
        Assert.Equal(1, spatial.EntityCount);
        var results = spatial.QueryRadius(Vector3.Zero, 50f).ToList();
        Assert.Contains(entity, results);
    }

    [Fact]
    public void Plugin_RemovingSpatialIndexedTag_RemovesFromIndex()
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

        // Remove SpatialIndexed tag - should trigger OnComponentRemoved handler
        world.Remove<SpatialIndexed>(entity);

        // Entity should be removed from index immediately
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void Plugin_DespawnNonIndexedEntity_DoesNotThrow()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without SpatialIndexed tag
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Despawn should not throw even though entity is not indexed
        world.Despawn(entity);

        // Still no entities in index
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void Plugin_UpdateWithoutSpatialIndexedTag_IgnoresEntity()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity with Transform3D but no SpatialIndexed tag
        var entity = world.Spawn()
            .With(new Transform3D(Vector3.Zero, Quaternion.Identity, Vector3.One))
            .Build();

        world.Update(0.016f);
        Assert.Equal(0, spatial.EntityCount);

        // Modify transform - should mark as dirty
        world.Set(entity, new Transform3D(new Vector3(100, 0, 0), Quaternion.Identity, Vector3.One));
        world.Update(0.016f);

        // Entity should still not be indexed (filtered out in Update)
        Assert.Equal(0, spatial.EntityCount);
    }

    [Fact]
    public void Plugin_AddingSpatialIndexedToEntityWithoutTransform_DoesNotIndex()
    {
        world = new World();
        world.InstallPlugin(new SpatialPlugin());

        var spatial = world.GetExtension<SpatialQueryApi>();

        // Create entity without Transform3D
        var entity = world.Spawn().Build();

        // Add SpatialIndexed tag - should not index because no Transform3D
        world.Add(entity, new SpatialIndexed());

        // Entity should not be indexed
        Assert.Equal(0, spatial.EntityCount);
    }

    #endregion

    #region Strategy Validation Tests

    [Fact]
    public void SpatialConfig_Validate_UnknownStrategy_ReturnsError()
    {
        // Create config with invalid strategy value (cast from int)
        var config = new SpatialConfig
        {
            Strategy = (SpatialStrategy)999
        };

        var error = config.Validate();

        Assert.NotNull(error);
        Assert.Contains("Unknown spatial strategy", error);
        Assert.Contains("999", error);
    }

    [Fact]
    public void Install_UnknownStrategy_ThrowsArgumentException()
    {
        world = new World();

        // Create config with invalid strategy value (cast from int)
        var config = new SpatialConfig
        {
            Strategy = (SpatialStrategy)999
        };

        // Plugin constructor should throw because validation fails
        var ex = Assert.Throws<ArgumentException>(() => new SpatialPlugin(config));
        Assert.Contains("Unknown spatial strategy", ex.Message);
    }

    #endregion

    #region MockPluginContext Tests

    [Fact]
    public void Install_RegistersSpatialQueryApiExtension()
    {
        using var world = new World();
        var plugin = new SpatialPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        context
            .ShouldHaveSetExtension<SpatialQueryApi>()
            .ShouldHaveSetExtensionCount(1);
    }

    [Fact]
    public void Install_RegistersSpatialUpdateSystem()
    {
        using var world = new World();
        var plugin = new SpatialPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        Assert.True(context.WasSystemRegistered<SpatialUpdateSystem>());
        Assert.True(context.WasSystemRegisteredAtPhase<SpatialUpdateSystem>(SystemPhase.LateUpdate));
    }

    [Fact]
    public void Install_RegistersSpatialIndexedComponent()
    {
        using var world = new World();
        var plugin = new SpatialPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        Assert.True(context.WasComponentRegistered<SpatialIndexed>());
    }

    [Fact]
    public void Install_SpatialUpdateSystem_HasNegativeOrder()
    {
        using var world = new World();
        var plugin = new SpatialPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var registration = context.GetSystemRegistration<SpatialUpdateSystem>();
        Assert.NotNull(registration);
        // System runs early in LateUpdate phase (order = -100)
        Assert.Equal(-100, registration.Value.Order);
    }

    [Fact]
    public void Install_WithGridStrategy_CreatesSpatialQueryApi()
    {
        using var world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Grid };
        var plugin = new SpatialPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var api = context.GetSetExtension<SpatialQueryApi>();
        Assert.NotNull(api);
    }

    [Fact]
    public void Install_WithQuadtreeStrategy_CreatesSpatialQueryApi()
    {
        using var world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Quadtree };
        var plugin = new SpatialPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var api = context.GetSetExtension<SpatialQueryApi>();
        Assert.NotNull(api);
    }

    [Fact]
    public void Install_WithOctreeStrategy_CreatesSpatialQueryApi()
    {
        using var world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Octree };
        var plugin = new SpatialPlugin(config);
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        var api = context.GetSetExtension<SpatialQueryApi>();
        Assert.NotNull(api);
    }

    [Fact]
    public void Install_RegistersExactlyOneSystem()
    {
        using var world = new World();
        var plugin = new SpatialPlugin();
        var context = new MockPluginContext(plugin, world);

        plugin.Install(context);

        Assert.Single(context.RegisteredSystems);
    }

    [Fact]
    public void Install_AllStrategies_RegisterSameSystemsAndComponents()
    {
        var strategies = new[] { SpatialStrategy.Grid, SpatialStrategy.Quadtree, SpatialStrategy.Octree };

        foreach (var strategy in strategies)
        {
            using var world = new World();
            var config = new SpatialConfig { Strategy = strategy };
            var plugin = new SpatialPlugin(config);
            var context = new MockPluginContext(plugin, world);

            plugin.Install(context);

            Assert.True(context.WasSystemRegistered<SpatialUpdateSystem>(),
                $"SpatialUpdateSystem not registered for {strategy}");
            Assert.True(context.WasComponentRegistered<SpatialIndexed>(),
                $"SpatialIndexed not registered for {strategy}");
            context.ShouldHaveSetExtension<SpatialQueryApi>();
        }
    }

    #endregion
}
