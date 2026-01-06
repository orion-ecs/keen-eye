using System.Diagnostics;
using KeenEyes.Capabilities;

namespace KeenEyes.Debugging;

/// <summary>
/// Tracks query execution times and cache statistics for performance profiling.
/// </summary>
/// <remarks>
/// <para>
/// The QueryProfiler combines automatic cache hit/miss statistics from the ECS
/// with manual profiling methods for detailed query timing. Since queries execute
/// inline within systems without built-in hooks, users must manually instrument
/// queries they want to profile using <see cref="BeginQuery"/> and <see cref="EndQuery"/>.
/// </para>
/// <para>
/// Cache statistics (hit rate, cached query count) are collected automatically
/// from the underlying <see cref="IStatisticsCapability"/> and require no manual
/// instrumentation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var queryProfiler = world.GetExtension&lt;QueryProfiler&gt;();
///
/// // Manual query timing
/// queryProfiler.BeginQuery("MovementQuery");
/// foreach (var entity in world.Query&lt;Position, Velocity&gt;())
/// {
///     // Process entities
/// }
/// queryProfiler.EndQuery("MovementQuery", entityCount: 1000);
///
/// // Get cache statistics
/// var cacheStats = queryProfiler.GetCacheStatistics();
/// Console.WriteLine($"Cache hit rate: {cacheStats.HitRate:F1}%");
///
/// // Get query timing profile
/// var profile = queryProfiler.GetQueryProfile("MovementQuery");
/// Console.WriteLine($"Avg query time: {profile.AverageTime.TotalMicroseconds:F2}µs");
///
/// // Get formatted report
/// Console.WriteLine(queryProfiler.GetQueryReport());
/// </code>
/// </example>
public sealed class QueryProfiler
{
    private readonly IStatisticsCapability statisticsCapability;
    private readonly Dictionary<string, QueryProfile> profiles = [];
    private readonly Dictionary<string, QuerySample> activeSamples = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryProfiler"/> class.
    /// </summary>
    /// <param name="statisticsCapability">The statistics capability for cache statistics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="statisticsCapability"/> is null.</exception>
    public QueryProfiler(IStatisticsCapability statisticsCapability)
    {
        ArgumentNullException.ThrowIfNull(statisticsCapability);
        this.statisticsCapability = statisticsCapability;
    }

    /// <summary>
    /// Begins profiling a query with the specified name.
    /// </summary>
    /// <param name="queryName">A descriptive name for the query being profiled.</param>
    /// <remarks>
    /// This method records the current timestamp. Call <see cref="EndQuery"/> with the same
    /// name to complete the measurement. If a query with the same name is already being timed,
    /// it will be overwritten.
    /// </remarks>
    public void BeginQuery(string queryName)
    {
        activeSamples[queryName] = new QuerySample
        {
            StartTimestamp = Stopwatch.GetTimestamp()
        };
    }

    /// <summary>
    /// Ends profiling a query and records the elapsed time and entity count.
    /// </summary>
    /// <param name="queryName">The name of the query (must match the name passed to <see cref="BeginQuery"/>).</param>
    /// <param name="entityCount">Optional count of entities processed by the query.</param>
    /// <remarks>
    /// If no active sample with the specified name exists, this method does nothing.
    /// The elapsed time and entity count are added to the profile statistics for the named query.
    /// </remarks>
    public void EndQuery(string queryName, int entityCount = 0)
    {
        if (!activeSamples.TryGetValue(queryName, out var sample))
        {
            return;
        }

        var elapsed = Stopwatch.GetElapsedTime(sample.StartTimestamp);
        activeSamples.Remove(queryName);

        if (!profiles.TryGetValue(queryName, out var profile))
        {
            profile = new QueryProfile
            {
                Name = queryName,
                TotalTime = TimeSpan.Zero,
                CallCount = 0,
                TotalEntities = 0,
                AverageTime = TimeSpan.Zero,
                AverageEntities = 0,
                MinTime = TimeSpan.MaxValue,
                MaxTime = TimeSpan.Zero,
                MinEntities = int.MaxValue,
                MaxEntities = 0
            };
            profiles[queryName] = profile;
        }

        var updatedProfile = profile with
        {
            TotalTime = profile.TotalTime + elapsed,
            CallCount = profile.CallCount + 1,
            TotalEntities = profile.TotalEntities + entityCount,
            MinTime = elapsed < profile.MinTime ? elapsed : profile.MinTime,
            MaxTime = elapsed > profile.MaxTime ? elapsed : profile.MaxTime,
            MinEntities = entityCount < profile.MinEntities ? entityCount : profile.MinEntities,
            MaxEntities = entityCount > profile.MaxEntities ? entityCount : profile.MaxEntities
        };

        updatedProfile = updatedProfile with
        {
            AverageTime = TimeSpan.FromTicks(updatedProfile.TotalTime.Ticks / updatedProfile.CallCount),
            AverageEntities = updatedProfile.TotalEntities / updatedProfile.CallCount
        };

        profiles[queryName] = updatedProfile;
    }

    /// <summary>
    /// Gets the profile for a specific query.
    /// </summary>
    /// <param name="queryName">The name of the query.</param>
    /// <returns>The query profile if it exists; otherwise, an empty profile.</returns>
    public QueryProfile GetQueryProfile(string queryName)
    {
        if (profiles.TryGetValue(queryName, out var profile))
        {
            return profile;
        }

        return new QueryProfile
        {
            Name = queryName,
            TotalTime = TimeSpan.Zero,
            CallCount = 0,
            TotalEntities = 0,
            AverageTime = TimeSpan.Zero,
            AverageEntities = 0,
            MinTime = TimeSpan.Zero,
            MaxTime = TimeSpan.Zero,
            MinEntities = 0,
            MaxEntities = 0
        };
    }

    /// <summary>
    /// Gets all query profiles.
    /// </summary>
    /// <returns>A read-only list of all query profiles.</returns>
    public IReadOnlyList<QueryProfile> GetAllQueryProfiles()
    {
        return profiles.Values.ToList();
    }

    /// <summary>
    /// Gets the slowest queries ordered by average execution time.
    /// </summary>
    /// <param name="count">Maximum number of queries to return.</param>
    /// <returns>The slowest queries in descending order by average time.</returns>
    public IReadOnlyList<QueryProfile> GetSlowestQueries(int count = 10)
    {
        return profiles.Values
            .OrderByDescending(p => p.AverageTime)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Gets the cache statistics for queries.
    /// </summary>
    /// <returns>Current query cache statistics.</returns>
    public QueryCacheStatistics GetCacheStatistics()
    {
        var stats = statisticsCapability.GetMemoryStats();
        return new QueryCacheStatistics
        {
            CacheHits = stats.QueryCacheHits,
            CacheMisses = stats.QueryCacheMisses,
            CachedQueryCount = stats.CachedQueryCount,
            HitRate = stats.QueryCacheHitRate
        };
    }

    /// <summary>
    /// Gets a formatted report of query profiling data.
    /// </summary>
    /// <returns>A multi-line string containing formatted query statistics.</returns>
    public string GetQueryReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine("=== Query Profiling Report ===");

        // Cache statistics
        var cacheStats = GetCacheStatistics();
        report.AppendLine();
        report.AppendLine("Query Cache Statistics:");
        report.AppendLine($"  Cached Queries: {cacheStats.CachedQueryCount}");
        report.AppendLine($"  Cache Hits: {cacheStats.CacheHits}");
        report.AppendLine($"  Cache Misses: {cacheStats.CacheMisses}");
        report.AppendLine($"  Hit Rate: {cacheStats.HitRate:F1}%");

        // Query timing profiles
        if (profiles.Count > 0)
        {
            report.AppendLine();
            report.AppendLine("Query Timing Profiles:");
            report.AppendLine($"  {"Query Name",-30} {"Calls",8} {"Avg Time",12} {"Avg Entities",12} {"Total Time",12}");
            report.AppendLine($"  {new string('-', 30)} {new string('-', 8)} {new string('-', 12)} {new string('-', 12)} {new string('-', 12)}");

            foreach (var profile in GetSlowestQueries(20))
            {
                var avgTime = FormatTime(profile.AverageTime);
                var totalTime = FormatTime(profile.TotalTime);
                report.AppendLine($"  {profile.Name,-30} {profile.CallCount,8} {avgTime,12} {profile.AverageEntities,12} {totalTime,12}");
            }
        }
        else
        {
            report.AppendLine();
            report.AppendLine("No query timing profiles recorded.");
            report.AppendLine("Use BeginQuery/EndQuery to profile individual queries.");
        }

        return report.ToString();
    }

    /// <summary>
    /// Resets all profiling data.
    /// </summary>
    /// <remarks>
    /// This clears all accumulated profile statistics. Note that cache statistics
    /// from <see cref="IStatisticsCapability"/> are not reset by this method.
    /// </remarks>
    public void Reset()
    {
        profiles.Clear();
        activeSamples.Clear();
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalMilliseconds < 1)
        {
            return $"{time.TotalMicroseconds:F1}µs";
        }

        if (time.TotalSeconds < 1)
        {
            return $"{time.TotalMilliseconds:F2}ms";
        }

        return $"{time.TotalSeconds:F2}s";
    }

    private readonly record struct QuerySample
    {
        public required long StartTimestamp { get; init; }
    }
}

/// <summary>
/// Represents profiling statistics for a query.
/// </summary>
/// <remarks>
/// This record contains timing and entity count metrics collected during query execution.
/// </remarks>
public readonly record struct QueryProfile
{
    /// <summary>
    /// Gets the name of the query.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total time spent executing this query across all calls.
    /// </summary>
    public required TimeSpan TotalTime { get; init; }

    /// <summary>
    /// Gets the number of times this query has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the total number of entities processed across all query executions.
    /// </summary>
    public required long TotalEntities { get; init; }

    /// <summary>
    /// Gets the average time per query execution.
    /// </summary>
    public required TimeSpan AverageTime { get; init; }

    /// <summary>
    /// Gets the average number of entities per query execution.
    /// </summary>
    public required long AverageEntities { get; init; }

    /// <summary>
    /// Gets the minimum execution time observed.
    /// </summary>
    public required TimeSpan MinTime { get; init; }

    /// <summary>
    /// Gets the maximum execution time observed.
    /// </summary>
    public required TimeSpan MaxTime { get; init; }

    /// <summary>
    /// Gets the minimum entity count observed.
    /// </summary>
    public required int MinEntities { get; init; }

    /// <summary>
    /// Gets the maximum entity count observed.
    /// </summary>
    public required int MaxEntities { get; init; }
}

/// <summary>
/// Statistics about query cache performance.
/// </summary>
/// <remarks>
/// These statistics are collected automatically by the ECS query system
/// and reflect the effectiveness of query result caching.
/// </remarks>
public readonly record struct QueryCacheStatistics
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
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public required double HitRate { get; init; }
}
