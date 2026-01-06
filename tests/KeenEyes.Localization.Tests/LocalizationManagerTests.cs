namespace KeenEyes.Localization.Tests;

public class LocalizationManagerTests : IDisposable
{
    private readonly World world;

    public LocalizationManagerTests()
    {
        world = new World();
    }

    public void Dispose()
    {
        world.Dispose();
    }

    private LocalizationManager CreateManager(LocalizationConfig? config = null)
    {
        config ??= LocalizationConfig.Default;
        return new LocalizationManager(config, world);
    }

    #region CurrentLocale

    [Fact]
    public void CurrentLocale_DefaultConfig_ReturnsDefaultLocale()
    {
        using var manager = CreateManager();

        manager.CurrentLocale.ShouldBe(Locale.EnglishUS);
    }

    [Fact]
    public void CurrentLocale_CustomDefault_ReturnsConfiguredLocale()
    {
        var config = new LocalizationConfig { DefaultLocale = Locale.JapaneseJP };
        using var manager = CreateManager(config);

        manager.CurrentLocale.ShouldBe(Locale.JapaneseJP);
    }

    #endregion

    #region SetLocale

    [Fact]
    public void SetLocale_ChangesCurrentLocale()
    {
        using var manager = CreateManager();

        manager.SetLocale(Locale.JapaneseJP);

        manager.CurrentLocale.ShouldBe(Locale.JapaneseJP);
    }

    [Fact]
    public void SetLocale_PublishesEvent()
    {
        using var testWorld = new World();
        var config = LocalizationConfig.Default;
        using var manager = new LocalizationManager(config, testWorld);

        LocaleChangedEvent? receivedEvent = null;
        testWorld.Subscribe<LocaleChangedEvent>(e => receivedEvent = e);

        manager.SetLocale(Locale.JapaneseJP);

        receivedEvent.ShouldNotBeNull();
        receivedEvent.Value.PreviousLocale.ShouldBe(Locale.EnglishUS);
        receivedEvent.Value.NewLocale.ShouldBe(Locale.JapaneseJP);
    }

    [Fact]
    public void SetLocale_SameLocale_DoesNotPublishEvent()
    {
        using var testWorld = new World();
        var config = LocalizationConfig.Default;
        using var manager = new LocalizationManager(config, testWorld);

        int eventCount = 0;
        testWorld.Subscribe<LocaleChangedEvent>(_ => eventCount++);

        manager.SetLocale(Locale.EnglishUS); // Same as default

        eventCount.ShouldBe(0);
    }

    #endregion

    #region AddSource and RemoveSource

    [Fact]
    public void AddSource_IncreasesSourceCount()
    {
        using var manager = CreateManager();
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>());

        manager.AddSource(source);

        manager.SourceCount.ShouldBe(1);
    }

    [Fact]
    public void RemoveSource_DecreasesSourceCount()
    {
        using var manager = CreateManager();
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>());

        manager.AddSource(source);
        manager.RemoveSource(source);

        manager.SourceCount.ShouldBe(0);
    }

    [Fact]
    public void RemoveSource_NotAdded_ReturnsFalse()
    {
        using var manager = CreateManager();
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>());

        manager.RemoveSource(source).ShouldBeFalse();
    }

    #endregion

    #region Get - Basic

    [Fact]
    public void Get_ExistingKey_ReturnsValue()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        var value = manager.Get("greeting");

        value.ShouldBe("Hello");
    }

    [Fact]
    public void Get_MissingKey_ReturnKey_ReturnsKeyItself()
    {
        var config = new LocalizationConfig { MissingKeyBehavior = MissingKeyBehavior.ReturnKey };
        using var manager = CreateManager(config);

        var value = manager.Get("missing.key");

        value.ShouldBe("missing.key");
    }

    [Fact]
    public void Get_MissingKey_ReturnEmpty_ReturnsEmptyString()
    {
        var config = new LocalizationConfig { MissingKeyBehavior = MissingKeyBehavior.ReturnEmpty };
        using var manager = CreateManager(config);

        var value = manager.Get("missing.key");

        value.ShouldBe(string.Empty);
    }

    [Fact]
    public void Get_MissingKey_ReturnPlaceholder_ReturnsPlaceholder()
    {
        var config = new LocalizationConfig { MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder };
        using var manager = CreateManager(config);

        var value = manager.Get("missing.key");

        value.ShouldBe("[MISSING: missing.key]");
    }

    [Fact]
    public void Get_MissingKey_ThrowException_ThrowsKeyNotFoundException()
    {
        var config = new LocalizationConfig { MissingKeyBehavior = MissingKeyBehavior.ThrowException };
        using var manager = CreateManager(config);

        Should.Throw<KeyNotFoundException>(() => manager.Get("missing.key"));
    }

    #endregion

    #region Get - Fallback

    [Fact]
    public void Get_FallbackToLanguageOnly_ReturnsValue()
    {
        using var manager = CreateManager();

        // Add source with language-only locale "en"
        manager.AddSource(new DictionaryStringSource(new Locale("en"), new Dictionary<string, string>
        {
            ["greeting"] = "Hello from en"
        }));

        // Current locale is en-US, should fall back to "en"
        var value = manager.Get("greeting");

        value.ShouldBe("Hello from en");
    }

    [Fact]
    public void Get_FallbackToDefaultLocale_ReturnsValue()
    {
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        using var manager = CreateManager(config);

        // Add source for default locale
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello from default"
        }));

        // Switch to a locale without translations
        manager.SetLocale(Locale.JapaneseJP);

        var value = manager.Get("greeting");

        value.ShouldBe("Hello from default");
    }

    [Fact]
    public void Get_FallbackOverride_UsesFallbackLocale()
    {
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            FallbackOverrides =
            {
                [new Locale("pt-PT")] = new Locale("pt-BR")
            }
        };
        using var manager = CreateManager(config);

        // Add Brazilian Portuguese translations
        manager.AddSource(new DictionaryStringSource(new Locale("pt-BR"), new Dictionary<string, string>
        {
            ["greeting"] = "Olá do Brasil"
        }));

        // Switch to Portuguese Portugal
        manager.SetLocale(new Locale("pt-PT"));

        // Should fall back to pt-BR
        var value = manager.Get("greeting");

        value.ShouldBe("Olá do Brasil");
    }

    [Fact]
    public void Get_FallbackDisabled_DoesNotFallback()
    {
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            EnableFallback = false,
            MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder
        };
        using var manager = CreateManager(config);

        // Add source for default locale only
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        // Switch to another locale
        manager.SetLocale(Locale.JapaneseJP);

        // Should NOT fall back to en-US
        var value = manager.Get("greeting");

        value.ShouldBe("[MISSING: greeting]");
    }

    #endregion

    #region Get - Source Priority

    [Fact]
    public void Get_MultipleSourcesOverride_LaterSourceWins()
    {
        using var manager = CreateManager();

        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "First source"
        }));

        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Second source"
        }));

        var value = manager.Get("greeting");

        value.ShouldBe("Second source");
    }

    #endregion

    #region Format

    [Fact]
    public void Format_WithArgs_SubstitutesValues()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["score"] = "Score: {0} / {1}"
        }));

        var value = manager.Format("score", 150, 1000);

        value.ShouldBe("Score: 150 / 1000");
    }

    [Fact]
    public void Format_NoArgs_ReturnsTemplateUnchanged()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        var value = manager.Format("greeting");

        value.ShouldBe("Hello");
    }

    [Fact]
    public void Format_InvalidFormat_ReturnsTemplateUnchanged()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["broken"] = "Value: {0} {1}"
        }));

        // Only passing one arg when two are expected
        var value = manager.Format("broken", 42);

        // Should not throw, returns template as-is
        value.ShouldBe("Value: {0} {1}");
    }

    #endregion

    #region TryGet

    [Fact]
    public void TryGet_ExistingKey_ReturnsTrueAndValue()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        var found = manager.TryGet("greeting", out var value);

        found.ShouldBeTrue();
        value.ShouldBe("Hello");
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        using var manager = CreateManager();

        var found = manager.TryGet("missing", out var value);

        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    #endregion

    #region HasKey

    [Fact]
    public void HasKey_ExistingKey_ReturnsTrue()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        manager.HasKey("greeting").ShouldBeTrue();
    }

    [Fact]
    public void HasKey_MissingKey_ReturnsFalse()
    {
        using var manager = CreateManager();

        manager.HasKey("missing").ShouldBeFalse();
    }

    [Fact]
    public void HasKey_WithFallback_FindsInFallbackLocale()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(new Locale("en"), new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        // Current locale is en-US, key exists in fallback "en"
        manager.HasKey("greeting").ShouldBeTrue();
    }

    #endregion

    #region AvailableLocales

    [Fact]
    public void AvailableLocales_ReturnsAllLocalesFromSources()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(new Dictionary<Locale, IReadOnlyDictionary<string, string>>
        {
            [Locale.EnglishUS] = new Dictionary<string, string> { ["key"] = "value" },
            [Locale.JapaneseJP] = new Dictionary<string, string> { ["key"] = "value" }
        }));

        var locales = manager.AvailableLocales.ToList();

        locales.ShouldContain(Locale.EnglishUS);
        locales.ShouldContain(Locale.JapaneseJP);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllSources()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello"
        }));

        manager.Clear();

        manager.SourceCount.ShouldBe(0);
        manager.HasKey("greeting").ShouldBeFalse();
    }

    #endregion

    #region FormatIcu

    [Fact]
    public void FormatIcu_PluralPattern_ReturnsCorrectForm()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["items.count"] = "{count, plural, =0 {No items} =1 {One item} other {# items}}"
        }));

        var result = manager.FormatIcu("items.count", new { count = 5 });

        result.ShouldBe("5 items");
    }

    [Fact]
    public void FormatIcu_PluralZero_ReturnsZeroForm()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["items.count"] = "{count, plural, =0 {No items} =1 {One item} other {# items}}"
        }));

        var result = manager.FormatIcu("items.count", new { count = 0 });

        result.ShouldBe("No items");
    }

    [Fact]
    public void FormatIcu_PluralOne_ReturnsSingularForm()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["items.count"] = "{count, plural, =0 {No items} =1 {One item} other {# items}}"
        }));

        var result = manager.FormatIcu("items.count", new { count = 1 });

        result.ShouldBe("One item");
    }

    [Fact]
    public void FormatIcu_GenderSelect_ReturnsCorrectForm()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["player.greeting"] = "{gender, select, male {He} female {She} other {They}} found treasure!"
        }));

        var result = manager.FormatIcu("player.greeting", new { gender = "male" });

        result.ShouldBe("He found treasure!");
    }

    [Fact]
    public void FormatIcu_WithDictionary_SubstitutesValues()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["items.count"] = "{count, plural, =0 {No items} =1 {One item} other {# items}}"
        }));

        var args = new Dictionary<string, object?> { ["count"] = 3 };
        var result = manager.FormatIcu("items.count", args);

        result.ShouldBe("3 items");
    }

    [Fact]
    public void FormatIcu_TimeRemaining_ReturnsCorrectForm()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["time.remaining"] = "{minutes, plural, =0 {Less than a minute} =1 {1 minute} other {# minutes}} remaining"
        }));

        var result = manager.FormatIcu("time.remaining", new { minutes = 5 });

        result.ShouldBe("5 minutes remaining");
    }

    [Fact]
    public void FormatIcu_NullArgs_FormatsWithoutSubstitution()
    {
        using var manager = CreateManager();
        manager.AddSource(new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["greeting"] = "Hello, World!"
        }));

        var result = manager.FormatIcu("greeting", null);

        result.ShouldBe("Hello, World!");
    }

    [Fact]
    public void FormatIcu_MissingKey_ReturnsMissingKeyBehavior()
    {
        var config = new LocalizationConfig { MissingKeyBehavior = MissingKeyBehavior.ReturnPlaceholder };
        using var manager = CreateManager(config);

        var result = manager.FormatIcu("missing.key", new { count = 5 });

        result.ShouldBe("[MISSING: missing.key]");
    }

    #endregion

    #region GetFontConfig

    [Fact]
    public void GetFontConfig_ExactLocaleMatch_ReturnsFontConfig()
    {
        var fontConfig = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" };
        var config = new LocalizationConfig
        {
            FontConfigs =
            {
                [Locale.EnglishUS] = fontConfig
            }
        };
        using var manager = CreateManager(config);

        var result = manager.GetFontConfig(Locale.EnglishUS);

        result.ShouldBe(fontConfig);
    }

    [Fact]
    public void GetFontConfig_LanguageOnlyFallback_ReturnsFontConfig()
    {
        var fontConfig = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" };
        var config = new LocalizationConfig
        {
            FontConfigs =
            {
                [new Locale("en")] = fontConfig
            }
        };
        using var manager = CreateManager(config);

        // Request en-US, should fall back to "en"
        var result = manager.GetFontConfig(Locale.EnglishUS);

        result.ShouldBe(fontConfig);
    }

    [Fact]
    public void GetFontConfig_DefaultLocaleFallback_ReturnsFontConfig()
    {
        var fontConfig = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" };
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            FontConfigs =
            {
                [Locale.EnglishUS] = fontConfig
            }
        };
        using var manager = CreateManager(config);

        // Request Japanese, should fall back to default (en-US)
        var result = manager.GetFontConfig(Locale.JapaneseJP);

        result.ShouldBe(fontConfig);
    }

    [Fact]
    public void GetFontConfig_NoConfig_ReturnsNull()
    {
        var config = new LocalizationConfig();
        using var manager = CreateManager(config);

        var result = manager.GetFontConfig(Locale.EnglishUS);

        result.ShouldBeNull();
    }

    [Fact]
    public void GetCurrentFontConfig_ReturnsConfigForCurrentLocale()
    {
        var enConfig = new LocalizedFontConfig { PrimaryFont = "fonts/Roboto.ttf" };
        var jaConfig = new LocalizedFontConfig { PrimaryFont = "fonts/NotoSansJP.ttf" };
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            FontConfigs =
            {
                [Locale.EnglishUS] = enConfig,
                [Locale.JapaneseJP] = jaConfig
            }
        };
        using var manager = CreateManager(config);

        // Start with default (en-US)
        manager.GetCurrentFontConfig().ShouldBe(enConfig);

        // Switch to Japanese
        manager.SetLocale(Locale.JapaneseJP);
        manager.GetCurrentFontConfig().ShouldBe(jaConfig);
    }

    #endregion
}
