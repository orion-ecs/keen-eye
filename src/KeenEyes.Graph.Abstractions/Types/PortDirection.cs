namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Specifies the direction of a graph port.
/// </summary>
public enum PortDirection
{
    /// <summary>Port receives data from connections.</summary>
    Input,

    /// <summary>Port sends data to connections.</summary>
    Output
}
