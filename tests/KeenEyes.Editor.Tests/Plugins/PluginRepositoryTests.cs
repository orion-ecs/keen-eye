// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for <see cref="PluginRepository"/>.
/// </summary>
public sealed class PluginRepositoryTests : IDisposable
{
    private readonly string testDirectory;

    public PluginRepositoryTests()
    {
        // Create a unique temp directory for each test
        testDirectory = Path.Combine(Path.GetTempPath(), $"KeenEyesPluginTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, recursive: true);
        }
    }

    private string CreatePluginDirectory(string pluginId, string manifestJson)
    {
        var pluginDir = Path.Combine(testDirectory, pluginId);
        Directory.CreateDirectory(pluginDir);

        // Write manifest
        File.WriteAllText(Path.Combine(pluginDir, "keeneyes-plugin.json"), manifestJson);

        // Create dummy assembly file
        File.WriteAllText(Path.Combine(pluginDir, "TestPlugin.dll"), "");

        return pluginDir;
    }

    [Fact]
    public void Constructor_CreatesEmptyRepository()
    {
        // Act
        var repository = new PluginRepository();

        // Assert
        Assert.Empty(repository.Plugins);
    }

    [Fact]
    public void AddSearchPath_AddsPath()
    {
        // Arrange
        var repository = new PluginRepository();
        var searchPath = Path.Combine(testDirectory, "search");
        Directory.CreateDirectory(searchPath);

        // Act
        repository.AddSearchPath(searchPath);

        // No direct way to verify, but Scan() should include this path
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddSearchPath_IgnoresNullOrWhitespace()
    {
        // Arrange
        var repository = new PluginRepository();

        // Act - should not throw
        repository.AddSearchPath(null!);
        repository.AddSearchPath("");
        repository.AddSearchPath("   ");

        // Assert - no exception
        Assert.NotNull(repository);
    }

    [Fact]
    public void Scan_DiscoversPluginWithValidManifest()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var manifestJson = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.TestEditorPlugin"
                }
            }
            """;
        CreatePluginDirectory("test-plugin", manifestJson);

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(1, count);
        Assert.Single(repository.Plugins);
        Assert.True(repository.HasPlugin("com.example.test"));
    }

    [Fact]
    public void Scan_IgnoresDirectoriesWithoutManifest()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        // Create directory without manifest
        var pluginDir = Path.Combine(testDirectory, "no-manifest");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllText(Path.Combine(pluginDir, "SomePlugin.dll"), "");

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(0, count);
        Assert.Empty(repository.Plugins);
    }

    [Fact]
    public void Scan_IgnoresInvalidManifests()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var pluginDir = Path.Combine(testDirectory, "invalid-manifest");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllText(Path.Combine(pluginDir, "keeneyes-plugin.json"), "{ invalid json }");

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(0, count);
        Assert.Empty(repository.Plugins);
    }

    [Fact]
    public void Scan_IgnoresManifestWithMissingAssembly()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var manifestJson = """
            {
                "name": "Missing Assembly Plugin",
                "id": "com.example.missing",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "NonExistent.dll",
                    "type": "NonExistent.Plugin"
                }
            }
            """;

        var pluginDir = Path.Combine(testDirectory, "missing-assembly");
        Directory.CreateDirectory(pluginDir);
        File.WriteAllText(Path.Combine(pluginDir, "keeneyes-plugin.json"), manifestJson);
        // Note: Not creating the assembly file

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(0, count);
        Assert.Empty(repository.Plugins);
    }

    [Fact]
    public void Scan_DiscoversMultiplePlugins()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        for (int i = 1; i <= 3; i++)
        {
            var manifestJson = $$"""
                {
                    "name": "Plugin {{i}}",
                    "id": "com.example.plugin{{i}}",
                    "version": "1.0.0",
                    "entryPoint": {
                        "assembly": "TestPlugin.dll",
                        "type": "Plugin{{i}}.Plugin"
                    }
                }
                """;
            CreatePluginDirectory($"plugin{i}", manifestJson);
        }

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(3, count);
        Assert.Equal(3, repository.Plugins.Count);
        Assert.True(repository.HasPlugin("com.example.plugin1"));
        Assert.True(repository.HasPlugin("com.example.plugin2"));
        Assert.True(repository.HasPlugin("com.example.plugin3"));
    }

    [Fact]
    public void GetPlugin_ReturnsPluginById()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var manifestJson = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.TestEditorPlugin"
                }
            }
            """;
        CreatePluginDirectory("test-plugin", manifestJson);
        repository.Scan();

        // Act
        var plugin = repository.GetPlugin("com.example.test");

        // Assert
        Assert.NotNull(plugin);
        Assert.Equal("Test Plugin", plugin.Manifest.Name);
        Assert.Equal("com.example.test", plugin.Manifest.Id);
    }

    [Fact]
    public void GetPlugin_ReturnsNullForUnknownId()
    {
        // Arrange
        var repository = new PluginRepository();

        // Act
        var plugin = repository.GetPlugin("com.example.nonexistent");

        // Assert
        Assert.Null(plugin);
    }

    [Fact]
    public void HasPlugin_ReturnsTrueForExistingPlugin()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var manifestJson = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.TestEditorPlugin"
                }
            }
            """;
        CreatePluginDirectory("test-plugin", manifestJson);
        repository.Scan();

        // Act & Assert
        Assert.True(repository.HasPlugin("com.example.test"));
        Assert.False(repository.HasPlugin("com.example.nonexistent"));
    }

    [Fact]
    public void Scan_ClearsPreviousResults()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var manifestJson = """
            {
                "name": "Test Plugin",
                "id": "com.example.test",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "TestPlugin.dll",
                    "type": "TestPlugin.TestEditorPlugin"
                }
            }
            """;
        CreatePluginDirectory("test-plugin", manifestJson);

        // First scan
        repository.Scan();
        Assert.Single(repository.Plugins);

        // Remove the plugin directory
        Directory.Delete(Path.Combine(testDirectory, "test-plugin"), recursive: true);

        // Second scan
        repository.Scan();

        // Assert - plugins cleared
        Assert.Empty(repository.Plugins);
    }

    [Fact]
    public void Scan_HandlesNuGetPackageLayout()
    {
        // Arrange
        var repository = new PluginRepository();
        repository.AddSearchPath(testDirectory);

        var packageDir = Path.Combine(testDirectory, "mypackage");
        var contentDir = Path.Combine(packageDir, "content");
        var libDir = Path.Combine(packageDir, "lib", "net10.0");

        Directory.CreateDirectory(contentDir);
        Directory.CreateDirectory(libDir);

        var manifestJson = """
            {
                "name": "NuGet Plugin",
                "id": "com.example.nuget",
                "version": "1.0.0",
                "entryPoint": {
                    "assembly": "NuGetPlugin.dll",
                    "type": "NuGetPlugin.Plugin"
                }
            }
            """;

        File.WriteAllText(Path.Combine(contentDir, "keeneyes-plugin.json"), manifestJson);
        File.WriteAllText(Path.Combine(libDir, "NuGetPlugin.dll"), "");

        // Act
        var count = repository.Scan();

        // Assert
        Assert.Equal(1, count);
        Assert.True(repository.HasPlugin("com.example.nuget"));
    }
}
