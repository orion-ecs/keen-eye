using System.Numerics;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using KeenEyes.Navigation.Abstractions;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Wraps a DotRecast navigation mesh to implement the KeenEyes navigation mesh interface.
/// </summary>
/// <remarks>
/// <para>
/// This class provides a serializable wrapper around <see cref="DtNavMesh"/> that
/// implements <see cref="INavigationMesh"/> for integration with the KeenEyes
/// navigation system.
/// </para>
/// <para>
/// The mesh data can be serialized and deserialized for efficient loading and
/// caching of prebuilt navigation meshes.
/// </para>
/// </remarks>
public sealed class NavMeshData : INavigationMesh
{
    private readonly DtNavMesh navMesh;
    private readonly AgentSettings builtForAgent;
    private readonly string id;

    /// <summary>
    /// Creates a new NavMeshData wrapper around a DotRecast navigation mesh.
    /// </summary>
    /// <param name="navMesh">The DotRecast navigation mesh.</param>
    /// <param name="builtForAgent">The agent settings the mesh was built for.</param>
    /// <param name="id">Optional unique identifier. If not provided, a GUID is generated.</param>
    /// <exception cref="ArgumentNullException">Thrown when navMesh is null.</exception>
    public NavMeshData(DtNavMesh navMesh, AgentSettings builtForAgent, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(navMesh);

        this.navMesh = navMesh;
        this.builtForAgent = builtForAgent;
        this.id = id ?? Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Gets the underlying DotRecast navigation mesh.
    /// </summary>
    internal DtNavMesh InternalNavMesh => navMesh;

    /// <inheritdoc/>
    public string Id => id;

    /// <inheritdoc/>
    public (Vector3 Min, Vector3 Max) Bounds
    {
        get
        {
            navMesh.ComputeBounds(out var bmin, out var bmax);
            return (ToVector3(bmin), ToVector3(bmax));
        }
    }

    /// <inheritdoc/>
    public int PolygonCount
    {
        get
        {
            int count = 0;
            int maxTiles = navMesh.GetMaxTiles();

            for (int i = 0; i < maxTiles; i++)
            {
                var tile = navMesh.GetTile(i);
                if (tile?.data != null)
                {
                    count += tile.data.header.polyCount;
                }
            }

            return count;
        }
    }

    /// <inheritdoc/>
    public int VertexCount
    {
        get
        {
            int count = 0;
            int maxTiles = navMesh.GetMaxTiles();

            for (int i = 0; i < maxTiles; i++)
            {
                var tile = navMesh.GetTile(i);
                if (tile?.data != null)
                {
                    count += tile.data.header.vertCount;
                }
            }

            return count;
        }
    }

    /// <inheritdoc/>
    public AgentSettings BuiltForAgent => builtForAgent;

    /// <inheritdoc/>
    public NavPoint? FindNearestPoint(Vector3 position, float searchRadius = 10f)
    {
        var query = new DtNavMeshQuery(navMesh);
        var filter = new DtQueryDefaultFilter();

        var center = ToRcVec3f(position);
        var extents = new RcVec3f(searchRadius, searchRadius, searchRadius);

        var status = query.FindNearestPoly(center, extents, filter, out var nearestRef, out var nearestPt, out _);

        if (status.Failed() || nearestRef == 0)
        {
            return null;
        }

        navMesh.GetPolyArea(nearestRef, out var area);
        return new NavPoint(ToVector3(nearestPt), (NavAreaType)area, (uint)nearestRef);
    }

    /// <inheritdoc/>
    public NavPoint? GetRandomPoint(NavAreaMask areaMask = NavAreaMask.All)
    {
        var query = new DtNavMeshQuery(navMesh);
        var filter = CreateFilter(areaMask);
        var rand = new DotRecastRandom();

        var status = query.FindRandomPoint(filter, rand, out var randomRef, out var randomPt);

        if (status.Failed() || randomRef == 0)
        {
            return null;
        }

        navMesh.GetPolyArea(randomRef, out var area);
        return new NavPoint(ToVector3(randomPt), (NavAreaType)area, (uint)randomRef);
    }

    /// <inheritdoc/>
    public NavPoint? GetRandomPointInRadius(Vector3 center, float radius, NavAreaMask areaMask = NavAreaMask.All)
    {
        var query = new DtNavMeshQuery(navMesh);
        var filter = CreateFilter(areaMask);
        var rand = new DotRecastRandom();

        var centerVec = ToRcVec3f(center);
        var extents = new RcVec3f(radius, radius, radius);

        // First find the starting polygon
        var status = query.FindNearestPoly(centerVec, extents, filter, out var startRef, out _, out _);
        if (status.Failed() || startRef == 0)
        {
            return null;
        }

        status = query.FindRandomPointAroundCircle(startRef, centerVec, radius, filter, rand, out var randomRef, out var randomPt);

        if (status.Failed() || randomRef == 0)
        {
            return null;
        }

        navMesh.GetPolyArea(randomRef, out var area);
        return new NavPoint(ToVector3(randomPt), (NavAreaType)area, (uint)randomRef);
    }

    /// <inheritdoc/>
    public NavAreaType GetAreaType(Vector3 position)
    {
        var query = new DtNavMeshQuery(navMesh);
        var filter = new DtQueryDefaultFilter();

        var center = ToRcVec3f(position);
        var extents = new RcVec3f(0.5f, 2.0f, 0.5f);

        var status = query.FindNearestPoly(center, extents, filter, out var polyRef, out _, out _);

        if (status.Failed() || polyRef == 0)
        {
            return NavAreaType.NotWalkable;
        }

        navMesh.GetPolyArea(polyRef, out var area);
        return (NavAreaType)area;
    }

    /// <inheritdoc/>
    public bool IsOnNavMesh(Vector3 position, float tolerance = 0.5f)
    {
        var query = new DtNavMeshQuery(navMesh);
        var filter = new DtQueryDefaultFilter();

        var center = ToRcVec3f(position);
        var extents = new RcVec3f(tolerance, tolerance * 4, tolerance);

        var status = query.FindNearestPoly(center, extents, filter, out var polyRef, out var nearestPt, out _);

        if (status.Failed() || polyRef == 0)
        {
            return false;
        }

        // Check if the nearest point is within tolerance
        var dist = Vector3.Distance(position, ToVector3(nearestPt));
        return dist <= tolerance;
    }

    /// <inheritdoc/>
    public byte[] Serialize()
    {
        var tiles = new List<byte[]>();
        int maxTiles = navMesh.GetMaxTiles();

        for (int i = 0; i < maxTiles; i++)
        {
            var tile = navMesh.GetTile(i);
            if (tile?.data != null)
            {
                tiles.Add(SerializeTile(tile));
            }
        }

        // Write header + all tiles
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic number and version
        writer.Write(0x4B454E4D); // "KENM" - KeenEyes NavMesh
        writer.Write(1); // Version

        // Agent settings
        writer.Write(builtForAgent.Radius);
        writer.Write(builtForAgent.Height);
        writer.Write(builtForAgent.MaxSlopeAngle);
        writer.Write(builtForAgent.StepHeight);

        // NavMesh params
        navMesh.ComputeBounds(out var bmin, out var bmax);
        writer.Write(bmin.X);
        writer.Write(bmin.Y);
        writer.Write(bmin.Z);
        writer.Write(bmax.X);
        writer.Write(bmax.Y);
        writer.Write(bmax.Z);
        writer.Write(navMesh.GetMaxVertsPerPoly());

        // Tile count and data
        writer.Write(tiles.Count);
        foreach (var tileData in tiles)
        {
            writer.Write(tileData.Length);
            writer.Write(tileData);
        }

        // ID
        writer.Write(id);

        return ms.ToArray();
    }

    /// <inheritdoc/>
    public ReadOnlySpan<Vector3> GetPolygonVertices(uint polygonId)
    {
        navMesh.GetTileAndPolyByRef(polygonId, out var tile, out var poly);

        if (tile?.data == null || poly == null)
        {
            return ReadOnlySpan<Vector3>.Empty;
        }

        int vertCount = poly.vertCount;
        var vertices = new Vector3[vertCount];

        for (int i = 0; i < vertCount; i++)
        {
            int vertIndex = poly.verts[i] * 3;
            vertices[i] = new Vector3(
                tile.data.verts[vertIndex],
                tile.data.verts[vertIndex + 1],
                tile.data.verts[vertIndex + 2]);
        }

        return vertices;
    }

    /// <summary>
    /// Deserializes a NavMeshData from a byte array.
    /// </summary>
    /// <param name="data">The serialized data.</param>
    /// <returns>The deserialized navigation mesh.</returns>
    /// <exception cref="InvalidDataException">Thrown when data is invalid or corrupted.</exception>
    public static NavMeshData Deserialize(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Magic number and version
        int magic = reader.ReadInt32();
        if (magic != 0x4B454E4D)
        {
            throw new InvalidDataException("Invalid NavMesh data: wrong magic number");
        }

        int version = reader.ReadInt32();
        if (version != 1)
        {
            throw new InvalidDataException($"Unsupported NavMesh version: {version}");
        }

        // Agent settings
        float agentRadius = reader.ReadSingle();
        float agentHeight = reader.ReadSingle();
        float maxSlope = reader.ReadSingle();
        float stepHeight = reader.ReadSingle();
        var agentSettings = new AgentSettings(agentRadius, agentHeight, maxSlope, stepHeight);

        // NavMesh params
        var bmin = new RcVec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        var bmax = new RcVec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        int maxVertsPerPoly = reader.ReadInt32();

        // Create navmesh with params
        var navMeshParams = new DtNavMeshParams
        {
            orig = bmin,
            tileWidth = bmax.X - bmin.X,
            tileHeight = bmax.Z - bmin.Z,
            maxTiles = 1024, // Reasonable default
            maxPolys = 65535
        };

        var navMesh = new DtNavMesh();
        navMesh.Init(navMeshParams, maxVertsPerPoly);

        // Read tiles
        int tileCount = reader.ReadInt32();
        for (int i = 0; i < tileCount; i++)
        {
            int tileLength = reader.ReadInt32();
            byte[] tileData = reader.ReadBytes(tileLength);
            var meshData = DeserializeTile(tileData);
            if (meshData != null)
            {
                navMesh.AddTile(meshData, 0, 0, out _);
            }
        }

        // ID
        string id = reader.ReadString();

        return new NavMeshData(navMesh, agentSettings, id);
    }

    private static byte[] SerializeTile(DtMeshTile tile)
    {
        // Serialize tile data to bytes
        // This is a simplified serialization - full implementation would use DtNavMeshDataWriter
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        var header = tile.data.header;

        // Write header
        writer.Write(header.magic);
        writer.Write(header.version);
        writer.Write(header.x);
        writer.Write(header.y);
        writer.Write(header.layer);
        writer.Write(header.userId);
        writer.Write(header.polyCount);
        writer.Write(header.vertCount);
        writer.Write(header.maxLinkCount);
        writer.Write(header.detailMeshCount);
        writer.Write(header.detailVertCount);
        writer.Write(header.detailTriCount);
        writer.Write(header.bvNodeCount);
        writer.Write(header.offMeshConCount);
        writer.Write(header.offMeshBase);
        writer.Write(header.walkableHeight);
        writer.Write(header.walkableRadius);
        writer.Write(header.walkableClimb);
        writer.Write(header.bmin.X);
        writer.Write(header.bmin.Y);
        writer.Write(header.bmin.Z);
        writer.Write(header.bmax.X);
        writer.Write(header.bmax.Y);
        writer.Write(header.bmax.Z);
        writer.Write(header.bvQuantFactor);

        // Write vertices
        foreach (float v in tile.data.verts)
        {
            writer.Write(v);
        }

        // Write polygons
        foreach (var poly in tile.data.polys)
        {
            writer.Write(poly.firstLink);
            for (int i = 0; i < poly.verts.Length; i++)
            {
                writer.Write(poly.verts[i]);
            }
            for (int i = 0; i < poly.neis.Length; i++)
            {
                writer.Write(poly.neis[i]);
            }
            writer.Write(poly.flags);
            writer.Write(poly.vertCount);
            writer.Write((byte)poly.areaAndtype);
        }

        return ms.ToArray();
    }

    private static DtMeshData? DeserializeTile(byte[] data)
    {
        // Simplified deserialization - full implementation would use DtNavMeshDataReader
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var meshData = new DtMeshData
        {
            header = new DtMeshHeader()
        };

        meshData.header.magic = reader.ReadInt32();
        meshData.header.version = reader.ReadInt32();
        meshData.header.x = reader.ReadInt32();
        meshData.header.y = reader.ReadInt32();
        meshData.header.layer = reader.ReadInt32();
        meshData.header.userId = reader.ReadInt32();
        meshData.header.polyCount = reader.ReadInt32();
        meshData.header.vertCount = reader.ReadInt32();
        meshData.header.maxLinkCount = reader.ReadInt32();
        meshData.header.detailMeshCount = reader.ReadInt32();
        meshData.header.detailVertCount = reader.ReadInt32();
        meshData.header.detailTriCount = reader.ReadInt32();
        meshData.header.bvNodeCount = reader.ReadInt32();
        meshData.header.offMeshConCount = reader.ReadInt32();
        meshData.header.offMeshBase = reader.ReadInt32();
        meshData.header.walkableHeight = reader.ReadSingle();
        meshData.header.walkableRadius = reader.ReadSingle();
        meshData.header.walkableClimb = reader.ReadSingle();
        meshData.header.bmin = new RcVec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        meshData.header.bmax = new RcVec3f(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        meshData.header.bvQuantFactor = reader.ReadSingle();

        // Read vertices
        meshData.verts = new float[meshData.header.vertCount * 3];
        for (int i = 0; i < meshData.verts.Length; i++)
        {
            meshData.verts[i] = reader.ReadSingle();
        }

        // Read polygons
        meshData.polys = new DtPoly[meshData.header.polyCount];
        for (int i = 0; i < meshData.header.polyCount; i++)
        {
            int firstLink = reader.ReadInt32();

            // Read vert indices
            ushort[] verts = new ushort[6];
            for (int j = 0; j < 6; j++)
            {
                verts[j] = reader.ReadUInt16();
            }

            // Read neighbor indices
            ushort[] neis = new ushort[6];
            for (int j = 0; j < 6; j++)
            {
                neis[j] = reader.ReadUInt16();
            }

            int flags = reader.ReadInt32();
            byte vertCount = reader.ReadByte();
            byte areaAndtype = reader.ReadByte();

            var poly = new DtPoly(i, 6)
            {
                firstLink = firstLink,
                flags = flags,
                vertCount = vertCount,
                areaAndtype = areaAndtype
            };
            Array.Copy(verts, poly.verts, 6);
            Array.Copy(neis, poly.neis, 6);
            meshData.polys[i] = poly;
        }

        return meshData;
    }

    private static IDtQueryFilter CreateFilter(NavAreaMask areaMask)
    {
        var filter = new DtQueryDefaultFilter();

        // Set include flags based on area mask
        filter.SetIncludeFlags((int)areaMask);

        return filter;
    }

    private static Vector3 ToVector3(RcVec3f v) => new(v.X, v.Y, v.Z);

    private static RcVec3f ToRcVec3f(Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>
    /// Simple random number generator for DotRecast.
    /// </summary>
    private sealed class DotRecastRandom : IRcRand
    {
        private readonly Random random = new();

        public float Next() => (float)random.NextDouble();

        public double NextDouble() => random.NextDouble();

        public int NextInt32() => random.Next();
    }
}
