using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.Graph.Kesl.Nodes.Flow;

/// <summary>Float constant value.</summary>
public sealed class FloatConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.FloatConstant;

    /// <inheritdoc />
    public string Name => "Float";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, FloatConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        // Float value input field would be rendered here
        return 25f;
    }
}

/// <summary>Integer constant value.</summary>
public sealed class IntConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Int, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.IntConstant;

    /// <inheritdoc />
    public string Name => "Int";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, IntConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        return 25f;
    }
}

/// <summary>Boolean constant value.</summary>
public sealed class BoolConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Bool, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.BoolConstant;

    /// <inheritdoc />
    public string Name => "Bool";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, BoolConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        return 25f;
    }
}

/// <summary>Vector2 constant value.</summary>
public sealed class Vector2ConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float2, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Vector2Constant;

    /// <inheritdoc />
    public string Name => "Vector2";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, Vector2ConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        return 50f;
    }
}

/// <summary>Vector3 constant value.</summary>
public sealed class Vector3ConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float3, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Vector3Constant;

    /// <inheritdoc />
    public string Name => "Vector3";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, Vector3ConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        return 75f;
    }
}

/// <summary>Vector4 constant value.</summary>
public sealed class Vector4ConstantNode : INodeTypeDefinition
{
    private static readonly PortDefinition[] Outputs =
    [
        PortDefinition.Output("Value", PortTypeId.Float4, 60f)
    ];

    /// <inheritdoc />
    public int TypeId => KeslNodeIds.Vector4Constant;

    /// <inheritdoc />
    public string Name => "Vector4";

    /// <inheritdoc />
    public string Category => "KESL/Constants";

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> InputPorts => [];

    /// <inheritdoc />
    public IReadOnlyList<PortDefinition> OutputPorts => Outputs;

    /// <inheritdoc />
    public bool IsCollapsible => true;

    /// <inheritdoc />
    public void Initialize(Entity node, IWorld world)
    {
        world.Add(node, Vector4ConstantData.Default);
    }

    /// <inheritdoc />
    public float RenderBody(Entity node, IWorld world, I2DRenderer renderer, Rectangle bodyArea)
    {
        return 100f;
    }
}
