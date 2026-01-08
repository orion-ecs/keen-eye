using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Logging;

/// <summary>
/// A log provider that captures log entries for display in the editor console.
/// </summary>
/// <remarks>
/// This provider maintains a ring buffer of log entries to prevent unbounded memory usage.
/// Entries are stored thread-safely and can be accessed for UI display.
/// </remarks>
public sealed class EditorLogProvider : ILogProvider, ILogQueryable
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;
    private int _entryCount;
    private int _traceCount;
    private int _debugCount;
    private int _infoCount;
    private int _warningCount;
    private int _errorCount;
    private int _fatalCount;
    private DateTime? _oldestTimestamp;
    private DateTime? _newestTimestamp;

    /// <inheritdoc/>
    public string Name => "EditorConsole";

    /// <inheritdoc/>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Raised when a new log entry is added.
    /// </summary>
    public event Action<LogEntry>? LogAdded;

    /// <summary>
    /// Raised when logs are cleared.
    /// </summary>
    public event Action? LogsCleared;

    /// <summary>
    /// Gets the total number of log entries.
    /// </summary>
    public int EntryCount => _entryCount;

    /// <summary>
    /// Gets the count of info-level (and below) log entries.
    /// </summary>
    public int InfoCount => _traceCount + _debugCount + _infoCount;

    /// <summary>
    /// Gets the count of warning-level log entries.
    /// </summary>
    public int WarningCount => _warningCount;

    /// <summary>
    /// Gets the count of error-level (and above) log entries.
    /// </summary>
    public int ErrorCount => _errorCount + _fatalCount;

    /// <summary>
    /// Creates a new editor log provider with the specified maximum entry count.
    /// </summary>
    /// <param name="maxEntries">Maximum number of entries to retain. Defaults to 10000.</param>
    public EditorLogProvider(int maxEntries = 10000)
    {
        _maxEntries = maxEntries;
    }

    /// <inheritdoc/>
    public void Log(LogLevel level, string category, string message, IReadOnlyDictionary<string, object?>? properties)
    {
        var entry = new LogEntry(DateTime.Now, level, category, message, properties);

        _entries.Enqueue(entry);
        Interlocked.Increment(ref _entryCount);

        // Update timestamps
        if (_oldestTimestamp == null)
        {
            _oldestTimestamp = entry.Timestamp;
        }
        _newestTimestamp = entry.Timestamp;

        // Update level counts
        IncrementLevelCount(level);

        // Trim old entries if over limit
        while (_entryCount > _maxEntries && _entries.TryDequeue(out var removed))
        {
            Interlocked.Decrement(ref _entryCount);
            DecrementLevelCount(removed.Level);

            // Update oldest timestamp
            if (_entries.TryPeek(out var oldest))
            {
                _oldestTimestamp = oldest.Timestamp;
            }
        }

        LogAdded?.Invoke(entry);
    }

    private void IncrementLevelCount(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
                Interlocked.Increment(ref _traceCount);
                break;
            case LogLevel.Debug:
                Interlocked.Increment(ref _debugCount);
                break;
            case LogLevel.Info:
                Interlocked.Increment(ref _infoCount);
                break;
            case LogLevel.Warning:
                Interlocked.Increment(ref _warningCount);
                break;
            case LogLevel.Error:
                Interlocked.Increment(ref _errorCount);
                break;
            case LogLevel.Fatal:
                Interlocked.Increment(ref _fatalCount);
                break;
        }
    }

    private void DecrementLevelCount(LogLevel level)
    {
        switch (level)
        {
            case LogLevel.Trace:
                Interlocked.Decrement(ref _traceCount);
                break;
            case LogLevel.Debug:
                Interlocked.Decrement(ref _debugCount);
                break;
            case LogLevel.Info:
                Interlocked.Decrement(ref _infoCount);
                break;
            case LogLevel.Warning:
                Interlocked.Decrement(ref _warningCount);
                break;
            case LogLevel.Error:
                Interlocked.Decrement(ref _errorCount);
                break;
            case LogLevel.Fatal:
                Interlocked.Decrement(ref _fatalCount);
                break;
        }
    }

    /// <summary>
    /// Gets all log entries as a snapshot.
    /// </summary>
    /// <returns>A read-only list of all current log entries.</returns>
    public IReadOnlyList<LogEntry> GetEntries()
    {
        return _entries.ToArray();
    }

    /// <summary>
    /// Gets log entries filtered by severity level.
    /// </summary>
    /// <param name="showInfo">Include info and below.</param>
    /// <param name="showWarnings">Include warnings.</param>
    /// <param name="showErrors">Include errors and above.</param>
    /// <returns>Filtered log entries.</returns>
    public IEnumerable<LogEntry> GetFilteredEntries(bool showInfo, bool showWarnings, bool showErrors)
    {
        foreach (var entry in _entries)
        {
            var shouldInclude =
                (entry.Level >= LogLevel.Error && showErrors) ||
                (entry.Level == LogLevel.Warning && showWarnings) ||
                (entry.Level < LogLevel.Warning && showInfo);

            if (shouldInclude)
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Gets log entries filtered by severity and search text.
    /// </summary>
    /// <param name="showInfo">Include info and below.</param>
    /// <param name="showWarnings">Include warnings.</param>
    /// <param name="showErrors">Include errors and above.</param>
    /// <param name="searchText">Text to search for (case-insensitive).</param>
    /// <returns>Filtered log entries.</returns>
    public IEnumerable<LogEntry> GetFilteredEntries(bool showInfo, bool showWarnings, bool showErrors, string? searchText)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(searchText);

        foreach (var entry in GetFilteredEntries(showInfo, showWarnings, showErrors))
        {
            if (!hasSearch ||
                entry.Message.Contains(searchText!, StringComparison.OrdinalIgnoreCase) ||
                entry.Category.Contains(searchText!, StringComparison.OrdinalIgnoreCase))
            {
                yield return entry;
            }
        }
    }

    /// <summary>
    /// Clears all log entries.
    /// </summary>
    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
            // Drain the queue
        }

        _entryCount = 0;
        _traceCount = 0;
        _debugCount = 0;
        _infoCount = 0;
        _warningCount = 0;
        _errorCount = 0;
        _fatalCount = 0;
        _oldestTimestamp = null;
        _newestTimestamp = null;

        LogsCleared?.Invoke();
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> Query(LogQuery query)
    {
        var result = _entries.AsEnumerable();

        // Filter by level range
        if (query.MinLevel.HasValue)
        {
            result = result.Where(e => e.Level >= query.MinLevel.Value);
        }

        if (query.MaxLevel.HasValue)
        {
            result = result.Where(e => e.Level <= query.MaxLevel.Value);
        }

        // Filter by category pattern
        if (!string.IsNullOrEmpty(query.CategoryPattern))
        {
            var regex = WildcardToRegex(query.CategoryPattern);
            result = result.Where(e => regex.IsMatch(e.Category));
        }

        // Filter by message content
        if (!string.IsNullOrEmpty(query.MessageContains))
        {
            result = result.Where(e => e.Message.Contains(query.MessageContains, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by time range
        if (query.After.HasValue)
        {
            result = result.Where(e => e.Timestamp > query.After.Value);
        }

        if (query.Before.HasValue)
        {
            result = result.Where(e => e.Timestamp < query.Before.Value);
        }

        // Apply ordering
        result = query.NewestFirst
            ? result.OrderByDescending(e => e.Timestamp)
            : result.OrderBy(e => e.Timestamp);

        // Apply pagination
        if (query.Skip > 0)
        {
            result = result.Skip(query.Skip);
        }

        if (query.MaxResults > 0)
        {
            result = result.Take(query.MaxResults);
        }

        return result.ToList();
    }

    /// <inheritdoc />
    public LogStats GetStats()
    {
        return new LogStats
        {
            TotalCount = _entryCount,
            TraceCount = _traceCount,
            DebugCount = _debugCount,
            InfoCount = _infoCount,
            WarningCount = _warningCount,
            ErrorCount = _errorCount,
            FatalCount = _fatalCount,
            OldestTimestamp = _oldestTimestamp,
            NewestTimestamp = _newestTimestamp,
            Capacity = _maxEntries
        };
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return new Regex(regexPattern, RegexOptions.IgnoreCase);
    }

    /// <inheritdoc/>
    public void Flush()
    {
        // No buffering, nothing to flush
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Clear events to prevent memory leaks
        LogAdded = null;
        LogsCleared = null;
    }
}
