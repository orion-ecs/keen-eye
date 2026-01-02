// Copyright (c) Keen Eye, LLC. All rights reserved.
// Licensed under the MIT License.

using KeenEyes.Cli.Commands.Plugin;
using KeenEyes.Editor.Plugins.Registry;
using Xunit;

namespace KeenEyes.Cli.Tests.Commands;

[Collection("PluginRegistry")]
public sealed class PluginListCommandTests : IDisposable
{
    private readonly string testDir;
    private readonly TestConsoleOutput output;
    private readonly PluginListCommand command;

    public PluginListCommandTests()
    {
        testDir = Path.Combine(Path.GetTempPath(), "keeneyes-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(testDir);
        PluginRegistryPaths.SetTestOverride(testDir);

        output = new TestConsoleOutput();
        command = new PluginListCommand();
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
    public async Task ExecuteAsync_WithNoPlugins_ShowsEmptyMessage()
    {
        var result = await command.ExecuteAsync([], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("No") && l.Contains("plugins installed"));
    }

    [Fact]
    public async Task ExecuteAsync_WithPlugins_ListsPlugins()
    {
        // Setup: Install a plugin
        var registry = new PluginRegistry();
        registry.Load();
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "Test.Plugin",
            Version = "1.0.0",
            Enabled = true
        });
        registry.Save();

        var result = await command.ExecuteAsync([], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("Test.Plugin"));
        Assert.Contains(output.Lines, l => l.Contains("1.0.0"));
    }

    [Fact]
    public async Task ExecuteAsync_WithHelpOption_ShowsHelp()
    {
        var result = await command.ExecuteAsync(["--help"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("Usage:"));
    }

    [Fact]
    public async Task ExecuteAsync_WithEnabledFilter_ShowsOnlyEnabled()
    {
        var registry = new PluginRegistry();
        registry.Load();
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "Enabled.Plugin",
            Version = "1.0.0",
            Enabled = true
        });
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "Disabled.Plugin",
            Version = "1.0.0",
            Enabled = false
        });
        registry.Save();

        var result = await command.ExecuteAsync(["--enabled"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Contains(output.Lines, l => l.Contains("Enabled.Plugin"));
        Assert.DoesNotContain(output.Lines, l => l.Contains("Disabled.Plugin"));
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledFilter_ShowsOnlyDisabled()
    {
        var registry = new PluginRegistry();
        registry.Load();
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "Enabled.Plugin",
            Version = "1.0.0",
            Enabled = true
        });
        registry.RegisterPlugin(new InstalledPluginEntry
        {
            PackageId = "Disabled.Plugin",
            Version = "1.0.0",
            Enabled = false
        });
        registry.Save();

        var result = await command.ExecuteAsync(["--disabled"], output, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(output.Lines, l => l.Contains("Enabled.Plugin"));
        Assert.Contains(output.Lines, l => l.Contains("Disabled.Plugin"));
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownOption_ReturnsInvalidArguments()
    {
        var result = await command.ExecuteAsync(["--unknown"], output, TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(2, result.ExitCode);
    }
}
