namespace KeenEyes;

/// <summary>
/// Manages world statistics collection and reporting.
/// </summary>
/// <remarks>
/// <para>
/// This is an internal manager class that handles all statistics operations.
/// The public API is exposed through <see cref="World"/>.
/// </para>
/// <para>
/// Statistics are computed on-demand and represent a snapshot in time.
/// They include entity allocations, component storage, and pooling metrics.
/// </para>
/// </remarks>
internal sealed class StatisticsManager
{
    private readonly EntityPool entityPool;
    private readonly ArchetypeManager archetypeManager;
    private readonly ComponentRegistry components;
    private readonly SystemManager systemManager;
    private readonly QueryManager queryManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatisticsManager"/> class.
    /// </summary>
    /// <param name="entityPool">The entity pool to gather statistics from.</param>
    /// <param name="archetypeManager">The archetype manager to gather statistics from.</param>
    /// <param name="components">The component registry to gather statistics from.</param>
    /// <param name="systemManager">The system manager to gather statistics from.</param>
    /// <param name="queryManager">The query manager to gather statistics from.</param>
    internal StatisticsManager(
        EntityPool entityPool,
        ArchetypeManager archetypeManager,
        ComponentRegistry components,
        SystemManager systemManager,
        QueryManager queryManager)
    {
        this.entityPool = entityPool;
        this.archetypeManager = archetypeManager;
        this.components = components;
        this.systemManager = systemManager;
        this.queryManager = queryManager;
    }

    /// <summary>
    /// Gets memory usage statistics for the world.
    /// </summary>
    /// <returns>A snapshot of current memory statistics.</returns>
    internal MemoryStats GetMemoryStats()
    {
        // Calculate estimated component bytes
        long estimatedBytes = 0;
        foreach (var archetype in archetypeManager.Archetypes)
        {
            foreach (var componentType in archetype.ComponentTypes)
            {
                var info = components.Get(componentType);
                if (info is not null)
                {
                    estimatedBytes += (long)info.Size * archetype.Count;
                }
            }
        }

        return new MemoryStats
        {
            EntitiesAllocated = entityPool.TotalAllocated,
            EntitiesActive = entityPool.ActiveCount,
            EntitiesRecycled = entityPool.AvailableCount,
            EntityRecycleCount = entityPool.RecycleCount,
            ArchetypeCount = archetypeManager.ArchetypeCount,
            ComponentTypeCount = components.Count,
            SystemCount = systemManager.Count,
            CachedQueryCount = queryManager.CachedQueryCount,
            QueryCacheHits = queryManager.CacheHits,
            QueryCacheMisses = queryManager.CacheMisses,
            EstimatedComponentBytes = estimatedBytes
        };
    }

    /// <summary>
    /// Gets detailed statistics for all archetypes in the world.
    /// </summary>
    /// <returns>A list of statistics for each archetype.</returns>
    internal IReadOnlyList<ArchetypeStatistics> GetArchetypeStatistics()
    {
        var result = new List<ArchetypeStatistics>();

        foreach (var archetype in archetypeManager.Archetypes)
        {
            // Calculate estimated memory for this archetype
            long estimatedBytes = 0;
            foreach (var componentType in archetype.ComponentTypes)
            {
                var info = components.Get(componentType);
                if (info is not null)
                {
                    estimatedBytes += (long)info.Size * archetype.Count;
                }
            }

            // Calculate total capacity (sum of all chunk capacities)
            var totalCapacity = archetype.ChunkCount * ArchetypeChunk.DefaultCapacity;

            // Get component type names
            var componentTypeNames = archetype.ComponentTypes
                .Select(t => t.Name)
                .ToList();

            result.Add(new ArchetypeStatistics
            {
                Id = archetype.Id.GetHashCode(),
                EntityCount = archetype.Count,
                ChunkCount = archetype.ChunkCount,
                TotalCapacity = totalCapacity,
                ComponentTypeNames = componentTypeNames,
                EstimatedMemoryBytes = estimatedBytes
            });
        }

        return result;
    }
}
