using System.Text.Json;

namespace KeenEyes.TestBridge.Ipc.Protocol;

/// <summary>
/// Represents an IPC request message.
/// </summary>
/// <remarks>
/// Requests are sent from the test client to the bridge server.
/// Each request has a unique ID for response correlation.
/// </remarks>
public sealed record IpcRequest
{
    /// <summary>
    /// Gets the request ID for response correlation.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets the command type in dotted notation (e.g., "input.keyDown", "state.getEntityCount").
    /// </summary>
    public required string Command { get; init; }

    /// <summary>
    /// Gets the command arguments as a JSON element for deferred parsing.
    /// </summary>
    /// <remarks>
    /// Using <see cref="JsonElement"/> allows AOT-compatible deserialization
    /// where the specific argument types are determined by the command handler.
    /// </remarks>
    public JsonElement? Args { get; init; }
}
