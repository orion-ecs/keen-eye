namespace KeenEyes;

public sealed partial class World
{
    #region Memory Statistics

    /// <summary>
    /// Gets memory usage statistics for this world.
    /// </summary>
    /// <returns>A snapshot of current memory statistics.</returns>
    /// <remarks>
    /// <para>
    /// Statistics are computed on-demand and represent a snapshot in time.
    /// They include entity allocations, component storage, and pooling metrics.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var stats = world.GetMemoryStats();
    /// Console.WriteLine($"Entities: {stats.EntitiesActive} active");
    /// Console.WriteLine($"Archetypes: {stats.ArchetypeCount}");
    /// Console.WriteLine($"Cache hit rate: {stats.QueryCacheHitRate:F1}%");
    /// </code>
    /// </example>
    public MemoryStats GetMemoryStats()
        => statisticsManager.GetMemoryStats();

    /// <summary>
    /// Gets detailed statistics for all archetypes in the world.
    /// </summary>
    /// <returns>A list of statistics for each archetype.</returns>
    /// <remarks>
    /// <para>
    /// Archetype statistics provide insight into how entities are distributed
    /// across different component combinations and the memory efficiency of each.
    /// </para>
    /// <para>
    /// This is useful for identifying fragmented archetypes or those consuming
    /// unexpectedly large amounts of memory.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var archetypeStats = world.GetArchetypeStatistics();
    /// foreach (var stat in archetypeStats.OrderByDescending(s => s.EntityCount))
    /// {
    ///     Console.WriteLine($"Archetype {stat.Id}: {stat.EntityCount} entities, {stat.UtilizationPercentage:F1}% utilized");
    ///     Console.WriteLine($"  Components: {string.Join(", ", stat.ComponentTypeNames)}");
    /// }
    /// </code>
    /// </example>
    public IReadOnlyList<ArchetypeStatistics> GetArchetypeStatistics()
        => statisticsManager.GetArchetypeStatistics();

    #endregion
}
