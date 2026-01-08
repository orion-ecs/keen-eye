namespace KeenEyes.TestBridge.Logging;

/// <summary>
/// Query parameters for filtering log entries over IPC.
/// </summary>
public sealed record LogQueryDto
{
    /// <summary>
    /// Gets the minimum log level to include (inclusive).
    /// </summary>
    /// <remarks>
    /// Maps to KeenEyes.Logging.LogLevel enum values.
    /// </remarks>
    public int? MinLevel { get; init; }

    /// <summary>
    /// Gets the maximum log level to include (inclusive).
    /// </summary>
    public int? MaxLevel { get; init; }

    /// <summary>
    /// Gets the category pattern to filter by.
    /// </summary>
    /// <remarks>
    /// Supports wildcard patterns: * matches any sequence, ? matches any single character.
    /// </remarks>
    public string? CategoryPattern { get; init; }

    /// <summary>
    /// Gets the text that must be contained in the message.
    /// </summary>
    public string? MessageContains { get; init; }

    /// <summary>
    /// Gets the minimum timestamp (inclusive).
    /// </summary>
    public DateTime? After { get; init; }

    /// <summary>
    /// Gets the maximum timestamp (inclusive).
    /// </summary>
    public DateTime? Before { get; init; }

    /// <summary>
    /// Gets the maximum number of entries to return. Defaults to 1000.
    /// </summary>
    public int MaxResults { get; init; } = 1000;

    /// <summary>
    /// Gets the number of entries to skip. Defaults to 0.
    /// </summary>
    public int Skip { get; init; } = 0;

    /// <summary>
    /// Gets whether to return entries newest-first. Defaults to true.
    /// </summary>
    public bool NewestFirst { get; init; } = true;
}
