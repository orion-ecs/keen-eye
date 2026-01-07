namespace KeenEyes.TestBridge.Ipc.Protocol;

/// <summary>
/// Type of IPC message.
/// </summary>
public enum MessageType : byte
{
    /// <summary>
    /// JSON-encoded request or response.
    /// </summary>
    Json = 0x01,

    /// <summary>
    /// Raw binary data (for screenshots).
    /// </summary>
    Binary = 0x02,

    /// <summary>
    /// Keep-alive ping.
    /// </summary>
    Ping = 0x03,

    /// <summary>
    /// Keep-alive pong response.
    /// </summary>
    Pong = 0x04
}
