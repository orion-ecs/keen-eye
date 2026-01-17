using System.Diagnostics;
using System.Text.Json;
using KeenEyes.TestBridge.Ipc.Handlers;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Ipc.Transport;

namespace KeenEyes.TestBridge.Ipc;

/// <summary>
/// IPC server that accepts external connections to control a running application.
/// </summary>
/// <remarks>
/// <para>
/// The server listens for connections on the configured transport (named pipe or TCP)
/// and routes commands to the appropriate handlers.
/// </para>
/// <para>
/// Only one client can be connected at a time. Additional connection attempts
/// will be rejected until the current client disconnects.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In the game application
/// using var world = new World();
/// using var bridge = new InProcessBridge(world);
/// using var server = new IpcBridgeServer(bridge, new IpcOptions { PipeName = "MyGame.TestBridge" });
///
/// await server.StartAsync();
///
/// // Game loop continues while server handles connections
/// while (!gameEnded)
/// {
///     world.Update(deltaTime);
/// }
/// </code>
/// </example>
public sealed class IpcBridgeServer : IDisposable
{
    private readonly IIpcTransport transport;
    private readonly Dictionary<string, ICommandHandler> handlers;
    private CancellationTokenSource? serverCts;
    private Task? listenTask;
    private bool disposed;

    /// <summary>
    /// Creates a new IPC bridge server.
    /// </summary>
    /// <param name="bridge">The test bridge to expose via IPC.</param>
    /// <param name="options">IPC configuration options.</param>
    public IpcBridgeServer(ITestBridge bridge, KeenEyes.TestBridge.IpcOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(bridge);

        options ??= new KeenEyes.TestBridge.IpcOptions();

        transport = options.TransportMode switch
        {
            KeenEyes.TestBridge.IpcTransportMode.NamedPipe => new NamedPipeTransport(options.PipeName, isServer: true),
            KeenEyes.TestBridge.IpcTransportMode.Tcp => new TcpIpcTransport(options.TcpBindAddress, options.TcpPort, isServer: true),
            _ => throw new ArgumentException($"Unknown transport mode: {options.TransportMode}", nameof(options))
        };

        handlers = new Dictionary<string, ICommandHandler>
        {
            ["input"] = new InputCommandHandler(bridge.Input),
            ["state"] = new StateCommandHandler(bridge.State),
            ["capture"] = new CaptureCommandHandler(bridge.Capture),
            ["log"] = new LogCommandHandler(bridge.Logs),
            ["window"] = new WindowCommandHandler(bridge.Window),
            ["time"] = new TimeCommandHandler(bridge.Time),
            ["system"] = new SystemCommandHandler(bridge.Systems),
            ["mutation"] = new MutationCommandHandler(bridge.Mutation),
            ["profile"] = new ProfileCommandHandler(bridge.Profile),
            ["snapshot"] = new SnapshotCommandHandler(bridge.Snapshot),
            ["ai"] = new AICommandHandler(bridge.AI),
            ["replay"] = new ReplayCommandHandler(bridge.Replay)
        };

        transport.MessageReceived += OnMessageReceived;
        transport.ConnectionChanged += OnConnectionChanged;
    }

    /// <summary>
    /// Gets whether the server is currently listening.
    /// </summary>
    public bool IsListening => listenTask != null && !listenTask.IsCompleted;

    /// <summary>
    /// Gets whether a client is currently connected.
    /// </summary>
    public bool IsConnected => transport.IsConnected;

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    public event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Starts the server listening for connections.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the server starts listening.</returns>
    /// <exception cref="InvalidOperationException">Thrown if already listening.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (IsListening)
        {
            throw new InvalidOperationException("Server is already listening.");
        }

        serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        listenTask = ListenLoopAsync(serverCts.Token);

        // Wait for first listen to start
        await Task.Yield();
    }

    /// <summary>
    /// Stops the server and disconnects any connected client.
    /// </summary>
    /// <returns>A task that completes when the server has stopped.</returns>
    public async Task StopAsync()
    {
        if (!IsListening)
        {
            return;
        }

        serverCts?.Cancel();

        if (listenTask != null)
        {
            try
            {
                await listenTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        await transport.DisconnectAsync();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        serverCts?.Cancel();
        transport.Dispose();
        serverCts?.Dispose();
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Wait for a client connection
                await transport.ListenAsync(cancellationToken);

                // Client connected - wait until they disconnect
                while (transport.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }

                // Client disconnected - loop back to listen for next client
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue listening
                Debug.WriteLine($"IPC server error: {ex.Message}");
                await Task.Delay(1000, cancellationToken); // Brief delay before retry
            }
        }
    }

    private void OnConnectionChanged(bool isConnected)
    {
        ConnectionChanged?.Invoke(isConnected);
    }

    private async void OnMessageReceived(ReadOnlyMemory<byte> data)
    {
        try
        {
            // Parse JSON request
            var request = JsonSerializer.Deserialize(data.Span, IpcJsonContext.Default.IpcRequest);
            if (request == null)
            {
                return;
            }

            // Process and respond
            var response = await ProcessRequestAsync(request);
            await SendResponseAsync(response);
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            Debug.WriteLine($"IPC message processing error: {ex.Message}");
        }
    }

    private async ValueTask<IpcResponse> ProcessRequestAsync(IpcRequest request)
    {
        try
        {
            // Parse command: "prefix.action" -> prefix, action
            var dotIndex = request.Command.IndexOf('.');
            if (dotIndex < 0)
            {
                return IpcResponse.Fail(request.Id, $"Invalid command format: {request.Command}");
            }

            var prefix = request.Command[..dotIndex];
            var action = request.Command[(dotIndex + 1)..];

            if (!handlers.TryGetValue(prefix, out var handler))
            {
                return IpcResponse.Fail(request.Id, $"Unknown command prefix: {prefix}");
            }

            var result = await handler.HandleAsync(action, request.Args, CancellationToken.None);

            return IpcResponse.Ok(
                request.Id,
                result != null ? JsonSerializer.SerializeToElement(result, result.GetType(), IpcJsonContext.Default) : null);
        }
        catch (ArgumentException ex)
        {
            return IpcResponse.Fail(request.Id, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return IpcResponse.Fail(request.Id, ex.Message);
        }
        catch (Exception ex)
        {
            return IpcResponse.Fail(request.Id, $"Command execution failed: {ex.Message}");
        }
    }

    private async Task SendResponseAsync(IpcResponse response)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(response, IpcJsonContext.Default.IpcResponse);

        // Transport handles framing, just send the raw JSON
        await transport.SendAsync(json);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
