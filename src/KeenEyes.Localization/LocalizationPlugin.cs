namespace KeenEyes.Localization;

/// <summary>
/// Plugin that provides localization services for a world.
/// </summary>
/// <remarks>
/// <para>
/// The localization plugin enables multi-language support in your game by:
/// </para>
/// <list type="bullet">
/// <item><description>Managing locale switching and fallback chains</description></item>
/// <item><description>Loading translations from JSON files or other sources</description></item>
/// <item><description>Automatically updating UI text when the locale changes</description></item>
/// </list>
/// <para>
/// After installation, access the localization API via <c>world.Localization</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin
/// world.InstallPlugin(new LocalizationPlugin(new LocalizationConfig
/// {
///     DefaultLocale = Locale.EnglishUS,
///     MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder
/// }));
///
/// // Load translations
/// var source = JsonStringSource.FromFile("locales/en-US.json", Locale.EnglishUS);
/// world.Localization.AddSource(source);
///
/// // Get localized strings
/// string title = world.Localization.Get("game.title");
///
/// // Change locale
/// world.Localization.SetLocale(Locale.JapaneseJP);
/// </code>
/// </example>
public sealed class LocalizationPlugin : IWorldPlugin
{
    private readonly LocalizationConfig config;
    private LocalizationManager? localizationManager;

    /// <summary>
    /// Creates a new localization plugin with default configuration.
    /// </summary>
    public LocalizationPlugin() : this(LocalizationConfig.Default)
    {
    }

    /// <summary>
    /// Creates a new localization plugin with the specified configuration.
    /// </summary>
    /// <param name="config">The localization configuration.</param>
    /// <exception cref="ArgumentException">Thrown if the configuration is invalid.</exception>
    public LocalizationPlugin(LocalizationConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var error = config.Validate();
        if (error != null)
        {
            throw new ArgumentException(error, nameof(config));
        }

        this.config = config;
    }

    /// <summary>
    /// Gets the configuration for this plugin.
    /// </summary>
    public LocalizationConfig Config => config;

    /// <inheritdoc />
    public string Name => "Localization";

    /// <inheritdoc />
    public void Install(IPluginContext context)
    {
        // Register components
        context.RegisterComponent<LocalizedText>();
        context.RegisterComponent<LocalizedTextTag>();

        // Create and expose the localization manager
        localizationManager = new LocalizationManager(config, context.World);
        context.SetExtension(localizationManager);

        // Register systems
        context.AddSystem<LocalizedTextSystem>(SystemPhase.Update, order: 100);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Remove extension
        context.RemoveExtension<LocalizationManager>();

        // Dispose manager
        localizationManager?.Dispose();
        localizationManager = null;

        // Systems are cleaned up automatically by the framework
    }
}
