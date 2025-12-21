namespace KeenEyes.Assets;

/// <summary>
/// Statistics about the asset cache.
/// </summary>
/// <param name="TotalAssets">Total number of assets tracked.</param>
/// <param name="LoadedAssets">Number of assets currently loaded.</param>
/// <param name="PendingAssets">Number of assets pending or loading.</param>
/// <param name="FailedAssets">Number of assets that failed to load.</param>
/// <param name="TotalSizeBytes">Total size of loaded assets in bytes.</param>
/// <param name="MaxSizeBytes">Maximum cache size in bytes.</param>
/// <param name="CacheHits">Number of cache hits since start.</param>
/// <param name="CacheMisses">Number of cache misses since start.</param>
public readonly record struct CacheStats(
    int TotalAssets,
    int LoadedAssets,
    int PendingAssets,
    int FailedAssets,
    long TotalSizeBytes,
    long MaxSizeBytes,
    long CacheHits,
    long CacheMisses)
{
    /// <summary>
    /// Gets the cache hit ratio (0.0 to 1.0).
    /// </summary>
    public double HitRatio
    {
        get
        {
            var total = CacheHits + CacheMisses;
            return total > 0 ? (double)CacheHits / total : 0.0;
        }
    }

    /// <summary>
    /// Gets the cache utilization ratio (0.0 to 1.0).
    /// </summary>
    public double UtilizationRatio
        => MaxSizeBytes > 0 ? (double)TotalSizeBytes / MaxSizeBytes : 0.0;
}
