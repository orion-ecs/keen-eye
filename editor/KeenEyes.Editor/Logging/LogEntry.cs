using KeenEyes.Logging;

namespace KeenEyes.Editor.Logging;

/// <summary>
/// Represents a single log entry captured by the editor.
/// </summary>
/// <param name="Timestamp">When the log was captured.</param>
/// <param name="Level">The severity level of the log.</param>
/// <param name="Category">The category/source of the log message.</param>
/// <param name="Message">The log message content.</param>
/// <param name="Properties">Optional structured properties.</param>
public sealed record LogEntry(
    DateTime Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    IReadOnlyDictionary<string, object?>? Properties = null)
{
    /// <summary>
    /// Gets the formatted timestamp string.
    /// </summary>
    public string FormattedTime => Timestamp.ToString("HH:mm:ss.fff");

    /// <summary>
    /// Gets whether this log entry represents an error.
    /// </summary>
    public bool IsError => Level >= LogLevel.Error;

    /// <summary>
    /// Gets whether this log entry represents a warning.
    /// </summary>
    public bool IsWarning => Level == LogLevel.Warning;
}
