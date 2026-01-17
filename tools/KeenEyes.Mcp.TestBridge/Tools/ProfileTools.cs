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
[McpServerToolType]
public sealed class ProfileTools(BridgeConnectionManager connection)
{
    #region Debug Mode

    [McpServerTool(Name = "profile_debug_mode_status")]
    [Description("Check if debug mode is currently enabled.")]
    public async Task<DebugModeResult> GetDebugModeStatus()
    {
        var bridge = connection.GetBridge();
        var enabled = await bridge.Profile.IsDebugModeEnabledAsync();
        return new DebugModeResult { Enabled = enabled };
    }

    [McpServerTool(Name = "profile_debug_mode_enable")]
    [Description("Enable debug mode for enhanced diagnostics.")]
    public async Task<DebugModeResult> EnableDebugMode()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.EnableDebugModeAsync();
        return new DebugModeResult { Enabled = true };
    }

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

    [McpServerTool(Name = "profile_system_available")]
    [Description("Check if system profiling is available. Requires DebugPlugin to be installed.")]
    public async Task<AvailabilityResult> IsSystemProfilingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsProfilingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

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

    [McpServerTool(Name = "profile_query_available")]
    [Description("Check if query profiling is available. Requires DebugPlugin with query profiling enabled.")]
    public async Task<AvailabilityResult> IsQueryProfilingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsQueryProfilingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

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

    [McpServerTool(Name = "profile_gc_available")]
    [Description("Check if GC/allocation tracking is available. Requires DebugPlugin with GC tracking enabled.")]
    public async Task<AvailabilityResult> IsGCTrackingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsGCTrackingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

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

    [McpServerTool(Name = "memory_available")]
    [Description("Check if memory tracking is available.")]
    public async Task<AvailabilityResult> IsMemoryTrackingAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsMemoryTrackingAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

    [McpServerTool(Name = "memory_get_stats")]
    [Description("Get world memory statistics including entity counts, archetypes, and estimated memory usage.")]
    public async Task<MemoryResult> GetMemoryStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Profile.GetMemoryStatsAsync();
        return MemoryResult.FromSnapshot(stats);
    }

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

    [McpServerTool(Name = "timeline_available")]
    [Description("Check if timeline recording is available. Requires DebugPlugin with timeline enabled.")]
    public async Task<AvailabilityResult> IsTimelineAvailable()
    {
        var bridge = connection.GetBridge();
        var available = await bridge.Profile.IsTimelineAvailableAsync();
        return new AvailabilityResult { Available = available };
    }

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

    [McpServerTool(Name = "timeline_start")]
    [Description("Enable timeline recording.")]
    public async Task<OperationResult> StartTimelineRecording()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.EnableTimelineRecordingAsync();
        return new OperationResult { Success = true };
    }

    [McpServerTool(Name = "timeline_stop")]
    [Description("Disable timeline recording.")]
    public async Task<OperationResult> StopTimelineRecording()
    {
        var bridge = connection.GetBridge();
        await bridge.Profile.DisableTimelineRecordingAsync();
        return new OperationResult { Success = true };
    }

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
    public required bool Success { get; init; }
    public string? Name { get; init; }
    public double TotalTimeMs { get; init; }
    public int CallCount { get; init; }
    public double AverageTimeMs { get; init; }
    public double MinTimeMs { get; init; }
    public double MaxTimeMs { get; init; }
    public string? Error { get; init; }

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
    public required bool Success { get; init; }
    public IReadOnlyList<SystemProfileSnapshot> Profiles { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of a query profiling query.
/// </summary>
public sealed record QueryProfilingResult
{
    public required bool Success { get; init; }
    public string? Name { get; init; }
    public double TotalTimeMs { get; init; }
    public int CallCount { get; init; }
    public long TotalEntities { get; init; }
    public double AverageTimeMs { get; init; }
    public long AverageEntities { get; init; }
    public double MinTimeMs { get; init; }
    public double MaxTimeMs { get; init; }
    public int MinEntities { get; init; }
    public int MaxEntities { get; init; }
    public string? Error { get; init; }

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
    public required bool Success { get; init; }
    public IReadOnlyList<QueryProfileSnapshot> Profiles { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of a query cache stats query.
/// </summary>
public sealed record QueryCacheResult
{
    public required long CacheHits { get; init; }
    public required long CacheMisses { get; init; }
    public required int CachedQueryCount { get; init; }
    public required double HitRate { get; init; }
}

/// <summary>
/// Result of an allocation profiling query.
/// </summary>
public sealed record AllocationResult
{
    public required bool Success { get; init; }
    public string? Name { get; init; }
    public long TotalBytes { get; init; }
    public int CallCount { get; init; }
    public long AverageBytes { get; init; }
    public long MinBytes { get; init; }
    public long MaxBytes { get; init; }
    public string? Error { get; init; }

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
    public required bool Success { get; init; }
    public IReadOnlyList<AllocationProfileSnapshot> Profiles { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of a memory stats query.
/// </summary>
public sealed record MemoryResult
{
    public int EntitiesAllocated { get; init; }
    public int EntitiesActive { get; init; }
    public int EntitiesRecycled { get; init; }
    public long EntityRecycleCount { get; init; }
    public int ArchetypeCount { get; init; }
    public int ComponentTypeCount { get; init; }
    public int SystemCount { get; init; }
    public int CachedQueryCount { get; init; }
    public long QueryCacheHits { get; init; }
    public long QueryCacheMisses { get; init; }
    public long EstimatedComponentBytes { get; init; }
    public double RecycleEfficiency { get; init; }
    public double QueryCacheHitRate { get; init; }

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
    public required bool Success { get; init; }
    public IReadOnlyList<ArchetypeStatsSnapshot> Archetypes { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result of a timeline status query.
/// </summary>
public sealed record TimelineStatusResult
{
    public required bool IsRecording { get; init; }
    public required long CurrentFrame { get; init; }
    public required int EntryCount { get; init; }
}

/// <summary>
/// Result containing timeline entries.
/// </summary>
public sealed record TimelineEntriesResult
{
    public required bool Success { get; init; }
    public IReadOnlyList<TimelineEntrySnapshot> Entries { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result containing timeline system statistics.
/// </summary>
public sealed record TimelineSystemStatsResult
{
    public required bool Success { get; init; }
    public IReadOnlyList<TimelineSystemStatsSnapshot> Stats { get; init; } = [];
    public int Count { get; init; }
    public string? Error { get; init; }
}

#endregion
