namespace KeenEyes.TestBridge.Logging;

/// <summary>
/// IPC-transportable snapshot of log statistics.
/// </summary>
public sealed record LogStatsSnapshot
{
    /// <summary>
    /// Gets the total number of log entries.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of trace-level entries.
    /// </summary>
    public required int TraceCount { get; init; }

    /// <summary>
    /// Gets the number of debug-level entries.
    /// </summary>
    public required int DebugCount { get; init; }

    /// <summary>
    /// Gets the number of info-level entries.
    /// </summary>
    public required int InfoCount { get; init; }

    /// <summary>
    /// Gets the number of warning-level entries.
    /// </summary>
    public required int WarningCount { get; init; }

    /// <summary>
    /// Gets the number of error-level entries.
    /// </summary>
    public required int ErrorCount { get; init; }

    /// <summary>
    /// Gets the number of fatal-level entries.
    /// </summary>
    public required int FatalCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the oldest entry, if any.
    /// </summary>
    public DateTime? OldestTimestamp { get; init; }

    /// <summary>
    /// Gets the timestamp of the newest entry, if any.
    /// </summary>
    public DateTime? NewestTimestamp { get; init; }

    /// <summary>
    /// Gets the maximum capacity of the log buffer, if bounded.
    /// </summary>
    public int? Capacity { get; init; }
}
