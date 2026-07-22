using System;
using System.Numerics;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for the tile-cache build mode: obstacle-driven partial rebuilds where
/// adding an obstacle carves the walkable surface and removing it restores the
/// surface, one tile rebuild per update.
/// </summary>
public class NavMeshTileCacheTests
{
    private const float SlabSize = 60f;

    #region Helpers

    private static NavMeshTileCache BuildTestTileCache()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        return builder.BuildTileCache(vertices, indices);
    }

    private static void PumpUntilUpToDate(NavMeshTileCache cache, int maxIterations = 500)
    {
        for (int i = 0; i < maxIterations; i++)
        {
            if (cache.Update())
            {
                return;
            }
        }

        Assert.Fail("Tile cache did not finish processing obstacle requests.");
    }

    #endregion

    #region Build Tests

    [Fact]
    public void BuildTileCache_WithTiledConfig_ProducesPathableMesh()
    {
        var cache = BuildTestTileCache();

        using var provider = new DotRecastProvider(TestHelper.CreateTiledTestConfig());
        provider.SetNavMesh(cache.Mesh);

        var path = provider.FindPath(new Vector3(5f, 0f, 5f), new Vector3(55f, 0f, 55f), AgentSettings.Default);

        Assert.True(path.IsValid, "A freshly built tile cache mesh should support pathfinding.");
        Assert.True(path.IsComplete);
    }

    [Fact]
    public void BuildTileCache_WithTilesDisabled_ThrowsInvalidOperationException()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTestConfig());

        Assert.Throws<InvalidOperationException>(() => builder.BuildTileCache(vertices.AsSpan(), indices.AsSpan()));
    }

    #endregion

    #region Obstacle Carving Tests

    [Fact]
    public void AddCylinderObstacle_AfterUpdate_CarvesWalkableSurface()
    {
        var cache = BuildTestTileCache();
        var obstacleCenter = new Vector3(30f, 0f, 30f);

        Assert.True(cache.Mesh.IsOnNavMesh(obstacleCenter), "The surface should be walkable before carving.");

        cache.AddCylinderObstacle(new Vector3(30f, -0.5f, 30f), radius: 3f, height: 2f);
        PumpUntilUpToDate(cache);

        Assert.False(cache.Mesh.IsOnNavMesh(obstacleCenter), "The carved region should no longer be walkable.");
    }

    [Fact]
    public void RemoveObstacle_AfterUpdate_RestoresWalkableSurface()
    {
        var cache = BuildTestTileCache();
        var obstacleCenter = new Vector3(30f, 0f, 30f);

        long obstacleRef = cache.AddCylinderObstacle(new Vector3(30f, -0.5f, 30f), radius: 3f, height: 2f);
        PumpUntilUpToDate(cache);
        Assert.False(cache.Mesh.IsOnNavMesh(obstacleCenter));

        cache.RemoveObstacle(obstacleRef);
        PumpUntilUpToDate(cache);

        Assert.True(cache.Mesh.IsOnNavMesh(obstacleCenter), "Removing the obstacle should restore the surface.");
    }

    [Fact]
    public void AddBoxObstacle_PathThroughObstacle_DetoursAndStaysValid()
    {
        var cache = BuildTestTileCache();
        using var provider = new DotRecastProvider(TestHelper.CreateTiledTestConfig());
        provider.SetNavMesh(cache.Mesh);

        var start = new Vector3(20f, 0f, 30f);
        var end = new Vector3(40f, 0f, 30f);

        cache.AddBoxObstacle(new Vector3(30f, 0.5f, 30f), new Vector3(3f, 2f, 3f));
        PumpUntilUpToDate(cache);

        var path = provider.FindPath(start, end, AgentSettings.Default);

        Assert.True(path.IsValid, "The path should route around the carved obstacle.");
        Assert.True(path.IsComplete);
        Assert.False(cache.Mesh.IsOnNavMesh(new Vector3(30f, 0f, 30f)), "The box interior should be carved.");
    }

    #endregion
}
