using System.Collections.Concurrent;
using KeenEyes.Logging;

namespace KeenEyes.Editor.Logging;

/// <summary>
/// A log provider that captures log entries for display in the editor console.
/// </summary>
/// <remarks>
/// This provider maintains a ring buffer of log entries to prevent unbounded memory usage.
/// Entries are stored thread-safely and can be accessed for UI display.
/// </remarks>
public sealed class EditorLogProvider : ILogProvider
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly int _maxEntries;
    private int _entryCount;
    private int _infoCount;
    private int _warningCount;
    private int _errorCount;

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
    public int InfoCount => _infoCount;

    /// <summary>
    /// Gets the count of warning-level log entries.
    /// </summary>
    public int WarningCount => _warningCount;

    /// <summary>
    /// Gets the count of error-level (and above) log entries.
    /// </summary>
    public int ErrorCount => _errorCount;

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

        // Update level counts
        if (level >= LogLevel.Error)
        {
            Interlocked.Increment(ref _errorCount);
        }
        else if (level == LogLevel.Warning)
        {
            Interlocked.Increment(ref _warningCount);
        }
        else
        {
            Interlocked.Increment(ref _infoCount);
        }

        // Trim old entries if over limit
        while (_entryCount > _maxEntries && _entries.TryDequeue(out var removed))
        {
            Interlocked.Decrement(ref _entryCount);

            // Update level counts for removed entry
            if (removed.Level >= LogLevel.Error)
            {
                Interlocked.Decrement(ref _errorCount);
            }
            else if (removed.Level == LogLevel.Warning)
            {
                Interlocked.Decrement(ref _warningCount);
            }
            else
            {
                Interlocked.Decrement(ref _infoCount);
            }
        }

        LogAdded?.Invoke(entry);
    }

    /// <summary>
    /// Gets all log entries as a snapshot.
    /// </summary>
    /// <returns>An enumerable of all current log entries.</returns>
    public IEnumerable<LogEntry> GetEntries()
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
        _infoCount = 0;
        _warningCount = 0;
        _errorCount = 0;

        LogsCleared?.Invoke();
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
