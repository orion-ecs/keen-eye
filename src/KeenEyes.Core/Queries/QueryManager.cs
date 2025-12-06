namespace KeenEyes;

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
/// </remarks>
public sealed class QueryManager
{
    private readonly ArchetypeManager archetypeManager;
    private readonly Dictionary<QueryDescriptor, List<Archetype>> cache = [];
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
    public IReadOnlyList<Archetype> GetMatchingArchetypes(QueryDescriptor descriptor)
    {
        if (cache.TryGetValue(descriptor, out var cached))
        {
            Interlocked.Increment(ref cacheHits);
            return cached;
        }

        Interlocked.Increment(ref cacheMisses);

        // Compute matching archetypes
        var matching = new List<Archetype>();
        foreach (var archetype in archetypeManager.Archetypes)
        {
            if (descriptor.Matches(archetype))
            {
                matching.Add(archetype);
            }
        }

        cache[descriptor] = matching;
        return matching;
    }

    /// <summary>
    /// Invalidates the entire cache.
    /// </summary>
    /// <remarks>
    /// This is called automatically when archetypes are created.
    /// Manual invalidation is rarely needed.
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
    public void InvalidateQuery(QueryDescriptor descriptor)
    {
        cache.Remove(descriptor);
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
        // Incremental invalidation: only invalidate queries that could match the new archetype
        var toInvalidate = new List<QueryDescriptor>();

        foreach (var (descriptor, cachedArchetypes) in cache)
        {
            // If the new archetype matches this query, we need to invalidate
            // and re-compute (or incrementally add)
            if (descriptor.Matches(archetype))
            {
                // Use incremental update: add the new archetype to existing cache
                cachedArchetypes.Add(archetype);
            }
        }
    }
}
