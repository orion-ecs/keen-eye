namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component storing the state of a group node (subgraph container).
/// </summary>
/// <remarks>
/// <para>
/// Group nodes contain an internal canvas with child nodes. Interface ports
/// expose internal connections to the outer graph, allowing the group to
/// act as a single node while encapsulating complex subgraphs.
/// </para>
/// <para>
/// Double-click on a group node to enter and edit the internal canvas.
/// </para>
/// </remarks>
public struct GroupNodeData : IComponent
{
    /// <summary>
    /// The internal canvas entity containing child nodes.
    /// </summary>
    /// <remarks>
    /// The internal canvas has its own GraphCanvas component and can contain
    /// any nodes, including nested groups.
    /// </remarks>
    public Entity InternalCanvas;

    /// <summary>
    /// Interface ports exposed on the left (input) side of the group.
    /// </summary>
    public List<InterfacePort> InterfaceInputs;

    /// <summary>
    /// Interface ports exposed on the right (output) side of the group.
    /// </summary>
    public List<InterfacePort> InterfaceOutputs;

    /// <summary>
    /// Whether the group is currently being edited (viewing internal canvas).
    /// </summary>
    public bool IsEditing;

    /// <summary>
    /// Creates default group node data with empty collections.
    /// </summary>
    public static GroupNodeData Default => new()
    {
        InternalCanvas = Entity.Null,
        InterfaceInputs = [],
        InterfaceOutputs = [],
        IsEditing = false
    };
}

/// <summary>
/// Defines an interface port that bridges internal and external connections.
/// </summary>
/// <remarks>
/// <para>
/// Interface ports are created when a node inside the group needs to connect
/// to nodes outside. They appear on the group node's boundary and forward
/// connections to the appropriate internal node and port.
/// </para>
/// </remarks>
public readonly record struct InterfacePort
{
    /// <summary>
    /// The display name of the interface port.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The data type of the interface port.
    /// </summary>
    public required PortTypeId TypeId { get; init; }

    /// <summary>
    /// The internal node this port connects to.
    /// </summary>
    public required Entity InternalNode { get; init; }

    /// <summary>
    /// The port index on the internal node.
    /// </summary>
    public required int InternalPortIndex { get; init; }
}
