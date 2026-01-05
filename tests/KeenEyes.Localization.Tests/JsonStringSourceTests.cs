namespace KeenEyes.Localization.Tests;

public class JsonStringSourceTests
{
    #region FromString - Flat JSON

    [Fact]
    public void FromString_FlatJson_ParsesCorrectly()
    {
        var json = """
        {
            "menu.start": "Start Game",
            "menu.quit": "Quit"
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "menu.start", out var start).ShouldBeTrue();
        start.ShouldBe("Start Game");

        source.TryGetString(Locale.EnglishUS, "menu.quit", out var quit).ShouldBeTrue();
        quit.ShouldBe("Quit");
    }

    #endregion

    #region FromString - Nested JSON

    [Fact]
    public void FromString_NestedJson_FlattensWithDotNotation()
    {
        var json = """
        {
            "menu": {
                "start": "Start Game",
                "options": {
                    "sound": "Sound",
                    "video": "Video"
                }
            }
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "menu.start", out var start).ShouldBeTrue();
        start.ShouldBe("Start Game");

        source.TryGetString(Locale.EnglishUS, "menu.options.sound", out var sound).ShouldBeTrue();
        sound.ShouldBe("Sound");

        source.TryGetString(Locale.EnglishUS, "menu.options.video", out var video).ShouldBeTrue();
        video.ShouldBe("Video");
    }

    #endregion

    #region FromString - Various Value Types

    [Fact]
    public void FromString_NumberValues_ConvertsToString()
    {
        var json = """
        {
            "count": 42,
            "price": 19.99
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "count", out var count).ShouldBeTrue();
        count.ShouldBe("42");

        source.TryGetString(Locale.EnglishUS, "price", out var price).ShouldBeTrue();
        price.ShouldBe("19.99");
    }

    [Fact]
    public void FromString_BooleanValues_ConvertsToString()
    {
        var json = """
        {
            "enabled": true,
            "disabled": false
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "enabled", out var enabled).ShouldBeTrue();
        enabled.ShouldBe("true");

        source.TryGetString(Locale.EnglishUS, "disabled", out var disabled).ShouldBeTrue();
        disabled.ShouldBe("false");
    }

    [Fact]
    public void FromString_NullValue_ConvertsToEmptyString()
    {
        var json = """
        {
            "empty": null
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "empty", out var value).ShouldBeTrue();
        value.ShouldBe(string.Empty);
    }

    #endregion

    #region FromString - Comments and Trailing Commas

    [Fact]
    public void FromString_WithComments_ParsesCorrectly()
    {
        var json = """
        {
            // This is a comment
            "key": "value"
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "key", out var value).ShouldBeTrue();
        value.ShouldBe("value");
    }

    [Fact]
    public void FromString_WithTrailingCommas_ParsesCorrectly()
    {
        var json = """
        {
            "key1": "value1",
            "key2": "value2",
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "key1", out var value).ShouldBeTrue();
        value.ShouldBe("value1");
    }

    #endregion

    #region SupportedLocales

    [Fact]
    public void SupportedLocales_SingleLocale_ContainsLocale()
    {
        var json = """{"key": "value"}""";

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.SupportedLocales.ShouldContain(Locale.EnglishUS);
        source.SupportedLocales.Count().ShouldBe(1);
    }

    #endregion

    #region GetKeys

    [Fact]
    public void GetKeys_ReturnsAllFlattenedKeys()
    {
        var json = """
        {
            "a": "1",
            "b": {
                "c": "2",
                "d": "3"
            }
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);
        var keys = source.GetKeys(Locale.EnglishUS).ToList();

        keys.ShouldContain("a");
        keys.ShouldContain("b.c");
        keys.ShouldContain("b.d");
        keys.Count.ShouldBe(3);
    }

    #endregion

    #region MergeFromString

    [Fact]
    public void MergeFromString_AddsNewKeys()
    {
        var json1 = """{"key1": "value1"}""";
        var json2 = """{"key2": "value2"}""";

        var source = JsonStringSource.FromString(json1, Locale.EnglishUS);
        source.MergeFromString(json2, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "key1", out var v1).ShouldBeTrue();
        v1.ShouldBe("value1");

        source.TryGetString(Locale.EnglishUS, "key2", out var v2).ShouldBeTrue();
        v2.ShouldBe("value2");
    }

    [Fact]
    public void MergeFromString_OverwritesExistingKeys()
    {
        var json1 = """{"key": "original"}""";
        var json2 = """{"key": "updated"}""";

        var source = JsonStringSource.FromString(json1, Locale.EnglishUS);
        source.MergeFromString(json2, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "key", out var value).ShouldBeTrue();
        value.ShouldBe("updated");
    }

    [Fact]
    public void MergeFromString_AddsNewLocale()
    {
        var jsonEn = """{"key": "English"}""";
        var jsonJa = """{"key": "日本語"}""";

        var source = JsonStringSource.FromString(jsonEn, Locale.EnglishUS);
        source.MergeFromString(jsonJa, Locale.JapaneseJP);

        source.SupportedLocales.ShouldContain(Locale.EnglishUS);
        source.SupportedLocales.ShouldContain(Locale.JapaneseJP);

        source.TryGetString(Locale.EnglishUS, "key", out var en).ShouldBeTrue();
        en.ShouldBe("English");

        source.TryGetString(Locale.JapaneseJP, "key", out var ja).ShouldBeTrue();
        ja.ShouldBe("日本語");
    }

    #endregion

    #region Unicode Support

    [Fact]
    public void FromString_UnicodeCharacters_ParsesCorrectly()
    {
        var json = """
        {
            "japanese": "こんにちは",
            "chinese": "你好",
            "korean": "안녕하세요",
            "emoji": "Hello 🎮"
        }
        """;

        var source = JsonStringSource.FromString(json, Locale.EnglishUS);

        source.TryGetString(Locale.EnglishUS, "japanese", out var ja).ShouldBeTrue();
        ja.ShouldBe("こんにちは");

        source.TryGetString(Locale.EnglishUS, "chinese", out var zh).ShouldBeTrue();
        zh.ShouldBe("你好");

        source.TryGetString(Locale.EnglishUS, "korean", out var ko).ShouldBeTrue();
        ko.ShouldBe("안녕하세요");

        source.TryGetString(Locale.EnglishUS, "emoji", out var em).ShouldBeTrue();
        em.ShouldBe("Hello 🎮");
    }

    #endregion

    #region HasLocale

    [Fact]
    public void HasLocale_ExistingLocale_ReturnsTrue()
    {
        var source = JsonStringSource.FromString("""{"key": "value"}""", Locale.EnglishUS);

        source.HasLocale(Locale.EnglishUS).ShouldBeTrue();
    }

    [Fact]
    public void HasLocale_MissingLocale_ReturnsFalse()
    {
        var source = JsonStringSource.FromString("""{"key": "value"}""", Locale.EnglishUS);

        source.HasLocale(Locale.JapaneseJP).ShouldBeFalse();
    }

    #endregion
}
