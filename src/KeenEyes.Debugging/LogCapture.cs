using KeenEyes.Logging;

namespace KeenEyes.Debugging;

/// <summary>
/// Captures log entries during debug sessions for later analysis.
/// </summary>
/// <remarks>
/// <para>
/// LogCapture subscribes to an <see cref="ILogQueryable"/> provider and captures all
/// log entries that occur while capturing is active. This is useful for correlating
/// log output with profiling data or debugging specific scenarios.
/// </para>
/// <para>
/// Captured logs are stored in a bounded buffer to prevent unbounded memory growth
/// during long debug sessions.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var logCapture = world.GetExtension&lt;LogCapture&gt;();
///
/// // Start capturing logs
/// logCapture.StartCapture();
///
/// // Run some game logic that generates logs
/// world.Update(deltaTime);
///
/// // Stop and analyze
/// logCapture.StopCapture();
/// foreach (var entry in logCapture.GetCapturedEntries())
/// {
///     Console.WriteLine($"[{entry.Timestamp}] {entry.Level}: {entry.Message}");
/// }
/// </code>
/// </example>
public sealed class LogCapture : IDisposable
{
    private readonly ILogQueryable? logQueryable;
    private readonly List<LogEntry> capturedEntries = [];
    private readonly int maxEntries;
    private readonly Lock syncLock = new();
    private bool isCapturing;
    private bool disposed;

    /// <summary>
    /// Creates a new LogCapture instance.
    /// </summary>
    /// <param name="logQueryable">
    /// The queryable log provider to capture from. If null, capturing will be disabled.
    /// </param>
    /// <param name="maxEntries">
    /// Maximum number of entries to capture. Older entries are discarded when this limit is exceeded.
    /// Default is 10,000.
    /// </param>
    public LogCapture(ILogQueryable? logQueryable, int maxEntries = 10_000)
    {
        this.logQueryable = logQueryable;
        this.maxEntries = maxEntries;

        if (logQueryable is not null)
        {
            logQueryable.LogAdded += OnLogAdded;
        }
    }

    /// <summary>
    /// Gets whether log capturing is currently active.
    /// </summary>
    public bool IsCapturing => isCapturing;

    /// <summary>
    /// Gets the number of entries currently captured.
    /// </summary>
    public int CapturedCount
    {
        get
        {
            lock (syncLock)
            {
                return capturedEntries.Count;
            }
        }
    }

    /// <summary>
    /// Gets whether log capture is available (has a log provider to capture from).
    /// </summary>
    public bool IsAvailable => logQueryable is not null;

    /// <summary>
    /// Raised when a log entry is captured.
    /// </summary>
    public event Action<LogEntry>? EntryCaptured;

    /// <summary>
    /// Starts capturing log entries.
    /// </summary>
    /// <param name="clearPrevious">If true, clears any previously captured entries. Default is true.</param>
    /// <exception cref="InvalidOperationException">Thrown if already capturing.</exception>
    public void StartCapture(bool clearPrevious = true)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (isCapturing)
        {
            throw new InvalidOperationException("Log capture is already active.");
        }

        lock (syncLock)
        {
            if (clearPrevious)
            {
                capturedEntries.Clear();
            }
            isCapturing = true;
        }
    }

    /// <summary>
    /// Stops capturing log entries.
    /// </summary>
    public void StopCapture()
    {
        isCapturing = false;
    }

    /// <summary>
    /// Gets all captured log entries.
    /// </summary>
    /// <returns>A read-only list of captured entries, ordered by timestamp (oldest first).</returns>
    public IReadOnlyList<LogEntry> GetCapturedEntries()
    {
        lock (syncLock)
        {
            return capturedEntries.ToList();
        }
    }

    /// <summary>
    /// Gets captured entries filtered by minimum level.
    /// </summary>
    /// <param name="minLevel">Minimum log level to include.</param>
    /// <returns>Filtered entries.</returns>
    public IReadOnlyList<LogEntry> GetCapturedEntries(LogLevel minLevel)
    {
        lock (syncLock)
        {
            return capturedEntries.Where(e => e.Level >= minLevel).ToList();
        }
    }

    /// <summary>
    /// Gets captured entries filtered by category pattern.
    /// </summary>
    /// <param name="categoryPattern">Category pattern (supports * and ? wildcards).</param>
    /// <returns>Filtered entries.</returns>
    public IReadOnlyList<LogEntry> GetCapturedEntries(string categoryPattern)
    {
        var regex = WildcardToRegex(categoryPattern);
        lock (syncLock)
        {
            return capturedEntries.Where(e => regex.IsMatch(e.Category)).ToList();
        }
    }

    /// <summary>
    /// Clears all captured entries.
    /// </summary>
    public void Clear()
    {
        lock (syncLock)
        {
            capturedEntries.Clear();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        isCapturing = false;

        if (logQueryable is not null)
        {
            logQueryable.LogAdded -= OnLogAdded;
        }

        capturedEntries.Clear();
    }

    private void OnLogAdded(LogEntry entry)
    {
        if (!isCapturing)
        {
            return;
        }

        lock (syncLock)
        {
            // Trim if over capacity
            while (capturedEntries.Count >= maxEntries)
            {
                capturedEntries.RemoveAt(0);
            }

            capturedEntries.Add(entry);
        }

        EntryCaptured?.Invoke(entry);
    }

    private static System.Text.RegularExpressions.Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + System.Text.RegularExpressions.Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
