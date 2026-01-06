namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a ComputeShader node.
/// </summary>
/// <remarks>
/// <para>
/// The ComputeShader node is the root node of a KESL shader graph. It defines the
/// shader's name and aggregates inputs from QueryBinding and Parameter nodes.
/// </para>
/// </remarks>
public struct ComputeShaderNodeData : IComponent
{
    /// <summary>
    /// The name of the compute shader.
    /// </summary>
    public string ShaderName;

    /// <summary>
    /// Creates default ComputeShader node data.
    /// </summary>
    public static ComputeShaderNodeData Default => new()
    {
        ShaderName = "NewShader"
    };
}
