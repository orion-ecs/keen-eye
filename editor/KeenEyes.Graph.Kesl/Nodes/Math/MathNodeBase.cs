using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Math;

/// <summary>
/// Base class for binary math operations (A op B = Result).
/// </summary>
public abstract class BinaryMathNodeBase : INodeTypeDefinition
{
    private static readonly PortDefinition[] BinaryInputs =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 85f)
    ];

    private static readonly PortDefinition[] BinaryOutputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 72.5f)
    ];

    public abstract int TypeId { get; }
    public abstract string Name { get; }
    public string Category => "KESL/Math";
    public IReadOnlyList<PortDefinition> InputPorts => BinaryInputs;
    public IReadOnlyList<PortDefinition> OutputPorts => BinaryOutputs;
    public bool IsCollapsible => true;
    public void Initialize(Entity node, IWorld world) { }
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>
/// Base class for unary math operations (f(X) = Result).
/// </summary>
public abstract class UnaryMathNodeBase : INodeTypeDefinition
{
    private static readonly PortDefinition[] UnaryInputs =
    [
        PortDefinition.Input("X", PortTypeId.Float, 60f)
    ];

    private static readonly PortDefinition[] UnaryOutputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 60f)
    ];

    public abstract int TypeId { get; }
    public abstract string Name { get; }
    public string Category => "KESL/Math";
    public IReadOnlyList<PortDefinition> InputPorts => UnaryInputs;
    public IReadOnlyList<PortDefinition> OutputPorts => UnaryOutputs;
    public bool IsCollapsible => true;
    public void Initialize(Entity node, IWorld world) { }
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>
/// Base class for ternary math operations (f(A, B, C) = Result).
/// </summary>
public abstract class TernaryMathNodeBase : INodeTypeDefinition
{
    private static readonly PortDefinition[] TernaryInputs =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 85f),
        PortDefinition.Input("C", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] TernaryOutputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 85f)
    ];

    public abstract int TypeId { get; }
    public abstract string Name { get; }
    public string Category => "KESL/Math";
    public IReadOnlyList<PortDefinition> InputPorts => TernaryInputs;
    public IReadOnlyList<PortDefinition> OutputPorts => TernaryOutputs;
    public bool IsCollapsible => true;
    public void Initialize(Entity node, IWorld world) { }
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}
