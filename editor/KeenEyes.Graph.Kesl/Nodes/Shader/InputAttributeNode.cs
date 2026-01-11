using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Node that defines a vertex shader input attribute.
/// </summary>
/// <remarks>
/// <para>
/// InputAttribute nodes define vertex inputs that come from vertex buffers. They map to
/// the KESL "in" block in vertex shaders. Each input has a name, type, and location binding.
/// </para>
/// <para>
/// The attribute name, type, and location are configured via the node's body UI.
/// The output port provides the attribute value for use in shader calculations.
/// </para>
/// </remarks>
public sealed class InputAttributeNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float3, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.InputAttribute;

    /// <inheritdoc />
    public string Name => "Input Attribute";

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
        world.Add(node, InputAttributeNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Attribute name, type selector, and location would be rendered here
        return 70f;
    }
}
