namespace KeenEyes.TestBridge.Process;

/// <summary>
/// Result of waiting for a process to exit.
/// </summary>
public sealed record ProcessExitResult
{
    /// <summary>
    /// Gets whether the process exited within the timeout.
    /// </summary>
    public required bool Completed { get; init; }

    /// <summary>
    /// Gets the exit code if the process completed.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets all stdout output captured during the process lifetime.
    /// </summary>
    public required string Stdout { get; init; }

    /// <summary>
    /// Gets all stderr output captured during the process lifetime.
    /// </summary>
    public required string Stderr { get; init; }

    /// <summary>
    /// Gets the duration the process ran.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
