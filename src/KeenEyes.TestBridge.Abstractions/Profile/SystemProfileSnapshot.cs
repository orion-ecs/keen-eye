namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of system execution profiling data.
/// </summary>
/// <remarks>
/// This represents timing metrics for a system's execution, including
/// total time, call count, average time, and min/max times.
/// </remarks>
public sealed record SystemProfileSnapshot
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total time spent executing this system in milliseconds.
    /// </summary>
    public required double TotalTimeMs { get; init; }

    /// <summary>
    /// Gets the number of times this system has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the average time per execution in milliseconds.
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
