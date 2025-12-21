using System.Collections.Concurrent;

namespace KeenEyes.Assets;

/// <summary>
/// Thread-safe cache for loaded assets with reference counting and eviction policies.
/// </summary>
/// <param name="policy">Cache eviction policy.</param>
/// <param name="maxSizeBytes">Maximum cache size in bytes.</param>
internal sealed class AssetCache(CachePolicy policy, long maxSizeBytes)
{
    private readonly ConcurrentDictionary<string, AssetEntry> entriesByPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, AssetEntry> entriesById = new();
    private readonly Lock evictionLock = new();

    private int nextId = 1;
    private long currentSizeBytes;
    private long cacheHits;
    private long cacheMisses;

    /// <summary>
    /// Gets the cache policy.
    /// </summary>
    public CachePolicy Policy { get; } = policy;

    /// <summary>
    /// Gets the maximum cache size in bytes.
    /// </summary>
    public long MaxSizeBytes { get; } = maxSizeBytes;

    /// <summary>
    /// Gets the current cache size in bytes.
    /// </summary>
    public long CurrentSizeBytes => Interlocked.Read(ref currentSizeBytes);

    /// <summary>
    /// Gets the number of assets in the cache.
    /// </summary>
    public int Count => entriesById.Count;

    /// <summary>
    /// Gets or creates an entry for the given path.
    /// </summary>
    /// <param name="path">Asset path.</param>
    /// <param name="assetType">Type of asset.</param>
    /// <param name="wasCreated">True if a new entry was created.</param>
    /// <returns>The asset entry.</returns>
    public AssetEntry GetOrCreate(string path, Type assetType, out bool wasCreated)
    {
        if (entriesByPath.TryGetValue(path, out var existing))
        {
            existing.AddRef();
            Interlocked.Increment(ref cacheHits);
            wasCreated = false;
            return existing;
        }

        var id = Interlocked.Increment(ref nextId);
        var entry = new AssetEntry(id, path, assetType);

        // Try to add to both dictionaries atomically
        if (entriesByPath.TryAdd(path, entry))
        {
            entriesById.TryAdd(id, entry);
            Interlocked.Increment(ref cacheMisses);
            wasCreated = true;
            return entry;
        }

        // Another thread added it first
        wasCreated = false;
        var actualEntry = entriesByPath[path];
        actualEntry.AddRef();
        Interlocked.Increment(ref cacheHits);
        return actualEntry;
    }

    /// <summary>
    /// Gets an entry by ID.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <returns>The entry, or null if not found.</returns>
    public AssetEntry? GetById(int id)
        => entriesById.TryGetValue(id, out var entry) ? entry : null;

    /// <summary>
    /// Gets an entry by path.
    /// </summary>
    /// <param name="path">Asset path.</param>
    /// <returns>The entry, or null if not found.</returns>
    public AssetEntry? GetByPath(string path)
        => entriesByPath.TryGetValue(path, out var entry) ? entry : null;

    /// <summary>
    /// Adds reference count to an asset.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <remarks>
    /// When an asset is acquired, its dependencies also have their reference counts
    /// incremented. This ensures dependencies remain loaded while the parent asset
    /// is in use.
    /// </remarks>
    public void AddRef(int id)
    {
        if (entriesById.TryGetValue(id, out var entry))
        {
            entry.AddRef();

            // Also add refs to dependencies
            foreach (var depId in entry.Dependencies)
            {
                AddRef(depId);
            }
        }
    }

    /// <summary>
    /// Registers a dependency between two assets.
    /// </summary>
    /// <param name="parentId">The parent asset ID.</param>
    /// <param name="dependencyId">The dependency asset ID.</param>
    /// <remarks>
    /// When the parent asset is released, all its dependencies are also released.
    /// When the parent asset is acquired, all its dependencies are also acquired.
    /// </remarks>
    public void AddDependency(int parentId, int dependencyId)
    {
        if (entriesById.TryGetValue(parentId, out var entry))
        {
            entry.AddDependency(dependencyId);
        }
    }

    /// <summary>
    /// Releases a reference to an asset.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <returns>True if the asset should be unloaded (refcount hit zero with aggressive policy).</returns>
    /// <remarks>
    /// When an asset is released, its dependencies are also released. This ensures
    /// that assets loaded by other assets (e.g., textures in a glTF model) are properly
    /// reference-counted and can be evicted when no longer needed.
    /// </remarks>
    public bool Release(int id)
    {
        if (!entriesById.TryGetValue(id, out var entry))
        {
            return false;
        }

        var shouldUnload = entry.Release();

        // Release dependencies when this asset is released
        foreach (var depId in entry.Dependencies)
        {
            Release(depId);
        }

        if (shouldUnload && Policy == CachePolicy.Aggressive)
        {
            Evict(id);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Updates the size of an asset in the cache.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    /// <param name="sizeBytes">New size in bytes.</param>
    public void UpdateSize(int id, long sizeBytes)
    {
        if (entriesById.TryGetValue(id, out var entry))
        {
            var delta = sizeBytes - entry.SizeBytes;
            entry.SizeBytes = sizeBytes;
            Interlocked.Add(ref currentSizeBytes, delta);
        }
    }

    /// <summary>
    /// Evicts a specific asset from the cache.
    /// </summary>
    /// <param name="id">Asset ID.</param>
    public void Evict(int id)
    {
        if (!entriesById.TryRemove(id, out var entry))
        {
            return;
        }

        entriesByPath.TryRemove(entry.Path, out _);
        Interlocked.Add(ref currentSizeBytes, -entry.SizeBytes);
        entry.DisposeAsset();
    }

    /// <summary>
    /// Trims the cache to the target size using LRU eviction.
    /// </summary>
    /// <param name="targetBytes">Target size in bytes.</param>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> Only assets with a reference count of zero are
    /// eligible for eviction. Assets that are currently in use (held by active
    /// <see cref="AssetHandle{T}"/> instances) will not be evicted, even if trimming
    /// cannot reach the target size.
    /// </para>
    /// <para>
    /// Eviction order is based on last access time (LRU - Least Recently Used).
    /// The oldest unused assets are evicted first until the target size is reached
    /// or no more eviction candidates remain.
    /// </para>
    /// <para>
    /// This method has no effect when <see cref="Policy"/> is <see cref="CachePolicy.Manual"/>.
    /// </para>
    /// </remarks>
    public void TrimToSize(long targetBytes)
    {
        if (Policy == CachePolicy.Manual || CurrentSizeBytes <= targetBytes)
        {
            return;
        }

        lock (evictionLock)
        {
            // Only evict assets that are not in use (refcount <= 0)
            // Assets with active handles are protected from eviction
            var candidates = entriesById.Values
                .Where(e => e.RefCount <= 0 && e.State == AssetState.Loaded)
                .OrderBy(e => e.LastAccess)
                .ToList();

            foreach (var entry in candidates)
            {
                if (CurrentSizeBytes <= targetBytes)
                {
                    break;
                }

                Evict(entry.Id);
            }
        }
    }

    /// <summary>
    /// Evicts all assets from the cache.
    /// </summary>
    public void Clear()
    {
        foreach (var entry in entriesById.Values.ToList())
        {
            Evict(entry.Id);
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Current cache statistics.</returns>
    public CacheStats GetStats()
    {
        var entries = entriesById.Values.ToList();

        return new CacheStats(
            TotalAssets: entries.Count,
            LoadedAssets: entries.Count(e => e.State == AssetState.Loaded),
            PendingAssets: entries.Count(e => e.State is AssetState.Pending or AssetState.Loading),
            FailedAssets: entries.Count(e => e.State == AssetState.Failed),
            TotalSizeBytes: CurrentSizeBytes,
            MaxSizeBytes: MaxSizeBytes,
            CacheHits: Interlocked.Read(ref cacheHits),
            CacheMisses: Interlocked.Read(ref cacheMisses)
        );
    }

    /// <summary>
    /// Gets all loaded entries (for hot reload enumeration).
    /// </summary>
    /// <returns>Collection of loaded entries.</returns>
    public IEnumerable<AssetEntry> GetLoadedEntries()
        => entriesById.Values.Where(e => e.State == AssetState.Loaded);
}
