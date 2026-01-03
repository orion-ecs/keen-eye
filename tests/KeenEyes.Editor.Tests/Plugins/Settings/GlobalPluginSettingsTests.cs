// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Settings;

namespace KeenEyes.Editor.Tests.Plugins.Settings;

/// <summary>
/// Tests for <see cref="GlobalPluginSettings"/>.
/// </summary>
public sealed class GlobalPluginSettingsTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string settingsPath;

    public GlobalPluginSettingsTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"GlobalPluginSettingsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        settingsPath = Path.Combine(tempDirectory, "plugin-settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region Default Configuration Tests

    [Fact]
    public void Default_HasExpectedHotReloadSettings()
    {
        // Act
        var settings = GlobalPluginSettings.Default;

        // Assert
        Assert.True(settings.HotReload.Enabled);
    }

    [Fact]
    public void Default_HasExpectedDeveloperSettings()
    {
        // Act
        var settings = GlobalPluginSettings.Default;

        // Assert
        Assert.False(settings.Developer.Enabled);
        Assert.False(settings.Developer.VerboseLogging);
        Assert.False(settings.Developer.ShowInternalErrors);
    }

    [Fact]
    public void Default_HasExpectedSecuritySettings()
    {
        // Act
        var settings = GlobalPluginSettings.Default;

        // Assert
        Assert.False(settings.Security.RequireCodeSigning);
        Assert.True(settings.Security.EnablePermissionSystem);
        Assert.True(settings.Security.WarnUntrustedPublishers);
    }

    #endregion

    #region Development Settings Tests

    [Fact]
    public void CreateDevelopmentSettings_EnablesHotReload()
    {
        // Act
        var settings = GlobalPluginSettings.CreateDevelopmentSettings();

        // Assert
        Assert.True(settings.HotReload.Enabled);
    }

    [Fact]
    public void CreateDevelopmentSettings_EnablesDeveloperMode()
    {
        // Act
        var settings = GlobalPluginSettings.CreateDevelopmentSettings();

        // Assert
        Assert.True(settings.Developer.Enabled);
        Assert.True(settings.Developer.VerboseLogging);
        Assert.True(settings.Developer.ShowInternalErrors);
    }

    [Fact]
    public void CreateDevelopmentSettings_RelaxesSecurity()
    {
        // Act
        var settings = GlobalPluginSettings.CreateDevelopmentSettings();

        // Assert
        Assert.False(settings.Security.RequireCodeSigning);
        Assert.False(settings.Security.EnablePermissionSystem);
        Assert.False(settings.Security.WarnUntrustedPublishers);
    }

    #endregion

    #region Production Settings Tests

    [Fact]
    public void CreateProductionSettings_DisablesHotReload()
    {
        // Act
        var settings = GlobalPluginSettings.CreateProductionSettings();

        // Assert
        Assert.False(settings.HotReload.Enabled);
    }

    [Fact]
    public void CreateProductionSettings_DisablesDeveloperMode()
    {
        // Act
        var settings = GlobalPluginSettings.CreateProductionSettings();

        // Assert
        Assert.False(settings.Developer.Enabled);
        Assert.False(settings.Developer.VerboseLogging);
        Assert.False(settings.Developer.ShowInternalErrors);
    }

    [Fact]
    public void CreateProductionSettings_EnablesSecurity()
    {
        // Act
        var settings = GlobalPluginSettings.CreateProductionSettings();

        // Assert
        Assert.True(settings.Security.RequireCodeSigning);
        Assert.True(settings.Security.EnablePermissionSystem);
        Assert.True(settings.Security.WarnUntrustedPublishers);
    }

    #endregion

    #region Persistence Tests

    [Fact]
    public void Save_CreatesFile()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.SetHotReloadEnabled(false);

        // Act
        settings.Save();

        // Assert
        Assert.True(File.Exists(settingsPath));
    }

    [Fact]
    public void Load_RestoresSavedSettings()
    {
        // Arrange
        var original = new GlobalPluginSettings(settingsPath);
        original.SetHotReloadEnabled(false);
        original.SetDeveloperModeEnabled(true);
        original.SetVerboseLoggingEnabled(true);
        original.Save();

        // Act
        var loaded = new GlobalPluginSettings(settingsPath);
        loaded.Load();

        // Assert
        Assert.False(loaded.HotReload.Enabled);
        Assert.True(loaded.Developer.Enabled);
        Assert.True(loaded.Developer.VerboseLogging);
    }

    [Fact]
    public void Load_WithMissingFile_UsesDefaults()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.Load();

        // Assert - should have default values
        Assert.True(settings.HotReload.Enabled);
        Assert.False(settings.Developer.Enabled);
    }

    [Fact]
    public void Load_WithCorruptedFile_UsesDefaults()
    {
        // Arrange
        File.WriteAllText(settingsPath, "{ invalid json }}}");
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.Load();

        // Assert - should have default values
        Assert.True(settings.HotReload.Enabled);
        Assert.False(settings.Developer.Enabled);
    }

    #endregion

    #region Source Management Tests

    [Fact]
    public void GetSources_ReturnsDefaultNuGetSource()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        var sources = settings.GetSources();

        // Assert
        Assert.Single(sources);
        Assert.Equal("nuget.org", sources[0].Name);
        Assert.Contains("nuget.org", sources[0].Url);
    }

    [Fact]
    public void AddSource_AddsNewSource()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.AddSource("MyFeed", "https://mycompany.com/nuget/v3/index.json");

        // Assert
        var sources = settings.GetSources();
        Assert.Equal(2, sources.Count);
        Assert.Contains(sources, s => s.Name == "MyFeed");
    }

    [Fact]
    public void AddSource_WithMakeDefault_SetsAsDefault()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.AddSource("MyFeed", "https://mycompany.com/nuget/v3/index.json", makeDefault: true);

        // Assert
        var sources = settings.GetSources();
        Assert.False(sources.First(s => s.Name == "nuget.org").IsDefault);
        Assert.True(sources.First(s => s.Name == "MyFeed").IsDefault);
    }

    [Fact]
    public void RemoveSource_RemovesExistingSource()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSource("MyFeed", "https://mycompany.com/nuget/v3/index.json");

        // Act
        var result = settings.RemoveSource("MyFeed");

        // Assert
        Assert.True(result);
        Assert.Single(settings.GetSources());
    }

    [Fact]
    public void RemoveSource_WithNonexistentSource_ReturnsFalse()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        var result = settings.RemoveSource("NonexistentFeed");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void SetSourceEnabled_DisablesSource()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        var result = settings.SetSourceEnabled("nuget.org", false);

        // Assert
        Assert.True(result);
        Assert.Empty(settings.GetEnabledSources());
    }

    [Fact]
    public void GetDefaultSourceUrl_ReturnsDefaultSourceUrl()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        var url = settings.GetDefaultSourceUrl();

        // Assert
        Assert.Contains("nuget.org", url);
    }

    [Fact]
    public void MoveSourceUp_MovesSourceToHigherPriority()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSource("MyFeed", "https://mycompany.com/nuget/v3/index.json");

        // Act
        var result = settings.MoveSourceUp("MyFeed");

        // Assert
        Assert.True(result);
        Assert.Equal("MyFeed", settings.GetSources()[0].Name);
    }

    [Fact]
    public void MoveSourceDown_MovesSourceToLowerPriority()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSource("MyFeed", "https://mycompany.com/nuget/v3/index.json");

        // Act
        var result = settings.MoveSourceDown("nuget.org");

        // Assert
        Assert.True(result);
        Assert.Equal("MyFeed", settings.GetSources()[0].Name);
    }

    #endregion

    #region Search Path Tests

    [Fact]
    public void GetSearchPaths_InitiallyEmpty()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        var paths = settings.GetSearchPaths();

        // Assert
        Assert.Empty(paths);
    }

    [Fact]
    public void AddSearchPath_AddsNewPath()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.AddSearchPath(tempDirectory, "Test plugins");

        // Assert
        var paths = settings.GetSearchPaths();
        Assert.Single(paths);
        Assert.Equal(tempDirectory, paths[0].Path);
        Assert.Equal("Test plugins", paths[0].Description);
    }

    [Fact]
    public void AddSearchPath_WithDuplicate_DoesNotAdd()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSearchPath(tempDirectory, "First");

        // Act
        settings.AddSearchPath(tempDirectory, "Second");

        // Assert
        var paths = settings.GetSearchPaths();
        Assert.Single(paths);
        Assert.Equal("First", paths[0].Description);
    }

    [Fact]
    public void RemoveSearchPath_RemovesExistingPath()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSearchPath(tempDirectory, "Test");

        // Act
        var result = settings.RemoveSearchPath(tempDirectory);

        // Assert
        Assert.True(result);
        Assert.Empty(settings.GetSearchPaths());
    }

    [Fact]
    public void SetSearchPathEnabled_DisablesPath()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        settings.AddSearchPath(tempDirectory, "Test");

        // Act
        var result = settings.SetSearchPathEnabled(tempDirectory, false);

        // Assert
        Assert.True(result);
        Assert.Empty(settings.GetEnabledSearchPaths());
    }

    [Fact]
    public void MoveSearchPathUp_MovesPath()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);
        var path1 = Path.Combine(tempDirectory, "dir1");
        var path2 = Path.Combine(tempDirectory, "dir2");
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);
        settings.AddSearchPath(path1);
        settings.AddSearchPath(path2);

        // Act
        var result = settings.MoveSearchPathUp(path2);

        // Assert
        Assert.True(result);
        Assert.Equal(path2, settings.GetSearchPaths()[0].Path);
    }

    #endregion

    #region Option Toggle Tests

    [Fact]
    public void SetHotReloadEnabled_UpdatesValue()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.SetHotReloadEnabled(false);

        // Assert
        Assert.False(settings.HotReload.Enabled);
    }

    [Fact]
    public void SetDeveloperModeEnabled_UpdatesValue()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.SetDeveloperModeEnabled(true);

        // Assert
        Assert.True(settings.Developer.Enabled);
    }

    [Fact]
    public void SetVerboseLoggingEnabled_UpdatesValue()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.SetVerboseLoggingEnabled(true);

        // Assert
        Assert.True(settings.Developer.VerboseLogging);
    }

    [Fact]
    public void SetCodeSigningRequired_UpdatesValue()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.SetCodeSigningRequired(true);

        // Assert
        Assert.True(settings.Security.RequireCodeSigning);
    }

    [Fact]
    public void SetPermissionSystemEnabled_UpdatesValue()
    {
        // Arrange
        var settings = new GlobalPluginSettings(settingsPath);

        // Act
        settings.SetPermissionSystemEnabled(false);

        // Assert
        Assert.False(settings.Security.EnablePermissionSystem);
    }

    #endregion
}
