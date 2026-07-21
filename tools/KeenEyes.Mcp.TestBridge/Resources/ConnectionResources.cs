using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for connection status information.
/// </summary>
/// <param name="connection">The connection manager used to reach the active test bridge.</param>
[McpServerResourceType]
public sealed class ConnectionResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string ConnectionStatusUri = "keeneyes://connection/status";
#pragma warning restore S1075

    /// <summary>
    /// Gets the current connection status as a JSON text resource.
    /// </summary>
    /// <returns>A <see cref="TextResourceContents"/> containing the serialized connection status.</returns>
    [McpServerResource(
        UriTemplate = ConnectionStatusUri,
        Name = "connection-status",
        Title = "Connection Status",
        MimeType = "application/json")]
    public TextResourceContents GetConnectionStatus()
    {
        var status = connection.GetStatus();
        var json = JsonSerializer.Serialize(status, JsonOptions.Default);
        return new TextResourceContents
        {
            Uri = ConnectionStatusUri,
            MimeType = "application/json",
            Text = json
        };
    }
}
