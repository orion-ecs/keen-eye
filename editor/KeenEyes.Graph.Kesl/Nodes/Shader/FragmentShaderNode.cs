using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Root node for a KESL fragment shader graph.
/// </summary>
/// <remarks>
/// <para>
/// The FragmentShader node is the entry point for a KESL fragment shader graph. It defines
/// the shader name and connects to InputAttribute nodes (for inputs from vertex shader),
/// OutputAttribute nodes (for render target outputs), Parameter nodes (for uniforms),
/// and the execution flow.
/// </para>
/// <para>
/// Every valid KESL fragment shader graph must have exactly one FragmentShader node.
/// </para>
/// </remarks>
public sealed class FragmentShaderNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.FragmentShader;

    /// <inheritdoc />
    public string Name => "Fragment Shader";

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
        world.Add(node, FragmentShaderNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // The shader name would be rendered here with NodeWidgets.TextField
        // when text rendering is available
        return 25f;
    }
}
