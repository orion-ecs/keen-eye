using KeenEyes.Editor.Abstractions;
using KeenEyes.Graph.Kesl.Components;
using KeenEyes.Graph.Kesl.Nodes.Flow;
using KeenEyes.Graph.Kesl.Nodes.Logic;
using KeenEyes.Graph.Kesl.Nodes.Math;
using KeenEyes.Graph.Kesl.Nodes.Shader;
using KeenEyes.Graph.Kesl.Nodes.Vector;

namespace KeenEyes.Graph.Kesl;

/// <summary>
/// Plugin that registers KESL (KeenEyes Shader Language) node types for the graph editor.
/// </summary>
/// <remarks>
/// <para>
/// This plugin depends on the GraphPlugin being installed first. It registers KESL-specific
/// node types for visual shader programming, including:
/// </para>
/// <list type="bullet">
/// <item><b>Shader nodes</b>: ComputeShader, VertexShader, FragmentShader, QueryBinding, Parameter, InputAttribute, OutputAttribute</item>
/// <item><b>Math nodes</b>: Add, Subtract, Multiply, etc.</item>
/// <item><b>Vector nodes</b>: Construct, Split, Normalize, etc.</item>
/// <item><b>Logic nodes</b>: And, Or, Not, Compare, Select</item>
/// <item><b>Flow nodes</b>: If, ForLoop, SetVariable</item>
/// </list>
/// </remarks>
public sealed class KeslGraphPlugin : IWorldPlugin
{
    /// <inheritdoc/>
    public string Name => "KESL Graph";

    /// <inheritdoc/>
    public void Install(IPluginContext context)
    {
        // Get the NodeTypeRegistry from the GraphPlugin
        if (!context.World.TryGetExtension<NodeTypeRegistry>(out var nodeTypeRegistry) || nodeTypeRegistry is null)
        {
            throw new InvalidOperationException(
                "KeslGraphPlugin requires GraphPlugin to be installed first. " +
                "Ensure GraphPlugin is added to the world before KeslGraphPlugin.");
        }

        // Register KESL-specific components
        RegisterComponents(context);

        // Register KESL node types
        RegisterShaderNodes(nodeTypeRegistry);
        RegisterMathNodes(nodeTypeRegistry);
        RegisterVectorNodes(nodeTypeRegistry);
        RegisterLogicNodes(nodeTypeRegistry);
        RegisterFlowNodes(nodeTypeRegistry);
        RegisterConstantNodes(nodeTypeRegistry);
    }

    /// <inheritdoc/>
    public void Uninstall(IPluginContext context)
    {
        // Node type registrations persist with the NodeTypeRegistry
        // No explicit cleanup needed
    }

    private static void RegisterComponents(IPluginContext context)
    {
        // Shader node components
        context.RegisterComponent<ComputeShaderNodeData>();
        context.RegisterComponent<VertexShaderNodeData>();
        context.RegisterComponent<FragmentShaderNodeData>();
        context.RegisterComponent<QueryBindingNodeData>();
        context.RegisterComponent<ParameterNodeData>();
        context.RegisterComponent<InputAttributeNodeData>();
        context.RegisterComponent<OutputAttributeNodeData>();

        // Logic node components
        context.RegisterComponent<CompareNodeData>();

        // Flow node components
        context.RegisterComponent<VariableNodeData>();
        context.RegisterComponent<ForLoopNodeData>();

        // Constant node components
        context.RegisterComponent<FloatConstantData>();
        context.RegisterComponent<IntConstantData>();
        context.RegisterComponent<BoolConstantData>();
        context.RegisterComponent<Vector2ConstantData>();
        context.RegisterComponent<Vector3ConstantData>();
        context.RegisterComponent<Vector4ConstantData>();
    }

    private static void RegisterShaderNodes(NodeTypeRegistry registry)
    {
        registry.Register<ComputeShaderNode>();
        registry.Register<VertexShaderNode>();
        registry.Register<FragmentShaderNode>();
        registry.Register<QueryBindingNode>();
        registry.Register<ParameterNode>();
        registry.Register<InputAttributeNode>();
        registry.Register<OutputAttributeNode>();
    }

    private static void RegisterMathNodes(NodeTypeRegistry registry)
    {
        // Binary operators
        registry.Register<AddNode>();
        registry.Register<SubtractNode>();
        registry.Register<MultiplyNode>();
        registry.Register<DivideNode>();
        registry.Register<ModuloNode>();
        registry.Register<MinNode>();
        registry.Register<MaxNode>();
        registry.Register<PowNode>();
        registry.Register<Atan2Node>();
        registry.Register<StepNode>();

        // Unary functions
        registry.Register<AbsNode>();
        registry.Register<NegateNode>();
        registry.Register<FloorNode>();
        registry.Register<CeilNode>();
        registry.Register<RoundNode>();
        registry.Register<SignNode>();
        registry.Register<FracNode>();
        registry.Register<SqrtNode>();
        registry.Register<ExpNode>();
        registry.Register<LogNode>();
        registry.Register<Log2Node>();

        // Trigonometric
        registry.Register<SinNode>();
        registry.Register<CosNode>();
        registry.Register<TanNode>();
        registry.Register<AsinNode>();
        registry.Register<AcosNode>();
        registry.Register<AtanNode>();

        // Interpolation
        registry.Register<ClampNode>();
        registry.Register<LerpNode>();
        registry.Register<SmoothstepNode>();
    }

    private static void RegisterVectorNodes(NodeTypeRegistry registry)
    {
        // Construct
        registry.Register<ConstructVector2Node>();
        registry.Register<ConstructVector3Node>();
        registry.Register<ConstructVector4Node>();

        // Split
        registry.Register<SplitVector2Node>();
        registry.Register<SplitVector3Node>();
        registry.Register<SplitVector4Node>();

        // Operations
        registry.Register<NormalizeNode>();
        registry.Register<DotProductNode>();
        registry.Register<CrossProductNode>();
        registry.Register<LengthNode>();
        registry.Register<LengthSquaredNode>();
        registry.Register<DistanceNode>();
        registry.Register<DistanceSquaredNode>();
        registry.Register<ReflectNode>();
    }

    private static void RegisterLogicNodes(NodeTypeRegistry registry)
    {
        registry.Register<AndNode>();
        registry.Register<OrNode>();
        registry.Register<NotNode>();
        registry.Register<XorNode>();
        registry.Register<CompareNode>();
        registry.Register<SelectNode>();
    }

    private static void RegisterFlowNodes(NodeTypeRegistry registry)
    {
        registry.Register<IfNode>();
        registry.Register<ForLoopNode>();
        registry.Register<SetVariableNode>();
        registry.Register<GetVariableNode>();
    }

    private static void RegisterConstantNodes(NodeTypeRegistry registry)
    {
        registry.Register<FloatConstantNode>();
        registry.Register<IntConstantNode>();
        registry.Register<BoolConstantNode>();
        registry.Register<Vector2ConstantNode>();
        registry.Register<Vector3ConstantNode>();
        registry.Register<Vector4ConstantNode>();
    }
}
