// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Installation;
using KeenEyes.Editor.Plugins.Registry;

namespace KeenEyes.Editor.Tests.Plugins.Installation;

/// <summary>
/// Tests for <see cref="PluginUninstaller"/>.
/// </summary>
public sealed class PluginUninstallerTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly PluginRegistry registry;
    private readonly PluginUninstaller uninstaller;

    public PluginUninstallerTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"PluginUninstallerTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        // Set environment variable to use temp directory
        Environment.SetEnvironmentVariable("KEENEYES_CONFIG_DIR", tempDirectory);

        registry = new PluginRegistry();
        uninstaller = new PluginUninstaller(registry);
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("KEENEYES_CONFIG_DIR", null);

        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    #region CanUninstall Tests

    [Fact]
    public void CanUninstall_WithInstalledPlugin_ReturnsTrue()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "TestPlugin",
            Version = "1.0.0"
        });

        // Act
        var (canUninstall, dependents) = uninstaller.CanUninstall("TestPlugin");

        // Assert
        Assert.True(canUninstall);
        Assert.Empty(dependents);
    }

    [Fact]
    public void CanUninstall_WithNonexistentPlugin_ReturnsFalse()
    {
        // Act
        var (canUninstall, dependents) = uninstaller.CanUninstall("NonexistentPlugin");

        // Assert
        Assert.False(canUninstall);
        Assert.Empty(dependents);
    }

    [Fact]
    public void CanUninstall_WithDependentPlugins_ReturnsFalse()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        var (canUninstall, dependents) = uninstaller.CanUninstall("BasePlugin");

        // Assert
        Assert.False(canUninstall);
        Assert.Single(dependents);
        Assert.Contains("DependentPlugin", dependents);
    }

    #endregion

    #region Uninstall Tests

    [Fact]
    public void Uninstall_WithInstalledPlugin_Succeeds()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "TestPlugin",
            Version = "1.0.0"
        });

        // Act
        var result = uninstaller.Uninstall("TestPlugin");

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.UninstalledPlugins);
        Assert.Contains("TestPlugin", result.UninstalledPlugins);
        Assert.False(registry.IsInstalled("TestPlugin"));
    }

    [Fact]
    public void Uninstall_WithNonexistentPlugin_Fails()
    {
        // Act
        var result = uninstaller.Uninstall("NonexistentPlugin");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not installed", result.ErrorMessage);
    }

    [Fact]
    public void Uninstall_WithDependents_ReturnsDependentsError()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        var result = uninstaller.Uninstall("BasePlugin");

        // Assert
        Assert.False(result.Success);
        Assert.Single(result.DependentPlugins);
        Assert.Contains("DependentPlugin", result.DependentPlugins);
        Assert.True(registry.IsInstalled("BasePlugin"));
    }

    [Fact]
    public void Uninstall_WithForce_IgnoresDependents()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        var result = uninstaller.Uninstall("BasePlugin", force: true);

        // Assert
        Assert.True(result.Success);
        Assert.Contains("BasePlugin", result.UninstalledPlugins);
        Assert.False(registry.IsInstalled("BasePlugin"));
        Assert.True(registry.IsInstalled("DependentPlugin")); // Still installed
    }

    [Fact]
    public void Uninstall_ReportsProgress()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "TestPlugin",
            Version = "1.0.0"
        });

        var progressMessages = new List<string>();

        // Use a synchronous progress reporter to avoid async callback issues
        var progress = new SynchronousProgress<string>(msg => progressMessages.Add(msg));

        // Act
        uninstaller.Uninstall("TestPlugin", progress: progress);

        // Assert
        Assert.NotEmpty(progressMessages);
        Assert.Contains(progressMessages, m => m.Contains("TestPlugin"));
    }

    /// <summary>
    /// Synchronous progress reporter for testing.
    /// </summary>
    private sealed class SynchronousProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value)
        {
            handler(value);
        }
    }

    #endregion

    #region UninstallCascade Tests

    [Fact]
    public void UninstallCascade_RemovesDependentPluginsFirst()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        var result = uninstaller.UninstallCascade("BasePlugin");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.UninstalledPlugins.Count);
        Assert.False(registry.IsInstalled("BasePlugin"));
        Assert.False(registry.IsInstalled("DependentPlugin"));

        // Dependent should be uninstalled before base
        var baseIndex = result.UninstalledPlugins.ToList().IndexOf("BasePlugin");
        var dependentIndex = result.UninstalledPlugins.ToList().IndexOf("DependentPlugin");
        Assert.True(dependentIndex < baseIndex, "Dependent plugin should be uninstalled before base plugin");
    }

    [Fact]
    public void UninstallCascade_WithChainedDependencies_UninstallsInOrder()
    {
        // Arrange: A <- B <- C (C depends on B, B depends on A)
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "PluginA",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "PluginB",
            Version = "1.0.0",
            Dependencies = ["PluginA"]
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "PluginC",
            Version = "1.0.0",
            Dependencies = ["PluginB"]
        });

        // Act
        var result = uninstaller.UninstallCascade("PluginA");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.UninstalledPlugins.Count);

        var order = result.UninstalledPlugins.ToList();
        Assert.True(order.IndexOf("PluginC") < order.IndexOf("PluginB"));
        Assert.True(order.IndexOf("PluginB") < order.IndexOf("PluginA"));
    }

    [Fact]
    public void UninstallCascade_WithNonexistentPlugin_Fails()
    {
        // Act
        var result = uninstaller.UninstallCascade("NonexistentPlugin");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not installed", result.ErrorMessage);
    }

    #endregion

    #region PreviewCascadeUninstall Tests

    [Fact]
    public void PreviewCascadeUninstall_ReturnsPluginsToRemove()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        var plugins = uninstaller.PreviewCascadeUninstall("BasePlugin");

        // Assert
        Assert.Equal(2, plugins.Count);
        Assert.Contains("BasePlugin", plugins);
        Assert.Contains("DependentPlugin", plugins);
    }

    [Fact]
    public void PreviewCascadeUninstall_DoesNotModifyRegistry()
    {
        // Arrange
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "BasePlugin",
            Version = "1.0.0"
        });

        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "DependentPlugin",
            Version = "1.0.0",
            Dependencies = ["BasePlugin"]
        });

        // Act
        _ = uninstaller.PreviewCascadeUninstall("BasePlugin");

        // Assert - Registry should be unchanged
        Assert.True(registry.IsInstalled("BasePlugin"));
        Assert.True(registry.IsInstalled("DependentPlugin"));
    }

    #endregion

    #region UninstallResult Tests

    [Fact]
    public void UninstallResult_Succeeded_HasCorrectProperties()
    {
        // Act
        var result = UninstallResult.Succeeded(["Plugin1", "Plugin2"]);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.UninstalledPlugins.Count);
        Assert.Empty(result.DependentPlugins);
    }

    [Fact]
    public void UninstallResult_Failed_HasErrorMessage()
    {
        // Act
        var result = UninstallResult.Failed("Something went wrong");

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Something went wrong", result.ErrorMessage);
        Assert.Empty(result.UninstalledPlugins);
    }

    [Fact]
    public void UninstallResult_HasDependents_IncludesDependentsList()
    {
        // Act
        var result = UninstallResult.HasDependents(["Dep1", "Dep2"]);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("2 plugin(s)", result.ErrorMessage);
        Assert.Equal(2, result.DependentPlugins.Count);
        Assert.Contains("Dep1", result.DependentPlugins);
        Assert.Contains("Dep2", result.DependentPlugins);
    }

    #endregion
}
