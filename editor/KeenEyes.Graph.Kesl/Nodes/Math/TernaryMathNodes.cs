using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Math;

/// <summary>Clamp value between min and max.</summary>
public sealed class ClampNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Value", PortTypeId.Float, 60f),
        PortDefinition.Input("Min", PortTypeId.Float, 85f),
        PortDefinition.Input("Max", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Clamp;

    /// <inheritdoc />
    public string Name => "Clamp";

    /// <inheritdoc />
    public string Category => "KESL/Math";

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

/// <summary>Linear interpolation between two values.</summary>
public sealed class LerpNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 85f),
        PortDefinition.Input("T", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Lerp;

    /// <inheritdoc />
    public string Name => "Lerp";

    /// <inheritdoc />
    public string Category => "KESL/Math";

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

/// <summary>Smooth Hermite interpolation.</summary>
public sealed class SmoothstepNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Edge0", PortTypeId.Float, 60f),
        PortDefinition.Input("Edge1", PortTypeId.Float, 85f),
        PortDefinition.Input("X", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Smoothstep;

    /// <inheritdoc />
    public string Name => "Smoothstep";

    /// <inheritdoc />
    public string Category => "KESL/Math";

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
