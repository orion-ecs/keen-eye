using KeenEyes;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Sample.UI.Nodes;

/// <summary>
/// Number constant node - outputs a float value.
/// </summary>
public sealed class NumberNode : INodeTypeDefinition
{
    /// <inheritdoc />
    public int TypeId => 101;

    /// <inheritdoc />
    public string Name => "Number";

    /// <inheritdoc />
    public string Category => "Math";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts { get; } = [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts { get; } =
    [
        PortDefinition.Output("Value", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public bool IsCollapsible => false;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        // Could add component for storing the value
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
        => 0f; // Uses default port rendering
}

/// <summary>
/// Add node - adds two float inputs.
/// </summary>
public sealed class AddNode : INodeTypeDefinition
{
    /// <inheritdoc />
    public int TypeId => 102;

    /// <inheritdoc />
    public string Name => "Add";

    /// <inheritdoc />
    public string Category => "Math";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts { get; } =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 90f)
    ];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts { get; } =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 75f)
    ];

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
        => 0f;
}

/// <summary>
/// Multiply node - multiplies two float inputs.
/// </summary>
public sealed class MultiplyNode : INodeTypeDefinition
{
    /// <inheritdoc />
    public int TypeId => 103;

    /// <inheritdoc />
    public string Name => "Multiply";

    /// <inheritdoc />
    public string Category => "Math";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts { get; } =
    [
        PortDefinition.Input("A", PortTypeId.Float, 60f),
        PortDefinition.Input("B", PortTypeId.Float, 90f)
    ];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts { get; } =
    [
        PortDefinition.Output("Result", PortTypeId.Float, 75f)
    ];

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
        => 0f;
}
