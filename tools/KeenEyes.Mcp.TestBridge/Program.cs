using KeenEyes.Mcp.TestBridge;
using KeenEyes.Mcp.TestBridge.Connection;
using KeenEyes.Mcp.TestBridge.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

// Parse configuration from environment variables and command line
var config = ConfigurationParser.Parse(args);

// Build and run the MCP server
var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);

// Register the connection manager as singleton
builder.Services.AddSingleton<BridgeConnectionManager>(sp =>
{
    return new BridgeConnectionManager
    {
        HeartbeatInterval = TimeSpan.FromMilliseconds(config.HeartbeatInterval),
        PingTimeout = TimeSpan.FromMilliseconds(config.HeartbeatTimeout),
        MaxPingFailures = config.MaxPingFailures
    };
});

// Configure MCP server
builder.Services
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
            Resources = new() { ListChanged = true, Subscribe = true }
        };
    })
    .WithStdioServerTransport()
    .WithTools<GameTools>();

var app = builder.Build();

// Wire up connection state change notifications
var connectionManager = app.Services.GetRequiredService<BridgeConnectionManager>();
var mcpServer = app.Services.GetRequiredService<McpServer>();

connectionManager.ConnectionStateChanged += async () =>
{
    try
    {
        await mcpServer.SendNotificationAsync("notifications/tools/list_changed");
        await mcpServer.SendNotificationAsync("notifications/resources/list_changed");
    }
    catch
    {
        // Ignore notification errors
    }
};

// Log to stderr (stdout is for MCP protocol)
Console.Error.WriteLine($"KeenEyes MCP TestBridge Server starting...");
Console.Error.WriteLine($"Default pipe: {config.PipeName}");
Console.Error.WriteLine($"Default transport: {config.Transport}");
Console.Error.WriteLine($"Heartbeat interval: {config.HeartbeatInterval}ms");

await app.RunAsync();

// Cleanup
await connectionManager.DisposeAsync();

namespace KeenEyes.Mcp.TestBridge
{
    internal static class ConfigurationParser
    {
        public static McpConfiguration Parse(string[] args)
        {
            var pipeName = Environment.GetEnvironmentVariable("KEENEYES_PIPE_NAME") ?? "KeenEyes.TestBridge";
            var tcpHost = Environment.GetEnvironmentVariable("KEENEYES_HOST") ?? "127.0.0.1";
            var port = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_PORT"), out var p) ? p : 19283;
            var transport = Environment.GetEnvironmentVariable("KEENEYES_TRANSPORT") ?? "pipe";
            var heartbeatInterval = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_HEARTBEAT_INTERVAL"), out var hi) ? hi : 5000;
            var heartbeatTimeout = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_HEARTBEAT_TIMEOUT"), out var ht) ? ht : 10000;
            var maxPingFailures = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_MAX_PING_FAILURES"), out var mpf) ? mpf : 3;

            // Parse command line arguments
            var i = 0;
            while (i < args.Length)
            {
                var arg = args[i];
                if (arg == "--pipe" && i + 1 < args.Length)
                {
                    pipeName = args[i + 1];
                    i += 2;
                }
                else if (arg == "--host" && i + 1 < args.Length)
                {
                    tcpHost = args[i + 1];
                    i += 2;
                }
                else if (arg == "--port" && i + 1 < args.Length)
                {
                    port = int.Parse(args[i + 1]);
                    i += 2;
                }
                else if (arg == "--transport" && i + 1 < args.Length)
                {
                    transport = args[i + 1];
                    i += 2;
                }
                else
                {
                    i++;
                }
            }

            return new McpConfiguration(pipeName, tcpHost, port, transport, heartbeatInterval, heartbeatTimeout, maxPingFailures);
        }
    }

    internal sealed record McpConfiguration(
        string PipeName,
        string TcpHost,
        int Port,
        string Transport,
        int HeartbeatInterval,
        int HeartbeatTimeout,
        int MaxPingFailures);
}
