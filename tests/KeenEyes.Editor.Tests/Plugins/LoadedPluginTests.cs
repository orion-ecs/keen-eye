// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;

namespace KeenEyes.Editor.Tests.Plugins;

/// <summary>
/// Tests for <see cref="LoadedPlugin"/>.
/// </summary>
public sealed class LoadedPluginTests
{
    private static PluginManifest CreateTestManifest(
        string name = "Test Plugin",
        string id = "com.example.test",
        bool supportsHotReload = false,
        bool supportsDisable = true) => new()
        {
            Name = name,
            Id = id,
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint
            {
                Assembly = "TestPlugin.dll",
                Type = "TestPlugin.TestEditorPlugin"
            },
            Capabilities = new PluginCapabilities
            {
                SupportsHotReload = supportsHotReload,
                SupportsDisable = supportsDisable
            }
        };

    [Fact]
    public void Constructor_InitializesWithDiscoveredState()
    {
        // Arrange
        var manifest = CreateTestManifest();

        // Act
        var loadedPlugin = new LoadedPlugin(manifest, "/path/to/plugin");

        // Assert
        Assert.Equal(PluginState.Discovered, loadedPlugin.State);
        Assert.Null(loadedPlugin.Instance);
        Assert.Null(loadedPlugin.ErrorMessage);
    }

    [Fact]
    public void GetAssemblyPath_CombinesBasePathAndAssembly()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var basePath = Path.Combine("plugins", "myplugin");
        var loadedPlugin = new LoadedPlugin(manifest, basePath);

        // Act
        var assemblyPath = loadedPlugin.GetAssemblyPath();

        // Assert - use Path.Combine for platform-independent path construction
        var expected = Path.Combine("plugins", "myplugin", "TestPlugin.dll");
        Assert.Equal(expected, assemblyPath);
    }

    [Fact]
    public void SupportsHotReload_ReflectsManifestCapabilities()
    {
        // Arrange
        var hotReloadManifest = CreateTestManifest(supportsHotReload: true);
        var noHotReloadManifest = CreateTestManifest(supportsHotReload: false);

        // Act
        var hotReloadPlugin = new LoadedPlugin(hotReloadManifest, "/path");
        var noHotReloadPlugin = new LoadedPlugin(noHotReloadManifest, "/path");

        // Assert
        Assert.True(hotReloadPlugin.SupportsHotReload);
        Assert.False(noHotReloadPlugin.SupportsHotReload);
    }

    [Fact]
    public void SupportsDisable_ReflectsManifestCapabilities()
    {
        // Arrange
        var disableManifest = CreateTestManifest(supportsDisable: true);
        var noDisableManifest = CreateTestManifest(supportsDisable: false);

        // Act
        var disablePlugin = new LoadedPlugin(disableManifest, "/path");
        var noDisablePlugin = new LoadedPlugin(noDisableManifest, "/path");

        // Assert
        Assert.True(disablePlugin.SupportsDisable);
        Assert.False(noDisablePlugin.SupportsDisable);
    }

    [Fact]
    public void ToString_IncludesNameIdVersionAndState()
    {
        // Arrange
        var manifest = CreateTestManifest(name: "My Plugin", id: "com.example.myplugin");
        var loadedPlugin = new LoadedPlugin(manifest, "/path");

        // Act
        var str = loadedPlugin.ToString();

        // Assert
        Assert.Contains("My Plugin", str);
        Assert.Contains("com.example.myplugin", str);
        Assert.Contains("1.0.0", str);
        Assert.Contains("Discovered", str);
    }

    [Fact]
    public void State_CanBeUpdated()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var loadedPlugin = new LoadedPlugin(manifest, "/path");

        // Act & Assert - state transitions
        Assert.Equal(PluginState.Discovered, loadedPlugin.State);

        loadedPlugin.State = PluginState.Loaded;
        Assert.Equal(PluginState.Loaded, loadedPlugin.State);

        loadedPlugin.State = PluginState.Enabled;
        Assert.Equal(PluginState.Enabled, loadedPlugin.State);

        loadedPlugin.State = PluginState.Disabled;
        Assert.Equal(PluginState.Disabled, loadedPlugin.State);
    }

    [Fact]
    public void ErrorMessage_CanBeSet()
    {
        // Arrange
        var manifest = CreateTestManifest();
        var loadedPlugin = new LoadedPlugin(manifest, "/path")
        {
            State = PluginState.Failed,
            ErrorMessage = "Assembly not found"
        };

        // Assert
        Assert.Equal(PluginState.Failed, loadedPlugin.State);
        Assert.Equal("Assembly not found", loadedPlugin.ErrorMessage);
    }
}
