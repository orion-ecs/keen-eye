using System.Numerics;
using KeenEyes.Graph.Abstractions;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Input.Abstractions;

namespace KeenEyes.Graph.Rendering;

/// <summary>
/// Static helpers for rendering interactive widgets in node bodies.
/// </summary>
/// <remarks>
/// <para>
/// Each widget method renders its UI and handles input through the <see cref="WidgetContext"/>.
/// Methods return the height consumed by the widget, allowing sequential layout in RenderBody.
/// </para>
/// <para>
/// Widgets that need focus (text fields, dropdowns) will add/update a <see cref="WidgetFocus"/>
/// component on the canvas when clicked.
/// </para>
/// </remarks>
public static class NodeWidgets
{
    // Colors
    private static readonly Vector4 FieldBackground = new(0.1f, 0.1f, 0.1f, 1f);
    private static readonly Vector4 FieldBorder = new(0.3f, 0.3f, 0.3f, 1f);
    private static readonly Vector4 FieldBorderFocused = new(0.4f, 0.6f, 0.8f, 1f);
    private static readonly Vector4 SliderTrack = new(0.15f, 0.15f, 0.15f, 1f);
    private static readonly Vector4 SliderFill = new(0.3f, 0.5f, 0.7f, 1f);
    private static readonly Vector4 SliderHandle = new(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Vector4 DropdownArrow = new(0.6f, 0.6f, 0.6f, 1f);
    private static readonly Vector4 DropdownHover = new(0.2f, 0.3f, 0.4f, 1f);

    // Sizing constants
    private const float RowHeight = 22f;
    private const float FieldPadding = 4f;
    private const float LabelWidth = 60f;
    private const float BorderWidth = 1f;

    /// <summary>
    /// Renders an editable float field with a label.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="value">Reference to the value to edit.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="label">The label text to display.</param>
    /// <param name="widgetId">Unique ID for this widget within the node.</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float FloatField(WidgetContext ctx, ref float value, Rectangle area, string label, int widgetId)
    {
        var hasFocus = ctx.HasFocus(widgetId);
        var fieldArea = GetFieldArea(area);

        // Draw field background
        DrawFieldBackground(ctx.Renderer, fieldArea, hasFocus);

        // Handle click to focus
        if (ctx.WasClicked(fieldArea) && !hasFocus)
        {
            SetFocus(ctx, widgetId, WidgetType.FloatField, value.ToString("F3"));
        }

        // Note: Text rendering for label/value not yet available
        // Would draw label on left, value on right when ITextRenderer is implemented

        // Commit value when focus is lost
        if (!hasFocus && ctx.Focus.HasValue && ctx.Focus.Value.WidgetId == widgetId)
        {
            if (float.TryParse(ctx.Focus.Value.EditBuffer, out var parsed))
            {
                value = parsed;
            }
        }

        return RowHeight;
    }

    /// <summary>
    /// Renders an editable integer field with a label.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="value">Reference to the value to edit.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="label">The label text to display.</param>
    /// <param name="widgetId">Unique ID for this widget within the node.</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float IntField(WidgetContext ctx, ref int value, Rectangle area, string label, int widgetId)
    {
        var hasFocus = ctx.HasFocus(widgetId);
        var fieldArea = GetFieldArea(area);

        // Draw field background
        DrawFieldBackground(ctx.Renderer, fieldArea, hasFocus);

        // Handle click to focus
        if (ctx.WasClicked(fieldArea) && !hasFocus)
        {
            SetFocus(ctx, widgetId, WidgetType.IntField, value.ToString());
        }

        // Commit value when focus is lost
        if (!hasFocus && ctx.Focus.HasValue && ctx.Focus.Value.WidgetId == widgetId)
        {
            if (int.TryParse(ctx.Focus.Value.EditBuffer, out var parsed))
            {
                value = parsed;
            }
        }

        return RowHeight;
    }

    /// <summary>
    /// Renders a color picker with a color swatch.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="value">Reference to the color value to edit.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="widgetId">Unique ID for this widget within the node.</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float ColorPicker(WidgetContext ctx, ref Vector4 value, Rectangle area, int widgetId)
    {
        var hasFocus = ctx.HasFocus(widgetId);
        var swatchArea = new Rectangle(
            area.X + area.Width - RowHeight - FieldPadding,
            area.Y + 2,
            RowHeight - 4,
            RowHeight - 4);

        // Draw color swatch
        ctx.Renderer.FillRect(swatchArea.X, swatchArea.Y, swatchArea.Width, swatchArea.Height, value);
        ctx.Renderer.DrawRect(swatchArea.X, swatchArea.Y, swatchArea.Width, swatchArea.Height,
            hasFocus ? FieldBorderFocused : FieldBorder, BorderWidth);

        // Handle click to toggle picker
        if (ctx.WasClicked(swatchArea) && !hasFocus)
        {
            SetFocus(ctx, widgetId, WidgetType.ColorPicker, string.Empty, isExpanded: true);
        }

        // If expanded, draw color picker popup
        // (Full picker implementation would go here)

        return RowHeight;
    }

    /// <summary>
    /// Renders a dropdown selection with expandable options.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="selectedIndex">Reference to the selected index.</param>
    /// <param name="options">The available options.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="widgetId">Unique ID for this widget within the node.</param>
    /// <returns>The height consumed by the widget (including expanded options if shown).</returns>
    public static float Dropdown(WidgetContext ctx, ref int selectedIndex, string[] options, Rectangle area, int widgetId)
    {
        var hasFocus = ctx.HasFocus(widgetId);
        var isExpanded = hasFocus && ctx.Focus.HasValue && ctx.Focus.Value.IsExpanded;
        var fieldArea = GetFieldArea(area);

        // Draw dropdown background
        DrawFieldBackground(ctx.Renderer, fieldArea, hasFocus);

        // Draw dropdown arrow
        var arrowSize = 8f;
        var arrowX = fieldArea.Right - arrowSize - FieldPadding;
        var arrowY = fieldArea.Y + ((fieldArea.Height - arrowSize) / 2f);
        DrawDropdownArrow(ctx.Renderer, arrowX, arrowY, arrowSize, isExpanded);

        // Handle click to toggle
        if (ctx.WasClicked(fieldArea) && !hasFocus)
        {
            SetFocus(ctx, widgetId, WidgetType.Dropdown, selectedIndex.ToString(), isExpanded: true);
        }

        var height = RowHeight;

        // Draw expanded options
        if (isExpanded && options.Length > 0)
        {
            var optionY = fieldArea.Bottom + 2;
            for (int i = 0; i < options.Length; i++)
            {
                var optionArea = new Rectangle(fieldArea.X, optionY, fieldArea.Width, RowHeight);
                var isHovered = ctx.IsMouseOver(optionArea);

                // Draw option background
                var bgColor = isHovered ? DropdownHover : FieldBackground;
                ctx.Renderer.FillRect(optionArea.X, optionArea.Y, optionArea.Width, optionArea.Height, bgColor);

                // Handle option click
                if (isHovered && ctx.Mouse.IsButtonDown(MouseButton.Left))
                {
                    selectedIndex = i;
                    ctx.World.Remove<WidgetFocus>(ctx.Canvas);
                }

                optionY += RowHeight;
                height += RowHeight;
            }

            // Draw border around options
            ctx.Renderer.DrawRect(fieldArea.X, fieldArea.Bottom + 2,
                fieldArea.Width, options.Length * RowHeight,
                FieldBorder, BorderWidth);
        }

        return height;
    }

    /// <summary>
    /// Renders a draggable slider for float values.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="value">Reference to the value to edit.</param>
    /// <param name="min">Minimum value.</param>
    /// <param name="max">Maximum value.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="widgetId">Unique ID for this widget within the node.</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float Slider(WidgetContext ctx, ref float value, float min, float max, Rectangle area, int widgetId)
    {
        var hasFocus = ctx.HasFocus(widgetId);
        var fieldArea = GetFieldArea(area);

        // Clamp value to range
        value = Math.Clamp(value, min, max);

        // Draw track background
        var trackY = fieldArea.Y + ((fieldArea.Height - 6) / 2f);
        ctx.Renderer.FillRect(fieldArea.X, trackY, fieldArea.Width, 6, SliderTrack);

        // Draw filled portion
        var normalizedValue = (value - min) / (max - min);
        var fillWidth = fieldArea.Width * normalizedValue;
        ctx.Renderer.FillRect(fieldArea.X, trackY, fillWidth, 6, SliderFill);

        // Draw handle
        var handleX = fieldArea.X + fillWidth - 4;
        var handleY = fieldArea.Y + ((fieldArea.Height - 12) / 2f);
        ctx.Renderer.FillRect(handleX, handleY, 8, 12, SliderHandle);

        // Handle interaction
        if (ctx.IsMouseOver(fieldArea) && ctx.Mouse.IsButtonDown(MouseButton.Left))
        {
            if (!hasFocus)
            {
                SetFocus(ctx, widgetId, WidgetType.Slider, value.ToString(), dragStartValue: value);
            }

            // Update value based on mouse position
            var mouseX = ctx.Mouse.Position.X;
            var sliderNormalized = (mouseX - fieldArea.X) / fieldArea.Width;
            sliderNormalized = Math.Clamp(sliderNormalized, 0f, 1f);
            value = min + ((max - min) * sliderNormalized);
        }

        // Draw border
        ctx.Renderer.DrawRect(fieldArea.X, fieldArea.Y, fieldArea.Width, fieldArea.Height,
            hasFocus ? FieldBorderFocused : FieldBorder, BorderWidth);

        return RowHeight;
    }

    /// <summary>
    /// Renders a texture preview area.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="textureId">The texture ID to preview, or 0 for none.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="previewSize">The desired preview size (both width and height).</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float Preview(WidgetContext ctx, uint textureId, Rectangle area, float previewSize = 64f)
    {
        var previewArea = new Rectangle(
            area.X + ((area.Width - previewSize) / 2f),
            area.Y + FieldPadding,
            previewSize,
            previewSize);

        // Draw preview background
        ctx.Renderer.FillRect(previewArea.X, previewArea.Y, previewArea.Width, previewArea.Height, FieldBackground);

        // Draw texture if available
        if (textureId != 0)
        {
            // Texture rendering would be implemented when texture support is available
            // ctx.Renderer.DrawTexture(textureId, previewArea.X, previewArea.Y, previewArea.Width, previewArea.Height);
        }

        // Draw border
        ctx.Renderer.DrawRect(previewArea.X, previewArea.Y, previewArea.Width, previewArea.Height, FieldBorder, BorderWidth);

        return previewSize + (FieldPadding * 2);
    }

    /// <summary>
    /// Renders a labeled separator line.
    /// </summary>
    /// <param name="ctx">The widget context containing renderer and input state.</param>
    /// <param name="area">The area available for the widget.</param>
    /// <param name="label">Optional label text.</param>
    /// <returns>The height consumed by the widget.</returns>
    public static float Separator(WidgetContext ctx, Rectangle area, string? label = null)
    {
        var lineY = area.Y + (RowHeight / 2f);

        if (string.IsNullOrEmpty(label))
        {
            // Just draw a line
            ctx.Renderer.DrawLine(
                area.X + FieldPadding, lineY,
                area.Right - FieldPadding, lineY,
                FieldBorder, BorderWidth);
        }
        else
        {
            // Draw line with gap for label (would need text width measurement)
            var gapStart = area.X + LabelWidth;
            var gapEnd = area.Right - FieldPadding;

            ctx.Renderer.DrawLine(area.X + FieldPadding, lineY, gapStart - 4, lineY, FieldBorder, BorderWidth);
            ctx.Renderer.DrawLine(gapEnd + 4, lineY, area.Right - FieldPadding, lineY, FieldBorder, BorderWidth);
        }

        return RowHeight;
    }

    #region Private Helpers

    private static Rectangle GetFieldArea(Rectangle area)
    {
        return new Rectangle(
            area.X + LabelWidth + FieldPadding,
            area.Y + 2,
            area.Width - LabelWidth - (FieldPadding * 2),
            RowHeight - 4);
    }

    private static void DrawFieldBackground(I2DRenderer renderer, Rectangle area, bool hasFocus)
    {
        renderer.FillRect(area.X, area.Y, area.Width, area.Height, FieldBackground);
        renderer.DrawRect(area.X, area.Y, area.Width, area.Height,
            hasFocus ? FieldBorderFocused : FieldBorder, BorderWidth);
    }

    private static void DrawDropdownArrow(I2DRenderer renderer, float x, float y, float size, bool pointsUp)
    {
        Vector2[] triangle;
        if (pointsUp)
        {
            triangle =
            [
                new Vector2(x, y + size),
                new Vector2(x + (size / 2f), y),
                new Vector2(x + size, y + size)
            ];
        }
        else
        {
            triangle =
            [
                new Vector2(x, y),
                new Vector2(x + size, y),
                new Vector2(x + (size / 2f), y + size)
            ];
        }

        renderer.DrawPolygon(triangle, DropdownArrow, BorderWidth);
    }

    private static void SetFocus(
        WidgetContext ctx,
        int widgetId,
        WidgetType type,
        string editBuffer,
        bool isExpanded = false,
        float dragStartValue = 0f)
    {
        var focus = new WidgetFocus
        {
            Node = ctx.Node,
            WidgetId = widgetId,
            Type = type,
            EditBuffer = editBuffer,
            CursorPosition = editBuffer.Length,
            IsExpanded = isExpanded,
            DragStartValue = dragStartValue
        };

        if (ctx.World.Has<WidgetFocus>(ctx.Canvas))
        {
            ctx.World.Get<WidgetFocus>(ctx.Canvas) = focus;
        }
        else
        {
            ctx.World.Add(ctx.Canvas, focus);
        }
    }

    #endregion
}
