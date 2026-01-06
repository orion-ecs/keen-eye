namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Interface for text shaping engines that handle complex script rendering.
/// </summary>
/// <remarks>
/// <para>
/// Text shaping transforms input text into properly rendered glyphs for complex scripts.
/// This includes handling contextual letter forms (Arabic), stacking diacritics (Thai, Hindi),
/// ligatures, and bidirectional text.
/// </para>
/// <para>
/// Implementations can provide different levels of support:
/// <list type="bullet">
///   <item><description><see cref="ArabicTextShaper"/> - Arabic contextual letter forms</description></item>
///   <item><description><see cref="BidirectionalTextShaper"/> - Mixed LTR/RTL text handling</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var shaper = new ArabicTextShaper();
/// string shaped = shaper.Shape("مرحبا", locale);
/// // Returns Arabic text with proper contextual letter forms
/// </code>
/// </example>
public interface ITextShaper
{
    /// <summary>
    /// Gets the script types this shaper supports.
    /// </summary>
    IEnumerable<ScriptType> SupportedScripts { get; }

    /// <summary>
    /// Determines whether this shaper can handle text in the specified script.
    /// </summary>
    /// <param name="script">The script type to check.</param>
    /// <returns><c>true</c> if this shaper supports the script; otherwise, <c>false</c>.</returns>
    bool SupportsScript(ScriptType script);

    /// <summary>
    /// Shapes the input text for proper rendering in complex scripts.
    /// </summary>
    /// <param name="text">The input text to shape.</param>
    /// <param name="locale">The locale for context-sensitive shaping rules.</param>
    /// <returns>The shaped text ready for rendering.</returns>
    /// <remarks>
    /// <para>
    /// For Arabic text, this converts isolated letter forms to their proper contextual
    /// forms (initial, medial, final) based on position in the word.
    /// </para>
    /// <para>
    /// For bidirectional text, this reorders characters according to the Unicode
    /// Bidirectional Algorithm (UBA).
    /// </para>
    /// </remarks>
    string Shape(string text, Locale locale);

    /// <summary>
    /// Shapes the input text and returns detailed shaping information.
    /// </summary>
    /// <param name="text">The input text to shape.</param>
    /// <param name="locale">The locale for context-sensitive shaping rules.</param>
    /// <returns>A <see cref="ShapingResult"/> containing the shaped text and metadata.</returns>
    ShapingResult ShapeWithInfo(string text, Locale locale);
}
