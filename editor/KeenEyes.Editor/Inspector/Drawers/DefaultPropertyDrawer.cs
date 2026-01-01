using System.Reflection;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Application;
using KeenEyes.Editor.Common.Inspector;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Inspector.Drawers;

/// <summary>
/// Default property drawer that displays values as read-only text.
/// Used when no specific drawer is registered for a type.
/// </summary>
public sealed class DefaultPropertyDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(object);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var displayValue = FormatValue(value, field.FieldType);

        var label = WidgetFactory.CreateLabel(
            context.EditorWorld,
            context.Parent,
            $"Value_{field.Name}",
            displayValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextMuted,
                HorizontalAlign: TextAlignH.Right
            ));

        return label;
    }

    /// <inheritdoc/>
    public override void UpdateUI(PropertyDrawerContext context, Entity uiEntity, object? value)
    {
        if (context.EditorWorld.Has<UIText>(uiEntity))
        {
            ref var text = ref context.EditorWorld.Get<UIText>(uiEntity);
            text.Content = FormatValue(value, typeof(object));
        }
    }

    private static string FormatValue(object? value, Type type)
    {
        if (value is null)
        {
            return "null";
        }

        // Collections
        if (ComponentIntrospector.IsCollectionType(type))
        {
            if (value is System.Collections.IEnumerable enumerable)
            {
                var count = 0;
                foreach (var _ in enumerable)
                {
                    count++;
                }
                return $"[{count} items]";
            }
            return "[0 items]";
        }

        return value.ToString() ?? string.Empty;
    }
}
