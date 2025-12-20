namespace KeenEyes;

/// <summary>
/// Provides memory usage statistics for an ECS world.
/// Useful for monitoring, profiling, and debugging memory consumption.
/// </summary>
/// <remarks>
/// <para>
/// Memory statistics are computed on-demand when requested from the world.
/// This is not a live view and represents a snapshot in time.
/// </para>
/// <para>
/// Statistics include entity allocations, component storage, archetype counts,
/// and pooling efficiency metrics.
/// </para>
/// </remarks>
public readonly struct MemoryStats
{
    /// <summary>
    /// Gets the total number of entities that have ever been allocated.
    /// </summary>
    public int EntitiesAllocated { get; init; }

    /// <summary>
    /// Gets the number of entities currently alive (active).
    /// </summary>
    public int EntitiesActive { get; init; }

    /// <summary>
    /// Gets the number of entity IDs available for recycling.
    /// </summary>
    public int EntitiesRecycled { get; init; }

    /// <summary>
    /// Gets the total number of times entity IDs have been recycled.
    /// </summary>
    public long EntityRecycleCount { get; init; }

    /// <summary>
    /// Gets the number of archetypes in use.
    /// </summary>
    public int ArchetypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered component types.
    /// </summary>
    public int ComponentTypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public int SystemCount { get; init; }

    /// <summary>
    /// Gets the number of cached queries.
    /// </summary>
    public int CachedQueryCount { get; init; }

    /// <summary>
    /// Gets the query cache hit count.
    /// </summary>
    public long QueryCacheHits { get; init; }

    /// <summary>
    /// Gets the query cache miss count.
    /// </summary>
    public long QueryCacheMisses { get; init; }

    /// <summary>
    /// Gets the estimated total bytes used by component storage.
    /// </summary>
    /// <remarks>
    /// This is an estimate based on component sizes and counts.
    /// Actual memory usage may differ due to array pooling and alignment.
    /// </remarks>
    public long EstimatedComponentBytes { get; init; }

    /// <summary>
    /// Gets the entity recycling efficiency as a percentage.
    /// </summary>
    /// <remarks>
    /// Higher values indicate more entity ID reuse, reducing allocation pressure.
    /// Calculated as: RecycleCount / (TotalAllocated - ActiveCount) * 100
    /// </remarks>
    public double RecycleEfficiency
    {
        get
        {
            var destroyed = EntitiesAllocated - EntitiesActive;
            if (destroyed == 0)
            {
                return 0.0;
            }

            return (double)EntityRecycleCount / destroyed * 100.0;
        }
    }

    /// <summary>
    /// Gets the query cache hit rate as a percentage.
    /// </summary>
    public double QueryCacheHitRate
    {
        get
        {
            var total = QueryCacheHits + QueryCacheMisses;
            if (total == 0)
            {
                return 0.0;
            }

            return (double)QueryCacheHits / total * 100.0;
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"""
            MemoryStats:
              Entities: {EntitiesActive} active, {EntitiesAllocated} allocated, {EntitiesRecycled} recycled
              Recycling: {EntityRecycleCount} reuses ({RecycleEfficiency:F1}% efficiency)
              Archetypes: {ArchetypeCount}
              Components: {ComponentTypeCount} types
              Systems: {SystemCount}
              Queries: {CachedQueryCount} cached ({QueryCacheHitRate:F1}% hit rate)
              Storage: ~{EstimatedComponentBytes / 1024.0:F1} KB
            """;
    }
}
