using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for SetVariable and GetVariable nodes.
/// </summary>
/// <remarks>
/// <para>
/// Variable nodes allow storing and retrieving named values within a shader graph.
/// </para>
/// </remarks>
public struct VariableNodeData : IComponent
{
    /// <summary>
    /// The name of the variable.
    /// </summary>
    public string VariableName;

    /// <summary>
    /// The data type of the variable.
    /// </summary>
    public PortTypeId VariableType;

    /// <summary>
    /// Creates default Variable node data.
    /// </summary>
    public static VariableNodeData Default => new()
    {
        VariableName = "var",
        VariableType = PortTypeId.Float
    };
}
