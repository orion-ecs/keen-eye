using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Flow;

/// <summary>Conditional branching (if-then-else).</summary>
public sealed class IfNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f),
        PortDefinition.Input("Condition", PortTypeId.Bool, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("True", PortTypeId.Flow, 60f),
        PortDefinition.Output("False", PortTypeId.Flow, 85f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.If;

    /// <inheritdoc />
    public string Name => "If";

    /// <inheritdoc />
    public string Category => "KESL/Flow";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world) { }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea) => 0f;
}

/// <summary>For loop iteration.</summary>
public sealed class ForLoopNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f),
        PortDefinition.Input("Start", PortTypeId.Int, 85f),
        PortDefinition.Input("End", PortTypeId.Int, 110f),
        PortDefinition.Input("Step", PortTypeId.Int, 135f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Body", PortTypeId.Flow, 60f),
        PortDefinition.Output("Index", PortTypeId.Int, 85f),
        PortDefinition.Output("Complete", PortTypeId.Flow, 110f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.ForLoop;

    /// <inheritdoc />
    public string Name => "For Loop";

    /// <inheritdoc />
    public string Category => "KESL/Flow";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, ForLoopNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Loop index name field would be rendered here
        return 25f;
    }
}

/// <summary>Set/create a named variable.</summary>
public sealed class SetVariableNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Inputs =
    [
        PortDefinition.Input("Execute", PortTypeId.Flow, 60f),
        PortDefinition.Input("Value", PortTypeId.Float, 85f)
    ];

    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Execute", PortTypeId.Flow, 72.5f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.SetVariable;

    /// <inheritdoc />
    public string Name => "Set Variable";

    /// <inheritdoc />
    public string Category => "KESL/Flow";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => Inputs;

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, VariableNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Variable name and type fields would be rendered here
        return 50f;
    }
}

/// <summary>Get a named variable's value.</summary>
public sealed class GetVariableNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.GetVariable;

    /// <inheritdoc />
    public string Name => "Get Variable";

    /// <inheritdoc />
    public string Category => "KESL/Flow";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, VariableNodeData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Variable name dropdown would be rendered here
        return 25f;
    }
}
