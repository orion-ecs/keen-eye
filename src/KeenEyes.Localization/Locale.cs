namespace KeenEyes.Localization;

/// <summary>
/// Represents a locale identifier using IETF language tags (e.g., "en-US", "ja-JP").
/// </summary>
/// <remarks>
/// <para>
/// A locale identifies a specific language and optional region for localization.
/// This struct follows IETF BCP 47 language tag conventions.
/// </para>
/// <para>
/// Common examples:
/// </para>
/// <list type="bullet">
/// <item><description>"en" - English (generic)</description></item>
/// <item><description>"en-US" - English (United States)</description></item>
/// <item><description>"en-GB" - English (United Kingdom)</description></item>
/// <item><description>"ja-JP" - Japanese (Japan)</description></item>
/// <item><description>"zh-CN" - Chinese (Simplified, China)</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var locale = new Locale("en-US");
/// Console.WriteLine(locale.Language); // "en"
/// Console.WriteLine(locale.Region);   // "US"
///
/// // Use predefined locales
/// var english = Locale.EnglishUS;
/// var japanese = Locale.JapaneseJP;
/// </code>
/// </example>
/// <param name="Code">The IETF language tag (e.g., "en-US", "ja-JP").</param>
public readonly record struct Locale(string Code)
{
    /// <summary>
    /// Gets the language portion of the locale (e.g., "en" from "en-US").
    /// </summary>
    public string Language => Code.Contains('-')
        ? Code[..Code.IndexOf('-')]
        : Code;

    /// <summary>
    /// Gets the region portion of the locale, or null if no region is specified.
    /// </summary>
    /// <example>
    /// Returns "US" for "en-US", or null for "en".
    /// </example>
    public string? Region => Code.Contains('-')
        ? Code[(Code.IndexOf('-') + 1)..]
        : null;

    /// <summary>
    /// Returns true if this locale has a region component.
    /// </summary>
    public bool HasRegion => Code.Contains('-');

    /// <summary>
    /// Returns the language-only version of this locale.
    /// </summary>
    /// <remarks>
    /// Useful for fallback resolution. For "en-US" returns "en", for "en" returns "en".
    /// </remarks>
    public Locale LanguageOnly => new(Language);

    /// <summary>
    /// English (United States) - "en-US".
    /// </summary>
    public static Locale EnglishUS => new("en-US");

    /// <summary>
    /// English (United Kingdom) - "en-GB".
    /// </summary>
    public static Locale EnglishGB => new("en-GB");

    /// <summary>
    /// Japanese (Japan) - "ja-JP".
    /// </summary>
    public static Locale JapaneseJP => new("ja-JP");

    /// <summary>
    /// Chinese Simplified (China) - "zh-CN".
    /// </summary>
    public static Locale ChineseSimplified => new("zh-CN");

    /// <summary>
    /// Chinese Traditional (Taiwan) - "zh-TW".
    /// </summary>
    public static Locale ChineseTraditional => new("zh-TW");

    /// <summary>
    /// Korean (Korea) - "ko-KR".
    /// </summary>
    public static Locale KoreanKR => new("ko-KR");

    /// <summary>
    /// German (Germany) - "de-DE".
    /// </summary>
    public static Locale GermanDE => new("de-DE");

    /// <summary>
    /// French (France) - "fr-FR".
    /// </summary>
    public static Locale FrenchFR => new("fr-FR");

    /// <summary>
    /// Spanish (Spain) - "es-ES".
    /// </summary>
    public static Locale SpanishES => new("es-ES");

    /// <summary>
    /// Portuguese (Brazil) - "pt-BR".
    /// </summary>
    public static Locale PortugueseBR => new("pt-BR");

    /// <summary>
    /// Italian (Italy) - "it-IT".
    /// </summary>
    public static Locale ItalianIT => new("it-IT");

    /// <summary>
    /// Russian (Russia) - "ru-RU".
    /// </summary>
    public static Locale RussianRU => new("ru-RU");

    /// <summary>
    /// Returns the locale code as a string.
    /// </summary>
    public override string ToString() => Code;

    /// <summary>
    /// Implicitly converts a string to a Locale.
    /// </summary>
    public static implicit operator Locale(string code) => new(code);
}
