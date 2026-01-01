using System.Numerics;
using System.Reflection;

using KeenEyes.Editor.Abstractions.Inspector;
using KeenEyes.Editor.Application;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.UI.Abstractions;
using KeenEyes.UI.Widgets;

namespace KeenEyes.Editor.Inspector.Drawers;

/// <summary>
/// Property drawer for Vector2 fields.
/// </summary>
public sealed class Vector2Drawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Vector2);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var vec = value is Vector2 v ? v : Vector2.Zero;

        // Create container for X, Y labels
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Vector2_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // X component
        CreateComponentLabel(context, container, "X", vec.X, new Vector4(0.9f, 0.3f, 0.3f, 1f));

        // Y component
        CreateComponentLabel(context, container, "Y", vec.Y, new Vector4(0.3f, 0.9f, 0.3f, 1f));

        return container;
    }

    private static void CreateComponentLabel(PropertyDrawerContext context, Entity parent, string label, float value, Vector4 labelColor)
    {
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Label_{label}",
            label,
            context.Font,
            new LabelConfig(
                FontSize: 10,
                TextColor: labelColor,
                HorizontalAlign: TextAlignH.Left
            ));

        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Value_{label}",
            value.ToString("F2"),
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));
    }
}

/// <summary>
/// Property drawer for Vector3 fields.
/// </summary>
public sealed class Vector3Drawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Vector3);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var vec = value is Vector3 v ? v : Vector3.Zero;

        // Create container for X, Y, Z labels
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Vector3_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // X component (red)
        CreateComponentLabel(context, container, "X", vec.X, new Vector4(0.9f, 0.3f, 0.3f, 1f));

        // Y component (green)
        CreateComponentLabel(context, container, "Y", vec.Y, new Vector4(0.3f, 0.9f, 0.3f, 1f));

        // Z component (blue)
        CreateComponentLabel(context, container, "Z", vec.Z, new Vector4(0.3f, 0.5f, 0.9f, 1f));

        return container;
    }

    private static void CreateComponentLabel(PropertyDrawerContext context, Entity parent, string label, float value, Vector4 labelColor)
    {
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Label_{label}",
            label,
            context.Font,
            new LabelConfig(
                FontSize: 10,
                TextColor: labelColor,
                HorizontalAlign: TextAlignH.Left
            ));

        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Value_{label}",
            value.ToString("F2"),
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));
    }
}

/// <summary>
/// Property drawer for Vector4 fields.
/// </summary>
public sealed class Vector4Drawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Vector4);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var vec = value is Vector4 v ? v : Vector4.Zero;

        // Check if this looks like a color (field name contains "color" or similar)
        var isColor = field.Name.Contains("Color", StringComparison.OrdinalIgnoreCase)
            || field.Name.Contains("Colour", StringComparison.OrdinalIgnoreCase)
            || field.Name.Contains("Tint", StringComparison.OrdinalIgnoreCase);

        if (isColor)
        {
            return CreateColorDisplay(context, field, vec);
        }

        return CreateVector4Display(context, field, vec);
    }

    private static Entity CreateVector4Display(PropertyDrawerContext context, FieldInfo field, Vector4 vec)
    {
        // Create container for X, Y, Z, W labels
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Vector4_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // X component
        CreateComponentLabel(context, container, "X", vec.X, new Vector4(0.9f, 0.3f, 0.3f, 1f));

        // Y component
        CreateComponentLabel(context, container, "Y", vec.Y, new Vector4(0.3f, 0.9f, 0.3f, 1f));

        // Z component
        CreateComponentLabel(context, container, "Z", vec.Z, new Vector4(0.3f, 0.5f, 0.9f, 1f));

        // W component
        CreateComponentLabel(context, container, "W", vec.W, new Vector4(0.7f, 0.7f, 0.7f, 1f));

        return container;
    }

    private static Entity CreateColorDisplay(PropertyDrawerContext context, FieldInfo field, Vector4 color)
    {
        // Create container with color swatch and hex value
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Color_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 8
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // Color swatch
        _ = WidgetFactory.CreatePanel(
            context.EditorWorld,
            container,
            $"Swatch_{field.Name}",
            new PanelConfig(
                Width: 16,
                Height: 16,
                BackgroundColor: color
            ));

        // Hex value
        var r = (int)(color.X * 255);
        var g = (int)(color.Y * 255);
        var b = (int)(color.Z * 255);
        var a = (int)(color.W * 255);
        var hexValue = a < 255
            ? $"#{r:X2}{g:X2}{b:X2}{a:X2}"
            : $"#{r:X2}{g:X2}{b:X2}";

        WidgetFactory.CreateLabel(
            context.EditorWorld,
            container,
            $"Hex_{field.Name}",
            hexValue,
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));

        return container;
    }

    private static void CreateComponentLabel(PropertyDrawerContext context, Entity parent, string label, float value, Vector4 labelColor)
    {
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Label_{label}",
            label,
            context.Font,
            new LabelConfig(
                FontSize: 10,
                TextColor: labelColor,
                HorizontalAlign: TextAlignH.Left
            ));

        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Value_{label}",
            value.ToString("F2"),
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));
    }
}

/// <summary>
/// Property drawer for Quaternion fields, displayed as Euler angles.
/// </summary>
public sealed class QuaternionDrawer : PropertyDrawer
{
    /// <inheritdoc/>
    public override Type TargetType => typeof(Quaternion);

    /// <inheritdoc/>
    public override Entity CreateUI(PropertyDrawerContext context, FieldInfo field, object? value)
    {
        var quat = value is Quaternion q ? q : Quaternion.Identity;

        // Convert quaternion to Euler angles for display
        var euler = QuaternionToEuler(quat);

        // Create container for X, Y, Z (Euler angles)
        var container = WidgetFactory.CreatePanel(
            context.EditorWorld,
            context.Parent,
            $"Quaternion_{field.Name}",
            new PanelConfig(
                Direction: LayoutDirection.Horizontal,
                MainAxisAlign: LayoutAlign.End,
                CrossAxisAlign: LayoutAlign.Center,
                Spacing: 4
            ));

        ref var containerRect = ref context.EditorWorld.Get<UIRect>(container);
        containerRect.HeightMode = UISizeMode.FitContent;

        // X (Pitch) - red
        CreateComponentLabel(context, container, "X", euler.X, new Vector4(0.9f, 0.3f, 0.3f, 1f));

        // Y (Yaw) - green
        CreateComponentLabel(context, container, "Y", euler.Y, new Vector4(0.3f, 0.9f, 0.3f, 1f));

        // Z (Roll) - blue
        CreateComponentLabel(context, container, "Z", euler.Z, new Vector4(0.3f, 0.5f, 0.9f, 1f));

        return container;
    }

    private static void CreateComponentLabel(PropertyDrawerContext context, Entity parent, string label, float value, Vector4 labelColor)
    {
        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Label_{label}",
            label,
            context.Font,
            new LabelConfig(
                FontSize: 10,
                TextColor: labelColor,
                HorizontalAlign: TextAlignH.Left
            ));

        WidgetFactory.CreateLabel(
            context.EditorWorld,
            parent,
            $"Value_{label}",
            $"{value:F1}°",
            context.Font,
            new LabelConfig(
                FontSize: 11,
                TextColor: EditorColors.TextWhite,
                HorizontalAlign: TextAlignH.Right
            ));
    }

    private static Vector3 QuaternionToEuler(Quaternion q)
    {
        // Convert quaternion to Euler angles in degrees
        var sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        var cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
        var roll = MathF.Atan2(sinr_cosp, cosr_cosp);

        var sinp = 2 * (q.W * q.Y - q.Z * q.X);
        float pitch;
        if (MathF.Abs(sinp) >= 1)
        {
            pitch = MathF.CopySign(MathF.PI / 2, sinp);
        }
        else
        {
            pitch = MathF.Asin(sinp);
        }

        var siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        var cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
        var yaw = MathF.Atan2(siny_cosp, cosy_cosp);

        // Convert to degrees
        const float radToDeg = 180f / MathF.PI;
        return new Vector3(pitch * radToDeg, yaw * radToDeg, roll * radToDeg);
    }
}
