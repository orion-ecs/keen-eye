using System.Numerics;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// A complete set of pre-built navigation mesh tiles plus the grid parameters
/// needed to stream them into a runtime navigation mesh.
/// </summary>
/// <remarks>
/// <para>
/// A tile set is produced by <see cref="DotRecastMeshBuilder.BuildTileSet"/> and
/// consumed by <see cref="NavMeshStreamingManager"/>, which installs and removes
/// individual tiles around registered anchors at runtime.
/// </para>
/// <para>
/// The set can be serialized as a whole with <see cref="Serialize"/>, and each
/// <see cref="NavMeshTile"/> can also be persisted individually via
/// <see cref="NavMeshTile.Serialize"/> for worlds that store tiles as separate
/// assets. Tile sets restored with <see cref="Deserialize"/> keep tiles in their
/// serialized form and decode them lazily as they are streamed in.
/// </para>
/// </remarks>
public sealed class NavMeshTileSet
{
    private const int Magic = 0x4B455453; // "KETS" - KeenEyes Tile Set
    private const int FormatVersion = 1;

    internal NavMeshTileSet(
        Vector3 origin,
        float tileWorldSize,
        int maxTiles,
        int maxPolysPerTile,
        int maxVertsPerPoly,
        AgentSettings builtForAgent,
        IReadOnlyList<NavMeshTile> tiles)
    {
        Origin = origin;
        TileWorldSize = tileWorldSize;
        MaxTiles = maxTiles;
        MaxPolysPerTile = maxPolysPerTile;
        MaxVertsPerPoly = maxVertsPerPoly;
        BuiltForAgent = builtForAgent;
        Tiles = tiles;
    }

    /// <summary>
    /// Gets the world-space origin of the tile grid (minimum corner).
    /// </summary>
    public Vector3 Origin { get; }

    /// <summary>
    /// Gets the side length of a tile in world units.
    /// </summary>
    public float TileWorldSize { get; }

    /// <summary>
    /// Gets the maximum number of tiles the runtime navigation mesh can hold.
    /// </summary>
    public int MaxTiles { get; }

    /// <summary>
    /// Gets the maximum number of polygons per tile.
    /// </summary>
    public int MaxPolysPerTile { get; }

    /// <summary>
    /// Gets the maximum vertices per polygon the tiles were built with.
    /// </summary>
    public int MaxVertsPerPoly { get; }

    /// <summary>
    /// Gets the agent settings the tiles were built for.
    /// </summary>
    public AgentSettings BuiltForAgent { get; }

    /// <summary>
    /// Gets the tiles in this set. Only tiles containing walkable geometry are
    /// present; empty grid cells are omitted.
    /// </summary>
    public IReadOnlyList<NavMeshTile> Tiles { get; }

    /// <summary>
    /// Creates an empty runtime navigation mesh sized for this tile set, ready
    /// for tiles to be streamed in.
    /// </summary>
    internal NavMeshData CreateEmptyMesh()
    {
        var navMeshParams = new DtNavMeshParams
        {
            orig = new RcVec3f(Origin.X, Origin.Y, Origin.Z),
            tileWidth = TileWorldSize,
            tileHeight = TileWorldSize,
            maxTiles = MaxTiles,
            maxPolys = MaxPolysPerTile
        };

        var navMesh = new DtNavMesh();
        navMesh.Init(navMeshParams, MaxVertsPerPoly);
        return new NavMeshData(navMesh, BuiltForAgent);
    }

    /// <summary>
    /// Serializes the tile set (grid parameters and all tiles) to a byte array.
    /// </summary>
    /// <returns>The serialized tile set, readable by <see cref="Deserialize"/>.</returns>
    public byte[] Serialize()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);

        writer.Write(Magic);
        writer.Write(FormatVersion);

        writer.Write(Origin.X);
        writer.Write(Origin.Y);
        writer.Write(Origin.Z);
        writer.Write(TileWorldSize);
        writer.Write(MaxTiles);
        writer.Write(MaxPolysPerTile);
        writer.Write(MaxVertsPerPoly);

        writer.Write(BuiltForAgent.Radius);
        writer.Write(BuiltForAgent.Height);
        writer.Write(BuiltForAgent.MaxSlopeAngle);
        writer.Write(BuiltForAgent.StepHeight);

        writer.Write(Tiles.Count);
        foreach (var tile in Tiles)
        {
            byte[] tileBytes = tile.Serialize();
            writer.Write(tileBytes.Length);
            writer.Write(tileBytes);
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Deserializes a tile set previously produced by <see cref="Serialize"/>.
    /// </summary>
    /// <param name="data">The serialized tile set bytes.</param>
    /// <returns>
    /// The tile set. Individual tiles decode their mesh data lazily when first
    /// streamed in.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when data is null.</exception>
    /// <exception cref="InvalidDataException">Thrown when data is invalid or an unsupported version.</exception>
    public static NavMeshTileSet Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        int magic = reader.ReadInt32();
        if (magic != Magic)
        {
            throw new InvalidDataException("Invalid NavMeshTileSet data: wrong magic number");
        }

        int version = reader.ReadInt32();
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported NavMeshTileSet version: {version}");
        }

        var origin = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        float tileWorldSize = reader.ReadSingle();
        int maxTiles = reader.ReadInt32();
        int maxPolysPerTile = reader.ReadInt32();
        int maxVertsPerPoly = reader.ReadInt32();

        var agent = new AgentSettings(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());

        int tileCount = reader.ReadInt32();
        var tiles = new List<NavMeshTile>(tileCount);
        for (int i = 0; i < tileCount; i++)
        {
            int tileLength = reader.ReadInt32();
            byte[] tileBytes = reader.ReadBytes(tileLength);
            if (tileBytes.Length != tileLength)
            {
                throw new InvalidDataException("Invalid NavMeshTileSet data: truncated tile");
            }

            tiles.Add(NavMeshTile.Deserialize(tileBytes));
        }

        return new NavMeshTileSet(origin, tileWorldSize, maxTiles, maxPolysPerTile, maxVertsPerPoly, agent, tiles);
    }
}
