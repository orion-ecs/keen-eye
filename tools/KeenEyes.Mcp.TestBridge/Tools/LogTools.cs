using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge.Logging;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for log querying and management.
/// </summary>
[McpServerToolType]
public sealed class LogTools(BridgeConnectionManager connection)
{
    #region Statistics

    [McpServerTool(Name = "log_get_stats")]
    [Description("Get log statistics including counts per level, timestamps, and capacity.")]
    public async Task<LogStatsResult> GetStats()
    {
        var bridge = connection.GetBridge();
        var stats = await bridge.Logs.GetStatsAsync();
        return new LogStatsResult
        {
            Success = true,
            TotalCount = stats.TotalCount,
            TraceCount = stats.TraceCount,
            DebugCount = stats.DebugCount,
            InfoCount = stats.InfoCount,
            WarningCount = stats.WarningCount,
            ErrorCount = stats.ErrorCount,
            FatalCount = stats.FatalCount,
            OldestTimestamp = stats.OldestTimestamp,
            NewestTimestamp = stats.NewestTimestamp,
            Capacity = stats.Capacity
        };
    }

    [McpServerTool(Name = "log_get_count")]
    [Description("Get the total number of log entries currently stored.")]
    public async Task<LogCountResult> GetCount()
    {
        var bridge = connection.GetBridge();
        var count = await bridge.Logs.GetCountAsync();
        return new LogCountResult { Count = count };
    }

    #endregion

    #region Retrieval

    [McpServerTool(Name = "log_get_recent")]
    [Description("Get the most recent log entries. Returns newest first.")]
    public async Task<LogEntriesResult> GetRecent(
        [Description("Maximum number of entries to return (default: 100, max: 1000)")]
        int count = 100)
    {
        var bridge = connection.GetBridge();
        count = Math.Clamp(count, 1, 1000);
        var entries = await bridge.Logs.GetRecentAsync(count);
        return new LogEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    [McpServerTool(Name = "log_get_errors")]
    [Description("Get only error and fatal log entries. Useful for debugging issues.")]
    public async Task<LogEntriesResult> GetErrors(
        [Description("Maximum number of entries to return (default: 100)")]
        int maxResults = 100)
    {
        var bridge = connection.GetBridge();
        // Error level is 4, Fatal is 5
        var errors = await bridge.Logs.GetByLevelAsync(4, maxResults);
        var fatals = await bridge.Logs.GetByLevelAsync(5, maxResults);

        // Combine and sort by timestamp (newest first)
        var combined = errors.Concat(fatals)
            .OrderByDescending(e => e.Timestamp)
            .Take(maxResults)
            .ToList();

        return new LogEntriesResult
        {
            Success = true,
            Entries = combined,
            Count = combined.Count
        };
    }

    [McpServerTool(Name = "log_get_by_level")]
    [Description("Get log entries at a specific level. Level values: 0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Fatal.")]
    public async Task<LogEntriesResult> GetByLevel(
        [Description("Log level (0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Fatal)")]
        int level,
        [Description("Maximum number of entries to return (default: 100)")]
        int maxResults = 100)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.GetByLevelAsync(level, maxResults);
        return new LogEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    [McpServerTool(Name = "log_get_by_category")]
    [Description("Get log entries matching a category pattern. Supports wildcards (* for any, ? for single character).")]
    public async Task<LogEntriesResult> GetByCategory(
        [Description("Category pattern (e.g., 'Physics.*' or 'AI.Pathfinding')")]
        string categoryPattern,
        [Description("Maximum number of entries to return (default: 100)")]
        int maxResults = 100)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.GetByCategoryAsync(categoryPattern, maxResults);
        return new LogEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    #endregion

    #region Search

    [McpServerTool(Name = "log_search")]
    [Description("Search log messages for specific text. Case-insensitive substring search.")]
    public async Task<LogEntriesResult> Search(
        [Description("Text to search for in log messages")]
        string searchText,
        [Description("Maximum number of entries to return (default: 100)")]
        int maxResults = 100)
    {
        var bridge = connection.GetBridge();
        var entries = await bridge.Logs.SearchAsync(searchText, maxResults);
        return new LogEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    [McpServerTool(Name = "log_query")]
    [Description("Query logs with multiple filters. All filters are optional and combined with AND logic.")]
    public async Task<LogEntriesResult> Query(
        [Description("Minimum log level (0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Fatal)")]
        int? minLevel = null,
        [Description("Maximum log level (0=Trace, 1=Debug, 2=Info, 3=Warning, 4=Error, 5=Fatal)")]
        int? maxLevel = null,
        [Description("Category pattern (supports wildcards: *, ?)")]
        string? categoryPattern = null,
        [Description("Text to search for in log messages")]
        string? messageContains = null,
        [Description("Only include entries after this ISO 8601 timestamp")]
        DateTime? after = null,
        [Description("Only include entries before this ISO 8601 timestamp")]
        DateTime? before = null,
        [Description("Maximum number of entries to return (default: 100)")]
        int maxResults = 100,
        [Description("Number of entries to skip (for pagination)")]
        int skip = 0)
    {
        var bridge = connection.GetBridge();

        var query = new LogQueryDto
        {
            MinLevel = minLevel,
            MaxLevel = maxLevel,
            CategoryPattern = categoryPattern,
            MessageContains = messageContains,
            After = after,
            Before = before,
            MaxResults = maxResults,
            Skip = skip
        };

        var entries = await bridge.Logs.QueryAsync(query);
        return new LogEntriesResult
        {
            Success = true,
            Entries = entries,
            Count = entries.Count
        };
    }

    #endregion

    #region Management

    [McpServerTool(Name = "log_clear")]
    [Description("Clear all log entries. This action cannot be undone.")]
    public async Task<LogClearResult> Clear()
    {
        var bridge = connection.GetBridge();
        var countBefore = await bridge.Logs.GetCountAsync();
        await bridge.Logs.ClearAsync();
        return new LogClearResult
        {
            Success = true,
            ClearedCount = countBefore,
            Message = $"Cleared {countBefore} log entries"
        };
    }

    #endregion
}

#region Result Records

/// <summary>
/// Result of a log statistics query.
/// </summary>
public sealed record LogStatsResult
{
    public required bool Success { get; init; }
    public int TotalCount { get; init; }
    public int TraceCount { get; init; }
    public int DebugCount { get; init; }
    public int InfoCount { get; init; }
    public int WarningCount { get; init; }
    public int ErrorCount { get; init; }
    public int FatalCount { get; init; }
    public DateTime? OldestTimestamp { get; init; }
    public DateTime? NewestTimestamp { get; init; }
    public int? Capacity { get; init; }
}

/// <summary>
/// Result of a log count query.
/// </summary>
public sealed record LogCountResult
{
    public required int Count { get; init; }
}

/// <summary>
/// Result containing log entries.
/// </summary>
public sealed record LogEntriesResult
{
    public required bool Success { get; init; }
    public IReadOnlyList<LogEntrySnapshot> Entries { get; init; } = [];
    public int Count { get; init; }
}

/// <summary>
/// Result of a log clear operation.
/// </summary>
public sealed record LogClearResult
{
    public required bool Success { get; init; }
    public int ClearedCount { get; init; }
    public required string Message { get; init; }
}

#endregion
