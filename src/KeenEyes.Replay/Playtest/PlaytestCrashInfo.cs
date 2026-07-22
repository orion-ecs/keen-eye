namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Captures the details of an exception that terminated a playtest session.
/// </summary>
/// <remarks>
/// Crash information is stored as <c>crash.json</c> in a playtest bundle archive and is
/// only present in bundles produced by
/// <see cref="PlaytestSession.CaptureCrashBundle(Exception)"/>.
/// </remarks>
public sealed record PlaytestCrashInfo
{
    /// <summary>
    /// Gets the fully qualified type name of the captured exception.
    /// </summary>
    public required string ExceptionType { get; init; }

    /// <summary>
    /// Gets the exception message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the exception stack trace, if one was available.
    /// </summary>
    public string? StackTrace { get; init; }

    /// <summary>
    /// Gets the details of the inner exception, if one was present.
    /// </summary>
    public PlaytestCrashInfo? InnerException { get; init; }
}
