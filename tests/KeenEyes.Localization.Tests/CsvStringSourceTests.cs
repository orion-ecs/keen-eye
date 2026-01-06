namespace KeenEyes.Localization.Tests;

public class CsvStringSourceTests
{
    #region FromString

    [Fact]
    public void FromString_ValidCsv_ParsesCorrectly()
    {
        var csv = """
            key,en,es
            menu.start,Start Game,Iniciar Juego
            menu.quit,Quit,Salir
            """;

        var source = CsvStringSource.FromString(csv);

        source.HasLocale(new Locale("en")).ShouldBeTrue();
        source.HasLocale(new Locale("es")).ShouldBeTrue();
        source.TryGetString(new Locale("en"), "menu.start", out var enStart).ShouldBeTrue();
        enStart.ShouldBe("Start Game");
        source.TryGetString(new Locale("es"), "menu.start", out var esStart).ShouldBeTrue();
        esStart.ShouldBe("Iniciar Juego");
    }

    [Fact]
    public void FromString_MultipleLocales_ParsesAll()
    {
        var csv = """
            key,en,es,ar,ja
            greeting,Hello,Hola,مرحبا,こんにちは
            """;

        var source = CsvStringSource.FromString(csv);

        source.SupportedLocales.Count().ShouldBe(4);
        source.TryGetString(new Locale("ar"), "greeting", out var ar).ShouldBeTrue();
        ar.ShouldBe("مرحبا");
        source.TryGetString(new Locale("ja"), "greeting", out var ja).ShouldBeTrue();
        ja.ShouldBe("こんにちは");
    }

    [Fact]
    public void FromString_QuotedValues_ParsesCorrectly()
    {
        var csv = """
            key,en,es
            message,"Hello, World!","¡Hola, Mundo!"
            """;

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "message", out var en).ShouldBeTrue();
        en.ShouldBe("Hello, World!");
        source.TryGetString(new Locale("es"), "message", out var es).ShouldBeTrue();
        es.ShouldBe("¡Hola, Mundo!");
    }

    [Fact]
    public void FromString_EscapedQuotes_ParsesCorrectly()
    {
        var csv = "key,en\nquote,\"He said \"\"Hello\"\"\"";

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "quote", out var value).ShouldBeTrue();
        value.ShouldBe("He said \"Hello\"");
    }

    [Fact]
    public void FromString_MultilineValues_ParsesCorrectly()
    {
        var csv = "key,en\nmultiline,\"Line 1\nLine 2\"";

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "multiline", out var value).ShouldBeTrue();
        value.ShouldBe("Line 1\nLine 2");
    }

    [Fact]
    public void FromString_EmptyValues_SkipsEmpty()
    {
        var csv = """
            key,en,es
            only_en,Hello,
            only_es,,Hola
            """;

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "only_en", out var en).ShouldBeTrue();
        en.ShouldBe("Hello");
        source.TryGetString(new Locale("es"), "only_en", out _).ShouldBeFalse();

        source.TryGetString(new Locale("es"), "only_es", out var es).ShouldBeTrue();
        es.ShouldBe("Hola");
        source.TryGetString(new Locale("en"), "only_es", out _).ShouldBeFalse();
    }

    [Fact]
    public void FromString_EmptyRows_SkipsEmpty()
    {
        var csv = """
            key,en
            first,First

            second,Second
            """;

        var source = CsvStringSource.FromString(csv);

        source.GetKeys(new Locale("en")).Count().ShouldBe(2);
    }

    [Fact]
    public void FromString_InvalidHeader_ThrowsFormatException()
    {
        var csv = """
            invalid,en
            key,value
            """;

        Should.Throw<FormatException>(() => CsvStringSource.FromString(csv));
    }

    [Fact]
    public void FromString_EmptyString_ReturnsEmptySource()
    {
        var source = CsvStringSource.FromString("");

        source.SupportedLocales.Count().ShouldBe(0);
    }

    #endregion

    #region LocaleOrder

    [Fact]
    public void LocaleOrder_PreservesHeaderOrder()
    {
        var csv = """
            key,ja,en,es,ar
            test,テスト,Test,Prueba,اختبار
            """;

        var source = CsvStringSource.FromString(csv);

        source.LocaleOrder.Count.ShouldBe(4);
        source.LocaleOrder[0].Code.ShouldBe("ja");
        source.LocaleOrder[1].Code.ShouldBe("en");
        source.LocaleOrder[2].Code.ShouldBe("es");
        source.LocaleOrder[3].Code.ShouldBe("ar");
    }

    #endregion

    #region AllKeys

    [Fact]
    public void AllKeys_ReturnsAllUniqueKeys()
    {
        var csv = """
            key,en,es
            key1,One,Uno
            key2,Two,Dos
            key3,Three,Tres
            """;

        var source = CsvStringSource.FromString(csv);

        var keys = source.AllKeys.ToList();
        keys.Count.ShouldBe(3);
        keys.ShouldContain("key1");
        keys.ShouldContain("key2");
        keys.ShouldContain("key3");
    }

    #endregion

    #region ToCsv Export

    [Fact]
    public void ToCsv_ExportsCorrectly()
    {
        var csv = """
            key,en,es
            menu.start,Start Game,Iniciar Juego
            menu.quit,Quit,Salir
            """;

        var source = CsvStringSource.FromString(csv);
        var exported = source.ToCsv();

        // Re-parse and verify
        var reimported = CsvStringSource.FromString(exported);
        reimported.TryGetString(new Locale("en"), "menu.start", out var en).ShouldBeTrue();
        en.ShouldBe("Start Game");
        reimported.TryGetString(new Locale("es"), "menu.quit", out var es).ShouldBeTrue();
        es.ShouldBe("Salir");
    }

    [Fact]
    public void ToCsv_EscapesSpecialCharacters()
    {
        var source = new DictionaryStringSource(new Locale("en"), new Dictionary<string, string>
        {
            ["comma"] = "Hello, World",
            ["quote"] = "He said \"Hi\"",
            ["newline"] = "Line 1\nLine 2"
        });

        var csv = CsvStringSource.ToCsv(source, [new Locale("en")]);

        csv.ShouldContain("\"Hello, World\"");
        csv.ShouldContain("\"He said \"\"Hi\"\"\"");
        csv.ShouldContain("\"Line 1\nLine 2\"");
    }

    [Fact]
    public void ToCsv_FromStringSource_IncludesAllLocales()
    {
        var translations = new Dictionary<Locale, IReadOnlyDictionary<string, string>>
        {
            [new Locale("en")] = new Dictionary<string, string> { ["hello"] = "Hello" },
            [new Locale("es")] = new Dictionary<string, string> { ["hello"] = "Hola" },
            [new Locale("ar")] = new Dictionary<string, string> { ["hello"] = "مرحبا" }
        };
        var source = new DictionaryStringSource(translations);

        var csv = CsvStringSource.ToCsv(source, [new Locale("en"), new Locale("es"), new Locale("ar")]);

        csv.ShouldContain("key,en,es,ar");
        csv.ShouldContain("hello,Hello,Hola,مرحبا");
    }

    #endregion

    #region MergeFromString

    [Fact]
    public void MergeFromString_AddsNewTranslations()
    {
        var csv1 = """
            key,en
            greeting,Hello
            """;
        var csv2 = """
            key,es
            greeting,Hola
            """;

        var source = CsvStringSource.FromString(csv1);
        source.MergeFromString(csv2);

        source.HasLocale(new Locale("en")).ShouldBeTrue();
        source.HasLocale(new Locale("es")).ShouldBeTrue();
        source.TryGetString(new Locale("en"), "greeting", out var en).ShouldBeTrue();
        en.ShouldBe("Hello");
        source.TryGetString(new Locale("es"), "greeting", out var es).ShouldBeTrue();
        es.ShouldBe("Hola");
    }

    [Fact]
    public void MergeFromString_OverwritesExisting()
    {
        var csv1 = """
            key,en
            greeting,Hello
            """;
        var csv2 = """
            key,en
            greeting,Hi there
            """;

        var source = CsvStringSource.FromString(csv1);
        source.MergeFromString(csv2);

        source.TryGetString(new Locale("en"), "greeting", out var value).ShouldBeTrue();
        value.ShouldBe("Hi there");
    }

    #endregion

    #region IStringSource Interface

    [Fact]
    public void GetKeys_ReturnsKeysForLocale()
    {
        var csv = """
            key,en,es
            key1,One,
            key2,Two,Dos
            """;

        var source = CsvStringSource.FromString(csv);

        var enKeys = source.GetKeys(new Locale("en")).ToList();
        enKeys.Count.ShouldBe(2);

        var esKeys = source.GetKeys(new Locale("es")).ToList();
        esKeys.Count.ShouldBe(1);
        esKeys.ShouldContain("key2");
    }

    [Fact]
    public void GetKeys_UnknownLocale_ReturnsEmpty()
    {
        var csv = """
            key,en
            test,Test
            """;

        var source = CsvStringSource.FromString(csv);

        source.GetKeys(new Locale("fr")).ShouldBeEmpty();
    }

    [Fact]
    public void TryGetString_UnknownKey_ReturnsFalse()
    {
        var csv = """
            key,en
            test,Test
            """;

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "unknown", out _).ShouldBeFalse();
    }

    #endregion

    #region Windows Line Endings

    [Fact]
    public void FromString_WindowsLineEndings_ParsesCorrectly()
    {
        var csv = "key,en\r\ntest,Value\r\n";

        var source = CsvStringSource.FromString(csv);

        source.TryGetString(new Locale("en"), "test", out var value).ShouldBeTrue();
        value.ShouldBe("Value");
    }

    #endregion
}
