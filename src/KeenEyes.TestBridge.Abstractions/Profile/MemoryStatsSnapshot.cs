namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of memory usage statistics for an ECS world.
/// </summary>
/// <remarks>
/// Provides a comprehensive view of entity allocations, component storage,
/// archetype counts, and query cache efficiency.
/// </remarks>
public sealed record MemoryStatsSnapshot
{
    /// <summary>
    /// Gets the total number of entities that have ever been allocated.
    /// </summary>
    public required int EntitiesAllocated { get; init; }

    /// <summary>
    /// Gets the number of entities currently alive (active).
    /// </summary>
    public required int EntitiesActive { get; init; }

    /// <summary>
    /// Gets the number of entity IDs available for recycling.
    /// </summary>
    public required int EntitiesRecycled { get; init; }

    /// <summary>
    /// Gets the total number of times entity IDs have been recycled.
    /// </summary>
    public required long EntityRecycleCount { get; init; }

    /// <summary>
    /// Gets the number of archetypes in use.
    /// </summary>
    public required int ArchetypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered component types.
    /// </summary>
    public required int ComponentTypeCount { get; init; }

    /// <summary>
    /// Gets the number of registered systems.
    /// </summary>
    public required int SystemCount { get; init; }

    /// <summary>
    /// Gets the number of cached queries.
    /// </summary>
    public required int CachedQueryCount { get; init; }

    /// <summary>
    /// Gets the query cache hit count.
    /// </summary>
    public required long QueryCacheHits { get; init; }

    /// <summary>
    /// Gets the query cache miss count.
    /// </summary>
    public required long QueryCacheMisses { get; init; }

    /// <summary>
    /// Gets the estimated total bytes used by component storage.
    /// </summary>
    public required long EstimatedComponentBytes { get; init; }

    /// <summary>
    /// Gets the entity recycling efficiency as a percentage (0-100).
    /// </summary>
    public required double RecycleEfficiency { get; init; }

    /// <summary>
    /// Gets the query cache hit rate as a percentage (0-100).
    /// </summary>
    public required double QueryCacheHitRate { get; init; }
}
