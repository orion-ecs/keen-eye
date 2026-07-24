using System.Collections.Generic;
using System.Numerics;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;
using KeenEyes.Navigation.DotRecast;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for the tiled navigation mesh build path in <see cref="DotRecastMeshBuilder"/>.
/// </summary>
/// <remarks>
/// These tests build real navigation meshes from a slab (a floor with vertical extent),
/// which Recast can voxelize reliably - unlike the zero-thickness planes used by the
/// legacy skipped tests. The slab is large enough to span multiple tiles.
/// </remarks>
public class DotRecastTiledBuildTests
{
    private const float SlabSize = 120f;
    private static readonly Vector3 start = new(15f, 0f, 15f);
    private static readonly Vector3 end = new(105f, 0f, 105f);

    #region Helpers

    private static int CountTiles(NavMeshData mesh)
    {
        var navMesh = mesh.InternalNavMesh;
        int count = 0;
        for (int i = 0; i < navMesh.GetMaxTiles(); i++)
        {
            var tile = navMesh.GetTile(i);
            if (tile?.data != null)
            {
                count++;
            }
        }

        return count;
    }

    // Counts how many distinct grid tiles the path's waypoints fall into, derived from
    // the navmesh tile grid (origin + tile footprint) rather than the waypoint polygon
    // refs, so the count is independent of how refs are encoded.
    private static int CountDistinctTilesAlongPath(NavMeshData mesh, NavPath path)
    {
        ref readonly var navParams = ref mesh.InternalNavMesh.GetParams();
        var tiles = new HashSet<(int X, int Z)>();

        for (int i = 0; i < path.Count; i++)
        {
            var pos = path[i].Position;
            int tileX = (int)MathF.Floor((pos.X - navParams.orig.X) / navParams.tileWidth);
            int tileZ = (int)MathF.Floor((pos.Z - navParams.orig.Z) / navParams.tileHeight);
            tiles.Add((tileX, tileZ));
        }

        return tiles.Count;
    }

    #endregion

    [Fact]
    public void Build_WithTilesEnabled_ProducesMultipleTiles()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());

        var mesh = builder.Build(vertices, indices);

        Assert.True(CountTiles(mesh) > 1, "A tiled build over a multi-tile slab should install more than one tile.");
    }

    [Fact]
    public void Build_WithTilesDisabled_ProducesSingleTile()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var config = TestHelper.CreateTestConfig();  // UseTiles = false
        var builder = new DotRecastMeshBuilder(config);

        var mesh = builder.Build(vertices, indices);

        Assert.Equal(1, CountTiles(mesh));
    }

    [Fact]
    public void Build_TiledCrossTilePath_MatchesSingleTilePathLength()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);

        var singleConfig = TestHelper.CreateTestConfig();  // UseTiles = false
        var singleMesh = new DotRecastMeshBuilder(singleConfig).Build(vertices, indices);
        using var singleProvider = new DotRecastProvider(singleMesh, singleConfig);
        var singlePath = singleProvider.FindPath(start, end, AgentSettings.Default);

        var tiledConfig = TestHelper.CreateTiledTestConfig();
        var tiledMesh = new DotRecastMeshBuilder(tiledConfig).Build(vertices, indices);
        using var tiledProvider = new DotRecastProvider(tiledMesh, tiledConfig);
        var tiledPath = tiledProvider.FindPath(start, end, AgentSettings.Default);

        Assert.True(singlePath.IsValid, "Single-tile path should be found.");
        Assert.True(tiledPath.IsValid, "Tiled path should be found.");

        // The tiled path must actually cross tile boundaries (otherwise the comparison
        // is meaningless).
        Assert.True(
            CountDistinctTilesAlongPath(tiledMesh, tiledPath) > 1,
            "The tiled path should traverse more than one tile.");

        // Path length through the tiled mesh must match the single-tile mesh within a
        // small tolerance (tiled builds add borders per tile which can perturb geometry).
        float tolerance = System.Math.Max(0.5f, singlePath.Length * 0.05f);
        Assert.True(
            singlePath.Length.ApproximatelyEquals(tiledPath.Length, tolerance),
            $"Tiled path length {tiledPath.Length} should approximately equal single-tile length {singlePath.Length}.");
    }

    [Fact]
    public void Build_WithSmallerOverrideTileSize_ProducesMoreTiles()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var config = TestHelper.CreateTiledTestConfig(tileSize: 48);
        var builder = new DotRecastMeshBuilder(config);

        var defaultMesh = builder.Build(vertices, indices);
        var overriddenMesh = builder.Build(vertices, indices, areaIds: null, overrideTileSize: 24);

        int defaultTiles = CountTiles(defaultMesh);
        int overriddenTiles = CountTiles(overriddenMesh);

        Assert.True(defaultTiles > 1, "Default tile size should still produce multiple tiles.");
        Assert.True(
            overriddenTiles > defaultTiles,
            $"A smaller override tile size ({overriddenTiles} tiles) should produce more tiles than the default ({defaultTiles} tiles).");
    }

    [Fact]
    public void Build_WithNegativeOverrideTileSize_ThrowsArgumentOutOfRange()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.Build(vertices, indices, areaIds: null, overrideTileSize: -1));
    }

    [Fact]
    public void Serialize_MultiTileMesh_RoundTripPreservesTileCountAndPath()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var config = TestHelper.CreateTiledTestConfig();
        var mesh = new DotRecastMeshBuilder(config).Build(vertices, indices);

        int originalTiles = CountTiles(mesh);
        Assert.True(originalTiles > 1, "Precondition: mesh should be multi-tile.");

        using var originalProvider = new DotRecastProvider(mesh, config);
        var originalPath = originalProvider.FindPath(start, end, AgentSettings.Default);
        Assert.True(originalPath.IsValid);

        var bytes = mesh.Serialize();
        var restored = NavMeshData.Deserialize(bytes);

        Assert.Equal(originalTiles, CountTiles(restored));
        Assert.Equal(mesh.PolygonCount, restored.PolygonCount);

        using var restoredProvider = new DotRecastProvider(restored, config);
        var restoredPath = restoredProvider.FindPath(start, end, AgentSettings.Default);

        Assert.True(restoredPath.IsValid, "Path through the restored mesh should be found.");
        Assert.True(
            originalPath.Length.ApproximatelyEquals(restoredPath.Length, System.Math.Max(0.5f, originalPath.Length * 0.05f)),
            $"Restored path length {restoredPath.Length} should match original {originalPath.Length}.");
    }
}
