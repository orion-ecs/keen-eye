using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// A single navigation mesh tile that can be persisted and streamed independently.
/// </summary>
/// <remarks>
/// <para>
/// Tiles are the unit of persistence and runtime streaming for tiled navigation
/// meshes. Each tile carries its grid coordinate and the Detour mesh data for
/// that cell, serialized with DotRecast's tile format so cross-tile links are
/// rebuilt when the tile is installed into a <see cref="global::DotRecast.Detour.DtNavMesh"/>.
/// </para>
/// <para>
/// Tiles produced by <see cref="DotRecastMeshBuilder.BuildTileSet"/> hold their
/// mesh data in memory. Tiles created via <see cref="Deserialize"/> keep the
/// serialized payload and decode it lazily on first use, which lets the
/// streaming pipeline perform the decode on a background thread.
/// </para>
/// </remarks>
public sealed class NavMeshTile
{
    private const int Magic = 0x4B454E54; // "KENT" - KeenEyes NavMesh Tile
    private const int FormatVersion = 1;

    private readonly Lock materializeLock = new();
    private readonly byte[]? payload;
    private DtMeshData? meshData;

    internal NavMeshTile(DtMeshData meshData, int maxVertsPerPoly)
    {
        this.meshData = meshData;
        TileX = meshData.header.x;
        TileZ = meshData.header.y;
        Layer = meshData.header.layer;
        MaxVertsPerPoly = maxVertsPerPoly;
    }

    private NavMeshTile(byte[] payload, int tileX, int tileZ, int layer, int maxVertsPerPoly)
    {
        this.payload = payload;
        TileX = tileX;
        TileZ = tileZ;
        Layer = layer;
        MaxVertsPerPoly = maxVertsPerPoly;
    }

    /// <summary>
    /// Gets the tile's X coordinate in the tile grid.
    /// </summary>
    public int TileX { get; }

    /// <summary>
    /// Gets the tile's Z coordinate in the tile grid.
    /// </summary>
    public int TileZ { get; }

    /// <summary>
    /// Gets the tile's layer index. Always 0 for meshes built by
    /// <see cref="DotRecastMeshBuilder"/>.
    /// </summary>
    public int Layer { get; }

    /// <summary>
    /// Gets the maximum vertices per polygon the tile was built with, which is
    /// required to decode the serialized mesh data.
    /// </summary>
    internal int MaxVertsPerPoly { get; }

    /// <summary>
    /// Gets whether the tile's mesh data has been decoded into memory.
    /// </summary>
    internal bool IsMaterialized => meshData != null;

    /// <summary>
    /// Gets the tile's Detour mesh data, decoding the serialized payload on
    /// first access. Safe to call from a background thread.
    /// </summary>
    internal DtMeshData GetMeshData()
    {
        if (meshData is { } data)
        {
            return data;
        }

        lock (materializeLock)
        {
            if (meshData == null)
            {
                using var stream = new MemoryStream(payload!);
                using var reader = new BinaryReader(stream);
                meshData = new DtMeshDataReader().Read(reader, MaxVertsPerPoly);
            }

            return meshData;
        }
    }

    /// <summary>
    /// Serializes this tile to a byte array for individual persistence.
    /// </summary>
    /// <returns>The serialized tile, readable by <see cref="Deserialize"/>.</returns>
    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Magic);
        writer.Write(FormatVersion);
        writer.Write(TileX);
        writer.Write(TileZ);
        writer.Write(Layer);
        writer.Write(MaxVertsPerPoly);

        byte[] body = GetPayload();
        writer.Write(body.Length);
        writer.Write(body);

        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a tile previously produced by <see cref="Serialize"/>.
    /// </summary>
    /// <param name="data">The serialized tile bytes.</param>
    /// <returns>
    /// The tile. The mesh data itself is decoded lazily on first use.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data is invalid or an unsupported version.</exception>
    public static NavMeshTile Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        int magic = reader.ReadInt32();
        if (magic != Magic)
        {
            throw new InvalidDataException("Invalid NavMeshTile data: wrong magic number");
        }

        int version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported NavMeshTile version: {version}");
        }

        int tileX = reader.ReadInt32();
        int tileZ = reader.ReadInt32();
        int layer = reader.ReadInt32();
        int maxVertsPerPoly = reader.ReadInt32();

        int payloadLength = reader.ReadInt32();
        byte[] payload = reader.ReadBytes(payloadLength);
        if (payload.Length != payloadLength)
        {
            throw new InvalidDataException("Invalid NavMeshTile data: truncated payload");
        }

        return new NavMeshTile(payload, tileX, tileZ, layer, maxVertsPerPoly);
    }

    private byte[] GetPayload()
    {
        // A tile deserialized but never materialized can round-trip its
        // original payload without decoding it.
        if (meshData == null && payload != null)
        {
            return payload;
        }

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        new DtMeshDataWriter().Write(writer, GetMeshData(), RcByteOrder.LITTLE_ENDIAN, false);
        writer.Flush();
        return stream.ToArray();
    }
}
