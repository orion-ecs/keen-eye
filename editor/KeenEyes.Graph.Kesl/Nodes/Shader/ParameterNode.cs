using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Shader;

/// <summary>
/// Node that defines a uniform shader parameter.
/// </summary>
/// <remarks>
/// <para>
/// Parameter nodes define uniform inputs that can be set at runtime. They map to
/// the KESL params block. The parameter name and type are configured via the
/// node's body UI.
/// </para>
/// </remarks>
public sealed class ParameterNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Parameter;

    /// <inheritdoc />
    public string Name => "Parameter";

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
        world.Add(node, ParameterNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Parameter name and type selector would be rendered here
        return 50f;
    }
}
