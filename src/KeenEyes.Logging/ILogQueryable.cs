namespace KeenEyes.Logging;

/// <summary>
/// Defines the contract for log providers that support querying stored entries.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface in log providers that store entries in memory
/// and support retrieval and filtering. This enables integration with
/// debugging tools, editor consoles, and MCP resources.
/// </para>
/// <para>
/// Implementations should be thread-safe as queries may occur from different
/// threads while logging is active.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get all errors from the last hour
/// var query = new LogQuery
/// {
///     MinLevel = LogLevel.Error,
///     After = DateTime.Now.AddHours(-1)
/// };
/// var errors = queryableProvider.Query(query);
/// </code>
/// </example>
public interface ILogQueryable
{
    /// <summary>
    /// Gets the current number of stored log entries.
    /// </summary>
    int EntryCount { get; }

    /// <summary>
    /// Gets all stored log entries.
    /// </summary>
    /// <returns>A read-only list of all entries in storage order.</returns>
    /// <remarks>
    /// Returns a snapshot copy; modifications to the provider do not affect the returned list.
    /// </remarks>
    IReadOnlyList<LogEntry> GetEntries();

    /// <summary>
    /// Queries log entries with the specified filters.
    /// </summary>
    /// <param name="query">The query parameters to filter by.</param>
    /// <returns>A read-only list of matching entries.</returns>
    /// <remarks>
    /// Returns a snapshot copy; modifications to the provider do not affect the returned list.
    /// </remarks>
    IReadOnlyList<LogEntry> Query(LogQuery query);

    /// <summary>
    /// Gets statistics about the stored log entries.
    /// </summary>
    /// <returns>A snapshot of current statistics.</returns>
    LogStats GetStats();

    /// <summary>
    /// Removes all stored log entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Raised when a new log entry is added.
    /// </summary>
    /// <remarks>
    /// Subscribers should handle this event quickly to avoid blocking logging.
    /// </remarks>
    event Action<LogEntry>? LogAdded;

    /// <summary>
    /// Raised when all logs are cleared.
    /// </summary>
    event Action? LogsCleared;
}
