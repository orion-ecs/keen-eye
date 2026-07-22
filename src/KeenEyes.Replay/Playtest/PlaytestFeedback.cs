namespace KeenEyes.Replay.Playtest;

/// <summary>
/// Represents a single timestamped feedback entry recorded during a playtest session.
/// </summary>
/// <remarks>
/// Feedback entries are stored, in the order they were recorded, as <c>feedback.json</c>
/// in a playtest bundle archive.
/// </remarks>
public sealed record PlaytestFeedback
{
    /// <summary>
    /// Gets the UTC timestamp when the feedback was recorded.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the caller-defined category of the feedback (for example "bug" or "suggestion").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Gets the free-form feedback message.
    /// </summary>
    public required string Message { get; init; }
}
