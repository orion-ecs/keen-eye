namespace KeenEyes.Debugging;

/// <summary>
/// Tracks memory usage and provides detailed statistics about ECS memory consumption.
/// </summary>
/// <remarks>
/// <para>
/// The MemoryTracker provides convenient access to the built-in <see cref="World.GetMemoryStats"/>
/// with additional formatting and reporting capabilities.
/// </para>
/// <para>
/// Memory estimates are approximations based on component sizes and entity counts. Actual
/// memory usage may vary due to internal data structures, padding, and CLR overhead.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var tracker = world.GetExtension&lt;MemoryTracker&gt;();
///
/// // Get overall memory stats
/// var stats = tracker.GetMemoryStats();
/// Console.WriteLine($"Total entities: {stats.EntitiesActive}");
/// Console.WriteLine($"Estimated bytes: {stats.EstimatedComponentBytes}");
///
/// // Print formatted report
/// Console.WriteLine(tracker.GetMemoryReport());
/// </code>
/// </example>
public sealed class MemoryTracker
{
    private readonly World world;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryTracker"/> class.
    /// </summary>
    /// <param name="world">The world to track memory for.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="world"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="world"/> is not a concrete <see cref="World"/> instance.</exception>
    public MemoryTracker(IWorld world)
    {
        if (world == null)
            throw new ArgumentNullException(nameof(world));

        if (world is not World concreteWorld)
            throw new ArgumentException("MemoryTracker requires a concrete World instance", nameof(world));

        this.world = concreteWorld;
    }

    /// <summary>
    /// Gets memory statistics for the world.
    /// </summary>
    /// <returns>Current memory statistics.</returns>
    /// <remarks>
    /// This delegates to <see cref="World.GetMemoryStats"/> and returns the same information.
    /// </remarks>
    public MemoryStats GetMemoryStats()
    {
        return world.GetMemoryStats();
    }

    /// <summary>
    /// Gets a formatted memory report as a string.
    /// </summary>
    /// <returns>A multi-line string containing formatted memory statistics.</returns>
    /// <remarks>
    /// This method is useful for logging or displaying memory information in a human-readable format.
    /// </remarks>
    public string GetMemoryReport()
    {
        var stats = GetMemoryStats();

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Memory Statistics ===");
        report.AppendLine($"Entities: {stats.EntitiesActive} active, {stats.EntitiesRecycled} recycled, {stats.EntitiesAllocated} total allocated");
        report.AppendLine($"Entity Recycling: {stats.EntityRecycleCount} reuses ({stats.RecycleEfficiency:F1}% efficiency)");
        report.AppendLine($"Archetypes: {stats.ArchetypeCount}");
        report.AppendLine($"Component Types: {stats.ComponentTypeCount}");
        report.AppendLine($"Systems: {stats.SystemCount}");
        report.AppendLine($"Estimated Component Memory: {FormatBytes(stats.EstimatedComponentBytes)}");
        report.AppendLine($"Query Cache: {stats.CachedQueryCount} queries, {stats.QueryCacheHitRate:F1}% hit rate");

        return report.ToString();
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} bytes";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024):F2} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F2} GB";
    }
}
