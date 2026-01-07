namespace KeenEyes.TestBridge.Process;

/// <summary>
/// Controller for managing child processes spawned during testing.
/// </summary>
/// <remarks>
/// <para>
/// The process controller enables external process management for integration
/// and end-to-end testing scenarios. It provides functionality for:
/// </para>
/// <list type="bullet">
/// <item><description>Starting processes with configurable options</description></item>
/// <item><description>Capturing stdout/stderr output</description></item>
/// <item><description>Writing to stdin for interactive processes</description></item>
/// <item><description>Graceful and forced termination</description></item>
/// <item><description>Waiting for process exit or specific output</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Start a game process and wait for it to initialize
/// var process = await bridge.Process.StartAsync("./MyGame.exe", "--test-mode");
/// await bridge.Process.WaitForOutputAsync(process.ProcessId, "Ready", TimeSpan.FromSeconds(10));
///
/// // Interact with the game via TestBridge IPC
/// await bridge.Input.KeyPressAsync(Key.Space);
///
/// // Capture screenshot and terminate
/// await bridge.Capture.SaveScreenshotAsync("test.png");
/// await bridge.Process.TerminateAsync(process.ProcessId);
/// </code>
/// </example>
public interface IProcessController
{
    /// <summary>
    /// Gets all currently running managed processes.
    /// </summary>
    IReadOnlyList<ProcessInfo> RunningProcesses { get; }

    /// <summary>
    /// Starts a new process with the specified options.
    /// </summary>
    /// <param name="options">The process start options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the started process.</returns>
    /// <exception cref="FileNotFoundException">The executable was not found.</exception>
    /// <exception cref="InvalidOperationException">The process could not be started.</exception>
    Task<ProcessInfo> StartAsync(ProcessStartOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts a new process with minimal configuration.
    /// </summary>
    /// <param name="executable">The path to the executable.</param>
    /// <param name="arguments">Optional command line arguments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Information about the started process.</returns>
    /// <exception cref="FileNotFoundException">The executable was not found.</exception>
    Task<ProcessInfo> StartAsync(string executable, string? arguments = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current information about a managed process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The process info, or null if not found or not managed.</returns>
    ProcessInfo? GetProcess(int processId);

    /// <summary>
    /// Writes a line to the process's standard input.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="line">The line to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">The process is not running or stdin is not redirected.</exception>
    Task WriteLineAsync(int processId, string line, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads all available stdout output since the last read.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The captured stdout output.</returns>
    /// <remarks>
    /// This clears the internal buffer after reading. Subsequent calls return
    /// only new output received since the previous call.
    /// </remarks>
    string ReadStdout(int processId);

    /// <summary>
    /// Reads all available stderr output since the last read.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The captured stderr output.</returns>
    /// <remarks>
    /// This clears the internal buffer after reading. Subsequent calls return
    /// only new output received since the previous call.
    /// </remarks>
    string ReadStderr(int processId);

    /// <summary>
    /// Peeks at stdout without consuming the buffer.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The current stdout buffer contents.</returns>
    string PeekStdout(int processId);

    /// <summary>
    /// Peeks at stderr without consuming the buffer.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <returns>The current stderr buffer contents.</returns>
    string PeekStderr(int processId);

    /// <summary>
    /// Waits for the process to exit.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="timeout">Maximum time to wait. Null for infinite.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit result including exit code and captured output.</returns>
    Task<ProcessExitResult> WaitForExitAsync(int processId, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Waits for specific text to appear in stdout.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="text">The text to wait for.</param>
    /// <param name="timeout">Maximum time to wait.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the text appeared; false if timeout.</returns>
    Task<bool> WaitForOutputAsync(int processId, string text, TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests graceful termination of the process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// On Windows, this sends a console control event (Ctrl+C).
    /// On Unix, this sends SIGTERM.
    /// The process may not terminate immediately; use <see cref="WaitForExitAsync"/> to wait.
    /// </remarks>
    Task TerminateAsync(int processId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Forces immediate termination of the process.
    /// </summary>
    /// <param name="processId">The process ID.</param>
    /// <remarks>
    /// This forcefully kills the process without allowing cleanup.
    /// On Windows, this uses TerminateProcess.
    /// On Unix, this sends SIGKILL.
    /// </remarks>
    Task KillAsync(int processId);

    /// <summary>
    /// Kills all managed processes.
    /// </summary>
    /// <remarks>
    /// This is typically called during cleanup to ensure no orphan processes remain.
    /// </remarks>
    Task KillAllAsync();
}
