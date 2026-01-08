using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Logging;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles log query commands.
/// </summary>
internal sealed class LogCommandHandler(ILogController logController) : ICommandHandler
{
    public string Prefix => "log";

    public async ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken)
    {
        return command switch
        {
            "getCount" => await logController.GetCountAsync(),
            "getRecent" => await HandleGetRecentAsync(args),
            "query" => await HandleQueryAsync(args),
            "getStats" => await logController.GetStatsAsync(),
            "clear" => await HandleClearAsync(),
            "getByLevel" => await HandleGetByLevelAsync(args),
            "getByCategory" => await HandleGetByCategoryAsync(args),
            "search" => await HandleSearchAsync(args),
            _ => throw new InvalidOperationException($"Unknown log command: {command}")
        };
    }

    private async Task<object> HandleGetRecentAsync(JsonElement? args)
    {
        var count = GetOptionalInt(args, "count") ?? 100;
        return await logController.GetRecentAsync(count);
    }

    private async Task<object> HandleQueryAsync(JsonElement? args)
    {
        var query = args.HasValue
            ? args.Value.Deserialize(IpcJsonContext.Default.LogQueryDto) ?? new LogQueryDto()
            : new LogQueryDto();
        return await logController.QueryAsync(query);
    }

    private async Task<object?> HandleClearAsync()
    {
        await logController.ClearAsync();
        return null;
    }

    private async Task<object> HandleGetByLevelAsync(JsonElement? args)
    {
        var level = GetRequiredInt(args, "level");
        var maxResults = GetOptionalInt(args, "maxResults") ?? 1000;
        return await logController.GetByLevelAsync(level, maxResults);
    }

    private async Task<object> HandleGetByCategoryAsync(JsonElement? args)
    {
        var categoryPattern = GetRequiredString(args, "categoryPattern");
        var maxResults = GetOptionalInt(args, "maxResults") ?? 1000;
        return await logController.GetByCategoryAsync(categoryPattern, maxResults);
    }

    private async Task<object> HandleSearchAsync(JsonElement? args)
    {
        var searchText = GetRequiredString(args, "searchText");
        var maxResults = GetOptionalInt(args, "maxResults") ?? 1000;
        return await logController.SearchAsync(searchText, maxResults);
    }

    #region Typed Argument Helpers (AOT-compatible)

    private static string GetRequiredString(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetString() ?? throw new ArgumentException($"Invalid value for argument: {name}");
    }

    private static int GetRequiredInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            throw new ArgumentException($"Missing required argument: {name}");
        }

        return prop.GetInt32();
    }

    private static int? GetOptionalInt(JsonElement? args, string name)
    {
        if (!args.HasValue || !args.Value.TryGetProperty(name, out var prop))
        {
            return null;
        }

        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetInt32();
    }

    #endregion
}
