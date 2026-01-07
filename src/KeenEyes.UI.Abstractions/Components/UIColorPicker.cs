using System.Numerics;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Specifies the color picker mode.
/// </summary>
public enum ColorPickerMode : byte
{
    /// <summary>
    /// HSV (Hue, Saturation, Value) mode with color wheel.
    /// </summary>
    HSV = 0,

    /// <summary>
    /// RGB (Red, Green, Blue) mode with sliders.
    /// </summary>
    RGB = 1,

    /// <summary>
    /// Both HSV and RGB modes available with tabs.
    /// </summary>
    Both = 2
}

/// <summary>
/// Component for color picker widgets.
/// </summary>
/// <remarks>
/// <para>
/// The UIColorPicker component provides interactive color selection through
/// either HSV (color wheel + value slider) or RGB (three sliders) modes.
/// </para>
/// <para>
/// Color values are stored internally in both HSV and RGBA formats for
/// efficient conversion and display.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var picker = world.Spawn()
///     .With(new UIElement { Visible = true })
///     .With(new UIRect { Size = new Vector2(250, 300) })
///     .With(new UIColorPicker(new Vector4(1f, 0f, 0f, 1f))
///     {
///         Mode = ColorPickerMode.HSV,
///         ShowAlpha = true
///     })
///     .Build();
/// </code>
/// </example>
/// <param name="color">The initial color in RGBA format (0-1 range).</param>
public struct UIColorPicker(Vector4 color) : IComponent
{
    /// <summary>
    /// The color picker mode (HSV, RGB, or Both).
    /// </summary>
    public ColorPickerMode Mode = ColorPickerMode.HSV;

    /// <summary>
    /// The current color in RGBA format (0-1 range).
    /// </summary>
    public Vector4 Color = color;

    /// <summary>
    /// The current hue value (0-360 degrees).
    /// </summary>
    public float Hue = RgbToHue(color);

    /// <summary>
    /// The current saturation value (0-1).
    /// </summary>
    public float Saturation = RgbToSaturation(color);

    /// <summary>
    /// The current value/brightness (0-1).
    /// </summary>
    public float Value = RgbToValue(color);

    /// <summary>
    /// Whether to show the alpha slider.
    /// </summary>
    public bool ShowAlpha = true;

    /// <summary>
    /// Whether to show the hex input field.
    /// </summary>
    public bool ShowHexInput = true;

    /// <summary>
    /// Entity reference to the color preview panel.
    /// </summary>
    public Entity PreviewEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the hue slider.
    /// </summary>
    public Entity HueSliderEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the saturation-value area (HSV mode).
    /// </summary>
    public Entity SatValAreaEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the alpha slider.
    /// </summary>
    public Entity AlphaSliderEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the R slider (RGB mode).
    /// </summary>
    public Entity RedSliderEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the G slider (RGB mode).
    /// </summary>
    public Entity GreenSliderEntity = Entity.Null;

    /// <summary>
    /// Entity reference to the B slider (RGB mode).
    /// </summary>
    public Entity BlueSliderEntity = Entity.Null;

    /// <summary>
    /// Converts RGB color to hue (0-360).
    /// </summary>
    private static float RgbToHue(Vector4 color)
    {
        float r = color.X;
        float g = color.Y;
        float b = color.Z;

        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float delta = max - min;

        if (delta < 0.0001f)
        {
            return 0f;
        }

        float hue;
        // S1244 disabled: exact equality is intentional here - comparing against same computed max value
#pragma warning disable S1244
        if (max == r)
        {
            hue = 60f * (((g - b) / delta) % 6);
        }
        else if (max == g)
        {
            hue = 60f * (((b - r) / delta) + 2);
        }
        else
        {
            hue = 60f * (((r - g) / delta) + 4);
        }
#pragma warning restore S1244

        if (hue < 0)
        {
            hue += 360f;
        }

        return hue;
    }

    /// <summary>
    /// Converts RGB color to saturation (0-1).
    /// </summary>
    private static float RgbToSaturation(Vector4 color)
    {
        float max = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
        float min = MathF.Min(color.X, MathF.Min(color.Y, color.Z));

        if (max < 0.0001f)
        {
            return 0f;
        }

        return (max - min) / max;
    }

    /// <summary>
    /// Converts RGB color to value/brightness (0-1).
    /// </summary>
    private static float RgbToValue(Vector4 color)
    {
        return MathF.Max(color.X, MathF.Max(color.Y, color.Z));
    }
}

/// <summary>
/// Component for the saturation-value selection area in HSV mode.
/// </summary>
/// <param name="colorPicker">The parent color picker entity.</param>
public struct UIColorSatValArea(Entity colorPicker) : IComponent
{
    /// <summary>
    /// The parent color picker entity.
    /// </summary>
    public Entity ColorPicker = colorPicker;

    /// <summary>
    /// Whether the user is currently dragging in the area.
    /// </summary>
    public bool IsDragging = false;
}

/// <summary>
/// Component for color picker slider elements (hue, alpha, or RGB).
/// </summary>
/// <param name="colorPicker">The parent color picker entity.</param>
/// <param name="channel">The color channel this slider controls.</param>
public struct UIColorSlider(Entity colorPicker, ColorChannel channel) : IComponent
{
    /// <summary>
    /// The parent color picker entity.
    /// </summary>
    public Entity ColorPicker = colorPicker;

    /// <summary>
    /// The color channel this slider controls.
    /// </summary>
    public ColorChannel Channel = channel;
}

/// <summary>
/// Specifies which color channel a slider controls.
/// </summary>
public enum ColorChannel : byte
{
    /// <summary>
    /// Hue channel (0-360).
    /// </summary>
    Hue = 0,

    /// <summary>
    /// Alpha/opacity channel (0-1).
    /// </summary>
    Alpha = 1,

    /// <summary>
    /// Red channel (0-1).
    /// </summary>
    Red = 2,

    /// <summary>
    /// Green channel (0-1).
    /// </summary>
    Green = 3,

    /// <summary>
    /// Blue channel (0-1).
    /// </summary>
    Blue = 4
}

/// <summary>
/// Event raised when the color picker value changes.
/// </summary>
/// <param name="Entity">The color picker entity.</param>
/// <param name="OldColor">The previous color value.</param>
/// <param name="NewColor">The new color value.</param>
public readonly record struct UIColorChangedEvent(
    Entity Entity,
    Vector4 OldColor,
    Vector4 NewColor);
