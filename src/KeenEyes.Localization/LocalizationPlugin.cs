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
/// <item><description>Resolving locale-specific assets (textures, audio, fonts)</description></item>
/// </list>
/// <para>
/// After installation, access the localization API via <c>world.Localization</c>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Install the plugin with asset and font configuration
/// world.InstallPlugin(new LocalizationPlugin(new LocalizationConfig
/// {
///     DefaultLocale = Locale.EnglishUS,
///     MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder,
///     AssetRootPath = "Assets",
///     FontConfigs =
///     {
///         [Locale.EnglishUS] = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" },
///         [Locale.JapaneseJP] = new LocalizedFontConfig
///         {
///             PrimaryFont = "fonts/NotoSansJP.ttf",
///             FallbackFonts = ["fonts/Roboto.ttf"]
///         }
///     }
/// }));
///
/// // Load translations
/// var source = JsonStringSource.FromFile("locales/en-US.json", Locale.EnglishUS);
/// world.Localization.AddSource(source);
///
/// // Get localized strings
/// string title = world.Localization.Get("game.title");
///
/// // Create entities with localized assets
/// world.Spawn()
///     .With(new LocalizedAsset { AssetKey = "textures/logo" })
///     .Build();
///
/// // Change locale - text and assets update automatically
/// world.Localization.SetLocale(Locale.JapaneseJP);
/// </code>
/// </example>
public sealed class LocalizationPlugin : IWorldPlugin
{
    private readonly LocalizationConfig config;
    private LocalizationManager? localizationManager;
    private LocalizedAssetResolver? assetResolver;

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
        context.RegisterComponent<LocalizedAsset>();

        // Create and expose the localization manager
        localizationManager = new LocalizationManager(config, context.World);
        context.SetExtension(localizationManager);

        // Create and expose the asset resolver
        assetResolver = new LocalizedAssetResolver(config.AssetRootPath ?? string.Empty, config);
        context.SetExtension<ILocalizedAssetResolver>(assetResolver);

        // Register systems
        context.AddSystem<LocalizedTextSystem>(SystemPhase.Update, order: 100);
        context.AddSystem<LocalizedAssetSystem>(SystemPhase.EarlyUpdate, order: -50);
    }

    /// <inheritdoc />
    public void Uninstall(IPluginContext context)
    {
        // Remove extensions
        context.RemoveExtension<LocalizationManager>();
        context.RemoveExtension<ILocalizedAssetResolver>();

        // Dispose manager
        localizationManager?.Dispose();
        localizationManager = null;

        assetResolver = null;

        // Systems are cleaned up automatically by the framework
    }
}
