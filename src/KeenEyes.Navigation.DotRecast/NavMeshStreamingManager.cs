using System.Collections.Concurrent;
using System.Numerics;
using DotRecast.Detour;

namespace KeenEyes.Navigation.DotRecast;

/// <summary>
/// Streams navigation mesh tiles in and out of a runtime navmesh based on the
/// positions of registered anchors.
/// </summary>
/// <remarks>
/// <para>
/// The manager owns an initially empty navigation mesh (<see cref="Mesh"/>)
/// sized for a pre-built <see cref="NavMeshTileSet"/>. Each <see cref="Update"/>
/// evaluates tile residency: tiles within <see cref="NavMeshStreamingConfig.LoadRadius"/>
/// of any anchor are installed, and loaded tiles beyond the radius plus
/// <see cref="NavMeshStreamingConfig.UnloadHysteresis"/> are removed. At most
/// <see cref="NavMeshStreamingConfig.MaxTileOperationsPerUpdate"/> install/remove
/// operations are applied per call.
/// </para>
/// <para>
/// Tiles that still hold their serialized form (a tile set restored with
/// <see cref="NavMeshTileSet.Deserialize"/>) are decoded on background tasks;
/// the navigation mesh itself is only ever mutated inside <see cref="Update"/>,
/// so all pathfinding structures stay single-threaded. Install the mesh once
/// via <see cref="DotRecastProvider.SetNavMesh"/> — tiles are added and removed
/// in place, so the crowd simulation is not recreated by streaming operations.
/// </para>
/// <para>
/// This class is not thread-safe; drive it from the world update thread.
/// </para>
/// </remarks>
public sealed class NavMeshStreamingManager : IDisposable
{
    private readonly NavMeshStreamingConfig config;
    private readonly NavMeshData mesh;
    private readonly Vector3 origin;
    private readonly float tileWorldSize;

    private readonly Dictionary<(int X, int Z), NavMeshTile> tilesByCoord = [];
    private readonly Dictionary<(int X, int Z), long> loadedTiles = [];
    private readonly HashSet<(int X, int Z)> pendingLoads = [];
    private readonly ConcurrentQueue<NavMeshTile> materializedQueue = new();
    private readonly List<NavMeshTile> readyTiles = [];
    private readonly List<(int X, int Z)> unloadScratch = [];
    private readonly Dictionary<int, Vector3> anchors = [];
    private bool disposed;

    /// <summary>
    /// Creates a streaming manager for a pre-built tile set.
    /// </summary>
    /// <param name="tileSet">The tile set to stream from.</param>
    /// <param name="config">The streaming configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when tileSet or config is null.</exception>
    /// <exception cref="ArgumentException">Thrown when config validation fails.</exception>
    public NavMeshStreamingManager(NavMeshTileSet tileSet, NavMeshStreamingConfig config)
    {
        ArgumentNullException.ThrowIfNull(tileSet);
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException(error, nameof(config));
        }

        this.config = config;
        origin = tileSet.Origin;
        tileWorldSize = tileSet.TileWorldSize;
        mesh = tileSet.CreateEmptyMesh();

        foreach (var tile in tileSet.Tiles)
        {
            tilesByCoord[(tile.TileX, tile.TileZ)] = tile;
        }
    }

    /// <summary>
    /// Gets the streamed navigation mesh. Install it on a provider once via
    /// <see cref="DotRecastProvider.SetNavMesh"/>; streaming mutates it in place.
    /// </summary>
    public NavMeshData Mesh => mesh;

    /// <summary>
    /// Gets the number of tiles currently installed in the navigation mesh.
    /// </summary>
    public int LoadedTileCount => loadedTiles.Count;

    /// <summary>
    /// Gets the number of registered anchors.
    /// </summary>
    public int AnchorCount => anchors.Count;

    /// <summary>
    /// Gets whether no tile loads are in flight or awaiting installation.
    /// </summary>
    /// <remarks>
    /// When this is true and <see cref="Update"/> returns 0, tile residency has
    /// reached a steady state for the current anchor positions.
    /// </remarks>
    public bool IsIdle => pendingLoads.Count == 0 && readyTiles.Count == 0 && materializedQueue.IsEmpty;

    /// <summary>
    /// Checks whether the tile at the given grid coordinate is currently
    /// installed in the navigation mesh.
    /// </summary>
    /// <param name="tileX">The tile's X coordinate.</param>
    /// <param name="tileZ">The tile's Z coordinate.</param>
    /// <returns>True if the tile is loaded.</returns>
    public bool IsTileLoaded(int tileX, int tileZ) => loadedTiles.ContainsKey((tileX, tileZ));

    /// <summary>
    /// Gets the grid coordinate of the tile containing the given world position.
    /// </summary>
    /// <param name="position">The world position.</param>
    /// <returns>The tile coordinate; the tile may or may not exist in the set.</returns>
    public (int TileX, int TileZ) GetTileCoordinate(Vector3 position)
    {
        int tileX = (int)MathF.Floor((position.X - origin.X) / tileWorldSize);
        int tileZ = (int)MathF.Floor((position.Z - origin.Z) / tileWorldSize);
        return (tileX, tileZ);
    }

    /// <summary>
    /// Registers or moves a streaming anchor. Tiles are kept resident around
    /// all registered anchors.
    /// </summary>
    /// <param name="anchorId">A caller-chosen identifier for the anchor.</param>
    /// <param name="position">The anchor's world position.</param>
    public void SetAnchor(int anchorId, Vector3 position)
    {
        ThrowIfDisposed();
        anchors[anchorId] = position;
    }

    /// <summary>
    /// Removes a streaming anchor. Tiles held resident only by this anchor
    /// will unload on subsequent updates.
    /// </summary>
    /// <param name="anchorId">The anchor identifier passed to <see cref="SetAnchor"/>.</param>
    /// <returns>True if the anchor was registered.</returns>
    public bool RemoveAnchor(int anchorId)
    {
        ThrowIfDisposed();
        return anchors.Remove(anchorId);
    }

    /// <summary>
    /// Evaluates tile residency and applies up to
    /// <see cref="NavMeshStreamingConfig.MaxTileOperationsPerUpdate"/> tile
    /// install/remove operations. Call once per frame from the main thread.
    /// </summary>
    /// <returns>The number of tile operations applied this call.</returns>
    public int Update()
    {
        ThrowIfDisposed();

        // Collect tiles whose background decode finished since the last update.
        while (materializedQueue.TryDequeue(out var materialized))
        {
            readyTiles.Add(materialized);
        }

        StartRequiredLoads();

        int budget = config.MaxTileOperationsPerUpdate;
        int operations = ApplyReadyLoads(budget);
        operations += ApplyUnloads(budget - operations);
        return operations;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        anchors.Clear();
        pendingLoads.Clear();
        readyTiles.Clear();
        disposed = true;
    }

    /// <summary>
    /// Begins loading every tile that should be resident but is neither loaded
    /// nor already pending. Resident (already decoded) tiles are staged
    /// directly; serialized tiles are decoded on a background task first.
    /// </summary>
    private void StartRequiredLoads()
    {
        foreach (var (coord, tile) in tilesByCoord)
        {
            if (loadedTiles.ContainsKey(coord) || pendingLoads.Contains(coord))
            {
                continue;
            }

            if (DistanceToNearestAnchor(coord) > config.LoadRadius)
            {
                continue;
            }

            pendingLoads.Add(coord);

            if (tile.IsMaterialized)
            {
                readyTiles.Add(tile);
            }
            else
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        tile.GetMeshData();
                    }
                    catch (Exception)
                    {
                        // Never fault the background task: corrupt payloads
                        // surface deterministically on the main thread when
                        // the install re-attempts the decode.
                    }

                    materializedQueue.Enqueue(tile);
                });
            }
        }
    }

    private int ApplyReadyLoads(int budget)
    {
        int operations = 0;
        int index = 0;

        while (index < readyTiles.Count && operations < budget)
        {
            var tile = readyTiles[index];
            var coord = (tile.TileX, tile.TileZ);

            // Anchors may have moved while the tile was decoding; drop loads
            // that are no longer wanted without spending navmesh operations.
            if (DistanceToNearestAnchor(coord) > config.LoadRadius)
            {
                readyTiles.RemoveAt(index);
                pendingLoads.Remove(coord);
                continue;
            }

            var status = mesh.InternalNavMesh.AddTile(tile.GetMeshData(), 0, 0, out long tileRef);
            if (status.Succeeded())
            {
                loadedTiles[coord] = tileRef;
            }

            readyTiles.RemoveAt(index);
            pendingLoads.Remove(coord);
            operations++;
        }

        return operations;
    }

    private int ApplyUnloads(int budget)
    {
        if (budget <= 0)
        {
            return 0;
        }

        float unloadDistance = config.LoadRadius + config.UnloadHysteresis;

        unloadScratch.Clear();
        foreach (var coord in loadedTiles.Keys)
        {
            if (unloadScratch.Count >= budget)
            {
                break;
            }

            if (DistanceToNearestAnchor(coord) > unloadDistance)
            {
                unloadScratch.Add(coord);
            }
        }

        foreach (var coord in unloadScratch)
        {
            mesh.InternalNavMesh.RemoveTile(loadedTiles[coord]);
            loadedTiles.Remove(coord);
        }

        return unloadScratch.Count;
    }

    /// <summary>
    /// Computes the XZ-plane distance from the tile's footprint (not its
    /// center) to the nearest anchor, so a tile containing an anchor is always
    /// at distance zero. Returns infinity when no anchors are registered.
    /// </summary>
    private float DistanceToNearestAnchor((int X, int Z) coord)
    {
        float minX = origin.X + coord.X * tileWorldSize;
        float minZ = origin.Z + coord.Z * tileWorldSize;
        float maxX = minX + tileWorldSize;
        float maxZ = minZ + tileWorldSize;

        float nearest = float.PositiveInfinity;
        foreach (var position in anchors.Values)
        {
            float dx = MathF.Max(MathF.Max(minX - position.X, 0f), position.X - maxX);
            float dz = MathF.Max(MathF.Max(minZ - position.Z, 0f), position.Z - maxZ);
            float distance = MathF.Sqrt(dx * dx + dz * dz);

            if (distance < nearest)
            {
                nearest = distance;
            }
        }

        return nearest;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
