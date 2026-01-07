using KeenEyes.Network;
using KeenEyes.Network.Transport;
using KeenEyes.Network.Transport.Tcp;
using KeenEyes.Network.Transport.Udp;

namespace KeenEyes.Sample.Multiplayer;

/// <summary>
/// Supported transport types for the multiplayer demo.
/// </summary>
public enum TransportType
{
    /// <summary>
    /// In-process transport for testing. Runs both server and client in one process.
    /// </summary>
    Local,

    /// <summary>
    /// TCP transport. Reliable ordered delivery over TCP sockets.
    /// </summary>
    Tcp,

    /// <summary>
    /// UDP transport. Configurable reliability with multiple delivery modes.
    /// </summary>
    Udp
}

/// <summary>
/// Parsed command-line options for transport configuration.
/// </summary>
public sealed class TransportOptions
{
    /// <summary>
    /// The transport type to use.
    /// </summary>
    public TransportType Transport { get; init; } = TransportType.Local;

    /// <summary>
    /// Whether to run as server (true) or client (false).
    /// Only applies to TCP/UDP transports.
    /// </summary>
    public bool IsServer { get; init; }

    /// <summary>
    /// Server address to connect to (client mode only).
    /// </summary>
    public string Address { get; init; } = "localhost";

    /// <summary>
    /// Port number for server/client.
    /// </summary>
    public int Port { get; init; } = 7777;

    /// <summary>
    /// Whether to show help information.
    /// </summary>
    public bool ShowHelp { get; init; }

    /// <summary>
    /// Parses command-line arguments into transport options.
    /// </summary>
    // S127: Modifying loop counter in switch cases is a standard CLI arg parsing pattern
    // to consume the next argument as a value.
#pragma warning disable S127
    public static TransportOptions Parse(string[] args)
    {
        var transport = TransportType.Local;
        var isServer = false;
        var address = "localhost";
        var port = 7777;
        var showHelp = false;

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i].ToLowerInvariant();

            switch (arg)
            {
                case "--help":
                case "-h":
                case "-?":
                    showHelp = true;
                    break;

                case "--transport":
                case "-t":
                    if (i + 1 < args.Length)
                    {
                        transport = args[++i].ToLowerInvariant() switch
                        {
                            "local" => TransportType.Local,
                            "tcp" => TransportType.Tcp,
                            "udp" => TransportType.Udp,
                            _ => throw new ArgumentException($"Unknown transport: {args[i]}")
                        };
                    }
                    break;

                case "--server":
                case "-s":
                    isServer = true;
                    break;

                case "--client":
                case "-c":
                    isServer = false;
                    break;

                case "--address":
                case "-a":
                    if (i + 1 < args.Length)
                    {
                        address = args[++i];
                    }
                    break;

                case "--port":
                case "-p":
                    if (i + 1 < args.Length)
                    {
                        port = int.Parse(args[++i]);
                    }
                    break;
            }
        }

        return new TransportOptions
        {
            Transport = transport,
            IsServer = isServer,
            Address = address,
            Port = port,
            ShowHelp = showHelp
        };
    }
#pragma warning restore S127

    /// <summary>
    /// Prints usage information to the console.
    /// </summary>
    public static void PrintHelp()
    {
        Console.WriteLine("""
            KeenEyes Multiplayer Sample - Transport Options

            Usage:
              dotnet run                                    # Local transport (default, in-process)
              dotnet run -- --transport tcp --server        # TCP server
              dotnet run -- --transport tcp --client        # TCP client
              dotnet run -- --transport udp --server        # UDP server
              dotnet run -- --transport udp --client        # UDP client

            Options:
              -t, --transport <type>   Transport type: local, tcp, udp (default: local)
              -s, --server             Run as server (TCP/UDP only)
              -c, --client             Run as client (TCP/UDP only)
              -a, --address <host>     Server address for client mode (default: localhost)
              -p, --port <port>        Port number (default: 7777)
              -h, --help               Show this help message

            Examples:
              # Quick test with local transport (both server and client in one process)
              dotnet run

              # TCP networking - start server first, then client
              Terminal 1: dotnet run -- -t tcp -s
              Terminal 2: dotnet run -- -t tcp -c

              # UDP networking with custom port
              Terminal 1: dotnet run -- -t udp -s -p 8888
              Terminal 2: dotnet run -- -t udp -c -p 8888

              # Connect to remote server
              dotnet run -- -t tcp -c -a 192.168.1.100 -p 7777
            """);
    }

    /// <summary>
    /// Creates a transport instance based on the configured options.
    /// For Local transport, returns a paired server/client.
    /// For TCP/UDP, returns a single transport in the appropriate mode.
    /// </summary>
    public INetworkTransport CreateTransport()
    {
        return Transport switch
        {
            TransportType.Tcp => new TcpTransport(),
            TransportType.Udp => new UdpTransport(),
            _ => throw new InvalidOperationException("Use CreateLocalTransportPair for Local transport")
        };
    }

    /// <summary>
    /// Creates a paired local transport for in-process testing.
    /// </summary>
    public static (INetworkTransport Server, INetworkTransport Client) CreateLocalTransportPair()
    {
        return LocalTransport.CreatePair();
    }
}
