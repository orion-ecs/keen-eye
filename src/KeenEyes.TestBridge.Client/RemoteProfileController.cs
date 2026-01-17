using KeenEyes.TestBridge.Profile;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="IProfileController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteProfileController(TestBridgeClient client) : IProfileController
{
    #region Debug Mode

    /// <inheritdoc />
    public async Task<bool> IsDebugModeEnabledAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isDebugModeEnabled", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task EnableDebugModeAsync()
    {
        await client.SendRequestAsync<bool>("profile.enableDebugMode", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task DisableDebugModeAsync()
    {
        await client.SendRequestAsync<bool>("profile.disableDebugMode", null, CancellationToken.None);
    }

    #endregion

    #region System Profiling

    /// <inheritdoc />
    public async Task<bool> IsProfilingAvailableAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isProfilingAvailable", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<SystemProfileSnapshot?> GetSystemProfileAsync(string systemName)
    {
        return await client.SendRequestAsync<SystemProfileSnapshot?>(
            "profile.getSystemProfile",
            new { name = systemName },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemProfileSnapshot>> GetAllSystemProfilesAsync()
    {
        var result = await client.SendRequestAsync<SystemProfileSnapshot[]>(
            "profile.getAllSystemProfiles",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SystemProfileSnapshot>> GetSlowestSystemsAsync(int count = 10)
    {
        var result = await client.SendRequestAsync<SystemProfileSnapshot[]>(
            "profile.getSlowestSystems",
            new { count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task ResetSystemProfilesAsync()
    {
        await client.SendRequestAsync<bool>("profile.resetSystemProfiles", null, CancellationToken.None);
    }

    #endregion

    #region Query Profiling

    /// <inheritdoc />
    public async Task<bool> IsQueryProfilingAvailableAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isQueryProfilingAvailable", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<QueryProfileSnapshot?> GetQueryProfileAsync(string queryName)
    {
        return await client.SendRequestAsync<QueryProfileSnapshot?>(
            "profile.getQueryProfile",
            new { name = queryName },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueryProfileSnapshot>> GetAllQueryProfilesAsync()
    {
        var result = await client.SendRequestAsync<QueryProfileSnapshot[]>(
            "profile.getAllQueryProfiles",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<QueryProfileSnapshot>> GetSlowestQueriesAsync(int count = 10)
    {
        var result = await client.SendRequestAsync<QueryProfileSnapshot[]>(
            "profile.getSlowestQueries",
            new { count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<QueryCacheStatsSnapshot> GetQueryCacheStatsAsync()
    {
        var result = await client.SendRequestAsync<QueryCacheStatsSnapshot>(
            "profile.getQueryCacheStats",
            null,
            CancellationToken.None);
        return result ?? new QueryCacheStatsSnapshot
        {
            CacheHits = 0,
            CacheMisses = 0,
            CachedQueryCount = 0,
            HitRate = 0
        };
    }

    /// <inheritdoc />
    public async Task ResetQueryProfilesAsync()
    {
        await client.SendRequestAsync<bool>("profile.resetQueryProfiles", null, CancellationToken.None);
    }

    #endregion

    #region GC/Allocation Profiling

    /// <inheritdoc />
    public async Task<bool> IsGCTrackingAvailableAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isGCTrackingAvailable", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<AllocationProfileSnapshot?> GetAllocationProfileAsync(string systemName)
    {
        return await client.SendRequestAsync<AllocationProfileSnapshot?>(
            "profile.getAllocationProfile",
            new { name = systemName },
            CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllAllocationProfilesAsync()
    {
        var result = await client.SendRequestAsync<AllocationProfileSnapshot[]>(
            "profile.getAllAllocationProfiles",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllocationHotspotsAsync(int count = 10)
    {
        var result = await client.SendRequestAsync<AllocationProfileSnapshot[]>(
            "profile.getAllocationHotspots",
            new { count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task ResetAllocationProfilesAsync()
    {
        await client.SendRequestAsync<bool>("profile.resetAllocationProfiles", null, CancellationToken.None);
    }

    #endregion

    #region Memory Stats

    /// <inheritdoc />
    public async Task<bool> IsMemoryTrackingAvailableAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isMemoryTrackingAvailable", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<MemoryStatsSnapshot> GetMemoryStatsAsync()
    {
        var result = await client.SendRequestAsync<MemoryStatsSnapshot>(
            "profile.getMemoryStats",
            null,
            CancellationToken.None);
        return result ?? new MemoryStatsSnapshot
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
            RecycleEfficiency = 0,
            QueryCacheHitRate = 0
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArchetypeStatsSnapshot>> GetArchetypeStatsAsync()
    {
        var result = await client.SendRequestAsync<ArchetypeStatsSnapshot[]>(
            "profile.getArchetypeStats",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    #endregion

    #region Timeline Recording

    /// <inheritdoc />
    public async Task<bool> IsTimelineAvailableAsync()
    {
        return await client.SendRequestAsync<bool>("profile.isTimelineAvailable", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<TimelineStatsSnapshot> GetTimelineStatsAsync()
    {
        var result = await client.SendRequestAsync<TimelineStatsSnapshot>(
            "profile.getTimelineStats",
            null,
            CancellationToken.None);
        return result ?? new TimelineStatsSnapshot
        {
            IsRecording = false,
            CurrentFrame = 0,
            EntryCount = 0
        };
    }

    /// <inheritdoc />
    public async Task EnableTimelineRecordingAsync()
    {
        await client.SendRequestAsync<bool>("profile.enableTimelineRecording", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task DisableTimelineRecordingAsync()
    {
        await client.SendRequestAsync<bool>("profile.disableTimelineRecording", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimelineEntrySnapshot>> GetTimelineEntriesForFrameAsync(long frameNumber)
    {
        var result = await client.SendRequestAsync<TimelineEntrySnapshot[]>(
            "profile.getTimelineEntriesForFrame",
            new { frameNumber },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimelineEntrySnapshot>> GetRecentTimelineEntriesAsync(int count = 100)
    {
        var result = await client.SendRequestAsync<TimelineEntrySnapshot[]>(
            "profile.getRecentTimelineEntries",
            new { count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimelineSystemStatsSnapshot>> GetTimelineSystemStatsAsync()
    {
        var result = await client.SendRequestAsync<TimelineSystemStatsSnapshot[]>(
            "profile.getTimelineSystemStats",
            null,
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task ResetTimelineAsync()
    {
        await client.SendRequestAsync<bool>("profile.resetTimeline", null, CancellationToken.None);
    }

    #endregion
}
