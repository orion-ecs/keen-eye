using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Node that defines a shader output attribute.
/// </summary>
/// <remarks>
/// <para>
/// OutputAttribute nodes define shader outputs. For vertex shaders, these are passed to
/// fragment shaders as interpolated values (KESL "out" block). For fragment shaders,
/// these define render target outputs with location bindings.
/// </para>
/// <para>
/// The attribute name, type, and location are configured via the node's body UI.
/// The input port receives the value to output.
/// </para>
/// </remarks>
public sealed class OutputAttributeNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Value", PortTypeId.Float4, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.OutputAttribute;

    /// <inheritdoc />
    public string Name => "Output Attribute";

    /// <inheritdoc />
    public string Category => "KESL/Shader";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => [];

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, OutputAttributeNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Attribute name, type selector, and location would be rendered here
        return 70f;
    }
}
