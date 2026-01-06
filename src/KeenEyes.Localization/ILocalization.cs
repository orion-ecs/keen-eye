using System.Diagnostics.CodeAnalysis;

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
    /// Gets a localized string and formats it using ICU MessageFormat syntax.
    /// </summary>
    /// <param name="key">The translation key.</param>
    /// <param name="args">
    /// An object containing named arguments for substitution.
    /// Can be an anonymous object, a dictionary, or any object with readable properties.
    /// </param>
    /// <returns>The formatted localized string.</returns>
    /// <remarks>
    /// <para>
    /// ICU MessageFormat supports complex localization patterns including:
    /// </para>
    /// <list type="bullet">
    /// <item><description><b>Pluralization</b> - <c>{count, plural, =0 {No items} =1 {One item} other {# items}}</c></description></item>
    /// <item><description><b>Gender</b> - <c>{gender, select, male {He} female {She} other {They}}</c></description></item>
    /// <item><description><b>Select</b> - <c>{type, select, admin {Administrator} user {User} other {Guest}}</c></description></item>
    /// </list>
    /// <para>
    /// The <c>#</c> symbol in plural patterns is replaced with the actual numeric value.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // JSON translation file:
    /// // {
    /// //   "items.count": "{count, plural, =0 {No items} =1 {One item} other {# items}}",
    /// //   "player.greeting": "{gender, select, male {He} female {She} other {They}} found treasure!"
    /// // }
    ///
    /// localization.FormatIcu("items.count", new { count = 5 });    // "5 items"
    /// localization.FormatIcu("items.count", new { count = 1 });    // "One item"
    /// localization.FormatIcu("items.count", new { count = 0 });    // "No items"
    /// localization.FormatIcu("player.greeting", new { gender = "male" });  // "He found treasure!"
    /// </code>
    /// </example>
    [RequiresUnreferencedCode("Uses reflection to access properties on anonymous objects. For AOT, use Dictionary<string, object> instead.")]
    string FormatIcu(string key, object? args);

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

    /// <summary>
    /// Gets the font configuration for a specific locale.
    /// </summary>
    /// <param name="locale">The locale to get font configuration for.</param>
    /// <returns>
    /// The font configuration for the locale, or null if no specific configuration exists.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If no font configuration exists for the exact locale, the system will try:
    /// </para>
    /// <list type="number">
    /// <item><description>The exact locale</description></item>
    /// <item><description>The language-only locale (e.g., "en-US" → "en")</description></item>
    /// <item><description>The default locale's configuration</description></item>
    /// </list>
    /// </remarks>
    LocalizedFontConfig? GetFontConfig(Locale locale);

    /// <summary>
    /// Gets the font configuration for the current locale.
    /// </summary>
    /// <returns>
    /// The font configuration for the current locale, or null if no specific configuration exists.
    /// </returns>
    LocalizedFontConfig? GetCurrentFontConfig();

    /// <summary>
    /// Preloads all assets for a specific locale for seamless switching.
    /// </summary>
    /// <param name="locale">The locale to preload assets for.</param>
    /// <param name="assetKeys">The asset keys to preload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when all assets are preloaded.</returns>
    /// <remarks>
    /// <para>
    /// Call this method before switching to a locale to ensure all localized assets
    /// are loaded and ready, preventing loading stutters during locale changes.
    /// </para>
    /// <para>
    /// This method requires the world to have the Assets plugin installed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Preload Japanese assets before switching
    /// await localization.PreloadLocaleAssetsAsync(
    ///     Locale.JapaneseJP,
    ///     ["textures/logo", "textures/menu_background", "audio/intro_voice"]);
    ///
    /// // Now switch locale - assets are ready
    /// localization.SetLocale(Locale.JapaneseJP);
    /// </code>
    /// </example>
    Task PreloadLocaleAssetsAsync(
        Locale locale,
        IEnumerable<string> assetKeys,
        CancellationToken cancellationToken = default);
}
