// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Editor.Plugins.Registry;
using Xunit;

namespace KeenEyes.Cli.Tests.Registry;

[Collection("PluginRegistry")]
public sealed class PluginRegistryTests : IDisposable
{
    private readonly string testDir;

    public PluginRegistryTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), "keeneyes-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        PluginRegistryPaths.SetTestOverride(testDir);
    }

    public void Dispose()
    {
        PluginRegistryPaths.SetTestOverride(null);

        if (Directory.Exists(testDir))
        {
            try
            {
                Directory.Delete(testDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    [Fact]
    public void Load_WithNoFile_CreatesEmptyRegistry()
    {
        var registry = new PluginRegistry();
        registry.Load();

        Assert.Empty(registry.GetInstalledPlugins());
        Assert.NotEmpty(registry.GetSources()); // Default nuget.org source
    }

    [Fact]
    public void RegisterPlugin_AddsPlugin()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var entry = new InstalledPluginEntry
        {
            PackageId = "Test.Plugin",
            Version = "1.0.0"
        };

        registry.RegisterPlugin(entry);

        Assert.True(registry.IsInstalled("Test.Plugin"));
        Assert.Equal("Test.Plugin", registry.GetInstalledPlugin("Test.Plugin")?.PackageId);
    }

    [Fact]
    public void UnregisterPlugin_RemovesPlugin()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var entry = new InstalledPluginEntry
        {
            PackageId = "Test.Plugin",
            Version = "1.0.0"
        };

        registry.RegisterPlugin(entry);
        Assert.True(registry.IsInstalled("Test.Plugin"));

        registry.UnregisterPlugin("Test.Plugin");
        Assert.False(registry.IsInstalled("Test.Plugin"));
    }

    [Fact]
    public void SaveAndLoad_RoundTripsData()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var entry = new InstalledPluginEntry
        {
            PackageId = "Test.Plugin",
            Version = "1.2.3",
            Source = "https://example.com/v3/index.json",
            Enabled = true
        };

        registry.RegisterPlugin(entry);
        registry.Save();

        // Load in new instance
        var registry2 = new PluginRegistry();
        registry2.Load();

        var loaded = registry2.GetInstalledPlugin("Test.Plugin");
        Assert.NotNull(loaded);
        Assert.Equal("Test.Plugin", loaded.PackageId);
        Assert.Equal("1.2.3", loaded.Version);
        Assert.Equal("https://example.com/v3/index.json", loaded.Source);
        Assert.True(loaded.Enabled);
    }

    [Fact]
    public void GetDependentPlugins_ReturnsPluginsThatDependOnPackage()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var corePlugin = new InstalledPluginEntry
        {
            PackageId = "Core.Plugin",
            Version = "1.0.0"
        };

        var dependentPlugin = new InstalledPluginEntry
        {
            PackageId = "Dependent.Plugin",
            Version = "1.0.0",
            Dependencies = ["Core.Plugin"]
        };

        registry.RegisterPlugin(corePlugin);
        registry.RegisterPlugin(dependentPlugin);

        var dependents = registry.GetDependentPlugins("Core.Plugin");
        Assert.Single(dependents);
        Assert.Equal("Dependent.Plugin", dependents[0].PackageId);
    }

    [Fact]
    public void AddSource_AddsNewSource()
    {
        var registry = new PluginRegistry();
        registry.Load();

        registry.AddSource("mycompany", "https://pkgs.mycompany.com/v3/index.json");
        registry.Save();

        var registry2 = new PluginRegistry();
        registry2.Load();

        var sources = registry2.GetSources();
        Assert.Contains(sources, s => s.Name == "mycompany");
    }

    [Fact]
    public void RemoveSource_RemovesSource()
    {
        var registry = new PluginRegistry();
        registry.Load();

        registry.AddSource("mycompany", "https://pkgs.mycompany.com/v3/index.json");
        Assert.Contains(registry.GetSources(), s => s.Name == "mycompany");

        registry.RemoveSource("mycompany");
        Assert.DoesNotContain(registry.GetSources(), s => s.Name == "mycompany");
    }

    [Fact]
    public void AddSource_WithDefault_ClearsExistingDefault()
    {
        var registry = new PluginRegistry();
        registry.Load();

        // nuget.org should be default
        var defaultSource = registry.GetSources().FirstOrDefault(s => s.IsDefault);
        Assert.NotNull(defaultSource);
        Assert.Equal("nuget.org", defaultSource.Name);

        registry.AddSource("mycompany", "https://pkgs.mycompany.com/v3/index.json", makeDefault: true);

        var sources = registry.GetSources();
        var newDefault = sources.FirstOrDefault(s => s.IsDefault);

        Assert.NotNull(newDefault);
        Assert.Equal("mycompany", newDefault.Name);

        // Old default should no longer be default
        var oldNugetOrg = sources.FirstOrDefault(s => s.Name == "nuget.org");
        Assert.NotNull(oldNugetOrg);
        Assert.False(oldNugetOrg.IsDefault);
    }

    [Fact]
    public void GetDefaultSourceUrl_ReturnsDefaultSource()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var defaultUrl = registry.GetDefaultSourceUrl();
        Assert.Equal("https://api.nuget.org/v3/index.json", defaultUrl);
    }

    [Fact]
    public void IsInstalled_IsCaseInsensitive()
    {
        var registry = new PluginRegistry();
        registry.Load();

        var entry = new InstalledPluginEntry
        {
            PackageId = "Test.Plugin",
            Version = "1.0.0"
        };

        registry.RegisterPlugin(entry);

        Assert.True(registry.IsInstalled("Test.Plugin"));
        Assert.True(registry.IsInstalled("test.plugin"));
        Assert.True(registry.IsInstalled("TEST.PLUGIN"));
    }
}
