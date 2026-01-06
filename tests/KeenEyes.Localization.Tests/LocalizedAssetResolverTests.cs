namespace KeenEyes.Localization.Tests;

public class LocalizedAssetResolverTests : IDisposable
{
    private readonly string testDir;

    public LocalizedAssetResolverTests()
    {
        // Create a unique temp directory for each test
        testDir = Path.Combine(Path.GetTempPath(), $"LocalizedAssetTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDir);
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(testDir))
        {
            Directory.Delete(testDir, recursive: true);
        }
    }

    #region Resolve Tests

    [Fact]
    public void Resolve_ExactLocaleMatch_ReturnsLocalizedPath()
    {
        // Arrange
        CreateFile("textures/logo.en-US.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", Locale.EnglishUS);

        // Assert
        result.ShouldBe("textures/logo.en-US.png");
    }

    [Fact]
    public void Resolve_LanguageOnlyFallback_ReturnsLanguagePath()
    {
        // Arrange
        CreateFile("textures/logo.en.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", Locale.EnglishUS);

        // Assert
        result.ShouldBe("textures/logo.en.png");
    }

    [Fact]
    public void Resolve_DefaultFallback_ReturnsUnlocalizedPath()
    {
        // Arrange
        CreateFile("textures/logo.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", Locale.JapaneseJP);

        // Assert
        result.ShouldBe("textures/logo.png");
    }

    [Fact]
    public void Resolve_NoMatchingFile_ReturnsNull()
    {
        // Arrange
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/nonexistent", Locale.EnglishUS);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_EmptyAssetKey_ReturnsNull()
    {
        // Arrange
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("", Locale.EnglishUS);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Resolve_FallbackDisabled_OnlyChecksCurrentLocale()
    {
        // Arrange
        CreateFile("textures/logo.png"); // Default (unlocalized)
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            EnableFallback = false
        };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", Locale.JapaneseJP);

        // Assert - with fallback disabled, should still find default but not through full fallback chain
        result.ShouldBe("textures/logo.png");
    }

    [Fact]
    public void Resolve_CustomFallbackOverride_UsesFallbackLocale()
    {
        // Arrange
        CreateFile("textures/logo.en-GB.png");
        var config = new LocalizationConfig
        {
            DefaultLocale = Locale.EnglishUS,
            FallbackOverrides =
            {
                [new Locale("en-AU")] = Locale.EnglishGB
            }
        };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", new Locale("en-AU"));

        // Assert - Should use en-GB from FallbackOverrides
        result.ShouldBe("textures/logo.en-GB.png");
    }

    [Fact]
    public void Resolve_WithExtension_PreservesExtension()
    {
        // Arrange
        CreateFile("audio/music.en-US.ogg");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("audio/music.ogg", Locale.EnglishUS);

        // Assert
        result.ShouldBe("audio/music.en-US.ogg");
    }

    [Fact]
    public void Resolve_PrioritizesExactLocaleOverLanguageOnly()
    {
        // Arrange
        CreateFile("textures/logo.en.png");
        CreateFile("textures/logo.en-US.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.Resolve("textures/logo", Locale.EnglishUS);

        // Assert - Should prefer exact match
        result.ShouldBe("textures/logo.en-US.png");
    }

    [Fact]
    public void Resolve_FallsBackToDefaultLocale_WhenRequestedLocaleNotFound()
    {
        // Arrange
        CreateFile("textures/logo.en-US.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act - Request Japanese, but only English exists
        var result = resolver.Resolve("textures/logo", Locale.JapaneseJP);

        // Assert - Should fall back to default locale's asset
        result.ShouldBe("textures/logo.en-US.png");
    }

    #endregion

    #region HasLocaleVariant Tests

    [Fact]
    public void HasLocaleVariant_FileExists_ReturnsTrue()
    {
        // Arrange
        CreateFile("textures/logo.ja-JP.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.HasLocaleVariant("textures/logo", Locale.JapaneseJP);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HasLocaleVariant_FileNotExists_ReturnsFalse()
    {
        // Arrange
        CreateFile("textures/logo.en-US.png");
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.HasLocaleVariant("textures/logo", Locale.JapaneseJP);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HasLocaleVariant_EmptyAssetKey_ReturnsFalse()
    {
        // Arrange
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var result = resolver.HasLocaleVariant("", Locale.EnglishUS);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetAllPaths Tests

    [Fact]
    public void GetAllPaths_ReturnsAllMatchingFiles()
    {
        // Arrange
        CreateFile("textures/logo.png");
        CreateFile("textures/logo.en-US.png");
        CreateFile("textures/logo.ja-JP.png");
        CreateFile("textures/other.png"); // Should not be included
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var results = resolver.GetAllPaths("textures/logo").ToList();

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldContain("textures/logo.png");
        results.ShouldContain("textures/logo.en-US.png");
        results.ShouldContain("textures/logo.ja-JP.png");
    }

    [Fact]
    public void GetAllPaths_NoMatchingFiles_ReturnsEmpty()
    {
        // Arrange
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var results = resolver.GetAllPaths("textures/nonexistent").ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    [Fact]
    public void GetAllPaths_EmptyAssetKey_ReturnsEmpty()
    {
        // Arrange
        var config = new LocalizationConfig { DefaultLocale = Locale.EnglishUS };
        var resolver = new LocalizedAssetResolver(testDir, config);

        // Act
        var results = resolver.GetAllPaths("").ToList();

        // Assert
        results.ShouldBeEmpty();
    }

    #endregion

    #region Helper Methods

    private void CreateFile(string relativePath)
    {
        var fullPath = Path.Combine(testDir, relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllBytes(fullPath, [0x00]);
    }

    #endregion
}
