namespace KeenEyes.Localization;

/// <summary>
/// Configuration for locale-specific font settings.
/// </summary>
/// <remarks>
/// <para>
/// Different languages require different fonts to display correctly. For example,
/// Japanese text requires fonts with CJK character support, while Latin scripts
/// can use standard Western fonts.
/// </para>
/// <para>
/// This configuration allows specifying primary and fallback fonts for each locale,
/// ensuring text renders correctly regardless of the active language.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fontConfig = new LocalizedFontConfig
/// {
///     PrimaryFont = "fonts/NotoSansJP-Regular.ttf",
///     FallbackFonts = ["fonts/Roboto-Regular.ttf", "fonts/NotoEmoji-Regular.ttf"]
/// };
///
/// var locConfig = new LocalizationConfig
/// {
///     FontConfigs =
///     {
///         [Locale.EnglishUS] = new() { PrimaryFont = "fonts/Roboto-Regular.ttf" },
///         [Locale.JapaneseJP] = fontConfig
///     }
/// };
/// </code>
/// </example>
public sealed class LocalizedFontConfig
{
    /// <summary>
    /// The primary font asset path for this locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the main font used to render text in this locale. It should support
    /// all characters commonly used in the language.
    /// </para>
    /// <para>
    /// The path is relative to the asset root directory.
    /// </para>
    /// </remarks>
    public string? PrimaryFont { get; init; }

    /// <summary>
    /// Fallback font asset paths, checked in order when the primary font
    /// doesn't contain a required glyph.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Fallback fonts are used when the primary font doesn't contain a character.
    /// This is useful for:
    /// </para>
    /// <list type="bullet">
    /// <item><description>Emoji support (most fonts don't include emoji)</description></item>
    /// <item><description>Mixed-script text (e.g., Japanese text with Western names)</description></item>
    /// <item><description>Special symbols and punctuation</description></item>
    /// </list>
    /// <para>
    /// Fonts are checked in array order until a glyph is found.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var config = new LocalizedFontConfig
    /// {
    ///     PrimaryFont = "fonts/NotoSansJP-Regular.ttf",
    ///     FallbackFonts =
    ///     [
    ///         "fonts/Roboto-Regular.ttf",      // For Western characters
    ///         "fonts/NotoEmoji-Regular.ttf"    // For emoji
    ///     ]
    /// };
    /// </code>
    /// </example>
    public string[]? FallbackFonts { get; init; }

    /// <summary>
    /// Optional font size multiplier for this locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some scripts require different sizing to maintain readability. For example,
    /// CJK characters may need to be slightly larger than Latin characters at the
    /// same nominal point size.
    /// </para>
    /// <para>
    /// Default is 1.0 (no scaling). A value of 1.2 would make text 20% larger.
    /// </para>
    /// </remarks>
    public float SizeMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Optional line height multiplier for this locale.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Some scripts require more vertical space between lines. CJK text often
    /// benefits from increased line height compared to Latin scripts.
    /// </para>
    /// <para>
    /// Default is 1.0 (use font's default line height). A value of 1.5 would
    /// add 50% more space between lines.
    /// </para>
    /// </remarks>
    public float LineHeightMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Gets all font paths including primary and fallbacks.
    /// </summary>
    /// <returns>An enumerable of all font paths configured for this locale.</returns>
    /// <remarks>
    /// This is useful for preloading all fonts needed for a locale.
    /// </remarks>
    public IEnumerable<string> GetAllFontPaths()
    {
        if (!string.IsNullOrEmpty(PrimaryFont))
        {
            yield return PrimaryFont;
        }

        if (FallbackFonts != null)
        {
            foreach (var font in FallbackFonts)
            {
                if (!string.IsNullOrEmpty(font))
                {
                    yield return font;
                }
            }
        }
    }

    /// <summary>
    /// Gets the default font configuration with no fonts specified.
    /// </summary>
    public static LocalizedFontConfig Default => new();
}
