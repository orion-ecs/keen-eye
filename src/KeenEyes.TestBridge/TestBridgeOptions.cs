using KeenEyes.Input.Abstractions;
using KeenEyes.Logging;

namespace KeenEyes.TestBridge;

/// <summary>
/// Configuration options for the test bridge.
/// </summary>
public sealed record TestBridgeOptions
{
    /// <summary>
    /// Gets or sets whether to enable IPC mode for external test process connections.
    /// </summary>
    /// <remarks>
    /// When enabled, the bridge starts listening for external connections via the
    /// configured transport (named pipes or TCP).
    /// </remarks>
    public bool EnableIpc { get; init; } = false;

    /// <summary>
    /// Gets or sets the IPC configuration options.
    /// </summary>
    public IpcOptions IpcOptions { get; init; } = new();

    /// <summary>
    /// Gets or sets whether to enable screenshot/frame capture.
    /// </summary>
    /// <remarks>
    /// Capture requires a graphics context. If disabled or no graphics context is available,
    /// capture operations will throw <see cref="InvalidOperationException"/>.
    /// </remarks>
    public bool EnableCapture { get; init; } = true;

    /// <summary>
    /// Gets or sets the number of gamepad slots to create.
    /// </summary>
    public int GamepadCount { get; init; } = 4;

    /// <summary>
    /// Gets or sets a custom input context to use.
    /// </summary>
    /// <remarks>
    /// If null, a new <see cref="Testing.Input.MockInputContext"/> is created.
    /// This allows sharing an input context with other test infrastructure.
    /// </remarks>
    public IInputContext? CustomInputContext { get; init; }

    /// <summary>
    /// Gets or sets the log queryable provider for log browsing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If null, the bridge will attempt to find an <see cref="ILogQueryable"/>
    /// provider from an installed logging plugin's LogManager.
    /// </para>
    /// <para>
    /// Provide a queryable log provider (such as RingBufferLogProvider) for log querying support.
    /// </para>
    /// </remarks>
    public ILogQueryable? LogQueryable { get; init; }

    /// <summary>
    /// Gets or sets the real hardware input context to merge with virtual input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When set, the TestBridge creates a composite input context that merges
    /// both real hardware input and virtual TestBridge-injected input. This enables
    /// hybrid testing scenarios where both can work together.
    /// </para>
    /// <para>
    /// If null, only virtual input is available (useful for headless testing).
    /// </para>
    /// </remarks>
    public IInputContext? RealInputContext { get; init; }
}

/// <summary>
/// IPC-specific configuration options.
/// </summary>
public sealed record IpcOptions
{
    /// <summary>
    /// Gets or sets the transport mode for IPC.
    /// </summary>
    public IpcTransportMode TransportMode { get; init; } = IpcTransportMode.NamedPipe;

    /// <summary>
    /// Gets or sets the named pipe name (for NamedPipe mode).
    /// </summary>
    public string PipeName { get; init; } = "KeenEyes.TestBridge";

    /// <summary>
    /// Gets or sets the TCP port (for TCP mode).
    /// </summary>
    public int TcpPort { get; init; } = 19283;

    /// <summary>
    /// Gets or sets the TCP bind address (for TCP mode).
    /// </summary>
    public string TcpBindAddress { get; init; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the connection timeout.
    /// </summary>
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Transport mode for IPC communication.
/// </summary>
public enum IpcTransportMode
{
    /// <summary>
    /// Use named pipes for local IPC.
    /// </summary>
    NamedPipe,

    /// <summary>
    /// Use TCP for network IPC.
    /// </summary>
    Tcp
}
