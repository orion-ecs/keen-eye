namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Contains the result of text shaping along with metadata about the process.
/// </summary>
/// <remarks>
/// <para>
/// This struct provides detailed information about how text was shaped,
/// including the output text, detected scripts, and text direction information.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var shaper = new BidirectionalTextShaper();
/// ShapingResult result = shaper.ShapeWithInfo("Hello مرحبا World", locale);
///
/// Console.WriteLine(result.ShapedText);        // Properly ordered text
/// Console.WriteLine(result.ContainsRtl);       // true
/// Console.WriteLine(result.IsMixedDirection);  // true
/// </code>
/// </example>
/// <param name="ShapedText">The shaped output text.</param>
/// <param name="OriginalText">The original input text.</param>
/// <param name="BaseDirection">The base text direction.</param>
/// <param name="ContainsRtl">Whether the text contains RTL characters.</param>
/// <param name="IsMixedDirection">Whether the text has mixed directions.</param>
/// <param name="DetectedScripts">The scripts detected in the text.</param>
public readonly record struct ShapingResult(
    string ShapedText,
    string OriginalText,
    TextDirection BaseDirection,
    bool ContainsRtl,
    bool IsMixedDirection,
    IReadOnlyList<ScriptType> DetectedScripts)
{
    /// <summary>
    /// Creates a simple shaping result for text that required no shaping.
    /// </summary>
    /// <param name="text">The unchanged text.</param>
    /// <param name="direction">The text direction.</param>
    /// <returns>A simple shaping result.</returns>
    public static ShapingResult Unchanged(string text, TextDirection direction = TextDirection.LeftToRight)
    {
        return new ShapingResult(
            ShapedText: text,
            OriginalText: text,
            BaseDirection: direction,
            ContainsRtl: direction == TextDirection.RightToLeft,
            IsMixedDirection: false,
            DetectedScripts: []);
    }
}
