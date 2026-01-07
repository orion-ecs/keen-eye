using System.Text.Json;

namespace KeenEyes.TestBridge.Ipc.Protocol;

/// <summary>
/// Represents an IPC response message.
/// </summary>
/// <remarks>
/// Responses are sent from the bridge server to the test client.
/// Each response includes the request ID for correlation.
/// </remarks>
public sealed record IpcResponse
{
    /// <summary>
    /// Gets the request ID this response correlates to.
    /// </summary>
    public required int Id { get; init; }

    /// <summary>
    /// Gets whether the command executed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the error message if the command failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the response data (type depends on command).
    /// </summary>
    /// <remarks>
    /// Using <see cref="JsonElement"/> allows AOT-compatible serialization
    /// where the specific data type is determined by the command.
    /// </remarks>
    public JsonElement? Data { get; init; }

    /// <summary>
    /// Creates a successful response with optional data.
    /// </summary>
    public static IpcResponse Ok(int id, JsonElement? data = null) => new()
    {
        Id = id,
        Success = true,
        Data = data
    };

    /// <summary>
    /// Creates a failed response with an error message.
    /// </summary>
    public static IpcResponse Fail(int id, string error) => new()
    {
        Id = id,
        Success = false,
        Error = error
    };
}
