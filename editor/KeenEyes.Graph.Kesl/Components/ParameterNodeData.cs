using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a Parameter node.
/// </summary>
/// <remarks>
/// <para>
/// Parameter nodes define uniform shader inputs that can be set at runtime.
/// These map to the KESL params block.
/// </para>
/// </remarks>
public struct ParameterNodeData : IComponent
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string ParameterName;

    /// <summary>
    /// The data type of the parameter.
    /// </summary>
    public PortTypeId ParameterType;

    /// <summary>
    /// Creates default Parameter node data.
    /// </summary>
    public static ParameterNodeData Default => new()
    {
        ParameterName = "param",
        ParameterType = PortTypeId.Float
    };
}
