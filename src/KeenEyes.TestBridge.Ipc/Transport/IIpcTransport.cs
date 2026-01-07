namespace KeenEyes.TestBridge.Ipc.Transport;

/// <summary>
/// Transport abstraction for IPC communication between test processes and game applications.
/// </summary>
/// <remarks>
/// <para>
/// Unlike <c>INetworkTransport</c>, this interface is designed for point-to-point IPC
/// with reliable ordered delivery. It provides a simpler API suitable for test automation.
/// </para>
/// <para>
/// Implementations include named pipes (cross-platform) and TCP (for remote debugging).
/// </para>
/// </remarks>
public interface IIpcTransport : IDisposable
{
    /// <summary>
    /// Gets whether the transport is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Raised when the connection state changes.
    /// </summary>
    /// <remarks>
    /// The boolean parameter indicates whether the transport is now connected (true) or disconnected (false).
    /// </remarks>
    event Action<bool>? ConnectionChanged;

    /// <summary>
    /// Raised when a complete message is received.
    /// </summary>
    /// <remarks>
    /// The memory contains the raw message bytes (after frame header has been stripped).
    /// The memory is only valid during the event callback; callers must copy if persistence is needed.
    /// </remarks>
    event Action<ReadOnlyMemory<byte>>? MessageReceived;

    /// <summary>
    /// Starts listening for a connection (server mode).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when a client connects.</returns>
    /// <exception cref="InvalidOperationException">Thrown if already listening or connected.</exception>
    Task ListenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a server (client mode).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when connected.</returns>
    /// <exception cref="InvalidOperationException">Thrown if already connected.</exception>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message to the connected peer.
    /// </summary>
    /// <param name="data">The message data to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the message has been sent.</returns>
    /// <exception cref="InvalidOperationException">Thrown if not connected.</exception>
    ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects gracefully from the peer.
    /// </summary>
    /// <returns>A task that completes when disconnected.</returns>
    Task DisconnectAsync();
}
