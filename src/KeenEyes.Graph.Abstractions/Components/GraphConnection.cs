namespace KeenEyes.Graph.Abstractions;

/// <summary>
/// Component for a connection between two graph nodes.
/// </summary>
/// <remarks>
/// <para>
/// Connections link an output port on one node to an input port on another.
/// The connection entity is a child of the canvas and rendered as a line
/// (or bezier curve in later phases).
/// </para>
/// <para>
/// Port indices reference the port definitions in the port registry for each
/// node's type.
/// </para>
/// </remarks>
public struct GraphConnection : IComponent
{
    /// <summary>
    /// The source node entity (has the output port).
    /// </summary>
    public Entity SourceNode;

    /// <summary>
    /// Index of the output port on the source node.
    /// </summary>
    public int SourcePortIndex;

    /// <summary>
    /// The target node entity (has the input port).
    /// </summary>
    public Entity TargetNode;

    /// <summary>
    /// Index of the input port on the target node.
    /// </summary>
    public int TargetPortIndex;

    /// <summary>
    /// Reference to the parent canvas entity.
    /// </summary>
    public Entity Canvas;

    /// <summary>
    /// Creates a connection between two nodes.
    /// </summary>
    /// <param name="sourceNode">The source node entity.</param>
    /// <param name="sourcePort">The output port index on the source.</param>
    /// <param name="targetNode">The target node entity.</param>
    /// <param name="targetPort">The input port index on the target.</param>
    /// <param name="canvas">The parent canvas entity.</param>
    /// <returns>A new connection component.</returns>
    public static GraphConnection Create(
        Entity sourceNode,
        int sourcePort,
        Entity targetNode,
        int targetPort,
        Entity canvas)
    {
        return new()
        {
            SourceNode = sourceNode,
            SourcePortIndex = sourcePort,
            TargetNode = targetNode,
            TargetPortIndex = targetPort,
            Canvas = canvas
        };
    }
}
