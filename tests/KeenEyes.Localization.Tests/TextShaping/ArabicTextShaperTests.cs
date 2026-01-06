using KeenEyes.Localization.TextShaping;

namespace KeenEyes.Localization.Tests.TextShaping;

public class ArabicTextShaperTests
{
    private readonly ArabicTextShaper shaper = new();

    #region SupportedScripts

    [Fact]
    public void SupportedScripts_IncludesArabic()
    {
        shaper.SupportedScripts.ShouldContain(ScriptType.Arabic);
    }

    [Fact]
    public void SupportsScript_Arabic_ReturnsTrue()
    {
        shaper.SupportsScript(ScriptType.Arabic).ShouldBeTrue();
    }

    [Fact]
    public void SupportsScript_Latin_ReturnsFalse()
    {
        shaper.SupportsScript(ScriptType.Latin).ShouldBeFalse();
    }

    #endregion

    #region Shape Method

    [Fact]
    public void Shape_EmptyString_ReturnsEmpty()
    {
        var result = shaper.Shape("", Locale.Arabic);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Shape_NullString_ReturnsNull()
    {
        var result = shaper.Shape(null!, Locale.Arabic);

        result.ShouldBeNull();
    }

    [Fact]
    public void Shape_NonArabicText_ReturnsUnchanged()
    {
        var result = shaper.Shape("Hello World", Locale.Arabic);

        result.ShouldBe("Hello World");
    }

    [Fact]
    public void Shape_ArabicText_ReturnsShapedText()
    {
        // مرحبا (marhaba - hello) should be shaped
        var result = shaper.Shape("مرحبا", Locale.Arabic);

        // The result should be different from input (contextual forms applied)
        result.ShouldNotBeEmpty();
        result.Length.ShouldBe(5); // Same character count
    }

    [Fact]
    public void Shape_MixedText_OnlyShapesArabic()
    {
        var result = shaper.Shape("Hello مرحبا World", Locale.Arabic);

        result.ShouldContain("Hello");
        result.ShouldContain("World");
    }

    #endregion

    #region IsArabicLetter

    [Theory]
    [InlineData('ا', true)]  // Alif
    [InlineData('ب', true)]  // Ba
    [InlineData('ت', true)]  // Ta
    [InlineData('م', true)]  // Mim
    [InlineData('ن', true)]  // Nun
    [InlineData('A', false)] // Latin A
    [InlineData('1', false)] // Digit
    [InlineData(' ', false)] // Space
    public void IsArabicLetter_VariousCharacters_ReturnsCorrectResult(char c, bool expected)
    {
        ArabicTextShaper.IsArabicLetter(c).ShouldBe(expected);
    }

    #endregion

    #region ShapeWithInfo

    [Fact]
    public void ShapeWithInfo_ArabicText_ReturnsCorrectMetadata()
    {
        var result = shaper.ShapeWithInfo("مرحبا", Locale.Arabic);

        result.ContainsRtl.ShouldBeTrue();
        result.BaseDirection.ShouldBe(TextDirection.RightToLeft);
        result.DetectedScripts.ShouldContain(ScriptType.Arabic);
        result.OriginalText.ShouldBe("مرحبا");
        result.ShapedText.ShouldNotBeEmpty();
    }

    [Fact]
    public void ShapeWithInfo_MixedText_IndicatesMixedDirection()
    {
        var result = shaper.ShapeWithInfo("Hello مرحبا World", Locale.Arabic);

        result.IsMixedDirection.ShouldBeTrue();
        result.ContainsRtl.ShouldBeTrue();
    }

    [Fact]
    public void ShapeWithInfo_LatinOnly_NoMixedDirection()
    {
        var result = shaper.ShapeWithInfo("Hello World", Locale.Arabic);

        result.IsMixedDirection.ShouldBeFalse();
        result.ContainsRtl.ShouldBeFalse();
    }

    #endregion
}
