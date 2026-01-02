// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Security;

namespace KeenEyes.Editor.Tests.Plugins.Security;

/// <summary>
/// Tests for <see cref="SecurityConfiguration"/>.
/// </summary>
public sealed class SecurityConfigurationTests : IDisposable
{
    private readonly string tempDirectory;

    public SecurityConfigurationTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"SecurityConfigTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
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
    public void Default_HasExpectedValues()
    {
        // Act
        var config = SecurityConfiguration.Default;

        // Assert
        Assert.False(config.RequireSignature);
        Assert.True(config.EnableAnalysis);
        Assert.True(config.AllowUnsignedFromTrustedPaths);
        Assert.True(config.EnablePermissions);
        Assert.True(config.PromptForConsent);
        Assert.True(config.RememberPermissionDecisions);
        Assert.Contains("FileSystemProject", config.DefaultPermissions);
        Assert.Contains("ClipboardAccess", config.DefaultPermissions);
    }

    [Fact]
    public void Default_AnalysisConfig_IsWarnOnly()
    {
        // Act
        var config = SecurityConfiguration.Default;

        // Assert
        Assert.Equal(AnalysisMode.WarnOnly, config.AnalysisConfig.Mode);
    }

    #endregion

    #region Strict Configuration Tests

    [Fact]
    public void Strict_HasExpectedValues()
    {
        // Act
        var config = SecurityConfiguration.Strict;

        // Assert
        Assert.True(config.RequireSignature);
        Assert.True(config.EnableAnalysis);
        Assert.False(config.AllowUnsignedFromTrustedPaths);
        Assert.True(config.EnablePermissions);
        Assert.True(config.PromptForConsent);
        Assert.False(config.RememberPermissionDecisions);
        Assert.Empty(config.DefaultPermissions);
        Assert.Empty(config.TrustedPaths);
    }

    [Fact]
    public void Strict_AnalysisConfig_IsBlock()
    {
        // Act
        var config = SecurityConfiguration.Strict;

        // Assert
        Assert.Equal(AnalysisMode.Block, config.AnalysisConfig.Mode);
        Assert.Equal(SecuritySeverity.High, config.AnalysisConfig.BlockingSeverity);
    }

    #endregion

    #region Development Configuration Tests

    [Fact]
    public void Development_HasExpectedValues()
    {
        // Act
        var config = SecurityConfiguration.Development;

        // Assert
        Assert.False(config.RequireSignature);
        Assert.True(config.EnableAnalysis);
        Assert.True(config.AllowUnsignedFromTrustedPaths);
        Assert.False(config.EnablePermissions);
        Assert.False(config.PromptForConsent);
        Assert.True(config.RememberPermissionDecisions);
    }

    [Fact]
    public void Development_AnalysisConfig_IsPermissive()
    {
        // Act
        var config = SecurityConfiguration.Development;

        // Assert
        Assert.Equal(AnalysisMode.WarnOnly, config.AnalysisConfig.Mode);
    }

    [Fact]
    public void Development_HasTrustedPaths()
    {
        // Act
        var config = SecurityConfiguration.Development;

        // Assert
        Assert.NotEmpty(config.TrustedPaths);
        Assert.Contains(config.TrustedPaths, p => p.Contains("nuget"));
    }

    #endregion

    #region Load Tests

    [Fact]
    public void Load_WithNonexistentFile_ReturnsDefault()
    {
        // Arrange
        var nonExistentPath = Path.Combine(tempDirectory, "nonexistent.json");

        // Act
        var config = SecurityConfiguration.Load(nonExistentPath);

        // Assert - Should match default values
        Assert.Equal(SecurityConfiguration.Default.RequireSignature, config.RequireSignature);
        Assert.Equal(SecurityConfiguration.Default.EnableAnalysis, config.EnableAnalysis);
    }

    [Fact]
    public void Load_WithValidFile_LoadsValues()
    {
        // Arrange
        var configPath = Path.Combine(tempDirectory, "config.json");
        var json = """
            {
              "requireSignature": true,
              "enableAnalysis": false,
              "allowUnsignedFromTrustedPaths": false,
              "trustedPaths": ["/custom/path"],
              "enablePermissions": false,
              "promptForConsent": false
            }
            """;
        File.WriteAllText(configPath, json);

        // Act
        var config = SecurityConfiguration.Load(configPath);

        // Assert
        Assert.True(config.RequireSignature);
        Assert.False(config.EnableAnalysis);
        Assert.False(config.AllowUnsignedFromTrustedPaths);
        Assert.Contains("/custom/path", config.TrustedPaths);
        Assert.False(config.EnablePermissions);
        Assert.False(config.PromptForConsent);
    }

    [Fact]
    public void Load_WithInvalidJson_ReturnsDefault()
    {
        // Arrange
        var configPath = Path.Combine(tempDirectory, "invalid.json");
        File.WriteAllText(configPath, "{ invalid json }");

        // Act
        var config = SecurityConfiguration.Load(configPath);

        // Assert - Should match default values
        Assert.Equal(SecurityConfiguration.Default.RequireSignature, config.RequireSignature);
    }

    #endregion

    #region Save Tests

    [Fact]
    public void Save_CreatesFile()
    {
        // Arrange
        var configPath = Path.Combine(tempDirectory, "saved-config.json");
        var config = new SecurityConfiguration
        {
            RequireSignature = true,
            EnableAnalysis = true
        };

        // Act
        config.Save(configPath);

        // Assert
        Assert.True(File.Exists(configPath));
        var json = File.ReadAllText(configPath);
        Assert.Contains("requireSignature", json);
    }

    [Fact]
    public void Save_CreatesDirectoryIfNeeded()
    {
        // Arrange
        var deepPath = Path.Combine(tempDirectory, "nested", "dir", "config.json");
        var config = new SecurityConfiguration();

        // Act
        config.Save(deepPath);

        // Assert
        Assert.True(File.Exists(deepPath));
    }

    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        // Arrange
        var configPath = Path.Combine(tempDirectory, "roundtrip.json");
        var original = new SecurityConfiguration
        {
            RequireSignature = true,
            EnableAnalysis = false,
            AllowUnsignedFromTrustedPaths = true,
            TrustedPaths = ["/path/one", "/path/two"],
            EnablePermissions = true,
            DefaultPermissions = ["CustomPermission"],
            PromptForConsent = false,
            RememberPermissionDecisions = true
        };

        // Act
        original.Save(configPath);
        var loaded = SecurityConfiguration.Load(configPath);

        // Assert
        Assert.Equal(original.RequireSignature, loaded.RequireSignature);
        Assert.Equal(original.EnableAnalysis, loaded.EnableAnalysis);
        Assert.Equal(original.AllowUnsignedFromTrustedPaths, loaded.AllowUnsignedFromTrustedPaths);
        Assert.Equal(original.TrustedPaths.Count, loaded.TrustedPaths.Count);
        Assert.Equal(original.EnablePermissions, loaded.EnablePermissions);
        Assert.Equal(original.PromptForConsent, loaded.PromptForConsent);
    }

    #endregion

    #region IsPathTrusted Tests

    [Fact]
    public void IsPathTrusted_WithMatchingPath_ReturnsTrue()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            TrustedPaths = [tempDirectory]
        };
        var subPath = Path.Combine(tempDirectory, "subdir", "file.dll");

        // Act & Assert
        Assert.True(config.IsPathTrusted(subPath));
    }

    [Fact]
    public void IsPathTrusted_WithNonMatchingPath_ReturnsFalse()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            TrustedPaths = ["/some/trusted/path"]
        };
        var otherPath = Path.Combine(tempDirectory, "untrusted.dll");

        // Act & Assert
        Assert.False(config.IsPathTrusted(otherPath));
    }

    [Fact]
    public void IsPathTrusted_WithEmptyTrustedPaths_ReturnsFalse()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            TrustedPaths = []
        };

        // Act & Assert
        Assert.False(config.IsPathTrusted(tempDirectory));
    }

    [Fact]
    public void IsPathTrusted_IsCaseInsensitive()
    {
        // Arrange
        var config = new SecurityConfiguration
        {
            TrustedPaths = [tempDirectory.ToUpperInvariant()]
        };

        // Act & Assert
        Assert.True(config.IsPathTrusted(tempDirectory.ToLowerInvariant()));
    }

    #endregion

    #region GetDefaultConfigPath Tests

    [Fact]
    public void GetDefaultConfigPath_ReturnsPathInUserProfile()
    {
        // Act
        var path = SecurityConfiguration.GetDefaultConfigPath();

        // Assert
        Assert.Contains(".keeneyes", path);
        Assert.Contains("security-config.json", path);
    }

    #endregion
}
