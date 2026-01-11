namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a FragmentShader node.
/// </summary>
/// <remarks>
/// <para>
/// The FragmentShader node is the root node of a KESL fragment shader graph. It defines the
/// shader's name and aggregates inputs from InputAttribute, OutputAttribute, and Parameter nodes.
/// </para>
/// </remarks>
public struct FragmentShaderNodeData : IComponent
{
    /// <summary>
    /// The name of the fragment shader.
    /// </summary>
    public string ShaderName;

    /// <summary>
    /// Creates default FragmentShader node data.
    /// </summary>
    public static FragmentShaderNodeData Default => new()
    {
        ShaderName = "NewFragmentShader"
    };
}
