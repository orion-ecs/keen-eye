using System.Diagnostics;
using KeenEyes.TestBridge;
using KeenEyes.TestBridge.Client;

namespace KeenEyes.Mcp.TestBridge.Connection;

/// <summary>
/// Manages the lifecycle of a TestBridge IPC connection with heartbeat monitoring.
/// </summary>
public sealed class BridgeConnectionManager : IAsyncDisposable
{
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private readonly CancellationTokenSource heartbeatCts = new();

    private TestBridgeClient? client;
    private IpcOptions? currentOptions;
    private Task? heartbeatTask;
    private DateTimeOffset? connectedAt;
    private bool disposed;

    /// <summary>
    /// Gets the heartbeat interval.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets the ping timeout.
    /// </summary>
    public TimeSpan PingTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets the maximum consecutive ping failures before disconnection.
    /// </summary>
    public int MaxPingFailures { get; init; } = 3;

    /// <summary>
    /// Gets the connection timeout for initial connection attempts.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets whether the connection is currently active.
    /// </summary>
    public bool IsConnected => client?.IsConnected == true;

    /// <summary>
    /// Gets the last measured ping latency in milliseconds.
    /// </summary>
    public double? LastPingLatencyMs { get; private set; }

    /// <summary>
    /// Gets the time of the last successful ping.
    /// </summary>
    public DateTimeOffset? LastPingTime { get; private set; }

    /// <summary>
    /// Gets the current pipe name (if using named pipe transport).
    /// </summary>
    public string? PipeName => currentOptions?.PipeName;

    /// <summary>
    /// Gets the current host (if using TCP transport).
    /// </summary>
    public string? Host => currentOptions?.TcpBindAddress;

    /// <summary>
    /// Gets the current port (if using TCP transport).
    /// </summary>
    public int? Port => currentOptions?.TcpPort;

    /// <summary>
    /// Gets the current transport mode.
    /// </summary>
    public string? TransportMode => currentOptions?.TransportMode.ToString();

    /// <summary>
    /// Gets the connection uptime.
    /// </summary>
    public TimeSpan? ConnectionUptime => connectedAt.HasValue
        ? DateTimeOffset.UtcNow - connectedAt.Value
        : null;

    /// <summary>
    /// Gets the number of consecutive ping failures.
    /// </summary>
    public int ConsecutivePingFailures { get; private set; }

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    public event Func<Task>? ConnectionStateChanged;

    /// <summary>
    /// Gets the current connection status.
    /// </summary>
    public ConnectionStatus GetStatus()
    {
        return new ConnectionStatus
        {
            IsConnected = IsConnected,
            LastPingMs = LastPingLatencyMs,
            LastPingTime = LastPingTime,
            PipeName = PipeName,
            Host = Host,
            Port = Port,
            TransportMode = TransportMode,
            ConnectionUptime = ConnectionUptime,
            ConsecutivePingFailures = ConsecutivePingFailures
        };
    }

    /// <summary>
    /// Connects to a TestBridge server with the specified options.
    /// </summary>
    /// <param name="options">IPC connection options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ConnectAsync(IpcOptions options, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        await connectionLock.WaitAsync(cancellationToken);
        try
        {
            // Disconnect existing connection if any
            if (client != null)
            {
                await DisconnectInternalAsync();
            }

            currentOptions = options;
            client = new TestBridgeClient(options);
            client.ConnectionChanged += OnClientConnectionChanged;

            await client.ConnectAsync(cancellationToken);
            connectedAt = DateTimeOffset.UtcNow;
            ConsecutivePingFailures = 0;

            // Start heartbeat
            StartHeartbeat();

            await NotifyConnectionStateChangedAsync();
        }
        finally
        {
            connectionLock.Release();
        }
    }

    /// <summary>
    /// Gets the TestBridge client, throwing if not connected.
    /// </summary>
    /// <returns>The active TestBridge client.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
    public ITestBridge GetBridge()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (client == null || !client.IsConnected)
        {
            throw new InvalidOperationException(
                "Not connected to game. Call game_connect first.");
        }

        return client;
    }

    /// <summary>
    /// Tries to get the TestBridge client.
    /// </summary>
    /// <param name="bridge">The bridge if connected.</param>
    /// <returns>True if connected and bridge is available.</returns>
    public bool TryGetBridge(out ITestBridge? bridge)
    {
        if (disposed || client == null || !client.IsConnected)
        {
            bridge = null;
            return false;
        }

        bridge = client;
        return true;
    }

    /// <summary>
    /// Disconnects from the TestBridge server.
    /// </summary>
    public async Task DisconnectAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        await connectionLock.WaitAsync();
        try
        {
            await DisconnectInternalAsync();
            await NotifyConnectionStateChangedAsync();
        }
        finally
        {
            connectionLock.Release();
        }
    }

    private async Task DisconnectInternalAsync()
    {
        StopHeartbeat();

        if (client != null)
        {
            client.ConnectionChanged -= OnClientConnectionChanged;
            await client.DisposeAsync();
            client = null;
        }

        connectedAt = null;
        LastPingLatencyMs = null;
        LastPingTime = null;
        ConsecutivePingFailures = 0;
    }

    private void StartHeartbeat()
    {
        if (heartbeatTask == null || heartbeatTask.IsCompleted)
        {
            heartbeatTask = RunHeartbeatAsync(heartbeatCts.Token);
        }
    }

    private void StopHeartbeat()
    {
        // Heartbeat will stop on next iteration when it checks IsConnected
    }

    private async Task RunHeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(HeartbeatInterval, ct);

                if (client == null || !client.IsConnected)
                {
                    break;
                }

                // Send a lightweight query to verify connection is truly alive
                var sw = Stopwatch.StartNew();
                try
                {
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(PingTimeout);

                    // Use entity count as a lightweight ping
                    await client.State.GetEntityCountAsync();
                    sw.Stop();

                    LastPingLatencyMs = sw.Elapsed.TotalMilliseconds;
                    LastPingTime = DateTimeOffset.UtcNow;
                    ConsecutivePingFailures = 0;
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    // Ping timeout
                    ConsecutivePingFailures++;
                    await HandlePingFailureAsync();
                }
                catch (Exception)
                {
                    ConsecutivePingFailures++;
                    await HandlePingFailureAsync();
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task HandlePingFailureAsync()
    {
        if (ConsecutivePingFailures >= MaxPingFailures)
        {
            await connectionLock.WaitAsync();
            try
            {
                await DisconnectInternalAsync();
                await NotifyConnectionStateChangedAsync();
            }
            finally
            {
                connectionLock.Release();
            }
        }
    }

    private void OnClientConnectionChanged(bool isConnected)
    {
        if (!isConnected)
        {
            // Connection lost - clean up
            _ = Task.Run(async () =>
            {
                await connectionLock.WaitAsync();
                try
                {
                    connectedAt = null;
                    LastPingLatencyMs = null;
                    await NotifyConnectionStateChangedAsync();
                }
                finally
                {
                    connectionLock.Release();
                }
            });
        }
    }

    private async Task NotifyConnectionStateChangedAsync()
    {
        if (ConnectionStateChanged != null)
        {
            await ConnectionStateChanged.Invoke();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        await heartbeatCts.CancelAsync();

        if (heartbeatTask != null)
        {
            try
            {
                await heartbeatTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        if (client != null)
        {
            await client.DisposeAsync();
        }

        connectionLock.Dispose();
        heartbeatCts.Dispose();
    }
}
