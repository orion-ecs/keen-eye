using System.Numerics;
using System.Reflection;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Inspector.Drawers;

/// <summary>
/// Property drawer for enum fields.
/// </summary>
public sealed class EnumDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Enum);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var enumType = field.FieldType;
        var displayValue = value?.ToString() ?? "(none)";

        // Check if it's a flags enum
        var isFlags = enumType.GetCustomAttribute<FlagsAttribute>() is not null;

        if (isFlags && value is not null)
        {
            // For flags, show the combined value
            displayValue = FormatFlags(enumType, value);
        }

        var color = context.Metadata.IsReadOnly ? EditorColors.TextMuted : EditorColors.TextWhite;

        // Create dropdown-like display
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Enum_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // Value label
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            container,
            $"Value_{field.Name}",
            displayValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        // Dropdown indicator (read-only for now)
        if (!context.Metadata.IsReadOnly)
        {
            WidgetFactory.CreateLabel(
                context.EditorWorld,
                container,
                $"Arrow_{field.Name}",
                "\u25BC", // ▼
                context.Font,
                new LabelConfig(
                    FontSize: 8,
                    TextColor: EditorColors.TextMuted,
                    HorizontalAlign: TextAlignH.Right
                ));
        }

        return container;
    }

    private static string FormatFlags(Type enumType, object value)
    {
        var intValue = Convert.ToInt64(value);
        if (intValue == 0)
        {
            return "None";
        }

        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);
        var result = new List<string>();

        for (var i = 0; i < values.Length; i++)
        {
            var flagValue = Convert.ToInt64(values.GetValue(i));
            if (flagValue != 0 && (intValue & flagValue) == flagValue)
            {
                result.Add(names[i]);
            }
        }

        return result.Count > 0 ? string.Join(" | ", result) : value.ToString() ?? string.Empty;
    }
}

/// <summary>
/// Property drawer for Entity reference fields.
/// </summary>
public sealed class EntityDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Entity);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var entity = value is Entity e ? e : default;

        string displayValue;
        Vector4 color;

        if (!entity.IsValid)
        {
            displayValue = "None";
            color = EditorColors.TextMuted;
        }
        else
        {
            displayValue = $"Entity({entity.Id})";
            color = EditorColors.TextWhite;
        }

        if (context.Metadata.IsReadOnly)
        {
            color = EditorColors.TextMuted;
        }

        // Create container with entity display and picker button
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Entity_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // Entity icon
        WidgetFactory.CreatePanel(
            context.EditorWorld,
            container,
            $"Icon_{field.Name}",
            new PanelConfig(
                Width: 12,
                Height: 12,
                BackgroundColor: entity.IsValid
                    ? new Vector4(0.3f, 0.5f, 0.8f, 1f)
                    : new Vector4(0.3f, 0.3f, 0.3f, 1f)
            ));

        // Value label
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            container,
            $"Value_{field.Name}",
            displayValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: color,
                HorizontalAlign: TextAlignH.Right
            ));

        // Picker button indicator (read-only for now)
        if (!context.Metadata.IsReadOnly)
        {
            WidgetFactory.CreateLabel(
                context.EditorWorld,
                container,
                $"Picker_{field.Name}",
                "\u25CE", // ◎
                context.Font,
                new LabelConfig(
                    FontSize: 10,
                    TextColor: EditorColors.TextMuted,
                    HorizontalAlign: TextAlignH.Right
                ));
        }

        return container;
    }
}
