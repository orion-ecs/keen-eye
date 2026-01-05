namespace KeenEyes.Localization.Tests;

public class LocaleTests
{
    #region Construction

    [Fact]
    public void Constructor_WithCode_SetsCode()
    {
        var locale = new Locale("en-US");

        locale.Code.ShouldBe("en-US");
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesLocale()
    {
        Locale locale = "ja-JP";

        locale.Code.ShouldBe("ja-JP");
    }

    #endregion

    #region Language Property

    [Fact]
    public void Language_WithRegion_ReturnsLanguageOnly()
    {
        var locale = new Locale("en-US");

        locale.Language.ShouldBe("en");
    }

    [Fact]
    public void Language_WithoutRegion_ReturnsFullCode()
    {
        var locale = new Locale("en");

        locale.Language.ShouldBe("en");
    }

    #endregion

    #region Region Property

    [Fact]
    public void Region_WithRegion_ReturnsRegion()
    {
        var locale = new Locale("en-US");

        locale.Region.ShouldBe("US");
    }

    [Fact]
    public void Region_WithoutRegion_ReturnsNull()
    {
        var locale = new Locale("en");

        locale.Region.ShouldBeNull();
    }

    #endregion

    #region HasRegion Property

    [Fact]
    public void HasRegion_WithRegion_ReturnsTrue()
    {
        var locale = new Locale("en-US");

        locale.HasRegion.ShouldBeTrue();
    }

    [Fact]
    public void HasRegion_WithoutRegion_ReturnsFalse()
    {
        var locale = new Locale("en");

        locale.HasRegion.ShouldBeFalse();
    }

    #endregion

    #region LanguageOnly Property

    [Fact]
    public void LanguageOnly_WithRegion_ReturnsLanguageOnlyLocale()
    {
        var locale = new Locale("en-US");

        var languageOnly = locale.LanguageOnly;

        languageOnly.Code.ShouldBe("en");
    }

    [Fact]
    public void LanguageOnly_WithoutRegion_ReturnsSameCode()
    {
        var locale = new Locale("en");

        var languageOnly = locale.LanguageOnly;

        languageOnly.Code.ShouldBe("en");
    }

    #endregion

    #region Predefined Locales

    [Fact]
    public void PredefinedLocales_HaveCorrectCodes()
    {
        Locale.EnglishUS.Code.ShouldBe("en-US");
        Locale.EnglishGB.Code.ShouldBe("en-GB");
        Locale.JapaneseJP.Code.ShouldBe("ja-JP");
        Locale.ChineseSimplified.Code.ShouldBe("zh-CN");
        Locale.ChineseTraditional.Code.ShouldBe("zh-TW");
        Locale.KoreanKR.Code.ShouldBe("ko-KR");
        Locale.GermanDE.Code.ShouldBe("de-DE");
        Locale.FrenchFR.Code.ShouldBe("fr-FR");
        Locale.SpanishES.Code.ShouldBe("es-ES");
        Locale.PortugueseBR.Code.ShouldBe("pt-BR");
        Locale.ItalianIT.Code.ShouldBe("it-IT");
        Locale.RussianRU.Code.ShouldBe("ru-RU");
    }

    #endregion

    #region Equality

    [Fact]
    public void Equals_SameCode_ReturnsTrue()
    {
        var locale1 = new Locale("en-US");
        var locale2 = new Locale("en-US");

        locale1.ShouldBe(locale2);
    }

    [Fact]
    public void Equals_DifferentCode_ReturnsFalse()
    {
        var locale1 = new Locale("en-US");
        var locale2 = new Locale("en-GB");

        locale1.ShouldNotBe(locale2);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_ReturnsCode()
    {
        var locale = new Locale("en-US");

        locale.ToString().ShouldBe("en-US");
    }

    #endregion
}
