namespace KeenEyes.Graph.Kesl.Components;

/// <summary>
/// Component data for a QueryBinding node.
/// </summary>
/// <remarks>
/// <para>
/// QueryBinding nodes expose ECS component fields as outputs in the shader graph.
/// The shader compiler uses these bindings to generate the KESL query block.
/// </para>
/// </remarks>
public struct QueryBindingNodeData : IComponent
{
    /// <summary>
    /// The fully qualified type name of the component being bound.
    /// </summary>
    public string ComponentTypeName;

    /// <summary>
    /// The binding identifier used in the shader (e.g., "pos" for Position component).
    /// </summary>
    public string BindingName;

    /// <summary>
    /// Whether this binding is read-only (input only, not modified by shader).
    /// </summary>
    public bool IsReadOnly;

    /// <summary>
    /// Creates default QueryBinding node data.
    /// </summary>
    public static QueryBindingNodeData Default => new()
    {
        ComponentTypeName = "",
        BindingName = "binding",
        IsReadOnly = false
    };
}
