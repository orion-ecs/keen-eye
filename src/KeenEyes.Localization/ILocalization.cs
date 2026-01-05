namespace KeenEyes.Localization;

/// <summary>
/// Provides localization services for retrieving translated strings.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the primary API for accessing localized strings in the application.
/// It handles locale management, string retrieval, and format string substitution.
/// </para>
/// <para>
/// Obtain an instance via <c>world.Localization</c> after installing the
/// <see cref="LocalizationPlugin"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get a simple string
/// string title = localization.Get("game.title");
///
/// // Get a formatted string with substitutions
/// string greeting = localization.Format("greeting", player.Name);
/// // If "greeting" = "Hello, {0}!" and player.Name = "Alex"
/// // Returns "Hello, Alex!"
///
/// // Change the active locale
/// localization.SetLocale(Locale.JapaneseJP);
/// </code>
/// </example>
public interface ILocalization
{
    /// <summary>
    /// Gets the currently active locale.
    /// </summary>
    Locale CurrentLocale { get; }

    /// <summary>
    /// Gets all locales that have registered string sources.
    /// </summary>
    IEnumerable<Locale> AvailableLocales { get; }

    /// <summary>
    /// Gets a localized string for the specified key.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns>
    /// The localized string, or a fallback value based on <see cref="LocalizationConfig.MissingKeyBehavior"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If the key is not found in the current locale, the system will attempt
    /// fallback resolution based on the configuration:
    /// </para>
    /// <list type="number">
    /// <item><description>Check custom fallback overrides</description></item>
    /// <item><description>Try language-only locale (e.g., "en-US" → "en")</description></item>
    /// <item><description>Try the default locale</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// string startButton = localization.Get("menu.start");
    /// </code>
    /// </example>
    string Get(string key);

    /// <summary>
    /// Gets a localized string and formats it with the specified arguments.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="args">Arguments to substitute into the format string.</param>
    /// <returns>The formatted localized string.</returns>
    /// <remarks>
    /// <para>
    /// The localized string should contain standard .NET format placeholders like {0}, {1}, etc.
    /// These will be replaced with the corresponding arguments.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // If "score.display" = "Score: {0} / {1}"
    /// string text = localization.Format("score.display", currentScore, maxScore);
    /// // Returns "Score: 150 / 1000"
    /// </code>
    /// </example>
    string Format(string key, params object?[] args);

    /// <summary>
    /// Sets the active locale for subsequent string lookups.
    /// </summary>
    /// <param name="locale">The locale to activate.</param>
    /// <remarks>
    /// <para>
    /// Changing the locale will trigger a <see cref="LocaleChangedEvent"/> that systems
    /// can listen to for updating UI text.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// localization.SetLocale(Locale.JapaneseJP);
    /// </code>
    /// </example>
    void SetLocale(Locale locale);

    /// <summary>
    /// Attempts to get a localized string for the specified key.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="value">When successful, contains the localized string.</param>
    /// <returns><c>true</c> if the string was found; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// This method does not apply fallback behavior or missing key handling.
    /// Use this when you need to know definitively whether a key exists.
    /// </remarks>
    bool TryGet(string key, out string? value);

    /// <summary>
    /// Adds a string source to the localization system.
    /// </summary>
    /// <param name="source">The string source to add.</param>
    /// <remarks>
    /// Sources are queried in the order they were added. Later sources can
    /// override earlier ones for the same keys.
    /// </remarks>
    void AddSource(IStringSource source);

    /// <summary>
    /// Removes a string source from the localization system.
    /// </summary>
    /// <param name="source">The string source to remove.</param>
    /// <returns><c>true</c> if the source was removed; otherwise, <c>false</c>.</returns>
    bool RemoveSource(IStringSource source);

    /// <summary>
    /// Checks if a translation exists for the specified key in the current locale.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <returns><c>true</c> if the key exists; otherwise, <c>false</c>.</returns>
    bool HasKey(string key);
}
