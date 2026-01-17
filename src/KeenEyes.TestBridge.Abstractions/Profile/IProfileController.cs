namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Controller for profiling and debugging capabilities.
/// </summary>
/// <remarks>
/// <para>
/// The profile controller provides access to performance profiling data including
/// system timing, query statistics, memory usage, GC allocations, and timeline recording.
/// </para>
/// <para>
/// All profiling features require the DebugPlugin to be installed in the world.
/// If a specific profiler is not available, methods will return empty or default values.
/// </para>
/// </remarks>
public interface IProfileController
{
    #region Debug Mode

    /// <summary>
    /// Gets whether debug mode is currently enabled.
    /// </summary>
    /// <returns>True if debug mode is enabled; otherwise, false.</returns>
    Task<bool> IsDebugModeEnabledAsync();

    /// <summary>
    /// Enables debug mode.
    /// </summary>
    /// <returns>A task that completes when debug mode is enabled.</returns>
    Task EnableDebugModeAsync();

    /// <summary>
    /// Disables debug mode.
    /// </summary>
    /// <returns>A task that completes when debug mode is disabled.</returns>
    Task DisableDebugModeAsync();

    #endregion

    #region System Profiling

    /// <summary>
    /// Gets whether system profiling is available.
    /// </summary>
    /// <returns>True if profiling is available; otherwise, false.</returns>
    Task<bool> IsProfilingAvailableAsync();

    /// <summary>
    /// Gets the execution profile for a specific system.
    /// </summary>
    /// <param name="systemName">The name of the system.</param>
    /// <returns>The system profile, or null if not found.</returns>
    Task<SystemProfileSnapshot?> GetSystemProfileAsync(string systemName);

    /// <summary>
    /// Gets all system execution profiles.
    /// </summary>
    /// <returns>A list of all system profiles.</returns>
    Task<IReadOnlyList<SystemProfileSnapshot>> GetAllSystemProfilesAsync();

    /// <summary>
    /// Gets the slowest systems by average execution time.
    /// </summary>
    /// <param name="count">Maximum number of systems to return.</param>
    /// <returns>The slowest systems in descending order by average time.</returns>
    Task<IReadOnlyList<SystemProfileSnapshot>> GetSlowestSystemsAsync(int count = 10);

    /// <summary>
    /// Resets all system profiling data.
    /// </summary>
    /// <returns>A task that completes when profiling data is reset.</returns>
    Task ResetSystemProfilesAsync();

    #endregion

    #region Query Profiling

    /// <summary>
    /// Gets whether query profiling is available.
    /// </summary>
    /// <returns>True if query profiling is available; otherwise, false.</returns>
    Task<bool> IsQueryProfilingAvailableAsync();

    /// <summary>
    /// Gets the execution profile for a specific query.
    /// </summary>
    /// <param name="queryName">The name of the query.</param>
    /// <returns>The query profile, or null if not found.</returns>
    Task<QueryProfileSnapshot?> GetQueryProfileAsync(string queryName);

    /// <summary>
    /// Gets all query execution profiles.
    /// </summary>
    /// <returns>A list of all query profiles.</returns>
    Task<IReadOnlyList<QueryProfileSnapshot>> GetAllQueryProfilesAsync();

    /// <summary>
    /// Gets the slowest queries by average execution time.
    /// </summary>
    /// <param name="count">Maximum number of queries to return.</param>
    /// <returns>The slowest queries in descending order by average time.</returns>
    Task<IReadOnlyList<QueryProfileSnapshot>> GetSlowestQueriesAsync(int count = 10);

    /// <summary>
    /// Gets the query cache statistics.
    /// </summary>
    /// <returns>Current query cache statistics.</returns>
    Task<QueryCacheStatsSnapshot> GetQueryCacheStatsAsync();

    /// <summary>
    /// Resets all query profiling data.
    /// </summary>
    /// <returns>A task that completes when profiling data is reset.</returns>
    Task ResetQueryProfilesAsync();

    #endregion

    #region GC/Allocation Profiling

    /// <summary>
    /// Gets whether GC/allocation tracking is available.
    /// </summary>
    /// <returns>True if GC tracking is available; otherwise, false.</returns>
    Task<bool> IsGCTrackingAvailableAsync();

    /// <summary>
    /// Gets the allocation profile for a specific system.
    /// </summary>
    /// <param name="systemName">The name of the system.</param>
    /// <returns>The allocation profile, or null if not found.</returns>
    Task<AllocationProfileSnapshot?> GetAllocationProfileAsync(string systemName);

    /// <summary>
    /// Gets all allocation profiles.
    /// </summary>
    /// <returns>A list of all allocation profiles.</returns>
    Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllAllocationProfilesAsync();

    /// <summary>
    /// Gets the systems with the highest total allocations.
    /// </summary>
    /// <param name="count">Maximum number of systems to return.</param>
    /// <returns>The allocation hotspots in descending order by total bytes.</returns>
    Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllocationHotspotsAsync(int count = 10);

    /// <summary>
    /// Resets all allocation profiling data.
    /// </summary>
    /// <returns>A task that completes when allocation data is reset.</returns>
    Task ResetAllocationProfilesAsync();

    #endregion

    #region Memory Stats

    /// <summary>
    /// Gets whether memory statistics are available.
    /// </summary>
    /// <returns>True if memory stats are available; otherwise, false.</returns>
    Task<bool> IsMemoryTrackingAvailableAsync();

    /// <summary>
    /// Gets the current memory statistics for the world.
    /// </summary>
    /// <returns>Current memory statistics.</returns>
    Task<MemoryStatsSnapshot> GetMemoryStatsAsync();

    /// <summary>
    /// Gets detailed statistics for all archetypes.
    /// </summary>
    /// <returns>A list of archetype statistics.</returns>
    Task<IReadOnlyList<ArchetypeStatsSnapshot>> GetArchetypeStatsAsync();

    #endregion

    #region Timeline Recording

    /// <summary>
    /// Gets whether timeline recording is available.
    /// </summary>
    /// <returns>True if timeline recording is available; otherwise, false.</returns>
    Task<bool> IsTimelineAvailableAsync();

    /// <summary>
    /// Gets the current timeline recording status and statistics.
    /// </summary>
    /// <returns>Current timeline status.</returns>
    Task<TimelineStatsSnapshot> GetTimelineStatsAsync();

    /// <summary>
    /// Enables timeline recording.
    /// </summary>
    /// <returns>A task that completes when recording is enabled.</returns>
    Task EnableTimelineRecordingAsync();

    /// <summary>
    /// Disables timeline recording.
    /// </summary>
    /// <returns>A task that completes when recording is disabled.</returns>
    Task DisableTimelineRecordingAsync();

    /// <summary>
    /// Gets timeline entries for a specific frame.
    /// </summary>
    /// <param name="frameNumber">The frame number to query.</param>
    /// <returns>All entries for the specified frame.</returns>
    Task<IReadOnlyList<TimelineEntrySnapshot>> GetTimelineEntriesForFrameAsync(long frameNumber);

    /// <summary>
    /// Gets the most recent timeline entries.
    /// </summary>
    /// <param name="count">Maximum number of entries to return.</param>
    /// <returns>The most recent entries.</returns>
    Task<IReadOnlyList<TimelineEntrySnapshot>> GetRecentTimelineEntriesAsync(int count = 100);

    /// <summary>
    /// Gets aggregated statistics for each system in the timeline.
    /// </summary>
    /// <returns>A list of per-system statistics from the timeline.</returns>
    Task<IReadOnlyList<TimelineSystemStatsSnapshot>> GetTimelineSystemStatsAsync();

    /// <summary>
    /// Clears all timeline data and resets the frame counter.
    /// </summary>
    /// <returns>A task that completes when timeline is reset.</returns>
    Task ResetTimelineAsync();

    #endregion
}
