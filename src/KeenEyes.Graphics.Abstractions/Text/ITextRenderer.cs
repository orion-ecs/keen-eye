using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Horizontal text alignment options.
/// </summary>
public enum TextAlignH
{
    /// <summary>Align text to the left edge.</summary>
    Left,

    /// <summary>Center text horizontally.</summary>
    Center,

    /// <summary>Align text to the right edge.</summary>
    Right
}

/// <summary>
/// Vertical text alignment options.
/// </summary>
public enum TextAlignV
{
    /// <summary>Align text to the top.</summary>
    Top,

    /// <summary>Center text vertically.</summary>
    Middle,

    /// <summary>Align text to the bottom.</summary>
    Bottom,

    /// <summary>Align text to the baseline.</summary>
    Baseline
}

/// <summary>
/// Text rendering style options.
/// </summary>
[Flags]
public enum TextStyle
{
    /// <summary>Normal text rendering.</summary>
    Normal = 0,

    /// <summary>Bold text (if supported by font).</summary>
    Bold = 1,

    /// <summary>Italic text (if supported by font).</summary>
    Italic = 2,

    /// <summary>Underlined text.</summary>
    Underline = 4,

    /// <summary>Strikethrough text.</summary>
    Strikethrough = 8
}

/// <summary>
/// Interface for rendering text to the screen.
/// </summary>
/// <remarks>
/// <para>
/// The text renderer provides high-level text rendering with support for:
/// <list type="bullet">
///   <item><description>Multiple fonts and sizes</description></item>
///   <item><description>Horizontal and vertical alignment</description></item>
///   <item><description>Text wrapping within bounds</description></item>
///   <item><description>Color and styling</description></item>
/// </list>
/// </para>
/// <para>
/// Text rendering is typically batched for performance. Use <see cref="Begin()"/>
/// and <see cref="End"/> to group text rendering calls.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// textRenderer.Begin();
///
/// // Simple text at position
/// textRenderer.DrawText(font, "Hello World", 100, 100, Colors.White);
///
/// // Centered text
/// textRenderer.DrawText(font, "Centered", screenWidth / 2, 50, Colors.White,
///     alignH: TextAlignH.Center);
///
/// // Word-wrapped text in a box
/// textRenderer.DrawTextWrapped(font, longText, new Rectangle(10, 10, 200, 400), Colors.White);
///
/// textRenderer.End();
/// </code>
/// </example>
public interface ITextRenderer : IDisposable
{
    #region Batch Control

    /// <summary>
    /// Begins a text rendering batch.
    /// </summary>
    void Begin();

    /// <summary>
    /// Begins a text rendering batch with a custom projection matrix.
    /// </summary>
    /// <param name="projection">The projection matrix to use.</param>
    void Begin(in Matrix4x4 projection);

    /// <summary>
    /// Ends the current batch and flushes to the GPU.
    /// </summary>
    void End();

    /// <summary>
    /// Flushes the current batch without ending it.
    /// </summary>
    void Flush();

    #endregion

    #region Text Rendering

    /// <summary>
    /// Draws text at the specified position.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="color">The text color.</param>
    /// <param name="alignH">Horizontal alignment relative to the position.</param>
    /// <param name="alignV">Vertical alignment relative to the position.</param>
    void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    /// <summary>
    /// Draws text at the specified position with a scale factor.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="color">The text color.</param>
    /// <param name="scale">The scale factor (1.0 = normal size).</param>
    /// <param name="alignH">Horizontal alignment relative to the position.</param>
    /// <param name="alignV">Vertical alignment relative to the position.</param>
    void DrawText(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float scale,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    /// <summary>
    /// Draws text at the specified position with rotation.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The X coordinate of the rotation origin.</param>
    /// <param name="y">The Y coordinate of the rotation origin.</param>
    /// <param name="color">The text color.</param>
    /// <param name="rotation">The rotation angle in radians.</param>
    /// <param name="alignH">Horizontal alignment relative to the origin.</param>
    /// <param name="alignV">Vertical alignment relative to the origin.</param>
    void DrawTextRotated(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 color,
        float rotation,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    /// <summary>
    /// Draws text with automatic word wrapping within a bounding rectangle.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="bounds">The bounding rectangle for text layout.</param>
    /// <param name="color">The text color.</param>
    /// <param name="alignH">Horizontal alignment within the bounds.</param>
    /// <param name="alignV">Vertical alignment within the bounds.</param>
    void DrawTextWrapped(
        FontHandle font,
        ReadOnlySpan<char> text,
        in Rectangle bounds,
        Vector4 color,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    /// <summary>
    /// Draws text with an outline effect.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="textColor">The main text color.</param>
    /// <param name="outlineColor">The outline color.</param>
    /// <param name="outlineWidth">The outline width in pixels.</param>
    /// <param name="alignH">Horizontal alignment relative to the position.</param>
    /// <param name="alignV">Vertical alignment relative to the position.</param>
    void DrawTextOutlined(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 outlineColor,
        float outlineWidth = 1f,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    /// <summary>
    /// Draws text with a drop shadow effect.
    /// </summary>
    /// <param name="font">The font to use.</param>
    /// <param name="text">The text to render.</param>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="textColor">The main text color.</param>
    /// <param name="shadowColor">The shadow color.</param>
    /// <param name="shadowOffset">The shadow offset in pixels.</param>
    /// <param name="alignH">Horizontal alignment relative to the position.</param>
    /// <param name="alignV">Vertical alignment relative to the position.</param>
    void DrawTextShadowed(
        FontHandle font,
        ReadOnlySpan<char> text,
        float x,
        float y,
        Vector4 textColor,
        Vector4 shadowColor,
        Vector2 shadowOffset,
        TextAlignH alignH = TextAlignH.Left,
        TextAlignV alignV = TextAlignV.Top);

    #endregion
}
