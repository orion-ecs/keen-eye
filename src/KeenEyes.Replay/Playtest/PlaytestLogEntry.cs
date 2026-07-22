namespace KeenEyes.Replay.Playtest;

/// <summary>
/// A serializable projection of a captured log entry included in a playtest bundle.
/// </summary>
/// <remarks>
/// <para>
/// Log entries are captured from an optional <see cref="Logging.ILogQueryable"/> source at
/// the moment the bundle is written and stored as <c>logs.json</c> in the archive. This
/// projection is used instead of <see cref="Logging.LogEntry"/> so the bundle format stays
/// stable and source-generation friendly, independent of the logging provider internals.
/// </para>
/// </remarks>
public sealed record PlaytestLogEntry
{
    /// <summary>
    /// Gets the time the message was logged.
    /// </summary>
    public required DateTime Timestamp { get; init; }

    /// <summary>
    /// Gets the severity level of the message.
    /// </summary>
    public required string Level { get; init; }

    /// <summary>
    /// Gets the category or source of the message.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the log message text.
    /// </summary>
    public required string Message { get; init; }
}
