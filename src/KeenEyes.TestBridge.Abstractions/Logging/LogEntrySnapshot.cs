namespace KeenEyes.TestBridge.Logging;

/// <summary>
/// IPC-transportable snapshot of a log entry.
/// </summary>
/// <remarks>
/// This record is serializable for transmission over IPC and provides
/// a stable contract for log entry data.
/// </remarks>
public sealed record LogEntrySnapshot
{
    /// <summary>
    /// Gets the timestamp when the message was logged.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the log level as an integer.
    /// </summary>
    /// <remarks>
    /// Maps to KeenEyes.Logging.LogLevel enum values:
    /// 0 = Trace, 1 = Debug, 2 = Info, 3 = Warning, 4 = Error, 5 = Fatal.
    /// </remarks>
    public required int Level { get; init; }

    /// <summary>
    /// Gets the log level name for display purposes.
    /// </summary>
    public required string LevelName { get; init; }

    /// <summary>
    /// Gets the category or source of the message.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the log message text.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the structured properties, if any.
    /// </summary>
    /// <remarks>
    /// Properties are serialized as a dictionary of string keys to JSON-compatible values.
    /// </remarks>
    public IReadOnlyDictionary<string, object?>? Properties { get; init; }
}
