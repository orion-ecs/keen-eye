using KeenEyes.Logging;

namespace KeenEyes.Editor.Logging;

/// <summary>
/// Extension methods for <see cref="LogEntry"/>.
/// </summary>
public static class LogEntryExtensions
{
    /// <summary>
    /// Gets the formatted timestamp string.
    /// </summary>
    public static string GetFormattedTime(this LogEntry entry) => entry.Timestamp.ToString("HH:mm:ss.fff");

    /// <summary>
    /// Gets whether this log entry represents an error.
    /// </summary>
    public static bool IsError(this LogEntry entry) => entry.Level >= LogLevel.Error;

    /// <summary>
    /// Gets whether this log entry represents a warning.
    /// </summary>
    public static bool IsWarning(this LogEntry entry) => entry.Level == LogLevel.Warning;
}
