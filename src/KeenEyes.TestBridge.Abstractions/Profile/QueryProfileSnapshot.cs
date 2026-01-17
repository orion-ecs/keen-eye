namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of query execution profiling data.
/// </summary>
/// <remarks>
/// This represents timing and entity count metrics for query execution,
/// useful for identifying slow or frequently-called queries.
/// </remarks>
public sealed record QueryProfileSnapshot
{
    /// <summary>
    /// Gets the name of the query.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total time spent executing this query in milliseconds.
    /// </summary>
    public required double TotalTimeMs { get; init; }

    /// <summary>
    /// Gets the number of times this query has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the total number of entities processed across all executions.
    /// </summary>
    public required long TotalEntities { get; init; }

    /// <summary>
    /// Gets the average time per execution in milliseconds.
    /// </summary>
    public required double AverageTimeMs { get; init; }

    /// <summary>
    /// Gets the average number of entities per execution.
    /// </summary>
    public required long AverageEntities { get; init; }

    /// <summary>
    /// Gets the minimum execution time observed in milliseconds.
    /// </summary>
    public required double MinTimeMs { get; init; }

    /// <summary>
    /// Gets the maximum execution time observed in milliseconds.
    /// </summary>
    public required double MaxTimeMs { get; init; }

    /// <summary>
    /// Gets the minimum entity count observed in a single execution.
    /// </summary>
    public required int MinEntities { get; init; }

    /// <summary>
    /// Gets the maximum entity count observed in a single execution.
    /// </summary>
    public required int MaxEntities { get; init; }
}
