namespace KeenEyes.Logging;

/// <summary>
/// Defines the severity level of a log message.
/// </summary>
/// <remarks>
/// Log levels are ordered by severity, from lowest (Trace) to highest (Fatal).
/// When filtering by level, all messages at or above the specified level are included.
/// </remarks>
public enum LogLevel
{
    /// <summary>
    /// Most detailed level. Used for fine-grained diagnostic information.
    /// </summary>
    /// <remarks>
    /// Trace messages are typically disabled in production due to high volume.
    /// Use for detailed execution flow tracing.
    /// </remarks>
    Trace = 0,

    /// <summary>
    /// Detailed debug information for development and troubleshooting.
    /// </summary>
    /// <remarks>
    /// Debug messages provide more detail than Info but less than Trace.
    /// Useful during development and when diagnosing issues.
    /// </remarks>
    Debug = 1,

    /// <summary>
    /// General informational messages about application progress.
    /// </summary>
    /// <remarks>
    /// Info messages track the general flow of the application.
    /// These are typically enabled in production.
    /// </remarks>
    Info = 2,

    /// <summary>
    /// Indicates a potential problem or unusual situation.
    /// </summary>
    /// <remarks>
    /// Warning messages indicate something unexpected happened,
    /// but the application can continue operating normally.
    /// </remarks>
    Warning = 3,

    /// <summary>
    /// An error that prevented an operation from completing.
    /// </summary>
    /// <remarks>
    /// Error messages indicate a failure in the current operation,
    /// but the application may be able to continue running.
    /// </remarks>
    Error = 4,

    /// <summary>
    /// A critical error that may cause the application to terminate.
    /// </summary>
    /// <remarks>
    /// Fatal messages indicate a severe error that may require
    /// immediate attention and could cause application shutdown.
    /// </remarks>
    Fatal = 5
}
