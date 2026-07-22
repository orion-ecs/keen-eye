using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// World-level integration tests for <see cref="ObstacleCarveSystem"/>: entities
/// carrying a carving <see cref="NavMeshObstacle"/> drive tile-cache rebuilds
/// through the plugin-registered system, carving and restoring the walkable
/// surface as obstacles are added, moved, and removed.
/// </summary>
public class ObstacleCarveSystemTests
{
    private const float SlabSize = 60f;
    private const float TimeStep = 0.016f;

    #region Helpers

    private static NavMeshTileCache BuildTileCache()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        return builder.BuildTileCache(vertices, indices);
    }

    private static void PumpWorld(World world, int updates = 60)
    {
        for (int i = 0; i < updates; i++)
        {
            world.Update(TimeStep);
        }
    }

    /// <summary>
    /// Spawns an entity with a carving cylinder obstacle centered on the given
    /// world position (Center offset zero, so the transform position is the carve
    /// center).
    /// </summary>
    private static Entity SpawnCylinderObstacle(World world, Vector3 position, float radius = 3f, float height = 2f)
    {
        return world.Spawn()
            .With(new Transform3D(position, Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Cylinder(radius, height, carving: true))
            .Build();
    }

    #endregion

    [Fact]
    public void Update_CarvingObstacleEntity_BlocksPreviouslyValidPath()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(cache, TestHelper.CreateTiledTestConfig()));
        var provider = world.GetExtension<DotRecastProvider>();

        var start = new Vector3(30f, 0f, 8f);
        var end = new Vector3(30f, 0f, 52f);

        var before = provider.FindPath(start, end, AgentSettings.Default);
        Assert.True(before.IsValid);
        Assert.True(before.IsComplete, "A path across the open slab should exist before carving.");

        // A box wall spanning the full width of the slab (overhanging both X edges)
        // splits the slab into a near and far half, disconnecting the two points.
        world.Spawn()
            .With(new Transform3D(new Vector3(30f, 0f, 30f), Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Box(new Vector3(80f, 4f, 8f), carving: true))
            .Build();

        PumpWorld(world);

        Assert.False(cache.Mesh.IsOnNavMesh(new Vector3(30f, 0f, 30f)), "The wall interior should be carved.");

        var after = provider.FindPath(start, end, AgentSettings.Default);
        Assert.False(after.IsComplete, "The carved wall should block the previously valid path.");
    }

    [Fact]
    public void Update_RemovingObstacleComponent_RestoresCarvedSurface()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(cache, TestHelper.CreateTiledTestConfig()));

        var carveCenter = new Vector3(30f, 0f, 30f);
        var obstacle = SpawnCylinderObstacle(world, carveCenter);

        PumpWorld(world);
        Assert.False(cache.Mesh.IsOnNavMesh(carveCenter), "The obstacle should carve the surface.");

        Assert.True(world.Remove<NavMeshObstacle>(obstacle));
        PumpWorld(world);

        Assert.True(cache.Mesh.IsOnNavMesh(carveCenter), "Removing the obstacle component should restore the surface.");
    }

    [Fact]
    public void Update_DespawningObstacleEntity_RestoresCarvedSurface()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(cache, TestHelper.CreateTiledTestConfig()));

        var carveCenter = new Vector3(30f, 0f, 30f);
        var obstacle = SpawnCylinderObstacle(world, carveCenter);

        PumpWorld(world);
        Assert.False(cache.Mesh.IsOnNavMesh(carveCenter));

        world.Despawn(obstacle);
        PumpWorld(world);

        Assert.True(cache.Mesh.IsOnNavMesh(carveCenter), "Despawning the obstacle entity should clean up its carve.");
    }

    [Fact]
    public void Update_ObstacleEntityMoved_RelocatesCarve()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(cache, TestHelper.CreateTiledTestConfig()));

        var firstPosition = new Vector3(20f, 0f, 30f);
        var secondPosition = new Vector3(40f, 0f, 30f);
        var obstacle = SpawnCylinderObstacle(world, firstPosition);

        PumpWorld(world);
        Assert.False(cache.Mesh.IsOnNavMesh(firstPosition), "The initial position should be carved.");
        Assert.True(cache.Mesh.IsOnNavMesh(secondPosition), "The destination should still be walkable.");

        world.Get<Transform3D>(obstacle).Position = secondPosition;
        PumpWorld(world);

        Assert.True(cache.Mesh.IsOnNavMesh(firstPosition), "Moving the obstacle should restore the original position.");
        Assert.False(cache.Mesh.IsOnNavMesh(secondPosition), "The carve should relocate to the new position.");
    }

    [Fact]
    public void Update_BudgetOfOne_LimitsTileRebuildsPerFrame()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(
            cache,
            TestHelper.CreateTiledTestConfig(),
            new NavMeshObstacleCarveConfig { MaxTileRebuildsPerUpdate = 1 }));

        // A large box straddles many tiles, so a single-tile-per-frame budget
        // cannot drain the rebuild queue in one update.
        world.Spawn()
            .With(new Transform3D(new Vector3(30f, 0f, 30f), Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Box(new Vector3(40f, 4f, 40f), carving: true))
            .Build();

        // One world update: the obstacle is added and exactly one tile is rebuilt.
        world.Update(TimeStep);

        int extraPumps = 0;
        while (!cache.Update())
        {
            extraPumps++;
        }

        Assert.True(extraPumps >= 1, "With a budget of one, a multi-tile carve should leave work for later frames.");
    }

    [Fact]
    public void Update_LargeBudget_DrainsRebuildQueueInOneFrame()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(
            cache,
            TestHelper.CreateTiledTestConfig(),
            new NavMeshObstacleCarveConfig { MaxTileRebuildsPerUpdate = 256 }));

        world.Spawn()
            .With(new Transform3D(new Vector3(30f, 0f, 30f), Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Box(new Vector3(40f, 4f, 40f), carving: true))
            .Build();

        world.Update(TimeStep);

        // A generous budget drains every dirty tile in the same frame, so the tile
        // cache reports up to date on the first follow-up pump.
        Assert.True(cache.Update(), "A large budget should fully settle the carve within one frame.");
    }

    [Fact]
    public void Update_NonCarvingObstacle_LeavesSurfaceWalkable()
    {
        var cache = BuildTileCache();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(cache, TestHelper.CreateTiledTestConfig()));

        var center = new Vector3(30f, 0f, 30f);
        world.Spawn()
            .With(new Transform3D(center, Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Cylinder(3f, 2f, carving: false))
            .Build();

        PumpWorld(world);

        Assert.True(cache.Mesh.IsOnNavMesh(center), "Non-carving obstacles must not carve the tile cache.");
    }

    [Fact]
    public void Update_NonTileCachePlugin_DoesNotCarveMesh()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        var mesh = builder.Build(vertices, indices);

        using var world = new World();
        // A plain prebuilt-mesh plugin has no tile cache, so the carve system is
        // never registered and carving obstacles have no effect on the mesh.
        world.InstallPlugin(new DotRecastNavigationPlugin(mesh, TestHelper.CreateTiledTestConfig()));

        var center = new Vector3(30f, 0f, 30f);
        Assert.True(mesh.IsOnNavMesh(center));

        world.Spawn()
            .With(new Transform3D(center, Quaternion.Identity, Vector3.One))
            .With(NavMeshObstacle.Box(new Vector3(8f, 4f, 8f), carving: true))
            .Build();

        PumpWorld(world);

        Assert.True(mesh.IsOnNavMesh(center), "A non-tile-cache provider must ignore the carve path and leave the mesh untouched.");
    }
}
