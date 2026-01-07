using System.ComponentModel;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.TestBridge;
using KeenEyes.TestBridge.State;
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

    [McpServerTool(Name = "game_get_screen_size")]
    [Description("Get the current game window dimensions in pixels.")]
    public async Task<ScreenSizeResult> GameGetScreenSize()
    {
        var bridge = connection.GetBridge();
        var (width, height) = await bridge.Capture.GetFrameSizeAsync();

        return new ScreenSizeResult
        {
            Width = width,
            Height = height
        };
    }

    [McpServerTool(Name = "game_wait_for_condition")]
    [Description("Wait for a game state condition. Useful for waiting after input before checking state.")]
    public async Task<WaitResult> GameWaitForCondition(
        [Description("Condition: 'entity_exists', 'entity_gone', 'component_exists', 'component_gone'")]
        string condition,
        [Description("Timeout in milliseconds (default: 5000)")]
        int timeoutMs = 5000,
        [Description("Entity ID to check (use this or entityName)")]
        int? entityId = null,
        [Description("Entity name to check (use this or entityId)")]
        string? entityName = null,
        [Description("Component type name (for component conditions)")]
        string? componentType = null)
    {
        var bridge = connection.GetBridge();
        var timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var startTime = DateTime.UtcNow;

        Func<IStateController, Task<bool>> conditionFunc = condition.ToLowerInvariant() switch
        {
            "entity_exists" => async state =>
            {
                if (entityId.HasValue)
                {
                    return await state.GetEntityAsync(entityId.Value) != null;
                }

                if (!string.IsNullOrEmpty(entityName))
                {
                    return await state.GetEntityByNameAsync(entityName) != null;
                }

                throw new ArgumentException("Either entityId or entityName must be provided for 'entity_exists' condition.");
            }
            ,
            "entity_gone" => async state =>
            {
                if (entityId.HasValue)
                {
                    return await state.GetEntityAsync(entityId.Value) == null;
                }

                if (!string.IsNullOrEmpty(entityName))
                {
                    return await state.GetEntityByNameAsync(entityName) == null;
                }

                throw new ArgumentException("Either entityId or entityName must be provided for 'entity_gone' condition.");
            }
            ,
            "component_exists" => async state =>
            {
                if (string.IsNullOrEmpty(componentType))
                {
                    throw new ArgumentException("componentType must be provided for 'component_exists' condition.");
                }

                var id = await ResolveEntityIdAsync(state, entityId, entityName);
                if (id == null)
                {
                    return false;
                }

                var component = await state.GetComponentAsync(id.Value, componentType);
                return component != null;
            }
            ,
            "component_gone" => async state =>
            {
                if (string.IsNullOrEmpty(componentType))
                {
                    throw new ArgumentException("componentType must be provided for 'component_gone' condition.");
                }

                var id = await ResolveEntityIdAsync(state, entityId, entityName);
                if (id == null)
                {
                    return true; // Entity gone means component is also gone
                }

                var component = await state.GetComponentAsync(id.Value, componentType);
                return component == null;
            }
            ,
            _ => throw new ArgumentException($"Invalid condition: '{condition}'. Valid conditions: entity_exists, entity_gone, component_exists, component_gone.")
        };

        var success = await bridge.WaitForAsync(conditionFunc, timeout);
        var elapsed = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

        return new WaitResult
        {
            Success = success,
            Message = success ? $"Condition '{condition}' met" : $"Timeout waiting for '{condition}'",
            ElapsedMs = elapsed
        };
    }

    private static async Task<int?> ResolveEntityIdAsync(
        IStateController state,
        int? entityId,
        string? entityName)
    {
        if (entityId.HasValue)
        {
            return entityId.Value;
        }

        if (!string.IsNullOrEmpty(entityName))
        {
            var entity = await state.GetEntityByNameAsync(entityName);
            return entity?.Id;
        }

        throw new ArgumentException("Either entityId or entityName must be provided.");
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

/// <summary>
/// Result containing screen dimensions.
/// </summary>
public sealed record ScreenSizeResult
{
    public required int Width { get; init; }
    public required int Height { get; init; }
}

/// <summary>
/// Result of a wait operation.
/// </summary>
public sealed record WaitResult
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required int ElapsedMs { get; init; }
}
