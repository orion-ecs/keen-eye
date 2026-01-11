using KeenEyes.Graph.Abstractions;

namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for an OutputAttribute node.
/// </summary>
/// <remarks>
/// <para>
/// OutputAttribute nodes define shader outputs. For vertex shaders, these are passed to fragment
/// shaders as interpolated values. For fragment shaders, these define render target outputs.
/// </para>
/// </remarks>
public struct OutputAttributeNodeData : IComponent
{
    /// <summary>
    /// The name of the output attribute (e.g., "worldPos", "fragColor").
    /// </summary>
    public string AttributeName;

    /// <summary>
    /// The data type of the attribute.
    /// </summary>
    public PortTypeId AttributeType;

    /// <summary>
    /// The output location binding index.
    /// For fragment shaders, this specifies the render target index.
    /// For vertex shader outputs to fragment shader inputs, this defines interpolator slots.
    /// Use -1 for no explicit location (auto-assigned).
    /// </summary>
    public int Location;

    /// <summary>
    /// Creates default OutputAttribute node data.
    /// </summary>
    public static OutputAttributeNodeData Default => new()
    {
        AttributeName = "output",
        AttributeType = PortTypeId.Float4,
        Location = -1
    };
}
