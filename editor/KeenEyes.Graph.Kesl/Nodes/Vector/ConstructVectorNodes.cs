using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Vector;

/// <summary>Construct a Vector2 from X, Y components.</summary>
public sealed class ConstructVector2Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("X", PortTypeId.Float, 60f),
        PortDefinition.Input("Y", PortTypeId.Float, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Vector", PortTypeId.Float2, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.ConstructVector2;

    /// <inheritdoc />
    public string Name => "Vector2";

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

/// <summary>Construct a Vector3 from X, Y, Z components.</summary>
public sealed class ConstructVector3Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("X", PortTypeId.Float, 60f),
        PortDefinition.Input("Y", PortTypeId.Float, 85f),
        PortDefinition.Input("Z", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Vector", PortTypeId.Float3, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.ConstructVector3;

    /// <inheritdoc />
    public string Name => "Vector3";

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

/// <summary>Construct a Vector4 from X, Y, Z, W components.</summary>
public sealed class ConstructVector4Node : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("X", PortTypeId.Float, 60f),
        PortDefinition.Input("Y", PortTypeId.Float, 85f),
        PortDefinition.Input("Z", PortTypeId.Float, 110f),
        PortDefinition.Input("W", PortTypeId.Float, 135f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Vector", PortTypeId.Float4, 97.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.ConstructVector4;

    /// <inheritdoc />
    public string Name => "Vector4";

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
