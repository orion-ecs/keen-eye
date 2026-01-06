using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Vector;

/// <summary>Base class for unary vector operations.</summary>
public abstract class UnaryVectorNodeBase : INodeTypeDefinition
{
    private static readonly PortDefinition[] UnaryInputs =
    [
        PortDefinition.Input("Vector", PortTypeId.Float3, 60f)
    ];

    public abstract int TypeId { get; }
    public abstract string Name { get; }
    public string Category => "KESL/Vector";
    public IReadOnlyList<PortDefinition> InputPorts => UnaryInputs;
    public abstract IReadOnlyList<PortDefinition> OutputPorts { get; }
    public bool IsCollapsible => true;
    public void Initialize(Entity node, IWorld world) { }
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>Normalize a vector to unit length.</summary>
public sealed class NormalizeNode : UnaryVectorNodeBase
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float3, 60f)
    ];

    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Normalize;

    /// <inheritdoc />
    public override string Name => "Normalize";

    /// <inheritdoc />
    public override IReadOnlyList<PortDefinition> OutputPorts => Outputs;
}

/// <summary>Length (magnitude) of a vector.</summary>
public sealed class LengthNode : UnaryVectorNodeBase
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Length", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.Length;

    /// <inheritdoc />
    public override string Name => "Length";

    /// <inheritdoc />
    public override IReadOnlyList<PortDefinition> OutputPorts => Outputs;
}

/// <summary>Squared length of a vector (faster than Length).</summary>
public sealed class LengthSquaredNode : UnaryVectorNodeBase
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("LengthSq", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public override int TypeId => KeslNodeIds.LengthSquared;

    /// <inheritdoc />
    public override string Name => "Length Squared";

    /// <inheritdoc />
    public override IReadOnlyList<PortDefinition> OutputPorts => Outputs;
}

/// <summary>Dot product of two vectors.</summary>
public sealed class DotProductNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float3, 60f),
        PortDefinition.Input("B", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Dot", PortTypeId.Float, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.DotProduct;

    /// <inheritdoc />
    public string Name => "Dot Product";

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

/// <summary>Cross product of two Vector3s.</summary>
public sealed class CrossProductNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float3, 60f),
        PortDefinition.Input("B", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Cross", PortTypeId.Float3, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.CrossProduct;

    /// <inheritdoc />
    public string Name => "Cross Product";

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

/// <summary>Distance between two points.</summary>
public sealed class DistanceNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float3, 60f),
        PortDefinition.Input("B", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Distance", PortTypeId.Float, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Distance;

    /// <inheritdoc />
    public string Name => "Distance";

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

/// <summary>Squared distance between two points (faster than Distance).</summary>
public sealed class DistanceSquaredNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float3, 60f),
        PortDefinition.Input("B", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("DistanceSq", PortTypeId.Float, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.DistanceSquared;

    /// <inheritdoc />
    public string Name => "Distance Squared";

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

/// <summary>Reflect a vector about a normal.</summary>
public sealed class ReflectNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Incident", PortTypeId.Float3, 60f),
        PortDefinition.Input("Normal", PortTypeId.Float3, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Reflected", PortTypeId.Float3, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Reflect;

    /// <inheritdoc />
    public string Name => "Reflect";

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
