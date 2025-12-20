using KeenEyes.Logging;

namespace KeenEyes.Testing.Logging;

/// <summary>
/// A mock implementation of <see cref="ILogProvider"/> for testing log output
/// and verification.
/// </summary>
/// <remarks>
/// <para>
/// MockLogProvider captures all log messages for later assertion, enabling
/// verification of logging behavior in tests without writing to actual log destinations.
/// </para>
/// <para>
/// Use the <see cref="Entries"/> collection or convenience properties like
/// <see cref="Errors"/>, <see cref="Warnings"/>, and <see cref="Infos"/> to
/// verify logged messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logProvider = new MockLogProvider();
/// LogManager.AddProvider(logProvider);
///
/// // Code that logs
/// mySystem.Initialize();
///
/// logProvider.Entries.Should().NotBeEmpty();
/// logProvider.Errors.Should().BeEmpty();
/// logProvider.ShouldHaveLoggedContaining("Initialized");
/// </code>
/// </example>
public sealed class MockLogProvider : ILogProvider
{
    private readonly Lock lockObject = new();
    private bool disposed;

    /// <summary>
    /// Gets the list of all captured log entries.
    /// </summary>
    public List<LogEntry> Entries { get; } = [];

    /// <summary>
    /// Gets all trace-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Traces => Entries.Where(e => e.Level == LogLevel.Trace);

    /// <summary>
    /// Gets all debug-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Debugs => Entries.Where(e => e.Level == LogLevel.Debug);

    /// <summary>
    /// Gets all info-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Infos => Entries.Where(e => e.Level == LogLevel.Info);

    /// <summary>
    /// Gets all warning-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Warnings => Entries.Where(e => e.Level == LogLevel.Warning);

    /// <summary>
    /// Gets all error-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Errors => Entries.Where(e => e.Level == LogLevel.Error);

    /// <summary>
    /// Gets all fatal-level log entries.
    /// </summary>
    public IEnumerable<LogEntry> Fatals => Entries.Where(e => e.Level == LogLevel.Fatal);

    #region ILogProvider Implementation

    /// <inheritdoc />
    public string Name { get; } = "MockLogProvider";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <inheritdoc />
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        if (level < MinimumLevel)
        {
            return;
        }

        var entry = new LogEntry(
            DateTime.UtcNow,
            level,
            category,
            message,
            properties != null ? new Dictionary<string, object?>(properties) : null);

        lock (lockObject)
        {
            Entries.Add(entry);
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        // No-op for mock
    }

    #endregion

    #region Test Control

    /// <summary>
    /// Clears all captured log entries.
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            Entries.Clear();
        }
    }

    /// <summary>
    /// Gets the most recent log entry at or above the specified level.
    /// </summary>
    /// <param name="level">The minimum level to search for.</param>
    /// <returns>The most recent matching entry, or null if none found.</returns>
    public LogEntry? GetLastEntry(LogLevel level = LogLevel.Trace)
    {
        lock (lockObject)
        {
            return Entries.LastOrDefault(e => e.Level >= level);
        }
    }

    /// <summary>
    /// Gets all entries matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match entries against.</param>
    /// <returns>The matching log entries.</returns>
    public IEnumerable<LogEntry> GetEntriesWhere(Func<LogEntry, bool> predicate)
    {
        lock (lockObject)
        {
            return Entries.Where(predicate).ToList();
        }
    }

    /// <summary>
    /// Checks if any entry matches the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match entries against.</param>
    /// <returns>True if any entry matches.</returns>
    public bool HasEntry(Func<LogEntry, bool> predicate)
    {
        lock (lockObject)
        {
            return Entries.Any(predicate);
        }
    }

    /// <summary>
    /// Gets entries for a specific category.
    /// </summary>
    /// <param name="category">The category to filter by.</param>
    /// <returns>The matching log entries.</returns>
    public IEnumerable<LogEntry> GetEntriesForCategory(string category)
    {
        lock (lockObject)
        {
            return Entries.Where(e => e.Category == category).ToList();
        }
    }

    #endregion

    /// <inheritdoc />
    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            Clear();
        }
    }
}

/// <summary>
/// Represents a captured log entry.
/// </summary>
/// <param name="Timestamp">The time the entry was logged.</param>
/// <param name="Level">The log level.</param>
/// <param name="Category">The log category/source.</param>
/// <param name="Message">The log message.</param>
/// <param name="Properties">The structured properties, if any.</param>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    IReadOnlyDictionary<string, object?>? Properties);
