using System.Collections.Concurrent;

namespace KeenEyes;

/// <summary>
/// Thread-safe container for cached archetype matches using copy-on-write semantics.
/// </summary>
/// <remarks>
/// <para>
/// This class provides lock-free reads and synchronized writes for archetype caching.
/// Reads return a volatile reference to an immutable array snapshot, while writes
/// atomically replace the entire array under a lock.
/// </para>
/// <para>
/// The copy-on-write pattern is optimal for read-heavy workloads where archetype
/// creation (writes) is rare compared to query execution (reads).
/// </para>
/// </remarks>
internal sealed class ArchetypeCache
{
    private volatile Archetype[] archetypes = [];
    private volatile bool isPopulated;
    private readonly Lock writeLock = new();

    /// <summary>
    /// Gets a value indicating whether the cache has been populated.
    /// </summary>
    public bool IsPopulated => isPopulated;

    /// <summary>
    /// Gets the cached archetypes as a read-only list.
    /// </summary>
    /// <remarks>
    /// This property is lock-free and returns a snapshot of the cached archetypes.
    /// The returned array is safe to iterate even while other threads add archetypes.
    /// </remarks>
    public IReadOnlyList<Archetype> Archetypes => archetypes;

    /// <summary>
    /// Adds an archetype to the cache using copy-on-write semantics.
    /// </summary>
    /// <param name="archetype">The archetype to add.</param>
    public void Add(Archetype archetype)
    {
        lock (writeLock)
        {
            var current = archetypes;
            var newArray = new Archetype[current.Length + 1];
            Array.Copy(current, newArray, current.Length);
            newArray[current.Length] = archetype;
            archetypes = newArray;
        }
    }

    /// <summary>
    /// Sets all archetypes in the cache, replacing any existing entries.
    /// </summary>
    /// <param name="items">The archetypes to cache.</param>
    public void SetAll(List<Archetype> items)
    {
        lock (writeLock)
        {
            archetypes = [.. items];
            isPopulated = true;
        }
    }

    /// <summary>
    /// Populates the cache with matching archetypes if not already populated.
    /// Thread-safe: only the first caller populates, others wait.
    /// </summary>
    /// <param name="allArchetypes">All archetypes to search through.</param>
    /// <param name="descriptor">The query descriptor to match against.</param>
    /// <remarks>
    /// This method ensures that only one thread populates the cache.
    /// Any archetypes added via <see cref="Add"/> during population will be
    /// included in the final result because we use a HashSet to deduplicate.
    /// </remarks>
    public void PopulateIfEmpty(IReadOnlyList<Archetype> allArchetypes, QueryDescriptor descriptor)
    {
        // Fast path: already populated
        if (isPopulated)
        {
            return;
        }

        lock (writeLock)
        {
            // Double-check after acquiring lock
            if (isPopulated)
            {
                return;
            }

            // Find all matching archetypes
            var matching = new List<Archetype>();
            foreach (var archetype in allArchetypes)
            {
                if (descriptor.Matches(archetype))
                {
                    matching.Add(archetype);
                }
            }

            // Merge with any archetypes added by OnArchetypeCreated during our iteration
            // archetypes might have entries from concurrent Add() calls
            var currentArchetypes = archetypes;
            if (currentArchetypes.Length > 0)
            {
                var combined = new HashSet<Archetype>(matching);
                foreach (var arch in currentArchetypes)
                {
                    combined.Add(arch);
                }

                archetypes = [.. combined];
            }
            else
            {
                archetypes = [.. matching];
            }

            isPopulated = true;
        }
    }
}

/// <summary>
/// Manages query caching and archetype matching for efficient query execution.
/// Caches archetype matches per query descriptor and invalidates on archetype changes.
/// </summary>
/// <remarks>
/// <para>
/// The QueryManager maintains a cache of archetype matches for each unique query.
/// On first execution, a query computes which archetypes match and caches the result.
/// Subsequent queries with the same descriptor return cached results in O(1) time.
/// </para>
/// <para>
/// When a new archetype is created (due to entity component changes), the cache
/// is invalidated for queries that could match the new archetype. This uses
/// an incremental invalidation strategy to minimize overhead.
/// </para>
/// <para>
/// This implementation is thread-safe for concurrent query execution. The cache uses
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> for thread-safe entry management
/// and <see cref="ArchetypeCache"/> for lock-free archetype reads with synchronized writes.
/// </para>
/// </remarks>
public sealed class QueryManager
{
    private readonly ArchetypeManager archetypeManager;
    private readonly ConcurrentDictionary<QueryDescriptor, ArchetypeCache> cache = [];
    private long cacheHits;
    private long cacheMisses;
    private int cacheVersion;

    /// <summary>
    /// Gets the number of queries currently cached.
    /// </summary>
    public int CachedQueryCount => cache.Count;

    /// <summary>
    /// Gets the number of cache hits.
    /// </summary>
    public long CacheHits => Interlocked.Read(ref cacheHits);

    /// <summary>
    /// Gets the number of cache misses.
    /// </summary>
    public long CacheMisses => Interlocked.Read(ref cacheMisses);

    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public double HitRate
    {
        get
        {
            var total = CacheHits + CacheMisses;
            if (total == 0)
            {
                return 0.0;
            }

            return (double)CacheHits / total * 100.0;
        }
    }

    /// <summary>
    /// Creates a new QueryManager for the specified archetype manager.
    /// </summary>
    /// <param name="archetypeManager">The archetype manager to query.</param>
    public QueryManager(ArchetypeManager archetypeManager)
    {
        this.archetypeManager = archetypeManager;

        // Subscribe to archetype creation for cache invalidation
        archetypeManager.ArchetypeCreated += OnArchetypeCreated;
    }

    /// <summary>
    /// Gets the archetypes matching the specified query description.
    /// Uses cached results when available.
    /// </summary>
    /// <param name="description">The query description.</param>
    /// <returns>A list of matching archetypes.</returns>
    public IReadOnlyList<Archetype> GetMatchingArchetypes(QueryDescription description)
    {
        var descriptor = QueryDescriptor.FromDescription(description);
        return GetMatchingArchetypes(descriptor);
    }

    /// <summary>
    /// Gets the archetypes matching the specified query descriptor.
    /// Uses cached results when available.
    /// </summary>
    /// <param name="descriptor">The query descriptor.</param>
    /// <returns>A list of matching archetypes.</returns>
    /// <remarks>
    /// This method is thread-safe. Concurrent calls with the same descriptor
    /// may both compute matching archetypes if called simultaneously before
    /// the cache is populated, but the result will be consistent.
    /// </remarks>
    public IReadOnlyList<Archetype> GetMatchingArchetypes(QueryDescriptor descriptor)
    {
        // Check if we have a fully populated cache entry
        if (cache.TryGetValue(descriptor, out var cached) && cached.IsPopulated)
        {
            Interlocked.Increment(ref cacheHits);
            return cached.Archetypes;
        }

        Interlocked.Increment(ref cacheMisses);

        // Create cache entry first so OnArchetypeCreated can add to it during iteration
        // This prevents a race where archetypes created during iteration are missed
        var archetypeCache = cache.GetOrAdd(descriptor, _ => new ArchetypeCache());

        // Populate the cache with matching archetypes
        // OnArchetypeCreated will add any new archetypes created during this iteration
        archetypeCache.PopulateIfEmpty(archetypeManager.Archetypes, descriptor);

        return archetypeCache.Archetypes;
    }

    /// <summary>
    /// Invalidates the entire cache.
    /// </summary>
    /// <remarks>
    /// This method is thread-safe. New queries will recompute matching archetypes
    /// after invalidation.
    /// </remarks>
    public void InvalidateCache()
    {
        cache.Clear();
        Interlocked.Increment(ref cacheVersion);
    }

    /// <summary>
    /// Invalidates a specific query from the cache.
    /// </summary>
    /// <param name="descriptor">The query descriptor to invalidate.</param>
    /// <remarks>
    /// This method is thread-safe. The next query with this descriptor will
    /// recompute matching archetypes.
    /// </remarks>
    public void InvalidateQuery(QueryDescriptor descriptor)
    {
        cache.TryRemove(descriptor, out _);
    }

    /// <summary>
    /// Clears all statistics.
    /// </summary>
    public void ResetStatistics()
    {
        Interlocked.Exchange(ref cacheHits, 0);
        Interlocked.Exchange(ref cacheMisses, 0);
    }

    private void OnArchetypeCreated(Archetype archetype)
    {
        // Incremental invalidation: only update queries that could match the new archetype
        // This is thread-safe: ConcurrentDictionary allows iteration while other threads modify it,
        // and ArchetypeCache.Add is thread-safe using copy-on-write semantics.
        foreach (var (descriptor, archetypeCache) in cache)
        {
            // If the new archetype matches this query, incrementally add it
            if (descriptor.Matches(archetype))
            {
                archetypeCache.Add(archetype);
            }
        }
    }
}
