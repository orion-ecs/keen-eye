using KeenEyes.Localization.TextShaping;

namespace KeenEyes.Localization.Tests.TextShaping;

public class ComplexScriptInfoTests
{
    #region GetInfo

    [Theory]
    [InlineData(ScriptType.Arabic, "Arabic")]
    [InlineData(ScriptType.Hebrew, "Hebrew")]
    [InlineData(ScriptType.Thai, "Thai")]
    [InlineData(ScriptType.Devanagari, "Devanagari")]
    [InlineData(ScriptType.Tamil, "Tamil")]
    [InlineData(ScriptType.Bengali, "Bengali")]
    public void GetInfo_KnownScript_ReturnsInfo(ScriptType script, string expectedName)
    {
        var info = ComplexScriptInfo.GetInfo(script);

        info.ShouldNotBeNull();
        info!.ScriptName.ShouldBe(expectedName);
        info.Script.ShouldBe(script);
    }

    [Fact]
    public void GetInfo_UnknownScript_ReturnsNull()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Unknown);

        info.ShouldBeNull();
    }

    [Fact]
    public void GetInfo_LatinScript_ReturnsNull()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Latin);

        info.ShouldBeNull();
    }

    #endregion

    #region RequiresShaping

    [Theory]
    [InlineData(ScriptType.Arabic, true)]
    [InlineData(ScriptType.Hebrew, true)]
    [InlineData(ScriptType.Thai, true)]
    [InlineData(ScriptType.Devanagari, true)]
    public void GetInfo_ComplexScripts_RequiresShaping(ScriptType script, bool expected)
    {
        var info = ComplexScriptInfo.GetInfo(script);

        info.ShouldNotBeNull();
        info!.RequiresShaping.ShouldBe(expected);
    }

    [Theory]
    [InlineData(ScriptType.CJK, false)]
    [InlineData(ScriptType.Hangul, false)]
    public void GetInfo_SimpleScripts_NoShapingRequired(ScriptType script, bool expected)
    {
        var info = ComplexScriptInfo.GetInfo(script);

        info.ShouldNotBeNull();
        info!.RequiresShaping.ShouldBe(expected);
    }

    #endregion

    #region ShapingFeatures

    [Fact]
    public void GetInfo_Arabic_HasExpectedFeatures()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Arabic);

        info.ShouldNotBeNull();
        info!.ShapingFeatures.ShouldContain("init");
        info.ShapingFeatures.ShouldContain("medi");
        info.ShapingFeatures.ShouldContain("fina");
        info.ShapingFeatures.ShouldContain("isol");
    }

    [Fact]
    public void GetInfo_Thai_HasMarkFeatures()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Thai);

        info.ShouldNotBeNull();
        info!.ShapingFeatures.ShouldContain("mark");
        info.ShapingFeatures.ShouldContain("mkmk");
    }

    #endregion

    #region GetInfoForLocale

    [Fact]
    public void GetInfoForLocale_ArabicLocale_ReturnsArabicInfo()
    {
        var info = ComplexScriptInfo.GetInfoForLocale(Locale.Arabic);

        info.ShouldNotBeNull();
        info!.Script.ShouldBe(ScriptType.Arabic);
    }

    [Fact]
    public void GetInfoForLocale_PersianLocale_ReturnsArabicInfo()
    {
        // Persian uses Arabic script
        var info = ComplexScriptInfo.GetInfoForLocale(Locale.PersianIR);

        info.ShouldNotBeNull();
        info!.Script.ShouldBe(ScriptType.Arabic);
    }

    [Fact]
    public void GetInfoForLocale_ThaiLocale_ReturnsThaiInfo()
    {
        var info = ComplexScriptInfo.GetInfoForLocale(Locale.ThaiTH);

        info.ShouldNotBeNull();
        info!.Script.ShouldBe(ScriptType.Thai);
    }

    [Fact]
    public void GetInfoForLocale_HindiLocale_ReturnsDevanagariInfo()
    {
        var info = ComplexScriptInfo.GetInfoForLocale(Locale.HindiIN);

        info.ShouldNotBeNull();
        info!.Script.ShouldBe(ScriptType.Devanagari);
    }

    [Fact]
    public void GetInfoForLocale_EnglishLocale_ReturnsNull()
    {
        var info = ComplexScriptInfo.GetInfoForLocale(Locale.EnglishUS);

        info.ShouldBeNull();
    }

    #endregion

    #region GetScriptForLanguage

    [Theory]
    [InlineData("ar", ScriptType.Arabic)]
    [InlineData("fa", ScriptType.Arabic)]
    [InlineData("ur", ScriptType.Arabic)]
    [InlineData("he", ScriptType.Hebrew)]
    [InlineData("th", ScriptType.Thai)]
    [InlineData("hi", ScriptType.Devanagari)]
    [InlineData("ta", ScriptType.Tamil)]
    [InlineData("bn", ScriptType.Bengali)]
    [InlineData("zh", ScriptType.CJK)]
    [InlineData("ja", ScriptType.CJK)]
    [InlineData("ko", ScriptType.Hangul)]
    public void GetScriptForLanguage_KnownLanguages_ReturnsCorrectScript(string language, ScriptType expectedScript)
    {
        var script = ComplexScriptInfo.GetScriptForLanguage(language);

        script.ShouldNotBeNull();
        script.ShouldBe(expectedScript);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("de")]
    public void GetScriptForLanguage_LatinLanguages_ReturnsNull(string language)
    {
        var script = ComplexScriptInfo.GetScriptForLanguage(language);

        script.ShouldBeNull();
    }

    #endregion

    #region Languages Property

    [Fact]
    public void GetInfo_Arabic_ListsCorrectLanguages()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Arabic);

        info.ShouldNotBeNull();
        info!.Languages.ShouldContain("Arabic");
        info.Languages.ShouldContain("Persian");
        info.Languages.ShouldContain("Urdu");
    }

    [Fact]
    public void GetInfo_Devanagari_ListsCorrectLanguages()
    {
        var info = ComplexScriptInfo.GetInfo(ScriptType.Devanagari);

        info.ShouldNotBeNull();
        info!.Languages.ShouldContain("Hindi");
        info.Languages.ShouldContain("Sanskrit");
        info.Languages.ShouldContain("Marathi");
    }

    #endregion
}
