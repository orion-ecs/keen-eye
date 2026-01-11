using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for an InputAttribute node.
/// </summary>
/// <remarks>
/// <para>
/// InputAttribute nodes define vertex shader inputs with layout location bindings.
/// These map to the KESL "in" block in vertex shaders.
/// </para>
/// </remarks>
public struct InputAttributeNodeData : IComponent
{
    /// <summary>
    /// The name of the input attribute (e.g., "position", "normal", "texCoord").
    /// </summary>
    public string AttributeName;

    /// <summary>
    /// The data type of the attribute.
    /// </summary>
    public PortTypeId AttributeType;

    /// <summary>
    /// The layout location binding index.
    /// </summary>
    public int Location;

    /// <summary>
    /// Creates default InputAttribute node data.
    /// </summary>
    public static InputAttributeNodeData Default => new()
    {
        AttributeName = "attribute",
        AttributeType = PortTypeId.Float3,
        Location = 0
    };
}
