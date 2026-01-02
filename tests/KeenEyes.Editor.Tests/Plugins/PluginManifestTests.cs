// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for <see cref="PluginManifest"/> JSON parsing.
/// </summary>
public sealed class PluginManifestTests
{
    #region Parse Tests

    [Fact]
    public void Parse_WithValidJson_ReturnsManifest()
    {
        // Arrange
        var json = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "author": "Test Author",
                "description": "A test plugin",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.TestEditorPlugin"
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.Equal("Test Plugin", manifest.Name);
        Assert.Equal("com.example.test", manifest.Id);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("Test Author", manifest.Author);
        Assert.Equal("A test plugin", manifest.Description);
        Assert.Equal("TestPlugin.dll", manifest.EntryPoint.Assembly);
        Assert.Equal("TestPlugin.TestEditorPlugin", manifest.EntryPoint.Type);
    }

    [Fact]
    public void Parse_WithCapabilities_ParsesHotReloadFlag()
    {
        // Arrange
        var json = """
            {
                "name": "Hot Reload Plugin",
                "id": "com.example.hotreload",
                "version": "2.0.0",
                "entryPoint": {
                    "assembly": "HotReloadPlugin.dll",
                    "type": "HotReloadPlugin.Plugin"
                },
                "capabilities": {
                    "supportsHotReload": true,
                    "supportsDisable": true
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.True(manifest.Capabilities.SupportsHotReload);
        Assert.True(manifest.Capabilities.SupportsDisable);
    }

    [Fact]
    public void Parse_WithCompatibility_ParsesVersions()
    {
        // Arrange
        var json = """
            {
                "name": "Versioned Plugin",
                "id": "com.example.versioned",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "VersionedPlugin.dll",
                    "type": "VersionedPlugin.Plugin"
                },
                "compatibility": {
                    "minEditorVersion": "1.0.0",
                    "maxEditorVersion": "2.0.0"
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.NotNull(manifest.Compatibility);
        Assert.Equal("1.0.0", manifest.Compatibility.MinEditorVersion);
        Assert.Equal("2.0.0", manifest.Compatibility.MaxEditorVersion);
    }

    [Fact]
    public void Parse_WithDependencies_ParsesDependencyMap()
    {
        // Arrange
        var json = """
            {
                "name": "Dependent Plugin",
                "id": "com.example.dependent",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "DependentPlugin.dll",
                    "type": "DependentPlugin.Plugin"
                },
                "dependencies": {
                    "com.example.base": ">=1.0.0",
                    "com.example.utils": "^2.0.0"
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.Equal(2, manifest.Dependencies.Count);
        Assert.Equal(">=1.0.0", manifest.Dependencies["com.example.base"]);
        Assert.Equal("^2.0.0", manifest.Dependencies["com.example.utils"]);
    }

    [Fact]
    public void Parse_WithSettings_ParsesConfigFile()
    {
        // Arrange
        var json = """
            {
                "name": "Configurable Plugin",
                "id": "com.example.configurable",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "ConfigurablePlugin.dll",
                    "type": "ConfigurablePlugin.Plugin"
                },
                "settings": {
                    "configFile": "config.json"
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.NotNull(manifest.Settings);
        Assert.Equal("config.json", manifest.Settings.ConfigFile);
    }

    [Fact]
    public void Parse_WithMinimalJson_UsesDefaults()
    {
        // Arrange
        var json = """
            {
                "name": "Minimal Plugin",
                "id": "com.example.minimal",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "MinimalPlugin.dll",
                    "type": "MinimalPlugin.Plugin"
                }
            }
            """;

        // Act
        var manifest = PluginManifest.Parse(json);

        // Assert
        Assert.Null(manifest.Author);
        Assert.Null(manifest.Description);
        Assert.Null(manifest.Compatibility);
        Assert.Null(manifest.Settings);
        Assert.False(manifest.Capabilities.SupportsHotReload);
        Assert.True(manifest.Capabilities.SupportsDisable); // Default is true
        Assert.Empty(manifest.Dependencies);
    }

    [Fact]
    public void Parse_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => PluginManifest.Parse(json));
    }

    [Fact]
    public void Parse_WithMissingRequiredFields_ThrowsJsonException()
    {
        // Arrange - missing entryPoint
        var json = """
            {
                "name": "Incomplete Plugin",
                "id": "com.example.incomplete",
                "version": "1.0.0"
            }
            """;

        // Act & Assert
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => PluginManifest.Parse(json));
    }

    #endregion

    #region TryParse Tests

    [Fact]
    public void TryParse_WithValidJson_ReturnsTrue()
    {
        // Arrange
        var json = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.Plugin"
                }
            }
            """;

        // Act
        var result = PluginManifest.TryParse(json, out var manifest);

        // Assert
        Assert.True(result);
        Assert.NotNull(manifest);
        Assert.Equal("Test Plugin", manifest.Name);
    }

    [Fact]
    public void TryParse_WithInvalidJson_ReturnsFalse()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act
        var result = PluginManifest.TryParse(json, out var manifest);

        // Assert
        Assert.False(result);
        Assert.Null(manifest);
    }

    #endregion

    #region ToJson Tests

    [Fact]
    public void ToJson_SerializesCorrectly()
    {
        // Arrange
        var manifest = new PluginManifest
        {
            Name = "Test Plugin",
            Id = "com.example.test",
            Version = "1.0.0",
            Author = "Test Author",
            EntryPoint = new PluginEntryPoint
            {
                Assembly = "TestPlugin.dll",
                Type = "TestPlugin.Plugin"
            }
        };

        // Act
        var json = manifest.ToJson();

        // Assert
        Assert.Contains("\"name\": \"Test Plugin\"", json);
        Assert.Contains("\"id\": \"com.example.test\"", json);
        Assert.Contains("\"version\": \"1.0.0\"", json);
    }

    [Fact]
    public void ToJson_AndParse_RoundTrips()
    {
        // Arrange
        var original = new PluginManifest
        {
            Name = "Round Trip Plugin",
            Id = "com.example.roundtrip",
            Version = "1.2.3",
            Author = "Test Author",
            Description = "A test description",
            EntryPoint = new PluginEntryPoint
            {
                Assembly = "RoundTrip.dll",
                Type = "RoundTrip.Plugin"
            },
            Capabilities = new PluginCapabilities
            {
                SupportsHotReload = true,
                SupportsDisable = true
            }
        };

        // Act
        var json = original.ToJson();
        var parsed = PluginManifest.Parse(json);

        // Assert
        Assert.Equal(original.Name, parsed.Name);
        Assert.Equal(original.Id, parsed.Id);
        Assert.Equal(original.Version, parsed.Version);
        Assert.Equal(original.Author, parsed.Author);
        Assert.Equal(original.Description, parsed.Description);
        Assert.Equal(original.EntryPoint.Assembly, parsed.EntryPoint.Assembly);
        Assert.Equal(original.EntryPoint.Type, parsed.EntryPoint.Type);
        Assert.Equal(original.Capabilities.SupportsHotReload, parsed.Capabilities.SupportsHotReload);
        Assert.Equal(original.Capabilities.SupportsDisable, parsed.Capabilities.SupportsDisable);
    }

    #endregion
}
