using System.Text.Json;
using KeenEyes.Mcp.TestBridge.Connection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Resources;

/// <summary>
/// MCP resources for connection status information.
/// </summary>
[McpServerResourceType]
public sealed class ConnectionResources(BridgeConnectionManager connection)
{
    // S1075: MCP protocol requires these URIs for resource identification
#pragma warning disable S1075
    private const string ConnectionStatusUri = "keeneyes://connection/status";
#pragma warning restore S1075

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
