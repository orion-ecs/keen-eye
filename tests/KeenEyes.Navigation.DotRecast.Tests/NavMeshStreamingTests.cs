using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.Abstractions.Components;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for runtime navmesh tile streaming via <see cref="NavMeshStreamingManager"/>:
/// anchor-driven residency, hysteresis, per-update operation budgets, path
/// validity across streaming boundaries, and crowd survival across tile
/// unloads.
/// </summary>
/// <remarks>
/// The tiled test config uses 48-cell tiles at 0.3 cell size, so tiles are
/// 14.4 world units on a side; a 60-unit slab spans a 5x5 tile grid whose
/// tile (0, 0) covers x, z in [0, 14.4).
/// </remarks>
public class NavMeshStreamingTests
{
    private const float SlabSize = 60f;

    #region Helpers

    private static NavMeshTileSet BuildTestTileSet()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        return builder.BuildTileSet(vertices, indices);
    }

    private static NavMeshStreamingConfig CreateStreamingConfig(
        float loadRadius = 15f,
        float hysteresis = 10f,
        int budget = 64)
    {
        return new NavMeshStreamingConfig
        {
            LoadRadius = loadRadius,
            UnloadHysteresis = hysteresis,
            MaxTileOperationsPerUpdate = budget
        };
    }

    private static void PumpUntilIdle(NavMeshStreamingManager manager, int maxIterations = 2000)
    {
        for (int i = 0; i < maxIterations; i++)
        {
            int operations = manager.Update();
            if (operations == 0 && manager.IsIdle)
            {
                return;
            }

            Thread.Sleep(1);
        }

        Assert.Fail("Streaming manager did not reach a steady state.");
    }

    #endregion

    #region Tile Set Build Tests

    [Fact]
    public void BuildTileSet_WithTiledConfig_ProducesMultipleDistinctTiles()
    {
        var tileSet = BuildTestTileSet();

        Assert.True(tileSet.Tiles.Count > 1, "A multi-tile slab should produce more than one tile.");

        var coordinates = new HashSet<(int, int)>();
        foreach (var tile in tileSet.Tiles)
        {
            Assert.True(coordinates.Add((tile.TileX, tile.TileZ)), "Tile coordinates should be unique.");
        }
    }

    [Fact]
    public void BuildTileSet_WithTilesDisabled_ThrowsInvalidOperationException()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTestConfig());

        Assert.Throws<InvalidOperationException>(() => builder.BuildTileSet(vertices.AsSpan(), indices.AsSpan()));
    }

    #endregion

    #region Residency Tests

    [Fact]
    public void Update_WithAnchor_LoadsTilesWithinRadiusOnly()
    {
        var tileSet = BuildTestTileSet();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig());

        manager.SetAnchor(1, new Vector3(7f, 0f, 7f));
        PumpUntilIdle(manager);

        Assert.True(manager.IsTileLoaded(0, 0), "The tile containing the anchor should be loaded.");
        Assert.False(manager.IsTileLoaded(3, 3), "A tile far beyond the load radius should not be loaded.");
        Assert.True(manager.LoadedTileCount > 0);
        Assert.True(manager.LoadedTileCount < tileSet.Tiles.Count, "Distant tiles should stay unloaded.");
    }

    [Fact]
    public void Update_WithNoAnchors_LoadsNothing()
    {
        var tileSet = BuildTestTileSet();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig());

        PumpUntilIdle(manager);

        Assert.Equal(0, manager.LoadedTileCount);
    }

    [Fact]
    public void Update_AnchorMovedFarAway_UnloadsOldTilesAndLoadsNewOnes()
    {
        var tileSet = BuildTestTileSet();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig(loadRadius: 10f, hysteresis: 5f));

        manager.SetAnchor(1, new Vector3(7f, 0f, 7f));
        PumpUntilIdle(manager);
        Assert.True(manager.IsTileLoaded(0, 0));

        manager.SetAnchor(1, new Vector3(55f, 0f, 55f));
        PumpUntilIdle(manager);

        Assert.False(manager.IsTileLoaded(0, 0), "Tiles far behind the anchor should unload.");
        var (anchorTileX, anchorTileZ) = manager.GetTileCoordinate(new Vector3(55f, 0f, 55f));
        Assert.True(manager.IsTileLoaded(anchorTileX, anchorTileZ), "Tiles around the new anchor position should load.");
    }

    [Fact]
    public void Update_AnchorWithinHysteresisBand_KeepsTileLoaded()
    {
        var tileSet = BuildTestTileSet();

        // Load radius 15, unload threshold 15 + 20 = 35. An anchor at x=32 is
        // 17.6 units from tile (0, 0)'s edge (x <= 14.4): outside the load
        // radius but inside the hysteresis band.
        var config = CreateStreamingConfig(loadRadius: 15f, hysteresis: 20f);
        var bandPosition = new Vector3(32f, 0f, 7f);

        using var manager = new NavMeshStreamingManager(tileSet, config);
        manager.SetAnchor(1, new Vector3(7f, 0f, 7f));
        PumpUntilIdle(manager);
        Assert.True(manager.IsTileLoaded(0, 0));

        manager.SetAnchor(1, bandPosition);
        PumpUntilIdle(manager);
        Assert.True(manager.IsTileLoaded(0, 0), "A loaded tile inside the hysteresis band should stay loaded.");

        // A fresh manager whose anchor starts in the band never loads the tile,
        // proving the band preserves state rather than loading.
        using var freshManager = new NavMeshStreamingManager(tileSet, config);
        freshManager.SetAnchor(1, bandPosition);
        PumpUntilIdle(freshManager);
        Assert.False(freshManager.IsTileLoaded(0, 0), "A tile inside the hysteresis band should not be newly loaded.");
    }

    [Fact]
    public void Update_WithBudgetOfOne_AppliesAtMostOneOperationPerUpdate()
    {
        var tileSet = BuildTestTileSet();
        using var manager = new NavMeshStreamingManager(
            tileSet,
            CreateStreamingConfig(loadRadius: 1f, hysteresis: 1f, budget: 1));

        // An anchor on the shared corner of four tiles requires four loads.
        manager.SetAnchor(1, new Vector3(14.4f, 0f, 14.4f));

        int firstUpdateOperations = manager.Update();
        Assert.Equal(1, firstUpdateOperations);
        Assert.Equal(1, manager.LoadedTileCount);

        PumpUntilIdle(manager);
        Assert.Equal(4, manager.LoadedTileCount);
    }

    [Fact]
    public void SetAnchor_AfterDispose_ThrowsObjectDisposedException()
    {
        var tileSet = BuildTestTileSet();
        var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig());
        manager.Dispose();

        Assert.Throws<ObjectDisposedException>(() => manager.SetAnchor(1, Vector3.Zero));
    }

    #endregion

    #region Pathfinding Across Streaming Boundaries

    [Fact]
    public void FindPath_AcrossTileBoundary_WithBothTilesStreamedIn_IsValid()
    {
        var tileSet = BuildTestTileSet();
        var navConfig = TestHelper.CreateTiledTestConfig();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig(loadRadius: 25f));
        using var provider = new DotRecastProvider(navConfig);
        provider.SetNavMesh(manager.Mesh);

        var start = new Vector3(7f, 0f, 7f);    // tile (0, 0)
        var end = new Vector3(25f, 0f, 25f);    // tile (1, 1)

        manager.SetAnchor(1, start);
        PumpUntilIdle(manager);

        var path = provider.FindPath(start, end, AgentSettings.Default);

        Assert.True(path.IsValid, "Path across a streamed-in tile boundary should be valid.");
        Assert.True(path.IsComplete, "Path across a streamed-in tile boundary should reach the destination.");
    }

    [Fact]
    public void FindPath_IntoUnloadedRegion_FailsGracefullyThenSucceedsAfterLoad()
    {
        var tileSet = BuildTestTileSet();
        var navConfig = TestHelper.CreateTiledTestConfig();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig(loadRadius: 10f));
        using var provider = new DotRecastProvider(navConfig);
        provider.SetNavMesh(manager.Mesh);

        var start = new Vector3(7f, 0f, 7f);
        var end = new Vector3(50f, 0f, 50f);    // tile (3, 3), far outside the anchor radius

        manager.SetAnchor(1, start);
        PumpUntilIdle(manager);

        var blockedPath = provider.FindPath(start, end, AgentSettings.Default);
        Assert.False(blockedPath.IsValid, "Pathing into an unloaded region should fail without throwing.");

        // Stream in the destination region (and the corridor between).
        manager.SetAnchor(2, end);
        manager.SetAnchor(3, new Vector3(28f, 0f, 28f));
        PumpUntilIdle(manager);

        var openPath = provider.FindPath(start, end, AgentSettings.Default);
        Assert.True(openPath.IsValid, "Path should succeed once the destination region is streamed in.");
        Assert.True(openPath.IsComplete);
    }

    #endregion

    #region Crowd Interaction Tests

    [Fact]
    public void UpdateCrowd_TileUnloadedUnderAgent_DoesNotThrowAndKeepsAgentRegistered()
    {
        var tileSet = BuildTestTileSet();
        var navConfig = TestHelper.CreateTiledTestConfig();
        using var manager = new NavMeshStreamingManager(tileSet, CreateStreamingConfig(loadRadius: 40f, hysteresis: 5f));
        using var provider = new DotRecastProvider(navConfig);
        provider.SetNavMesh(manager.Mesh);

        var agentPosition = new Vector3(10f, 0f, 10f);
        manager.SetAnchor(1, agentPosition);
        PumpUntilIdle(manager);

        var entity = new Entity(1, 1);
        var agent = NavMeshAgent.Create();
        var crowdAgent = CrowdAgent.Create();
        Assert.True(provider.TryAddCrowdAgent(entity, agentPosition, in agent, in crowdAgent));
        Assert.True(provider.RequestCrowdMoveTarget(entity, new Vector3(30f, 0f, 30f)));

        for (int i = 0; i < 10; i++)
        {
            provider.UpdateCrowd(0.05f);
        }

        // Unload every tile, including the one under the agent. Because tiles
        // are removed from the mesh in place (the mesh object is not swapped),
        // the crowd must survive without being recreated.
        manager.RemoveAnchor(1);
        PumpUntilIdle(manager);
        Assert.Equal(0, manager.LoadedTileCount);

        for (int i = 0; i < 10; i++)
        {
            provider.UpdateCrowd(0.05f);
        }

        Assert.True(
            provider.TryGetCrowdAgentState(entity, out _),
            "The agent should remain registered after the ground under it unloads.");
    }

    #endregion
}
