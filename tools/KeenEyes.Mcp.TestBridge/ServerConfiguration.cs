namespace KeenEyes.Mcp.TestBridge;

/// <summary>
/// The transport used by MCP clients (such as Claude Code) to reach this MCP server.
/// This is orthogonal to how the server reaches the game (see <see cref="McpConfiguration.Transport"/>).
/// </summary>
internal enum McpClientTransport
{
    /// <summary>Standard input/output transport. Default; used by the local-subprocess flow.</summary>
    Stdio,

    /// <summary>Streamable HTTP transport, allowing a remote MCP client to connect over the network.</summary>
    Http,
}

/// <summary>
/// Fully-resolved configuration for the MCP TestBridge server.
/// </summary>
/// <param name="PipeName">Named pipe the server uses to reach the game (game hop).</param>
/// <param name="TcpHost">TCP host the server uses to reach the game (game hop).</param>
/// <param name="Port">TCP port the server uses to reach the game (game hop).</param>
/// <param name="Transport">How the server reaches the game: <c>pipe</c> or <c>tcp</c>.</param>
/// <param name="HeartbeatInterval">Interval, in milliseconds, between heartbeats to the game.</param>
/// <param name="HeartbeatTimeout">Ping timeout, in milliseconds, before a heartbeat is considered failed.</param>
/// <param name="MaxPingFailures">Consecutive ping failures tolerated before disconnecting.</param>
/// <param name="ConnectionTimeout">Connection timeout, in milliseconds, when reaching the game.</param>
/// <param name="ClientTransport">How MCP clients reach this server: <see cref="McpClientTransport.Stdio"/> or <see cref="McpClientTransport.Http"/>.</param>
/// <param name="HttpUrl">URL the HTTP transport listens on (only used when <paramref name="ClientTransport"/> is HTTP).</param>
/// <param name="AuthToken">Optional bearer token required by the HTTP endpoint; when <see langword="null"/> the endpoint is unauthenticated.</param>
internal sealed record McpConfiguration(
    string PipeName,
    string TcpHost,
    int Port,
    string Transport,
    int HeartbeatInterval,
    int HeartbeatTimeout,
    int MaxPingFailures,
    int ConnectionTimeout,
    McpClientTransport ClientTransport,
    string HttpUrl,
    string? AuthToken);

/// <summary>
/// Parses <see cref="McpConfiguration"/> from environment variables and command-line arguments.
/// </summary>
internal static class ConfigurationParser
{
    private const string LoopbackAddress = "127.0.0.1";
    private const int DefaultHttpPort = 19284;

    /// <summary>The default URL the HTTP transport binds to when none is configured. Loopback only.</summary>
    // S5332: plain HTTP is intentional here - the default binds to loopback (127.0.0.1) only, where
    // transport encryption adds nothing. Operators exposing the endpoint on a LAN set KEENEYES_MCP_URL
    // (with https:// and a reverse proxy / cert) plus KEENEYES_MCP_TOKEN per the security docs.
#pragma warning disable S5332
    public static string DefaultHttpUrl { get; } = $"http://{LoopbackAddress}:{DefaultHttpPort}/";
#pragma warning restore S5332

    /// <summary>
    /// Interprets a client-transport value (from env or flag) as an <see cref="McpClientTransport"/>.
    /// </summary>
    /// <param name="value">The raw value; <see langword="null"/> or empty selects the default (stdio).</param>
    /// <returns>The parsed transport.</returns>
    /// <exception cref="ArgumentException">Thrown when the value is not <c>stdio</c> or <c>http</c>.</exception>
    public static McpClientTransport ParseClientTransport(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return McpClientTransport.Stdio;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "stdio" => McpClientTransport.Stdio,
            "http" => McpClientTransport.Http,
            _ => throw new ArgumentException(
                $"Invalid MCP transport '{value}'. Expected 'stdio' or 'http'.", nameof(value)),
        };
    }

    /// <summary>
    /// Parses configuration from environment variables, then applies command-line overrides.
    /// </summary>
    /// <param name="args">The process command-line arguments.</param>
    /// <returns>The resolved configuration.</returns>
    public static McpConfiguration Parse(string[] args)
    {
        var pipeName = Environment.GetEnvironmentVariable("KEENEYES_PIPE_NAME") ?? "KeenEyes.TestBridge";
        var tcpHost = Environment.GetEnvironmentVariable("KEENEYES_HOST") ?? "127.0.0.1";
        var port = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_PORT"), out var p) ? p : 19283;
        var transport = Environment.GetEnvironmentVariable("KEENEYES_TRANSPORT") ?? "pipe";
        var heartbeatInterval = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_HEARTBEAT_INTERVAL"), out var hi) ? hi : 5000;
        var heartbeatTimeout = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_HEARTBEAT_TIMEOUT"), out var ht) ? ht : 10000;
        var maxPingFailures = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_MAX_PING_FAILURES"), out var mpf) ? mpf : 3;
        var connectionTimeout = int.TryParse(Environment.GetEnvironmentVariable("KEENEYES_TIMEOUT"), out var ct) ? ct : 30000;

        // Client-facing MCP transport (how Claude Code reaches this server).
        var clientTransport = Environment.GetEnvironmentVariable("KEENEYES_MCP_TRANSPORT");
        var httpUrl = Environment.GetEnvironmentVariable("KEENEYES_MCP_URL") ?? DefaultHttpUrl;
        var authToken = Environment.GetEnvironmentVariable("KEENEYES_MCP_TOKEN");

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
            else if (arg == "--timeout" && i + 1 < args.Length)
            {
                connectionTimeout = int.Parse(args[i + 1]);
                i += 2;
            }
            else if (arg == "--mcp-transport" && i + 1 < args.Length)
            {
                clientTransport = args[i + 1];
                i += 2;
            }
            else if (arg == "--mcp-url" && i + 1 < args.Length)
            {
                httpUrl = args[i + 1];
                i += 2;
            }
            else
            {
                i++;
            }
        }

        // Treat an explicitly blank token env as unset.
        if (string.IsNullOrWhiteSpace(authToken))
        {
            authToken = null;
        }

        return new McpConfiguration(
            pipeName,
            tcpHost,
            port,
            transport,
            heartbeatInterval,
            heartbeatTimeout,
            maxPingFailures,
            connectionTimeout,
            ParseClientTransport(clientTransport),
            httpUrl,
            authToken);
    }
}
