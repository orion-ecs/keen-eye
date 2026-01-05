namespace KeenEyes.Localization;

/// <summary>
/// Configuration options for the localization system.
/// </summary>
/// <remarks>
/// <para>
/// Use this configuration to customize how the localization plugin behaves,
/// including default locale settings, missing key handling, and locale fallback chains.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var config = new LocalizationConfig
/// {
///     DefaultLocale = Locale.EnglishUS,
///     MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder,
///     FallbackOverrides =
///     {
///         [new Locale("en-GB")] = new Locale("en-US"),
///         [new Locale("pt-PT")] = new Locale("pt-BR")
///     }
/// };
///
/// world.InstallPlugin(new LocalizationPlugin(config));
/// </code>
/// </example>
public sealed class LocalizationConfig
{
    /// <summary>
    /// Gets or sets the default locale to use when no locale is explicitly set.
    /// </summary>
    /// <remarks>
    /// This locale is used as the initial active locale and as the ultimate fallback
    /// when all other fallback attempts fail.
    /// </remarks>
    public Locale DefaultLocale { get; init; } = Locale.EnglishUS;

    /// <summary>
    /// Gets or sets how missing translation keys are handled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default behavior is <see cref="Localization.MissingKeyBehavior.ReturnKey"/>,
    /// which returns the key itself. This is useful during development.
    /// </para>
    /// <para>
    /// For production, consider using <see cref="Localization.MissingKeyBehavior.ReturnPlaceholder"/>
    /// to make missing translations visible without breaking the UI.
    /// </para>
    /// </remarks>
    public MissingKeyBehavior MissingKeyBehavior { get; init; } = MissingKeyBehavior.ReturnKey;

    /// <summary>
    /// Gets the custom fallback locale mappings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, locales fall back to their language-only variant first
    /// (e.g., "en-US" falls back to "en"), then to the default locale.
    /// </para>
    /// <para>
    /// Use this dictionary to override the fallback chain for specific locales.
    /// For example, you might want "pt-PT" to fall back to "pt-BR" instead of "pt".
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// config.FallbackOverrides[new Locale("en-GB")] = new Locale("en-US");
    /// // Now "en-GB" will fall back to "en-US" instead of "en"
    /// </code>
    /// </example>
    public Dictionary<Locale, Locale> FallbackOverrides { get; init; } = [];

    /// <summary>
    /// Gets or sets whether to enable automatic fallback resolution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When enabled (default), the system automatically tries fallback locales
    /// when a key is not found in the current locale:
    /// </para>
    /// <list type="number">
    /// <item><description>Check FallbackOverrides for a custom fallback</description></item>
    /// <item><description>Try the language-only locale (e.g., "en-US" → "en")</description></item>
    /// <item><description>Try the default locale</description></item>
    /// </list>
    /// <para>
    /// Disable this to only check the current locale without any fallback.
    /// </para>
    /// </remarks>
    public bool EnableFallback { get; init; } = true;

    /// <summary>
    /// Gets the default configuration with sensible defaults for development.
    /// </summary>
    public static LocalizationConfig Default => new();

    /// <summary>
    /// Validates the configuration and returns an error message if invalid.
    /// </summary>
    /// <returns>Null if valid, otherwise an error message describing the problem.</returns>
    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(DefaultLocale.Code))
        {
            return "DefaultLocale must have a valid locale code";
        }

        return null;
    }
}
