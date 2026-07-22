using System.Numerics;
using DotRecast.Core;
using DotRecast.Core.Numerics;
using DotRecast.Detour;
using DotRecast.Detour.Io;
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
    // Serialized format version. Bumped to 2 when the tile payload switched to
    // DotRecast's mesh-set format (full multi-tile topology, BV tree, detail meshes).
    private const int FormatVersion = 2;

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
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Magic number and version
        writer.Write(0x4B454E4D); // "KENM" - KeenEyes NavMesh
        writer.Write(FormatVersion);

        // Agent settings
        writer.Write(builtForAgent.Radius);
        writer.Write(builtForAgent.Height);
        writer.Write(builtForAgent.MaxSlopeAngle);
        writer.Write(builtForAgent.StepHeight);

        // Max vertices per polygon (required to re-read the mesh set)
        writer.Write(navMesh.GetMaxVertsPerPoly());

        // Serialize the full navmesh (params + every tile, including BV tree and detail
        // meshes) with DotRecast's own mesh-set writer. This preserves multi-tile
        // topology so cross-tile links are rebuilt on load.
        byte[] meshSet = SerializeNavMesh();
        writer.Write(meshSet.Length);
        writer.Write(meshSet);

        // ID
        writer.Write(id);

        return ms.ToArray();
    }

    private byte[] SerializeNavMesh()
    {
        using var meshStream = new MemoryStream();
        using var meshWriter = new BinaryWriter(meshStream);
        new DtMeshSetWriter().Write(meshWriter, navMesh, RcByteOrder.LITTLE_ENDIAN, false);
        meshWriter.Flush();
        return meshStream.ToArray();
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
        if (version != FormatVersion)
        {
            throw new InvalidDataException($"Unsupported NavMesh version: {version}");
        }

        // Agent settings
        float agentRadius = reader.ReadSingle();
        float agentHeight = reader.ReadSingle();
        float maxSlope = reader.ReadSingle();
        float stepHeight = reader.ReadSingle();
        var agentSettings = new AgentSettings(agentRadius, agentHeight, maxSlope, stepHeight);

        int maxVertsPerPoly = reader.ReadInt32();

        // Read the mesh-set body and rebuild the full multi-tile navmesh.
        int meshSetLength = reader.ReadInt32();
        byte[] meshSet = reader.ReadBytes(meshSetLength);
        var navMesh = DeserializeNavMesh(meshSet, maxVertsPerPoly);

        // ID
        string id = reader.ReadString();

        return new NavMeshData(navMesh, agentSettings, id);
    }

    private static DtNavMesh DeserializeNavMesh(byte[] meshSet, int maxVertsPerPoly)
    {
        using var meshStream = new MemoryStream(meshSet);
        using var meshReader = new BinaryReader(meshStream);
        return new DtMeshSetReader().Read(meshReader, maxVertsPerPoly);
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
