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
public sealed class TestLogProvider : ILogProvider
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

    /// <summary>
    /// Represents a captured log entry.
    /// </summary>
    /// <param name="Timestamp">The time the message was logged.</param>
    /// <param name="Level">The severity level of the message.</param>
    /// <param name="Category">The category or source of the message.</param>
    /// <param name="Message">The log message text.</param>
    /// <param name="Properties">The structured properties, if any.</param>
    public sealed record LogEntry(
        DateTime Timestamp,
        LogLevel Level,
        string Category,
        string Message,
        IReadOnlyDictionary<string, object?>? Properties);
}
