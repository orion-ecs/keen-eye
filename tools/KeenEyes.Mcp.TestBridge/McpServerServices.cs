using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.Mcp.TestBridge.Prompts;
using KeenEyes.Mcp.TestBridge.Resources;
using KeenEyes.Mcp.TestBridge.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace KeenEyes.Mcp.TestBridge;

/// <summary>
/// Registers the MCP TestBridge services shared by every client transport.
/// Centralising registration here guarantees the stdio and HTTP hosts expose an identical tool set.
/// </summary>
internal static class McpServerServices
{
    /// <summary>
    /// Registers the <see cref="BridgeConnectionManager"/> singleton and the MCP server with its
    /// full complement of tools, resources, and prompts. The caller adds the transport
    /// (for example <c>WithStdioServerTransport()</c> or <c>WithHttpTransport()</c>) to the returned builder.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    /// <param name="config">The resolved server configuration.</param>
    /// <returns>The MCP server builder, so the caller can attach a transport.</returns>
    public static IMcpServerBuilder AddSharedTestBridgeServices(IServiceCollection services, McpConfiguration config)
    {
        // Register the connection manager as singleton
        services.AddSingleton<BridgeConnectionManager>(sp =>
        {
            return new BridgeConnectionManager
            {
                HeartbeatInterval = TimeSpan.FromMilliseconds(config.HeartbeatInterval),
                PingTimeout = TimeSpan.FromMilliseconds(config.HeartbeatTimeout),
                MaxPingFailures = config.MaxPingFailures,
                ConnectionTimeout = TimeSpan.FromMilliseconds(config.ConnectionTimeout)
            };
        });

        // Configure MCP server with the same capabilities and tools regardless of transport.
        return services
            .AddMcpServer(options =>
            {
                options.ServerInfo = new()
                {
                    Name = "KeenEyes TestBridge",
                    Version = "1.0.0"
                };
                options.Capabilities = new()
                {
                    Tools = new() { ListChanged = true },
                    Resources = new() { ListChanged = true, Subscribe = true },
                    Prompts = new() { ListChanged = true }
                };
            })
            .WithTools<GameTools>()
            .WithTools<InputTools>()
            .WithTools<StateTools>()
            .WithTools<CaptureTools>()
            .WithResources<ConnectionResources>()
            .WithResources<WorldResources>()
            .WithResources<ExtensionResources>()
            .WithResources<CaptureResources>()
            .WithPrompts<WorkflowPrompts>();
    }
}
