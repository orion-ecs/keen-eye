namespace KeenEyes.Graph.Kesl.Preview;

/// <summary>
/// Represents a preview component with named float fields.
/// </summary>
/// <remarks>
/// <para>
/// PreviewComponent is a simplified representation of an ECS component
/// for shader preview purposes. It stores field values as floats in a dictionary,
/// allowing the shader executor to read and modify values without needing
/// the actual component types at runtime.
/// </para>
/// </remarks>
public sealed class PreviewComponent
{
    /// <summary>
    /// Gets the component type name (e.g., "Position", "Velocity").
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Gets the dictionary of field names to float values.
    /// </summary>
    public Dictionary<string, float> Fields { get; } = [];

    /// <summary>
    /// Creates a new preview component with the specified type name.
    /// </summary>
    /// <param name="typeName">The component type name.</param>
    public PreviewComponent(string typeName)
    {
        TypeName = typeName;
    }

    /// <summary>
    /// Creates a deep copy of this preview component.
    /// </summary>
    /// <returns>A new PreviewComponent with copied field values.</returns>
    public PreviewComponent Clone()
    {
        var clone = new PreviewComponent(TypeName);
        foreach (var (key, value) in Fields)
        {
            clone.Fields[key] = value;
        }
        return clone;
    }
}

/// <summary>
/// Represents a preview entity with multiple components.
/// </summary>
/// <remarks>
/// <para>
/// PreviewEntity is a simplified representation of an ECS entity
/// for shader preview purposes. Each entity has an index and a collection
/// of preview components that can be modified by the shader executor.
/// </para>
/// </remarks>
public sealed class PreviewEntity
{
    /// <summary>
    /// Gets the entity index (0-based).
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the dictionary of component type names to preview components.
    /// </summary>
    public Dictionary<string, PreviewComponent> Components { get; } = [];

    /// <summary>
    /// Creates a new preview entity with the specified index.
    /// </summary>
    /// <param name="index">The entity index.</param>
    public PreviewEntity(int index)
    {
        Index = index;
    }

    /// <summary>
    /// Creates a deep copy of this preview entity.
    /// </summary>
    /// <returns>A new PreviewEntity with copied components.</returns>
    public PreviewEntity Clone()
    {
        var clone = new PreviewEntity(Index);
        foreach (var (name, component) in Components)
        {
            clone.Components[name] = component.Clone();
        }
        return clone;
    }
}
