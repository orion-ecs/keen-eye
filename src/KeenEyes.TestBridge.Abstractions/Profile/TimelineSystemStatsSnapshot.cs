namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Summary statistics for a system's execution in the timeline.
/// </summary>
/// <remarks>
/// Provides aggregated timing statistics for a system across all
/// recorded timeline entries.
/// </remarks>
public sealed record TimelineSystemStatsSnapshot
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string SystemName { get; init; }

    /// <summary>
    /// Gets the number of times the system was executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the total execution time across all calls in milliseconds.
    /// </summary>
    public required double TotalTimeMs { get; init; }

    /// <summary>
    /// Gets the average execution time per call in milliseconds.
    /// </summary>
    public required double AverageTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum execution time observed in milliseconds.
    /// </summary>
    public required double MinTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum execution time observed in milliseconds.
    /// </summary>
    public required double MaxTimeMs { get; init; }
}
