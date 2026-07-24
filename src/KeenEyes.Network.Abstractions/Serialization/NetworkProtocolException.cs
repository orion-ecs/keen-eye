namespace KeenEyes.Network.Serialization;

/// <summary>
/// Thrown when network data cannot be deserialized because it is truncated,
/// malformed, or otherwise violates the wire protocol.
/// </summary>
/// <remarks>
/// This is a defined, catchable failure mode for untrusted input. Deserialization
/// paths throw this instead of leaking low-level exceptions such as
/// <see cref="IndexOutOfRangeException"/>, so callers draining packets can catch a
/// single type and drop the offending message.
/// </remarks>
public sealed class NetworkProtocolException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkProtocolException"/> class.
    /// </summary>
    public NetworkProtocolException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkProtocolException"/> class
    /// with a message describing the protocol violation.
    /// </summary>
    /// <param name="message">The message describing the protocol violation.</param>
    public NetworkProtocolException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkProtocolException"/> class
    /// with a message and the underlying cause.
    /// </summary>
    /// <param name="message">The message describing the protocol violation.</param>
    /// <param name="innerException">The exception that caused this protocol failure.</param>
    public NetworkProtocolException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
