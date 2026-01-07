using System.Text.Json;

namespace KeenEyes.TestBridge.Ipc.Handlers;

/// <summary>
/// Handles IPC commands of a specific category.
/// </summary>
/// <remarks>
/// Command handlers process commands for a specific prefix (e.g., "input", "state", "capture").
/// They parse arguments from JSON and execute the appropriate controller methods.
/// </remarks>
public interface ICommandHandler
{
    /// <summary>
    /// Gets the command prefix this handler processes.
    /// </summary>
    /// <remarks>
    /// For example, "input" handles commands like "input.keyDown", "input.click", etc.
    /// </remarks>
    string Prefix { get; }

    /// <summary>
    /// Handles a command and returns the response data.
    /// </summary>
    /// <param name="command">The command name (without prefix, e.g., "keyDown" not "input.keyDown").</param>
    /// <param name="args">The command arguments as a JSON element.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response data, or null for void commands.</returns>
    /// <exception cref="ArgumentException">Thrown when required arguments are missing or invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the command is unknown.</exception>
    ValueTask<object?> HandleAsync(string command, JsonElement? args, CancellationToken cancellationToken);
}
