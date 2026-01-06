namespace KeenEyes.Capabilities;

/// <summary>
/// Capability interface for accessing world memory and performance statistics.
/// </summary>
/// <remarks>
/// <para>
/// This capability provides access to memory usage statistics for profiling
/// and diagnostics. Statistics include entity allocations, component storage,
/// and pooling metrics.
/// </para>
/// <para>
/// Plugins that need to monitor world performance should request this capability via
/// <see cref="IPluginContext.GetCapability{T}"/> rather than casting to
/// the concrete World type.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public void Install(IPluginContext context)
/// {
///     if (context.TryGetCapability&lt;IStatisticsCapability&gt;(out var stats))
///     {
///         var memStats = stats.GetMemoryStats();
///         Console.WriteLine($"Entities: {memStats.EntitiesActive} active");
///         Console.WriteLine($"Archetypes: {memStats.ArchetypeCount}");
///     }
/// }
/// </code>
/// </example>
public interface IStatisticsCapability
{
    /// <summary>
    /// Gets memory usage statistics for this world.
    /// </summary>
    /// <returns>A snapshot of current memory statistics.</returns>
    /// <remarks>
    /// Statistics are computed on-demand and represent a snapshot in time.
    /// </remarks>
    MemoryStats GetMemoryStats();

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
    IReadOnlyList<ArchetypeStatistics> GetArchetypeStatistics();
}
