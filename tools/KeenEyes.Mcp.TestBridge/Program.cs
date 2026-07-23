using KeenEyes.Mcp.TestBridge;
using KeenEyes.Mcp.TestBridge.Connection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

// Parse configuration from environment variables and command line
var config = ConfigurationParser.Parse(args);

// Select the client-facing transport (how Claude Code reaches this server).
// The pipe/tcp settings above are orthogonal: they choose how this server reaches the GAME.
if (config.ClientTransport == McpClientTransport.Http)
{
    await RunHttpServerAsync(config, args);
}
else
{
    await RunStdioServerAsync(config, args);
}

// Runs the default stdio transport - preserves the local-subprocess flow that .mcp.json relies on.
static async Task RunStdioServerAsync(McpConfiguration config, string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);

    // CRITICAL: Disable all console logging - stdout must only contain MCP JSON-RPC messages
    builder.Logging.ClearProviders();

    McpServerServices.AddSharedTestBridgeServices(builder.Services, config)
        .WithStdioServerTransport();

    var app = builder.Build();

    // Wire up connection state change notifications for the single long-lived stdio server.
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

    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        Console.Error.WriteLine("Shutting down MCP TestBridge Server...");
    });

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        lifetime.StopApplication();
    };

    // Log to stderr (stdout is for MCP protocol)
    Console.Error.WriteLine("KeenEyes MCP TestBridge Server starting (stdio transport)...");
    Console.Error.WriteLine($"Default pipe: {config.PipeName}");
    Console.Error.WriteLine($"Game transport: {config.Transport}");
    Console.Error.WriteLine($"Heartbeat interval: {config.HeartbeatInterval}ms");

    await app.RunAsync();

    await connectionManager.DisposeAsync();
}

// Runs the HTTP transport - lets a remote MCP client connect over the network.
static async Task RunHttpServerAsync(McpConfiguration config, string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // Log to stderr so stdout stays clean and nothing leaks to a captured stdout channel.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

    builder.WebHost.UseUrls(config.HttpUrl);

    McpServerServices.AddSharedTestBridgeServices(builder.Services, config)
        .WithHttpTransport();

    var app = builder.Build();

    // Security: require a bearer token when one is configured; otherwise warn loudly.
    if (config.AuthToken is not null)
    {
        app.UseMiddleware<BearerTokenAuthenticationMiddleware>(config.AuthToken);
    }
    else
    {
        app.Logger.LogWarning(
            "KEENEYES_MCP_TOKEN is not set: the HTTP MCP endpoint is UNAUTHENTICATED. " +
            "This endpoint can control a game process - bind it to loopback or a trusted LAN address only, " +
            "never to 0.0.0.0 or a public interface.");
    }

    app.MapMcp();

    var connectionManager = app.Services.GetRequiredService<BridgeConnectionManager>();

    app.Lifetime.ApplicationStopping.Register(() =>
    {
        app.Logger.LogInformation("Shutting down MCP TestBridge Server (HTTP transport)...");
    });

    app.Logger.LogInformation("KeenEyes MCP TestBridge Server starting (HTTP transport) on {Url}", config.HttpUrl);
    app.Logger.LogInformation("Game transport: {Transport}, default pipe: {Pipe}", config.Transport, config.PipeName);

    await app.RunAsync();

    await connectionManager.DisposeAsync();
}
