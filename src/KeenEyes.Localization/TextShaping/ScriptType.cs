namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Identifies writing script types for text shaping.
/// </summary>
/// <remarks>
/// <para>
/// Script types categorize writing systems by their shaping requirements.
/// Simple scripts like Latin need minimal shaping, while complex scripts
/// like Arabic require contextual glyph selection.
/// </para>
/// </remarks>
public enum ScriptType : byte
{
    /// <summary>
    /// Unknown or unspecified script.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Latin script (English, Spanish, French, German, etc.).
    /// </summary>
    /// <remarks>
    /// Simple script with no contextual shaping requirements.
    /// </remarks>
    Latin = 1,

    /// <summary>
    /// Cyrillic script (Russian, Ukrainian, Bulgarian, etc.).
    /// </summary>
    /// <remarks>
    /// Simple script with no contextual shaping requirements.
    /// </remarks>
    Cyrillic = 2,

    /// <summary>
    /// Greek script.
    /// </summary>
    /// <remarks>
    /// Simple script with no contextual shaping requirements.
    /// </remarks>
    Greek = 3,

    /// <summary>
    /// Arabic script (Arabic, Persian, Urdu, etc.).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Complex script requiring contextual shaping. Arabic letters have
    /// different forms depending on their position in a word:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Isolated - standalone letter</description></item>
    ///   <item><description>Initial - beginning of word</description></item>
    ///   <item><description>Medial - middle of word</description></item>
    ///   <item><description>Final - end of word</description></item>
    /// </list>
    /// <para>
    /// Arabic is also a right-to-left script.
    /// </para>
    /// </remarks>
    Arabic = 4,

    /// <summary>
    /// Hebrew script.
    /// </summary>
    /// <remarks>
    /// Right-to-left script with simpler shaping than Arabic.
    /// Some letters have final forms.
    /// </remarks>
    Hebrew = 5,

    /// <summary>
    /// Thai script.
    /// </summary>
    /// <remarks>
    /// Complex script with stacking diacritics (tone marks, vowels)
    /// that appear above or below base consonants.
    /// </remarks>
    Thai = 6,

    /// <summary>
    /// Devanagari script (Hindi, Sanskrit, Marathi, etc.).
    /// </summary>
    /// <remarks>
    /// Complex script with stacking diacritics and conjunct consonants.
    /// Requires proper handling of the virama (halant) character.
    /// </remarks>
    Devanagari = 7,

    /// <summary>
    /// CJK (Chinese, Japanese, Korean) ideographs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generally simple shaping, but may require:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Vertical text layout support</description></item>
    ///   <item><description>Full-width punctuation handling</description></item>
    ///   <item><description>Ruby text (furigana) support for Japanese</description></item>
    /// </list>
    /// </remarks>
    CJK = 8,

    /// <summary>
    /// Japanese Hiragana syllabary.
    /// </summary>
    Hiragana = 9,

    /// <summary>
    /// Japanese Katakana syllabary.
    /// </summary>
    Katakana = 10,

    /// <summary>
    /// Korean Hangul syllabary.
    /// </summary>
    /// <remarks>
    /// May require syllable block composition from individual jamo.
    /// </remarks>
    Hangul = 11,

    /// <summary>
    /// Tamil script.
    /// </summary>
    /// <remarks>
    /// Complex script with stacking diacritics and special character combinations.
    /// </remarks>
    Tamil = 12,

    /// <summary>
    /// Bengali script.
    /// </summary>
    /// <remarks>
    /// Complex script similar to Devanagari with conjunct consonants.
    /// </remarks>
    Bengali = 13
}

/// <summary>
/// Provides extension methods for <see cref="ScriptType"/>.
/// </summary>
public static class ScriptTypeExtensions
{
    /// <summary>
    /// Determines whether the script uses right-to-left text direction.
    /// </summary>
    /// <param name="script">The script type to check.</param>
    /// <returns><c>true</c> if the script is RTL; otherwise, <c>false</c>.</returns>
    public static bool IsRightToLeft(this ScriptType script) => script switch
    {
        ScriptType.Arabic => true,
        ScriptType.Hebrew => true,
        _ => false
    };

    /// <summary>
    /// Determines whether the script requires contextual shaping.
    /// </summary>
    /// <param name="script">The script type to check.</param>
    /// <returns><c>true</c> if the script needs contextual shaping; otherwise, <c>false</c>.</returns>
    public static bool RequiresContextualShaping(this ScriptType script) => script switch
    {
        ScriptType.Arabic => true,
        ScriptType.Hebrew => true, // Final forms
        ScriptType.Thai => true,
        ScriptType.Devanagari => true,
        ScriptType.Tamil => true,
        ScriptType.Bengali => true,
        _ => false
    };

    /// <summary>
    /// Determines whether the script uses stacking diacritics.
    /// </summary>
    /// <param name="script">The script type to check.</param>
    /// <returns><c>true</c> if the script uses stacking marks; otherwise, <c>false</c>.</returns>
    public static bool UsesStackingDiacritics(this ScriptType script) => script switch
    {
        ScriptType.Thai => true,
        ScriptType.Devanagari => true,
        ScriptType.Tamil => true,
        ScriptType.Bengali => true,
        _ => false
    };

    /// <summary>
    /// Determines whether the script supports vertical text layout.
    /// </summary>
    /// <param name="script">The script type to check.</param>
    /// <returns><c>true</c> if the script supports vertical layout; otherwise, <c>false</c>.</returns>
    public static bool SupportsVerticalLayout(this ScriptType script) => script switch
    {
        ScriptType.CJK => true,
        ScriptType.Hiragana => true,
        ScriptType.Katakana => true,
        ScriptType.Hangul => true,
        _ => false
    };
}
