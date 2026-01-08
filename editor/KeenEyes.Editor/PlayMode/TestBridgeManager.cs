using KeenEyes.Input.Abstractions;
using KeenEyes.Logging;
using KeenEyes.TestBridge;
using KeenEyes.TestBridge.Ipc;

namespace KeenEyes.Editor.PlayMode;

/// <summary>
/// Manages the TestBridge IPC server for external tool connections.
/// </summary>
/// <remarks>
/// <para>
/// The TestBridge allows external tools (such as the MCP server) to connect to the editor
/// and interact with the scene world for automated testing and debugging.
/// </para>
/// <para>
/// When enabled, the manager installs a <see cref="TestBridgePlugin"/> on the scene world
/// and starts an <see cref="IpcBridgeServer"/> that listens for external connections.
/// </para>
/// </remarks>
public sealed class TestBridgeManager : IAsyncDisposable
{
    private readonly World sceneWorld;
    private readonly TestBridgeOptions options;
    private TestBridgePlugin? plugin;
    private IpcBridgeServer? server;
    private bool disposed;

    /// <summary>
    /// Default named pipe name for the editor's TestBridge.
    /// </summary>
    public const string DefaultPipeName = "KeenEyes.Editor.TestBridge";

    /// <summary>
    /// Creates a new TestBridge manager for the specified scene world.
    /// </summary>
    /// <param name="sceneWorld">The scene world to bridge.</param>
    /// <param name="pipeName">The named pipe name for IPC connections. If null, uses <see cref="DefaultPipeName"/>.</param>
    /// <param name="logQueryable">Optional log queryable provider for log browsing via MCP.</param>
    /// <param name="realInputContext">
    /// Optional real hardware input context to merge with virtual input.
    /// When provided, enables hybrid input mode where both real hardware input
    /// and virtual TestBridge-injected input work together.
    /// </param>
    public TestBridgeManager(
        World sceneWorld,
        string? pipeName = null,
        ILogQueryable? logQueryable = null,
        IInputContext? realInputContext = null)
    {
        ArgumentNullException.ThrowIfNull(sceneWorld);

        this.sceneWorld = sceneWorld;
        options = new TestBridgeOptions
        {
            EnableIpc = true,
            EnableCapture = true,
            LogQueryable = logQueryable,
            RealInputContext = realInputContext,
            IpcOptions = new IpcOptions
            {
                PipeName = pipeName ?? DefaultPipeName,
                TransportMode = IpcTransportMode.NamedPipe
            }
        };
    }

    /// <summary>
    /// Gets whether the IPC server is currently running.
    /// </summary>
    public bool IsRunning => server?.IsListening == true;

    /// <summary>
    /// Gets whether a client is currently connected.
    /// </summary>
    public bool IsConnected => server?.IsConnected == true;

    /// <summary>
    /// Gets the named pipe name being used for IPC.
    /// </summary>
    public string PipeName => options.IpcOptions.PipeName;

    /// <summary>
    /// Raised when the server connection state changes.
    /// </summary>
    public event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Starts the TestBridge IPC server.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the server starts listening.</returns>
    /// <exception cref="InvalidOperationException">Thrown if already running.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (IsRunning)
        {
            throw new InvalidOperationException("TestBridge server is already running.");
        }

        // Install the plugin on the scene world
        plugin = new TestBridgePlugin(options);
        sceneWorld.InstallPlugin(plugin);

        // Get the bridge from the world extension
        var bridge = sceneWorld.GetExtension<ITestBridge>();

        // Create and start the IPC server
        server = new IpcBridgeServer(bridge, options.IpcOptions);
        server.ConnectionChanged += OnConnectionChanged;

        await server.StartAsync(cancellationToken);

        Console.WriteLine($"[TestBridge] IPC server started on pipe: {PipeName}");
    }

    /// <summary>
    /// Stops the TestBridge IPC server.
    /// </summary>
    /// <returns>A task that completes when the server has stopped.</returns>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        // Stop and dispose the IPC server
        if (server != null)
        {
            server.ConnectionChanged -= OnConnectionChanged;
            await server.StopAsync();
            server.Dispose();
            server = null;
        }

        // Uninstall the plugin
        if (plugin != null)
        {
            sceneWorld.UninstallPlugin(plugin.Name);
            plugin = null;
        }

        Console.WriteLine("[TestBridge] IPC server stopped");
    }

    /// <summary>
    /// Records frame completion for performance metrics.
    /// </summary>
    /// <remarks>
    /// Call this at the end of each frame to enable frame timing tracking.
    /// </remarks>
    public void OnFrameComplete()
    {
        plugin?.OnFrameComplete();
    }

    private void OnConnectionChanged(bool isConnected)
    {
        if (isConnected)
        {
            Console.WriteLine("[TestBridge] Client connected");
        }
        else
        {
            Console.WriteLine("[TestBridge] Client disconnected");
        }

        ConnectionChanged?.Invoke(isConnected);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        await StopAsync();
    }
}
