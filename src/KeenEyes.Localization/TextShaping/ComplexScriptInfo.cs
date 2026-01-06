namespace KeenEyes.Localization.TextShaping;

/// <summary>
/// Provides information about complex script requirements for different languages.
/// </summary>
/// <remarks>
/// <para>
/// Complex scripts like Thai and Devanagari (Hindi) require special handling for proper
/// text rendering. This class provides information about what features are needed.
/// </para>
/// <para>
/// For full rendering support, these scripts typically require:
/// <list type="bullet">
///   <item><description>OpenType font with appropriate GSUB/GPOS tables</description></item>
///   <item><description>HarfBuzz or similar text shaping library integration</description></item>
///   <item><description>Proper handling of combining characters and diacritics</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var info = ComplexScriptInfo.GetInfo(ScriptType.Thai);
/// if (info.RequiresShaping)
/// {
///     Console.WriteLine($"Thai text requires: {string.Join(", ", info.ShapingFeatures)}");
/// }
/// </code>
/// </example>
public sealed class ComplexScriptInfo
{
    private ComplexScriptInfo(
        ScriptType script,
        string scriptName,
        string[] languages,
        bool requiresShaping,
        string[] shapingFeatures,
        string? notes)
    {
        Script = script;
        ScriptName = scriptName;
        Languages = languages;
        RequiresShaping = requiresShaping;
        ShapingFeatures = shapingFeatures;
        Notes = notes;
    }

    /// <summary>
    /// Gets the script type.
    /// </summary>
    public ScriptType Script { get; }

    /// <summary>
    /// Gets the human-readable name of the script.
    /// </summary>
    public string ScriptName { get; }

    /// <summary>
    /// Gets the languages that use this script.
    /// </summary>
    public string[] Languages { get; }

    /// <summary>
    /// Gets whether this script requires text shaping for proper rendering.
    /// </summary>
    public bool RequiresShaping { get; }

    /// <summary>
    /// Gets the OpenType features required for proper rendering.
    /// </summary>
    public string[] ShapingFeatures { get; }

    /// <summary>
    /// Gets additional notes about rendering this script.
    /// </summary>
    public string? Notes { get; }

    /// <summary>
    /// Gets information about a specific script type.
    /// </summary>
    /// <param name="script">The script type to get information for.</param>
    /// <returns>Information about the script, or null if unknown.</returns>
    public static ComplexScriptInfo? GetInfo(ScriptType script) => script switch
    {
        ScriptType.Arabic => arabicInfo,
        ScriptType.Hebrew => hebrewInfo,
        ScriptType.Thai => thaiInfo,
        ScriptType.Devanagari => devanagariInfo,
        ScriptType.Tamil => tamilInfo,
        ScriptType.Bengali => bengaliInfo,
        ScriptType.CJK => cjkInfo,
        ScriptType.Hangul => hangulInfo,
        _ => null
    };

    /// <summary>
    /// Gets information about the script used by a locale.
    /// </summary>
    /// <param name="locale">The locale to get script information for.</param>
    /// <returns>Information about the primary script used by the locale.</returns>
    public static ComplexScriptInfo? GetInfoForLocale(Locale locale)
    {
        var script = GetScriptForLanguage(locale.Language);
        return script.HasValue ? GetInfo(script.Value) : null;
    }

    /// <summary>
    /// Determines the primary script used by a language code.
    /// </summary>
    /// <param name="language">The ISO 639-1 language code.</param>
    /// <returns>The script type, or null if unknown.</returns>
    public static ScriptType? GetScriptForLanguage(string language) => language switch
    {
        "ar" => ScriptType.Arabic,
        "fa" => ScriptType.Arabic, // Persian uses Arabic script
        "ur" => ScriptType.Arabic, // Urdu uses Arabic script
        "he" => ScriptType.Hebrew,
        "yi" => ScriptType.Hebrew, // Yiddish uses Hebrew script
        "th" => ScriptType.Thai,
        "hi" => ScriptType.Devanagari,
        "mr" => ScriptType.Devanagari, // Marathi
        "ne" => ScriptType.Devanagari, // Nepali
        "sa" => ScriptType.Devanagari, // Sanskrit
        "ta" => ScriptType.Tamil,
        "bn" => ScriptType.Bengali,
        "zh" => ScriptType.CJK,
        "ja" => ScriptType.CJK, // Japanese uses multiple scripts but CJK for main text
        "ko" => ScriptType.Hangul,
        _ => null
    };

    private static readonly ComplexScriptInfo arabicInfo = new(
        ScriptType.Arabic,
        "Arabic",
        ["Arabic", "Persian", "Urdu", "Pashto", "Kurdish"],
        requiresShaping: true,
        ["init", "medi", "fina", "isol", "liga", "rlig", "calt"],
        "Arabic requires contextual letter forms (initial, medial, final, isolated) and right-to-left layout. " +
        "Built-in ArabicTextShaper provides basic support. For complex Arabic typography, integrate HarfBuzz.");

    private static readonly ComplexScriptInfo hebrewInfo = new(
        ScriptType.Hebrew,
        "Hebrew",
        ["Hebrew", "Yiddish"],
        requiresShaping: true,
        ["fina", "liga"],
        "Hebrew requires right-to-left layout. Some letters have final forms. " +
        "Simpler shaping requirements than Arabic.");

    private static readonly ComplexScriptInfo thaiInfo = new(
        ScriptType.Thai,
        "Thai",
        ["Thai"],
        requiresShaping: true,
        ["ccmp", "liga", "mark", "mkmk"],
        "Thai has stacking diacritics (tone marks, vowel signs) that appear above or below consonants. " +
        "Requires proper handling of combining characters and glyph positioning. " +
        "For proper rendering, use a font with Thai OpenType tables and HarfBuzz integration.");

    private static readonly ComplexScriptInfo devanagariInfo = new(
        ScriptType.Devanagari,
        "Devanagari",
        ["Hindi", "Marathi", "Nepali", "Sanskrit"],
        requiresShaping: true,
        ["akhn", "rphf", "blwf", "half", "pstf", "vatu", "cjct", "pres", "abvs", "blws", "psts", "haln", "calt"],
        "Devanagari has conjunct consonants (consonant clusters) formed using the virama (halant). " +
        "Complex rendering with above and below marks. Requires HarfBuzz for proper rendering.");

    private static readonly ComplexScriptInfo tamilInfo = new(
        ScriptType.Tamil,
        "Tamil",
        ["Tamil"],
        requiresShaping: true,
        ["akhn", "pref", "blwf", "pstf", "abvs", "blws", "psts", "haln", "calt"],
        "Tamil has special character combinations and diacritics. " +
        "Similar complexity to Devanagari. Requires HarfBuzz for proper rendering.");

    private static readonly ComplexScriptInfo bengaliInfo = new(
        ScriptType.Bengali,
        "Bengali",
        ["Bengali", "Assamese"],
        requiresShaping: true,
        ["akhn", "rphf", "blwf", "half", "pstf", "vatu", "cjct", "pres", "abvs", "blws", "psts", "haln", "calt"],
        "Bengali has conjunct consonants similar to Devanagari. " +
        "Complex shaping requirements. Requires HarfBuzz for proper rendering.");

    private static readonly ComplexScriptInfo cjkInfo = new(
        ScriptType.CJK,
        "CJK (Chinese/Japanese/Korean)",
        ["Chinese", "Japanese", "Korean"],
        requiresShaping: false,
        ["vert", "vrt2"],
        "CJK ideographs generally don't require complex shaping, but may need: " +
        "vertical text layout support, full-width punctuation, and proper line breaking. " +
        "Japanese may need ruby text (furigana) support for kanji readings.");

    private static readonly ComplexScriptInfo hangulInfo = new(
        ScriptType.Hangul,
        "Hangul",
        ["Korean"],
        requiresShaping: false,
        ["ccmp", "ljmo", "vjmo", "tjmo"],
        "Modern Hangul uses precomposed syllable blocks. " +
        "Old Hangul or text with individual jamo may need composition. " +
        "Generally simpler than other complex scripts.");
}
