namespace KeenEyes.Logging;

/// <summary>
/// Statistics about captured log entries.
/// </summary>
/// <remarks>
/// Provides summary information about the log entries stored in a queryable
/// log provider, including counts per level and timing information.
/// </remarks>
public sealed record LogStats
{
    /// <summary>
    /// Gets the total number of log entries.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the number of trace-level entries.
    /// </summary>
    public int TraceCount { get; init; }

    /// <summary>
    /// Gets the number of debug-level entries.
    /// </summary>
    public int DebugCount { get; init; }

    /// <summary>
    /// Gets the number of info-level entries.
    /// </summary>
    public int InfoCount { get; init; }

    /// <summary>
    /// Gets the number of warning-level entries.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets the number of error-level entries.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Gets the number of fatal-level entries.
    /// </summary>
    public int FatalCount { get; init; }

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
    /// <remarks>
    /// For unbounded providers, this will be null.
    /// </remarks>
    public int? Capacity { get; init; }

    /// <summary>
    /// Gets the count for a specific log level.
    /// </summary>
    /// <param name="level">The log level to get the count for.</param>
    /// <returns>The number of entries at the specified level.</returns>
    public int GetCountForLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => TraceCount,
            LogLevel.Debug => DebugCount,
            LogLevel.Info => InfoCount,
            LogLevel.Warning => WarningCount,
            LogLevel.Error => ErrorCount,
            LogLevel.Fatal => FatalCount,
            _ => 0
        };
    }
}
