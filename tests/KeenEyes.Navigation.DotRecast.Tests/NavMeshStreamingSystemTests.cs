using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// World-level integration tests for <see cref="NavMeshStreamingSystem"/>:
/// entities with <see cref="NavMeshStreamingAnchor"/> drive tile residency
/// through the plugin-registered system.
/// </summary>
public class NavMeshStreamingSystemTests
{
    private const float SlabSize = 60f;
    private const float TimeStep = 0.016f;

    #region Helpers

    private static NavMeshStreamingManager CreateStreamingManager()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        var tileSet = builder.BuildTileSet(vertices, indices);

        return new NavMeshStreamingManager(tileSet, new NavMeshStreamingConfig
        {
            LoadRadius = 15f,
            UnloadHysteresis = 5f,
            MaxTileOperationsPerUpdate = 32
        });
    }

    private static void PumpWorld(World world, int updates = 30)
    {
        for (int i = 0; i < updates; i++)
        {
            world.Update(TimeStep);
        }
    }

    #endregion

    [Fact]
    public void Update_AnchorEntity_LoadsSurroundingTiles()
    {
        using var manager = CreateStreamingManager();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(manager, TestHelper.CreateTiledTestConfig()));

        world.Spawn()
            .With(new Transform3D(new Vector3(7f, 0f, 7f), Quaternion.Identity, Vector3.One))
            .With(new NavMeshStreamingAnchor())
            .Build();

        PumpWorld(world);

        Assert.True(manager.IsTileLoaded(0, 0), "The tile under the anchor entity should be loaded.");
        Assert.True(manager.LoadedTileCount > 0);
    }

    [Fact]
    public void Update_AnchorEntityMoved_ShiftsResidencyToNewPosition()
    {
        using var manager = CreateStreamingManager();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(manager, TestHelper.CreateTiledTestConfig()));

        var anchor = world.Spawn()
            .With(new Transform3D(new Vector3(7f, 0f, 7f), Quaternion.Identity, Vector3.One))
            .With(new NavMeshStreamingAnchor())
            .Build();

        PumpWorld(world);
        Assert.True(manager.IsTileLoaded(0, 0));

        world.Get<Transform3D>(anchor).Position = new Vector3(55f, 0f, 55f);
        PumpWorld(world);

        Assert.False(manager.IsTileLoaded(0, 0), "Tiles left behind by the anchor entity should unload.");
        var (tileX, tileZ) = manager.GetTileCoordinate(new Vector3(55f, 0f, 55f));
        Assert.True(manager.IsTileLoaded(tileX, tileZ), "Tiles around the anchor's new position should load.");
    }

    [Fact]
    public void Update_AnchorEntityDespawned_UnloadsAllTiles()
    {
        using var manager = CreateStreamingManager();
        using var world = new World();
        world.InstallPlugin(new DotRecastNavigationPlugin(manager, TestHelper.CreateTiledTestConfig()));

        var anchor = world.Spawn()
            .With(new Transform3D(new Vector3(7f, 0f, 7f), Quaternion.Identity, Vector3.One))
            .With(new NavMeshStreamingAnchor())
            .Build();

        PumpWorld(world);
        Assert.True(manager.LoadedTileCount > 0);

        world.Despawn(anchor);
        PumpWorld(world);

        Assert.Equal(0, manager.LoadedTileCount);
        Assert.Equal(0, manager.AnchorCount);
    }
}
