using KeenEyes.Logging;
using KeenEyes.TestBridge.Logging;

namespace KeenEyes.TestBridge.LoggingImpl;

/// <summary>
/// In-process implementation of <see cref="ILogController"/>.
/// </summary>
/// <param name="logQueryable">The queryable log provider, or null if logging is not configured.</param>
internal sealed class LogControllerImpl(ILogQueryable? logQueryable) : ILogController
{

    /// <inheritdoc />
    public Task<int> GetCountAsync()
    {
        return Task.FromResult(logQueryable?.EntryCount ?? 0);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LogEntrySnapshot>> GetRecentAsync(int count = 100)
    {
        if (logQueryable == null)
        {
            return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>([]);
        }

        var query = new LogQuery
        {
            MaxResults = count,
            NewestFirst = true
        };

        return Task.FromResult(ConvertEntries(logQueryable.Query(query)));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LogEntrySnapshot>> QueryAsync(LogQueryDto query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (logQueryable == null)
        {
            return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>([]);
        }

        var logQuery = new LogQuery
        {
            MinLevel = query.MinLevel.HasValue ? (LogLevel)query.MinLevel.Value : null,
            MaxLevel = query.MaxLevel.HasValue ? (LogLevel)query.MaxLevel.Value : null,
            CategoryPattern = query.CategoryPattern,
            MessageContains = query.MessageContains,
            After = query.After,
            Before = query.Before,
            MaxResults = query.MaxResults,
            Skip = query.Skip,
            NewestFirst = query.NewestFirst
        };

        return Task.FromResult(ConvertEntries(logQueryable.Query(logQuery)));
    }

    /// <inheritdoc />
    public Task<LogStatsSnapshot> GetStatsAsync()
    {
        if (logQueryable == null)
        {
            return Task.FromResult(new LogStatsSnapshot
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
                Capacity = null
            });
        }

        var stats = logQueryable.GetStats();
        return Task.FromResult(new LogStatsSnapshot
        {
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
        });
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        logQueryable?.Clear();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LogEntrySnapshot>> GetByLevelAsync(int level, int maxResults = 1000)
    {
        if (logQueryable == null)
        {
            return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>([]);
        }

        var query = new LogQuery
        {
            MinLevel = (LogLevel)level,
            MaxResults = maxResults,
            NewestFirst = true
        };

        return Task.FromResult(ConvertEntries(logQueryable.Query(query)));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LogEntrySnapshot>> GetByCategoryAsync(string categoryPattern, int maxResults = 1000)
    {
        if (logQueryable == null)
        {
            return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>([]);
        }

        var query = new LogQuery
        {
            CategoryPattern = categoryPattern,
            MaxResults = maxResults,
            NewestFirst = true
        };

        return Task.FromResult(ConvertEntries(logQueryable.Query(query)));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<LogEntrySnapshot>> SearchAsync(string searchText, int maxResults = 1000)
    {
        if (logQueryable == null)
        {
            return Task.FromResult<IReadOnlyList<LogEntrySnapshot>>([]);
        }

        var query = new LogQuery
        {
            MessageContains = searchText,
            MaxResults = maxResults,
            NewestFirst = true
        };

        return Task.FromResult(ConvertEntries(logQueryable.Query(query)));
    }

    private static IReadOnlyList<LogEntrySnapshot> ConvertEntries(IReadOnlyList<LogEntry> entries)
    {
        var snapshots = new LogEntrySnapshot[entries.Count];
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            snapshots[i] = new LogEntrySnapshot
            {
                Timestamp = entry.Timestamp,
                Level = (int)entry.Level,
                LevelName = entry.Level.ToString(),
                Category = entry.Category,
                Message = entry.Message,
                Properties = entry.Properties
            };
        }

        return snapshots;
    }
}
