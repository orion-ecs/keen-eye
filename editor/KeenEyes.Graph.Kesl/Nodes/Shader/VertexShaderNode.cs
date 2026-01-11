using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Root node for a KESL vertex shader graph.
/// </summary>
/// <remarks>
/// <para>
/// The VertexShader node is the entry point for a KESL vertex shader graph. It defines
/// the shader name and connects to InputAttribute nodes (for vertex inputs),
/// OutputAttribute nodes (for outputs to fragment shader), Parameter nodes (for uniforms),
/// and the execution flow.
/// </para>
/// <para>
/// Every valid KESL vertex shader graph must have exactly one VertexShader node.
/// </para>
/// </remarks>
public sealed class VertexShaderNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.VertexShader;

    /// <inheritdoc />
    public string Name => "Vertex Shader";

    /// <inheritdoc />
    public string Category => "KESL/Shader";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => [];

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, VertexShaderNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // The shader name would be rendered here with NodeWidgets.TextField
        // when text rendering is available
        return 25f;
    }
}
