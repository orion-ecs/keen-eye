namespace KeenEyes.TestBridge.Process;

/// <summary>
/// Information about a managed process.
/// </summary>
public sealed record ProcessInfo
{
    /// <summary>
    /// Gets the process ID.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Gets the executable path.
    /// </summary>
    public required string Executable { get; init; }

    /// <summary>
    /// Gets the command line arguments.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Gets the working directory.
    /// </summary>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets whether the process has exited.
    /// </summary>
    public required bool HasExited { get; init; }

    /// <summary>
    /// Gets the exit code if the process has exited.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets the time when the process was started.
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// Gets the time when the process exited, if it has exited.
    /// </summary>
    public DateTime? ExitTime { get; init; }
}
