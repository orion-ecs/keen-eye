using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge;
using ModelContextProtocol.Server;

namespace KeenEyes.Mcp.TestBridge.Tools;

/// <summary>
/// MCP tools for game connection management.
/// </summary>
[McpServerToolType]
public sealed class GameTools(BridgeConnectionManager connection, McpServer server)
{
    [McpServerTool(Name = "game_connect")]
    [Description("Connect to a running KeenEyes game. Must be called before using other tools. Returns connection status.")]
    public async Task<ConnectionResult> GameConnect(
        [Description("Named pipe name (default: from KEENEYES_PIPE_NAME env var or 'KeenEyes.TestBridge')")]
        string? pipeName = null,
        [Description("TCP host for network connection (default: '127.0.0.1')")]
        string? host = null,
        [Description("TCP port for network connection (default: 19283)")]
        int? port = null,
        [Description("Transport mode: 'pipe' or 'tcp' (default: 'pipe')")]
        string transport = "pipe")
    {
        try
        {
            var options = new IpcOptions
            {
                PipeName = pipeName
                    ?? Environment.GetEnvironmentVariable("KEENEYES_PIPE_NAME")
                    ?? "KeenEyes.TestBridge",
                TransportMode = transport.Equals("tcp", StringComparison.OrdinalIgnoreCase)
                    ? IpcTransportMode.Tcp
                    : IpcTransportMode.NamedPipe,
                TcpBindAddress = host ?? "127.0.0.1",
                TcpPort = port ?? 19283
            };

            await connection.ConnectAsync(options);

            // Notify MCP client that tool/resource lists have changed
            await server.SendNotificationAsync("notifications/tools/list_changed");
            await server.SendNotificationAsync("notifications/resources/list_changed");

            return new ConnectionResult
            {
                Success = true,
                Message = $"Connected to game via {transport}",
                PipeName = options.PipeName,
                Host = options.TcpBindAddress,
                Port = options.TcpPort,
                Transport = options.TransportMode.ToString()
            };
        }
        catch (Exception ex)
        {
            return new ConnectionResult
            {
                Success = false,
                Message = $"Failed to connect: {ex.Message}"
            };
        }
    }

    [McpServerTool(Name = "game_disconnect")]
    [Description("Disconnect from the game.")]
    public async Task<ConnectionResult> GameDisconnect()
    {
        if (!connection.IsConnected)
        {
            return new ConnectionResult
            {
                Success = false,
                Message = "Not connected to any game"
            };
        }

        await connection.DisconnectAsync();

        // Notify MCP client that tool/resource lists have changed
        await server.SendNotificationAsync("notifications/tools/list_changed");
        await server.SendNotificationAsync("notifications/resources/list_changed");

        return new ConnectionResult
        {
            Success = true,
            Message = "Disconnected from game"
        };
    }

    [McpServerTool(Name = "game_status")]
    [Description("Get detailed connection status including latency and uptime.")]
    public ConnectionStatus GameStatus()
    {
        return connection.GetStatus();
    }
}

/// <summary>
/// Result of a connection operation.
/// </summary>
public sealed record ConnectionResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public string? PipeName { get; init; }
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Transport { get; init; }
}
