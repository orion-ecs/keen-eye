namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a VertexShader node.
/// </summary>
/// <remarks>
/// <para>
/// The VertexShader node is the root node of a KESL vertex shader graph. It defines the
/// shader's name and aggregates inputs from InputAttribute, OutputAttribute, and Parameter nodes.
/// </para>
/// </remarks>
public struct VertexShaderNodeData : IComponent
{
    /// <summary>
    /// The name of the vertex shader.
    /// </summary>
    public string ShaderName;

    /// <summary>
    /// Creates default VertexShader node data.
    /// </summary>
    public static VertexShaderNodeData Default => new()
    {
        ShaderName = "NewVertexShader"
    };
}
