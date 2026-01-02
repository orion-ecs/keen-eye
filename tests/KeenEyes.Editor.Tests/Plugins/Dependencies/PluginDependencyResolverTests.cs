// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins;
using KeenEyes.Editor.Plugins.Dependencies;

namespace KeenEyes.Editor.Tests.Plugins.Dependencies;

/// <summary>
/// Tests for <see cref="PluginDependencyResolver"/>.
/// </summary>
public sealed class PluginDependencyResolverTests
{
    private const string EditorVersion = "1.0.0";

    private static LoadedPlugin CreatePlugin(
        string id,
        string version = "1.0.0",
        Dictionary<string, string>? dependencies = null,
        string? minEditorVersion = null,
        string? maxEditorVersion = null)
    {
        var manifest = new PluginManifest
        {
            Id = id,
            Name = $"Plugin {id}",
            Version = version,
            EntryPoint = new PluginEntryPoint
            {
                Assembly = $"{id}.dll",
                Type = $"{id}.Plugin"
            },
            Dependencies = dependencies ?? [],
            Compatibility = (minEditorVersion != null || maxEditorVersion != null)
                ? new PluginCompatibility
                {
                    MinEditorVersion = minEditorVersion,
                    MaxEditorVersion = maxEditorVersion
                }
                : null
        };

        return new LoadedPlugin(manifest, $"/plugins/{id}");
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidVersion_Succeeds()
    {
        // Arrange & Act
        var resolver = new PluginDependencyResolver("1.0.0");

        // Assert - no exception thrown
        Assert.NotNull(resolver);
    }

    [Fact]
    public void Constructor_WithInvalidVersion_ThrowsArgumentException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new PluginDependencyResolver("invalid"));
    }

    [Fact]
    public void Constructor_WithNullVersion_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PluginDependencyResolver(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyVersion_ThrowsArgumentException(string version)
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentException>(() => new PluginDependencyResolver(version));
    }

    #endregion

    #region Resolve Tests - Success Cases

    [Fact]
    public void Resolve_EmptyPlugins_ReturnsSuccess()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>();

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.LoadOrder);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Resolve_SinglePlugin_ReturnsSuccess()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.True(result.IsValid);
        Assert.Single(result.LoadOrder);
        Assert.Equal("plugin-a", result.LoadOrder[0]);
    }

    [Fact]
    public void Resolve_SimpleChain_ReturnsDependenciesFirst()
    {
        // Arrange: A depends on B, B depends on C
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = ">=1.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", dependencies: new() { ["plugin-c"] = ">=1.0.0" }),
            ["plugin-c"] = CreatePlugin("plugin-c")
        };

        // Act
        var result = resolver.Resolve(plugins);
        var loadOrder = result.LoadOrder.ToList();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(3, loadOrder.Count);
        Assert.True(loadOrder.IndexOf("plugin-c") < loadOrder.IndexOf("plugin-b"));
        Assert.True(loadOrder.IndexOf("plugin-b") < loadOrder.IndexOf("plugin-a"));
    }

    [Fact]
    public void Resolve_DiamondPattern_ReturnsDependenciesFirst()
    {
        // Arrange: A depends on B and C, B and C depend on D
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new()
            {
                ["plugin-b"] = ">=1.0.0",
                ["plugin-c"] = ">=1.0.0"
            }),
            ["plugin-b"] = CreatePlugin("plugin-b", dependencies: new() { ["plugin-d"] = ">=1.0.0" }),
            ["plugin-c"] = CreatePlugin("plugin-c", dependencies: new() { ["plugin-d"] = ">=1.0.0" }),
            ["plugin-d"] = CreatePlugin("plugin-d")
        };

        // Act
        var result = resolver.Resolve(plugins);
        var loadOrder = result.LoadOrder.ToList();

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(4, loadOrder.Count);
        Assert.True(loadOrder.IndexOf("plugin-d") < loadOrder.IndexOf("plugin-b"));
        Assert.True(loadOrder.IndexOf("plugin-d") < loadOrder.IndexOf("plugin-c"));
        Assert.True(loadOrder.IndexOf("plugin-b") < loadOrder.IndexOf("plugin-a"));
        Assert.True(loadOrder.IndexOf("plugin-c") < loadOrder.IndexOf("plugin-a"));
    }

    #endregion

    #region Resolve Tests - Missing Dependencies

    [Fact]
    public void Resolve_MissingDependency_ReturnsMissingDependencyError()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-missing"] = ">=1.0.0" })
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        var error = result.Errors[0] as MissingDependencyError;
        Assert.NotNull(error);
        Assert.Equal("plugin-a", error.PluginId);
        Assert.Equal("plugin-missing", error.MissingPluginId);
    }

    [Fact]
    public void Resolve_MultipleMissingDependencies_ReturnsAllErrors()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new()
            {
                ["missing-1"] = ">=1.0.0",
                ["missing-2"] = ">=1.0.0"
            })
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
        Assert.All(result.Errors, e => Assert.IsType<MissingDependencyError>(e));
    }

    #endregion

    #region Resolve Tests - Version Mismatches

    [Fact]
    public void Resolve_VersionMismatch_ReturnsVersionMismatchError()
    {
        // Arrange: plugin-a requires plugin-b >=2.0.0, but only 1.0.0 is installed
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = ">=2.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", version: "1.0.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        var error = result.Errors[0] as VersionMismatchError;
        Assert.NotNull(error);
        Assert.Equal("plugin-a", error.PluginId);
        Assert.Equal("plugin-b", error.DependencyId);
        Assert.Equal("1.0.0", error.InstalledVersion);
    }

    [Fact]
    public void Resolve_CaretConstraintSatisfied_ReturnsSuccess()
    {
        // Arrange: plugin-a requires plugin-b ^1.0.0, plugin-b is 1.5.0
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = "^1.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", version: "1.5.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Resolve_CaretConstraintViolated_ReturnsVersionMismatchError()
    {
        // Arrange: plugin-a requires plugin-b ^1.0.0, plugin-b is 2.0.0
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = "^1.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", version: "2.0.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.IsType<VersionMismatchError>(result.Errors[0]);
    }

    #endregion

    #region Resolve Tests - Circular Dependencies

    [Fact]
    public void Resolve_SimpleCycle_ReturnsCyclicDependencyError()
    {
        // Arrange: A → B → C → A (cycle)
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = ">=1.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", dependencies: new() { ["plugin-c"] = ">=1.0.0" }),
            ["plugin-c"] = CreatePlugin("plugin-c", dependencies: new() { ["plugin-a"] = ">=1.0.0" })
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        var error = result.Errors[0] as CyclicDependencyError;
        Assert.NotNull(error);
        Assert.NotEmpty(error.CyclePath);
    }

    [Fact]
    public void Resolve_SelfDependency_ReturnsCyclicDependencyError()
    {
        // Arrange: A depends on itself
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-a"] = ">=1.0.0" })
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.IsType<CyclicDependencyError>(result.Errors[0]);
    }

    #endregion

    #region Resolve Tests - Editor Compatibility

    [Fact]
    public void Resolve_EditorVersionTooLow_ReturnsEditorVersionIncompatibleError()
    {
        // Arrange: Plugin requires editor >= 2.0.0, but editor is 1.0.0
        var resolver = new PluginDependencyResolver("1.0.0");
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", minEditorVersion: "2.0.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        var error = result.Errors[0] as EditorVersionIncompatibleError;
        Assert.NotNull(error);
        Assert.Equal("plugin-a", error.PluginId);
        Assert.Equal("1.0.0", error.EditorVersion);
    }

    [Fact]
    public void Resolve_EditorVersionTooHigh_ReturnsEditorVersionIncompatibleError()
    {
        // Arrange: Plugin requires editor <= 0.5.0, but editor is 1.0.0
        var resolver = new PluginDependencyResolver("1.0.0");
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", maxEditorVersion: "0.5.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.IsType<EditorVersionIncompatibleError>(result.Errors[0]);
    }

    [Fact]
    public void Resolve_EditorVersionInRange_ReturnsSuccess()
    {
        // Arrange: Plugin requires editor 0.5.0 to 2.0.0, editor is 1.0.0
        var resolver = new PluginDependencyResolver("1.0.0");
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", minEditorVersion: "0.5.0", maxEditorVersion: "2.0.0")
        };

        // Act
        var result = resolver.Resolve(plugins);

        // Assert
        Assert.True(result.IsValid);
    }

    #endregion

    #region CanLoad Tests

    [Fact]
    public void CanLoad_AllDependenciesSatisfied_ReturnsSuccess()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var loaded = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-b"] = CreatePlugin("plugin-b")
        };
        loaded["plugin-b"].State = PluginState.Enabled;

        var manifest = new PluginManifest
        {
            Id = "plugin-a",
            Name = "Plugin A",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "a.dll", Type = "a.Plugin" },
            Dependencies = new() { ["plugin-b"] = ">=1.0.0" }
        };

        // Act
        var result = resolver.CanLoad("plugin-a", manifest, loaded);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CanLoad_MissingDependency_ReturnsError()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var loaded = new Dictionary<string, LoadedPlugin>();

        var manifest = new PluginManifest
        {
            Id = "plugin-a",
            Name = "Plugin A",
            Version = "1.0.0",
            EntryPoint = new PluginEntryPoint { Assembly = "a.dll", Type = "a.Plugin" },
            Dependencies = new() { ["plugin-missing"] = ">=1.0.0" }
        };

        // Act
        var result = resolver.CanLoad("plugin-a", manifest, loaded);

        // Assert
        Assert.False(result.IsValid);
        Assert.IsType<MissingDependencyError>(result.Errors[0]);
    }

    #endregion

    #region CanUnload Tests

    [Fact]
    public void CanUnload_NoDependents_ReturnsTrue()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a")
        };

        // Act
        var (canUnload, blocking) = resolver.CanUnload("plugin-a", plugins);

        // Assert
        Assert.True(canUnload);
        Assert.Empty(blocking);
    }

    [Fact]
    public void CanUnload_WithEnabledDependents_ReturnsFalse()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var pluginB = CreatePlugin("plugin-b");
        pluginB.State = PluginState.Enabled;

        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a"),
            ["plugin-b"] = pluginB
        };
        plugins["plugin-b"].Manifest.Dependencies["plugin-a"] = ">=1.0.0";

        // Act
        var (canUnload, blocking) = resolver.CanUnload("plugin-a", plugins);

        // Assert
        Assert.False(canUnload);
        Assert.Contains("plugin-b", blocking);
    }

    [Fact]
    public void CanUnload_WithDisabledDependents_ReturnsTrue()
    {
        // Arrange
        var resolver = new PluginDependencyResolver(EditorVersion);
        var pluginB = CreatePlugin("plugin-b");
        pluginB.State = PluginState.Disabled;

        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a"),
            ["plugin-b"] = pluginB
        };
        plugins["plugin-b"].Manifest.Dependencies["plugin-a"] = ">=1.0.0";

        // Act
        var (canUnload, blocking) = resolver.CanUnload("plugin-a", plugins);

        // Assert
        Assert.True(canUnload);
        Assert.Empty(blocking);
    }

    #endregion

    #region GetUnloadOrder Tests

    [Fact]
    public void GetUnloadOrder_ReturnsReverseOfLoadOrder()
    {
        // Arrange: A depends on B, B depends on C
        // Load order: C, B, A
        // Unload order: A, B, C
        var resolver = new PluginDependencyResolver(EditorVersion);
        var plugins = new Dictionary<string, LoadedPlugin>
        {
            ["plugin-a"] = CreatePlugin("plugin-a", dependencies: new() { ["plugin-b"] = ">=1.0.0" }),
            ["plugin-b"] = CreatePlugin("plugin-b", dependencies: new() { ["plugin-c"] = ">=1.0.0" }),
            ["plugin-c"] = CreatePlugin("plugin-c")
        };

        // Act
        var unloadOrder = resolver.GetUnloadOrder(plugins).ToList();

        // Assert
        Assert.Equal(3, unloadOrder.Count);
        Assert.True(unloadOrder.IndexOf("plugin-a") < unloadOrder.IndexOf("plugin-b"));
        Assert.True(unloadOrder.IndexOf("plugin-b") < unloadOrder.IndexOf("plugin-c"));
    }

    #endregion
}
