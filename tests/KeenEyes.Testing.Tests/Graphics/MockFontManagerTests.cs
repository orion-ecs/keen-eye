using KeenEyes.Graphics.Abstractions;
using KeenEyes.Testing.Graphics;

namespace KeenEyes.Testing.Tests.Graphics;

public class MockFontManagerTests
{
    #region Font Loading

    [Fact]
    public void LoadFont_ReturnsValidHandle()
    {
        using var fontManager = new MockFontManager();

        var font = fontManager.LoadFont("test.ttf", 16);

        Assert.True(font.Id > 0);
        Assert.True(fontManager.IsValid(font));
    }

    [Fact]
    public void LoadFont_TracksPath()
    {
        using var fontManager = new MockFontManager();

        fontManager.LoadFont("test.ttf", 16);

        Assert.Contains("test.ttf", fontManager.LoadedFontPaths);
    }

    [Fact]
    public void LoadFont_StoresFontInfo()
    {
        using var fontManager = new MockFontManager();

        var font = fontManager.LoadFont("test.ttf", 16);

        Assert.Equal("test.ttf", fontManager.Fonts[font].Path);
        Assert.Equal(16f, fontManager.Fonts[font].Size);
    }

    [Fact]
    public void LoadFont_WhenShouldFailLoad_ThrowsException()
    {
        using var fontManager = new MockFontManager { ShouldFailLoad = true };

        Assert.Throws<InvalidOperationException>(() => fontManager.LoadFont("test.ttf", 16));
    }

    [Fact]
    public void LoadFontFromMemory_ReturnsValidHandle()
    {
        using var fontManager = new MockFontManager();
        var data = new byte[1024];

        var font = fontManager.LoadFontFromMemory(data, 14, "TestFont");

        Assert.True(font.Id > 0);
        Assert.True(fontManager.IsValid(font));
        Assert.Equal("TestFont", fontManager.Fonts[font].Name);
        Assert.Equal(1024, fontManager.Fonts[font].DataSize);
    }

    [Fact]
    public void LoadFontFromMemory_WhenShouldFailLoad_ThrowsException()
    {
        using var fontManager = new MockFontManager { ShouldFailLoad = true };
        var data = new byte[100];

        Assert.Throws<InvalidOperationException>(() => fontManager.LoadFontFromMemory(data, 12));
    }

    [Fact]
    public void CreateSizedFont_CreatesNewFontWithDifferentSize()
    {
        using var fontManager = new MockFontManager();
        var baseFont = fontManager.LoadFont("test.ttf", 16);

        var sizedFont = fontManager.CreateSizedFont(baseFont, 24);

        Assert.NotEqual(baseFont, sizedFont);
        Assert.True(fontManager.IsValid(sizedFont));
        Assert.Equal(24f, fontManager.Fonts[sizedFont].Size);
        Assert.Equal(baseFont, fontManager.Fonts[sizedFont].BaseFont);
    }

    [Fact]
    public void CreateSizedFont_WithInvalidBase_ThrowsException()
    {
        using var fontManager = new MockFontManager();
        var invalidFont = new FontHandle(999);

        Assert.Throws<InvalidOperationException>(() => fontManager.CreateSizedFont(invalidFont, 24));
    }

    #endregion

    #region Font Metrics

    [Fact]
    public void GetFontSize_ReturnsCorrectSize()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 18);

        var size = fontManager.GetFontSize(font);

        Assert.Equal(18f, size);
    }

    [Fact]
    public void GetFontSize_WithInvalidHandle_ReturnsZero()
    {
        using var fontManager = new MockFontManager();
        var invalidFont = new FontHandle(999);

        var size = fontManager.GetFontSize(invalidFont);

        Assert.Equal(0f, size);
    }

    [Fact]
    public void GetLineHeight_ReturnsDefaultValue()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultLineHeight = 20f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var lineHeight = fontManager.GetLineHeight(font);

        Assert.Equal(20f, lineHeight);
    }

    [Fact]
    public void GetLineHeight_ReturnsCustomValue()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);
        fontManager.SetFontMetrics(font, lineHeight: 30f);

        var lineHeight = fontManager.GetLineHeight(font);

        Assert.Equal(30f, lineHeight);
    }

    [Fact]
    public void GetBaseline_ReturnsAscent()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultAscent = 15f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var baseline = fontManager.GetBaseline(font);

        Assert.Equal(15f, baseline);
    }

    [Fact]
    public void GetAscent_ReturnsDefaultValue()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultAscent = 14f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var ascent = fontManager.GetAscent(font);

        Assert.Equal(14f, ascent);
    }

    [Fact]
    public void GetAscent_ReturnsCustomValue()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);
        fontManager.SetFontMetrics(font, ascent: 18f);

        var ascent = fontManager.GetAscent(font);

        Assert.Equal(18f, ascent);
    }

    [Fact]
    public void GetDescent_ReturnsDefaultValue()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultDescent = 5f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var descent = fontManager.GetDescent(font);

        Assert.Equal(5f, descent);
    }

    [Fact]
    public void GetDescent_ReturnsCustomValue()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);
        fontManager.SetFontMetrics(font, descent: 6f);

        var descent = fontManager.GetDescent(font);

        Assert.Equal(6f, descent);
    }

    #endregion

    #region Text Measurement

    [Fact]
    public void MeasureText_ReturnsCorrectWidth()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var size = fontManager.MeasureText(font, "Hello");

        Assert.Equal(50f, size.X); // 5 chars * 10 width
    }

    [Fact]
    public void MeasureText_ReturnsCorrectHeight()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultLineHeight = 20f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var size = fontManager.MeasureText(font, "Hello");

        Assert.Equal(20f, size.Y);
    }

    [Fact]
    public void MeasureText_HandlesMultipleLines()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultLineHeight = 16f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var size = fontManager.MeasureText(font, "Line1\nLine2\nLine3");

        Assert.Equal(48f, size.Y); // 3 lines * 16 height
    }

    [Fact]
    public void MeasureTextWidth_ReturnsMaxLineWidth()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 8f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var width = fontManager.MeasureTextWidth(font, "Hi\nHello\nOK");

        Assert.Equal(40f, width); // "Hello" = 5 chars * 8 = 40 is widest
    }

    [Fact]
    public void GetCharacterAdvance_ReturnsDefaultWidth()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var advance = fontManager.GetCharacterAdvance(font, 'A');

        Assert.Equal(10f, advance);
    }

    [Fact]
    public void GetCharacterAdvance_ReturnsCustomWidth()
    {
        using var fontManager = new MockFontManager();
        fontManager.CharacterWidths['W'] = 15f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var advance = fontManager.GetCharacterAdvance(font, 'W');

        Assert.Equal(15f, advance);
    }

    [Fact]
    public void GetKerning_ReturnsDefaultZero()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);

        var kerning = fontManager.GetKerning(font, 'A', 'V');

        Assert.Equal(0f, kerning);
    }

    [Fact]
    public void GetKerning_ReturnsCustomValue()
    {
        using var fontManager = new MockFontManager();
        fontManager.KerningPairs[('A', 'V')] = -2f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var kerning = fontManager.GetKerning(font, 'A', 'V');

        Assert.Equal(-2f, kerning);
    }

    [Fact]
    public void MeasureTextWidth_IncludesKerning()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        fontManager.KerningPairs[('A', 'V')] = -2f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var width = fontManager.MeasureTextWidth(font, "AV");

        Assert.Equal(18f, width); // 10 + (-2 kerning) + 10
    }

    #endregion

    #region Word Wrap

    [Fact]
    public void CalculateWordWrap_ReturnsEmptyForShortText()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var breaks = fontManager.CalculateWordWrap(font, "Hi", 100);

        Assert.Empty(breaks);
    }

    [Fact]
    public void CalculateWordWrap_BreaksAtNewlines()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var breaks = fontManager.CalculateWordWrap(font, "Hi\nThere", 1000);

        Assert.Single(breaks);
        Assert.Equal(2, breaks[0]); // Index of \n
    }

    [Fact]
    public void CalculateWordWrap_BreaksAtSpaceWhenTooLong()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var breaks = fontManager.CalculateWordWrap(font, "Hello World Test", 60);

        Assert.NotEmpty(breaks);
    }

    [Fact]
    public void CalculateWordWrap_BreaksWithinWordWhenNoSpace()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 10f;
        var font = fontManager.LoadFont("test.ttf", 16);

        var breaks = fontManager.CalculateWordWrap(font, "Supercalifragilistic", 50);

        Assert.NotEmpty(breaks);
    }

    #endregion

    #region Resource Management

    [Fact]
    public void Count_ReturnsNumberOfFonts()
    {
        using var fontManager = new MockFontManager();
        fontManager.LoadFont("a.ttf", 12);
        fontManager.LoadFont("b.ttf", 14);

        Assert.Equal(2, fontManager.Count);
    }

    [Fact]
    public void Capacity_ReturnsCount()
    {
        using var fontManager = new MockFontManager();
        fontManager.LoadFont("a.ttf", 12);

        Assert.Equal(fontManager.Count, fontManager.Capacity);
    }

    [Fact]
    public void Release_RemovesFont()
    {
        using var fontManager = new MockFontManager();
        var font = fontManager.LoadFont("test.ttf", 16);

        var result = fontManager.Release(font);

        Assert.True(result);
        Assert.False(fontManager.IsValid(font));
    }

    [Fact]
    public void Release_ReturnsFalseForInvalidHandle()
    {
        using var fontManager = new MockFontManager();
        var invalidFont = new FontHandle(999);

        var result = fontManager.Release(invalidFont);

        Assert.False(result);
    }

    [Fact]
    public void ReleaseAll_ClearsAllFonts()
    {
        using var fontManager = new MockFontManager();
        fontManager.LoadFont("a.ttf", 12);
        fontManager.LoadFont("b.ttf", 14);

        fontManager.ReleaseAll();

        Assert.Equal(0, fontManager.Count);
    }

    [Fact]
    public void Reset_ClearsAllStateAndResetsDefaults()
    {
        using var fontManager = new MockFontManager();
        fontManager.DefaultCharWidth = 20f;
        fontManager.DefaultLineHeight = 30f;
        fontManager.CharacterWidths['X'] = 50f;
        fontManager.KerningPairs[('A', 'B')] = -5f;
        fontManager.LoadFont("test.ttf", 16);

        fontManager.Reset();

        Assert.Equal(0, fontManager.Count);
        Assert.Empty(fontManager.LoadedFontPaths);
        Assert.Empty(fontManager.CharacterWidths);
        Assert.Empty(fontManager.KerningPairs);
        Assert.Equal(8f, fontManager.DefaultCharWidth);
        Assert.Equal(16f, fontManager.DefaultLineHeight);
    }

    [Fact]
    public void Dispose_ReleasesAllFonts()
    {
        var fontManager = new MockFontManager();
        fontManager.LoadFont("test.ttf", 16);

        fontManager.Dispose();

        Assert.Equal(0, fontManager.Count);
    }

    #endregion
}

public class MockFontInfoTests
{
    [Fact]
    public void MockFontInfo_StoresAllProperties()
    {
        var info = new MockFontInfo("test.ttf", "Test Font", 16f);

        Assert.Equal("test.ttf", info.Path);
        Assert.Equal("Test Font", info.Name);
        Assert.Equal(16f, info.Size);
    }

    [Fact]
    public void MockFontInfo_SupportsCustomMetrics()
    {
        var info = new MockFontInfo("test.ttf", null, 14f)
        {
            CustomLineHeight = 20f,
            CustomAscent = 15f,
            CustomDescent = 5f
        };

        Assert.Equal(20f, info.CustomLineHeight);
        Assert.Equal(15f, info.CustomAscent);
        Assert.Equal(5f, info.CustomDescent);
    }

    [Fact]
    public void MockFontInfo_TracksBaseFont()
    {
        var baseFont = new FontHandle(1);
        var info = new MockFontInfo(null, null, 24f)
        {
            BaseFont = baseFont
        };

        Assert.Equal(baseFont, info.BaseFont);
    }

    [Fact]
    public void MockFontInfo_TracksDataSize()
    {
        var info = new MockFontInfo(null, "Memory Font", 12f)
        {
            DataSize = 2048
        };

        Assert.Equal(2048, info.DataSize);
    }
}
