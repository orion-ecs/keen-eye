namespace KeenEyes.Localization.Tests;

public class DictionaryStringSourceTests
{
    #region Single Locale Construction

    [Fact]
    public void Constructor_SingleLocale_SetsSupportedLocales()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        source.SupportedLocales.ShouldContain(Locale.EnglishUS);
    }

    [Fact]
    public void Constructor_SingleLocale_StoresStrings()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2"
        });

        source.TryGetString(Locale.EnglishUS, "key1", out var value1).ShouldBeTrue();
        value1.ShouldBe("value1");

        source.TryGetString(Locale.EnglishUS, "key2", out var value2).ShouldBeTrue();
        value2.ShouldBe("value2");
    }

    #endregion

    #region Multiple Locale Construction

    [Fact]
    public void Constructor_MultipleLocales_SetsSupportedLocales()
    {
        var source = new DictionaryStringSource(new Dictionary<Locale, IReadOnlyDictionary<string, string>>
        {
            [Locale.EnglishUS] = new Dictionary<string, string> { ["key"] = "English" },
            [Locale.JapaneseJP] = new Dictionary<string, string> { ["key"] = "Japanese" }
        });

        source.SupportedLocales.ShouldContain(Locale.EnglishUS);
        source.SupportedLocales.ShouldContain(Locale.JapaneseJP);
    }

    [Fact]
    public void Constructor_MultipleLocales_StoresStringsPerLocale()
    {
        var source = new DictionaryStringSource(new Dictionary<Locale, IReadOnlyDictionary<string, string>>
        {
            [Locale.EnglishUS] = new Dictionary<string, string> { ["greeting"] = "Hello" },
            [Locale.JapaneseJP] = new Dictionary<string, string> { ["greeting"] = "こんにちは" }
        });

        source.TryGetString(Locale.EnglishUS, "greeting", out var english).ShouldBeTrue();
        english.ShouldBe("Hello");

        source.TryGetString(Locale.JapaneseJP, "greeting", out var japanese).ShouldBeTrue();
        japanese.ShouldBe("こんにちは");
    }

    #endregion

    #region TryGetString

    [Fact]
    public void TryGetString_ExistingKey_ReturnsTrueAndValue()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["menu.start"] = "Start Game"
        });

        var found = source.TryGetString(Locale.EnglishUS, "menu.start", out var value);

        found.ShouldBeTrue();
        value.ShouldBe("Start Game");
    }

    [Fact]
    public void TryGetString_MissingKey_ReturnsFalse()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["menu.start"] = "Start Game"
        });

        var found = source.TryGetString(Locale.EnglishUS, "menu.missing", out var value);

        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Fact]
    public void TryGetString_MissingLocale_ReturnsFalse()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["menu.start"] = "Start Game"
        });

        var found = source.TryGetString(Locale.JapaneseJP, "menu.start", out var value);

        found.ShouldBeFalse();
        value.ShouldBeNull();
    }

    #endregion

    #region GetKeys

    [Fact]
    public void GetKeys_ExistingLocale_ReturnsAllKeys()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1",
            ["key2"] = "value2",
            ["key3"] = "value3"
        });

        var keys = source.GetKeys(Locale.EnglishUS).ToList();

        keys.ShouldContain("key1");
        keys.ShouldContain("key2");
        keys.ShouldContain("key3");
        keys.Count.ShouldBe(3);
    }

    [Fact]
    public void GetKeys_MissingLocale_ReturnsEmpty()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        var keys = source.GetKeys(Locale.JapaneseJP).ToList();

        keys.ShouldBeEmpty();
    }

    #endregion

    #region HasLocale

    [Fact]
    public void HasLocale_ExistingLocale_ReturnsTrue()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        source.HasLocale(Locale.EnglishUS).ShouldBeTrue();
    }

    [Fact]
    public void HasLocale_MissingLocale_ReturnsFalse()
    {
        var source = new DictionaryStringSource(Locale.EnglishUS, new Dictionary<string, string>
        {
            ["key1"] = "value1"
        });

        source.HasLocale(Locale.JapaneseJP).ShouldBeFalse();
    }

    #endregion
}
