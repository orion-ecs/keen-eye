using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Profile;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for profiling and debugging.
/// </summary>
/// <remarks>
/// <para>
/// These tools expose the DebugPlugin profiling infrastructure via MCP, including
/// system profiling, query statistics, memory tracking, GC allocation tracking,
/// and timeline recording.
/// </para>
/// <para>
/// Note: Many of these tools require the DebugPlugin to be installed in the
/// target world. If not installed, availability checks will return false.
/// </para>
/// </remarks>
/// <param name="connection">The connection manager used to reach the active test bridge.</param>
[McpServerToolType]
public sealed class ProfileTools(BridgeConnectionManager connection)
{
    #region Debug Mode

    /// <summary>
    /// Checks whether debug mode is currently enabled.
    /// </summary>
    /// <returns>A <see cref="DebugModeResult"/> reporting the current debug mode state.</returns>
    [McpServerTool(Name = "profile_debug_mode_status")]
    [Description("Check if debug mode is currently enabled.")]
    public async Task<DebugModeResult> GetDebugModeStatus()
    {
        var bridge = connection.GetBridge();
        var enabled = await bridge.Profile.IsDebugModeEnabledAsync();
        return new DebugModeResult { Enabled = enabled };
    }

    /// <summary>
    /// Enables debug mode for enhanced diagnostics.
    /// </summary>
    /// <returns>A <see cref="DebugModeResult"/> confirming debug mode is enabled.</returns>
    [McpServerTool(Name = "profile_debug_mode_enable")]
    [Description("Enable debug mode for enhanced diagnostics.")]
    public async Task<DebugModeResult> EnableDebugMode()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.EnableDebugModeAsync();
        return new DebugModeResult { Enabled = true };
    }

    /// <summary>
    /// Disables debug mode.
    /// </summary>
    /// <returns>A <see cref="DebugModeResult"/> confirming debug mode is disabled.</returns>
    [McpServerTool(Name = "profile_debug_mode_disable")]
    [Description("Disable debug mode.")]
    public async Task<DebugModeResult> DisableDebugMode()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.DisableDebugModeAsync();
        return new DebugModeResult { Enabled = false };
    }

    #endregion

    #region System Profiling

    /// <summary>
    /// Checks whether system profiling is available.
    /// </summary>
    /// <returns>An <see cref="AvailabilityResult"/> indicating whether the DebugPlugin is installed and system profiling is available.</returns>
    [McpServerTool(Name = "profile_system_available")]
    [Description("Check if system profiling is available. Requires DebugPlugin to be installed.")]
    public async Task<AvailabilityResult> IsSystemProfilingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsProfilingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    /// <summary>
    /// Gets profiling data for a specific system.
    /// </summary>
    /// <param name="name">The system name (e.g., 'MovementSystem').</param>
    /// <returns>A <see cref="SystemProfilingResult"/> containing the system's profiling data, or an error if no profile was found.</returns>
    [McpServerTool(Name = "profile_system_get")]
    [Description("Get profiling data for a specific system.")]
    public async Task<SystemProfilingResult> GetSystemProfile(
        [Description("The system name (e.g., 'MovementSystem')")]
        string name)
    {
        var bridge = connection.GetBridge();
        var profile = await bridge.Profile.GetSystemProfileAsync(name);

        if (profile == null)
        {
            return new SystemProfilingResult
            {
                Success = false,
                Error = $"No profile found for system: {name}"
            };
        }

        return SystemProfilingResult.FromSnapshot(profile);
    }

    /// <summary>
    /// Gets profiling data for all systems.
    /// </summary>
    /// <returns>A <see cref="SystemProfilingListResult"/> containing profiling data for every profiled system.</returns>
    [McpServerTool(Name = "profile_system_list")]
    [Description("Get profiling data for all systems.")]
    public async Task<SystemProfilingListResult> GetAllSystemProfiles()
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetAllSystemProfilesAsync();
        return new SystemProfilingListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Gets the slowest systems by average execution time.
    /// </summary>
    /// <param name="count">The maximum number of systems to return (default: 10).</param>
    /// <returns>A <see cref="SystemProfilingListResult"/> containing the slowest systems, ordered by average execution time.</returns>
    [McpServerTool(Name = "profile_system_slowest")]
    [Description("Get the slowest systems by average execution time.")]
    public async Task<SystemProfilingListResult> GetSlowestSystems(
        [Description("Maximum number of systems to return (default: 10)")]
        int count = 10)
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetSlowestSystemsAsync(count);
        return new SystemProfilingListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Resets all system profiling data.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating the reset succeeded.</returns>
    [McpServerTool(Name = "profile_system_reset")]
    [Description("Reset all system profiling data.")]
    public async Task<OperationResult> ResetSystemProfiles()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.ResetSystemProfilesAsync();
        return new OperationResult { Success = true };
    }

    #endregion

    #region Query Profiling

    /// <summary>
    /// Checks whether query profiling is available.
    /// </summary>
    /// <returns>An <see cref="AvailabilityResult"/> indicating whether the DebugPlugin has query profiling enabled.</returns>
    [McpServerTool(Name = "profile_query_available")]
    [Description("Check if query profiling is available. Requires DebugPlugin with query profiling enabled.")]
    public async Task<AvailabilityResult> IsQueryProfilingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsQueryProfilingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    /// <summary>
    /// Gets profiling data for a specific named query.
    /// </summary>
    /// <param name="name">The query name, as passed to BeginQuery/EndQuery.</param>
    /// <returns>A <see cref="QueryProfilingResult"/> containing the query's profiling data, or an error if no profile was found.</returns>
    [McpServerTool(Name = "profile_query_get")]
    [Description("Get profiling data for a specific named query.")]
    public async Task<QueryProfilingResult> GetQueryProfile(
        [Description("The query name (as passed to BeginQuery/EndQuery)")]
        string name)
    {
        var bridge = connection.GetBridge();
        var profile = await bridge.Profile.GetQueryProfileAsync(name);

        if (profile == null)
        {
            return new QueryProfilingResult
            {
                Success = false,
                Error = $"No profile found for query: {name}"
            };
        }

        return QueryProfilingResult.FromSnapshot(profile);
    }

    /// <summary>
    /// Gets profiling data for all queries.
    /// </summary>
    /// <returns>A <see cref="QueryProfilingListResult"/> containing profiling data for every profiled query.</returns>
    [McpServerTool(Name = "profile_query_list")]
    [Description("Get profiling data for all queries.")]
    public async Task<QueryProfilingListResult> GetAllQueryProfiles()
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetAllQueryProfilesAsync();
        return new QueryProfilingListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Gets the slowest queries by average execution time.
    /// </summary>
    /// <param name="count">The maximum number of queries to return (default: 10).</param>
    /// <returns>A <see cref="QueryProfilingListResult"/> containing the slowest queries, ordered by average execution time.</returns>
    [McpServerTool(Name = "profile_query_slowest")]
    [Description("Get the slowest queries by average execution time.")]
    public async Task<QueryProfilingListResult> GetSlowestQueries(
        [Description("Maximum number of queries to return (default: 10)")]
        int count = 10)
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetSlowestQueriesAsync(count);
        return new QueryProfilingListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Gets query cache hit/miss statistics.
    /// </summary>
    /// <returns>A <see cref="QueryCacheResult"/> containing cache hit, miss, and hit-rate statistics.</returns>
    [McpServerTool(Name = "profile_query_cache_stats")]
    [Description("Get query cache hit/miss statistics.")]
    public async Task<QueryCacheResult> GetQueryCacheStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetQueryCacheStatsAsync();
        return new QueryCacheResult
        {
            CacheHits = stats.CacheHits,
            CacheMisses = stats.CacheMisses,
            CachedQueryCount = stats.CachedQueryCount,
            HitRate = stats.HitRate
        };
    }

    /// <summary>
    /// Resets all query profiling data.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating the reset succeeded.</returns>
    [McpServerTool(Name = "profile_query_reset")]
    [Description("Reset all query profiling data.")]
    public async Task<OperationResult> ResetQueryProfiles()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.ResetQueryProfilesAsync();
        return new OperationResult { Success = true };
    }

    #endregion

    #region GC/Allocation Profiling

    /// <summary>
    /// Checks whether GC/allocation tracking is available.
    /// </summary>
    /// <returns>An <see cref="AvailabilityResult"/> indicating whether the DebugPlugin has GC tracking enabled.</returns>
    [McpServerTool(Name = "profile_gc_available")]
    [Description("Check if GC/allocation tracking is available. Requires DebugPlugin with GC tracking enabled.")]
    public async Task<AvailabilityResult> IsGCTrackingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsGCTrackingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    /// <summary>
    /// Gets allocation data for a specific system.
    /// </summary>
    /// <param name="name">The system name (e.g., 'RenderSystem').</param>
    /// <returns>An <see cref="AllocationResult"/> containing the system's allocation data, or an error if no profile was found.</returns>
    [McpServerTool(Name = "profile_gc_get")]
    [Description("Get allocation data for a specific system.")]
    public async Task<AllocationResult> GetAllocationProfile(
        [Description("The system name (e.g., 'RenderSystem')")]
        string name)
    {
        var bridge = connection.GetBridge();
        var profile = await bridge.Profile.GetAllocationProfileAsync(name);

        if (profile == null)
        {
            return new AllocationResult
            {
                Success = false,
                Error = $"No allocation profile found for system: {name}"
            };
        }

        return AllocationResult.FromSnapshot(profile);
    }

    /// <summary>
    /// Gets allocation data for all systems.
    /// </summary>
    /// <returns>An <see cref="AllocationListResult"/> containing allocation data for every tracked system.</returns>
    [McpServerTool(Name = "profile_gc_list")]
    [Description("Get allocation data for all systems.")]
    public async Task<AllocationListResult> GetAllAllocationProfiles()
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetAllAllocationProfilesAsync();
        return new AllocationListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Gets the systems with the highest total allocations.
    /// </summary>
    /// <param name="count">The maximum number of systems to return (default: 10).</param>
    /// <returns>An <see cref="AllocationListResult"/> containing the systems with the highest total allocations.</returns>
    [McpServerTool(Name = "profile_gc_hotspots")]
    [Description("Get the systems with the highest total allocations.")]
    public async Task<AllocationListResult> GetAllocationHotspots(
        [Description("Maximum number of systems to return (default: 10)")]
        int count = 10)
    {
        var bridge = connection.GetBridge();
        var profiles = await bridge.Profile.GetAllocationHotspotsAsync(count);
        return new AllocationListResult
        {
            Success = true,
            Profiles = profiles,
            Count = profiles.Count
        };
    }

    /// <summary>
    /// Resets all allocation tracking data.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating the reset succeeded.</returns>
    [McpServerTool(Name = "profile_gc_reset")]
    [Description("Reset all allocation tracking data.")]
    public async Task<OperationResult> ResetAllocationProfiles()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.ResetAllocationProfilesAsync();
        return new OperationResult { Success = true };
    }

    #endregion

    #region Memory Stats

    /// <summary>
    /// Checks whether memory tracking is available.
    /// </summary>
    /// <returns>An <see cref="AvailabilityResult"/> indicating whether memory tracking is available.</returns>
    [McpServerTool(Name = "memory_available")]
    [Description("Check if memory tracking is available.")]
    public async Task<AvailabilityResult> IsMemoryTrackingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsMemoryTrackingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    /// <summary>
    /// Gets world memory statistics including entity counts, archetypes, and estimated memory usage.
    /// </summary>
    /// <returns>A <see cref="MemoryResult"/> containing the world's memory statistics.</returns>
    [McpServerTool(Name = "memory_get_stats")]
    [Description("Get world memory statistics including entity counts, archetypes, and estimated memory usage.")]
    public async Task<MemoryResult> GetMemoryStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetMemoryStatsAsync();
        return MemoryResult.FromSnapshot(stats);
    }

    /// <summary>
    /// Gets detailed statistics for all archetypes including entity distribution and memory usage.
    /// </summary>
    /// <returns>An <see cref="ArchetypeListResult"/> containing statistics for every archetype.</returns>
    [McpServerTool(Name = "memory_get_archetypes")]
    [Description("Get detailed statistics for all archetypes including entity distribution and memory usage.")]
    public async Task<ArchetypeListResult> GetArchetypeStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetArchetypeStatsAsync();
        return new ArchetypeListResult
        {
            Success = true,
            Archetypes = stats,
            Count = stats.Count
        };
    }

    #endregion

    #region Timeline Recording

    /// <summary>
    /// Checks whether timeline recording is available.
    /// </summary>
    /// <returns>An <see cref="AvailabilityResult"/> indicating whether the DebugPlugin has timeline recording enabled.</returns>
    [McpServerTool(Name = "timeline_available")]
    [Description("Check if timeline recording is available. Requires DebugPlugin with timeline enabled.")]
    public async Task<AvailabilityResult> IsTimelineAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsTimelineAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    /// <summary>
    /// Gets the current timeline recording status.
    /// </summary>
    /// <returns>A <see cref="TimelineStatusResult"/> describing whether recording is active and the current frame and entry count.</returns>
    [McpServerTool(Name = "timeline_status")]
    [Description("Get the current timeline recording status.")]
    public async Task<TimelineStatusResult> GetTimelineStatus()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetTimelineStatsAsync();
        return new TimelineStatusResult
        {
            IsRecording = stats.IsRecording,
            CurrentFrame = stats.CurrentFrame,
            EntryCount = stats.EntryCount
        };
    }

    /// <summary>
    /// Enables timeline recording.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating recording was enabled.</returns>
    [McpServerTool(Name = "timeline_start")]
    [Description("Enable timeline recording.")]
    public async Task<OperationResult> StartTimelineRecording()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.EnableTimelineRecordingAsync();
        return new OperationResult { Success = true };
    }

    /// <summary>
    /// Disables timeline recording.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating recording was disabled.</returns>
    [McpServerTool(Name = "timeline_stop")]
    [Description("Disable timeline recording.")]
    public async Task<OperationResult> StopTimelineRecording()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.DisableTimelineRecordingAsync();
        return new OperationResult { Success = true };
    }

    /// <summary>
    /// Gets timeline entries for a specific frame.
    /// </summary>
    /// <param name="frameNumber">The frame number to query.</param>
    /// <returns>A <see cref="TimelineEntriesResult"/> containing the timeline entries recorded for the frame.</returns>
    [McpServerTool(Name = "timeline_get_frame")]
    [Description("Get timeline entries for a specific frame.")]
    public async Task<TimelineEntriesResult> GetTimelineEntriesForFrame(
        [Description("The frame number to query")]
        long frameNumber)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Profile.GetTimelineEntriesForFrameAsync(frameNumber);
        return new TimelineEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    /// <summary>
    /// Gets the most recent timeline entries.
    /// </summary>
    /// <param name="count">The maximum number of entries to return (default: 100).</param>
    /// <returns>A <see cref="TimelineEntriesResult"/> containing the most recently recorded timeline entries.</returns>
    [McpServerTool(Name = "timeline_get_recent")]
    [Description("Get the most recent timeline entries.")]
    public async Task<TimelineEntriesResult> GetRecentTimelineEntries(
        [Description("Maximum number of entries to return (default: 100)")]
        int count = 100)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Profile.GetRecentTimelineEntriesAsync(count);
        return new TimelineEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    /// <summary>
    /// Gets aggregated timeline statistics per system.
    /// </summary>
    /// <returns>A <see cref="TimelineSystemStatsResult"/> containing per-system aggregated timeline statistics.</returns>
    [McpServerTool(Name = "timeline_system_stats")]
    [Description("Get aggregated timeline statistics per system.")]
    public async Task<TimelineSystemStatsResult> GetTimelineSystemStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetTimelineSystemStatsAsync();
        return new TimelineSystemStatsResult
        {
            Success = true,
            Stats = stats,
            Count = stats.Count
        };
    }

    /// <summary>
    /// Clears all timeline data and resets the frame counter.
    /// </summary>
    /// <returns>An <see cref="OperationResult"/> indicating the timeline was reset.</returns>
    [McpServerTool(Name = "timeline_reset")]
    [Description("Clear all timeline data and reset the frame counter.")]
    public async Task<OperationResult> ResetTimeline()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.ResetTimelineAsync();
        return new OperationResult { Success = true };
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a debug mode query.
/// </summary>
public sealed record DebugModeResult
{
    /// <summary>
    /// Gets whether debug mode is enabled.
    /// </summary>
    public required bool Enabled { get; init; }
}

/// <summary>
/// Result of an availability check.
/// </summary>
public sealed record AvailabilityResult
{
    /// <summary>
    /// Gets whether the feature is available.
    /// </summary>
    public required bool Available { get; init; }
}

/// <summary>
/// Generic operation result.
/// </summary>
public sealed record OperationResult
{
    /// <summary>
    /// Gets whether the operation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets an optional error message.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a system profiling query.
/// </summary>
public sealed record SystemProfilingResult
{
    /// <summary>
    /// Gets whether a profile was found for the requested system.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the profiled system.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the total execution time, in milliseconds, accumulated across all calls.
    /// </summary>
    public double TotalTimeMs { get; init; }

    /// <summary>
    /// Gets the number of times the system has executed.
    /// </summary>
    public int CallCount { get; init; }

    /// <summary>
    /// Gets the average execution time, in milliseconds, per call.
    /// </summary>
    public double AverageTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum recorded execution time, in milliseconds.
    /// </summary>
    public double MinTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum recorded execution time, in milliseconds.
    /// </summary>
    public double MaxTimeMs { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a <see cref="SystemProfilingResult"/> from a <see cref="SystemProfileSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">The system profile snapshot to convert.</param>
    /// <returns>A successful <see cref="SystemProfilingResult"/> populated from the snapshot.</returns>
    public static SystemProfilingResult FromSnapshot(SystemProfileSnapshot snapshot) => new()
    {
        Success = true,
        Name = snapshot.Name,
        TotalTimeMs = snapshot.TotalTimeMs,
        CallCount = snapshot.CallCount,
        AverageTimeMs = snapshot.AverageTimeMs,
        MinTimeMs = snapshot.MinTimeMs,
        MaxTimeMs = snapshot.MaxTimeMs
    };
}

/// <summary>
/// Result containing a list of system profiles.
/// </summary>
public sealed record SystemProfilingListResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the profiling snapshots for the returned systems.
    /// </summary>
    public IReadOnlyList<SystemProfileSnapshot> Profiles { get; init; } = [];

    /// <summary>
    /// Gets the number of profiles in <see cref="Profiles"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a query profiling query.
/// </summary>
public sealed record QueryProfilingResult
{
    /// <summary>
    /// Gets whether a profile was found for the requested query.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the profiled query.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the total execution time, in milliseconds, accumulated across all calls.
    /// </summary>
    public double TotalTimeMs { get; init; }

    /// <summary>
    /// Gets the number of times the query has executed.
    /// </summary>
    public int CallCount { get; init; }

    /// <summary>
    /// Gets the total number of entities matched across all calls.
    /// </summary>
    public long TotalEntities { get; init; }

    /// <summary>
    /// Gets the average execution time, in milliseconds, per call.
    /// </summary>
    public double AverageTimeMs { get; init; }

    /// <summary>
    /// Gets the average number of entities matched per call.
    /// </summary>
    public long AverageEntities { get; init; }

    /// <summary>
    /// Gets the minimum recorded execution time, in milliseconds.
    /// </summary>
    public double MinTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum recorded execution time, in milliseconds.
    /// </summary>
    public double MaxTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum number of entities matched in a single call.
    /// </summary>
    public int MinEntities { get; init; }

    /// <summary>
    /// Gets the maximum number of entities matched in a single call.
    /// </summary>
    public int MaxEntities { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates a <see cref="QueryProfilingResult"/> from a <see cref="QueryProfileSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">The query profile snapshot to convert.</param>
    /// <returns>A successful <see cref="QueryProfilingResult"/> populated from the snapshot.</returns>
    public static QueryProfilingResult FromSnapshot(QueryProfileSnapshot snapshot) => new()
    {
        Success = true,
        Name = snapshot.Name,
        TotalTimeMs = snapshot.TotalTimeMs,
        CallCount = snapshot.CallCount,
        TotalEntities = snapshot.TotalEntities,
        AverageTimeMs = snapshot.AverageTimeMs,
        AverageEntities = snapshot.AverageEntities,
        MinTimeMs = snapshot.MinTimeMs,
        MaxTimeMs = snapshot.MaxTimeMs,
        MinEntities = snapshot.MinEntities,
        MaxEntities = snapshot.MaxEntities
    };
}

/// <summary>
/// Result containing a list of query profiles.
/// </summary>
public sealed record QueryProfilingListResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the profiling snapshots for the returned queries.
    /// </summary>
    public IReadOnlyList<QueryProfileSnapshot> Profiles { get; init; } = [];

    /// <summary>
    /// Gets the number of profiles in <see cref="Profiles"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a query cache stats query.
/// </summary>
public sealed record QueryCacheResult
{
    /// <summary>
    /// Gets the number of query cache hits.
    /// </summary>
    public required long CacheHits { get; init; }

    /// <summary>
    /// Gets the number of query cache misses.
    /// </summary>
    public required long CacheMisses { get; init; }

    /// <summary>
    /// Gets the number of queries currently cached.
    /// </summary>
    public required int CachedQueryCount { get; init; }

    /// <summary>
    /// Gets the cache hit rate, as a fraction between 0 and 1.
    /// </summary>
    public required double HitRate { get; init; }
}

/// <summary>
/// Result of an allocation profiling query.
/// </summary>
public sealed record AllocationResult
{
    /// <summary>
    /// Gets whether an allocation profile was found for the requested system.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the name of the profiled system.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets the total number of bytes allocated across all calls.
    /// </summary>
    public long TotalBytes { get; init; }

    /// <summary>
    /// Gets the number of times the system has executed.
    /// </summary>
    public int CallCount { get; init; }

    /// <summary>
    /// Gets the average number of bytes allocated per call.
    /// </summary>
    public long AverageBytes { get; init; }

    /// <summary>
    /// Gets the minimum number of bytes allocated in a single call.
    /// </summary>
    public long MinBytes { get; init; }

    /// <summary>
    /// Gets the maximum number of bytes allocated in a single call.
    /// </summary>
    public long MaxBytes { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Creates an <see cref="AllocationResult"/> from an <see cref="AllocationProfileSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">The allocation profile snapshot to convert.</param>
    /// <returns>A successful <see cref="AllocationResult"/> populated from the snapshot.</returns>
    public static AllocationResult FromSnapshot(AllocationProfileSnapshot snapshot) => new()
    {
        Success = true,
        Name = snapshot.Name,
        TotalBytes = snapshot.TotalBytes,
        CallCount = snapshot.CallCount,
        AverageBytes = snapshot.AverageBytes,
        MinBytes = snapshot.MinBytes,
        MaxBytes = snapshot.MaxBytes
    };
}

/// <summary>
/// Result containing a list of allocation profiles.
/// </summary>
public sealed record AllocationListResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the allocation profiling snapshots for the returned systems.
    /// </summary>
    public IReadOnlyList<AllocationProfileSnapshot> Profiles { get; init; } = [];

    /// <summary>
    /// Gets the number of profiles in <see cref="Profiles"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a memory stats query.
/// </summary>
public sealed record MemoryResult
{
    /// <summary>
    /// Gets the total number of entity slots ever allocated.
    /// </summary>
    public int EntitiesAllocated { get; init; }

    /// <summary>
    /// Gets the number of entities currently active in the world.
    /// </summary>
    public int EntitiesActive { get; init; }

    /// <summary>
    /// Gets the number of entity slots currently available for recycling.
    /// </summary>
    public int EntitiesRecycled { get; init; }

    /// <summary>
    /// Gets the total number of times an entity slot has been recycled.
    /// </summary>
    public long EntityRecycleCount { get; init; }

    /// <summary>
    /// Gets the number of distinct archetypes in the world.
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
    /// Gets the number of queries currently cached.
    /// </summary>
    public int CachedQueryCount { get; init; }

    /// <summary>
    /// Gets the number of query cache hits.
    /// </summary>
    public long QueryCacheHits { get; init; }

    /// <summary>
    /// Gets the number of query cache misses.
    /// </summary>
    public long QueryCacheMisses { get; init; }

    /// <summary>
    /// Gets the estimated number of bytes used by component storage.
    /// </summary>
    public long EstimatedComponentBytes { get; init; }

    /// <summary>
    /// Gets the entity recycling efficiency, as a fraction between 0 and 1.
    /// </summary>
    public double RecycleEfficiency { get; init; }

    /// <summary>
    /// Gets the query cache hit rate, as a fraction between 0 and 1.
    /// </summary>
    public double QueryCacheHitRate { get; init; }

    /// <summary>
    /// Creates a <see cref="MemoryResult"/> from a <see cref="MemoryStatsSnapshot"/>.
    /// </summary>
    /// <param name="snapshot">The memory statistics snapshot to convert.</param>
    /// <returns>A <see cref="MemoryResult"/> populated from the snapshot.</returns>
    public static MemoryResult FromSnapshot(MemoryStatsSnapshot snapshot) => new()
    {
        EntitiesAllocated = snapshot.EntitiesAllocated,
        EntitiesActive = snapshot.EntitiesActive,
        EntitiesRecycled = snapshot.EntitiesRecycled,
        EntityRecycleCount = snapshot.EntityRecycleCount,
        ArchetypeCount = snapshot.ArchetypeCount,
        ComponentTypeCount = snapshot.ComponentTypeCount,
        SystemCount = snapshot.SystemCount,
        CachedQueryCount = snapshot.CachedQueryCount,
        QueryCacheHits = snapshot.QueryCacheHits,
        QueryCacheMisses = snapshot.QueryCacheMisses,
        EstimatedComponentBytes = snapshot.EstimatedComponentBytes,
        RecycleEfficiency = snapshot.RecycleEfficiency,
        QueryCacheHitRate = snapshot.QueryCacheHitRate
    };
}

/// <summary>
/// Result containing a list of archetype statistics.
/// </summary>
public sealed record ArchetypeListResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the statistics for each archetype in the world.
    /// </summary>
    public IReadOnlyList<ArchetypeStatsSnapshot> Archetypes { get; init; } = [];

    /// <summary>
    /// Gets the number of archetypes in <see cref="Archetypes"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result of a timeline status query.
/// </summary>
public sealed record TimelineStatusResult
{
    /// <summary>
    /// Gets whether timeline recording is currently active.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public required long CurrentFrame { get; init; }

    /// <summary>
    /// Gets the total number of entries currently recorded in the timeline.
    /// </summary>
    public required int EntryCount { get; init; }
}

/// <summary>
/// Result containing timeline entries.
/// </summary>
public sealed record TimelineEntriesResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the recorded timeline entries.
    /// </summary>
    public IReadOnlyList<TimelineEntrySnapshot> Entries { get; init; } = [];

    /// <summary>
    /// Gets the number of entries in <see cref="Entries"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Result containing timeline system statistics.
/// </summary>
public sealed record TimelineSystemStatsResult
{
    /// <summary>
    /// Gets whether the query succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the aggregated timeline statistics for each system.
    /// </summary>
    public IReadOnlyList<TimelineSystemStatsSnapshot> Stats { get; init; } = [];

    /// <summary>
    /// Gets the number of entries in <see cref="Stats"/>.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Gets an error message if the query failed.
    /// </summary>
    public string? Error { get; init; }
}

#endregion
