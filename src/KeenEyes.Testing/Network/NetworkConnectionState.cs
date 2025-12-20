namespace KeenEyes.Testing.Network;

/// <summary>
/// Represents the current state of a network connection.
/// </summary>
public enum NetworkConnectionState
{
    /// <summary>
    /// Not connected to any server.
    /// </summary>
    Disconnected,

    /// <summary>
    /// Currently attempting to establish a connection.
    /// </summary>
    Connecting,

    /// <summary>
    /// Successfully connected to a server.
    /// </summary>
    Connected,

    /// <summary>
    /// Currently disconnecting from the server.
    /// </summary>
    Disconnecting
}
