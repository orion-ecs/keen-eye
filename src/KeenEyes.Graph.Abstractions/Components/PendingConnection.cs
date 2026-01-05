using System.Numerics;

namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component added to a canvas entity during connection creation drag.
/// </summary>
/// <remarks>
/// <para>
/// When a user begins dragging from a port to create a connection, this component
/// is added to the canvas to track the in-progress connection state. The render
/// system reads this to draw the connection preview.
/// </para>
/// <para>
/// The component is removed when the connection is completed or cancelled.
/// </para>
/// </remarks>
public struct PendingConnection : IComponent
{
    /// <summary>
    /// The node entity where the drag started.
    /// </summary>
    public Entity SourceNode;

    /// <summary>
    /// Index of the source port on the source node.
    /// </summary>
    public int SourcePortIndex;

    /// <summary>
    /// Whether dragging from an output port (true) or input port (false).
    /// </summary>
    public bool IsFromOutput;

    /// <summary>
    /// Current mouse position in canvas coordinates.
    /// </summary>
    public Vector2 CurrentPosition;

    /// <summary>
    /// The port type being dragged from.
    /// </summary>
    public PortTypeId SourceType;
}
