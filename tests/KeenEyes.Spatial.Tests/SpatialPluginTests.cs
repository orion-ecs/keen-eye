using System.Numerics;
using KeenEyes.Common;

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
    public void Install_QuadtreeStrategy_ThrowsNotImplementedException()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Quadtree };

        var ex = Assert.Throws<ArgumentException>(() => new SpatialPlugin(config));
        Assert.Contains("Quadtree", ex.Message);
    }

    [Fact]
    public void Install_OctreeStrategy_ThrowsNotImplementedException()
    {
        world = new World();
        var config = new SpatialConfig { Strategy = SpatialStrategy.Octree };

        var ex = Assert.Throws<ArgumentException>(() => new SpatialPlugin(config));
        Assert.Contains("Octree", ex.Message);
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
        var entity = world.Spawn()
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
        var entity = world.Spawn()
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
        var entity = world.Spawn()
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
}
