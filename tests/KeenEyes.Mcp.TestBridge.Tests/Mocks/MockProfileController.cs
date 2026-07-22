using KeenEyes.TestBridge.Profile;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of IProfileController for testing MCP tools.
/// </summary>
/// <remarks>
/// Reports profiling as unavailable and returns empty result sets, mirroring a
/// world with no DebugPlugin installed.
/// </remarks>
internal sealed class MockProfileController : IProfileController
{
    public Task<bool> IsDebugModeEnabledAsync() => Task.FromResult(false);

    public Task EnableDebugModeAsync() => Task.CompletedTask;

    public Task DisableDebugModeAsync() => Task.CompletedTask;

    public Task<bool> IsProfilingAvailableAsync() => Task.FromResult(false);

    public Task ResetSystemProfilesAsync() => Task.CompletedTask;

    public Task ResetQueryProfilesAsync() => Task.CompletedTask;

    public Task ResetAllocationProfilesAsync() => Task.CompletedTask;

    public Task EnableTimelineRecordingAsync() => Task.CompletedTask;

    public Task DisableTimelineRecordingAsync() => Task.CompletedTask;

    public Task ResetTimelineAsync() => Task.CompletedTask;

    public Task<SystemProfileSnapshot?> GetSystemProfileAsync(string systemName)
        => Task.FromResult<SystemProfileSnapshot?>(null);

    public Task<IReadOnlyList<SystemProfileSnapshot>> GetAllSystemProfilesAsync()
        => Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);

    public Task<IReadOnlyList<SystemProfileSnapshot>> GetSlowestSystemsAsync(int count = 10)
        => Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);

    public Task<bool> IsQueryProfilingAvailableAsync() => Task.FromResult(false);

    public Task<QueryProfileSnapshot?> GetQueryProfileAsync(string queryName)
        => Task.FromResult<QueryProfileSnapshot?>(null);

    public Task<IReadOnlyList<QueryProfileSnapshot>> GetAllQueryProfilesAsync()
        => Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);

    public Task<IReadOnlyList<QueryProfileSnapshot>> GetSlowestQueriesAsync(int count = 10)
        => Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);

    public Task<QueryCacheStatsSnapshot> GetQueryCacheStatsAsync()
        => Task.FromResult(new QueryCacheStatsSnapshot
        {
            CacheHits = 0,
            CacheMisses = 0,
            CachedQueryCount = 0,
            HitRate = 0.0
        });

    public Task<bool> IsGCTrackingAvailableAsync() => Task.FromResult(false);

    public Task<AllocationProfileSnapshot?> GetAllocationProfileAsync(string systemName)
        => Task.FromResult<AllocationProfileSnapshot?>(null);

    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllAllocationProfilesAsync()
        => Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);

    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllocationHotspotsAsync(int count = 10)
        => Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);

    public Task<bool> IsMemoryTrackingAvailableAsync() => Task.FromResult(false);

    public Task<MemoryStatsSnapshot> GetMemoryStatsAsync()
        => Task.FromResult(new MemoryStatsSnapshot
        {
            EntitiesAllocated = 0,
            EntitiesActive = 0,
            EntitiesRecycled = 0,
            EntityRecycleCount = 0,
            ArchetypeCount = 0,
            ComponentTypeCount = 0,
            SystemCount = 0,
            CachedQueryCount = 0,
            QueryCacheHits = 0,
            QueryCacheMisses = 0,
            EstimatedComponentBytes = 0,
            RecycleEfficiency = 0.0,
            QueryCacheHitRate = 0.0
        });

    public Task<IReadOnlyList<ArchetypeStatsSnapshot>> GetArchetypeStatsAsync()
        => Task.FromResult<IReadOnlyList<ArchetypeStatsSnapshot>>([]);

    public Task<bool> IsTimelineAvailableAsync() => Task.FromResult(false);

    public Task<TimelineStatsSnapshot> GetTimelineStatsAsync()
        => Task.FromResult(new TimelineStatsSnapshot
        {
            IsRecording = false,
            CurrentFrame = 0,
            EntryCount = 0
        });

    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetTimelineEntriesForFrameAsync(long frameNumber)
        => Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);

    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetRecentTimelineEntriesAsync(int count = 100)
        => Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);

    public Task<IReadOnlyList<TimelineSystemStatsSnapshot>> GetTimelineSystemStatsAsync()
        => Task.FromResult<IReadOnlyList<TimelineSystemStatsSnapshot>>([]);
}
