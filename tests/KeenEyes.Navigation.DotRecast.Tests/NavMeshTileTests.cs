using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using KeenEyes.Common;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast.Tests;

/// <summary>
/// Tests for per-tile and tile-set persistence: individual tile round-trips,
/// whole-set round-trips, lazy materialization of deserialized tiles, and
/// rejection of invalid data.
/// </summary>
public class NavMeshTileTests
{
    private const float SlabSize = 60f;

    #region Helpers

    private static NavMeshTileSet BuildTestTileSet()
    {
        var (vertices, indices) = TestHelper.BuildSlabGeometry(SlabSize);
        var builder = new DotRecastMeshBuilder(TestHelper.CreateTiledTestConfig());
        return builder.BuildTileSet(vertices, indices);
    }

    #endregion

    #region NavMeshTile Round-Trip Tests

    [Fact]
    public void Serialize_RoundTrip_PreservesCoordinatesAndPolygons()
    {
        var tileSet = BuildTestTileSet();
        var original = tileSet.Tiles[0];

        byte[] bytes = original.Serialize();
        var restored = NavMeshTile.Deserialize(bytes);

        Assert.Equal(original.TileX, restored.TileX);
        Assert.Equal(original.TileZ, restored.TileZ);
        Assert.Equal(original.Layer, restored.Layer);
        Assert.Equal(original.GetMeshData().header.polyCount, restored.GetMeshData().header.polyCount);
        Assert.Equal(original.GetMeshData().header.vertCount, restored.GetMeshData().header.vertCount);
    }

    [Fact]
    public void Serialize_OfUnmaterializedDeserializedTile_IsByteIdentical()
    {
        var tileSet = BuildTestTileSet();
        byte[] bytes = tileSet.Tiles[0].Serialize();

        var restored = NavMeshTile.Deserialize(bytes);
        Assert.False(restored.IsMaterialized, "Deserialized tiles should decode lazily.");

        byte[] reserialized = restored.Serialize();
        Assert.True(bytes.SequenceEqual(reserialized), "Re-serializing an untouched tile should be lossless.");
        Assert.False(restored.IsMaterialized, "Re-serializing should not force materialization.");
    }

    [Fact]
    public void Deserialize_WithWrongMagic_ThrowsInvalidDataException()
    {
        byte[] bogus = new byte[64];
        Assert.Throws<InvalidDataException>(() => NavMeshTile.Deserialize(bogus));
    }

    [Fact]
    public void Deserialize_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NavMeshTile.Deserialize(null!));
    }

    #endregion

    #region NavMeshTileSet Round-Trip Tests

    [Fact]
    public void TileSetSerialize_RoundTrip_PreservesGridParametersAndTiles()
    {
        var tileSet = BuildTestTileSet();

        byte[] bytes = tileSet.Serialize();
        var restored = NavMeshTileSet.Deserialize(bytes);

        Assert.Equal(tileSet.Origin, restored.Origin);
        Assert.Equal(tileSet.TileWorldSize, restored.TileWorldSize, 3);
        Assert.Equal(tileSet.MaxTiles, restored.MaxTiles);
        Assert.Equal(tileSet.MaxPolysPerTile, restored.MaxPolysPerTile);
        Assert.Equal(tileSet.MaxVertsPerPoly, restored.MaxVertsPerPoly);
        Assert.Equal(tileSet.BuiltForAgent.Radius, restored.BuiltForAgent.Radius, 3);
        Assert.Equal(tileSet.Tiles.Count, restored.Tiles.Count);
    }

    [Fact]
    public void TileSetDeserialize_StreamedWithLazyTiles_SupportsPathfinding()
    {
        var tileSet = BuildTestTileSet();
        var restored = NavMeshTileSet.Deserialize(tileSet.Serialize());

        Assert.All(restored.Tiles, tile => Assert.False(tile.IsMaterialized));

        // Stream the restored set; tiles decode on background tasks as they load.
        using var manager = new NavMeshStreamingManager(restored, new NavMeshStreamingConfig
        {
            LoadRadius = 30f,
            UnloadHysteresis = 10f,
            MaxTileOperationsPerUpdate = 8
        });

        manager.SetAnchor(1, new Vector3(15f, 0f, 15f));

        for (int i = 0; i < 2000; i++)
        {
            int operations = manager.Update();
            if (operations == 0 && manager.IsIdle)
            {
                break;
            }

            Thread.Sleep(1);
        }

        Assert.True(manager.LoadedTileCount > 1, "Lazily decoded tiles should stream in.");

        using var provider = new DotRecastProvider(TestHelper.CreateTiledTestConfig());
        provider.SetNavMesh(manager.Mesh);

        var path = provider.FindPath(new Vector3(7f, 0f, 7f), new Vector3(25f, 0f, 25f), AgentSettings.Default);
        Assert.True(path.IsValid, "Paths should work on a mesh streamed from a deserialized tile set.");
    }

    [Fact]
    public void TileSetDeserialize_WithWrongMagic_ThrowsInvalidDataException()
    {
        byte[] bogus = new byte[64];
        Assert.Throws<InvalidDataException>(() => NavMeshTileSet.Deserialize(bogus));
    }

    #endregion
}
