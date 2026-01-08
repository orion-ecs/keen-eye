using KeenEyes.TestBridge.Logging;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Remote implementation of <see cref="ILogController"/> that communicates over IPC.
/// </summary>
internal sealed class RemoteLogController(TestBridgeClient client) : ILogController
{
    /// <inheritdoc />
    public async Task<int> GetCountAsync()
    {
        return await client.SendRequestAsync<int>("log.getCount", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntrySnapshot>> GetRecentAsync(int count = 100)
    {
        var result = await client.SendRequestAsync<LogEntrySnapshot[]>(
            "log.getRecent",
            new { count },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntrySnapshot>> QueryAsync(LogQueryDto query)
    {
        var result = await client.SendRequestAsync<LogEntrySnapshot[]>("log.query", query, CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<LogStatsSnapshot> GetStatsAsync()
    {
        var result = await client.SendRequestAsync<LogStatsSnapshot>("log.getStats", null, CancellationToken.None);
        return result ?? new LogStatsSnapshot
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
        };
    }

    /// <inheritdoc />
    public async Task ClearAsync()
    {
        await client.SendRequestAsync<object?>("log.clear", null, CancellationToken.None);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntrySnapshot>> GetByLevelAsync(int level, int maxResults = 1000)
    {
        var result = await client.SendRequestAsync<LogEntrySnapshot[]>(
            "log.getByLevel",
            new { level, maxResults },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntrySnapshot>> GetByCategoryAsync(string categoryPattern, int maxResults = 1000)
    {
        var result = await client.SendRequestAsync<LogEntrySnapshot[]>(
            "log.getByCategory",
            new { categoryPattern, maxResults },
            CancellationToken.None);
        return result ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogEntrySnapshot>> SearchAsync(string searchText, int maxResults = 1000)
    {
        var result = await client.SendRequestAsync<LogEntrySnapshot[]>(
            "log.search",
            new { searchText, maxResults },
            CancellationToken.None);
        return result ?? [];
    }
}
