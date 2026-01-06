using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Logic;

/// <summary>Logical AND of two boolean values.</summary>
public sealed class AndNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Bool, 60f),
        PortDefinition.Input("B", PortTypeId.Bool, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Bool, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.And;

    /// <inheritdoc />
    public string Name => "And";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

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

/// <summary>Logical OR of two boolean values.</summary>
public sealed class OrNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Bool, 60f),
        PortDefinition.Input("B", PortTypeId.Bool, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Bool, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Or;

    /// <inheritdoc />
    public string Name => "Or";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

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

/// <summary>Logical NOT of a boolean value.</summary>
public sealed class NotNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Value", PortTypeId.Bool, 60f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Bool, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Not;

    /// <inheritdoc />
    public string Name => "Not";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

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

/// <summary>Logical XOR of two boolean values.</summary>
public sealed class XorNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Bool, 60f),
        PortDefinition.Input("B", PortTypeId.Bool, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Bool, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Xor;

    /// <inheritdoc />
    public string Name => "Xor";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

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

/// <summary>Compare two values with configurable operator.</summary>
public sealed class CompareNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Bool, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Compare;

    /// <inheritdoc />
    public string Name => "Compare";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, CompareNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Operator selector dropdown would be rendered here
        return 25f;
    }
}

/// <summary>Select between two values based on condition (ternary).</summary>
public sealed class SelectNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Condition", PortTypeId.Bool, 60f),
        PortDefinition.Input("True", PortTypeId.Float, 85f),
        PortDefinition.Input("False", PortTypeId.Float, 110f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Select;

    /// <inheritdoc />
    public string Name => "Select";

    /// <inheritdoc />
    public string Category => "KESL/Logic";

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
