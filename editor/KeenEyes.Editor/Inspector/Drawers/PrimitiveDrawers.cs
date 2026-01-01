using System.Reflection;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Common.Inspector;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Inspector.Drawers;

/// <summary>
/// Property drawer for integer fields.
/// </summary>
public sealed class IntDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(int);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var intValue = value is int i ? i : 0;
        var displayValue = intValue.ToString();

        // Check for range attribute
        if (context.Metadata.Range is var (min, max))
        {
            return CreateSlider(context, field, intValue, (int)min, (int)max);
        }

        return CreateTextField(context, field, displayValue, context.Metadata.IsReadOnly);
    }

    private static Entity CreateSlider(PropertyDrawerContext context, FieldInfo field, int value, int min, int max)
    {
        // For now, create a label showing the value and range
        // Full slider implementation would require the slider widget
        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            $"{value} [{min}-{max}]",
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }

    private static Entity CreateTextField(PropertyDrawerContext context, FieldInfo field, string value, bool readOnly)
    {
        var color = readOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            value,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }
}

/// <summary>
/// Property drawer for float fields.
/// </summary>
public sealed class FloatDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(float);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var floatValue = value is float f ? f : 0f;

        // Check for range attribute
        if (context.Metadata.Range is var (min, max))
        {
            return CreateRangeDisplay(context, field, floatValue, min, max);
        }

        return CreateTextField(context, field, floatValue.ToString("F2"), context.Metadata.IsReadOnly);
    }

    private static Entity CreateRangeDisplay(PropertyDrawerContext context, FieldInfo field, float value, float min, float max)
    {
        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            $"{value:F2} [{min:F0}-{max:F0}]",
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }

    private static Entity CreateTextField(PropertyDrawerContext context, FieldInfo field, string value, bool readOnly)
    {
        var color = readOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            value,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }
}

/// <summary>
/// Property drawer for double fields.
/// </summary>
public sealed class DoubleDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(double);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var doubleValue = value is double d ? d : 0.0;
        var displayValue = doubleValue.ToString("F2");
        var color = context.Metadata.IsReadOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            displayValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }
}

/// <summary>
/// Property drawer for boolean fields.
/// </summary>
public sealed class BoolDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(bool);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var boolValue = value is bool b && b;

        // Display as checkbox symbol
        var displayValue = boolValue ? "\u2611" : "\u2610"; // ☑ or ☐
        var color = context.Metadata.IsReadOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            displayValue,
            context.Font,
            new LabelConfig(
                FontSize: 13,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }
}

/// <summary>
/// Property drawer for string fields.
/// </summary>
public sealed class StringDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(string);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var stringValue = value as string ?? string.Empty;

        // Truncate long strings for display
        if (stringValue.Length > 30)
        {
            stringValue = stringValue[..27] + "...";
        }

        var color = context.Metadata.IsReadOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        // Check for TextArea attribute
        if (context.Metadata.TextArea is not null)
        {
            // For text areas, show truncated text with indication
            stringValue = $"\"{stringValue}\"";
        }
        else if (string.IsNullOrEmpty(stringValue))
        {
            stringValue = "(empty)";
            color = EditorColors.TextMuted;
        }

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            stringValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }

    /// <inheritdoc/>
    public override float GetHeight(FieldInfo field, object? value)
    {
        var metadata = ComponentIntrospector.GetFieldMetadata(field);
        if (metadata.TextArea is var (minLines, _))
        {
            return 20f * minLines;
        }
        return 20f;
    }
}
