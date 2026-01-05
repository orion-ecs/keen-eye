using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component added to a canvas entity when hovering over a port.
/// </summary>
/// <remarks>
/// <para>
/// Only one port can be hovered at a time. This component is added to the canvas
/// (not the node) to avoid querying all nodes to find the hovered port.
/// </para>
/// <para>
/// The render system reads this to draw port highlights. The input system
/// updates this component as the mouse moves over different ports.
/// </para>
/// </remarks>
public struct HoveredPort : IComponent
{
    /// <summary>
    /// The node containing the hovered port.
    /// </summary>
    public Entity Node;

    /// <summary>
    /// The port direction (Input or Output).
    /// </summary>
    public PortDirection Direction;

    /// <summary>
    /// The port index within the direction group.
    /// </summary>
    public int PortIndex;

    /// <summary>
    /// The type of the hovered port.
    /// </summary>
    public PortTypeId TypeId;

    /// <summary>
    /// Canvas position of the port center.
    /// </summary>
    public Vector2 Position;
}
