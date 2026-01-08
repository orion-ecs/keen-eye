namespace KeenEyes.TestBridge.Logging;

/// <summary>
/// Controller for querying log entries from the running application.
/// </summary>
/// <remarks>
/// <para>
/// The log controller provides access to log entries captured during application
/// execution. It supports querying by level, category, message content, and time range.
/// </para>
/// <para>
/// Logs are stored in a bounded ring buffer, so older entries may be evicted
/// when the buffer reaches capacity.
/// </para>
/// </remarks>
public interface ILogController
{
    /// <summary>
    /// Gets the number of log entries currently stored.
    /// </summary>
    /// <returns>The log entry count.</returns>
    Task<int> GetCountAsync();

    /// <summary>
    /// Gets the most recent log entries.
    /// </summary>
    /// <param name="count">The maximum number of entries to return. Defaults to 100.</param>
    /// <returns>The most recent entries, newest first.</returns>
    Task<IReadOnlyList<LogEntrySnapshot>> GetRecentAsync(int count = 100);

    /// <summary>
    /// Queries log entries with filters.
    /// </summary>
    /// <param name="query">The query parameters.</param>
    /// <returns>A list of matching log entries.</returns>
    Task<IReadOnlyList<LogEntrySnapshot>> QueryAsync(LogQueryDto query);

    /// <summary>
    /// Gets log statistics.
    /// </summary>
    /// <returns>Statistics about the logged entries.</returns>
    Task<LogStatsSnapshot> GetStatsAsync();

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    /// <returns>A task that completes when logs are cleared.</returns>
    Task ClearAsync();

    /// <summary>
    /// Gets all entries at or above the specified level.
    /// </summary>
    /// <param name="level">The minimum log level.</param>
    /// <param name="maxResults">Maximum number of entries to return. Defaults to 1000.</param>
    /// <returns>Entries at or above the specified level, newest first.</returns>
    Task<IReadOnlyList<LogEntrySnapshot>> GetByLevelAsync(int level, int maxResults = 1000);

    /// <summary>
    /// Gets all entries matching the specified category pattern.
    /// </summary>
    /// <param name="categoryPattern">The category pattern (supports * and ? wildcards).</param>
    /// <param name="maxResults">Maximum number of entries to return. Defaults to 1000.</param>
    /// <returns>Entries matching the category pattern, newest first.</returns>
    Task<IReadOnlyList<LogEntrySnapshot>> GetByCategoryAsync(string categoryPattern, int maxResults = 1000);

    /// <summary>
    /// Searches log messages for the specified text.
    /// </summary>
    /// <param name="searchText">The text to search for (case-insensitive).</param>
    /// <param name="maxResults">Maximum number of entries to return. Defaults to 1000.</param>
    /// <returns>Entries containing the search text, newest first.</returns>
    Task<IReadOnlyList<LogEntrySnapshot>> SearchAsync(string searchText, int maxResults = 1000);
}
