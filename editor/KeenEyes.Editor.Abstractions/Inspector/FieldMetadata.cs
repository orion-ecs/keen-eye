namespace KeenEyes.Editor.Abstractions.Inspector;

/// <summary>
/// Metadata about a component field extracted from attributes.
/// </summary>
public sealed record FieldMetadata
{
    /// <summary>
    /// Gets the display name for the field.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Gets the tooltip text, if any.
    /// </summary>
    public string? Tooltip { get; init; }

    /// <summary>
    /// Gets the header text to display above this field, if any.
    /// </summary>
    public string? Header { get; init; }

    /// <summary>
    /// Gets the space height to add before this field, if any.
    /// </summary>
    public float? SpaceHeight { get; init; }

    /// <summary>
    /// Gets the range constraint for numeric fields, if any.
    /// </summary>
    public (float Min, float Max)? Range { get; init; }

    /// <summary>
    /// Gets whether the field is read-only in the inspector.
    /// </summary>
    public bool IsReadOnly { get; init; }

    /// <summary>
    /// Gets the foldout group name, if any.
    /// </summary>
    public string? FoldoutGroup { get; init; }

    /// <summary>
    /// Gets text area configuration for string fields, if any.
    /// </summary>
    public (int MinLines, int MaxLines)? TextArea { get; init; }
}
