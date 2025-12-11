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

    #endregion
}
