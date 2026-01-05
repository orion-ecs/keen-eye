namespace KeenEyes.Localization.Tests;

public class LocalizationPluginTests
{
    #region Plugin Installation

    [Fact]
    public void Install_RegistersLocalizationExtension()
    {
        using var world = new World();
        var plugin = new LocalizationPlugin();

        world.InstallPlugin(plugin);

        world.TryGetExtension<LocalizationManager>(out var manager).ShouldBeTrue();
        manager.ShouldNotBeNull();
    }

    [Fact]
    public void Install_DefaultConfig_UsesEnglishUS()
    {
        using var world = new World();
        var plugin = new LocalizationPlugin();

        world.InstallPlugin(plugin);

        var localization = world.GetExtension<LocalizationManager>();
        localization.CurrentLocale.ShouldBe(Locale.EnglishUS);
    }

    [Fact]
    public void Install_CustomConfig_UsesConfiguredLocale()
    {
        using var world = new World();
        var config = new LocalizationConfig { DefaultLocale = Locale.JapaneseJP };
        var plugin = new LocalizationPlugin(config);

        world.InstallPlugin(plugin);

        var localization = world.GetExtension<LocalizationManager>();
        localization.CurrentLocale.ShouldBe(Locale.JapaneseJP);
    }

    #endregion

    #region Plugin Uninstallation

    [Fact]
    public void Uninstall_RemovesLocalizationExtension()
    {
        using var world = new World();
        var plugin = new LocalizationPlugin();
        world.InstallPlugin(plugin);

        world.UninstallPlugin<LocalizationPlugin>();

        world.TryGetExtension<LocalizationManager>(out _).ShouldBeFalse();
    }

    #endregion

    #region Plugin Name

    [Fact]
    public void Name_ReturnsLocalization()
    {
        var plugin = new LocalizationPlugin();

        plugin.Name.ShouldBe("Localization");
    }

    #endregion

    #region Configuration Validation

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => new LocalizationPlugin(null!));
    }

    [Fact]
    public void Constructor_InvalidConfig_ThrowsArgumentException()
    {
        var config = new LocalizationConfig { DefaultLocale = new Locale("") };

        Should.Throw<ArgumentException>(() => new LocalizationPlugin(config));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullWorkflow_GetAndSetLocale()
    {
        using var world = new World();
        var plugin = new LocalizationPlugin(new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS
        });
        world.InstallPlugin(plugin);

        var localization = world.GetExtension<LocalizationManager>();

        // Add translations
        localization.AddSource(new DictionaryStringSource(new Dictionary<Locale, IReadOnlyDictionary<string, string>>
        {
            [Locale.EnglishUS] = new Dictionary<string, string>
            {
                ["greeting"] = "Hello",
                ["farewell"] = "Goodbye"
            },
            [Locale.JapaneseJP] = new Dictionary<string, string>
            {
                ["greeting"] = "こんにちは",
                ["farewell"] = "さようなら"
            }
        }));

        // Test English
        localization.Get("greeting").ShouldBe("Hello");
        localization.Get("farewell").ShouldBe("Goodbye");

        // Switch to Japanese
        localization.SetLocale(Locale.JapaneseJP);

        // Test Japanese
        localization.Get("greeting").ShouldBe("こんにちは");
        localization.Get("farewell").ShouldBe("さようなら");
    }

    [Fact]
    public void FullWorkflow_FormatStrings()
    {
        using var world = new World();
        world.InstallPlugin(new LocalizationPlugin());

        var localization = world.GetExtension<LocalizationManager>();
        localization.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["player.score"] = "Player {0}: {1} points",
            ["items.collected"] = "You collected {0} item(s)"
        }));

        localization.Format("player.score", "Alice", 1500).ShouldBe("Player Alice: 1500 points");
        localization.Format("items.collected", 5).ShouldBe("You collected 5 item(s)");
    }

    [Fact]
    public void FullWorkflow_FallbackChain()
    {
        using var world = new World();
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            FallbackOverrides =
            {
                [new Locale("en-AU")] = Locale.EnglishGB
            }
        };
        world.InstallPlugin(new LocalizationPlugin(config));

        var localization = world.GetExtension<LocalizationManager>();

        // en-US has all keys
        localization.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["color"] = "color",
            ["greeting"] = "Hello"
        }));

        // en-GB has British spelling
        localization.AddSource(new DictionaryStringSource(Locale.EnglishGB, new Dictionary<string, string>
        {
            ["color"] = "colour"
        }));

        // Test Australian English fallback chain: en-AU -> en-GB -> en -> en-US
        localization.SetLocale(new Locale("en-AU"));

        // "color" should fall back to en-GB (via FallbackOverrides)
        localization.Get("color").ShouldBe("colour");

        // "greeting" should fall back to en-US (default)
        localization.Get("greeting").ShouldBe("Hello");
    }

    #endregion
}
