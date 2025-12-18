using System.Numerics;
using KeenEyes.Graphics.Abstractions;

namespace KeenEyes.UI.Abstractions;

/// <summary>
/// Component that renders text within a UI element.
/// </summary>
/// <remarks>
/// <para>
/// Text rendering uses fonts loaded through the font manager. If no font is specified,
/// the system default font will be used.
/// </para>
/// <para>
/// For multiline text, enable <see cref="WordWrap"/>. The text will wrap at the
/// element's computed bounds.
/// </para>
/// </remarks>
public struct UIText : IComponent
{
    /// <summary>
    /// The text content to display.
    /// </summary>
    public string Content;

    /// <summary>
    /// The font to use for rendering.
    /// </summary>
    public FontHandle Font;

    /// <summary>
    /// The font size in pixels.
    /// </summary>
    public float FontSize;

    /// <summary>
    /// The text color (RGBA, 0-1 range).
    /// </summary>
    public Vector4 Color;

    /// <summary>
    /// Horizontal text alignment within the element bounds.
    /// </summary>
    public TextAlignH HorizontalAlign;

    /// <summary>
    /// Vertical text alignment within the element bounds.
    /// </summary>
    public TextAlignV VerticalAlign;

    /// <summary>
    /// Whether text should wrap at the element boundary.
    /// </summary>
    public bool WordWrap;

    /// <summary>
    /// How to handle text that exceeds the element bounds.
    /// </summary>
    public TextOverflow Overflow;

    /// <summary>
    /// Creates a basic text component with default styling.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    public static UIText Create(string content, float fontSize = 16f) => new()
    {
        Content = content,
        Font = FontHandle.Invalid, // Use default font
        FontSize = fontSize,
        Color = new Vector4(1, 1, 1, 1), // White
        HorizontalAlign = TextAlignH.Left,
        VerticalAlign = TextAlignV.Top,
        WordWrap = false,
        Overflow = TextOverflow.Visible
    };

    /// <summary>
    /// Creates a centered text component.
    /// </summary>
    /// <param name="content">The text content.</param>
    /// <param name="fontSize">The font size in pixels.</param>
    public static UIText Centered(string content, float fontSize = 16f) => new()
    {
        Content = content,
        Font = FontHandle.Invalid,
        FontSize = fontSize,
        Color = new Vector4(1, 1, 1, 1),
        HorizontalAlign = TextAlignH.Center,
        VerticalAlign = TextAlignV.Middle,
        WordWrap = false,
        Overflow = TextOverflow.Visible
    };
}
