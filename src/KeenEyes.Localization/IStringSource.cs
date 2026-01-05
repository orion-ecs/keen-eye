namespace KeenEyes.Localization;

/// <summary>
/// Defines a source of localized strings for a specific locale.
/// </summary>
/// <remarks>
/// <para>
/// String sources provide the actual translation data for the localization system.
/// Implementations can load strings from various formats like JSON files, databases,
/// or embedded resources.
/// </para>
/// <para>
/// Each string source contains translations for one or more locales. The localization
/// manager will query registered sources in order until it finds a translation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load strings from a JSON file
/// var source = JsonStringSource.FromFile("locales/en-US.json", Locale.EnglishUS);
/// localization.AddSource(source);
///
/// // Load strings from embedded data
/// var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary&lt;string, string&gt;
/// {
///     ["menu.start"] = "Start Game",
///     ["menu.quit"] = "Quit"
/// });
/// </code>
/// </example>
public interface IStringSource
{
    /// <summary>
    /// Gets the locales that this source provides translations for.
    /// </summary>
    IEnumerable<Locale> SupportedLocales { get; }

    /// <summary>
    /// Attempts to get a localized string for the specified key and locale.
    /// </summary>
    /// <param name="locale">The locale to get the string for.</param>
    /// <param name="key">The translation key.</param>
    /// <param name="value">When successful, contains the localized string.</param>
    /// <returns><c>true</c> if the string was found; otherwise, <c>false</c>.</returns>
    bool TryGetString(Locale locale, string key, out string? value);

    /// <summary>
    /// Gets all keys available for the specified locale.
    /// </summary>
    /// <param name="locale">The locale to get keys for.</param>
    /// <returns>An enumerable of all available keys for the locale.</returns>
    IEnumerable<string> GetKeys(Locale locale);

    /// <summary>
    /// Returns true if this source contains translations for the specified locale.
    /// </summary>
    /// <param name="locale">The locale to check.</param>
    bool HasLocale(Locale locale);
}
