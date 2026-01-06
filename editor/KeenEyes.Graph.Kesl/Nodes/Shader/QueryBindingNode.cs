using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Node that binds an ECS component for shader access.
/// </summary>
/// <remarks>
/// <para>
/// QueryBinding nodes expose ECS component fields as outputs in the shader graph.
/// They map to the KESL query block. Connected fields can be read and optionally
/// written by the shader.
/// </para>
/// <para>
/// The component type and binding name are configured via the node's body UI.
/// Output ports are dynamically generated based on the selected component's fields.
/// </para>
/// </remarks>
public sealed class QueryBindingNode : INodeTypeDefinition
{
    // Note: In a full implementation, output ports would be dynamic based on the
    // selected component type. For now, we use static placeholder ports.
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("X", PortTypeId.Float, 60f),
        PortDefinition.Output("Y", PortTypeId.Float, 85f),
        PortDefinition.Output("Z", PortTypeId.Float, 110f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.QueryBinding;

    /// <inheritdoc />
    public string Name => "Query Binding";

    /// <inheritdoc />
    public string Category => "KESL/Shader";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, QueryBindingNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Component selector and binding name would be rendered here
        // when text rendering and dropdowns are available
        return 50f;
    }
}
