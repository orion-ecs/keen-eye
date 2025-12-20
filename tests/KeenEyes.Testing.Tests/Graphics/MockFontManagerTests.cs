using System.Numerics;
using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockFontManagerTests
{
    #region Font Loading

    [Fact]
    public void LoadFont_CreatesFont()
    {
        using var manager = new MockFontManager();

        var font = manager.LoadFont("test.ttf", 16);

        font.Id.ShouldNotBe(0);
        manager.Fonts.ShouldContainKey(font);
    }

    [Fact]
    public void LoadFont_TracksPath()
    {
        using var manager = new MockFontManager();

        manager.LoadFont("assets/fonts/arial.ttf", 16);

        manager.LoadedFontPaths.ShouldContain("assets/fonts/arial.ttf");
    }

    [Fact]
    public void LoadFont_StoresFontInfo()
    {
        using var manager = new MockFontManager();

        var font = manager.LoadFont("test.ttf", 24);

        manager.Fonts[font].Path.ShouldBe("test.ttf");
        manager.Fonts[font].Size.ShouldBe(24);
    }

    [Fact]
    public void LoadFont_WhenShouldFail_Throws()
    {
        using var manager = new MockFontManager();
        manager.ShouldFailLoad = true;

        Should.Throw<InvalidOperationException>(() => manager.LoadFont("test.ttf", 16));
    }

    [Fact]
    public void LoadFontFromMemory_CreatesFont()
    {
        using var manager = new MockFontManager();
        var data = new byte[100];

        var font = manager.LoadFontFromMemory(data, 16, "MemoryFont");

        font.Id.ShouldNotBe(0);
        manager.Fonts[font].Name.ShouldBe("MemoryFont");
        manager.Fonts[font].DataSize.ShouldBe(100);
    }

    [Fact]
    public void CreateSizedFont_CreatesNewFontWithSize()
    {
        using var manager = new MockFontManager();
        var baseFont = manager.LoadFont("test.ttf", 16);

        var newFont = manager.CreateSizedFont(baseFont, 32);

        newFont.Id.ShouldNotBe(baseFont.Id);
        manager.Fonts[newFont].Size.ShouldBe(32);
        manager.Fonts[newFont].BaseFont.ShouldBe(baseFont);
    }

    [Fact]
    public void CreateSizedFont_WithInvalidBase_Throws()
    {
        using var manager = new MockFontManager();
        var invalidFont = new FontHandle(999);

        Should.Throw<InvalidOperationException>(() => manager.CreateSizedFont(invalidFont, 32));
    }

    #endregion

    #region Font Metrics

    [Fact]
    public void GetFontSize_ReturnsStoredSize()
    {
        using var manager = new MockFontManager();
        var font = manager.LoadFont("test.ttf", 24);

        manager.GetFontSize(font).ShouldBe(24);
    }

    [Fact]
    public void GetFontSize_InvalidFont_ReturnsZero()
    {
        using var manager = new MockFontManager();

        manager.GetFontSize(new FontHandle(999)).ShouldBe(0);
    }

    [Fact]
    public void GetLineHeight_ReturnsDefaultLineHeight()
    {
        using var manager = new MockFontManager();
        manager.DefaultLineHeight = 20f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetLineHeight(font).ShouldBe(20f);
    }

    [Fact]
    public void GetLineHeight_WithCustomValue_ReturnsCustom()
    {
        using var manager = new MockFontManager();
        var font = manager.LoadFont("test.ttf", 16);
        manager.SetFontMetrics(font, lineHeight: 24f);

        manager.GetLineHeight(font).ShouldBe(24f);
    }

    [Fact]
    public void GetBaseline_ReturnsAscent()
    {
        using var manager = new MockFontManager();
        manager.DefaultAscent = 15f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetBaseline(font).ShouldBe(15f);
    }

    [Fact]
    public void GetAscent_ReturnsDefaultAscent()
    {
        using var manager = new MockFontManager();
        manager.DefaultAscent = 12f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetAscent(font).ShouldBe(12f);
    }

    [Fact]
    public void GetDescent_ReturnsDefaultDescent()
    {
        using var manager = new MockFontManager();
        manager.DefaultDescent = 4f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetDescent(font).ShouldBe(4f);
    }

    #endregion

    #region Text Measurement

    [Fact]
    public void MeasureText_ReturnsSize()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        manager.DefaultLineHeight = 20f;
        var font = manager.LoadFont("test.ttf", 16);

        var size = manager.MeasureText(font, "Hello");

        size.X.ShouldBe(50f); // 5 chars * 10
        size.Y.ShouldBe(20f); // 1 line
    }

    [Fact]
    public void MeasureText_WithNewlines_CountsLines()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        manager.DefaultLineHeight = 20f;
        var font = manager.LoadFont("test.ttf", 16);

        var size = manager.MeasureText(font, "Hello\nWorld");

        size.Y.ShouldBe(40f); // 2 lines
    }

    [Fact]
    public void MeasureTextWidth_ReturnsWidth()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 8f;
        var font = manager.LoadFont("test.ttf", 16);

        var width = manager.MeasureTextWidth(font, "Test");

        width.ShouldBe(32f); // 4 chars * 8
    }

    [Fact]
    public void MeasureTextWidth_WithNewlines_ReturnsMaxLineWidth()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        var font = manager.LoadFont("test.ttf", 16);

        var width = manager.MeasureTextWidth(font, "Hello\nHi");

        width.ShouldBe(50f); // "Hello" is wider
    }

    [Fact]
    public void GetCharacterAdvance_ReturnsDefaultWidth()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetCharacterAdvance(font, 'A').ShouldBe(10f);
    }

    [Fact]
    public void GetCharacterAdvance_WithCustomWidth_ReturnsCustom()
    {
        using var manager = new MockFontManager();
        manager.CharacterWidths['W'] = 15f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetCharacterAdvance(font, 'W').ShouldBe(15f);
    }

    [Fact]
    public void GetKerning_ReturnsZeroByDefault()
    {
        using var manager = new MockFontManager();
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetKerning(font, 'A', 'V').ShouldBe(0f);
    }

    [Fact]
    public void GetKerning_WithCustomPair_ReturnsCustom()
    {
        using var manager = new MockFontManager();
        manager.KerningPairs[('A', 'V')] = -2f;
        var font = manager.LoadFont("test.ttf", 16);

        manager.GetKerning(font, 'A', 'V').ShouldBe(-2f);
    }

    #endregion

    #region Word Wrap

    [Fact]
    public void CalculateWordWrap_ReturnsBreakPoints()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        var font = manager.LoadFont("test.ttf", 16);

        var breaks = manager.CalculateWordWrap(font, "Hello World", 60f);

        // "Hello " is 60, "World" would exceed, so break after space
        breaks.Count.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CalculateWordWrap_WithNewlines_IncludesNewlineBreaks()
    {
        using var manager = new MockFontManager();
        manager.DefaultCharWidth = 10f;
        var font = manager.LoadFont("test.ttf", 16);

        var breaks = manager.CalculateWordWrap(font, "Hello\nWorld", 1000f);

        breaks.ShouldContain(5); // Index of \n
    }

    #endregion

    #region Resource Management

    [Fact]
    public void IsValid_WithValidFont_ReturnsTrue()
    {
        using var manager = new MockFontManager();
        var font = manager.LoadFont("test.ttf", 16);

        manager.IsValid(font).ShouldBeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidFont_ReturnsFalse()
    {
        using var manager = new MockFontManager();

        manager.IsValid(new FontHandle(999)).ShouldBeFalse();
    }

    [Fact]
    public void Release_RemovesFont()
    {
        using var manager = new MockFontManager();
        var font = manager.LoadFont("test.ttf", 16);

        manager.Release(font);

        manager.Fonts.ShouldNotContainKey(font);
    }

    [Fact]
    public void ReleaseAll_RemovesAllFonts()
    {
        using var manager = new MockFontManager();
        manager.LoadFont("font1.ttf", 16);
        manager.LoadFont("font2.ttf", 16);

        manager.ReleaseAll();

        manager.Fonts.ShouldBeEmpty();
    }

    [Fact]
    public void Count_ReturnsLoadedFontCount()
    {
        using var manager = new MockFontManager();
        manager.LoadFont("font1.ttf", 16);
        manager.LoadFont("font2.ttf", 16);

        manager.Count.ShouldBe(2);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllState()
    {
        using var manager = new MockFontManager();
        manager.LoadFont("test.ttf", 16);
        manager.DefaultCharWidth = 20f;
        manager.CharacterWidths['A'] = 15f;
        manager.KerningPairs[('A', 'V')] = -2f;

        manager.Reset();

        manager.Fonts.ShouldBeEmpty();
        manager.LoadedFontPaths.ShouldBeEmpty();
        manager.CharacterWidths.ShouldBeEmpty();
        manager.KerningPairs.ShouldBeEmpty();
        manager.DefaultCharWidth.ShouldBe(8f);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ReleasesAllFonts()
    {
        var manager = new MockFontManager();
        manager.LoadFont("test.ttf", 16);

        manager.Dispose();

        manager.Fonts.ShouldBeEmpty();
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var manager = new MockFontManager();

        Should.NotThrow(() =>
        {
            manager.Dispose();
            manager.Dispose();
        });
    }

    #endregion
}
