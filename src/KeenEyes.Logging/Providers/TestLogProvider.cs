namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that captures log messages for testing and verification.
/// </summary>
/// <remarks>
/// <para>
/// TestLogProvider stores all log messages in memory, allowing tests to verify
/// that specific messages were logged with the expected levels, categories,
/// and properties.
/// </para>
/// <para>
/// This provider is thread-safe and can be used in concurrent test scenarios.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var testProvider = new TestLogProvider();
/// logManager.AddProvider(testProvider);
///
/// // Perform operations that should log...
///
/// Assert.Single(testProvider.Messages);
/// Assert.Equal(LogLevel.Info, testProvider.Messages[0].Level);
/// Assert.Contains("expected text", testProvider.Messages[0].Message);
/// </code>
/// </example>
public sealed class TestLogProvider : ILogProvider, ILogQueryable
{
    private readonly List<LogEntry> messages = [];
    private readonly Lock messagesLock = new();

    /// <inheritdoc />
    public string Name => "Test";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets a copy of all captured log messages.
    /// </summary>
    public IReadOnlyList<LogEntry> Messages
    {
        get
        {
            lock (messagesLock)
            {
                return [.. messages];
            }
        }
    }

    /// <summary>
    /// Gets the number of captured messages.
    /// </summary>
    public int MessageCount
    {
        get
        {
            lock (messagesLock)
            {
                return messages.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        var entry = new LogEntry(
            DateTime.Now,
            level,
            category,
            message,
            properties != null ? new Dictionary<string, object?>(properties) : null);

        lock (messagesLock)
        {
            messages.Add(entry);
        }

        LogAdded?.Invoke(entry);
    }

    /// <summary>
    /// Clears all captured messages.
    /// </summary>
    public void Clear()
    {
        lock (messagesLock)
        {
            messages.Clear();
        }

        LogsCleared?.Invoke();
    }

    /// <summary>
    /// Checks if any message contains the specified text.
    /// </summary>
    /// <param name="text">The text to search for.</param>
    /// <returns>True if any message contains the text; otherwise, false.</returns>
    public bool ContainsMessage(string text)
    {
        lock (messagesLock)
        {
            return messages.Any(m => m.Message.Contains(text, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Checks if any message matches the specified level.
    /// </summary>
    /// <param name="level">The level to search for.</param>
    /// <returns>True if any message has the specified level; otherwise, false.</returns>
    public bool ContainsLevel(LogLevel level)
    {
        lock (messagesLock)
        {
            return messages.Any(m => m.Level == level);
        }
    }

    /// <summary>
    /// Checks if any message matches the specified category.
    /// </summary>
    /// <param name="category">The category to search for.</param>
    /// <returns>True if any message has the specified category; otherwise, false.</returns>
    public bool ContainsCategory(string category)
    {
        lock (messagesLock)
        {
            return messages.Any(m => m.Category == category);
        }
    }

    /// <summary>
    /// Gets all messages with the specified level.
    /// </summary>
    /// <param name="level">The level to filter by.</param>
    /// <returns>All messages at the specified level.</returns>
    public IReadOnlyList<LogEntry> GetByLevel(LogLevel level)
    {
        lock (messagesLock)
        {
            return messages.Where(m => m.Level == level).ToList();
        }
    }

    /// <summary>
    /// Gets all messages with the specified category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>All messages with the specified category.</returns>
    public IReadOnlyList<LogEntry> GetByCategory(string category)
    {
        lock (messagesLock)
        {
            return messages.Where(m => m.Category == category).ToList();
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        // Nothing to flush - messages are stored synchronously
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
    }

    #region ILogQueryable Implementation

    /// <inheritdoc />
    int ILogQueryable.EntryCount => MessageCount;

    /// <inheritdoc />
    IReadOnlyList<LogEntry> ILogQueryable.GetEntries() => Messages;

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> Query(LogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        lock (messagesLock)
        {
            IEnumerable<LogEntry> result = messages;

            // Apply level filters
            if (query.MinLevel.HasValue)
            {
                result = result.Where(e => e.Level >= query.MinLevel.Value);
            }

            if (query.MaxLevel.HasValue)
            {
                result = result.Where(e => e.Level <= query.MaxLevel.Value);
            }

            // Apply category filter (simple contains for TestLogProvider)
            if (!string.IsNullOrEmpty(query.CategoryPattern))
            {
                var pattern = query.CategoryPattern;
                if (pattern.Contains('*') || pattern.Contains('?'))
                {
                    // Simple wildcard - just check prefix for "Pattern.*"
                    var prefix = pattern.TrimEnd('*', '?', '.');
                    result = result.Where(e => e.Category.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
                }
                else
                {
                    result = result.Where(e => e.Category.Equals(pattern, StringComparison.OrdinalIgnoreCase));
                }
            }

            // Apply message filter
            if (!string.IsNullOrEmpty(query.MessageContains))
            {
                result = result.Where(e => e.Message.Contains(query.MessageContains, StringComparison.OrdinalIgnoreCase));
            }

            // Apply time filters
            if (query.After.HasValue)
            {
                result = result.Where(e => e.Timestamp >= query.After.Value);
            }

            if (query.Before.HasValue)
            {
                result = result.Where(e => e.Timestamp <= query.Before.Value);
            }

            // Apply ordering
            if (query.NewestFirst)
            {
                result = result.Reverse();
            }

            // Apply pagination
            if (query.Skip > 0)
            {
                result = result.Skip(query.Skip);
            }

            if (query.MaxResults > 0)
            {
                result = result.Take(query.MaxResults);
            }

            return [.. result];
        }
    }

    /// <inheritdoc />
    public LogStats GetStats()
    {
        lock (messagesLock)
        {
            return new LogStats
            {
                TotalCount = messages.Count,
                TraceCount = messages.Count(m => m.Level == LogLevel.Trace),
                DebugCount = messages.Count(m => m.Level == LogLevel.Debug),
                InfoCount = messages.Count(m => m.Level == LogLevel.Info),
                WarningCount = messages.Count(m => m.Level == LogLevel.Warning),
                ErrorCount = messages.Count(m => m.Level == LogLevel.Error),
                FatalCount = messages.Count(m => m.Level == LogLevel.Fatal),
                OldestTimestamp = messages.Count > 0 ? messages[0].Timestamp : null,
                NewestTimestamp = messages.Count > 0 ? messages[^1].Timestamp : null,
                Capacity = null // Unbounded
            };
        }
    }

    /// <inheritdoc />
    public event Action<LogEntry>? LogAdded;

    /// <inheritdoc />
    public event Action? LogsCleared;

    #endregion
}
