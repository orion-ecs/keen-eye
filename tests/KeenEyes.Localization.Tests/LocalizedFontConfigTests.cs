namespace KeenEyes.Localization.Tests;

public class LocalizedFontConfigTests
{
    #region Default Values

    [Fact]
    public void Default_HasNullPrimaryFont()
    {
        var config = LocalizedFontConfig.Default;

        config.PrimaryFont.ShouldBeNull();
    }

    [Fact]
    public void Default_HasNullFallbackFonts()
    {
        var config = LocalizedFontConfig.Default;

        config.FallbackFonts.ShouldBeNull();
    }

    [Fact]
    public void Default_SizeMultiplierIsOne()
    {
        var config = LocalizedFontConfig.Default;

        config.SizeMultiplier.ShouldBe(1.0f);
    }

    [Fact]
    public void Default_LineHeightMultiplierIsOne()
    {
        var config = LocalizedFontConfig.Default;

        config.LineHeightMultiplier.ShouldBe(1.0f);
    }

    #endregion

    #region GetAllFontPaths

    [Fact]
    public void GetAllFontPaths_WithPrimaryOnly_ReturnsPrimary()
    {
        var config = new LocalizedFontConfig
        {
            PrimaryFont = "fonts/Roboto.ttf"
        };

        var paths = config.GetAllFontPaths().ToList();

        paths.Count.ShouldBe(1);
        paths[0].ShouldBe("fonts/Roboto.ttf");
    }

    [Fact]
    public void GetAllFontPaths_WithFallbacks_ReturnsAllFonts()
    {
        var config = new LocalizedFontConfig
        {
            PrimaryFont = "fonts/NotoSansJP.ttf",
            FallbackFonts = ["fonts/Roboto.ttf", "fonts/NotoEmoji.ttf"]
        };

        var paths = config.GetAllFontPaths().ToList();

        paths.Count.ShouldBe(3);
        paths[0].ShouldBe("fonts/NotoSansJP.ttf");
        paths[1].ShouldBe("fonts/Roboto.ttf");
        paths[2].ShouldBe("fonts/NotoEmoji.ttf");
    }

    [Fact]
    public void GetAllFontPaths_WithNoPrimary_ReturnsOnlyFallbacks()
    {
        var config = new LocalizedFontConfig
        {
            FallbackFonts = ["fonts/Roboto.ttf"]
        };

        var paths = config.GetAllFontPaths().ToList();

        paths.Count.ShouldBe(1);
        paths[0].ShouldBe("fonts/Roboto.ttf");
    }

    [Fact]
    public void GetAllFontPaths_Empty_ReturnsEmpty()
    {
        var config = new LocalizedFontConfig();

        var paths = config.GetAllFontPaths().ToList();

        paths.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllFontPaths_SkipsNullAndEmptyFallbacks()
    {
        var config = new LocalizedFontConfig
        {
            PrimaryFont = "fonts/Roboto.ttf",
            FallbackFonts = ["fonts/Valid.ttf", null!, "", "fonts/Another.ttf"]
        };

        var paths = config.GetAllFontPaths().ToList();

        paths.Count.ShouldBe(3);
        paths[0].ShouldBe("fonts/Roboto.ttf");
        paths[1].ShouldBe("fonts/Valid.ttf");
        paths[2].ShouldBe("fonts/Another.ttf");
    }

    #endregion

    #region Custom Values

    [Fact]
    public void CustomSizeMultiplier_IsPreserved()
    {
        var config = new LocalizedFontConfig
        {
            SizeMultiplier = 1.2f
        };

        config.SizeMultiplier.ShouldBe(1.2f);
    }

    [Fact]
    public void CustomLineHeightMultiplier_IsPreserved()
    {
        var config = new LocalizedFontConfig
        {
            LineHeightMultiplier = 1.5f
        };

        config.LineHeightMultiplier.ShouldBe(1.5f);
    }

    #endregion
}
