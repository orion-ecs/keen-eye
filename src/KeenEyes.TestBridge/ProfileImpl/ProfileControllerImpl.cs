using System.Diagnostics;
using KeenEyes.Debugging;
using KeenEyes.Debugging.Timeline;
using KeenEyes.TestBridge.Profile;

namespace KeenEyes.TestBridge.ProfileImpl;

/// <summary>
/// In-process implementation of <see cref="IProfileController"/>.
/// </summary>
/// <remarks>
/// This implementation accesses the DebugPlugin extensions directly from the World
/// to provide profiling data. If a specific profiler is not installed, the corresponding
/// methods will return empty or default values.
/// </remarks>
internal sealed class ProfileControllerImpl(World world) : IProfileController
{
    #region Debug Mode

    /// <inheritdoc />
    public Task<bool> IsDebugModeEnabledAsync()
    {
        world.TryGetExtension<DebugController>(out var controller);
        return Task.FromResult(controller?.IsDebugMode ?? false);
    }

    /// <inheritdoc />
    public Task EnableDebugModeAsync()
    {
        world.TryGetExtension<DebugController>(out var controller);
        controller?.Enable();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisableDebugModeAsync()
    {
        world.TryGetExtension<DebugController>(out var controller);
        controller?.Disable();
        return Task.CompletedTask;
    }

    #endregion

    #region System Profiling

    /// <inheritdoc />
    public Task<bool> IsProfilingAvailableAsync()
    {
        return Task.FromResult(world.TryGetExtension<Profiler>(out _));
    }

    /// <inheritdoc />
    public Task<SystemProfileSnapshot?> GetSystemProfileAsync(string systemName)
    {
        if (!world.TryGetExtension<Profiler>(out var profiler))
        {
            return Task.FromResult<SystemProfileSnapshot?>(null);
        }

        var profile = profiler.GetSystemProfile(systemName);
        if (profile.CallCount == 0)
        {
            return Task.FromResult<SystemProfileSnapshot?>(null);
        }

        return Task.FromResult<SystemProfileSnapshot?>(ToSnapshot(profile));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemProfileSnapshot>> GetAllSystemProfilesAsync()
    {
        if (!world.TryGetExtension<Profiler>(out var profiler))
        {
            return Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);
        }

        var profiles = profiler.GetAllSystemProfiles();
        return Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>(
            profiles.Select(ToSnapshot).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<SystemProfileSnapshot>> GetSlowestSystemsAsync(int count = 10)
    {
        if (!world.TryGetExtension<Profiler>(out var profiler))
        {
            return Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>([]);
        }

        var profiles = profiler.GetAllSystemProfiles()
            .OrderByDescending(p => p.AverageTime)
            .Take(count)
            .Select(ToSnapshot)
            .ToList();

        return Task.FromResult<IReadOnlyList<SystemProfileSnapshot>>(profiles);
    }

    /// <inheritdoc />
    public Task ResetSystemProfilesAsync()
    {
        world.TryGetExtension<Profiler>(out var profiler);
        profiler?.Reset();
        return Task.CompletedTask;
    }

    private static SystemProfileSnapshot ToSnapshot(SystemProfile profile) => new()
    {
        Name = profile.Name,
        TotalTimeMs = profile.TotalTime.TotalMilliseconds,
        CallCount = profile.CallCount,
        AverageTimeMs = profile.AverageTime.TotalMilliseconds,
        MinTimeMs = profile.MinTime.TotalMilliseconds,
        MaxTimeMs = profile.MaxTime.TotalMilliseconds
    };

    #endregion

    #region Query Profiling

    /// <inheritdoc />
    public Task<bool> IsQueryProfilingAvailableAsync()
    {
        return Task.FromResult(world.TryGetExtension<QueryProfiler>(out _));
    }

    /// <inheritdoc />
    public Task<QueryProfileSnapshot?> GetQueryProfileAsync(string queryName)
    {
        if (!world.TryGetExtension<QueryProfiler>(out var profiler))
        {
            return Task.FromResult<QueryProfileSnapshot?>(null);
        }

        var profile = profiler.GetQueryProfile(queryName);
        if (profile.CallCount == 0)
        {
            return Task.FromResult<QueryProfileSnapshot?>(null);
        }

        return Task.FromResult<QueryProfileSnapshot?>(ToSnapshot(profile));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QueryProfileSnapshot>> GetAllQueryProfilesAsync()
    {
        if (!world.TryGetExtension<QueryProfiler>(out var profiler))
        {
            return Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);
        }

        var profiles = profiler.GetAllQueryProfiles();
        return Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>(
            profiles.Select(ToSnapshot).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<QueryProfileSnapshot>> GetSlowestQueriesAsync(int count = 10)
    {
        if (!world.TryGetExtension<QueryProfiler>(out var profiler))
        {
            return Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>([]);
        }

        var profiles = profiler.GetSlowestQueries(count)
            .Select(ToSnapshot)
            .ToList();

        return Task.FromResult<IReadOnlyList<QueryProfileSnapshot>>(profiles);
    }

    /// <inheritdoc />
    public Task<QueryCacheStatsSnapshot> GetQueryCacheStatsAsync()
    {
        if (!world.TryGetExtension<QueryProfiler>(out var profiler))
        {
            return Task.FromResult(new QueryCacheStatsSnapshot
            {
                CacheHits = 0,
                CacheMisses = 0,
                CachedQueryCount = 0,
                HitRate = 0
            });
        }

        var stats = profiler.GetCacheStatistics();
        return Task.FromResult(new QueryCacheStatsSnapshot
        {
            CacheHits = stats.CacheHits,
            CacheMisses = stats.CacheMisses,
            CachedQueryCount = stats.CachedQueryCount,
            HitRate = stats.HitRate
        });
    }

    /// <inheritdoc />
    public Task ResetQueryProfilesAsync()
    {
        world.TryGetExtension<QueryProfiler>(out var profiler);
        profiler?.Reset();
        return Task.CompletedTask;
    }

    private static QueryProfileSnapshot ToSnapshot(QueryProfile profile) => new()
    {
        Name = profile.Name,
        TotalTimeMs = profile.TotalTime.TotalMilliseconds,
        CallCount = profile.CallCount,
        TotalEntities = profile.TotalEntities,
        AverageTimeMs = profile.AverageTime.TotalMilliseconds,
        AverageEntities = profile.AverageEntities,
        MinTimeMs = profile.MinTime.TotalMilliseconds,
        MaxTimeMs = profile.MaxTime.TotalMilliseconds,
        MinEntities = profile.MinEntities,
        MaxEntities = profile.MaxEntities
    };

    #endregion

    #region GC/Allocation Profiling

    /// <inheritdoc />
    public Task<bool> IsGCTrackingAvailableAsync()
    {
        return Task.FromResult(world.TryGetExtension<GCTracker>(out _));
    }

    /// <inheritdoc />
    public Task<AllocationProfileSnapshot?> GetAllocationProfileAsync(string systemName)
    {
        if (!world.TryGetExtension<GCTracker>(out var tracker))
        {
            return Task.FromResult<AllocationProfileSnapshot?>(null);
        }

        var profile = tracker.GetSystemAllocations(systemName);
        if (profile.CallCount == 0)
        {
            return Task.FromResult<AllocationProfileSnapshot?>(null);
        }

        return Task.FromResult<AllocationProfileSnapshot?>(ToSnapshot(profile));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllAllocationProfilesAsync()
    {
        if (!world.TryGetExtension<GCTracker>(out var tracker))
        {
            return Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);
        }

        var profiles = tracker.GetAllAllocationProfiles();
        return Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>(
            profiles.Select(ToSnapshot).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AllocationProfileSnapshot>> GetAllocationHotspotsAsync(int count = 10)
    {
        if (!world.TryGetExtension<GCTracker>(out var tracker))
        {
            return Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>([]);
        }

        var profiles = tracker.GetAllAllocationProfiles()
            .OrderByDescending(p => p.TotalBytes)
            .Take(count)
            .Select(ToSnapshot)
            .ToList();

        return Task.FromResult<IReadOnlyList<AllocationProfileSnapshot>>(profiles);
    }

    /// <inheritdoc />
    public Task ResetAllocationProfilesAsync()
    {
        world.TryGetExtension<GCTracker>(out var tracker);
        tracker?.Reset();
        return Task.CompletedTask;
    }

    private static AllocationProfileSnapshot ToSnapshot(AllocationProfile profile) => new()
    {
        Name = profile.Name,
        TotalBytes = profile.TotalBytes,
        CallCount = profile.CallCount,
        AverageBytes = profile.AverageBytes,
        MinBytes = profile.MinBytes,
        MaxBytes = profile.MaxBytes
    };

    #endregion

    #region Memory Stats

    /// <inheritdoc />
    public Task<bool> IsMemoryTrackingAvailableAsync()
    {
        return Task.FromResult(world.TryGetExtension<MemoryTracker>(out _));
    }

    /// <inheritdoc />
    public Task<MemoryStatsSnapshot> GetMemoryStatsAsync()
    {
        if (!world.TryGetExtension<MemoryTracker>(out var tracker))
        {
            return Task.FromResult(new MemoryStatsSnapshot
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
            });
        }

        var stats = tracker.GetMemoryStats();
        return Task.FromResult(new MemoryStatsSnapshot
        {
            EntitiesAllocated = stats.EntitiesAllocated,
            EntitiesActive = stats.EntitiesActive,
            EntitiesRecycled = stats.EntitiesRecycled,
            EntityRecycleCount = stats.EntityRecycleCount,
            ArchetypeCount = stats.ArchetypeCount,
            ComponentTypeCount = stats.ComponentTypeCount,
            SystemCount = stats.SystemCount,
            CachedQueryCount = stats.CachedQueryCount,
            QueryCacheHits = stats.QueryCacheHits,
            QueryCacheMisses = stats.QueryCacheMisses,
            EstimatedComponentBytes = stats.EstimatedComponentBytes,
            RecycleEfficiency = stats.RecycleEfficiency,
            QueryCacheHitRate = stats.QueryCacheHitRate
        });
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ArchetypeStatsSnapshot>> GetArchetypeStatsAsync()
    {
        if (!world.TryGetExtension<MemoryTracker>(out var tracker))
        {
            return Task.FromResult<IReadOnlyList<ArchetypeStatsSnapshot>>([]);
        }

        var stats = tracker.GetArchetypeStats();
        return Task.FromResult<IReadOnlyList<ArchetypeStatsSnapshot>>(
            stats.Select(s => new ArchetypeStatsSnapshot
            {
                Id = s.Id,
                EntityCount = s.EntityCount,
                ChunkCount = s.ChunkCount,
                TotalCapacity = s.TotalCapacity,
                ComponentTypeNames = s.ComponentTypeNames,
                EstimatedMemoryBytes = s.EstimatedMemoryBytes,
                FragmentationPercentage = s.FragmentationPercentage,
                UtilizationPercentage = s.UtilizationPercentage
            }).ToList());
    }

    #endregion

    #region Timeline Recording

    /// <inheritdoc />
    public Task<bool> IsTimelineAvailableAsync()
    {
        return Task.FromResult(world.TryGetExtension<TimelineRecorder>(out _));
    }

    /// <inheritdoc />
    public Task<TimelineStatsSnapshot> GetTimelineStatsAsync()
    {
        if (!world.TryGetExtension<TimelineRecorder>(out var recorder))
        {
            return Task.FromResult(new TimelineStatsSnapshot
            {
                IsRecording = false,
                CurrentFrame = 0,
                EntryCount = 0
            });
        }

        return Task.FromResult(new TimelineStatsSnapshot
        {
            IsRecording = recorder.IsRecording,
            CurrentFrame = recorder.CurrentFrame,
            EntryCount = recorder.EntryCount
        });
    }

    /// <inheritdoc />
    public Task EnableTimelineRecordingAsync()
    {
        world.TryGetExtension<TimelineRecorder>(out var recorder);
        recorder?.EnableRecording();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisableTimelineRecordingAsync()
    {
        world.TryGetExtension<TimelineRecorder>(out var recorder);
        recorder?.DisableRecording();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetTimelineEntriesForFrameAsync(long frameNumber)
    {
        if (!world.TryGetExtension<TimelineRecorder>(out var recorder))
        {
            return Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);
        }

        var entries = recorder.GetEntriesForFrame(frameNumber);
        return Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>(
            entries.Select(ToSnapshot).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimelineEntrySnapshot>> GetRecentTimelineEntriesAsync(int count = 100)
    {
        if (!world.TryGetExtension<TimelineRecorder>(out var recorder))
        {
            return Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>([]);
        }

        var entries = recorder.GetRecentEntries(count);
        return Task.FromResult<IReadOnlyList<TimelineEntrySnapshot>>(
            entries.Select(ToSnapshot).ToList());
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<TimelineSystemStatsSnapshot>> GetTimelineSystemStatsAsync()
    {
        if (!world.TryGetExtension<TimelineRecorder>(out var recorder))
        {
            return Task.FromResult<IReadOnlyList<TimelineSystemStatsSnapshot>>([]);
        }

        var stats = recorder.GetSystemStats();
        return Task.FromResult<IReadOnlyList<TimelineSystemStatsSnapshot>>(
            stats.Values.Select(s => new TimelineSystemStatsSnapshot
            {
                SystemName = s.SystemName,
                CallCount = s.CallCount,
                TotalTimeMs = s.TotalTime.TotalMilliseconds,
                AverageTimeMs = s.AverageTime.TotalMilliseconds,
                MinTimeMs = s.MinTime.TotalMilliseconds,
                MaxTimeMs = s.MaxTime.TotalMilliseconds
            }).ToList());
    }

    /// <inheritdoc />
    public Task ResetTimelineAsync()
    {
        world.TryGetExtension<TimelineRecorder>(out var recorder);
        recorder?.Reset();
        return Task.CompletedTask;
    }

    private static TimelineEntrySnapshot ToSnapshot(TimelineEntry entry) => new()
    {
        FrameNumber = entry.FrameNumber,
        SystemName = entry.SystemName,
        Phase = entry.Phase.ToString(),
        StartOffsetMs = entry.StartTicks / (double)Stopwatch.Frequency * 1000.0,
        DurationMs = entry.Duration.TotalMilliseconds,
        DeltaTime = entry.DeltaTime
    };

    #endregion
}
