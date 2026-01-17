namespace KeenEyes.TestBridge.Profile;

/// <summary>
/// Snapshot of GC allocation profiling data for a system.
/// </summary>
/// <remarks>
/// This represents memory allocation metrics per system, useful for
/// identifying allocation hotspots that may cause GC pressure.
/// </remarks>
public sealed record AllocationProfileSnapshot
{
    /// <summary>
    /// Gets the name of the system.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the total bytes allocated by this system across all calls.
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Gets the number of times this system has been executed.
    /// </summary>
    public required int CallCount { get; init; }

    /// <summary>
    /// Gets the average bytes allocated per system execution.
    /// </summary>
    public required long AverageBytes { get; init; }

    /// <summary>
    /// Gets the minimum bytes allocated in a single execution.
    /// </summary>
    public required long MinBytes { get; init; }

    /// <summary>
    /// Gets the maximum bytes allocated in a single execution.
    /// </summary>
    public required long MaxBytes { get; init; }
}
