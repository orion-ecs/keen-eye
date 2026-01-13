using System.Collections.Concurrent;
using System.Text.Json;
using KeenEyes.Input.Abstractions;
using KeenEyes.TestBridge.Capture;
using KeenEyes.TestBridge.Commands;
using KeenEyes.TestBridge.Input;
using KeenEyes.TestBridge.Ipc;
using KeenEyes.TestBridge.Ipc.Protocol;
using KeenEyes.TestBridge.Ipc.Transport;
using KeenEyes.TestBridge.Logging;
using KeenEyes.TestBridge.Process;
using KeenEyes.TestBridge.State;
using KeenEyes.TestBridge.Systems;
using KeenEyes.TestBridge.Time;
using KeenEyes.TestBridge.Window;

namespace KeenEyes.TestBridge.Client;

/// <summary>
/// Client for connecting to a running KeenEyes application via IPC.
/// </summary>
/// <remarks>
/// <para>
/// This client implements <see cref="ITestBridge"/> allowing the same test code
/// to work in-process or out-of-process.
/// </para>
/// <para>
/// All operations are asynchronous and communicate over the configured transport
/// (named pipes or TCP).
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Connect to running application
/// await using var client = new TestBridgeClient(new IpcOptions { PipeName = "MyGame.TestBridge" });
/// await client.ConnectAsync();
///
/// // Use same API as in-process
/// await client.Input.KeyPressAsync(Key.Space);
/// var entity = await client.State.GetEntityByNameAsync("Player");
/// </code>
/// </example>
public sealed class TestBridgeClient : ITestBridge, IAsyncDisposable
{
    private readonly IIpcTransport transport;
    private readonly ConcurrentDictionary<int, TaskCompletionSource<IpcResponse>> pendingRequests;
    private readonly TimeSpan requestTimeout;
    private int nextRequestId;
    private bool disposed;

    /// <summary>
    /// Creates a new test bridge client.
    /// </summary>
    /// <param name="options">IPC configuration options.</param>
    public TestBridgeClient(KeenEyes.TestBridge.IpcOptions? options = null)
    {
        options ??= new KeenEyes.TestBridge.IpcOptions();

        transport = options.TransportMode switch
        {
            KeenEyes.TestBridge.IpcTransportMode.NamedPipe => new NamedPipeTransport(options.PipeName, isServer: false),
            KeenEyes.TestBridge.IpcTransportMode.Tcp => new TcpIpcTransport(options.TcpBindAddress, options.TcpPort, isServer: false),
            _ => throw new ArgumentException($"Unknown transport mode: {options.TransportMode}", nameof(options))
        };

        pendingRequests = new ConcurrentDictionary<int, TaskCompletionSource<IpcResponse>>();
        requestTimeout = options.ConnectionTimeout;

        Input = new RemoteInputController(this);
        State = new RemoteStateController(this);
        Capture = new RemoteCaptureController(this);
        Logs = new RemoteLogController(this);
        Window = new RemoteWindowController(this);
        Time = new RemoteTimeController(this);
        Systems = new RemoteSystemController(this);

        transport.MessageReceived += OnMessageReceived;
        transport.ConnectionChanged += OnConnectionChanged;
    }

    /// <inheritdoc />
    public bool IsConnected => transport.IsConnected;

    /// <inheritdoc />
    public IInputController Input { get; }

    /// <inheritdoc />
    public IStateController State { get; }

    /// <inheritdoc />
    public ICaptureController Capture { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Process management is not supported over IPC. Use in-process testing
    /// with <see cref="InProcessBridge"/> for process management capabilities.
    /// </remarks>
    public IProcessController Process => throw new NotSupportedException(
        "Process management is not supported over IPC. Use in-process testing for process management.");

    /// <inheritdoc />
    public ILogController Logs { get; }

    /// <inheritdoc />
    public IWindowController Window { get; }

    /// <inheritdoc />
    public ITimeController Time { get; }

    /// <inheritdoc />
    public ISystemController Systems { get; }

    /// <inheritdoc />
    /// <remarks>
    /// Direct input context access is not supported over IPC. Use the <see cref="Input"/>
    /// controller to inject input events, which will be forwarded to the server.
    /// </remarks>
    public IInputContext InputContext => throw new NotSupportedException(
        "Direct input context access is not supported over IPC. Use the Input controller to inject input events.");

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    public event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Connects to the IPC server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when connected.</returns>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await transport.ConnectAsync(cancellationToken);
    }

    /// <summary>
    /// Disconnects from the IPC server.
    /// </summary>
    /// <returns>A task that completes when disconnected.</returns>
    public async Task DisconnectAsync()
    {
        await transport.DisconnectAsync();
    }

    /// <inheritdoc />
    public async Task<CommandResult> ExecuteAsync(ITestCommand command, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var response = await SendRequestAsync(command.CommandType, null, cancellationToken);
            return new CommandResult
            {
                Success = response.Success,
                Error = response.Error,
                // Return the raw JsonElement clone for maximum flexibility
                Data = response.Data.HasValue ? response.Data.Value.Clone() : null
            };
        }
        catch (Exception ex)
        {
            return CommandResult.Fail(ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<bool> WaitForAsync(
        Func<IStateController, Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!IsConnected)
        {
            return false;
        }

        var interval = pollInterval ?? TimeSpan.FromMilliseconds(16); // ~60fps
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline && IsConnected)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await condition(State))
            {
                return true;
            }

            await Task.Delay(interval, cancellationToken);
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> WaitForAsync(
        Func<IStateController, bool> condition,
        TimeSpan timeout,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        return await WaitForAsync(
            state => Task.FromResult(condition(state)),
            timeout,
            pollInterval,
            cancellationToken);
    }

    /// <summary>
    /// Sends a request to the server and waits for a response.
    /// </summary>
    internal async Task<IpcResponse> SendRequestAsync(
        string command,
        object? args,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        ThrowIfNotConnected();

        var requestId = Interlocked.Increment(ref nextRequestId);
        var tcs = new TaskCompletionSource<IpcResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        pendingRequests[requestId] = tcs;

        try
        {
            var request = new IpcRequest
            {
                Id = requestId,
                Command = command,
                Args = args != null
                    ? JsonSerializer.SerializeToElement(args, args.GetType(), IpcJsonContext.Default)
                    : null
            };

            var json = JsonSerializer.SerializeToUtf8Bytes(request, IpcJsonContext.Default.IpcRequest);

            // Transport handles framing, just send the raw JSON
            await transport.SendAsync(json, cancellationToken);

            // Wait for response with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(requestTimeout);

            await using (timeoutCts.Token.Register(() => tcs.TrySetCanceled(timeoutCts.Token)))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            pendingRequests.TryRemove(requestId, out _);
        }
    }

    /// <summary>
    /// Sends a request and deserializes the response data.
    /// </summary>
    internal async Task<T?> SendRequestAsync<T>(
        string command,
        object? args,
        CancellationToken cancellationToken)
    {
        var response = await SendRequestAsync(command, args, cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(response.Error ?? "Command failed");
        }

        if (!response.Data.HasValue)
        {
            return default;
        }

        return DeserializeResponse<T>(response.Data.Value);
    }

    /// <summary>
    /// AOT-compatible deserialization helper using type dispatch.
    /// </summary>
    private static T? DeserializeResponse<T>(JsonElement element)
    {
        var type = typeof(T);

        // Primitives - use direct JsonElement methods
        if (type == typeof(int))
        {
            return (T)(object)element.GetInt32();
        }

        if (type == typeof(bool))
        {
            return (T)(object)element.GetBoolean();
        }

        if (type == typeof(string))
        {
            return (T?)(object?)element.GetString();
        }

        if (type == typeof(float))
        {
            return (T)(object)element.GetSingle();
        }

        if (type == typeof(double))
        {
            return (T)(object)element.GetDouble();
        }

        if (type == typeof(long))
        {
            return (T)(object)element.GetInt64();
        }

        // Nullable primitives
        if (type == typeof(int?))
        {
            return element.ValueKind == JsonValueKind.Null ? default : (T)(object)element.GetInt32();
        }

        if (type == typeof(bool?))
        {
            return element.ValueKind == JsonValueKind.Null ? default : (T)(object)element.GetBoolean();
        }

        // Complex types - use IpcJsonContext TypeInfo
        // Note: For reference types, T and T? are the same at runtime
        if (type == typeof(EntitySnapshot))
        {
            return element.ValueKind == JsonValueKind.Null ? default : (T?)(object?)element.Deserialize(IpcJsonContext.Default.EntitySnapshot);
        }

        if (type == typeof(EntitySnapshot[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.EntitySnapshotArray);
        }

        if (type == typeof(WorldStats))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.WorldStats);
        }

        if (type == typeof(SystemInfo[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.SystemInfoArray);
        }

        if (type == typeof(PerformanceMetrics))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.PerformanceMetrics);
        }

        if (type == typeof(FrameCapture))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.FrameCapture);
        }

        if (type == typeof(FrameCapture[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.FrameCaptureArray);
        }

        if (type == typeof(FrameSizeResult))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.FrameSizeResult);
        }

        if (type == typeof(MousePositionResult))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.MousePositionResult);
        }

        if (type == typeof(int[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.Int32Array);
        }

        if (type == typeof(Dictionary<string, object?>))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.DictionaryStringObject);
        }

        // Logging types
        if (type == typeof(LogEntrySnapshot[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.LogEntrySnapshotArray);
        }

        if (type == typeof(LogStatsSnapshot))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.LogStatsSnapshot);
        }

        // Window types
        if (type == typeof(WindowStateSnapshot))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.WindowStateSnapshot);
        }

        if (type == typeof(WindowSizeResult))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.WindowSizeResult);
        }

        // Time types
        if (type == typeof(TimeStateSnapshot))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.TimeStateSnapshot);
        }

        // System types
        if (type == typeof(SystemSnapshot))
        {
            return element.ValueKind == JsonValueKind.Null ? default : (T?)(object?)element.Deserialize(IpcJsonContext.Default.SystemSnapshot);
        }

        if (type == typeof(SystemSnapshot[]))
        {
            return (T?)(object?)element.Deserialize(IpcJsonContext.Default.SystemSnapshotArray);
        }

        // Fallback for unknown types - let the exception surface during development
        throw new NotSupportedException($"Type {type.Name} is not registered in IpcJsonContext for AOT deserialization. Add it to DeserializeResponse<T>().");
    }

    private void OnConnectionChanged(bool isConnected)
    {
        if (!isConnected)
        {
            // Cancel all pending requests on disconnect
            foreach (var kvp in pendingRequests)
            {
                kvp.Value.TrySetException(new InvalidOperationException("Connection lost"));
            }
            pendingRequests.Clear();
        }

        ConnectionChanged?.Invoke(isConnected);
    }

    private void OnMessageReceived(ReadOnlyMemory<byte> data)
    {
        try
        {
            var response = JsonSerializer.Deserialize(data.Span, IpcJsonContext.Default.IpcResponse);
            if (response == null)
            {
                return;
            }

            if (pendingRequests.TryGetValue(response.Id, out var tcs))
            {
                tcs.TrySetResult(response);
            }
        }
        catch
        {
            // Ignore malformed messages
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        transport.Dispose();

        // Cancel all pending requests
        foreach (var kvp in pendingRequests)
        {
            kvp.Value.TrySetCanceled();
        }
        pendingRequests.Clear();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        await DisconnectAsync();
        Dispose();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected to server.");
        }
    }
}
