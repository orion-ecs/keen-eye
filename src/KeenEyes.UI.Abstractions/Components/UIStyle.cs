using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that defines the visual appearance of a UI element.
/// </summary>
/// <remarks>
/// <para>
/// UIStyle controls background rendering, borders, and spacing. For text appearance,
/// use <see cref="UIText"/>. For image rendering, use <see cref="UIImage"/>.
/// </para>
/// <para>
/// Colors use Vector4 in RGBA format with values from 0 to 1.
/// </para>
/// </remarks>
public struct UIStyle : IComponent
{
    /// <summary>
    /// The background color (RGBA, 0-1 range).
    /// </summary>
    public Vector4 BackgroundColor;

    /// <summary>
    /// Optional background texture.
    /// </summary>
    public TextureHandle BackgroundTexture;

    /// <summary>
    /// The border color (RGBA, 0-1 range).
    /// </summary>
    public Vector4 BorderColor;

    /// <summary>
    /// The border width in pixels (0 for no border).
    /// </summary>
    public float BorderWidth;

    /// <summary>
    /// The corner radius for rounded rectangles (0 for sharp corners).
    /// </summary>
    public float CornerRadius;

    /// <summary>
    /// Internal padding (space between border and content).
    /// </summary>
    public UIEdges Padding;

    /// <summary>
    /// Creates a style with a solid background color.
    /// </summary>
    /// <param name="color">The background color.</param>
    public static UIStyle SolidColor(Vector4 color) => new()
    {
        BackgroundColor = color,
        BackgroundTexture = TextureHandle.Invalid
    };

    /// <summary>
    /// Creates a transparent style (no background).
    /// </summary>
    public static UIStyle Transparent => new()
    {
        BackgroundColor = Vector4.Zero,
        BackgroundTexture = TextureHandle.Invalid
    };

    /// <summary>
    /// Creates a style with a border and no fill.
    /// </summary>
    /// <param name="borderColor">The border color.</param>
    /// <param name="borderWidth">The border width in pixels.</param>
    public static UIStyle BorderOnly(Vector4 borderColor, float borderWidth) => new()
    {
        BackgroundColor = Vector4.Zero,
        BackgroundTexture = TextureHandle.Invalid,
        BorderColor = borderColor,
        BorderWidth = borderWidth
    };
}
