using System.Numerics;

namespace KeenEyes.Graphics.Abstractions;

/// <summary>
/// Interface for font resource management and text measurement.
/// </summary>
/// <remarks>
/// <para>
/// The font manager handles loading and managing font resources, as well as
/// providing text measurement capabilities for layout purposes.
/// </para>
/// <para>
/// Fonts are loaded from files or memory and referenced using <see cref="FontHandle"/>.
/// Multiple sizes of the same font family are treated as separate resources.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load a font from file
/// var font = fontManager.LoadFont("assets/fonts/Roboto.ttf", 16);
///
/// // Measure text for layout
/// var size = fontManager.MeasureText(font, "Hello World");
/// var lineHeight = fontManager.GetLineHeight(font);
/// </code>
/// </example>
public interface IFontManager : IResourceManager<FontHandle>
{
    #region Font Loading

    /// <summary>
    /// Loads a font from a file.
    /// </summary>
    /// <param name="path">The path to the font file (TTF, OTF).</param>
    /// <param name="size">The font size in pixels.</param>
    /// <returns>The font handle.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the font file is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the font cannot be loaded.</exception>
    FontHandle LoadFont(string path, float size);

    /// <summary>
    /// Loads a font from memory.
    /// </summary>
    /// <param name="data">The font file data.</param>
    /// <param name="size">The font size in pixels.</param>
    /// <param name="name">A name for the font (for debugging).</param>
    /// <returns>The font handle.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the font data is invalid.</exception>
    FontHandle LoadFontFromMemory(ReadOnlySpan<byte> data, float size, string? name = null);

    /// <summary>
    /// Creates a font variant with a different size from an existing font.
    /// </summary>
    /// <param name="baseFont">The base font handle.</param>
    /// <param name="newSize">The new font size in pixels.</param>
    /// <returns>A new font handle for the resized font.</returns>
    /// <remarks>
    /// This may be more efficient than loading the font file again if the
    /// implementation caches font data.
    /// </remarks>
    FontHandle CreateSizedFont(FontHandle baseFont, float newSize);

    #endregion

    #region Font Metrics

    /// <summary>
    /// Gets the font size in pixels.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <returns>The font size.</returns>
    float GetFontSize(FontHandle font);

    /// <summary>
    /// Gets the line height (distance between baselines) for a font.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <returns>The line height in pixels.</returns>
    float GetLineHeight(FontHandle font);

    /// <summary>
    /// Gets the baseline offset from the top of the line.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <returns>The baseline offset in pixels.</returns>
    float GetBaseline(FontHandle font);

    /// <summary>
    /// Gets the ascent (height above baseline) for a font.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <returns>The ascent in pixels.</returns>
    float GetAscent(FontHandle font);

    /// <summary>
    /// Gets the descent (depth below baseline) for a font.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <returns>The descent in pixels (positive value).</returns>
    float GetDescent(FontHandle font);

    #endregion

    #region Text Measurement

    /// <summary>
    /// Measures the size of rendered text.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The bounding size of the rendered text.</returns>
    Vector2 MeasureText(FontHandle font, ReadOnlySpan<char> text);

    /// <summary>
    /// Measures the width of rendered text.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="text">The text to measure.</param>
    /// <returns>The width of the rendered text in pixels.</returns>
    float MeasureTextWidth(FontHandle font, ReadOnlySpan<char> text);

    /// <summary>
    /// Gets the advance width for a single character.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="character">The character to measure.</param>
    /// <returns>The advance width in pixels.</returns>
    float GetCharacterAdvance(FontHandle font, char character);

    /// <summary>
    /// Gets the kerning adjustment between two characters.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="first">The first character.</param>
    /// <param name="second">The second character.</param>
    /// <returns>The kerning adjustment in pixels (positive = move apart).</returns>
    float GetKerning(FontHandle font, char first, char second);

    /// <summary>
    /// Calculates word wrap points for text within a maximum width.
    /// </summary>
    /// <param name="font">The font handle.</param>
    /// <param name="text">The text to wrap.</param>
    /// <param name="maxWidth">The maximum line width in pixels.</param>
    /// <returns>The indices where line breaks should occur.</returns>
    IReadOnlyList<int> CalculateWordWrap(FontHandle font, ReadOnlySpan<char> text, float maxWidth);

    #endregion
}
