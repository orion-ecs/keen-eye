namespace KeenEyes.Logging;

/// <summary>
/// Defines query parameters for filtering log entries.
/// </summary>
/// <remarks>
/// Use this record with <see cref="ILogQueryable.Query"/> to filter log entries
/// by level, category, message content, time range, and pagination.
/// All properties are optional; unset properties apply no filter.
/// </remarks>
/// <example>
/// <code>
/// var query = new LogQuery
/// {
///     MinLevel = LogLevel.Warning,
///     CategoryPattern = "KeenEyes.*",
///     MaxResults = 50,
///     NewestFirst = true
/// };
/// var results = provider.Query(query);
/// </code>
/// </example>
public sealed record LogQuery
{
    /// <summary>
    /// Gets the minimum log level to include (inclusive).
    /// </summary>
    /// <remarks>
    /// When set, only entries at or above this level are returned.
    /// </remarks>
    public LogLevel? MinLevel { get; init; }

    /// <summary>
    /// Gets the maximum log level to include (inclusive).
    /// </summary>
    /// <remarks>
    /// When set, only entries at or below this level are returned.
    /// Combine with <see cref="MinLevel"/> to get a specific range.
    /// </remarks>
    public LogLevel? MaxLevel { get; init; }

    /// <summary>
    /// Gets the category pattern to filter by.
    /// </summary>
    /// <remarks>
    /// Supports wildcard patterns:
    /// <list type="bullet">
    ///     <item><c>*</c> matches any sequence of characters</item>
    ///     <item><c>?</c> matches any single character</item>
    /// </list>
    /// Example: <c>"KeenEyes.*"</c> matches <c>"KeenEyes.Core"</c>, <c>"KeenEyes.Physics"</c>, etc.
    /// </remarks>
    public string? CategoryPattern { get; init; }

    /// <summary>
    /// Gets the text that must be contained in the message.
    /// </summary>
    /// <remarks>
    /// Case-insensitive substring match.
    /// </remarks>
    public string? MessageContains { get; init; }

    /// <summary>
    /// Gets the minimum timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// When set, only entries logged at or after this time are returned.
    /// </remarks>
    public DateTime? After { get; init; }

    /// <summary>
    /// Gets the maximum timestamp (inclusive).
    /// </summary>
    /// <remarks>
    /// When set, only entries logged at or before this time are returned.
    /// </remarks>
    public DateTime? Before { get; init; }

    /// <summary>
    /// Gets the maximum number of entries to return.
    /// </summary>
    /// <remarks>
    /// Defaults to 1000. Use with <see cref="Skip"/> for pagination.
    /// </remarks>
    public int MaxResults { get; init; } = 1000;

    /// <summary>
    /// Gets the number of entries to skip before returning results.
    /// </summary>
    /// <remarks>
    /// Use with <see cref="MaxResults"/> for pagination.
    /// </remarks>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Gets whether to return entries newest-first.
    /// </summary>
    /// <remarks>
    /// When true (default), entries are returned in reverse chronological order.
    /// When false, entries are returned in chronological order.
    /// </remarks>
    public bool NewestFirst { get; init; } = true;
}
