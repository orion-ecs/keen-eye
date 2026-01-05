using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Defines a port on a graph node type.
/// </summary>
/// <remarks>
/// <para>
/// Ports are not entities - they are defined in a registry and referenced by index.
/// Each node type has a fixed set of port definitions that describe the available
/// inputs and outputs.
/// </para>
/// <para>
/// The <see cref="LocalOffset"/> is relative to the node's bounds. Input ports
/// are typically positioned on the left edge, output ports on the right.
/// </para>
/// </remarks>
/// <param name="Name">The display name of the port.</param>
/// <param name="Direction">Whether this port receives or sends data.</param>
/// <param name="TypeId">The data type of the port.</param>
/// <param name="LocalOffset">Position offset relative to the node origin.</param>
/// <param name="AllowMultiple">Whether multiple connections are allowed (inputs only).</param>
public readonly record struct PortDefinition(
    string Name,
    PortDirection Direction,
    PortTypeId TypeId,
    Vector2 LocalOffset,
    bool AllowMultiple = false
)
{
    /// <summary>
    /// Creates an input port definition.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="typeId">The data type.</param>
    /// <param name="yOffset">The vertical offset from node top.</param>
    /// <param name="allowMultiple">Whether multiple connections are allowed.</param>
    /// <returns>A new port definition.</returns>
    public static PortDefinition Input(string name, PortTypeId typeId, float yOffset, bool allowMultiple = false)
        => new(name, PortDirection.Input, typeId, new Vector2(0, yOffset), allowMultiple);

    /// <summary>
    /// Creates an output port definition.
    /// </summary>
    /// <param name="name">The display name.</param>
    /// <param name="typeId">The data type.</param>
    /// <param name="yOffset">The vertical offset from node top.</param>
    /// <param name="nodeWidth">The node width (for right-edge positioning).</param>
    /// <returns>A new port definition.</returns>
    public static PortDefinition Output(string name, PortTypeId typeId, float yOffset, float nodeWidth = 200f)
        => new(name, PortDirection.Output, typeId, new Vector2(nodeWidth, yOffset), false);
}
