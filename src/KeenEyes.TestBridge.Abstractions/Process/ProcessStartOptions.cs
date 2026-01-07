namespace KeenEyes.TestBridge.Process;

/// <summary>
/// Options for starting a managed process.
/// </summary>
public sealed record ProcessStartOptions
{
    /// <summary>
    /// Gets the path to the executable.
    /// </summary>
    public required string Executable { get; init; }

    /// <summary>
    /// Gets the command line arguments.
    /// </summary>
    public string? Arguments { get; init; }

    /// <summary>
    /// Gets the working directory for the process.
    /// </summary>
    /// <remarks>
    /// If null, the current working directory is used.
    /// </remarks>
    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Gets environment variables to set or override for the process.
    /// </summary>
    /// <remarks>
    /// These are merged with the current environment. Existing variables
    /// with the same name are overwritten.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? EnvironmentVariables { get; init; }

    /// <summary>
    /// Gets whether to redirect standard input. Defaults to true.
    /// </summary>
    public bool RedirectStdin { get; init; } = true;

    /// <summary>
    /// Gets whether to redirect standard output. Defaults to true.
    /// </summary>
    public bool RedirectStdout { get; init; } = true;

    /// <summary>
    /// Gets whether to redirect standard error. Defaults to true.
    /// </summary>
    public bool RedirectStderr { get; init; } = true;

    /// <summary>
    /// Gets the maximum stdout buffer size in bytes. Defaults to 10MB.
    /// </summary>
    /// <remarks>
    /// When the buffer exceeds this size, the oldest data is discarded.
    /// </remarks>
    public int MaxStdoutBuffer { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets the maximum stderr buffer size in bytes. Defaults to 10MB.
    /// </summary>
    /// <remarks>
    /// When the buffer exceeds this size, the oldest data is discarded.
    /// </remarks>
    public int MaxStderrBuffer { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    /// Gets whether to create the process without a window. Defaults to true.
    /// </summary>
    public bool CreateNoWindow { get; init; } = true;

    /// <summary>
    /// Gets whether to use shell execute. Defaults to false.
    /// </summary>
    /// <remarks>
    /// When false, the executable is run directly. When true, the system
    /// shell is used, which enables features like file associations but
    /// disables stream redirection.
    /// </remarks>
    public bool UseShellExecute { get; init; } = false;
}
