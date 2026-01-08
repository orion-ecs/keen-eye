namespace KeenEyes.Logging;

/// <summary>
/// Represents a captured log entry.
/// </summary>
/// <param name="Timestamp">The time the message was logged.</param>
/// <param name="Level">The severity level of the message.</param>
/// <param name="Category">The category or source of the message.</param>
/// <param name="Message">The log message text.</param>
/// <param name="Properties">The structured properties, if any.</param>
/// <remarks>
/// This record is used by log providers that capture and store log entries
/// for later retrieval, such as <see cref="Providers.RingBufferLogProvider"/>
/// and <see cref="Providers.TestLogProvider"/>.
/// </remarks>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    IReadOnlyDictionary<string, object?>? Properties);
