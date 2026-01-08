using KeenEyes.TestBridge.Logging;

namespace KeenEyes.Mcp.TestBridge.Tests.Mocks;

/// <summary>
/// Mock implementation of ILogController for testing.
/// </summary>
internal sealed class MockLogController : ILogController
{
    private readonly List<LogEntrySnapshot> entries = [];

    /// <summary>
    /// Gets or sets the log entries that will be returned by query methods.
    /// </summary>
    public List<LogEntrySnapshot> Entries => entries;

    /// <summary>
    /// Gets or sets the stats to return from GetStatsAsync.
    /// </summary>
    public LogStatsSnapshot Stats { get; set; } = new()
    {
        TotalCount = 0,
        TraceCount = 0,
        DebugCount = 0,
        InfoCount = 0,
        WarningCount = 0,
        ErrorCount = 0,
        FatalCount = 0,
        OldestTimestamp = null,
        NewestTimestamp = null,
        Capacity = 10000
    };

    public Task<int> GetCountAsync()
    {
        return Task.FromResult(entries.Count);
    }

    public Task<IReadOnlyList<LogEntrySnapshot>> GetRecentAsync(int count = 100)
    {
        var result = entries
            .OrderByDescending(e => e.Timestamp)
            .Take(count)
            .ToList();
        return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>(result);
    }

    public Task<IReadOnlyList<LogEntrySnapshot>> QueryAsync(LogQueryDto query)
    {
        var result = entries.AsEnumerable();

        if (query.MinLevel.HasValue)
        {
            result = result.Where(e => e.Level >= query.MinLevel.Value);
        }

        if (query.MaxLevel.HasValue)
        {
            result = result.Where(e => e.Level <= query.MaxLevel.Value);
        }

        if (!string.IsNullOrEmpty(query.CategoryPattern))
        {
            result = result.Where(e => e.Category.Contains(query.CategoryPattern.Replace("*", "").Replace("?", "")));
        }

        if (!string.IsNullOrEmpty(query.MessageContains))
        {
            result = result.Where(e => e.Message.Contains(query.MessageContains, StringComparison.OrdinalIgnoreCase));
        }

        if (query.After.HasValue)
        {
            result = result.Where(e => e.Timestamp > query.After.Value);
        }

        if (query.Before.HasValue)
        {
            result = result.Where(e => e.Timestamp < query.Before.Value);
        }

        result = result.OrderByDescending(e => e.Timestamp);

        if (query.Skip > 0)
        {
            result = result.Skip(query.Skip);
        }

        if (query.MaxResults > 0)
        {
            result = result.Take(query.MaxResults);
        }

        return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>(result.ToList());
    }

    public Task<LogStatsSnapshot> GetStatsAsync()
    {
        return Task.FromResult(Stats);
    }

    public Task ClearAsync()
    {
        entries.Clear();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<LogEntrySnapshot>> GetByLevelAsync(int level, int maxResults = 1000)
    {
        var result = entries
            .Where(e => e.Level == level)
            .OrderByDescending(e => e.Timestamp)
            .Take(maxResults)
            .ToList();
        return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>(result);
    }

    public Task<IReadOnlyList<LogEntrySnapshot>> GetByCategoryAsync(string categoryPattern, int maxResults = 1000)
    {
        var result = entries
            .Where(e => e.Category.Contains(categoryPattern.Replace("*", "").Replace("?", "")))
            .OrderByDescending(e => e.Timestamp)
            .Take(maxResults)
            .ToList();
        return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>(result);
    }

    public Task<IReadOnlyList<LogEntrySnapshot>> SearchAsync(string searchText, int maxResults = 1000)
    {
        var result = entries
            .Where(e => e.Message.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.Timestamp)
            .Take(maxResults)
            .ToList();
        return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>(result);
    }

    /// <summary>
    /// Adds a test log entry.
    /// </summary>
    public void AddEntry(int level, string category, string message)
    {
        entries.Add(new LogEntrySnapshot
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            LevelName = GetLevelName(level),
            Category = category,
            Message = message,
            Properties = null
        });
    }

    private static string GetLevelName(int level) => level switch
    {
        0 => "Trace",
        1 => "Debug",
        2 => "Info",
        3 => "Warning",
        4 => "Error",
        5 => "Fatal",
        _ => "Unknown"
    };
}
