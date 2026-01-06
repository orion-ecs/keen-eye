using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Root node for a KESL compute shader graph.
/// </summary>
/// <remarks>
/// <para>
/// The ComputeShader node is the entry point for a KESL shader graph. It defines
/// the shader name and connects to QueryBinding nodes (for ECS component access),
/// Parameter nodes (for uniforms), and the execution flow.
/// </para>
/// <para>
/// Every valid KESL graph must have exactly one ComputeShader node.
/// </para>
/// </remarks>
public sealed class ComputeShaderNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.ComputeShader;

    /// <inheritdoc />
    public string Name => "Compute Shader";

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
        world.Add(node, ComputeShaderNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // The shader name would be rendered here with NodeWidgets.TextField
        // when text rendering is available
        return 25f;
    }
}
