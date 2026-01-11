using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Shaders.Compiler.Parsing.Ast;

namespace KeenEyes.Graph.Kesl.Tests;

/// <summary>
/// Helper class for building test graphs.
/// </summary>
internal sealed class TestGraphBuilder : IDisposable
{
    private readonly World world;
    private readonly Entity canvas;

    public TestGraphBuilder()
    {
        world = new World();
        canvas = world.Spawn()
            .With(GraphCanvas.Default)
            .Build();
    }

    public World World => world;
    public Entity Canvas => canvas;

    /// <summary>
    /// Creates a node of the specified type.
    /// </summary>
    public Entity CreateNode(int nodeTypeId, Vector2? position = null)
    {
        var pos = position ?? Vector2.Zero;
        return world.Spawn()
            .With(GraphNode.Create(pos, nodeTypeId, canvas))
            .Build();
    }

    /// <summary>
    /// Creates a ComputeShader root node.
    /// </summary>
    public Entity CreateComputeShader(string name = "TestShader")
    {
        var node = CreateNode(KeslNodeIds.ComputeShader);
        world.Add(node, new ComputeShaderNodeData { ShaderName = name });
        return node;
    }

    /// <summary>
    /// Creates a VertexShader root node.
    /// </summary>
    public Entity CreateVertexShader(string name = "TestVertexShader")
    {
        var node = CreateNode(KeslNodeIds.VertexShader);
        world.Add(node, new VertexShaderNodeData { ShaderName = name });
        return node;
    }

    /// <summary>
    /// Creates a FragmentShader root node.
    /// </summary>
    public Entity CreateFragmentShader(string name = "TestFragmentShader")
    {
        var node = CreateNode(KeslNodeIds.FragmentShader);
        world.Add(node, new FragmentShaderNodeData { ShaderName = name });
        return node;
    }

    /// <summary>
    /// Creates an InputAttribute node for vertex/fragment shaders.
    /// </summary>
    public Entity CreateInputAttribute(string name, PortTypeId type, int location = 0)
    {
        var node = CreateNode(KeslNodeIds.InputAttribute);
        world.Add(node, new InputAttributeNodeData
        {
            AttributeName = name,
            AttributeType = type,
            Location = location
        });
        return node;
    }

    /// <summary>
    /// Creates an OutputAttribute node for vertex/fragment shaders.
    /// </summary>
    public Entity CreateOutputAttribute(string name, PortTypeId type, int location = -1)
    {
        var node = CreateNode(KeslNodeIds.OutputAttribute);
        world.Add(node, new OutputAttributeNodeData
        {
            AttributeName = name,
            AttributeType = type,
            Location = location
        });
        return node;
    }

    /// <summary>
    /// Creates a QueryBinding node.
    /// </summary>
    public Entity CreateQueryBinding(string componentName, AccessMode accessMode = AccessMode.Read)
    {
        var node = CreateNode(KeslNodeIds.QueryBinding);
        world.Add(node, new QueryBindingNodeData
        {
            ComponentTypeName = componentName,
            BindingName = componentName.ToLowerInvariant(),
            IsReadOnly = accessMode == AccessMode.Read || accessMode == AccessMode.Optional
        });
        return node;
    }

    /// <summary>
    /// Creates a Parameter node.
    /// </summary>
    public Entity CreateParameter(string name, PortTypeId type)
    {
        var node = CreateNode(KeslNodeIds.Parameter);
        world.Add(node, new ParameterNodeData { ParameterName = name, ParameterType = type });
        return node;
    }

    /// <summary>
    /// Creates a float constant node.
    /// </summary>
    public Entity CreateFloatConstant(float value)
    {
        var node = CreateNode(KeslNodeIds.FloatConstant);
        world.Add(node, new FloatConstantData { Value = value });
        return node;
    }

    /// <summary>
    /// Creates an int constant node.
    /// </summary>
    public Entity CreateIntConstant(int value)
    {
        var node = CreateNode(KeslNodeIds.IntConstant);
        world.Add(node, new IntConstantData { Value = value });
        return node;
    }

    /// <summary>
    /// Creates a bool constant node.
    /// </summary>
    public Entity CreateBoolConstant(bool value)
    {
        var node = CreateNode(KeslNodeIds.BoolConstant);
        world.Add(node, new BoolConstantData { Value = value });
        return node;
    }

    /// <summary>
    /// Creates an Add node.
    /// </summary>
    public Entity CreateAddNode() => CreateNode(KeslNodeIds.Add);

    /// <summary>
    /// Creates a Multiply node.
    /// </summary>
    public Entity CreateMultiplyNode() => CreateNode(KeslNodeIds.Multiply);

    /// <summary>
    /// Creates a SetVariable node.
    /// </summary>
    public Entity CreateSetVariable(string variableName)
    {
        var node = CreateNode(KeslNodeIds.SetVariable);
        world.Add(node, new VariableNodeData { VariableName = variableName });
        return node;
    }

    /// <summary>
    /// Creates a GetVariable node.
    /// </summary>
    public Entity CreateGetVariable(string variableName)
    {
        var node = CreateNode(KeslNodeIds.GetVariable);
        world.Add(node, new VariableNodeData { VariableName = variableName });
        return node;
    }

    /// <summary>
    /// Creates a ForLoop node.
    /// </summary>
    public Entity CreateForLoop(string indexName = "i")
    {
        var node = CreateNode(KeslNodeIds.ForLoop);
        world.Add(node, new ForLoopNodeData { IndexName = indexName });
        return node;
    }

    /// <summary>
    /// Creates a Compare node.
    /// </summary>
    public Entity CreateCompare(ComparisonOperator op = ComparisonOperator.Equal)
    {
        var node = CreateNode(KeslNodeIds.Compare);
        world.Add(node, new CompareNodeData { Operator = op });
        return node;
    }

    /// <summary>
    /// Connects two nodes.
    /// </summary>
    public Entity Connect(Entity sourceNode, int sourcePort, Entity targetNode, int targetPort)
    {
        return world.Spawn()
            .With(GraphConnection.Create(sourceNode, sourcePort, targetNode, targetPort, canvas))
            .Build();
    }

    public void Dispose() => world.Dispose();
}
