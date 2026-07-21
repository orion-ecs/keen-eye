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

    /// <inheritdoc />
    public abstract int TypeId { get; }
    /// <inheritdoc />
    public abstract string Name { get; }
    /// <inheritdoc />
    public string Category => "KESL/Math";
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => BinaryInputs;
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => BinaryOutputs;
    /// <inheritdoc />
    public bool IsCollapsible => true;
    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }
    /// <inheritdoc />
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

    /// <inheritdoc />
    public abstract int TypeId { get; }
    /// <inheritdoc />
    public abstract string Name { get; }
    /// <inheritdoc />
    public string Category => "KESL/Math";
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => UnaryInputs;
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => UnaryOutputs;
    /// <inheritdoc />
    public bool IsCollapsible => true;
    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }
    /// <inheritdoc />
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

    /// <inheritdoc />
    public abstract int TypeId { get; }
    /// <inheritdoc />
    public abstract string Name { get; }
    /// <inheritdoc />
    public string Category => "KESL/Math";
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => TernaryInputs;
    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => TernaryOutputs;
    /// <inheritdoc />
    public bool IsCollapsible => true;
    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }
    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}
