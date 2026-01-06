using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Vector;

/// <summary>Split a Vector2 into X, Y components.</summary>
public sealed class SplitVector2Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Vector", PortTypeId.Float2, 72.5f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("X", PortTypeId.Float, 60f),
        PortDefinition.Output("Y", PortTypeId.Float, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.SplitVector2;

    /// <inheritdoc />
    public string Name => "Split Vector2";

    /// <inheritdoc />
    public string Category => "KESL/Vector";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>Split a Vector3 into X, Y, Z components.</summary>
public sealed class SplitVector3Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Vector", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("X", PortTypeId.Float, 60f),
        PortDefinition.Output("Y", PortTypeId.Float, 85f),
        PortDefinition.Output("Z", PortTypeId.Float, 110f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.SplitVector3;

    /// <inheritdoc />
    public string Name => "Split Vector3";

    /// <inheritdoc />
    public string Category => "KESL/Vector";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>Split a Vector4 into X, Y, Z, W components.</summary>
public sealed class SplitVector4Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Vector", PortTypeId.Float4, 97.5f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("X", PortTypeId.Float, 60f),
        PortDefinition.Output("Y", PortTypeId.Float, 85f),
        PortDefinition.Output("Z", PortTypeId.Float, 110f),
        PortDefinition.Output("W", PortTypeId.Float, 135f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.SplitVector4;

    /// <inheritdoc />
    public string Name => "Split Vector4";

    /// <inheritdoc />
    public string Category => "KESL/Vector";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}
