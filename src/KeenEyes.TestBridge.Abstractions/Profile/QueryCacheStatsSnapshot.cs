namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of query cache statistics.
/// </summary>
/// <remarks>
/// Provides insight into query caching effectiveness, including hit rate
/// and the number of cached queries.
/// </remarks>
public sealed record QueryCacheStatsSnapshot
{
    /// <summary>
    /// Gets the number of times a cached query result was reused.
    /// </summary>
    public required long CacheHits { get; init; }

    /// <summary>
    /// Gets the number of times a query result was not found in the cache.
    /// </summary>
    public required long CacheMisses { get; init; }

    /// <summary>
    /// Gets the number of unique queries currently cached.
    /// </summary>
    public required int CachedQueryCount { get; init; }

    /// <summary>
    /// Gets the cache hit rate as a percentage (0-100).
    /// </summary>
    public required double HitRate { get; init; }
}
