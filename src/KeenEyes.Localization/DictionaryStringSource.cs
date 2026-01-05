namespace KeenEyes.Localization;

/// <summary>
/// A simple string source backed by an in-memory dictionary.
/// </summary>
/// <remarks>
/// <para>
/// Use this for testing, small static string sets, or as a base for custom sources.
/// For loading from files, prefer <see cref="JsonStringSource"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary&lt;string, string&gt;
/// {
///     ["menu.start"] = "Start Game",
///     ["menu.options"] = "Options",
///     ["menu.quit"] = "Quit"
/// });
/// localization.AddSource(source);
/// </code>
/// </example>
public sealed class DictionaryStringSource : IStringSource
{
    private readonly Dictionary<Locale, Dictionary<string, string>> translations;

    /// <summary>
    /// Creates a new dictionary string source with a single locale.
    /// </summary>
    /// <param name="locale">The locale for the strings.</param>
    /// <param name="strings">The key-value translation pairs.</param>
    public DictionaryStringSource(Locale locale, IReadOnlyDictionary<string, string> strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        translations = new Dictionary<Locale, Dictionary<string, string>>
        {
            [locale] = new Dictionary<string, string>(strings)
        };
    }

    /// <summary>
    /// Creates a new dictionary string source with multiple locales.
    /// </summary>
    /// <param name="translations">A dictionary mapping locales to their translation dictionaries.</param>
    public DictionaryStringSource(IReadOnlyDictionary<Locale, IReadOnlyDictionary<string, string>> translations)
    {
        ArgumentNullException.ThrowIfNull(translations);

        this.translations = [];
        foreach (var (locale, strings) in translations)
        {
            this.translations[locale] = new Dictionary<string, string>(strings);
        }
    }

    /// <inheritdoc />
    public IEnumerable<Locale> SupportedLocales => translations.Keys;

    /// <inheritdoc />
    public bool TryGetString(Locale locale, string key, out string? value)
    {
        if (translations.TryGetValue(locale, out var strings) &&
            strings.TryGetValue(key, out value))
        {
            return true;
        }

        value = null;
        return false;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetKeys(Locale locale)
    {
        if (translations.TryGetValue(locale, out var strings))
        {
            return strings.Keys;
        }

        return [];
    }

    /// <inheritdoc />
    public bool HasLocale(Locale locale) => translations.ContainsKey(locale);
}
