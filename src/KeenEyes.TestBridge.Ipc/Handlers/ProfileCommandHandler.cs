using System.Text.Json;
using KeenEyes.TestBridge.Profile;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles profiling and debugging commands.
/// </summary>
internal sealed class ProfileCommandHandler(IProfileController profileController) : ICommandHandler
{
    public string Prefix => "profile";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            // Debug mode
            "isDebugModeEnabled" => await profileController.IsDebugModeEnabledAsync(),
            "enableDebugMode" => await HandleEnableDebugModeAsync(),
            "disableDebugMode" => await HandleDisableDebugModeAsync(),

            // System profiling
            "isProfilingAvailable" => await profileController.IsProfilingAvailableAsync(),
            "getSystemProfile" => await HandleGetSystemProfileAsync(args),
            "getAllSystemProfiles" => await profileController.GetAllSystemProfilesAsync(),
            "getSlowestSystems" => await HandleGetSlowestSystemsAsync(args),
            "resetSystemProfiles" => await HandleResetSystemProfilesAsync(),

            // Query profiling
            "isQueryProfilingAvailable" => await profileController.IsQueryProfilingAvailableAsync(),
            "getQueryProfile" => await HandleGetQueryProfileAsync(args),
            "getAllQueryProfiles" => await profileController.GetAllQueryProfilesAsync(),
            "getSlowestQueries" => await HandleGetSlowestQueriesAsync(args),
            "getQueryCacheStats" => await profileController.GetQueryCacheStatsAsync(),
            "resetQueryProfiles" => await HandleResetQueryProfilesAsync(),

            // GC/Allocation profiling
            "isGCTrackingAvailable" => await profileController.IsGCTrackingAvailableAsync(),
            "getAllocationProfile" => await HandleGetAllocationProfileAsync(args),
            "getAllAllocationProfiles" => await profileController.GetAllAllocationProfilesAsync(),
            "getAllocationHotspots" => await HandleGetAllocationHotspotsAsync(args),
            "resetAllocationProfiles" => await HandleResetAllocationProfilesAsync(),

            // Memory stats
            "isMemoryTrackingAvailable" => await profileController.IsMemoryTrackingAvailableAsync(),
            "getMemoryStats" => await profileController.GetMemoryStatsAsync(),
            "getArchetypeStats" => await profileController.GetArchetypeStatsAsync(),

            // Timeline
            "isTimelineAvailable" => await profileController.IsTimelineAvailableAsync(),
            "getTimelineStats" => await profileController.GetTimelineStatsAsync(),
            "enableTimelineRecording" => await HandleEnableTimelineRecordingAsync(),
            "disableTimelineRecording" => await HandleDisableTimelineRecordingAsync(),
            "getTimelineEntriesForFrame" => await HandleGetTimelineEntriesForFrameAsync(args),
            "getRecentTimelineEntries" => await HandleGetRecentTimelineEntriesAsync(args),
            "getTimelineSystemStats" => await profileController.GetTimelineSystemStatsAsync(),
            "resetTimeline" => await HandleResetTimelineAsync(),

            _ => throw new InvalidOperationException($"Unknown profile command: {command}")
        };
    }

    #region Debug Mode Handlers

    private async Task<bool> HandleEnableDebugModeAsync()
    {
        await profileController.EnableDebugModeAsync();
        return true;
    }

    private async Task<bool> HandleDisableDebugModeAsync()
    {
        await profileController.DisableDebugModeAsync();
        return true;
    }

    #endregion

    #region System Profiling Handlers

    private async Task<SystemProfileSnapshot?> HandleGetSystemProfileAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await profileController.GetSystemProfileAsync(name);
    }

    private async Task<IReadOnlyList<SystemProfileSnapshot>> HandleGetSlowestSystemsAsync(JsonElement? args)
    {
        var count = GetOptionalInt(args, "count", 10);
        return await profileController.GetSlowestSystemsAsync(count);
    }

    private async Task<bool> HandleResetSystemProfilesAsync()
    {
        await profileController.ResetSystemProfilesAsync();
        return true;
    }

    #endregion

    #region Query Profiling Handlers

    private async Task<QueryProfileSnapshot?> HandleGetQueryProfileAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await profileController.GetQueryProfileAsync(name);
    }

    private async Task<IReadOnlyList<QueryProfileSnapshot>> HandleGetSlowestQueriesAsync(JsonElement? args)
    {
        var count = GetOptionalInt(args, "count", 10);
        return await profileController.GetSlowestQueriesAsync(count);
    }

    private async Task<bool> HandleResetQueryProfilesAsync()
    {
        await profileController.ResetQueryProfilesAsync();
        return true;
    }

    #endregion

    #region Allocation Profiling Handlers

    private async Task<AllocationProfileSnapshot?> HandleGetAllocationProfileAsync(JsonElement? args)
    {
        var name = GetRequiredString(args, "name");
        return await profileController.GetAllocationProfileAsync(name);
    }

    private async Task<IReadOnlyList<AllocationProfileSnapshot>> HandleGetAllocationHotspotsAsync(JsonElement? args)
    {
        var count = GetOptionalInt(args, "count", 10);
        return await profileController.GetAllocationHotspotsAsync(count);
    }

    private async Task<bool> HandleResetAllocationProfilesAsync()
    {
        await profileController.ResetAllocationProfilesAsync();
        return true;
    }

    #endregion

    #region Timeline Handlers

    private async Task<bool> HandleEnableTimelineRecordingAsync()
    {
        await profileController.EnableTimelineRecordingAsync();
        return true;
    }

    private async Task<bool> HandleDisableTimelineRecordingAsync()
    {
        await profileController.DisableTimelineRecordingAsync();
        return true;
    }

    private async Task<IReadOnlyList<TimelineEntrySnapshot>> HandleGetTimelineEntriesForFrameAsync(JsonElement? args)
    {
        var frame = GetRequiredLong(args, "frameNumber");
        return await profileController.GetTimelineEntriesForFrameAsync(frame);
    }

    private async Task<IReadOnlyList<TimelineEntrySnapshot>> HandleGetRecentTimelineEntriesAsync(JsonElement? args)
    {
        var count = GetOptionalInt(args, "count", 100);
        return await profileController.GetRecentTimelineEntriesAsync(count);
    }

    private async Task<bool> HandleResetTimelineAsync()
    {
        await profileController.ResetTimelineAsync();
        return true;
    }

    #endregion

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static int GetOptionalInt(JsonElement? args, string name, int defaultValue)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return defaultValue;
        }

        return prop.GetInt32();
    }

    private static long GetRequiredLong(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt64();
    }

    #endregion
}
