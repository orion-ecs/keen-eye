using System;
using System.Diagnostics.CodeAnalysis;

namespace KeenEyes;

/// <summary>
/// Hides the field from the editor inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class HideInInspectorAttribute : Attribute;

/// <summary>
/// Displays the field as read-only in the editor inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class ReadOnlyInInspectorAttribute : Attribute;

/// <summary>
/// Constrains a numeric field to a range and displays it as a slider.
/// </summary>
/// <param name="min">The minimum value.</param>
/// <param name="max">The maximum value.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class RangeAttribute(float min, float max) : Attribute
{
    /// <summary>
    /// Gets the minimum value.
    /// </summary>
    public float Min { get; } = min;

    /// <summary>
    /// Gets the maximum value.
    /// </summary>
    public float Max { get; } = max;
}

/// <summary>
/// Provides a tooltip for the field in the editor inspector.
/// </summary>
/// <param name="text">The tooltip text.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TooltipAttribute(string text) : Attribute
{
    /// <summary>
    /// Gets the tooltip text.
    /// </summary>
    public string Text { get; } = text;
}

/// <summary>
/// Displays a header above the field in the editor inspector.
/// </summary>
/// <param name="text">The header text.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class HeaderAttribute(string text) : Attribute
{
    /// <summary>
    /// Gets the header text.
    /// </summary>
    public string Text { get; } = text;
}

/// <summary>
/// Adds vertical spacing before the field in the editor inspector.
/// </summary>
/// <param name="height">The height of the space in pixels.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class SpaceAttribute(float height = 8f) : Attribute
{
    /// <summary>
    /// Gets the height of the space in pixels.
    /// </summary>
    public float Height { get; } = height;
}

/// <summary>
/// Specifies a custom display name for the field in the editor inspector.
/// </summary>
/// <param name="name">The display name.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class DisplayNameAttribute(string name) : Attribute
{
    /// <summary>
    /// Gets the display name.
    /// </summary>
    public string Name { get; } = name;
}

/// <summary>
/// Indicates that a string field should be displayed as a multi-line text area.
/// </summary>
/// <param name="minLines">The minimum number of lines to display.</param>
/// <param name="maxLines">The maximum number of lines to display.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class TextAreaAttribute(int minLines = 3, int maxLines = 10) : Attribute
{
    /// <summary>
    /// Gets the minimum number of lines.
    /// </summary>
    public int MinLines { get; } = minLines;

    /// <summary>
    /// Gets the maximum number of lines.
    /// </summary>
    public int MaxLines { get; } = maxLines;
}

/// <summary>
/// Groups fields under a foldout section in the editor inspector.
/// </summary>
/// <param name="groupName">The name of the group.</param>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
[ExcludeFromCodeCoverage]
public sealed class FoldoutGroupAttribute(string groupName) : Attribute
{
    /// <summary>
    /// Gets the group name.
    /// </summary>
    public string GroupName { get; } = groupName;
}
