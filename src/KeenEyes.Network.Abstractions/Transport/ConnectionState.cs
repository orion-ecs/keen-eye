namespace KeenEyes.Network.Transport;

/// <summary>
/// Represents the state of a network connection.
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Not connected to any endpoint.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    /// Attempting to establish a connection.
    /// </summary>
    Connecting = 1,

    /// <summary>
    /// Connection established and ready for data transfer.
    /// </summary>
    Connected = 2,

    /// <summary>
    /// Connection is being gracefully closed.
    /// </summary>
    Disconnecting = 3,
}
