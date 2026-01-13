using System.Numerics;

using KeenEyes.UI.Abstractions;

namespace KeenEyes.UI;

/// <summary>
/// System that handles color picker interaction and color conversion.
/// </summary>
/// <remarks>
/// <para>
/// This system manages:
/// <list type="bullet">
/// <item>HSV color wheel/area interactions</item>
/// <item>RGB slider interactions</item>
/// <item>Alpha slider interactions</item>
/// <item>HSV to RGB and RGB to HSV conversions</item>
/// <item>Color preview updates</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UIColorPickerSystem : SystemBase
{
    private EventSubscription? dragSubscription;
    private EventSubscription? clickSubscription;

    /// <inheritdoc />
    protected override void OnInitialize()
    {
        // Subscribe to drag events for saturation-value area
        dragSubscription = World.Subscribe<UIDragEvent>(OnDrag);
        clickSubscription = World.Subscribe<UIClickEvent>(OnClick);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            dragSubscription?.Dispose();
            clickSubscription?.Dispose();
            dragSubscription = null;
            clickSubscription = null;
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override void Update(float deltaTime)
    {
        // Most work is event-driven, but we can update visual state here if needed
    }

    private void OnClick(UIClickEvent evt)
    {
        // Handle sat-val area click
        if (World.Has<UIColorSatValArea>(evt.Element))
        {
            HandleSatValClick(evt.Element, evt.Position);
            return;
        }

        // Handle color slider click
        if (World.Has<UIColorSlider>(evt.Element))
        {
            HandleSliderClick(evt.Element, evt.Position);
        }
    }

    private void OnDrag(UIDragEvent evt)
    {
        // Handle sat-val area drag
        if (World.Has<UIColorSatValArea>(evt.Element))
        {
            HandleSatValClick(evt.Element, evt.Position);
            return;
        }

        // Handle color slider drag
        if (World.Has<UIColorSlider>(evt.Element))
        {
            HandleSliderClick(evt.Element, evt.Position);
        }
    }

    private void HandleSatValClick(Entity satValEntity, Vector2 position)
    {
        ref readonly var satValArea = ref World.Get<UIColorSatValArea>(satValEntity);
        var pickerEntity = satValArea.ColorPicker;

        if (!World.IsAlive(pickerEntity) || !World.Has<UIColorPicker>(pickerEntity))
        {
            return;
        }

        // Get the bounds of the sat-val area
        if (!World.Has<UIRect>(satValEntity))
        {
            return;
        }

        ref readonly var rect = ref World.Get<UIRect>(satValEntity);
        var bounds = rect.ComputedBounds;

        // Calculate saturation and value from position
        float saturation = Math.Clamp((position.X - bounds.X) / bounds.Width, 0f, 1f);
        float value = Math.Clamp(1f - (position.Y - bounds.Y) / bounds.Height, 0f, 1f);

        ref var picker = ref World.Get<UIColorPicker>(pickerEntity);
        var oldColor = picker.Color;

        picker.Saturation = saturation;
        picker.Value = value;
        picker.Color = HsvToRgb(picker.Hue, picker.Saturation, picker.Value, picker.Color.W);

        UpdatePreview(pickerEntity, ref picker);
        UpdateSliderVisuals(pickerEntity, ref picker);

        if (oldColor != picker.Color)
        {
            World.Send(new UIColorChangedEvent(pickerEntity, oldColor, picker.Color));
        }
    }

    private void HandleSliderClick(Entity sliderEntity, Vector2 position)
    {
        ref readonly var slider = ref World.Get<UIColorSlider>(sliderEntity);
        var pickerEntity = slider.ColorPicker;

        if (!World.IsAlive(pickerEntity) || !World.Has<UIColorPicker>(pickerEntity))
        {
            return;
        }

        // Get the bounds of the slider
        if (!World.Has<UIRect>(sliderEntity))
        {
            return;
        }

        ref readonly var rect = ref World.Get<UIRect>(sliderEntity);
        var bounds = rect.ComputedBounds;

        // Calculate value from position (horizontal slider assumed)
        float normalizedValue = Math.Clamp((position.X - bounds.X) / bounds.Width, 0f, 1f);

        ref var picker = ref World.Get<UIColorPicker>(pickerEntity);
        var oldColor = picker.Color;

        switch (slider.Channel)
        {
            case ColorChannel.Hue:
                picker.Hue = normalizedValue * 360f;
                picker.Color = HsvToRgb(picker.Hue, picker.Saturation, picker.Value, picker.Color.W);
                break;

            case ColorChannel.Alpha:
                picker.Color = new Vector4(picker.Color.X, picker.Color.Y, picker.Color.Z, normalizedValue);
                break;

            case ColorChannel.Red:
                picker.Color = new Vector4(normalizedValue, picker.Color.Y, picker.Color.Z, picker.Color.W);
                UpdateHsvFromRgb(ref picker);
                break;

            case ColorChannel.Green:
                picker.Color = new Vector4(picker.Color.X, normalizedValue, picker.Color.Z, picker.Color.W);
                UpdateHsvFromRgb(ref picker);
                break;

            case ColorChannel.Blue:
                picker.Color = new Vector4(picker.Color.X, picker.Color.Y, normalizedValue, picker.Color.W);
                UpdateHsvFromRgb(ref picker);
                break;
        }

        UpdatePreview(pickerEntity, ref picker);
        UpdateSliderVisuals(pickerEntity, ref picker);

        if (oldColor != picker.Color)
        {
            World.Send(new UIColorChangedEvent(pickerEntity, oldColor, picker.Color));
        }
    }

    private void UpdatePreview(Entity pickerEntity, ref UIColorPicker picker)
    {
        if (World.IsAlive(picker.PreviewEntity) && World.Has<UIStyle>(picker.PreviewEntity))
        {
            ref var style = ref World.Get<UIStyle>(picker.PreviewEntity);
            style.BackgroundColor = picker.Color;
        }
    }

    private void UpdateSliderVisuals(Entity pickerEntity, ref UIColorPicker picker)
    {
        // Update the saturation-value area background color when hue changes
        // The background should show the pure hue at max saturation/value
        foreach (var satValEntity in World.Query<UIColorSatValArea>())
        {
            ref readonly var satValArea = ref World.Get<UIColorSatValArea>(satValEntity);
            if (satValArea.ColorPicker != pickerEntity)
            {
                continue;
            }

            if (World.Has<UIStyle>(satValEntity))
            {
                ref var style = ref World.Get<UIStyle>(satValEntity);
                // Background shows pure hue at full saturation and value
                style.BackgroundColor = HsvToRgb(picker.Hue, 1f, 1f);
            }

            break;
        }

        // Note: Slider thumb positioning would require thumb entities to be created
        // and tracked in the widget factory. Currently sliders are simple track
        // backgrounds without explicit thumb children.
    }

    private static void UpdateHsvFromRgb(ref UIColorPicker picker)
    {
        picker.Hue = RgbToHue(picker.Color);
        picker.Saturation = RgbToSaturation(picker.Color);
        picker.Value = RgbToValue(picker.Color);
    }

    /// <summary>
    /// Sets the color of a color picker.
    /// </summary>
    /// <param name="entity">The color picker entity.</param>
    /// <param name="color">The new color in RGBA format.</param>
    public void SetColor(Entity entity, Vector4 color)
    {
        if (!World.IsAlive(entity) || !World.Has<UIColorPicker>(entity))
        {
            return;
        }

        ref var picker = ref World.Get<UIColorPicker>(entity);
        var oldColor = picker.Color;

        picker.Color = color;
        UpdateHsvFromRgb(ref picker);
        UpdatePreview(entity, ref picker);

        if (oldColor != color)
        {
            World.Send(new UIColorChangedEvent(entity, oldColor, color));
        }
    }

    /// <summary>
    /// Gets the current color from a color picker.
    /// </summary>
    /// <param name="entity">The color picker entity.</param>
    /// <returns>The current color in RGBA format, or transparent black if invalid.</returns>
    public Vector4 GetColor(Entity entity)
    {
        if (!World.IsAlive(entity) || !World.Has<UIColorPicker>(entity))
        {
            return Vector4.Zero;
        }

        return World.Get<UIColorPicker>(entity).Color;
    }

    /// <summary>
    /// Converts HSV color to RGB.
    /// </summary>
    /// <param name="hue">Hue in degrees (0-360).</param>
    /// <param name="saturation">Saturation (0-1).</param>
    /// <param name="value">Value/brightness (0-1).</param>
    /// <param name="alpha">Alpha (0-1).</param>
    /// <returns>The RGBA color.</returns>
    public static Vector4 HsvToRgb(float hue, float saturation, float value, float alpha = 1f)
    {
        float c = value * saturation;
        float x = c * (1 - MathF.Abs((hue / 60f % 2) - 1));
        float m = value - c;

        float r, g, b;

        if (hue < 60)
        {
            r = c; g = x; b = 0;
        }
        else if (hue < 120)
        {
            r = x; g = c; b = 0;
        }
        else if (hue < 180)
        {
            r = 0; g = c; b = x;
        }
        else if (hue < 240)
        {
            r = 0; g = x; b = c;
        }
        else if (hue < 300)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return new Vector4(r + m, g + m, b + m, alpha);
    }

    /// <summary>
    /// Converts RGB color to hue (0-360).
    /// </summary>
    public static float RgbToHue(Vector4 color)
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
        if (MathF.Abs(max - r) < 0.0001f)
        {
            hue = 60f * (((g - b) / delta) % 6);
        }
        else if (MathF.Abs(max - g) < 0.0001f)
        {
            hue = 60f * (((b - r) / delta) + 2);
        }
        else
        {
            hue = 60f * (((r - g) / delta) + 4);
        }

        if (hue < 0)
        {
            hue += 360f;
        }

        return hue;
    }

    /// <summary>
    /// Converts RGB color to saturation (0-1).
    /// </summary>
    public static float RgbToSaturation(Vector4 color)
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
    public static float RgbToValue(Vector4 color)
    {
        return MathF.Max(color.X, MathF.Max(color.Y, color.Z));
    }

    /// <summary>
    /// Converts a color to hex string format (e.g., "#FF0000" or "#FF0000FF" with alpha).
    /// </summary>
    /// <param name="color">The color to convert.</param>
    /// <param name="includeAlpha">Whether to include the alpha channel.</param>
    /// <returns>The hex color string.</returns>
    public static string ColorToHex(Vector4 color, bool includeAlpha = false)
    {
        int r = (int)(color.X * 255);
        int g = (int)(color.Y * 255);
        int b = (int)(color.Z * 255);
        int a = (int)(color.W * 255);

        if (includeAlpha)
        {
            return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
        }
        else
        {
            return $"#{r:X2}{g:X2}{b:X2}";
        }
    }

    /// <summary>
    /// Parses a hex color string to a Vector4 color.
    /// </summary>
    /// <param name="hex">The hex string (with or without #, 6 or 8 characters).</param>
    /// <param name="color">The parsed color.</param>
    /// <returns>True if parsing succeeded.</returns>
    public static bool TryParseHex(string hex, out Vector4 color)
    {
        color = Vector4.Zero;

        if (string.IsNullOrEmpty(hex))
        {
            return false;
        }

        // Remove # prefix if present
        if (hex.StartsWith('#'))
        {
            hex = hex[1..];
        }

        // Validate length
        if (hex.Length != 6 && hex.Length != 8)
        {
            return false;
        }

        try
        {
            int r = Convert.ToInt32(hex[..2], 16);
            int g = Convert.ToInt32(hex[2..4], 16);
            int b = Convert.ToInt32(hex[4..6], 16);
            int a = hex.Length == 8 ? Convert.ToInt32(hex[6..8], 16) : 255;

            color = new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
