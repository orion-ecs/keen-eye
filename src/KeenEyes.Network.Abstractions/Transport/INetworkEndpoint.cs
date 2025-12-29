namespace KeenEyes.Network.Transport;

/// <summary>
/// Represents a network endpoint (address and port).
/// </summary>
/// <remarks>
/// A transport-agnostic way to identify network endpoints.
/// </remarks>
public readonly record struct NetworkEndpoint
{
    /// <summary>
    /// Gets the address (IP, hostname, or transport-specific identifier).
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Gets the port number.
    /// </summary>
    public required int Port { get; init; }

    /// <summary>
    /// Creates a localhost endpoint on the specified port.
    /// </summary>
    /// <param name="port">The port number.</param>
    public static NetworkEndpoint Localhost(int port) =>
        new() { Address = "127.0.0.1", Port = port };

    /// <summary>
    /// Creates an any-address endpoint for listening on all interfaces.
    /// </summary>
    /// <param name="port">The port number.</param>
    public static NetworkEndpoint Any(int port) =>
        new() { Address = "0.0.0.0", Port = port };

    /// <inheritdoc/>
    public override string ToString() => $"{Address}:{Port}";

    /// <summary>
    /// Parses an endpoint from a string in "address:port" format.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <returns>The parsed endpoint.</returns>
    /// <exception cref="FormatException">Thrown if the format is invalid.</exception>
    public static NetworkEndpoint Parse(string value)
    {
        var colonIndex = value.LastIndexOf(':');
        if (colonIndex < 0)
        {
            throw new FormatException($"Invalid endpoint format: {value}. Expected 'address:port'.");
        }

        var address = value[..colonIndex];
        var portStr = value[(colonIndex + 1)..];

        if (!int.TryParse(portStr, out var port) || port < 0 || port > 65535)
        {
            throw new FormatException($"Invalid port: {portStr}. Must be 0-65535.");
        }

        return new NetworkEndpoint { Address = address, Port = port };
    }

    /// <summary>
    /// Tries to parse an endpoint from a string.
    /// </summary>
    /// <param name="value">The string to parse.</param>
    /// <param name="endpoint">The parsed endpoint if successful.</param>
    /// <returns>True if parsing succeeded; false otherwise.</returns>
    public static bool TryParse(string value, out NetworkEndpoint endpoint)
    {
        try
        {
            endpoint = Parse(value);
            return true;
        }
        catch
        {
            endpoint = default;
            return false;
        }
    }
}
