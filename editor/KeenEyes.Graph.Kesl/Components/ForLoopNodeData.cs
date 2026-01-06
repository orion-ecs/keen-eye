namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a ForLoop node.
/// </summary>
/// <remarks>
/// <para>
/// ForLoop nodes generate loop constructs in the shader with configurable
/// start, end, and step values.
/// </para>
/// </remarks>
public struct ForLoopNodeData : IComponent
{
    /// <summary>
    /// The name of the loop index variable.
    /// </summary>
    public string IndexName;

    /// <summary>
    /// Creates default ForLoop node data.
    /// </summary>
    public static ForLoopNodeData Default => new()
    {
        IndexName = "i"
    };
}
