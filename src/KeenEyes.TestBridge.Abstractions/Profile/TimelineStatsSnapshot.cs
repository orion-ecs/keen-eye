namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of timeline recording status and statistics.
/// </summary>
/// <remarks>
/// Provides information about the current state of timeline recording,
/// including whether recording is active and the current frame number.
/// </remarks>
public sealed record TimelineStatsSnapshot
{
    /// <summary>
    /// Gets whether timeline recording is currently enabled.
    /// </summary>
    public required bool IsRecording { get; init; }

    /// <summary>
    /// Gets the current frame number.
    /// </summary>
    public required long CurrentFrame { get; init; }

    /// <summary>
    /// Gets the total number of recorded entries.
    /// </summary>
    public required int EntryCount { get; init; }
}
