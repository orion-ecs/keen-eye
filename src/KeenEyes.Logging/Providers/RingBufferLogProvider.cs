using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace KeenEyes.Logging.Providers;

/// <summary>
/// A log provider that stores entries in a bounded ring buffer with query support.
/// </summary>
/// <remarks>
/// <para>
/// This provider maintains a fixed-size buffer of log entries. When the buffer
/// is full, the oldest entries are automatically evicted to make room for new ones.
/// </para>
/// <para>
/// The provider supports querying entries by level, category pattern, message content,
/// and time range. It is thread-safe for concurrent logging and querying.
/// </para>
/// <para>
/// Use this provider for:
/// <list type="bullet">
///     <item>Editor log consoles</item>
///     <item>Debug session captures</item>
///     <item>MCP log browsing</item>
///     <item>Any scenario requiring in-memory log history with bounded storage</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new RingBufferLogProvider(capacity: 5000);
/// logManager.AddProvider(provider);
///
/// // Later, query for errors
/// var errors = provider.Query(new LogQuery { MinLevel = LogLevel.Error });
/// </code>
/// </example>
public sealed class RingBufferLogProvider : ILogProvider, ILogQueryable
{
    /// <summary>
    /// The default capacity for new instances.
    /// </summary>
    public const int DefaultCapacity = 10000;

    private readonly ConcurrentQueue<LogEntry> entries = new();
    private readonly int capacity;
    private readonly Lock statsLock = new();

    // Statistics tracking
    private int traceCount;
    private int debugCount;
    private int infoCount;
    private int warningCount;
    private int errorCount;
    private int fatalCount;

    /// <summary>
    /// Initializes a new instance with the default capacity.
    /// </summary>
    public RingBufferLogProvider()
        : this(DefaultCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of entries to store.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when capacity is less than 1.</exception>
    public RingBufferLogProvider(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        this.capacity = capacity;
    }

    /// <inheritdoc />
    public string Name => "RingBuffer";

    /// <inheritdoc />
    public LogLevel MinimumLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets the maximum number of entries this provider can store.
    /// </summary>
    public int Capacity => capacity;

    /// <inheritdoc />
    public int EntryCount => entries.Count;

    /// <inheritdoc />
    public event Action<LogEntry>? LogAdded;

    /// <inheritdoc />
    public event Action? LogsCleared;

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

        entries.Enqueue(entry);
        IncrementLevelCount(level);

        // Evict oldest entries if over capacity
        while (entries.Count > capacity && entries.TryDequeue(out var evicted))
        {
            DecrementLevelCount(evicted.Level);
        }

        LogAdded?.Invoke(entry);
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> GetEntries()
    {
        return [.. entries];
    }

    /// <inheritdoc />
    public IReadOnlyList<LogEntry> Query(LogQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        IEnumerable<LogEntry> result = entries;

        // Apply level filters
        if (query.MinLevel.HasValue)
        {
            result = result.Where(e => e.Level >= query.MinLevel.Value);
        }

        if (query.MaxLevel.HasValue)
        {
            result = result.Where(e => e.Level <= query.MaxLevel.Value);
        }

        // Apply category filter
        if (!string.IsNullOrEmpty(query.CategoryPattern))
        {
            var pattern = WildcardToRegex(query.CategoryPattern);
            result = result.Where(e => pattern.IsMatch(e.Category));
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

    /// <inheritdoc />
    public LogStats GetStats()
    {
        var snapshot = entries.ToArray();

        lock (statsLock)
        {
            return new LogStats
            {
                TotalCount = snapshot.Length,
                TraceCount = traceCount,
                DebugCount = debugCount,
                InfoCount = infoCount,
                WarningCount = warningCount,
                ErrorCount = errorCount,
                FatalCount = fatalCount,
                OldestTimestamp = snapshot.Length > 0 ? snapshot[0].Timestamp : null,
                NewestTimestamp = snapshot.Length > 0 ? snapshot[^1].Timestamp : null,
                Capacity = capacity
            };
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        while (entries.TryDequeue(out _))
        {
            // Drain the queue
        }

        lock (statsLock)
        {
            traceCount = 0;
            debugCount = 0;
            infoCount = 0;
            warningCount = 0;
            errorCount = 0;
            fatalCount = 0;
        }

        LogsCleared?.Invoke();
    }

    /// <inheritdoc />
    public void Flush()
    {
        // Nothing to flush - entries are stored synchronously
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Clear();
    }

    private void IncrementLevelCount(LogLevel level)
    {
        lock (statsLock)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    traceCount++;
                    break;
                case LogLevel.Debug:
                    debugCount++;
                    break;
                case LogLevel.Info:
                    infoCount++;
                    break;
                case LogLevel.Warning:
                    warningCount++;
                    break;
                case LogLevel.Error:
                    errorCount++;
                    break;
                case LogLevel.Fatal:
                    fatalCount++;
                    break;
            }
        }
    }

    private void DecrementLevelCount(LogLevel level)
    {
        lock (statsLock)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    traceCount = Math.Max(0, traceCount - 1);
                    break;
                case LogLevel.Debug:
                    debugCount = Math.Max(0, debugCount - 1);
                    break;
                case LogLevel.Info:
                    infoCount = Math.Max(0, infoCount - 1);
                    break;
                case LogLevel.Warning:
                    warningCount = Math.Max(0, warningCount - 1);
                    break;
                case LogLevel.Error:
                    errorCount = Math.Max(0, errorCount - 1);
                    break;
                case LogLevel.Fatal:
                    fatalCount = Math.Max(0, fatalCount - 1);
                    break;
            }
        }
    }

    private static Regex WildcardToRegex(string pattern)
    {
        // Escape regex special characters except * and ?
        var escaped = Regex.Escape(pattern);

        // Replace wildcard characters with regex equivalents
        // * matches any sequence of characters
        // ? matches any single character
        var regexPattern = escaped
            .Replace("\\*", ".*")
            .Replace("\\?", ".");

        return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
