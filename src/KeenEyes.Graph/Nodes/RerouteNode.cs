using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Nodes;

/// <summary>
/// A minimal reroute node for routing connections.
/// </summary>
/// <remarks>
/// <para>
/// Reroute nodes are pass-through nodes that allow connections to be visually
/// organized without affecting data flow. They have one input and one output
/// port of type Any, which allows them to connect to any other port type.
/// </para>
/// <para>
/// Reroute nodes render at a minimal size (just showing the connection points)
/// and are not collapsible.
/// </para>
/// </remarks>
public sealed class RerouteNode : INodeTypeDefinition
{
    /// <inheritdoc />
    public int TypeId => BuiltInNodeIds.Reroute;

    /// <inheritdoc />
    public string Name => "Reroute";

    /// <inheritdoc />
    public string Category => "Utility";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts { get; } =
    [
        PortDefinition.Input("In", PortTypeId.Any, GraphLayoutSystem.HeaderHeight / 2f)
    ];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts { get; } =
    [
        PortDefinition.Output("Out", PortTypeId.Any, GraphLayoutSystem.HeaderHeight / 2f)
    ];

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        // Reroute nodes have no custom state - they just pass through connections
        // Optionally, we could store the inferred type when connections are made

        // Set a smaller width for reroute nodes
        ref var nodeData = ref world.Get<GraphNode>(node);
        nodeData.Width = 60f; // Minimal width for reroute
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Reroute nodes have no body content - they're just connection pass-throughs
        return 0f;
    }
}
